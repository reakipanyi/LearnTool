
namespace LearningAssistant.Services.Notifications
{
    /// <summary>
    /// 通知服务接口 - 提供应用内通知和消息展示功能
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// 显示一般通知
        /// </summary>
        /// <param name="notification">通知对象（包含标题、消息、图标等）</param>
        void ShowNotification(Notification notification);

        /// <summary>
        /// 显示成就解锁通知
        /// </summary>
        /// <param name="icon">成就图标标识</param>
        /// <param name="title">通知标题</param>
        /// <param name="message">通知消息内容</param>
        void ShowAchievementUnlock(string icon, string title, string message);

        /// <summary>
        /// 显示学习里程碑通知
        /// </summary>
        /// <param name="message">里程碑消息</param>
        void ShowLearningMilestone(string message);

        /// <summary>
        /// 显示错误通知
        /// </summary>
        /// <param name="title">错误标题</param>
        /// <param name="message">错误消息内容</param>
        void ShowError(string title, string message);
    }

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
    /// 通知数据类 - 包含通知的完整信息
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// 通知标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 通知消息内容
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 通知图标标识
        /// </summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// 通知类型（Info, Success, Warning, Error, Achievement）
        /// </summary>
        public NotificationType Type { get; set; } = NotificationType.Info;

        /// <summary>
        /// 通知显示时长，默认为3秒
        /// </summary>
        public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// 点击通知时的回调动作
        /// </summary>
        public Action? OnClick { get; set; }
    }

    /// <summary>
    /// 通知类型枚举
    /// </summary>
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        Achievement
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
