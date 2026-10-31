namespace LearningAssistant.Models.Home
{
    /// <summary>
    /// 首页一次性聚合快照。
    /// 视图只消费该对象，避免统计接口参数持续膨胀，也便于两套首页共享同一数据口径。
    /// </summary>
    public sealed record HomeSnapshot(
        int TodayStudyMinutes,
        int StreakDays,
        int TotalXp,
        int Level,
        int XpToNextLevel,
        int CompletedChallenges,
        int TotalChallenges,
        int NoteCount,
        int TodayNewNotes,
        int DueReviewCount,
        int GoalCompleted,
        int GoalTarget)
    {
        /// <summary>
        /// 全部为零的安全空快照，用于服务不可用时的兜底。
        /// </summary>
        public static HomeSnapshot Empty { get; } = new(
            0, 0, 0, 1, 0,
            0, 0,
            0, 0,
            0,
            0, 0);

        /// <summary>
        /// 是否为全新用户：没有任何学习时长、笔记、待复习项和累计经验。
        /// 供首页空状态引导使用。
        /// </summary>
        public bool IsBrandNew =>
            TodayStudyMinutes <= 0 && NoteCount <= 0 && DueReviewCount <= 0 && TotalXp <= 0;

        /// <summary>
        /// 今日目标完成百分比（0-100）。
        /// </summary>
        public int GoalPercent => GoalTarget <= 0
            ? 0
            : Math.Min(100, (int)Math.Round(GoalCompleted * 100.0 / GoalTarget));
    }
}
