using LearningAssistant.Models.Learning;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 统一统计聚合服务（统计底座模块 A2+A7+A8）
    /// 单一入口聚合多源数据，输出统一 DTO；内置分段缓存 + 失效，以及异步批量写路径。
    /// </summary>
    public interface ILearningStatsAggregator : IDisposable
    {
        /// <summary>
        /// 记录一次学习活动（计数 + 可选时长）。高频调用，内部入队批量落库，不阻塞调用方。
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="activityType">活动类型：Learn / Review / Correct / Wrong 等</param>
        /// <param name="subCategory">子类别</param>
        /// <param name="count">数量（默认1）</param>
        /// <param name="timeSpentMinutes">本次学习时长（分钟，默认0）</param>
        /// <param name="idempotencyKey">幂等键：同一键只累计一次，用于防止重复计数</param>
        void RecordActivity(string userId, string activityType, string subCategory, int count = 1,
            int timeSpentMinutes = 0, string? idempotencyKey = null);

        /// <summary>
        /// 记录学习时长（会话/番茄钟等完成时按真实耗时写入）。
        /// </summary>
        void RecordStudyTime(string userId, int minutes, string subCategory, string? idempotencyKey = null);

        /// <summary>
        /// 立即冲刷写队列（批量落库），用于进程退出或强制落盘。
        /// </summary>
        void Flush();

        // ============ A2 统一聚合 DTO（读路径，带分段缓存） ============

        DailyOverview GetDailyOverview(string userId, DateTime date);

        WeeklyOverview GetWeeklyOverview(string userId, DateTime date);

        MonthlyOverview GetMonthlyOverview(string userId, DateTime date);

        TrendSeries GetTrend(string userId, DateTime start, DateTime end, TrendSeriesType type = TrendSeriesType.Trend);

        List<CategoryBreakdown> GetCategoryBreakdown(string userId, DateTime start, DateTime end);

        MemoryInsights GetMemoryInsights(string userId);

        WrongAnswerSummary GetWrongAnswerSummary(string userId);

        EfficiencyReport GetEfficiencyReport(string userId);

        // ============ A7 分段缓存失效 ============

        /// <summary>
        /// 使某个用户某一天的统计分段缓存失效（仅清受影响的分段，非全量）。
        /// </summary>
        void Invalidate(string userId, DateTime date);

        /// <summary>
        /// 使某个用户的全部统计缓存失效。
        /// </summary>
        void InvalidateAll(string userId);

        /// <summary>
        /// 强制刷新：绕过缓存重新计算（供 UI 手动刷新兜底）。
        /// </summary>
        void ForceRefresh(string userId);
    }
}