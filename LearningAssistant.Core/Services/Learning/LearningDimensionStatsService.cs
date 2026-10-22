using LearningAssistant.Common;
using LearningAssistant.Data.Database;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.Practice;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Learning
{
    /// <summary>统计维度（P4）：知识点 / 适龄段 / 重要等级 / 来源。</summary>
    public enum DimensionKind
    {
        KnowledgePoint,
        AgeRange,
        Importance,
        Source
    }

    /// <summary>单行维度统计：覆盖量、掌握量与练习产出。</summary>
    public sealed class DimensionStatRow
    {
        public string Key { get; init; } = string.Empty;
        public int Total { get; init; }
        public int Known { get; init; }
        public int PracticeCount { get; init; }
        public double AverageScore { get; init; }

        public double KnownRate => Total == 0 ? 0 : (double)Known / Total;
    }

    public interface ILearningDimensionStatsService
    {
        /// <summary>按指定维度汇总当前用户全部学习项（跨学科/子类别）。</summary>
        IReadOnlyList<DimensionStatRow> GetBreakdown(string userId, DimensionKind kind);
    }

    /// <summary>
    /// 分类维度统计（P4-1）：以词库/个人内容为分母，结合学习项状态与练习记录计算掌握率与练习产出。
    /// </summary>
    public sealed class LearningDimensionStatsService : ILearningDimensionStatsService
    {
        private readonly IContentLoaderService _contentLoaderService;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<LearningDimensionStatsService>? _logger;

        public LearningDimensionStatsService(
            IContentLoaderService contentLoaderService,
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<LearningDimensionStatsService>? logger = null)
        {
            _contentLoaderService = contentLoaderService ?? throw new ArgumentNullException(nameof(contentLoaderService));
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger;
        }

        public IReadOnlyList<DimensionStatRow> GetBreakdown(string userId, DimensionKind kind)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Array.Empty<DimensionStatRow>();

            var items = LoadAllItems(userId);
            if (items.Count == 0)
                return Array.Empty<DimensionStatRow>();

            var (knownKeys, knownContents) = LoadKnownStates(userId);
            bool IsKnown(LearningItem item)
            {
                if (!string.IsNullOrWhiteSpace(item.Id) && knownKeys.Contains(item.Id))
                    return true;
                var contentKey = BuildContentKey(item.SubCategory, item.GetMainContent());
                return knownContents.Contains(contentKey);
            }

            return Calculate(items, IsKnown, LoadPracticeRecords(userId), kind);
        }

        /// <summary>遍历全部学科/子类别加载学习项；单一类别失败不影响整体统计。</summary>
        private List<LearningItem> LoadAllItems(string userId)
        {
            var result = new List<LearningItem>();
            foreach (var subject in Enum.GetValues<SubjectType>())
            {
                foreach (var subCategory in _contentLoaderService.GetSubCategories(subject))
                {
                    try
                    {
                        result.AddRange(_contentLoaderService.LoadItems(
                            new LearningContext(userId, subject, subCategory)));
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "加载维度统计学习项失败: {Subject}/{SubCategory}", subject, subCategory);
                    }
                }
            }

            return result;
        }

        private (HashSet<string> Keys, HashSet<string> Contents) LoadKnownStates(string userId)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var contents = new HashSet<string>(StringComparer.Ordinal);

            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var states = db.LearningItemStates.AsNoTracking()
                    .Where(s => s.UserId == userId && s.IsKnown)
                    .Select(s => new { s.ItemKey, s.CategoryName, s.Content })
                    .ToList();

                foreach (var state in states)
                {
                    if (!string.IsNullOrWhiteSpace(state.ItemKey))
                        keys.Add(state.ItemKey);
                    else if (!string.IsNullOrWhiteSpace(state.Content))
                        contents.Add(BuildContentKey(state.CategoryName, state.Content));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "读取学习项状态失败: {UserId}", userId);
            }

            return (keys, contents);
        }

        private List<PracticeRecord> LoadPracticeRecords(string userId)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                return db.PracticeRecords.AsNoTracking()
                    .Where(p => p.UserId == userId && p.IsActive)
                    .Select(p => new PracticeRecord
                    {
                        LearningItemId = p.LearningItemId,
                        ItemContent = p.ItemContent,
                        Score = p.Score,
                        MaxScore = p.MaxScore,
                        CreatedAt = p.CreatedAt
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "读取练习记录失败: {UserId}", userId);
                return new List<PracticeRecord>();
            }
        }

        private static string BuildContentKey(SubCategoryType subCategory, string? content)
            => $"{subCategory}|{LearningItemIdentity.Normalize(content)}";

        private static string BuildContentKey(string? categoryName, string? content)
            => $"{categoryName}|{LearningItemIdentity.Normalize(content)}";

        /// <summary>
        /// 纯计算：按维度聚合覆盖量、掌握量、练习次数与平均分（便于单元测试）。
        /// </summary>
        public static IReadOnlyList<DimensionStatRow> Calculate(
            IEnumerable<LearningItem> items,
            Func<LearningItem, bool> isKnown,
            IEnumerable<PracticeRecord> practices,
            DimensionKind kind)
        {
            var practiceByItem = new Dictionary<string, (int Count, double Total)>(StringComparer.Ordinal);
            var practiceByContent = new Dictionary<string, (int Count, double Total)>(StringComparer.Ordinal);

            foreach (var practice in practices)
            {
                var score = Math.Max(0, practice.Score);
                if (!string.IsNullOrWhiteSpace(practice.LearningItemId))
                {
                    practiceByItem.TryGetValue(practice.LearningItemId, out var current);
                    practiceByItem[practice.LearningItemId] = (current.Count + 1, current.Total + score);
                }

                var contentKey = LearningItemIdentity.Normalize(practice.ItemContent);
                if (!string.IsNullOrWhiteSpace(contentKey))
                {
                    practiceByContent.TryGetValue(contentKey, out var current);
                    practiceByContent[contentKey] = (current.Count + 1, current.Total + score);
                }
            }

            var buckets = new Dictionary<string, (int Total, int Known, int PracticeCount, double TotalScore)>(
                StringComparer.Ordinal);

            foreach (var item in items)
            {
                var key = ResolveKey(item, kind);
                buckets.TryGetValue(key, out var bucket);

                var (practiceCount, totalScore) = ResolvePractice(item, practiceByItem, practiceByContent);

                buckets[key] = (
                    bucket.Total + 1,
                    bucket.Known + (isKnown(item) ? 1 : 0),
                    bucket.PracticeCount + practiceCount,
                    bucket.TotalScore + totalScore);
            }

            return buckets
                .Select(pair => new DimensionStatRow
                {
                    Key = pair.Key,
                    Total = pair.Value.Total,
                    Known = pair.Value.Known,
                    PracticeCount = pair.Value.PracticeCount,
                    AverageScore = pair.Value.PracticeCount == 0
                        ? 0
                        : pair.Value.TotalScore / pair.Value.PracticeCount
                })
                .OrderByDescending(row => row.Total)
                .ThenBy(row => row.Key, StringComparer.Ordinal)
                .ToList();
        }

        private static (int Count, double TotalScore) ResolvePractice(
            LearningItem item,
            IReadOnlyDictionary<string, (int Count, double Total)> byItem,
            IReadOnlyDictionary<string, (int Count, double Total)> byContent)
        {
            if (!string.IsNullOrWhiteSpace(item.Id)
                && byItem.TryGetValue(item.Id, out var byId))
            {
                return byId;
            }

            var contentKey = LearningItemIdentity.Normalize(item.GetMainContent());
            return !string.IsNullOrWhiteSpace(contentKey) && byContent.TryGetValue(contentKey, out var byText)
                ? byText
                : (0, 0);
        }

        private static string ResolveKey(LearningItem item, DimensionKind kind)
        {
            return kind switch
            {
                DimensionKind.KnowledgePoint => string.IsNullOrWhiteSpace(item.KnowledgePointPath)
                    ? "未分类"
                    : item.KnowledgePointPath,
                DimensionKind.AgeRange => item.AgeRange.ToDisplayString(),
                DimensionKind.Importance => item.Importance.ToDisplayString(),
                DimensionKind.Source => LearningItemClassification.GetEffectiveSourceType(item).ToDisplayString(),
                _ => "未知"
            };
        }
    }
}
