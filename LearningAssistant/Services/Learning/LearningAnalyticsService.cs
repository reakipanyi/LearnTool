using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using Microsoft.Extensions.Logging;
using LearningAssistant.Common;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习数据分析服务实现
    /// </summary>
    public class LearningAnalyticsService : ILearningAnalyticsService
    {
        private readonly ConcurrentDictionary<string, UserAnalyticsData> _userAnalytics = new ConcurrentDictionary<string, UserAnalyticsData>();
        private readonly object _dailyRecordsLock = new object();
        private readonly string _analyticsFilePath;
        private readonly ILogger<LearningAnalyticsService>? _logger;

        public LearningAnalyticsService(ILogger<LearningAnalyticsService>? logger = null)
        {
            _logger = logger;
            _analyticsFilePath = AppPaths.AnalyticsPath;
            LoadAnalytics();
        }

        public void RecordActivity(string userId, string activityType, string subCategory, int count = 1)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger?.LogWarning("尝试记录活动但用户ID为空");
                return;
            }

            // 使用 GetOrAdd 线程安全地获取或创建用户数据
            var userData = _userAnalytics.GetOrAdd(userId, _ => new UserAnalyticsData { UserId = userId });

            var today = DateTime.Today;

            // 更新每日记录（需要加锁保护内部字典）
            lock (_dailyRecordsLock)
            {
                if (!userData.DailyRecords.ContainsKey(today))
                {
                    userData.DailyRecords[today] = new DailyRecord();
                }

                var dailyRecord = userData.DailyRecords[today];
                
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
                }

                // 更新分类统计
                if (!userData.CategoryStats.ContainsKey(subCategory))
                {
                    userData.CategoryStats[subCategory] = 0;
                }
                userData.CategoryStats[subCategory] += count;

                // 更新最后学习日期
                userData.LastLearningDate = today;
            }

            SaveAnalytics();
            _logger?.LogDebug("记录活动: {UserId}, 类型: {ActivityType}, 分类: {Category}, 数量: {Count}", userId, activityType, subCategory, count);
        }

        public Dictionary<string, int> GetCategoryStats(string userId)
        {
            if (_userAnalytics.TryGetValue(userId, out var userData))
            {
                return new Dictionary<string, int>(userData.CategoryStats);
            }
            return new Dictionary<string, int>();
        }

        public int GetStudyStreak(string userId)
        {
            if (!_userAnalytics.TryGetValue(userId, out var userData))
                return 0;

            int streak = 0;
            var checkDate = DateTime.Today;

            while (true)
            {
                lock (_dailyRecordsLock)
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
            }

            return streak;
        }

        public int GetTotalStudyMinutes(string userId, DateTime startDate)
        {
            if (!_userAnalytics.TryGetValue(userId, out var userData))
                return 0;

            int total = 0;
            lock (_dailyRecordsLock)
            {
                for (var date = startDate.Date; date <= DateTime.Today; date = date.AddDays(1))
                {
                    if (userData.DailyRecords.TryGetValue(date, out var record))
                    {
                        total += record.TimeSpentMinutes;
                    }
                }
            }
            return total;
        }

        public int GetTotalLearnedItems(string userId, DateTime startDate)
        {
            if (!_userAnalytics.TryGetValue(userId, out var userData))
                return 0;

            int total = 0;
            lock (_dailyRecordsLock)
            {
                for (var date = startDate.Date; date <= DateTime.Today; date = date.AddDays(1))
                {
                    if (userData.DailyRecords.TryGetValue(date, out var record))
                    {
                        total += record.ItemsLearned;
                    }
                }
            }
            return total;
        }

        public int GetConsecutiveDays(string userId)
        {
            return GetStudyStreak(userId);
        }

        public double GetAccuracyRate(string userId, DateTime startDate)
        {
            if (!_userAnalytics.TryGetValue(userId, out var userData))
                return 0;

            int correctCount = 0;
            int totalCount = 0;

            lock (_dailyRecordsLock)
            {
                for (var date = startDate.Date; date <= DateTime.Today; date = date.AddDays(1))
                {
                    if (userData.DailyRecords.TryGetValue(date, out var record))
                    {
                        correctCount += record.CorrectCount;
                        totalCount += record.CorrectCount + record.WrongCount;
                    }
                }
            }

            return totalCount > 0 ? (double)correctCount / totalCount * 100 : 0;
        }

        public void SaveAnalytics()
        {
            try
            {
                JsonHelper.SaveToFile(_analyticsFilePath, _userAnalytics);
                _logger?.LogDebug("保存分析数据成功，用户数: {Count}", _userAnalytics.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存分析数据失败: {Path}", _analyticsFilePath);
            }
        }

        public void LoadAnalytics()
        {
            try
            {
                if (File.Exists(_analyticsFilePath))
                {
                    var loaded = JsonHelper.LoadFromFile<Dictionary<string, UserAnalyticsData>>(_analyticsFilePath);
                    if (loaded != null)
                    {
                        foreach (var kvp in loaded)
                        {
                            _userAnalytics.TryAdd(kvp.Key, kvp.Value);
                        }
                        _logger?.LogInformation("加载分析数据成功，用户数: {Count}", _userAnalytics.Count);
                    }
                }
                else
                {
                    _logger?.LogDebug("分析数据文件不存在，将创建新数据");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载分析数据失败: {Path}", _analyticsFilePath);
                // 保持 ConcurrentDictionary 为空
            }
        }
        
        #region 为报告服务添加的方法
        
        public DailyStatistics GetDailyStatistics(string userId, DateTime date)
        {
            var stats = new DailyStatistics { Date = date, UserId = userId };

            if (_userAnalytics.TryGetValue(userId, out var userData))
            {
                lock (_dailyRecordsLock)
                {
                    if (userData.DailyRecords.TryGetValue(date, out var record))
                    {
                        stats.TotalItems = record.ItemsLearned;
                        stats.TotalMinutes = record.TimeSpentMinutes;
                        stats.CorrectRate = record.CorrectCount + record.WrongCount > 0 
                            ? (double)record.CorrectCount / (record.CorrectCount + record.WrongCount) 
                            : 0;
                    }
                }
                stats.CategoryBreakdown = new Dictionary<string, int>(userData.CategoryStats);
            }

            return stats;
        }

        public WeeklyStatistics GetWeeklyStatistics(string userId, int year, int weekNumber)
        {
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
                lock (_dailyRecordsLock)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        var date = startDate.AddDays(i);
                        if (userData.DailyRecords.TryGetValue(date, out var record))
                        {
                            correctCount += record.CorrectCount;
                            totalCount += record.CorrectCount + record.WrongCount;
                        }
                    }
                }
                weeklyStats.CorrectRate = totalCount > 0 ? (double)correctCount / totalCount : 0;
                weeklyStats.CategoryBreakdown = new Dictionary<string, int>(userData.CategoryStats);
            }

            return weeklyStats;
        }

        public MonthlyStatistics GetMonthlyStatistics(string userId, int year, int month)
        {
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
                lock (_dailyRecordsLock)
                {
                    for (int i = 0; i < daysInMonth; i++)
                    {
                        var date = startDate.AddDays(i);
                        if (userData.DailyRecords.TryGetValue(date, out var record))
                        {
                            correctCount += record.CorrectCount;
                            totalCount += record.CorrectCount + record.WrongCount;
                        }
                    }
                }
                monthlyStats.CorrectRate = totalCount > 0 ? (double)correctCount / totalCount : 0;
                monthlyStats.CategoryBreakdown = new Dictionary<string, int>(userData.CategoryStats);
            }

            return monthlyStats;
        }

        public List<DailyStatistics> GetLearningTrend(string userId, DateTime startDate, DateTime endDate)
        {
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
    }

    /// <summary>
    /// 用户分析数据
    /// </summary>
    public class UserAnalyticsData
    {
        public string UserId { get; set; } = string.Empty;
        public DateTime LastLearningDate { get; set; }
        public Dictionary<DateTime, DailyRecord> DailyRecords { get; set; } = new Dictionary<DateTime, DailyRecord>();
        public Dictionary<string, int> CategoryStats { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>
    /// 每日记录
    /// </summary>
    public class DailyRecord
    {
        public int ItemsLearned { get; set; }
        public int ItemsReviewed { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public int TimeSpentMinutes { get; set; }
    }
}
