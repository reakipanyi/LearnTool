namespace UnifiedLearningAssistant.Services.Learning
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
        /// 获取每周学习统计
        /// </summary>
        WeeklyLearningStats GetWeeklyStats(string userId, DateTime weekStart);
        
        /// <summary>
        /// 获取学习进度趋势（过去7天）
        /// </summary>
        List<DailyProgressPoint> GetProgressTrend(string userId, int days = 7);
        
        /// <summary>
        /// 获取各分类学习统计
        /// </summary>
        Dictionary<string, int> GetCategoryStats(string userId);
        
        /// <summary>
        /// 获取学习 streak（连续学习天数）
        /// </summary>
        int GetLearningStreak(string userId);
        
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
}
