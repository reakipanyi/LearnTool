
using LearningAssistant.Common.Events;
using LearningAssistant.Models.User;

namespace LearningAssistant.Services.Learning
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
            _eventBus.Subscribe<FeynmanCompletedEvent>(OnFeynmanCompleted);
        }

        private void UnsubscribeFromEvents()
        {
            _eventBus.Unsubscribe<LearningItemCompletedEvent>(OnLearningItemCompleted);
            _eventBus.Unsubscribe<LearningSessionCompletedEvent>(OnLearningSessionCompleted);
            _eventBus.Unsubscribe<UserProfileUpdatedEvent>(OnUserProfileUpdated);
            _eventBus.Unsubscribe<FeynmanCompletedEvent>(OnFeynmanCompleted);
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

        private void OnFeynmanCompleted(FeynmanCompletedEvent evt)
        {
            if (_currentUserProfile != null && evt.UserId == _currentUserId)
            {
                _currentUserProfile.LearningProgress.FeynmanCompletedCount++;
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
                AchievementType.TotalItemsStudied => progress.ComputedTotalItemsStudied >= requirement.TargetValue,
                AchievementType.MasteredItems => progress.ComputedTotalItemsMastered >= requirement.TargetValue,
                AchievementType.ConsecutiveDays => CheckConsecutiveDays(progress, requirement.TargetValue),
                AchievementType.PerfectSession => CheckPerfectSession(progress),
                AchievementType.FeynmanCompleted => progress.FeynmanCompletedCount >= requirement.TargetValue,
                AchievementType.FeynmanMaster => progress.FeynmanCompletedCount >= requirement.TargetValue,
                _ => false
            };
        }

        private bool CheckConsecutiveDays(LearningProgress progress, int targetDays)
        {
            if (_currentUserProfile != null)
            {
                return _currentUserProfile.ConsecutiveStudyDays >= targetDays;
            }
            return false;
        }

        private bool CheckPerfectSession(LearningProgress progress)
        {
            foreach (var categoryProgress in progress.CategoryProgresses.Values)
            {
                if (categoryProgress.TotalTestCount >= 10 && 
                    categoryProgress.CorrectCount == categoryProgress.TotalTestCount)
                {
                    return true;
                }
            }
            return progress.PerfectSessions > 0;
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

