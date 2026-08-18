namespace LearningAssistant.Models.Learning
{
    /// <summary>
    /// 统一统计聚合 DTO（统计底座模块产出，图表/数据中心/报告/首页消费）
    /// 口径定义（与 docs/优化改进方案/02-数据分析-统计底座模块.md A4 一致）：
    /// - 学习时长（分钟）= 学习会话 + 番茄钟完成时长（是否计入 PDF/游戏由设置开关控制）
    /// - 正确率 = Correct / (Correct + Wrong)（0-100）
    /// - 连击 = 有学习行为的连续天数
    /// - 等级/经验以 GamificationService 为准
    /// - 跨天学习归属：以结束时间所在日计
    /// </summary>
    public static class StatsAggregation
    {
        /// <summary>计算正确率（0-100）；无有效作答返回 0</summary>
        public static double ComputeAccuracy(int correctCount, int wrongCount)
        {
            var total = correctCount + wrongCount;
            return total > 0 ? Math.Round(correctCount * 100.0 / total, 2) : 0;
        }
    }

    /// <summary>
    /// 每日概览 DTO（04 概览 Tab、06 首页消费）
    /// </summary>
    public class DailyOverview
    {
        public DateTime Date { get; set; }
        public int TimeSpentMinutes { get; set; }
        public int ItemsStudied { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public double Accuracy { get; set; }
        public int StreakDays { get; set; }
        public int XP { get; set; }
        public int Level { get; set; }
        public bool GoalCompleted { get; set; }
    }

    /// <summary>
    /// 周期概览基类（周/月共用，含环比增量与强弱分类）
    /// </summary>
    public abstract class PeriodOverview
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TimeSpentMinutes { get; set; }
        public int ItemsStudied { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public double Accuracy { get; set; }
        public int StreakDays { get; set; }
        public int XP { get; set; }
        public int Level { get; set; }
        public bool GoalCompleted { get; set; }

        // 环比增量（相对上一周期）
        public int TimeSpentDeltaMinutes { get; set; }
        public int ItemsStudiedDelta { get; set; }
        public double AccuracyDelta { get; set; }

        public string TopCategory { get; set; } = string.Empty;
        public string WeakCategory { get; set; } = string.Empty;
    }

    /// <summary>
    /// 周概览 DTO（04 概览、05 报告消费）
    /// </summary>
    public class WeeklyOverview : PeriodOverview
    {
        public int Year { get; set; }
        public int WeekNumber { get; set; }
    }

    /// <summary>
    /// 月概览 DTO（04 概览、05 报告消费）
    /// </summary>
    public class MonthlyOverview : PeriodOverview
    {
        public int Year { get; set; }
        public int Month { get; set; }
    }

    /// <summary>
    /// 趋势序列类型
    /// </summary>
    public enum TrendSeriesType
    {
        /// <summary>学习量趋势</summary>
        Trend,
        /// <summary>正确率趋势</summary>
        Accuracy,
        /// <summary>分类趋势</summary>
        Category
    }

    /// <summary>
    /// 趋势序列 DTO（03 趋势图、04/05 消费）
    /// </summary>
    public class TrendSeries
    {
        public TrendSeriesType SeriesType { get; set; }
        public string? SeriesKey { get; set; }
        public List<TrendPoint> Points { get; set; } = new();
    }

    /// <summary>
    /// 趋势点
    /// </summary>
    public class TrendPoint
    {
        public DateTime Date { get; set; }
        public double Value { get; set; }
    }

    /// <summary>
    /// 分类维度汇总 DTO（03 分类图、04/05 消费）
    /// </summary>
    public class CategoryBreakdown
    {
        public string Category { get; set; } = string.Empty;
        public int TimeSpentMinutes { get; set; }
        public int ItemsStudied { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public double Accuracy { get; set; }
    }

    /// <summary>
    /// 记忆成熟度分布（新生/学习中/已掌握）
    /// </summary>
    public class MaturityDistribution
    {
        public int NewCount { get; set; }
        public int LearningCount { get; set; }
        public int MasteredCount { get; set; }
    }

    /// <summary>
    /// 记忆洞察 DTO（03 记忆图、04 记忆 Tab 消费）
    /// </summary>
    public class MemoryInsights
    {
        /// <summary>保留率（0-1）</summary>
        public double RetentionRate { get; set; }
        public Dictionary<int, double> ForgettingCurve { get; set; } = new();
        public MaturityDistribution Maturity { get; set; } = new();
        public Dictionary<int, int> ReviewDistribution { get; set; } = new();
        public int TotalItems { get; set; }
        public int DueToday { get; set; }
    }

    /// <summary>
    /// 错题汇总 DTO（04 错题 Tab 消费）
    /// </summary>
    public class WrongAnswerSummary
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int UnreviewedCount { get; set; }
        /// <summary>难度分布（key 为难度区间，如 "0-2"）</summary>
        public Dictionary<string, int> DifficultyDistribution { get; set; } = new();
        /// <summary>高频错因（key 为分类/学科）</summary>
        public Dictionary<string, int> TopWrongReasons { get; set; } = new();
    }

    /// <summary>
    /// 效率报告 DTO（05 报告消费）
    /// 综合时长/正确率/连击计算效率分
    /// </summary>
    public class EfficiencyReport
    {
        public int TimeSpentMinutes { get; set; }
        public double Accuracy { get; set; }
        public int StreakDays { get; set; }
        /// <summary>综合效率分（0-100）</summary>
        public double EfficiencyScore { get; set; }
        public string Summary { get; set; } = string.Empty;
    }
}
