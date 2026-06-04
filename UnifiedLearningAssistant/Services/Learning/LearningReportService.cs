using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace UnifiedLearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习报告服务
    /// 生成每日/每周学习报告
    /// </summary>
    public class LearningReportService
    {
        private readonly ILearningAnalyticsService _analyticsService;
        private readonly ILogger<LearningReportService>? _logger;

        public LearningReportService(ILearningAnalyticsService analyticsService, ILogger<LearningReportService>? logger = null)
        {
            _analyticsService = analyticsService;
            _logger = logger;
        }

        /// <summary>
        /// 生成今日学习报告
        /// </summary>
        public DailyReport GenerateDailyReport(string userId, DateTime date)
        {
            try
            {
                var stats = _analyticsService.GetDailyStatistics(userId, date);
                var trends = _analyticsService.GetLearningTrend(userId, date.AddDays(-6), date);

                return new DailyReport
                {
                    Date = date,
                    UserId = userId,
                    TotalStudyMinutes = stats.TotalMinutes,
                    ItemsStudied = stats.TotalItems,
                    CorrectRate = stats.CorrectRate,
                    Categories = stats.CategoryBreakdown.ToDictionary(kv => kv.Key, kv => kv.Value),
                    Trend = trends.Select(t => t.TotalMinutes).ToList(),
                    Streak = _analyticsService.GetStudyStreak(userId),
                    Suggestions = GenerateDailySuggestions(stats)
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成每日报告失败");
                return new DailyReport { Date = date, UserId = userId };
            }
        }

        /// <summary>
        /// 生成本周学习报告
        /// </summary>
        public WeeklyReport GenerateWeeklyReport(string userId, int year, int weekNumber)
        {
            try
            {
                var startDate = GetStartOfWeek(year, weekNumber);
                var endDate = startDate.AddDays(6);

                var dailyStats = new List<DailyStatistics>();
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    dailyStats.Add(_analyticsService.GetDailyStatistics(userId, date));
                }

                var weeklyStats = _analyticsService.GetWeeklyStatistics(userId, year, weekNumber);

                return new WeeklyReport
                {
                    Year = year,
                    WeekNumber = weekNumber,
                    StartDate = startDate,
                    EndDate = endDate,
                    UserId = userId,
                    TotalStudyMinutes = weeklyStats.TotalMinutes,
                    AverageDailyMinutes = dailyStats.Any() ? (int)dailyStats.Average(s => s.TotalMinutes) : 0,
                    ItemsStudied = weeklyStats.TotalItems,
                    CorrectRate = weeklyStats.CorrectRate,
                    DailyBreakdown = dailyStats.ToDictionary(d => d.Date.DayOfWeek.ToString(), d => d.TotalMinutes),
                    Categories = weeklyStats.CategoryBreakdown.ToDictionary(kv => kv.Key, kv => kv.Value),
                    Streak = _analyticsService.GetStudyStreak(userId),
                    Improvement = CalculateWeeklyImprovement(userId, year, weekNumber),
                    Grade = CalculateWeeklyGrade(weeklyStats),
                    Suggestions = GenerateWeeklySuggestions(weeklyStats, dailyStats)
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成周报告失败");
                return new WeeklyReport { Year = year, WeekNumber = weekNumber, UserId = userId };
            }
        }

        /// <summary>
        /// 生成月度学习报告
        /// </summary>
        public MonthlyReport GenerateMonthlyReport(string userId, int year, int month)
        {
            try
            {
                var firstDay = new DateTime(year, month, 1);
                var lastDay = firstDay.AddMonths(1).AddDays(-1);

                var weeklyStats = new List<WeeklyStatistics>();
                var currentDate = firstDay;
                while (currentDate <= lastDay)
                {
                    var weekNumber = ISOWeek.GetWeekOfYear(currentDate);
                    weeklyStats.Add(_analyticsService.GetWeeklyStatistics(userId, year, weekNumber));
                    currentDate = currentDate.AddDays(7);
                }

                var monthlyStats = _analyticsService.GetMonthlyStatistics(userId, year, month);

                return new MonthlyReport
                {
                    Year = year,
                    Month = month,
                    UserId = userId,
                    TotalStudyMinutes = monthlyStats.TotalMinutes,
                    ItemsStudied = monthlyStats.TotalItems,
                    CorrectRate = monthlyStats.CorrectRate,
                    WeeklyBreakdown = weeklyStats.ToDictionary(w => w.WeekNumber, w => w.TotalMinutes),
                    Categories = monthlyStats.CategoryBreakdown.ToDictionary(kv => kv.Key, kv => kv.Value),
                    Streak = _analyticsService.GetStudyStreak(userId),
                    AchievementRate = CalculateAchievementRate(userId, year, month),
                    Suggestions = GenerateMonthlySuggestions(monthlyStats)
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成月报告失败");
                return new MonthlyReport { Year = year, Month = month, UserId = userId };
            }
        }

        /// <summary>
        /// 生成学习报告的文本内容
        /// </summary>
        public string GenerateReportText(ReportBase report)
        {
            var sb = new StringBuilder();

            if (report is DailyReport daily)
            {
                sb.AppendLine($"📅 {daily.Date:yyyy年MM月dd日} 学习报告");
                sb.AppendLine("────────────────────────");
                sb.AppendLine($"⏱️ 今日学习时长: {daily.TotalStudyMinutes} 分钟");
                sb.AppendLine($"📚 学习项目数: {daily.ItemsStudied} 个");
                sb.AppendLine($"✅ 正确率: {daily.CorrectRate:P2}");
                sb.AppendLine($"🔥 连续学习天数: {daily.Streak} 天");
                
                if (daily.Categories.Any())
                {
                    sb.AppendLine("\n📊 分类学习情况:");
                    foreach (var category in daily.Categories)
                    {
                        sb.AppendLine($"  • {category.Key}: {category.Value} 分钟");
                    }
                }

                if (daily.Suggestions.Any())
                {
                    sb.AppendLine("\n💡 今日建议:");
                    foreach (var suggestion in daily.Suggestions)
                    {
                        sb.AppendLine($"  • {suggestion}");
                    }
                }
            }
            else if (report is WeeklyReport weekly)
            {
                sb.AppendLine($"📅 {weekly.Year}年第{weekly.WeekNumber}周学习报告");
                sb.AppendLine($"日期范围: {weekly.StartDate:MM/dd} ~ {weekly.EndDate:MM/dd}");
                sb.AppendLine("────────────────────────");
                sb.AppendLine($"⏱️ 本周学习时长: {weekly.TotalStudyMinutes} 分钟");
                sb.AppendLine($"📊 日均学习时长: {weekly.AverageDailyMinutes} 分钟");
                sb.AppendLine($"📚 学习项目数: {weekly.ItemsStudied} 个");
                sb.AppendLine($"✅ 正确率: {weekly.CorrectRate:P2}");
                sb.AppendLine($"🔥 连续学习天数: {weekly.Streak} 天");
                sb.AppendLine($"📈 进步幅度: {weekly.Improvement:P2}");
                sb.AppendLine($"🎯 本周评分: {weekly.Grade}");

                if (weekly.DailyBreakdown.Any())
                {
                    sb.AppendLine("\n📅 每日学习时长:");
                    foreach (var day in weekly.DailyBreakdown)
                    {
                        sb.AppendLine($"  • {day.Key}: {day.Value} 分钟");
                    }
                }

                if (weekly.Suggestions.Any())
                {
                    sb.AppendLine("\n💡 下周建议:");
                    foreach (var suggestion in weekly.Suggestions)
                    {
                        sb.AppendLine($"  • {suggestion}");
                    }
                }
            }

            return sb.ToString();
        }

        #region 辅助方法

        private DateTime GetStartOfWeek(int year, int weekNumber)
        {
            var jan1 = new DateTime(year, 1, 1);
            var daysOffset = (int)jan1.DayOfWeek - (int)DayOfWeek.Monday;
            if (daysOffset > 0) daysOffset -= 7;
            
            var firstMonday = jan1.AddDays(-daysOffset);
            return firstMonday.AddDays((weekNumber - 1) * 7);
        }

        private List<string> GenerateDailySuggestions(DailyStatistics stats)
        {
            var suggestions = new List<string>();

            if (stats.TotalMinutes < 30)
            {
                suggestions.Add("今天学习时间较短，建议每天至少学习30分钟以保持学习效果。");
            }
            else if (stats.TotalMinutes >= 120)
            {
                suggestions.Add("今天学习非常认真！注意适当休息，保持身心健康。");
            }

            if (stats.CorrectRate < 0.7)
            {
                suggestions.Add("正确率较低，建议复习之前学习的内容，巩固基础。");
            }
            else if (stats.CorrectRate >= 0.9)
            {
                suggestions.Add("正确率很高！可以尝试更有挑战性的学习内容。");
            }

            return suggestions;
        }

        private List<string> GenerateWeeklySuggestions(WeeklyStatistics stats, List<DailyStatistics> dailyStats)
        {
            var suggestions = new List<string>();

            var activeDays = dailyStats.Count(d => d.TotalMinutes > 0);
            if (activeDays < 5)
            {
                suggestions.Add($"本周仅学习了{activeDays}天，建议保持每周至少学习5天。");
            }

            var improvement = CalculateWeeklyImprovement(stats.UserId, stats.Year, stats.WeekNumber);
            if (improvement < -0.2)
            {
                suggestions.Add("本周学习时间有所下降，分析原因并制定改进计划。");
            }
            else if (improvement > 0.2)
            {
                suggestions.Add("本周学习时间有明显提升，继续保持！");
            }

            return suggestions;
        }

        private List<string> GenerateMonthlySuggestions(MonthlyStatistics stats)
        {
            var suggestions = new List<string>();

            if (stats.TotalMinutes < 600) // 少于10小时
            {
                suggestions.Add("本月学习时间较少，设定学习目标并坚持执行。");
            }
            else if (stats.TotalMinutes >= 1500) // 超过25小时
            {
                suggestions.Add("本月学习非常努力！注意劳逸结合。");
            }

            return suggestions;
        }

        private double CalculateWeeklyImprovement(string userId, int year, int weekNumber)
        {
            try
            {
                var currentWeek = _analyticsService.GetWeeklyStatistics(userId, year, weekNumber);
                
                int prevWeek = weekNumber > 1 ? weekNumber - 1 : GetLastWeekOfYear(year - 1);
                var previousWeek = _analyticsService.GetWeeklyStatistics(userId, 
                    weekNumber > 1 ? year : year - 1, prevWeek);

                if (previousWeek.TotalMinutes == 0) return 0;
                
                return (currentWeek.TotalMinutes - previousWeek.TotalMinutes) / (double)previousWeek.TotalMinutes;
            }
            catch
            {
                return 0;
            }
        }

        private int GetLastWeekOfYear(int year)
        {
            var dec31 = new DateTime(year, 12, 31);
            return ISOWeek.GetWeekOfYear(dec31);
        }

        private string CalculateWeeklyGrade(WeeklyStatistics stats)
        {
            var score = 0;
            
            if (stats.TotalMinutes >= 350) score += 30; // 每周至少350分钟
            else if (stats.TotalMinutes >= 200) score += 20;
            else if (stats.TotalMinutes >= 100) score += 10;

            if (stats.CorrectRate >= 0.9) score += 25;
            else if (stats.CorrectRate >= 0.8) score += 20;
            else if (stats.CorrectRate >= 0.7) score += 15;

            if (stats.TotalItems >= 100) score += 25;
            else if (stats.TotalItems >= 50) score += 20;
            else if (stats.TotalItems >= 20) score += 10;

            if (score >= 80) return "A";
            if (score >= 70) return "B";
            if (score >= 60) return "C";
            if (score >= 50) return "D";
            return "F";
        }

        private double CalculateAchievementRate(string userId, int year, int month)
        {
            // 简化实现，实际应用中可以从用户目标设置中获取
            var monthlyGoalMinutes = 1200; // 每月目标20小时
            var stats = _analyticsService.GetMonthlyStatistics(userId, year, month);
            return Math.Min(stats.TotalMinutes / (double)monthlyGoalMinutes, 1.0);
        }

        #endregion
    }

    #region 数据模型

    public abstract class ReportBase
    {
        public string UserId { get; set; } = string.Empty;
        public double CorrectRate { get; set; }
        public int Streak { get; set; }
        public List<string> Suggestions { get; set; } = new List<string>();
    }

    public class DailyReport : ReportBase
    {
        public DateTime Date { get; set; }
        public int TotalStudyMinutes { get; set; }
        public int ItemsStudied { get; set; }
        public Dictionary<string, int> Categories { get; set; } = new Dictionary<string, int>();
        public List<int> Trend { get; set; } = new List<int>();
    }

    public class WeeklyReport : ReportBase
    {
        public int Year { get; set; }
        public int WeekNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalStudyMinutes { get; set; }
        public int AverageDailyMinutes { get; set; }
        public int ItemsStudied { get; set; }
        public Dictionary<string, int> DailyBreakdown { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> Categories { get; set; } = new Dictionary<string, int>();
        public double Improvement { get; set; }
        public string Grade { get; set; } = string.Empty;
    }

    public class MonthlyReport : ReportBase
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalStudyMinutes { get; set; }
        public int ItemsStudied { get; set; }
        public Dictionary<int, int> WeeklyBreakdown { get; set; } = new Dictionary<int, int>();
        public Dictionary<string, int> Categories { get; set; } = new Dictionary<string, int>();
        public double AchievementRate { get; set; }
    }

    #endregion
}
