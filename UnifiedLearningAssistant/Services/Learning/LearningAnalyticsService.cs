using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnifiedLearningAssistant.Common;

namespace UnifiedLearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习数据分析服务实现
    /// </summary>
    public class LearningAnalyticsService : ILearningAnalyticsService
    {
        private Dictionary<string, UserAnalyticsData> _userAnalytics = new Dictionary<string, UserAnalyticsData>();
        private readonly string _analyticsFilePath;

        public LearningAnalyticsService()
        {
            _analyticsFilePath = Path.Combine(FileHelper.GetAppDirectory(), "learning_analytics.json");
            LoadAnalytics();
        }

        public void RecordActivity(string userId, string activityType, string subCategory, int count = 1)
        {
            if (!_userAnalytics.ContainsKey(userId))
            {
                _userAnalytics[userId] = new UserAnalyticsData { UserId = userId };
            }

            var today = DateTime.Today;
            var userData = _userAnalytics[userId];

            // 更新每日记录
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

            SaveAnalytics();
        }

        public DailyLearningStats GetDailyStats(string userId, DateTime date)
        {
            var stats = new DailyLearningStats { Date = date };

            if (_userAnalytics.TryGetValue(userId, out var userData) && 
                userData.DailyRecords.TryGetValue(date, out var record))
            {
                stats.ItemsLearned = record.ItemsLearned;
                stats.TotalTimeMinutes = record.TimeSpentMinutes;
                stats.AccuracyRate = record.CorrectCount + record.WrongCount > 0 
                    ? (double)record.CorrectCount / (record.CorrectCount + record.WrongCount) * 100 
                    : 0;
            }

            return stats;
        }

        public WeeklyLearningStats GetWeeklyStats(string userId, DateTime weekStart)
        {
            var weeklyStats = new WeeklyLearningStats { WeekStart = weekStart };

            if (_userAnalytics.TryGetValue(userId, out var userData))
            {
                for (int i = 0; i < 7; i++)
                {
                    var date = weekStart.AddDays(i);
                    var dailyStats = GetDailyStats(userId, date);
                    weeklyStats.DailyStats.Add(dailyStats);
                    weeklyStats.TotalItemsLearned += dailyStats.ItemsLearned;
                    weeklyStats.TotalTimeMinutes += dailyStats.TotalTimeMinutes;
                }

                weeklyStats.AverageAccuracyRate = weeklyStats.DailyStats.Any() 
                    ? weeklyStats.DailyStats.Average(s => s.AccuracyRate) 
                    : 0;
            }

            return weeklyStats;
        }

        public List<DailyProgressPoint> GetProgressTrend(string userId, int days = 7)
        {
            var trend = new List<DailyProgressPoint>();
            
            if (_userAnalytics.TryGetValue(userId, out var userData))
            {
                for (int i = days - 1; i >= 0; i--)
                {
                    var date = DateTime.Today.AddDays(-i);
                    var record = userData.DailyRecords.TryGetValue(date, out var r) ? r : new DailyRecord();
                    
                    trend.Add(new DailyProgressPoint
                    {
                        Date = date,
                        ItemsLearned = record.ItemsLearned,
                        TimeMinutes = record.TimeSpentMinutes
                    });
                }
            }

            return trend;
        }

        public Dictionary<string, int> GetCategoryStats(string userId)
        {
            if (_userAnalytics.TryGetValue(userId, out var userData))
            {
                return new Dictionary<string, int>(userData.CategoryStats);
            }
            return new Dictionary<string, int>();
        }

        public int GetLearningStreak(string userId)
        {
            if (!_userAnalytics.TryGetValue(userId, out var userData))
                return 0;

            int streak = 0;
            var checkDate = DateTime.Today;

            while (true)
            {
                if (userData.DailyRecords.TryGetValue(checkDate, out var record) && 
                    record.ItemsLearned > 0)
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

        public void SaveAnalytics()
        {
            try
            {
                JsonHelper.SaveToFile(_analyticsFilePath, _userAnalytics);
            }
            catch
            {
                // 静默处理保存错误
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
                        _userAnalytics = loaded;
                    }
                }
            }
            catch
            {
                // 静默处理加载错误
            }
        }
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
