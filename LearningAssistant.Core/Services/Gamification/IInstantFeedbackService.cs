namespace LearningAssistant.Services.Gamification
{
    /// <summary>
    /// 即时反馈类型 - 学习过程中触发的激励类别
    /// </summary>
    public enum FeedbackType
    {
        /// <summary>
        /// 连续答对
        /// </summary>
        Streak,

        /// <summary>
        /// 里程碑达成
        /// </summary>
        Milestone,

        /// <summary>
        /// 掌握达标
        /// </summary>
        Mastery,

        /// <summary>
        /// 鼓励
        /// </summary>
        Encouragement,

        /// <summary>
        /// 升级
        /// </summary>
        LevelUp
    }

    /// <summary>
    /// 即时反馈 - 一次激励事件的完整描述
    /// </summary>
    public class InstantFeedback
    {
        /// <summary>
        /// 反馈类型
        /// </summary>
        public FeedbackType Type { get; set; }

        /// <summary>
        /// 反馈标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 反馈消息内容
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 图标名称（可空）
        /// </summary>
        public string? IconName { get; set; }

        /// <summary>
        /// 经验值奖励（默认 0）
        /// </summary>
        public int XPReward { get; set; } = 0;

        /// <summary>
        /// 触发时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 即时反馈服务接口 - 强化学习过程中的即时激励
    /// </summary>
    public interface IInstantFeedbackService
    {
        /// <summary>
        /// 触发即时反馈
        /// </summary>
        /// <param name="type">反馈类型</param>
        /// <param name="context">触发上下文描述</param>
        /// <param name="value">关联数值（如连续次数、达成值等，默认 0）</param>
        void TriggerFeedback(FeedbackType type, string context, int value = 0);

        /// <summary>
        /// 获取最近的反馈记录
        /// </summary>
        /// <param name="count">返回条数（默认 10）param>
        List<InstantFeedback> GetRecentFeedbacks(int count = 10);

        /// <summary>
        /// 获取当前连续答对次数
        /// </summary>
        int GetCurrentStreak();

        /// <summary>
        /// 记录答对 - 更新连续计数
        /// </summary>
        void RecordCorrectAnswer();

        /// <summary>
        /// 记录答错 - 重置连续计数
        /// </summary>
        void RecordWrongAnswer();

        /// <summary>
        /// 清除连续计数
        /// </summary>
        void ClearStreak();

        /// <summary>
        /// 事件：反馈被触发
        /// </summary>
        event EventHandler<InstantFeedback>? FeedbackTriggered;
    }
}
