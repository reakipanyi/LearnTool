
using UnifiedLearningAssistant.Common.Events;
using UnifiedLearningAssistant.Models.User;

namespace UnifiedLearningAssistant.Services.Learning
{
    public class AchievementService : IAchievementService, IDisposable
    {
        private List&lt;Achievement&gt; _achievements;
        private readonly object _lock = new object();
        private readonly IEventBus _eventBus;
        private string _currentUserId = string.Empty;
        private UserProfile? _currentUserProfile;

        public event EventHandler&lt;AchievementUnlockedEventArgs&gt;? AchievementUnlocked;

        public AchievementService(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _achievements = AchievementHelper.GetAllAchievements();
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            _eventBus.Subscribe&lt;LearningItemCompletedEvent&gt;(OnLearningItemCompleted);
            _eventBus.Subscribe&lt;LearningSessionCompletedEvent&gt;(OnLearningSessionCompleted);
            _eventBus.Subscribe&lt;UserProfileUpdatedEvent&gt;(OnUserProfileUpdated);
        }

        private void UnsubscribeFromEvents()
        {
            _eventBus.Unsubscribe&lt;LearningItemCompletedEvent&gt;(OnLearningItemCompleted);
            _eventBus.Unsubscribe&lt;LearningSessionCompletedEvent&gt;(OnLearningSessionCompleted);
            _eventBus.Unsubscribe&lt;UserProfileUpdatedEvent&gt;(OnUserProfileUpdated);
        }

        private void OnLearningItemCompleted(LearningItemCompletedEvent evt)
        {
            if (_currentUserProfile != null &amp;&amp; evt.UserId == _currentUserId)
            {
                CheckAndUnlockAchievements(_currentUserProfile, _currentUserProfile.LearningProgress);
            }
        }

        private void OnLearningSessionCompleted(LearningSessionCompletedEvent evt)
        {
            if (_currentUserProfile != null &amp;&amp; evt.UserId == _currentUserId)
            {
                CheckAndUnlockAchievements(_currentUserProfile, _currentUserProfile.LearningProgress);
            }
        }

        private void OnUserProfileUpdated(UserProfileUpdatedEvent evt)
        {
            // 可以处理用户资料更新的逻辑
        }

        public void LoadProgress(UserProfile profile)
        {
            _currentUserId = profile.UserId;
            _currentUserProfile = profile;

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
                _eventBus.Publish(new AchievementUnlockedEvent
                {
                    UserId = profile.UserId,
                    AchievementId = achievement.Id,
                    AchievementName = achievement.Name,
                    Description = achievement.Description,
                    Icon = achievement.Icon
                });
            }
        }

        private bool CheckRequirement(AchievementRequirement requirement, LearningProgress progress)
        {
            return requirement.Type switch
            {
                AchievementType.TotalItemsStudied =&gt; progress.TotalItemsStudied &gt;= requirement.TargetValue,
                AchievementType.MasteredItems =&gt; progress.TotalItemsMastered &gt;= requirement.TargetValue,
                AchievementType.ConsecutiveDays =&gt; CheckConsecutiveDays(progress, requirement.TargetValue),
                AchievementType.PerfectSession =&gt; CheckPerfectSession(progress),
                _ =&gt; false
            };
        }

        private bool CheckConsecutiveDays(LearningProgress progress, int targetDays)
        {
            // 这里可以实现根据学习记录计算连续学习天数
            return false;
        }

        private bool CheckPerfectSession(LearningProgress progress)
        {
            // 检查是否有完美的学习会话
            return false;
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

        public void Dispose()
        {
            UnsubscribeFromEvents();
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

