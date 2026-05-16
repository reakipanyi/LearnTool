
namespace UnifiedLearningAssistant.Services.Notifications
{
    public interface INotificationService
    {
        void ShowNotification(Notification notification);
        void ShowAchievementUnlock(string icon, string title, string message);
        void ShowLearningMilestone(string message);
        void ShowError(string title, string message);
    }

    public interface ICelebrationService
    {
        void TriggerCelebration(CelebrationType type, CelebrationContext context);
        void ShowConfetti();
        void PlayCelebrationSound();
    }

    public class Notification
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public NotificationType Type { get; set; } = NotificationType.Info;
        public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(3);
        public Action? OnClick { get; set; }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        Achievement
    }

    public class CelebrationContext
    {
        public string UserId { get; set; } = string.Empty;
        public string? AchievementId { get; set; }
        public int? Score { get; set; }
        public string? Message { get; set; }
    }

    public enum CelebrationType
    {
        AchievementUnlocked,
        LearningComplete,
        PerfectScore,
        MilestoneReached
    }
}

