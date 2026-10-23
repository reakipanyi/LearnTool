using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Data.Database;
using LearningAssistant.Models.Practice;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Practice
{
    public interface IPracticeRecordService
    {
        Task SaveAsync(PracticeRecord record, CancellationToken cancellationToken = default);
        IReadOnlyList<PracticeRecord> GetRecent(string userId, string? learningItemId = null, PracticeType? type = null, int limit = 50);

        /// <summary>批量取每个学习项最近一次练习记录（列表状态展示用，避免逐项查询）。</summary>
        IReadOnlyDictionary<string, PracticeRecord> GetLatestByItemIds(string userId, IEnumerable<string> learningItemIds);

        PracticeSummary GetSummary(string userId, DateTime? since = null);
    }

    /// <summary>
    /// 练习记录持久化（P2）。保存成功后发布 PracticeCompletedEvent，统计与成就按事件联动。
    /// </summary>
    public sealed class PracticeRecordService : IPracticeRecordService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IEventBus? _eventBus;
        private readonly ILogger<PracticeRecordService>? _logger;

        public PracticeRecordService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IEventBus? eventBus = null,
            ILogger<PracticeRecordService>? logger = null)
        {
            _dbContextFactory = dbContextFactory;
            _eventBus = eventBus;
            _logger = logger;
        }

        public async Task SaveAsync(PracticeRecord record, CancellationToken cancellationToken = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (string.IsNullOrWhiteSpace(record.UserId))
                throw new ArgumentException("用户不能为空", nameof(record));

            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var now = DateTime.Now;

                db.PracticeRecords.Add(new PracticeRecordEntity
                {
                    Id = record.Id,
                    UserId = record.UserId,
                    LearningItemId = record.LearningItemId,
                    ItemContent = record.ItemContent ?? string.Empty,
                    Type = (int)record.Type,
                    SourceType = (int)record.SourceType,
                    Score = record.Score,
                    MaxScore = record.MaxScore,
                    DurationMs = record.DurationMs,
                    RecordingPath = record.RecordingPath,
                    MetaJson = string.IsNullOrWhiteSpace(record.MetaJson) ? "{}" : record.MetaJson,
                    CreatedAt = record.CreatedAt,
                    UpdatedAt = now,
                    IsActive = true,
                    RowVersion = 1
                });

                await db.SaveChangesAsync(cancellationToken);

                _eventBus?.Publish(new PracticeCompletedEvent
                {
                    UserId = record.UserId,
                    RecordId = record.Id,
                    LearningItemId = record.LearningItemId,
                    ItemContent = record.ItemContent,
                    PracticeType = record.Type.ToString(),
                    SourceType = record.SourceType.ToString(),
                    Score = record.Score,
                    MaxScore = record.MaxScore,
                    DurationMs = record.DurationMs,
                    RecordingPath = record.RecordingPath
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存练习记录失败: {ItemContent}", record.ItemContent);
                throw;
            }
        }

        public IReadOnlyList<PracticeRecord> GetRecent(
            string userId,
            string? learningItemId = null,
            PracticeType? type = null,
            int limit = 50)
        {
            if (string.IsNullOrWhiteSpace(userId)) return Array.Empty<PracticeRecord>();
            if (limit <= 0) limit = 50;

            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var query = db.PracticeRecords
                    .Where(p => p.UserId == userId && p.IsActive);

                if (!string.IsNullOrWhiteSpace(learningItemId))
                    query = query.Where(p => p.LearningItemId == learningItemId);

                if (type.HasValue)
                {
                    var typeValue = (int)type.Value;
                    query = query.Where(p => p.Type == typeValue);
                }

                return query
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(limit)
                    .ToList()
                    .Select(ToModel)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "读取练习记录失败: {UserId}", userId);
                return Array.Empty<PracticeRecord>();
            }
        }

        public IReadOnlyDictionary<string, PracticeRecord> GetLatestByItemIds(
            string userId, IEnumerable<string> learningItemIds)
        {
            var result = new Dictionary<string, PracticeRecord>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(userId) || learningItemIds == null) return result;

            var ids = learningItemIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (ids.Count == 0) return result;

            try
            {
                using var db = _dbContextFactory.CreateDbContext();

                // 分批查询，避免超过 SQLite 参数上限
                const int batchSize = 500;
                for (int offset = 0; offset < ids.Count; offset += batchSize)
                {
                    var batch = ids.Skip(offset).Take(batchSize).ToList();
                    var records = db.PracticeRecords
                        .Where(p => p.UserId == userId && p.IsActive
                                    && p.LearningItemId != null
                                    && batch.Contains(p.LearningItemId))
                        .OrderByDescending(p => p.CreatedAt)
                        .ToList();

                    foreach (var entity in records)
                    {
                        var key = entity.LearningItemId!;
                        if (!result.ContainsKey(key))
                            result[key] = ToModel(entity);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "批量读取最近练习记录失败: {UserId}", userId);
            }

            return result;
        }

        public PracticeSummary GetSummary(string userId, DateTime? since = null)
        {
            var summary = new PracticeSummary();
            if (string.IsNullOrWhiteSpace(userId)) return summary;

            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var query = db.PracticeRecords
                    .Where(p => p.UserId == userId && p.IsActive);

                if (since.HasValue)
                    query = query.Where(p => p.CreatedAt >= since.Value);

                var records = query
                    .Select(p => new { p.Type, p.Score, p.CreatedAt })
                    .ToList();

                summary.TotalCount = records.Count;
                summary.ShadowingCount = records.Count(r => r.Type == (int)PracticeType.Shadowing);
                summary.AverageScore = records.Count == 0 ? 0 : records.Average(r => r.Score);

                if (records.Count > 0)
                    summary.LastPracticedAt = records.Max(r => r.CreatedAt);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "汇总练习记录失败: {UserId}", userId);
            }

            return summary;
        }

        private static PracticeRecord ToModel(PracticeRecordEntity entity)
        {
            var type = Enum.IsDefined(typeof(PracticeType), entity.Type)
                ? (PracticeType)entity.Type
                : PracticeType.Shadowing;

            var sourceType = Enum.IsDefined(typeof(StudyContentSource), entity.SourceType)
                ? (StudyContentSource)entity.SourceType
                : StudyContentSource.Manual;

            return new PracticeRecord
            {
                Id = entity.Id,
                UserId = entity.UserId,
                LearningItemId = entity.LearningItemId,
                ItemContent = entity.ItemContent,
                Type = type,
                SourceType = sourceType,
                Score = entity.Score,
                MaxScore = entity.MaxScore,
                DurationMs = entity.DurationMs,
                RecordingPath = entity.RecordingPath,
                MetaJson = entity.MetaJson,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
