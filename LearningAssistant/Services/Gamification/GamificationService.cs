using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Managers;
using LearningAssistant.Models;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Feedback;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Gamification
{
    public class GamificationService : IGamificationService, IDisposable
    {
        private readonly ILogger<GamificationService>? _logger;
        private readonly IEventBus? _eventBus;

        private readonly StudyStatsManager _statsManager;
        private readonly BadgeManager _badgeManager;
        private readonly ChallengeManager _challengeManager;

        private string _userId = "default";
        private TimeSpan _studyDuration;
        private int _quizCorrectCount;
        private int _favoriteCount;
        private int _noteCount;
        private int _wrongCount;
        private bool _disposed = false;

        #region Stats
        public int TodayLearnedCount => _statsManager.TodayLearnedCount;
        public int StreakDays => _statsManager.StreakDays;
        public int TotalScore => _statsManager.TotalScore;
        public int TotalLearnedCount => _statsManager.TotalLearnedCount;
        public int XP => _statsManager.XP;
        public int CurrentLevel => _statsManager.CurrentLevel;
        public int XPToNextLevel => _statsManager.XPToNextLevel;
        public string LevelTitle => _statsManager.LevelTitle;
        public TimeSpan StudyDuration => _studyDuration;
        #endregion

        #region Events
        public event EventHandler<ScoreChangedEventArgs>? ScoreChanged;
        public event EventHandler<XPChangedEventArgs>? XPChanged;
        public event EventHandler<LevelUpEventArgs>? LevelUp;
        public event EventHandler<BadgesUnlockedEventArgs>? BadgesUnlocked;
        public event EventHandler<ChallengeCompletedEventArgs>? ChallengeCompleted;
        #endregion

        public GamificationService(
            ILoggerFactory? loggerFactory = null,
            IEventBus? eventBus = null)
        {
            _logger = loggerFactory?.CreateLogger<GamificationService>();
            _eventBus = eventBus;

            _statsManager = new StudyStatsManager(
                loggerFactory?.CreateLogger<StudyStatsManager>(),
                OnLevelUp,
                OnScoreChanged,
                OnXPChanged);

            _badgeManager = new BadgeManager(loggerFactory?.CreateLogger<BadgeManager>());
            _badgeManager.BadgesUnlocked += OnBadgesUnlocked;

            _challengeManager = new ChallengeManager(
                loggerFactory?.CreateLogger<ChallengeManager>(),
                OnScoreChanged,
                OnXPChanged,
                OnLevelUp,
                OnChallengeCompleted);

            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (_eventBus == null) return;

            _eventBus.Subscribe<ItemLearnedEvent>(OnItemLearned);
            _eventBus.Subscribe<ItemWrongEvent>(OnItemWrong);
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventBus == null) return;

            _eventBus.Unsubscribe<ItemLearnedEvent>(OnItemLearned);
            _eventBus.Unsubscribe<ItemWrongEvent>(OnItemWrong);
        }

        private void OnItemLearned(ItemLearnedEvent evt)
        {
            if (evt.UserId != _userId) return;

            try
            {
                AddXP(10);
                IncrementTodayLearned();
                CheckBadgesAndChallenges();

                _logger?.LogInformation("学习项完成事件处理: {ItemContent}, XP+10", evt.ItemContent);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "处理学习项完成事件失败");
            }
        }

        private void OnItemWrong(ItemWrongEvent evt)
        {
            if (evt.UserId != _userId) return;

            try
            {
                _wrongCount++;
                CheckBadgesAndChallenges();

                _logger?.LogInformation("学习项答错事件处理: {ItemContent}, 累计错题数: {WrongCount}", 
                    evt.ItemContent, _wrongCount);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "处理学习项答错事件失败");
            }
        }

        #region Stats Methods
        public void AddScore(int points)
        {
            _statsManager.AddScore(points);
            CheckBadgesAndChallenges();
            PublishStatsChangedEvent();
        }

        public void AddXP(int xp)
        {
            _statsManager.AddXP(xp);
            PublishStatsChangedEvent();
        }

        public void IncrementTodayLearned()
        {
            _statsManager.IncrementLearnedCount();
            CheckBadgesAndChallenges();
            PublishStatsChangedEvent();
        }

        public void UpdateStudyDuration(TimeSpan duration)
        {
            _studyDuration = duration;
        }

        public void CheckStreak()
        {
            CheckBadgesAndChallenges();
            PublishStatsChangedEvent();
        }

        public void RecordQuizCorrect()
        {
            _quizCorrectCount++;
            CheckBadgesAndChallenges();
        }

        public void RecordFavorite()
        {
            _favoriteCount++;
            CheckBadgesAndChallenges();
        }

        public void RecordNote()
        {
            _noteCount++;
            CheckBadgesAndChallenges();
        }

        private void CheckBadgesAndChallenges()
        {
            _badgeManager.CheckUnlock(
                _statsManager.TotalLearnedCount,
                _statsManager.StreakDays,
                _statsManager.TodayLearnedCount,
                _quizCorrectCount,
                _favoriteCount,
                _noteCount);

            _challengeManager.SetLearningData(
                _statsManager.TodayLearnedCount,
                _quizCorrectCount,
                _favoriteCount);
            _challengeManager.UpdateProgress();
        }
        #endregion

        #region Badge Methods
        public IEnumerable<Badge> GetAllBadges()
        {
            return _badgeManager.AllBadges;
        }

        public int UnlockedBadgeCount => _badgeManager.UnlockedCount;

        public void CheckBadgeUnlock(string type, int value)
        {
            CheckBadgesAndChallenges();
        }

        public Dictionary<string, int> GetBadgeProgress()
        {
            return _badgeManager.GetProgressDictionary(
                TotalLearnedCount,
                StreakDays,
                (int)StudyDuration.TotalMinutes,
                _quizCorrectCount,
                _favoriteCount,
                _noteCount);
        }
        #endregion

        #region Challenge Methods
        public IEnumerable<Challenge> GetDailyChallenges()
        {
            return _challengeManager.GetAllChallenges();
        }

        public int CompletedChallengeCount => _challengeManager.CompletedCount;

        public void UpdateChallengeProgress(string type, int value)
        {
            _challengeManager.UpdateProgress();
        }

        public bool ClaimChallengeReward(string challengeId)
        {
            var challenge = _challengeManager.GetAllChallenges().FirstOrDefault(c => c.Id == challengeId);
            if (challenge == null || !challenge.Completed || challenge.Claimed)
                return false;

            _challengeManager.ClaimReward(challenge);
            return true;
        }
        #endregion

        #region Persistence
        public void Load(string userId)
        {
            _userId = userId;
            _statsManager.Load(userId);
            _badgeManager.Load(userId);
            _challengeManager.Load(userId);
        }

        public void Save()
        {
            _statsManager.Save(_userId);
            _badgeManager.Save(_userId);
            _challengeManager.Save(_userId);
        }
        #endregion

        #region UI Integration (Backward Compatibility)
        [Obsolete("Use events instead of SetUI pattern for better separation of concerns")]
        public void SetStatsUI(Label studyTime, Label score, Label todayCount, Label streak,
            Label? level, Label? xp, ProgressBar? progressXp)
        {
            _statsManager.SetUI(studyTime, score, todayCount, streak, level, xp, progressXp);
            _statsManager.UpdateDisplay();
        }

        [Obsolete("Use events instead of SetUI pattern for better separation of concerns")]
        public void SetBadgeUI(FlowLayoutPanel panel, ToolTip toolTip)
        {
            _badgeManager.SetUI(panel, toolTip);
            _badgeManager.UpdateDisplay();
        }

        [Obsolete("Use events instead of SetUI pattern for better separation of concerns")]
        public void SetChallengeUI(FlowLayoutPanel panel, object? soundService = null)
        {
            _challengeManager.SetUI(panel, soundService as ISoundService);
            _challengeManager.UpdateDisplay();
        }

        public void UpdateAllDisplays()
        {
            _statsManager.UpdateDisplay();
            _badgeManager.UpdateDisplay();
            _challengeManager.UpdateDisplay();
        }
        #endregion

        #region Event Handlers
        private void OnScoreChanged(int newScore)
        {
            ScoreChanged?.Invoke(this, new ScoreChangedEventArgs
            {
                NewScore = newScore,
                Added = 0
            });
        }

        private void OnXPChanged(int newXP)
        {
            XPChanged?.Invoke(this, new XPChangedEventArgs
            {
                NewXP = newXP,
                Added = 0
            });

            PublishStatsChangedEvent();
        }

        private void OnLevelUp()
        {
            LevelUp?.Invoke(this, new LevelUpEventArgs
            {
                NewLevel = 0,
                LevelTitle = _statsManager.LevelTitle
            });
        }

        private void OnBadgesUnlocked(List<string> badgeIds)
        {
            BadgesUnlocked?.Invoke(this, new BadgesUnlockedEventArgs
            {
                BadgeIds = badgeIds
            });

            if (_eventBus != null && badgeIds.Count > 0)
            {
                var firstBadge = _badgeManager.AllBadges.FirstOrDefault(b => b.Id == badgeIds[0]);
                if (firstBadge != null)
                {
                    _ = Task.Run(async () =>
                    {
                        await _eventBus.PublishAsync(new AchievementUnlockedEvent
                        {
                            UserId = _userId,
                            AchievementId = firstBadge.Id,
                            AchievementName = firstBadge.Name,
                            Description = firstBadge.Description,
                            Icon = firstBadge.Icon,
                            IsHidden = firstBadge.IsHidden
                        });
                    });
                }
            }
        }

        private void OnChallengeCompleted()
        {
            var completed = _challengeManager.GetAllChallenges().FirstOrDefault(c => c.Completed && !c.Claimed);
            if (completed != null)
            {
                ChallengeCompleted?.Invoke(this, new ChallengeCompletedEventArgs
                {
                    ChallengeId = completed.Id,
                    ChallengeName = completed.Name,
                    RewardXP = completed.Reward,
                    RewardScore = completed.Reward
                });
            }
        }

        private void PublishStatsChangedEvent()
        {
            if (_eventBus != null)
            {
                _ = Task.Run(async () =>
                {
                    await _eventBus.PublishAsync(new UserProfileUpdatedEvent
                    {
                        UserId = _userId
                    });
                });
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_disposed) return;

            UnsubscribeFromEvents();
            _disposed = true;
        }
        #endregion
    }
}
