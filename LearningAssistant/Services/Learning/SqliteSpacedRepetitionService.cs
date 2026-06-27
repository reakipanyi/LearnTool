using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Data.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 间隔重复学习服务接口 - 基于SM-2/FSRS算法实现科学的复习间隔
    /// </summary>
    public interface ISpacedRepetitionService
    {
        /// <summary>
        /// 计算下次复习间隔
        /// </summary>
        ReviewResult CalculateNextReview(ReviewItem item, int quality);

        /// <summary>
        /// 计算下次复习间隔（使用指定算法）
        /// </summary>
        ReviewResult CalculateNextReview(ReviewItem item, int quality, string algorithmType);

        /// <summary>
        /// 计算下次复习间隔（带复习耗时）
        /// </summary>
        /// <param name="item">复习项</param>
        /// <param name="quality">评分 (0-5)</param>
        /// <param name="durationMs">复习耗时（毫秒）</param>
        ReviewResult CalculateNextReview(ReviewItem item, int quality, int durationMs);

        /// <summary>
        /// 创建新的复习项
        /// </summary>
        ReviewItem CreateNewItem(string userId, string content, string answer = "");

        /// <summary>
        /// 获取待复习项列表
        /// </summary>
        List<ReviewItem> GetItemsDueForReview(string userId, DateTime? date = null);

        /// <summary>
        /// 更新复习项
        /// </summary>
        void UpdateItem(ReviewItem item);

        /// <summary>
        /// 获取用户所有复习项
        /// </summary>
        List<ReviewItem> GetAllItems(string userId);

        /// <summary>
        /// 删除复习项
        /// </summary>
        void DeleteItem(Guid itemId);

        /// <summary>
        /// 获取今日复习数量
        /// </summary>
        int GetTodayReviewCount(string userId);

        /// <summary>
        /// 计算记忆保留率
        /// </summary>
        double CalculateRetentionRate(string userId);

        /// <summary>
        /// 获取可用的算法列表
        /// </summary>
        List<string> GetAvailableAlgorithms();

        /// <summary>
        /// 切换算法
        /// </summary>
        void SetAlgorithm(string algorithmType);

        /// <summary>
        /// 获取当前算法类型
        /// </summary>
        string GetCurrentAlgorithm();

        /// <summary>
        /// 对比两种算法的效果
        /// </summary>
        AlgorithmComparisonResult CompareAlgorithms(string userId);

        /// <summary>
        /// 获取自适应推荐算法
        /// </summary>
        string GetAdaptiveRecommendation(string userId);

        /// <summary>
        /// 获取复习日志（用于分析）
        /// </summary>
        List<ReviewLog> GetReviewLogs(string userId, int days = 30);

        /// <summary>
        /// 获取推荐复习时间（基于遗忘曲线）
        /// </summary>
        DateTime GetRecommendedReviewTime(string userId);
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
        public double Stability { get; set; } = 0;
        public double Difficulty { get; set; } = 5;
        public double Retrievability { get; set; } = 1;
        public int LearningStage { get; set; } = 0;
        public DateTime? LastReviewDate { get; set; }
        public int ReviewCount { get; set; } = 0;

        public string Question
        {
            get => Content;
            set => Content = value;
        }

        public string? AlgorithmType { get; set; }

        public string? Category { get; set; }

        public string? Subject { get; set; }
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
        public int Duration { get; set; }
    }

    public class ReviewLog
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public Guid ContentId { get; set; }
        public int Rating { get; set; }
        public int Interval { get; set; }
        public double? EaseFactor { get; set; }
        public double? Stability { get; set; }
        public double? Difficulty { get; set; }
        public DateTime ReviewTime { get; set; } = DateTime.Now;
        public int Duration { get; set; }
        public string? AlgorithmType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class SqliteSpacedRepetitionService : ISpacedRepetitionService, IDisposable
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<SqliteSpacedRepetitionService>? _logger;
        private readonly IEventBus? _eventBus;
        private bool _disposed = false;

        private ISpacedRepetitionAlgorithm _currentAlgorithm;
        private readonly Dictionary<string, ISpacedRepetitionAlgorithm> _algorithms;

        public SqliteSpacedRepetitionService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<SqliteSpacedRepetitionService>? logger = null,
            IEventBus? eventBus = null)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger;
            _eventBus = eventBus;

            _algorithms = new Dictionary<string, ISpacedRepetitionAlgorithm>
            {
                ["SM-2"] = new SM2Algorithm(),
                ["FSRS"] = new FSRSAlgorithm()
            };
            _currentAlgorithm = _algorithms["SM-2"];

            SubscribeToEvents();
        }

        public List<string> GetAvailableAlgorithms()
        {
            return new List<string>(_algorithms.Keys);
        }

        public void SetAlgorithm(string algorithmType)
        {
            if (_algorithms.TryGetValue(algorithmType, out var algorithm))
            {
                _currentAlgorithm = algorithm;
                _logger?.LogInformation("切换间隔重复算法: {AlgorithmType}", algorithmType);
            }
            else
            {
                _logger?.LogWarning("未知的算法类型: {AlgorithmType}，使用默认 SM-2", algorithmType);
                _currentAlgorithm = _algorithms["SM-2"];
            }
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
            return CalculateNextReview(item, quality, _currentAlgorithm.AlgorithmType);
        }

        public ReviewResult CalculateNextReview(ReviewItem item, int quality, int durationMs)
        {
            var result = CalculateNextReview(item, quality, _currentAlgorithm.AlgorithmType);
            result.Duration = durationMs;
            return result;
        }

        public ReviewResult CalculateNextReview(ReviewItem item, int quality, string algorithmType)
        {
            ISpacedRepetitionAlgorithm algorithm;
            if (!_algorithms.TryGetValue(algorithmType, out var algo))
            {
                algo = _currentAlgorithm;
            }
            algorithm = algo;

            var algorithmResult = algorithm.Calculate(item, quality);

            var result = new ReviewResult
            {
                ShouldReview = algorithmResult.ShouldReview,
                NewInterval = algorithmResult.NewInterval,
                NewRepetitions = algorithmResult.NewRepetitions,
                NewEFactor = algorithmResult.NewEFactor,
                NextReviewDate = DateTime.Today.AddDays(algorithmResult.NewInterval),
                Message = algorithmResult.Message
            };

            item.Interval = algorithmResult.NewInterval;
            item.Repetitions = algorithmResult.NewRepetitions;
            item.EFactor = algorithmResult.NewEFactor;
            item.Stability = algorithmResult.NewStability;
            item.Difficulty = algorithmResult.NewDifficulty;
            item.NextReviewDate = result.NextReviewDate;
            item.LastReviewDate = DateTime.Now;
            item.UpdatedAt = DateTime.Now;
            item.ReviewCount++;
            item.LearningStage = item.Repetitions >= 2 ? 2 : (item.Repetitions > 0 ? 1 : 0);

            if (quality >= 3)
            {
                item.CorrectCount++;
                item.CorrectStreak++;
            }
            else
            {
                item.WrongCount++;
                item.CorrectStreak = 0;
            }

            UpdateItem(item);
            SaveReviewLog(item, quality, algorithm.AlgorithmType, result.Duration);
            _logger?.LogInformation("计算复习结果 [{AlgorithmType}]: 用户 {UserId}, 间隔 {Interval} 天, 重复次数 {Repetitions}",
                algorithm.AlgorithmType, item.UserId, algorithmResult.NewInterval, algorithmResult.NewRepetitions);

            return result;
        }

        private void SaveReviewLog(ReviewItem item, int rating, string algorithmType, int duration = 0)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var log = new ReviewLogEntity
                {
                    UserId = item.UserId,
                    ContentId = item.Id,
                    Rating = rating,
                    Interval = item.Interval,
                    EaseFactor = item.EFactor,
                    Stability = item.Stability,
                    Difficulty = item.Difficulty,
                    ReviewTime = DateTime.Now,
                    Duration = duration,
                    AlgorithmType = algorithmType,
                    CreatedAt = DateTime.Now
                };

                db.ReviewLogs.Add(log);
                db.SaveChanges();
                _logger?.LogDebug("保存复习日志: ContentId {ContentId}, Rating {Rating}, Algorithm {Algorithm}",
                    item.Id, rating, algorithmType);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存复习日志失败: ContentId {ContentId}", item.Id);
            }
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

        public string GetCurrentAlgorithm()
        {
            return _currentAlgorithm.AlgorithmType;
        }

        public AlgorithmComparisonResult CompareAlgorithms(string userId)
        {
            var result = new AlgorithmComparisonResult();

            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var logs = db.ReviewLogs
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.ReviewTime)
                    .Take(100)
                    .ToList();

                if (logs.Count < 10)
                {
                    result.RecommendedAlgorithm = "SM-2";
                    result.Reason = "复习数据不足，使用稳定的 SM-2 算法";
                    result.AlgorithmStats["SM-2"] = new AlgorithmStats { AlgorithmType = "SM-2" };
                    result.AlgorithmStats["FSRS"] = new AlgorithmStats { AlgorithmType = "FSRS" };
                    return result;
                }

                var sm2Logs = logs.Where(l => l.AlgorithmType == "SM-2" || string.IsNullOrEmpty(l.AlgorithmType)).ToList();
                var fsrsLogs = logs.Where(l => l.AlgorithmType == "FSRS").ToList();

                result.AlgorithmStats["SM-2"] = CalculateAlgorithmStats(sm2Logs, _algorithms["SM-2"]);
                result.AlgorithmStats["FSRS"] = CalculateAlgorithmStats(fsrsLogs, _algorithms["FSRS"]);

                double sm2Score = CalculateAlgorithmScore(result.AlgorithmStats["SM-2"]);
                double fsrsScore = CalculateAlgorithmScore(result.AlgorithmStats["FSRS"]);

                if (fsrsLogs.Count >= 10 && fsrsScore > sm2Score * 1.1)
                {
                    result.RecommendedAlgorithm = "FSRS";
                    result.RecommendedScore = fsrsScore;
                    result.Reason = "FSRS 在您的复习数据上表现更优";
                }
                else if (sm2Logs.Count >= 10 && sm2Score >= fsrsScore)
                {
                    result.RecommendedAlgorithm = "SM-2";
                    result.RecommendedScore = sm2Score;
                    result.Reason = "SM-2 算法稳定可靠";
                }
                else
                {
                    result.RecommendedAlgorithm = _currentAlgorithm.AlgorithmType;
                    result.RecommendedScore = _currentAlgorithm.AlgorithmType == "FSRS" ? fsrsScore : sm2Score;
                    result.Reason = "继续使用当前算法";
                }

                _logger?.LogInformation("算法对比完成: 用户 {UserId}, 推荐 {Algorithm}", userId, result.RecommendedAlgorithm);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "算法对比失败: 用户 {UserId}", userId);
                result.RecommendedAlgorithm = "SM-2";
                result.Reason = "分析失败，使用默认 SM-2 算法";
            }

            return result;
        }

        private AlgorithmStats CalculateAlgorithmStats(List<ReviewLogEntity> logs, ISpacedRepetitionAlgorithm algorithm)
        {
            var stats = new AlgorithmStats
            {
                AlgorithmType = algorithm.AlgorithmType,
                TotalReviews = logs.Count
            };

            if (logs.Count == 0)
            {
                stats.AccuracyRate = algorithm.AccuracyScore * 100;
                stats.RetentionRate = algorithm.RecommendedRetention * 100;
                return stats;
            }

            stats.CorrectReviews = logs.Count(l => l.Rating >= 3);
            stats.AccuracyRate = logs.Count > 0 ? (double)stats.CorrectReviews / logs.Count * 100 : 0;

            if (logs.Any(l => l.Interval > 0))
            {
                stats.AverageInterval = logs.Where(l => l.Interval > 0).Average(l => l.Interval);
            }

            stats.RetentionRate = stats.AccuracyRate;

            if (logs.Count >= 5)
            {
                var intervals = logs.Where(l => l.Interval > 0).Select(l => (double)l.Interval).ToList();
                if (intervals.Count >= 2)
                {
                    double mean = intervals.Average();
                    double variance = intervals.Sum(x => Math.Pow(x - mean, 2)) / intervals.Count;
                    double stdDev = Math.Sqrt(variance);
                    stats.ConsistencyScore = Math.Max(0, 100 - stdDev * 2);
                }
            }
            else
            {
                stats.ConsistencyScore = algorithm.AccuracyScore * 100;
            }

            return stats;
        }

        private double CalculateAlgorithmScore(AlgorithmStats stats)
        {
            double accuracyWeight = 0.4;
            double retentionWeight = 0.3;
            double consistencyWeight = 0.2;
            double efficiencyWeight = 0.1;

            double efficiencyScore = stats.TotalReviews > 0 ? Math.Min(100, stats.AverageInterval * 5) : 50;

            return stats.AccuracyRate * accuracyWeight +
                   stats.RetentionRate * retentionWeight +
                   stats.ConsistencyScore * consistencyWeight +
                   efficiencyScore * efficiencyWeight;
        }

        public string GetAdaptiveRecommendation(string userId)
        {
            var comparison = CompareAlgorithms(userId);
            return comparison.RecommendedAlgorithm;
        }

        public List<ReviewLog> GetReviewLogs(string userId, int days = 30)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var startDate = DateTime.Now.AddDays(-days);

                var logs = db.ReviewLogs
                    .Where(r => r.UserId == userId && r.ReviewTime >= startDate)
                    .OrderByDescending(r => r.ReviewTime)
                    .ToList();

                return logs.Select(l => new ReviewLog
                {
                    Id = l.Id,
                    UserId = l.UserId,
                    ContentId = l.ContentId,
                    Rating = l.Rating,
                    Interval = l.Interval,
                    EaseFactor = l.EaseFactor,
                    Stability = l.Stability,
                    Difficulty = l.Difficulty,
                    ReviewTime = l.ReviewTime,
                    Duration = l.Duration,
                    AlgorithmType = l.AlgorithmType,
                    CreatedAt = l.CreatedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取复习日志失败: 用户 {UserId}", userId);
                return new List<ReviewLog>();
            }
        }

        public DateTime GetRecommendedReviewTime(string userId)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var dueItems = db.SpacedRepetitionItems
                    .Where(i => i.UserId == userId && i.IsActive && i.NextReviewDate <= DateTime.Today.AddDays(1))
                    .OrderBy(i => i.NextReviewDate)
                    .Take(10)
                    .ToList();

                if (dueItems.Count == 0)
                {
                    return DateTime.Today.AddHours(20);
                }

                int overdueCount = dueItems.Count(i => i.NextReviewDate < DateTime.Today);
                if (overdueCount > 20)
                {
                    return DateTime.Now.AddMinutes(5);
                }

                var avgInterval = dueItems.Where(i => i.Interval > 0).Average(i => i.Interval);
                if (avgInterval < 3)
                {
                    return DateTime.Today.AddHours(10);
                }
                else if (avgInterval < 7)
                {
                    return DateTime.Today.AddHours(14);
                }
                else
                {
                    return DateTime.Today.AddHours(20);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取推荐复习时间失败: 用户 {UserId}", userId);
                return DateTime.Today.AddHours(20);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            UnsubscribeFromEvents();
            _disposed = true;
        }
    }

}
