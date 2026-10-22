using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LearningAssistant.Data.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习数据分析服务实现（纯 SQLite 读取）
    /// 数据来源：DailyRollups（日聚合）+ LearningRecords（明细）+ ReviewLogs / SpacedRepetitionItems（记忆分析）。
    /// 写入统一由 LearningStatsAggregator 的事件订阅链路负责，本服务不再写入任何存储。
    /// </summary>
    public class LearningAnalyticsService : ILearningAnalyticsService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<LearningAnalyticsService>? _logger;
        private readonly ISpacedRepetitionAlgorithm _sm2Algorithm;
        private readonly ISpacedRepetitionAlgorithm _fsrsAlgorithm;

        public LearningAnalyticsService(
            IDbContextFactory<AppDbContext> dbFactory,
            ILogger<LearningAnalyticsService>? logger = null)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _logger = logger;
            _sm2Algorithm = new SM2Algorithm();
            _fsrsAlgorithm = new FSRSAlgorithm();
        }

        /// <summary>
        /// 记录学习活动（空操作）。
        /// 写入链路统一由 LearningStatsAggregator 订阅 ItemLearnedEvent / ItemWrongEvent / ReviewDoneEvent /
        /// PomodoroCompletedEvent / LearningSessionCompletedEvent 后批量落库（LearningRecords + DailyRollups）。
        /// 保留该方法仅为满足接口契约，禁止在此写入 JSON 或任何文件。
        /// </summary>
        public void RecordActivity(string userId, string activityType, string subCategory, int count = 1)
        {
            // 统一写入由 LearningStatsAggregator 事件驱动，此处不做任何持久化。
        }

        /// <summary>
        /// 保存分析数据（空操作）。统计已全部落到 SQLite，无需额外的文件持久化。
        /// </summary>
        public void SaveAnalytics()
        {
            // 统计写入由 LearningStatsAggregator 负责，无 JSON 快照需要保存。
        }

        /// <summary>
        /// 加载分析数据（空操作）。读取始终直接查询 SQLite，无需预加载缓存。
        /// </summary>
        public void LoadAnalytics()
        {
            // 读取按需直查数据库，无缓存需要加载。
        }

        #region 基础统计

        public Dictionary<string, int> GetCategoryStats(string userId)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(userId))
                return result;

            try
            {
                using var db = _dbFactory.CreateDbContext();
                var rows = db.LearningRecords.AsNoTracking()
                    .Where(r => r.UserId == userId)
                    .Select(r => new { r.SubCategory, r.Count })
                    .ToList();

                foreach (var row in rows)
                {
                    if (string.IsNullOrWhiteSpace(row.SubCategory))
                        continue;
                    result[row.SubCategory] = result.GetValueOrDefault(row.SubCategory) + row.Count;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取分类统计失败: {UserId}", userId);
            }

            return result;
        }

        public int GetStudyStreak(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return 0;

            try
            {
                using var db = _dbFactory.CreateDbContext();

                var activeDates = new HashSet<DateTime>();
                var rollupDates = db.DailyRollups.AsNoTracking()
                    .Where(r => r.UserId == userId && (r.ItemsStudied > 0 || r.TimeSpentMinutes > 0))
                    .Select(r => r.Date)
                    .ToList();
                foreach (var date in rollupDates)
                    activeDates.Add(date.Date);

                if (activeDates.Count == 0)
                {
                    var recordDates = db.LearningRecords.AsNoTracking()
                        .Where(r => r.UserId == userId)
                        .Select(r => r.RecordDate)
                        .ToList();
                    foreach (var date in recordDates)
                        activeDates.Add(date.Date);
                }

                if (activeDates.Count == 0)
                    return 0;

                // 今天无记录时允许从昨天起算，保持“未中断”的语义
                var cursor = DateTime.Today;
                if (!activeDates.Contains(cursor))
                    cursor = cursor.AddDays(-1);

                var streak = 0;
                while (activeDates.Contains(cursor))
                {
                    streak++;
                    cursor = cursor.AddDays(-1);
                }

                return streak;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "计算连续学习天数失败: {UserId}", userId);
                return 0;
            }
        }

        public int GetTotalStudyMinutes(string userId, DateTime startDate)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return 0;

            try
            {
                using var db = _dbFactory.CreateDbContext();
                var start = startDate.Date;
                var end = DateTime.Today;

                return db.DailyRollups.AsNoTracking()
                    .Where(r => r.UserId == userId && r.Date.Date >= start && r.Date.Date <= end)
                    .Sum(r => r.TimeSpentMinutes);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "统计总学习时长失败: {UserId}", userId);
                return 0;
            }
        }

        public int GetTotalLearnedItems(string userId, DateTime startDate)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return 0;

            try
            {
                using var db = _dbFactory.CreateDbContext();
                var start = startDate.Date;
                var end = DateTime.Today;

                var rollups = db.DailyRollups.AsNoTracking()
                    .Where(r => r.UserId == userId && r.Date.Date >= start && r.Date.Date <= end)
                    .Select(r => r.ItemsStudied)
                    .ToList();

                if (rollups.Count > 0)
                    return rollups.Sum();

                // 无日聚合时回退到明细表的 Learn 记录
                return db.LearningRecords.AsNoTracking()
                    .Where(r => r.UserId == userId && r.ActivityType == "Learn"
                                && r.RecordDate.Date >= start && r.RecordDate.Date <= end)
                    .Sum(r => (int?)r.Count) ?? 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "统计总学习项目数失败: {UserId}", userId);
                return 0;
            }
        }

        public double GetAccuracyRate(string userId, DateTime startDate)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return 0;

            try
            {
                using var db = _dbFactory.CreateDbContext();
                var start = startDate.Date;
                var end = DateTime.Today;

                var rollups = db.DailyRollups.AsNoTracking()
                    .Where(r => r.UserId == userId && r.Date.Date >= start && r.Date.Date <= end)
                    .Select(r => new { r.CorrectCount, r.WrongCount })
                    .ToList();

                int correctCount = rollups.Sum(r => r.CorrectCount);
                int wrongCount = rollups.Sum(r => r.WrongCount);

                if (correctCount + wrongCount == 0)
                {
                    var records = db.LearningRecords.AsNoTracking()
                        .Where(r => r.UserId == userId
                                    && (r.ActivityType == "Correct" || r.ActivityType == "Wrong")
                                    && r.RecordDate.Date >= start && r.RecordDate.Date <= end)
                        .Select(r => new { r.ActivityType, r.Count })
                        .ToList();

                    correctCount = records.Where(r => r.ActivityType == "Correct").Sum(r => r.Count);
                    wrongCount = records.Where(r => r.ActivityType == "Wrong").Sum(r => r.Count);
                }

                return Math.Round(AccuracyPercent(correctCount, wrongCount), 2);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "统计正确率失败: {UserId}", userId);
                return 0;
            }
        }

        #endregion

        #region 日/周/月统计

        public DailyStatistics GetDailyStatistics(string userId, DateTime date)
        {
            var day = date.Date;

            if (string.IsNullOrWhiteSpace(userId))
                return EmptyDailyStatistics(userId ?? string.Empty, day);

            try
            {
                using var db = _dbFactory.CreateDbContext();

                var rollup = db.DailyRollups.AsNoTracking()
                    .FirstOrDefault(r => r.UserId == userId && r.Date.Date == day);

                var records = db.LearningRecords.AsNoTracking()
                    .Where(r => r.UserId == userId && r.RecordDate.Date == day)
                    .ToList();

                return BuildDailyStatistics(userId, day, rollup, records);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取每日统计失败: {UserId}, {Date}", userId, date);
                return EmptyDailyStatistics(userId, day);
            }
        }

        public WeeklyStatistics GetWeeklyStatistics(string userId, int year, int weekNumber)
        {
            var weeklyStats = new WeeklyStatistics
            {
                Year = year,
                WeekNumber = weekNumber,
                UserId = userId ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(userId))
                return weeklyStats;

            try
            {
                var startDate = GetFirstDateOfWeek(year, weekNumber);
                var endDate = startDate.AddDays(6);

                using var db = _dbFactory.CreateDbContext();
                var rollups = db.DailyRollups.AsNoTracking()
                    .Where(r => r.UserId == userId && r.Date.Date >= startDate && r.Date.Date <= endDate)
                    .ToList();
                var records = db.LearningRecords.AsNoTracking()
                    .Where(r => r.UserId == userId && r.RecordDate.Date >= startDate && r.RecordDate.Date <= endDate)
                    .ToList();

                var aggregated = AggregatePeriod(rollups, records);
                weeklyStats.TotalMinutes = aggregated.TotalMinutes;
                weeklyStats.TotalItems = aggregated.TotalItems;
                weeklyStats.CorrectRate = aggregated.CorrectRate;
                weeklyStats.CategoryBreakdown = aggregated.CategoryBreakdown;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取周统计失败: {UserId}, {Year}-W{Week}", userId, year, weekNumber);
            }

            return weeklyStats;
        }

        public MonthlyStatistics GetMonthlyStatistics(string userId, int year, int month)
        {
            var monthlyStats = new MonthlyStatistics
            {
                Year = year,
                Month = month,
                UserId = userId ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(userId))
                return monthlyStats;

            try
            {
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                using var db = _dbFactory.CreateDbContext();
                var rollups = db.DailyRollups.AsNoTracking()
                    .Where(r => r.UserId == userId && r.Date.Date >= startDate && r.Date.Date <= endDate)
                    .ToList();
                var records = db.LearningRecords.AsNoTracking()
                    .Where(r => r.UserId == userId && r.RecordDate.Date >= startDate && r.RecordDate.Date <= endDate)
                    .ToList();

                var aggregated = AggregatePeriod(rollups, records);
                monthlyStats.TotalMinutes = aggregated.TotalMinutes;
                monthlyStats.TotalItems = aggregated.TotalItems;
                monthlyStats.CorrectRate = aggregated.CorrectRate;
                monthlyStats.CategoryBreakdown = aggregated.CategoryBreakdown;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取月统计失败: {UserId}, {Year}-{Month}", userId, year, month);
            }

            return monthlyStats;
        }

        public List<DailyStatistics> GetLearningTrend(string userId, DateTime startDate, DateTime endDate)
        {
            var trend = new List<DailyStatistics>();

            if (string.IsNullOrWhiteSpace(userId))
                return trend;

            var start = startDate.Date;
            var end = endDate.Date;
            if (end < start)
                return trend;

            try
            {
                using var db = _dbFactory.CreateDbContext();

                var rollups = db.DailyRollups.AsNoTracking()
                    .Where(r => r.UserId == userId && r.Date.Date >= start && r.Date.Date <= end)
                    .ToList()
                    .GroupBy(r => r.Date.Date)
                    .ToDictionary(g => g.Key, g => g.First());

                var recordsByDate = db.LearningRecords.AsNoTracking()
                    .Where(r => r.UserId == userId && r.RecordDate.Date >= start && r.RecordDate.Date <= end)
                    .ToList()
                    .GroupBy(r => r.RecordDate.Date)
                    .ToDictionary(g => g.Key, g => g.ToList());

                for (var date = start; date <= end; date = date.AddDays(1))
                {
                    rollups.TryGetValue(date, out var rollup);
                    recordsByDate.TryGetValue(date, out var records);
                    trend.Add(BuildDailyStatistics(userId, date, rollup, records ?? new List<LearningRecordEntity>()));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取学习趋势失败: {UserId}", userId);
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

        private static DailyStatistics EmptyDailyStatistics(string userId, DateTime date) => new DailyStatistics
        {
            Date = date.Date,
            UserId = userId,
            Year = date.Year,
            WeekNumber = ISOWeek.GetWeekOfYear(date)
        };

        /// <summary>整合单日数据：日聚合优先（时长/数量/正确率），分类明细取自明细表。</summary>
        private static DailyStatistics BuildDailyStatistics(
            string userId,
            DateTime date,
            DailyRollupEntity? rollup,
            List<LearningRecordEntity> records)
        {
            int learned = 0, reviewed = 0, correct = 0, wrong = 0;
            var breakdown = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var record in records)
            {
                switch (record.ActivityType)
                {
                    case "Learn": learned += record.Count; break;
                    case "Review": reviewed += record.Count; break;
                    case "Correct": correct += record.Count; break;
                    case "Wrong": wrong += record.Count; break;
                }

                if (!string.IsNullOrWhiteSpace(record.SubCategory))
                    breakdown[record.SubCategory] = breakdown.GetValueOrDefault(record.SubCategory) + record.Count;
            }

            var correctCount = rollup != null ? rollup.CorrectCount : correct;
            var wrongCount = rollup != null ? rollup.WrongCount : wrong;
            var timeSpentMinutes = rollup?.TimeSpentMinutes ?? 0;
            var totalItems = rollup != null ? rollup.ItemsStudied : learned + reviewed;
            var accuracy = rollup != null && rollup.Accuracy > 0
                ? rollup.Accuracy
                : AccuracyPercent(correctCount, wrongCount);

            return new DailyStatistics
            {
                Date = date.Date,
                UserId = userId,
                Year = date.Year,
                WeekNumber = ISOWeek.GetWeekOfYear(date),
                TotalMinutes = timeSpentMinutes,
                TotalItems = totalItems,
                CorrectRate = Math.Round(accuracy, 2),
                CategoryBreakdown = breakdown,
                ItemsLearned = learned,
                ItemsReviewed = reviewed,
                CorrectCount = correctCount,
                WrongCount = wrongCount,
                TimeSpentMinutes = timeSpentMinutes
            };
        }

        /// <summary>汇总一个日期区间：优先使用日聚合，缺失时回退到明细表。</summary>
        private static (int TotalMinutes, int TotalItems, double CorrectRate, Dictionary<string, int> CategoryBreakdown)
            AggregatePeriod(List<DailyRollupEntity> rollups, List<LearningRecordEntity> records)
        {
            int totalMinutes = rollups.Sum(r => r.TimeSpentMinutes);
            int totalItems;
            int correctCount;
            int wrongCount;

            if (rollups.Count > 0)
            {
                totalItems = rollups.Sum(r => r.ItemsStudied);
                correctCount = rollups.Sum(r => r.CorrectCount);
                wrongCount = rollups.Sum(r => r.WrongCount);
            }
            else
            {
                totalItems = records.Where(r => r.ActivityType is "Learn" or "Review").Sum(r => r.Count);
                correctCount = records.Where(r => r.ActivityType == "Correct").Sum(r => r.Count);
                wrongCount = records.Where(r => r.ActivityType == "Wrong").Sum(r => r.Count);
            }

            var breakdown = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var record in records)
            {
                if (string.IsNullOrWhiteSpace(record.SubCategory))
                    continue;
                breakdown[record.SubCategory] = breakdown.GetValueOrDefault(record.SubCategory) + record.Count;
            }

            return (totalMinutes, totalItems, Math.Round(AccuracyPercent(correctCount, wrongCount), 2), breakdown);
        }

        private static double AccuracyPercent(int correctCount, int wrongCount)
        {
            var total = correctCount + wrongCount;
            return total > 0 ? correctCount * 100.0 / total : 0;
        }

        #endregion

        #region 间隔重复统计分析

        public double CalculateRetentionRate(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return 0.5;

            try
            {
                using var db = _dbFactory.CreateDbContext();
                var items = db.SpacedRepetitionItems.AsNoTracking()
                    .Where(i => i.UserId == userId && i.IsActive && i.LastReviewDate.HasValue)
                    .Select(i => new
                    {
                        i.LastReviewDate,
                        i.AlgorithmType,
                        i.Stability,
                        i.Difficulty,
                        i.Interval,
                        i.EFactor
                    })
                    .ToList();

                if (items.Count == 0) return 0.5;

                double totalRetention = 0;
                int validCount = 0;
                var now = DateTime.Now;

                foreach (var item in items)
                {
                    if (!item.LastReviewDate.HasValue)
                        continue;

                    var daysSinceReview = Math.Max(0, (int)(now - item.LastReviewDate.Value).TotalDays);
                    double retention;

                    if (item.AlgorithmType == "FSRS" && item.Stability > 0)
                    {
                        retention = _fsrsAlgorithm.PredictRetention(item.Stability, item.Difficulty, daysSinceReview);
                    }
                    else
                    {
                        retention = _sm2Algorithm.PredictRetention(item.Interval, item.EFactor, daysSinceReview);
                    }

                    totalRetention += retention;
                    validCount++;
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

            if (string.IsNullOrWhiteSpace(userId) || days < 0)
                return BuildDefaultCurve(curve, Math.Max(0, days));

            try
            {
                using var db = _dbFactory.CreateDbContext();
                var items = db.SpacedRepetitionItems.AsNoTracking()
                    .Where(i => i.UserId == userId && i.IsActive)
                    .Select(i => new { i.Stability, i.Difficulty })
                    .ToList();

                if (items.Count == 0)
                    return BuildDefaultCurve(curve, days);

                var stabilityValues = items.Where(i => i.Stability > 0).Select(i => i.Stability).ToList();
                double avgStability = stabilityValues.Count > 0 ? stabilityValues.Average() : 10;
                if (avgStability <= 0) avgStability = 10;

                var difficultyValues = items.Where(i => i.Difficulty > 0).Select(i => i.Difficulty).ToList();
                double avgDifficulty = difficultyValues.Count > 0 ? difficultyValues.Average() : 5;
                if (avgDifficulty <= 0) avgDifficulty = 5;

                for (int day = 0; day <= days; day++)
                {
                    double retention = _fsrsAlgorithm.PredictRetention(avgStability, avgDifficulty, day);
                    curve[day] = Math.Round(retention * 100) / 100;
                }

                return curve;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成遗忘曲线失败: {UserId}", userId);
                return BuildDefaultCurve(curve, days);
            }
        }

        private static Dictionary<int, double> BuildDefaultCurve(Dictionary<int, double> curve, int days)
        {
            for (int i = 0; i <= days; i++)
            {
                curve[i] = Math.Exp(-i / 10.0);
            }
            return curve;
        }

        public Dictionary<DateTime, int> PredictFutureWorkload(string userId, int days = 30)
        {
            var workload = new Dictionary<DateTime, int>();
            var today = DateTime.Today;

            if (string.IsNullOrWhiteSpace(userId) || days <= 0)
                return workload;

            try
            {
                using var db = _dbFactory.CreateDbContext();
                var nextReviewDates = db.SpacedRepetitionItems.AsNoTracking()
                    .Where(i => i.UserId == userId && i.IsActive)
                    .Select(i => i.NextReviewDate)
                    .ToList()
                    .Select(d => d.Date)
                    .ToList();

                for (int i = 0; i < days; i++)
                {
                    var targetDate = today.AddDays(i);
                    workload[targetDate] = nextReviewDates.Count(d => d <= targetDate);
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

            if (string.IsNullOrWhiteSpace(userId))
                return stats;

            try
            {
                using var db = _dbFactory.CreateDbContext();
                var logs = db.ReviewLogs.AsNoTracking()
                    .Where(r => r.UserId == userId)
                    .Select(r => new { r.Rating, r.Stability, r.Difficulty, r.Duration, r.AlgorithmType })
                    .ToList();

                if (logs.Count == 0) return stats;

                stats.TotalReviews = logs.Count;
                stats.TotalCorrect = logs.Count(l => l.Rating >= 3);
                stats.TotalWrong = logs.Count(l => l.Rating < 3);

                var stabilityValues = logs.Where(l => l.Stability.HasValue).Select(l => l.Stability!.Value).ToList();
                stats.AverageStability = stabilityValues.Count > 0 ? stabilityValues.Average() : 0;

                var difficultyValues = logs.Where(l => l.Difficulty.HasValue).Select(l => l.Difficulty!.Value).ToList();
                stats.AverageDifficulty = difficultyValues.Count > 0 ? difficultyValues.Average() : 0;

                stats.RatingDistribution = logs
                    .GroupBy(l => l.Rating)
                    .ToDictionary(g => g.Key, g => g.Count());

                stats.RetentionRate = CalculateRetentionRate(userId);

                long totalDuration = logs.Sum(l => (long)l.Duration);
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

            if (string.IsNullOrWhiteSpace(userId) || weeks <= 0)
                return heatmap;

            try
            {
                using var db = _dbFactory.CreateDbContext();
                var endDate = DateTime.Today;
                var startDate = endDate.AddDays(-weeks * 7);

                // 学习明细优先（含学习/复习/对错），无明细时回退到复习日志
                var records = db.LearningRecords.AsNoTracking()
                    .Where(r => r.UserId == userId && r.RecordDate.Date >= startDate && r.RecordDate.Date <= endDate)
                    .Select(r => new { r.RecordDate, r.ActivityType, r.Count })
                    .ToList()
                    .Where(r => r.ActivityType != "StudyTime")
                    .ToList();

                var counts = new Dictionary<DateTime, int>();
                if (records.Count > 0)
                {
                    foreach (var group in records.GroupBy(r => r.RecordDate.Date))
                    {
                        counts[group.Key] = group.Sum(r => r.Count);
                    }
                }
                else
                {
                    var reviewTimes = db.ReviewLogs.AsNoTracking()
                        .Where(r => r.UserId == userId && r.ReviewTime.Date >= startDate && r.ReviewTime.Date <= endDate)
                        .Select(r => r.ReviewTime)
                        .ToList();

                    foreach (var group in reviewTimes.GroupBy(d => d.Date))
                    {
                        counts[group.Key] = group.Count();
                    }
                }

                int maxCount = counts.Count > 0 ? counts.Values.Max() : 0;
                if (maxCount <= 0) maxCount = 1;

                for (int i = 0; i < weeks * 7; i++)
                {
                    var date = startDate.AddDays(i);
                    int count = counts.GetValueOrDefault(date, 0);
                    int level = count == 0
                        ? 0
                        : (count < maxCount * 0.25 ? 1 : (count < maxCount * 0.5 ? 2 : (count < maxCount * 0.75 ? 3 : 4)));

                    heatmap.Add(new HeatmapData
                    {
                        Year = date.Year,
                        Week = ISOWeek.GetWeekOfYear(date),
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

        #region P-007 多维分析与学习效率

        /// <summary>
        /// 获取多维交叉分析（P-007）
        /// 按学科、分类、时间段交叉分析学习数据
        /// </summary>
        public MultiDimensionAnalysis GetMultiDimensionAnalysis(string userId, DateTime startDate, DateTime endDate)
        {
            var result = new MultiDimensionAnalysis();

            if (string.IsNullOrWhiteSpace(userId) || endDate.Date < startDate.Date)
                return result;

            try
            {
                var start = startDate.Date;
                var end = endDate.Date;

                using var db = _dbFactory.CreateDbContext();

                var rollups = db.DailyRollups.AsNoTracking()
                    .Where(r => r.UserId == userId && r.Date.Date >= start && r.Date.Date <= end)
                    .Select(r => new { r.Date, r.ItemsStudied, r.CorrectCount, r.WrongCount })
                    .ToList();

                var records = db.LearningRecords.AsNoTracking()
                    .Where(r => r.UserId == userId && r.RecordDate.Date >= start && r.RecordDate.Date <= end)
                    .Select(r => new { r.RecordDate, r.ActivityType, r.SubCategory, r.Count })
                    .ToList();

                // 按分类聚合学习数据
                var categoryBreakdown = new Dictionary<string, int>();
                foreach (var record in records)
                {
                    if (string.IsNullOrWhiteSpace(record.SubCategory))
                        continue;
                    categoryBreakdown[record.SubCategory] = categoryBreakdown.GetValueOrDefault(record.SubCategory) + record.Count;
                }

                int totalItems = rollups.Count > 0
                    ? rollups.Sum(r => r.ItemsStudied)
                    : records.Where(r => r.ActivityType is "Learn" or "Review").Sum(r => r.Count);
                int totalCorrect = rollups.Count > 0
                    ? rollups.Sum(r => r.CorrectCount)
                    : records.Where(r => r.ActivityType == "Correct").Sum(r => r.Count);
                int totalWrong = rollups.Count > 0
                    ? rollups.Sum(r => r.WrongCount)
                    : records.Where(r => r.ActivityType == "Wrong").Sum(r => r.Count);

                // 按星期几统计学习量
                var dayNames = new[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
                var dailyItems = new Dictionary<DateTime, int>();
                if (rollups.Count > 0)
                {
                    foreach (var group in rollups.GroupBy(r => r.Date.Date))
                        dailyItems[group.Key] = group.Sum(r => r.ItemsStudied);
                }
                else
                {
                    foreach (var group in records
                                 .Where(r => r.ActivityType is "Learn" or "Review")
                                 .GroupBy(r => r.RecordDate.Date))
                        dailyItems[group.Key] = group.Sum(r => r.Count);
                }

                var weeklyPattern = new Dictionary<string, int>();
                foreach (var pair in dailyItems)
                {
                    var dayName = dayNames[(int)pair.Key.DayOfWeek];
                    weeklyPattern[dayName] = weeklyPattern.GetValueOrDefault(dayName) + pair.Value;
                }

                // SubjectCategoryBreakdown: 使用"全部"作为学科键，包含分类明细
                result.SubjectCategoryBreakdown["全部"] = categoryBreakdown;
                result.WeeklyPattern = weeklyPattern;

                // 时间段效率（基于整体正确率）
                double accuracy = AccuracyPercent(totalCorrect, totalWrong);
                result.TimeSlotEfficiency["整体"] = Math.Round(accuracy, 2);

                // 综合评分：正确率 50% + 学习量 30% + 连续性 20%
                double itemsScore = Math.Min(100, totalItems * 2);
                double consistencyScore = Math.Min(100, weeklyPattern.Count * 14.3);
                result.OverallScore = Math.Round(accuracy * 0.5 + itemsScore * 0.3 + consistencyScore * 0.2, 2);

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取多维交叉分析失败: {UserId}", userId);
                return result;
            }
        }

        /// <summary>
        /// 评估学习效率（P-007）
        /// 综合正确率、学习速度、记忆保留率等指标
        /// </summary>
        public LearningEfficiencyReport GetLearningEfficiency(string userId)
        {
            var report = new LearningEfficiencyReport();

            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return report;

                var startDate = DateTime.Today.AddDays(-30);

                // 正确率评分（0-100）
                double accuracy = GetAccuracyRate(userId, startDate);
                report.AccuracyScore = Math.Round(accuracy, 2);

                // 记忆保留率评分（CalculateRetentionRate 返回 0-1，转换为 0-100）
                double retention = CalculateRetentionRate(userId);
                report.RetentionScore = Math.Round(retention * 100, 2);

                // 学习速度评分：基于近30天学习项目数和时长
                int learnedItems = GetTotalLearnedItems(userId, startDate);
                int studyMinutes = GetTotalStudyMinutes(userId, startDate);
                double speed = studyMinutes > 0 ? Math.Min(100, (double)learnedItems / studyMinutes * 60) : 0;
                report.SpeedScore = Math.Round(speed, 2);

                // 一致性评分：基于连续学习天数
                int streak = GetStudyStreak(userId);
                report.ConsistencyScore = Math.Round(Math.Min(100, streak * 5.0), 2);

                // 综合效率：正确率 35% + 保留率 25% + 速度 20% + 一致性 20%
                report.OverallEfficiency = Math.Round(
                    report.AccuracyScore * 0.35 + report.RetentionScore * 0.25 +
                    report.SpeedScore * 0.2 + report.ConsistencyScore * 0.2, 2);

                report.Summary = GenerateEfficiencySummary(report);

                return report;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "评估学习效率失败: {UserId}", userId);
                report.Summary = "暂时无法评估学习效率";
                return report;
            }
        }

        private string GenerateEfficiencySummary(LearningEfficiencyReport report)
        {
            if (report.OverallEfficiency >= 80)
                return "学习效率优秀，继续保持当前学习节奏";
            if (report.OverallEfficiency >= 60)
                return "学习效率良好，可在薄弱环节上进一步加强";
            if (report.OverallEfficiency >= 40)
                return "学习效率一般，建议提升正确率和学习连续性";
            return "学习效率较低，建议制定规律的学习计划并坚持执行";
        }

        /// <summary>
        /// 生成学习建议（P-007）
        /// 基于分析数据自动生成个性化建议
        /// </summary>
        public List<LearningSuggestion> GenerateSuggestions(string userId)
        {
            var suggestions = new List<LearningSuggestion>();

            try
            {
                // 基于效率报告生成建议
                var efficiency = GetLearningEfficiency(userId);

                if (efficiency.AccuracyScore < 60)
                {
                    suggestions.Add(new LearningSuggestion
                    {
                        Category = "正确率",
                        Title = "提升答题正确率",
                        Description = $"当前正确率为 {efficiency.AccuracyScore}%，低于60%",
                        Priority = 1,
                        Action = "复习错题并重新学习薄弱知识点"
                    });
                }

                if (efficiency.RetentionScore < 50)
                {
                    suggestions.Add(new LearningSuggestion
                    {
                        Category = "记忆保留",
                        Title = "加强记忆巩固",
                        Description = $"当前记忆保留率为 {efficiency.RetentionScore}%，需要加强复习",
                        Priority = 2,
                        Action = "增加间隔重复复习频率"
                    });
                }

                if (efficiency.ConsistencyScore < 50)
                {
                    suggestions.Add(new LearningSuggestion
                    {
                        Category = "学习连续性",
                        Title = "保持每日学习习惯",
                        Description = "学习连续性不足，建议每日坚持学习",
                        Priority = 3,
                        Action = "制定每日学习计划并设置提醒"
                    });
                }

                if (efficiency.SpeedScore < 30)
                {
                    suggestions.Add(new LearningSuggestion
                    {
                        Category = "学习速度",
                        Title = "提升学习效率",
                        Description = "学习速度偏低，建议专注学习减少分心",
                        Priority = 4,
                        Action = "使用番茄工作法提升专注度"
                    });
                }

                // 如果没有具体建议，给出鼓励性建议
                if (suggestions.Count == 0)
                {
                    suggestions.Add(new LearningSuggestion
                    {
                        Category = "综合",
                        Title = "继续保持良好学习状态",
                        Description = "各项指标表现良好",
                        Priority = 5,
                        Action = "继续按当前节奏学习并适当挑战更高难度"
                    });
                }

                return suggestions.OrderBy(s => s.Priority).ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成学习建议失败: {UserId}", userId);
                return suggestions;
            }
        }

        #endregion
    }
}
