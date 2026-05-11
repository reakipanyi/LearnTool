namespace UnifiedLearningAssistant.Models.User
{
    public class UserProfile
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastLoginTime { get; set; } = DateTime.Now;
        public string AvatarPath { get; set; } = string.Empty;
        public LearningProgress LearningProgress { get; set; } = new LearningProgress();
    }
}