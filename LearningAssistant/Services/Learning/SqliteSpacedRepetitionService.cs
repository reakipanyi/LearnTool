using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Data.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 间隔重复学习服务接口 - 基于SM-2算法实现科学的复习间隔
    /// </summary>
    public interface ISpacedRepetitionService
    {
        ReviewResult CalculateNextReview(ReviewItem item, int quality);
        ReviewItem CreateNewItem(string userId, string content, string answer = "");
        List<ReviewItem> GetItemsDueForReview(string userId, DateTime? date = null);
        void UpdateItem(ReviewItem item);
        List<ReviewItem> GetAllItems(string userId);
        void DeleteItem(Guid itemId);
        int GetTodayReviewCount(string userId);
        double CalculateRetentionRate(string userId);
    }

    /// <summary>
    /// 复习项模型 - 包含SM-2算法所需的所有参数
    /// </summary>
    public class ReviewItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Hint { get; set; } = string.Empty;
        public int Interval { get; set; } = 0;
        public int Repetitions { get; set; } = 0;
        public double EFactor { get; set; } = 2.5;
        public DateTime NextReviewDate { get; set; } = DateTime.Now;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public int WrongCount { get; set; } = 0;
        public int CorrectCount { get; set; } = 0;
        public int CorrectStreak { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        public string Question
        {
            get => Content;
            set => Content = value;
        }

        public double Difficulty => EFactor < 2.3 ? 3 : EFactor < 2.5 ? 2 : 1;
    }

    /// <summary>
    /// 复习结果 - 计算后的间隔和难度因子
    /// </summary>
    public class ReviewResult
    {
        public bool ShouldReview { get; set; }
        public int NewInterval { get; set; }
        public int NewRepetitions { get; set; }
        public double NewEFactor { get; set; }
        public DateTime NextReviewDate { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SqliteSpacedRepetitionService : ISpacedRepetitionService, IDisposable
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<SqliteSpacedRepetitionService>? _logger;
        private readonly IEventBus? _eventBus;
        private bool _disposed = false;

        public SqliteSpacedRepetitionService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<SqliteSpacedRepetitionService>? logger = null,
            IEventBus? eventBus = null)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger;
            _eventBus = eventBus;

            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (_eventBus == null) return;

            _eventBus.Subscribe<ItemLearnedEvent>(OnItemLearned);
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventBus == null) return;

            _eventBus.Unsubscribe<ItemLearnedEvent>(OnItemLearned);
        }

        private void OnItemLearned(ItemLearnedEvent evt)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var existing = db.SpacedRepetitionItems
                    .FirstOrDefault(i => i.UserId == evt.UserId && i.Content == evt.ItemContent && i.IsActive);

                if (existing == null)
                {
                    var item = new ReviewItem
                    {
                        UserId = evt.UserId,
                        Content = evt.ItemContent,
                        Answer = evt.ItemContent,
                        Interval = 0,
                        Repetitions = 0,
                        EFactor = 2.5,
                        NextReviewDate = DateTime.Today.AddDays(1),
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    db.SpacedRepetitionItems.Add(item.ToEntity());
                    db.SaveChanges();

                    _logger?.LogInformation("自动加入间隔重复队列: {UserId}, {ItemContent}", 
                        evt.UserId, evt.ItemContent.Length > 30 ? evt.ItemContent.Substring(0, 30) + "..." : evt.ItemContent);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "处理学习项完成事件失败: {ItemContent}", evt.ItemContent);
            }
        }

        public ReviewResult CalculateNextReview(ReviewItem item, int quality)
        {
            var result = new ReviewResult();

            if (quality < 0 || quality > 5)
            {
                result.ShouldReview = true;
                result.Message = "质量评分无效，需要重新学习";
                _logger?.LogWarning("无效的质量评分: {Quality}", quality);
                return result;
            }

            double newEFactor = Math.Max(1.3, item.EFactor + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02)));

            int newInterval;
            int newRepetitions;

            if (quality < 3)
            {
                newRepetitions = 0;
                newInterval = 1;
                result.Message = "需要重新学习";
            }
            else
            {
                if (item.Repetitions == 0)
                {
                    newInterval = 1;
                }
                else if (item.Repetitions == 1)
                {
                    newInterval = 6;
                }
                else
                {
                    newInterval = (int)Math.Round(item.Interval * newEFactor);
                }

                newRepetitions = item.Repetitions + 1;
                result.Message = quality == 5 ? "完美！下次复习将在 {0} 天后" : "继续加油！下次复习将在 {0} 天后";
            }

            var nextReview = DateTime.Today.AddDays(newInterval);

            result.ShouldReview = false;
            result.NewInterval = newInterval;
            result.NewRepetitions = newRepetitions;
            result.NewEFactor = newEFactor;
            result.NextReviewDate = nextReview;

            item.Interval = newInterval;
            item.Repetitions = newRepetitions;
            item.EFactor = newEFactor;
            item.NextReviewDate = nextReview;
            item.UpdatedAt = DateTime.Now;

            if (quality >= 3)
            {
                item.CorrectCount++;
            }
            else
            {
                item.WrongCount++;
            }

            UpdateItem(item);
            _logger?.LogInformation("计算复习结果: 用户 {UserId}, 间隔 {Interval} 天, 重复次数 {Repetitions}, EF {EFactor}",
                item.UserId, newInterval, newRepetitions, newEFactor);

            return result;
        }

        public ReviewItem CreateNewItem(string userId, string content, string answer = "")
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId cannot be null or empty", nameof(userId));

            var item = new ReviewItem
            {
                UserId = userId,
                Content = content,
                Answer = answer,
                Interval = 0,
                Repetitions = 0,
                EFactor = 2.5,
                NextReviewDate = DateTime.Today,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            UpdateItem(item);

            var logContent = content.Length > 30 ? content.Substring(0, 30) + "..." : content;
            _logger?.LogInformation("创建新复习项: 用户 {UserId}, 内容 {Content}", userId, logContent);
            return item;
        }

        public List<ReviewItem> GetItemsDueForReview(string userId, DateTime? date = null)
        {
            var targetDate = date ?? DateTime.Today;

            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var dueItems = db.SpacedRepetitionItems
                    .Where(i => i.UserId == userId && i.IsActive && i.NextReviewDate <= targetDate.Date)
                    .OrderBy(i => i.NextReviewDate)
                    .Select(i => i.ToModel())
                    .ToList();

                _logger?.LogDebug("获取待复习项: 用户 {UserId}, 数量 {Count}", userId, dueItems.Count);
                return dueItems;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取待复习项失败: 用户 {UserId}", userId);
                return new List<ReviewItem>();
            }
        }

        public void UpdateItem(ReviewItem item)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var entity = db.SpacedRepetitionItems.FirstOrDefault(i => i.Id == item.Id);

                if (entity != null)
                {
                    entity.UpdateFromModel(item);
                }
                else
                {
                    db.SpacedRepetitionItems.Add(item.ToEntity());
                }

                db.SaveChanges();
                _logger?.LogDebug("更新复习项: {Id}", item.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "更新复习项失败: {Id}", item.Id);
            }
        }

        public List<ReviewItem> GetAllItems(string userId)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var items = db.SpacedRepetitionItems
                    .Where(i => i.UserId == userId && i.IsActive)
                    .Select(i => i.ToModel())
                    .ToList();

                return items;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取所有复习项失败: 用户 {UserId}", userId);
                return new List<ReviewItem>();
            }
        }

        public void DeleteItem(Guid itemId)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var entity = db.SpacedRepetitionItems.FirstOrDefault(i => i.Id == itemId);

                if (entity != null)
                {
                    entity.IsActive = false;
                    entity.UpdatedAt = DateTime.Now;
                    db.SaveChanges();
                    _logger?.LogInformation("标记复习项为删除: {Id}", itemId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "删除复习项失败: {Id}", itemId);
            }
        }

        public int GetTodayReviewCount(string userId)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var today = DateTime.Today;
                var count = db.SpacedRepetitionItems
                    .Count(i => i.UserId == userId &&
                                i.IsActive &&
                                i.NextReviewDate <= today &&
                                i.UpdatedAt >= today &&
                                i.CorrectCount > 0);

                return count;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取今日复习数量失败: 用户 {UserId}", userId);
                return 0;
            }
        }

        public double CalculateRetentionRate(string userId)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var activeItems = db.SpacedRepetitionItems
                    .Where(i => i.UserId == userId && i.IsActive)
                    .ToList();

                if (activeItems.Count == 0)
                {
                    return 0;
                }

                var totalReviews = activeItems.Sum(i => i.CorrectCount + i.WrongCount);
                if (totalReviews == 0)
                {
                    return 0;
                }

                var correctReviews = activeItems.Sum(i => i.CorrectCount);
                var retentionRate = (double)correctReviews / totalReviews * 100;

                _logger?.LogDebug("计算保持率: 用户 {UserId}, 保持率 {Rate}%", userId, retentionRate);
                return retentionRate;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "计算保持率失败: 用户 {UserId}", userId);
                return 0;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            UnsubscribeFromEvents();
            _disposed = true;
        }
    }

    public static class SpacedRepetitionMappingExtensions
    {
        public static SpacedRepetitionItemEntity ToEntity(this ReviewItem item)
        {
            return new SpacedRepetitionItemEntity
            {
                Id = item.Id,
                UserId = item.UserId,
                Content = item.Content,
                Answer = item.Answer,
                Interval = item.Interval,
                Repetitions = item.Repetitions,
                EFactor = item.EFactor,
                NextReviewDate = item.NextReviewDate,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                WrongCount = item.WrongCount,
                CorrectCount = item.CorrectCount,
                IsActive = item.IsActive
            };
        }

        public static ReviewItem ToModel(this SpacedRepetitionItemEntity entity)
        {
            return new ReviewItem
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Content = entity.Content,
                Answer = entity.Answer,
                Interval = entity.Interval,
                Repetitions = entity.Repetitions,
                EFactor = entity.EFactor,
                NextReviewDate = entity.NextReviewDate,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                WrongCount = entity.WrongCount,
                CorrectCount = entity.CorrectCount,
                IsActive = entity.IsActive
            };
        }

        public static void UpdateFromModel(this SpacedRepetitionItemEntity entity, ReviewItem item)
        {
            entity.Content = item.Content;
            entity.Answer = item.Answer;
            entity.Interval = item.Interval;
            entity.Repetitions = item.Repetitions;
            entity.EFactor = item.EFactor;
            entity.NextReviewDate = item.NextReviewDate;
            entity.UpdatedAt = item.UpdatedAt;
            entity.WrongCount = item.WrongCount;
            entity.CorrectCount = item.CorrectCount;
            entity.IsActive = item.IsActive;
        }
    }
}
