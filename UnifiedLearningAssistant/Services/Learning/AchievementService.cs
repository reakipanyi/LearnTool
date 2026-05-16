
using UnifiedLearningAssistant.Models.User;

namespace UnifiedLearningAssistant.Services.Learning
{
    public class AchievementService
    {
        private List&lt;Achievement&gt; _achievements;
        private readonly object _lock = new object();

        public event EventHandler&lt;AchievementUnlockedEventArgs&gt;? AchievementUnlocked;

        public AchievementService()
        {
            _achievements = AchievementHelper.GetAllAchievements();
        }

        public void LoadProgress(UserProfile profile)
        {
            lock (_lock)
            {
                foreach (var achievement in _achievements)
                {
                    var unlockedAchievement = profile.UnlockedAchievements.FirstOrDefault(a =&gt; a.Id == achievement.Id);
                    if (unlockedAchievement != null)
                    {
                        achievement.IsUnlocked = true;
                        achievement.UnlockedAt = unlockedAchievement.UnlockedAt;
                    }
                }
            }
        }

        public void CheckAndUnlockAchievements(UserProfile profile, LearningProgress progress)
        {
            var newUnlocks = new List&lt;Achievement&gt;();

            lock (_lock)
            {
                foreach (var achievement in _achievements.Where(a =&gt; !a.IsUnlocked))
                {
                    bool shouldUnlock = CheckRequirement(achievement.Requirement, progress);
                    if (shouldUnlock)
                    {
                        achievement.IsUnlocked = true;
                        achievement.UnlockedAt = DateTime.Now;
                        newUnlocks.Add(achievement);

                        if (!profile.UnlockedAchievements.Any(a =&gt; a.Id == achievement.Id))
                        {
                            profile.UnlockedAchievements.Add(new UnlockedAchievement
                            {
                                Id = achievement.Id,
                                UnlockedAt = achievement.UnlockedAt.Value
                            });
                        }
                    }
                }
            }

            foreach (var achievement in newUnlocks)
            {
                AchievementUnlocked?.Invoke(this, new AchievementUnlockedEventArgs(achievement));
            }
        }

        private bool CheckRequirement(AchievementRequirement requirement, LearningProgress progress)
        {
            return requirement.Type switch
            {
                AchievementType.TotalItemsStudied =&gt; progress.TotalItemsStudied &gt;= requirement.TargetValue,
                AchievementType.MasteredItems =&gt; progress.TotalItemsMastered &gt;= requirement.TargetValue,
                _ =&gt; false
            };
        }

        public List&lt;Achievement&gt; GetAllAchievements()
        {
            lock (_lock)
            {
                return _achievements.OrderBy(a =&gt; a.DisplayOrder).ToList();
            }
        }

        public List&lt;Achievement&gt; GetUnlockedAchievements()
        {
            lock (_lock)
            {
                return _achievements.Where(a =&gt; a.IsUnlocked).OrderBy(a =&gt; a.DisplayOrder).ToList();
            }
        }

        public List&lt;Achievement&gt; GetLockedAchievements()
        {
            lock (_lock)
            {
                return _achievements.Where(a =&gt; !a.IsUnlocked).OrderBy(a =&gt; a.DisplayOrder).ToList();
            }
        }
    }

    public class AchievementUnlockedEventArgs : EventArgs
    {
        public Achievement Achievement { get; }

        public AchievementUnlockedEventArgs(Achievement achievement)
        {
            Achievement = achievement;
        }
    }

    public class UnlockedAchievement
    {
        public string Id { get; set; } = string.Empty;
        public DateTime UnlockedAt { get; set; }
    }
}

