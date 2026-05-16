
using UnifiedLearningAssistant.Models.User;

namespace UnifiedLearningAssistant.Services.Learning
{
    public interface IAchievementService
    {
        void LoadProgress(UserProfile profile);
        void CheckAndUnlockAchievements(UserProfile profile, LearningProgress progress);
        List&lt;Achievement&gt; GetAllAchievements();
        List&lt;Achievement&gt; GetUnlockedAchievements();
        List&lt;Achievement&gt; GetLockedAchievements();
        event EventHandler&lt;AchievementUnlockedEventArgs&gt;? AchievementUnlocked;
    }
}

