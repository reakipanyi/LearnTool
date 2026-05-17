
using UnifiedLearningAssistant.Common.Events;
using UnifiedLearningAssistant.Models.User;

namespace UnifiedLearningAssistant.Services.Learning
{
    public class AchievementService : IAchievementService, IDisposable
    {
        private List<Achievement> _achievements;
        private readonly object _lock = new object();
        private readonly IEventBus _eventBus;
        private string _currentUserId = string.Empty;
        private UserProfile? _currentUserProfile;

        public event EventHandler<AchievementUnlockedEventArgs>? AchievementUnlocked;

        public AchievementService(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _achievements = AchievementHelper.GetAllAchievements();
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            _eventBus.Subscribe<LearningItemCompletedEvent>(OnLearningItemCompleted);
            _eventBus.Subscribe<LearningSessionCompletedEvent>(OnLearningSessionCompleted);
            _eventBus.Subscribe<UserProfileUpdatedEvent>(OnUserProfileUpdated);
        }

        private void UnsubscribeFromEvents()
        {
            _eventBus.Unsubscribe<LearningItemCompletedEvent>(OnLearningItemCompleted);
            _eventBus.Unsubscribe<LearningSessionCompletedEvent>(OnLearningSessionCompleted);
            _eventBus.Unsubscribe<UserProfileUpdatedEvent>(OnUserProfileUpdated);
        }

        private void OnLearningItemCompleted(LearningItemCompletedEvent evt)
        {
            if (_currentUserProfile != null && evt.UserId == _currentUserId)
            {
                CheckAndUnlockAchievements(_currentUserProfile, _currentUserProfile.LearningProgress);
            }
        }

        private void OnLearningSessionCompleted(LearningSessionCompletedEvent evt)
        {
            if (_currentUserProfile != null && evt.UserId == _currentUserId)
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
                    var unlockedAchievement = profile.UnlockedAchievements.FirstOrDefault(a => a.Id == achievement.Id);
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
            var newUnlocks = new List<Achievement>();

            lock (_lock)
            {
                foreach (var achievement in _achievements.Where(a => !a.IsUnlocked))
                {
                    bool shouldUnlock = CheckRequirement(achievement.Requirement, progress);
                    if (shouldUnlock)
                    {
                        achievement.IsUnlocked = true;
                        achievement.UnlockedAt = DateTime.Now;
                        newUnlocks.Add(achievement);

                        if (!profile.UnlockedAchievements.Any(a => a.Id == achievement.Id))
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
                AchievementType.TotalItemsStudied => progress.TotalItemsStudied >= requirement.TargetValue,
                AchievementType.MasteredItems => progress.TotalItemsMastered >= requirement.TargetValue,
                AchievementType.ConsecutiveDays => CheckConsecutiveDays(progress, requirement.TargetValue),
                AchievementType.PerfectSession => CheckPerfectSession(progress),
                _ => false
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

        public List<Achievement> GetAllAchievements()
        {
            lock (_lock)
            {
                return _achievements.OrderBy(a => a.DisplayOrder).ToList();
            }
        }

        public List<Achievement> GetUnlockedAchievements()
        {
            lock (_lock)
            {
                return _achievements.Where(a => a.IsUnlocked).OrderBy(a => a.DisplayOrder).ToList();
            }
        }

        public List<Achievement> GetLockedAchievements()
        {
            lock (_lock)
            {
                return _achievements.Where(a => !a.IsUnlocked).OrderBy(a => a.DisplayOrder).ToList();
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

