
namespace UnifiedLearningAssistant.Common.Events
{
    // 学习相关事件
    public class LearningSessionStartedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public string WordBankFile { get; set; } = string.Empty;
    }

    public class LearningItemCompletedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public string ItemContent { get; set; } = string.Empty;
        public bool IsKnown { get; set; }
        public string SubCategory { get; set; } = string.Empty;
    }

    public class LearningSessionCompletedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public int CorrectCount { get; set; }
        public double Accuracy { get; set; }
        public string SubCategory { get; set; } = string.Empty;
    }

    // 成就相关事件
    public class AchievementUnlockedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
        public string AchievementId { get; set; } = string.Empty;
        public string AchievementName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    // 主题相关事件
    public class ThemeChangedEvent : ApplicationEventBase
    {
        public ThemeMode NewTheme { get; set; }
        public ThemeMode OldTheme { get; set; }
    }

    // 用户相关事件
    public class UserProfileUpdatedEvent : ApplicationEventBase
    {
        public string UserId { get; set; } = string.Empty;
    }
}

