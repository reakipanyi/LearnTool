using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using LearningAssistant.Common;
using LearningAssistant.Data.Database;
using LearningAssistant.Services.Persistence;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习数据分析服务实现
    /// </summary>
    public class LearningAnalyticsService : ILearningAnalyticsService
    {
        private readonly ConcurrentDictionary<string, UserAnalyticsData> _userAnalytics = new ConcurrentDictionary<string, UserAnalyticsData>();
        private readonly IDataPersistenceService? _persistenceService;
        private readonly ILogger<LearningAnalyticsService>? _logger;
        private readonly IDbContextFactory<AppDbContext>? _dbContextFactory;
        private readonly ISpacedRepetitionAlgorithm? _sm2Algorithm;
        private readonly ISpacedRepetitionAlgorithm? _fsrsAlgorithm;
        private bool _isLoaded = false;

        public LearningAnalyticsService(
            ILogger<LearningAnalyticsService>? logger = null,
            IDataPersistenceService? persistenceService = null,
            IDbContextFactory<AppDbContext>? dbContextFactory = null)
        {
            _logger = logger;
            _persistenceService = persistenceService;
            _dbContextFactory = dbContextFactory;
            _sm2Algorithm = new SM2Algorithm();
            _fsrsAlgorithm = new FSRSAlgorithm();
        }

        private void EnsureLoaded()
        {
            if (_isLoaded) return;

            try
            {
                LoadFromOldConfig();
                LoadFromUserDirectories();

                _logger?.LogInformation("加载分析数据成功，用户数: {Count}", _userAnalytics.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载分析数据失败");
            }
            finally
            {
                _isLoaded = true;
            }
        }

        private void LoadFromOldConfig()
        {
            var oldPath = AppPaths.AnalyticsPath;
            if (!File.Exists(oldPath)) return;

            try
            {
                var loaded = JsonHelper.LoadFromFile<Dictionary<string, UserAnalyticsData>>(oldPath);
                if (loaded != null)
                {
                    foreach (var kvp in loaded)
                    {
                        _userAnalytics.TryAdd(kvp.Key, kvp.Value);
                    }
                    _logger?.LogInformation("从旧配置目录迁移分析数据，用户数: {Count}", loaded.Count);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "迁移旧分析数据失败");
            }
        }

        private void LoadFromUserDirectories()
        {
            if (!Directory.Exists(AppPaths.UsersDir)) return;

            foreach (var userDir in Directory.EnumerateDirectories(AppPaths.UsersDir))
            {
                var userId = Path.GetFileName(userDir);
                if (string.IsNullOrEmpty(userId)) continue;

                var userPath = AppPaths.GetUserAnalyticsPath(userId);
                if (!File.Exists(userPath)) continue;

                try
                {
                    var userData = JsonHelper.LoadFromFile<UserAnalyticsData>(userPath);
                    if (userData != null && !_userAnalytics.ContainsKey(userId))
                    {
                        _userAnalytics.TryAdd(userId, userData);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "加载用户 {UserId} 的分析数据失败", userId);
                }
            }
        }

        public void RecordActivity(string userId, string activityType, string subCategory, int count = 1)
        {
            EnsureLoaded();

            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger?.LogWarning("尝试记录活动但用户ID为空");
                return;
            }

            if (count <= 0)
            {
                _logger?.LogDebug("尝试记录无效数量的活动: {Count}", count);
                return;
            }

            if (string.IsNullOrWhiteSpace(activityType))
            {
                _logger?.LogWarning("尝试记录空类型的活动");
                return;
            }

            try
            {
                var userData = _userAnalytics.GetOrAdd(userId, _ => new UserAnalyticsData { UserId = userId });

                var today = DateTime.Today;

                var dailyRecord = userData.DailyRecords.GetOrAdd(today, _ => new DailyStatistics { Date = today });

                switch (activityType)
                {
                    case "Learn":
                        dailyRecord.ItemsLearned += count;
                        break;
                    case "Review":
                        dailyRecord.ItemsReviewed += count;
                        break;
                    case "Correct":
                        dailyRecord.CorrectCount += count;
                        break;
                    case "Wrong":
                        dailyRecord.WrongCount += count;
                        break;
                    default:
                        _logger?.LogDebug("未知的活动类型: {ActivityType}", activityType);
                        break;
                }

                if (!string.IsNullOrWhiteSpace(subCategory))
                {
                    userData.CategoryStats.AddOrUpdate(subCategory, count, (_, existing) => existing + count);
                }

                userData.LastLearningDate = today;

                SaveAnalytics();
                _logger?.LogDebug("记录活动: {UserId}, 类型: {ActivityType}, 分类: {Category}, 数量: {Count}", userId, activityType, subCategory, count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "记录活动失败: {UserId}, 类型: {ActivityType}, 数量: {Count}", userId, activityType, count);
            }
        }

        public Dictionary<string, int> GetCategoryStats(string userId)
        {
            EnsureLoaded();
            if (_userAnalytics.TryGetValue(userId, out var userData))
            {
                return new Dictionary<string, int>(userData.CategoryStats);
            }
            return new Dictionary<string, int>();
        }

        public int GetStudyStreak(string userId)
        {
            EnsureLoaded();
            if (!_userAnalytics.TryGetValue(userId, out var userData))
                return 0;

            int streak = 0;
            var checkDate = DateTime.Today;

            while (true)
            {
                if (userData.DailyRecords.TryGetValue(checkDate, out var record) && 
                    (record.ItemsLearned > 0 || record.ItemsReviewed > 0))
                {
                    streak++;
                    checkDate = checkDate.AddDays(-1);
                }
                else
                {
                    break;
                }
            }

            return streak;
        }

        public int GetTotalStudyMinutes(string userId, DateTime startDate)
        {
            EnsureLoaded();
            if (!_userAnalytics.TryGetValue(userId, out var userData))
                return 0;

            int total = 0;
            for (var date = startDate.Date; date <= DateTime.Today; date = date.AddDays(1))
            {
                if (userData.DailyRecords.TryGetValue(date, out var record))
                {
                    total += record.TimeSpentMinutes;
                }
            }
            return total;
        }

        public int GetTotalLearnedItems(string userId, DateTime startDate)
        {
            EnsureLoaded();
            if (!_userAnalytics.TryGetValue(userId, out var userData))
                return 0;

            int total = 0;
            for (var date = startDate.Date; date <= DateTime.Today; date = date.AddDays(1))
            {
                if (userData.DailyRecords.TryGetValue(date, out var record))
                {
                    total += record.ItemsLearned;
                }
            }
            return total;
        }

        public double GetAccuracyRate(string userId, DateTime startDate)
        {
            EnsureLoaded();
            if (!_userAnalytics.TryGetValue(userId, out var userData))
                return 0;

            int correctCount = 0;
            int totalCount = 0;

            for (var date = startDate.Date; date <= DateTime.Today; date = date.AddDays(1))
            {
                if (userData.DailyRecords.TryGetValue(date, out var record))
                {
                    correctCount += record.CorrectCount;
                    totalCount += record.CorrectCount + record.WrongCount;
                }
            }

            return totalCount > 0 ? (double)correctCount / totalCount * 100 : 0;
        }

        public void SaveAnalytics()
        {
            const int maxRetries = 3;
            const int retryDelayMs = 500;

            foreach (var kvp in _userAnalytics)
            {
                var userId = kvp.Key;
                var userData = kvp.Value;
                var userPath = AppPaths.GetUserAnalyticsPath(userId);

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        JsonHelper.SaveToFile(userPath, userData);
                        break;
                    }
                    catch (IOException ex) when (attempt < maxRetries)
                    {
                        _logger?.LogWarning(ex, "保存用户 {UserId} 分析数据失败(尝试 {Attempt}/{MaxRetries})，{Delay}ms后重试", userId, attempt, maxRetries, retryDelayMs);
                        Thread.Sleep(retryDelayMs);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "保存用户 {UserId} 分析数据失败", userId);
                        break;
                    }
                }
            }

            _logger?.LogDebug("保存分析数据成功，用户数: {Count}", _userAnalytics.Count);
        }

        public void LoadAnalytics()
        {
            EnsureLoaded();
        }
        
        #region 为报告服务添加的方法
        
        public DailyStatistics GetDailyStatistics(string userId, DateTime date)
        {
            EnsureLoaded();

            if (_userAnalytics.TryGetValue(userId, out var userData))
            {
                if (userData.DailyRecords.TryGetValue(date, out var record))
                {
                    record.UserId = userId;
                    record.CategoryBreakdown = new Dictionary<string, int>(userData.CategoryStats);
                    record.TotalItems = record.ItemsLearned;
                    record.TotalMinutes = record.TimeSpentMinutes;
                    record.CorrectRate = record.CorrectCount + record.WrongCount > 0 
                        ? (double)record.CorrectCount / (record.CorrectCount + record.WrongCount) 
                        : 0;
                    return record;
                }
            }

            return new DailyStatistics { Date = date, UserId = userId };
        }

        public WeeklyStatistics GetWeeklyStatistics(string userId, int year, int weekNumber)
        {
            EnsureLoaded();
            var weeklyStats = new WeeklyStatistics { Year = year, WeekNumber = weekNumber, UserId = userId };

            if (_userAnalytics.TryGetValue(userId, out var userData))
            {
                var startDate = GetFirstDateOfWeek(year, weekNumber);
                for (int i = 0; i < 7; i++)
                {
                    var date = startDate.AddDays(i);
                    var dailyStats = GetDailyStatistics(userId, date);
                    weeklyStats.TotalItems += dailyStats.TotalItems;
                    weeklyStats.TotalMinutes += dailyStats.TotalMinutes;
                }
                
                // 计算平均正确率
                var correctCount = 0;
                var totalCount = 0;
                for (int i = 0; i < 7; i++)
                {
                    var date = startDate.AddDays(i);
                    if (userData.DailyRecords.TryGetValue(date, out var record))
                    {
                        correctCount += record.CorrectCount;
                        totalCount += record.CorrectCount + record.WrongCount;
                    }
                }
                weeklyStats.CorrectRate = totalCount > 0 ? (double)correctCount / totalCount : 0;
                weeklyStats.CategoryBreakdown = new Dictionary<string, int>(userData.CategoryStats);
            }

            return weeklyStats;
        }

        public MonthlyStatistics GetMonthlyStatistics(string userId, int year, int month)
        {
            EnsureLoaded();
            var monthlyStats = new MonthlyStatistics { Year = year, Month = month, UserId = userId };

            if (_userAnalytics.TryGetValue(userId, out var userData))
            {
                var startDate = new DateTime(year, month, 1);
                var daysInMonth = DateTime.DaysInMonth(year, month);
                
                for (int i = 0; i < daysInMonth; i++)
                {
                    var date = startDate.AddDays(i);
                    var dailyStats = GetDailyStatistics(userId, date);
                    monthlyStats.TotalItems += dailyStats.TotalItems;
                    monthlyStats.TotalMinutes += dailyStats.TotalMinutes;
                }
                
                // 计算平均正确率
                var correctCount = 0;
                var totalCount = 0;
                for (int i = 0; i < daysInMonth; i++)
                {
                    var date = startDate.AddDays(i);
                    if (userData.DailyRecords.TryGetValue(date, out var record))
                    {
                        correctCount += record.CorrectCount;
                        totalCount += record.CorrectCount + record.WrongCount;
                    }
                }
                monthlyStats.CorrectRate = totalCount > 0 ? (double)correctCount / totalCount : 0;
                monthlyStats.CategoryBreakdown = new Dictionary<string, int>(userData.CategoryStats);
            }

            return monthlyStats;
        }

        public List<DailyStatistics> GetLearningTrend(string userId, DateTime startDate, DateTime endDate)
        {
            EnsureLoaded();
            var trend = new List<DailyStatistics>();

            if (_userAnalytics.TryGetValue(userId, out var userData))
            {
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    trend.Add(GetDailyStatistics(userId, date));
                }
            }

            return trend;
        }
        
        private DateTime GetFirstDateOfWeek(int year, int weekNumber)
        {
            var jan1 = new DateTime(year, 1, 1);
            var firstDayOfYear = ISOWeek.GetYear(jan1) == year 
                ? ISOWeek.ToDateTime(year, 1, DayOfWeek.Monday)
                : ISOWeek.ToDateTime(year - 1, ISOWeek.GetWeeksInYear(year - 1), DayOfWeek.Monday).AddDays(7);
            
            return firstDayOfYear.AddDays((weekNumber - 1) * 7);
        }

        #endregion

        #region 间隔重复统计分析

        public double CalculateRetentionRate(string userId)
        {
            if (_dbContextFactory == null)
            {
                _logger?.LogWarning("数据库上下文未注入，无法计算保留率");
                return 0.5;
            }

            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var items = db.SpacedRepetitionItems
                    .Where(i => i.UserId == userId && i.IsActive && i.LastReviewDate.HasValue)
                    .ToList();

                if (items.Count == 0) return 0.5;

                double totalRetention = 0;
                int validCount = 0;

                foreach (var item in items)
                {
                    if (item.LastReviewDate.HasValue)
                    {
                        var daysSinceReview = (DateTime.Now - item.LastReviewDate.Value).TotalDays;
                        double retention;

                        if (item.AlgorithmType == "FSRS" && item.Stability > 0)
                        {
                            retention = _fsrsAlgorithm?.PredictRetention(item.Stability, item.Difficulty, (int)daysSinceReview) ?? 0.5;
                        }
                        else
                        {
                            retention = _sm2Algorithm?.PredictRetention(item.Interval, item.EFactor, (int)daysSinceReview) ?? 0.5;
                        }

                        totalRetention += retention;
                        validCount++;
                    }
                }

                return validCount > 0 ? totalRetention / validCount : 0.5;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "计算保留率失败: {UserId}", userId);
                return 0.5;
            }
        }

        public Dictionary<int, double> GenerateForgettingCurve(string userId, int days = 30)
        {
            var curve = new Dictionary<int, double>();

            if (_dbContextFactory == null)
            {
                for (int i = 0; i <= days; i++)
                {
                    curve[i] = Math.Exp(-i / 10.0);
                }
                return curve;
            }

            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var items = db.SpacedRepetitionItems
                    .Where(i => i.UserId == userId && i.IsActive)
                    .ToList();

                if (items.Count == 0)
                {
                    for (int i = 0; i <= days; i++)
                    {
                        curve[i] = Math.Exp(-i / 10.0);
                    }
                    return curve;
                }

                double avgStability = items.Where(i => i.Stability > 0).Average(i => i.Stability);
                if (avgStability <= 0) avgStability = 10;

                double avgDifficulty = items.Where(i => i.Difficulty > 0).Average(i => i.Difficulty);
                if (avgDifficulty <= 0) avgDifficulty = 5;

                for (int day = 0; day <= days; day++)
                {
                    double retention = _fsrsAlgorithm?.PredictRetention(avgStability, avgDifficulty, day) ??
                                      _sm2Algorithm?.PredictRetention(avgStability, avgDifficulty, day) ??
                                      Math.Exp(-day / avgStability);
                    curve[day] = Math.Round(retention * 100) / 100;
                }

                return curve;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成遗忘曲线失败: {UserId}", userId);
                for (int i = 0; i <= days; i++)
                {
                    curve[i] = Math.Exp(-i / 10.0);
                }
                return curve;
            }
        }

        public Dictionary<DateTime, int> PredictFutureWorkload(string userId, int days = 30)
        {
            var workload = new Dictionary<DateTime, int>();

            if (_dbContextFactory == null)
            {
                var today = DateTime.Today;
                for (int i = 0; i < days; i++)
                {
                    workload[today.AddDays(i)] = 0;
                }
                return workload;
            }

            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var items = db.SpacedRepetitionItems
                    .Where(i => i.UserId == userId && i.IsActive)
                    .ToList();

                var today = DateTime.Today;
                for (int i = 0; i < days; i++)
                {
                    var targetDate = today.AddDays(i);
                    int count = items.Count(i => i.NextReviewDate.Date <= targetDate.Date);
                    workload[targetDate] = count;
                }

                return workload;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "预测复习工作量失败: {UserId}", userId);
                return workload;
            }
        }

        public ReviewEfficiencyStats GetReviewEfficiencyStats(string userId)
        {
            var stats = new ReviewEfficiencyStats();

            if (_dbContextFactory == null)
            {
                return stats;
            }

            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var logs = db.ReviewLogs
                    .Where(r => r.UserId == userId)
                    .ToList();

                if (logs.Count == 0) return stats;

                stats.TotalReviews = logs.Count;
                stats.TotalCorrect = logs.Count(l => l.Rating >= 3);
                stats.TotalWrong = logs.Count(l => l.Rating < 3);
                stats.AverageStability = logs.Where(l => l.Stability.HasValue).Average(l => l.Stability!.Value);
                stats.AverageDifficulty = logs.Where(l => l.Difficulty.HasValue).Average(l => l.Difficulty!.Value);

                stats.RatingDistribution = logs
                    .GroupBy(l => l.Rating)
                    .ToDictionary(g => g.Key, g => g.Count());

                stats.RetentionRate = CalculateRetentionRate(userId);

                int totalDuration = logs.Sum(l => l.Duration);
                stats.ReviewTimePerCard = logs.Count > 0 ? (double)totalDuration / logs.Count / 1000 : 0;

                stats.MostUsedAlgorithm = logs
                    .GroupBy(l => l.AlgorithmType ?? "SM-2")
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? "SM-2";

                return stats;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取复习效率统计失败: {UserId}", userId);
                return stats;
            }
        }

        public List<HeatmapData> GetWeeklyHeatmap(string userId, int weeks = 12)
        {
            var heatmap = new List<HeatmapData>();

            if (_dbContextFactory == null)
            {
                return heatmap;
            }

            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var endDate = DateTime.Today;
                var startDate = endDate.AddDays(-weeks * 7);

                var reviewLogs = db.ReviewLogs
                    .Where(r => r.UserId == userId && r.ReviewTime >= startDate && r.ReviewTime <= endDate)
                    .ToList();

                var reviewCounts = reviewLogs
                    .GroupBy(r => r.ReviewTime.Date)
                    .ToDictionary(g => g.Key, g => g.Count());

                int maxCount = reviewCounts.Values.Max();
                if (maxCount == 0) maxCount = 1;

                for (int i = 0; i < weeks * 7; i++)
                {
                    var date = startDate.AddDays(i);
                    int count = reviewCounts.GetValueOrDefault(date, 0);
                    int level = count == 0 ? 0 : (count < maxCount * 0.25 ? 1 : (count < maxCount * 0.5 ? 2 : (count < maxCount * 0.75 ? 3 : 4)));

                    heatmap.Add(new HeatmapData
                    {
                        Year = date.Year,
                        Week = System.Globalization.ISOWeek.GetWeekOfYear(date),
                        DayOfWeek = (int)date.DayOfWeek,
                        Date = date,
                        Count = count,
                        Level = level
                    });
                }

                return heatmap;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取热力图数据失败: {UserId}", userId);
                return heatmap;
            }
        }

        #endregion
    }

    /// <summary>
    /// 用户分析数据
    /// </summary>
    public class UserAnalyticsData
    {
        public string UserId { get; set; } = string.Empty;
        public DateTime LastLearningDate { get; set; }
        public ConcurrentDictionary<DateTime, DailyStatistics> DailyRecords { get; set; } = new ConcurrentDictionary<DateTime, DailyStatistics>();
        public ConcurrentDictionary<string, int> CategoryStats { get; set; } = new ConcurrentDictionary<string, int>();
    }
}
