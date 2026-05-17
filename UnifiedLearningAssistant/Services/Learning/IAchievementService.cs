
using UnifiedLearningAssistant.Models.User;

namespace UnifiedLearningAssistant.Services.Learning
{
    public interface IAchievementService
    {
        void LoadProgress(UserProfile profile);
        void CheckAndUnlockAchievements(UserProfile profile, LearningProgress progress);
        List<Achievement> GetAllAchievements();
        List<Achievement> GetUnlockedAchievements();
        List<Achievement> GetLockedAchievements();
        event EventHandler<AchievementUnlockedEventArgs>? AchievementUnlocked;
    }
}

