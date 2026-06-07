namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习数据分析服务接口
    /// </summary>
    public interface ILearningAnalyticsService
    {
        /// <summary>
        /// 记录学习活动
        /// </summary>
        void RecordActivity(string userId, string activityType, string subCategory, int count = 1);
        
        /// <summary>
        /// 获取每日学习统计
        /// </summary>
        DailyLearningStats GetDailyStats(string userId, DateTime date);
        
        /// <summary>
        /// 获取每日学习统计（为报告服务）
        /// </summary>
        DailyStatistics GetDailyStatistics(string userId, DateTime date);
        
        /// <summary>
        /// 获取每周学习统计
        /// </summary>
        WeeklyLearningStats GetWeeklyStats(string userId, DateTime weekStart);
        
        /// <summary>
        /// 获取每周学习统计（为报告服务）
        /// </summary>
        WeeklyStatistics GetWeeklyStatistics(string userId, int year, int weekNumber);
        
        /// <summary>
        /// 获取月度学习统计（为报告服务）
        /// </summary>
        MonthlyStatistics GetMonthlyStatistics(string userId, int year, int month);
        
        /// <summary>
        /// 获取学习进度趋势（过去7天）
        /// </summary>
        List<DailyProgressPoint> GetProgressTrend(string userId, int days = 7);
        
        /// <summary>
        /// 获取学习趋势（为报告服务）
        /// </summary>
        List<DailyStatistics> GetLearningTrend(string userId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// 获取各分类学习统计
        /// </summary>
        Dictionary<string, int> GetCategoryStats(string userId);
        
        /// <summary>
        /// 获取学习 streak（连续学习天数）
        /// </summary>
        int GetLearningStreak(string userId);
        
        /// <summary>
        /// 获取学习 streak（为报告服务）
        /// </summary>
        int GetStudyStreak(string userId);
        
        /// <summary>
        /// 保存分析数据
        /// </summary>
        void SaveAnalytics();
        
        /// <summary>
        /// 加载分析数据
        /// </summary>
        void LoadAnalytics();
    }

    /// <summary>
    /// 每日学习统计
    /// </summary>
    public class DailyLearningStats
    {
        public DateTime Date { get; set; }
        public int ItemsLearned { get; set; }
        public int TotalTimeMinutes { get; set; }
        public double AccuracyRate { get; set; }
    }

    /// <summary>
    /// 每周学习统计
    /// </summary>
    public class WeeklyLearningStats
    {
        public DateTime WeekStart { get; set; }
        public int TotalItemsLearned { get; set; }
        public int TotalTimeMinutes { get; set; }
        public double AverageAccuracyRate { get; set; }
        public List<DailyLearningStats> DailyStats { get; set; } = new List<DailyLearningStats>();
    }

    /// <summary>
    /// 每日进度点
    /// </summary>
    public class DailyProgressPoint
    {
        public DateTime Date { get; set; }
        public int ItemsLearned { get; set; }
        public int TimeMinutes { get; set; }
    }
    
    /// <summary>
    /// 每日学习统计（为报告服务）
    /// </summary>
    public class DailyStatistics
    {
        public DateTime Date { get; set; }
        public int TotalMinutes { get; set; }
        public int TotalItems { get; set; }
        public double CorrectRate { get; set; }
        public Dictionary<string, int> CategoryBreakdown { get; set; } = new Dictionary<string, int>();
        public string UserId { get; set; } = string.Empty;
        public int Year { get; set; }
        public int WeekNumber { get; set; }
    }
    
    /// <summary>
    /// 每周学习统计（为报告服务）
    /// </summary>
    public class WeeklyStatistics
    {
        public int Year { get; set; }
        public int WeekNumber { get; set; }
        public int TotalMinutes { get; set; }
        public int TotalItems { get; set; }
        public double CorrectRate { get; set; }
        public Dictionary<string, int> CategoryBreakdown { get; set; } = new Dictionary<string, int>();
        public string UserId { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 月度学习统计（为报告服务）
    /// </summary>
    public class MonthlyStatistics
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalMinutes { get; set; }
        public int TotalItems { get; set; }
        public double CorrectRate { get; set; }
        public Dictionary<string, int> CategoryBreakdown { get; set; } = new Dictionary<string, int>();
        public string UserId { get; set; } = string.Empty;
    }
}
