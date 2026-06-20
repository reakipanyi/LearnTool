
namespace LearningAssistant.Services.Notifications
{
    /// <summary>
    /// 庆祝服务接口 - 提供成就解锁等场景的视觉和音效反馈
    /// </summary>
    public interface ICelebrationService
    {
        /// <summary>
        /// 触发指定类型的庆祝效果
        /// </summary>
        /// <param name="type">庆祝类型</param>
        /// <param name="context">庆祝上下文（包含用户ID、成就ID等信息）</param>
        void TriggerCelebration(CelebrationType type, CelebrationContext context);

        /// <summary>
        /// 显示彩带/礼花动画效果
        /// </summary>
        void ShowConfetti();

        /// <summary>
        /// 播放庆祝音效
        /// </summary>
        void PlayCelebrationSound();
    }

    /// <summary>
    /// 庆祝上下文 - 传递庆祝效果所需的数据
    /// </summary>
    public class CelebrationContext
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 成就ID（成就解锁类型时使用）
        /// </summary>
        public string? AchievementId { get; set; }

        /// <summary>
        /// 分数（完美得分类型时使用）
        /// </summary>
        public int? Score { get; set; }

        /// <summary>
        /// 自定义消息
        /// </summary>
        public string? Message { get; set; }
    }

    /// <summary>
    /// 庆祝类型枚举
    /// </summary>
    public enum CelebrationType
    {
        /// <summary>
        /// 成就解锁
        /// </summary>
        AchievementUnlocked,

        /// <summary>
        /// 学习完成
        /// </summary>
        LearningComplete,

        /// <summary>
        /// 完美得分
        /// </summary>
        PerfectScore,

        /// <summary>
        /// 达成里程碑
        /// </summary>
        MilestoneReached
    }
}
