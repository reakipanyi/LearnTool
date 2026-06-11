namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习数据分析服务接口 - 提供学习数据的统计分析功能
    /// 支持日/周/月统计、趋势分析、连续学习天数等
    /// </summary>
    public interface ILearningAnalyticsService
    {
        /// <summary>
        /// 记录学习活动
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="activityType">活动类型（如"word_learned", "quiz_completed"）</param>
        /// <param name="subCategory">子类别</param>
        /// <param name="count">数量，默认为1</param>
        void RecordActivity(string userId, string activityType, string subCategory, int count = 1);

        /// <summary>
        /// 获取每日学习统计
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="date">日期</param>
        /// <returns>每日学习统计数据</returns>
        DailyLearningStats GetDailyStats(string userId, DateTime date);

        /// <summary>
        /// 获取每日学习统计（为报告服务）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="date">日期</param>
        /// <returns>每日统计数据</returns>
        DailyStatistics GetDailyStatistics(string userId, DateTime date);

        /// <summary>
        /// 获取每周学习统计
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="weekStart">周起始日期</param>
        /// <returns>每周学习统计数据</returns>
        WeeklyLearningStats GetWeeklyStats(string userId, DateTime weekStart);

        /// <summary>
        /// 获取每周学习统计（为报告服务）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="year">年份</param>
        /// <param name="weekNumber">周数（1-52）</param>
        /// <returns>每周统计数据</returns>
        WeeklyStatistics GetWeeklyStatistics(string userId, int year, int weekNumber);

        /// <summary>
        /// 获取月度学习统计（为报告服务）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="year">年份</param>
        /// <param name="month">月份（1-12）</param>
        /// <returns>月度统计数据</returns>
        MonthlyStatistics GetMonthlyStatistics(string userId, int year, int month);

        /// <summary>
        /// 获取学习进度趋势（过去N天）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="days">天数，默认为7</param>
        /// <returns>每日进度点列表</returns>
        List<DailyProgressPoint> GetProgressTrend(string userId, int days = 7);

        /// <summary>
        /// 获取学习趋势（为报告服务）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>每日统计数据列表</returns>
        List<DailyStatistics> GetLearningTrend(string userId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取各分类学习统计
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>分类名称到学习数量的字典</returns>
        Dictionary<string, int> GetCategoryStats(string userId);

        /// <summary>
        /// 获取学习连续天数（streak）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>连续学习天数</returns>
        int GetLearningStreak(string userId);

        /// <summary>
        /// 获取学习连续天数（为报告服务）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>连续学习天数</returns>
        int GetStudyStreak(string userId);

        /// <summary>
        /// 保存分析数据到持久化存储
        /// </summary>
        void SaveAnalytics();

        /// <summary>
        /// 从持久化存储加载分析数据
        /// </summary>
        void LoadAnalytics();
    }

    /// <summary>
    /// 每日学习统计 - 包含单日学习的关键指标
    /// </summary>
    public class DailyLearningStats
    {
        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 学习了多少项
        /// </summary>
        public int ItemsLearned { get; set; }

        /// <summary>
        /// 总学习时长（分钟）
        /// </summary>
        public int TotalTimeMinutes { get; set; }

        /// <summary>
        /// 正确率（百分比）
        /// </summary>
        public double AccuracyRate { get; set; }
    }

    /// <summary>
    /// 每周学习统计 - 包含一周的学习汇总数据
    /// </summary>
    public class WeeklyLearningStats
    {
        /// <summary>
        /// 周起始日期
        /// </summary>
        public DateTime WeekStart { get; set; }

        /// <summary>
        /// 本周总学习项数
        /// </summary>
        public int TotalItemsLearned { get; set; }

        /// <summary>
        /// 本周总学习时长（分钟）
        /// </summary>
        public int TotalTimeMinutes { get; set; }

        /// <summary>
        /// 本周平均正确率
        /// </summary>
        public double AverageAccuracyRate { get; set; }

        /// <summary>
        /// 每日统计数据列表
        /// </summary>
        public List<DailyLearningStats> DailyStats { get; set; } = new List<DailyLearningStats>();
    }

    /// <summary>
    /// 每日进度点 - 用于绘制学习趋势图
    /// </summary>
    public class DailyProgressPoint
    {
        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 学习了多少项
        /// </summary>
        public int ItemsLearned { get; set; }

        /// <summary>
        /// 学习时长（分钟）
        /// </summary>
        public int TimeMinutes { get; set; }
    }

    /// <summary>
    /// 每日学习统计（为报告服务）
    /// </summary>
    public class DailyStatistics
    {
        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 总时长（分钟）
        /// </summary>
        public int TotalMinutes { get; set; }

        /// <summary>
        /// 总项数
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// 正确率
        /// </summary>
        public double CorrectRate { get; set; }

        /// <summary>
        /// 分类明细
        /// </summary>
        public Dictionary<string, int> CategoryBreakdown { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 年份
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// 周数
        /// </summary>
        public int WeekNumber { get; set; }
    }

    /// <summary>
    /// 每周学习统计（为报告服务）
    /// </summary>
    public class WeeklyStatistics
    {
        /// <summary>
        /// 年份
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// 周数
        /// </summary>
        public int WeekNumber { get; set; }

        /// <summary>
        /// 总时长（分钟）
        /// </summary>
        public int TotalMinutes { get; set; }

        /// <summary>
        /// 总项数
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// 正确率
        /// </summary>
        public double CorrectRate { get; set; }

        /// <summary>
        /// 分类明细
        /// </summary>
        public Dictionary<string, int> CategoryBreakdown { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;
    }

    /// <summary>
    /// 月度学习统计（为报告服务）
    /// </summary>
    public class MonthlyStatistics
    {
        /// <summary>
        /// 年份
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// 月份
        /// </summary>
        public int Month { get; set; }

        /// <summary>
        /// 总时长（分钟）
        /// </summary>
        public int TotalMinutes { get; set; }

        /// <summary>
        /// 总项数
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// 正确率
        /// </summary>
        public double CorrectRate { get; set; }

        /// <summary>
        /// 分类明细
        /// </summary>
        public Dictionary<string, int> CategoryBreakdown { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;
    }
}
