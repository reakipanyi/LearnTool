using LearningAssistant.Abstractions;
using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Data.Database;
using LearningAssistant.Managers;
using LearningAssistant.Models;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Feedback;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Gamification
{
    public class GamificationService : IGamificationService, IGamificationUIBinding, IDisposable
    {
        private readonly ILogger<GamificationService>? _logger;
        private readonly IEventBus? _eventBus;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        private readonly StudyStatsManager _statsManager;
        private readonly BadgeManager _badgeManager;
        private readonly ChallengeManager _challengeManager;

        private string _userId = Constants.DefaultUserId;
        private TimeSpan _studyDuration;
        private int _quizCorrectCount;
        private int _quizTotalCount;
        private int _favoriteCount;
        private int _noteCount;
        private int _wrongCount;
        private bool _disposed = false;

        // 速度学习追踪：5分钟内连续学习计数
        private int _speedLearnBurstCount;
        private DateTime _speedLearnBurstStart;
        private int _speedLearnMaxCount;
        // 深夜学习追踪
        private int _nightStudyCount;

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
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILoggerFactory? loggerFactory = null,
            IEventBus? eventBus = null,
            IAppPaths? appPaths = null)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = loggerFactory?.CreateLogger<GamificationService>();
            _eventBus = eventBus;

            _statsManager = new StudyStatsManager(
                dbContextFactory,
                loggerFactory?.CreateLogger<StudyStatsManager>(),
                OnLevelUp,
                OnScoreChanged,
                OnXPChanged,
                appPaths);

            _badgeManager = new BadgeManager(dbContextFactory, loggerFactory?.CreateLogger<BadgeManager>(), null, appPaths);
            _badgeManager.BadgesUnlocked += OnBadgesUnlocked;

            _challengeManager = new ChallengeManager(
                dbContextFactory,
                loggerFactory?.CreateLogger<ChallengeManager>(),
                OnScoreChanged,
                OnXPChanged,
                OnLevelUp,
                OnChallengeCompleted,
                appPaths);

            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (_eventBus == null) return;

            _eventBus.Subscribe<ItemLearnedEvent>(OnItemLearned);
            _eventBus.Subscribe<ItemWrongEvent>(OnItemWrong);
            _eventBus.Subscribe<FeynmanCompletedEvent>(OnFeynmanCompleted);
            _eventBus.Subscribe<PomodoroCompletedEvent>(OnPomodoroCompleted);
            _eventBus.Subscribe<NoteAddedEvent>(OnNoteAdded);
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventBus == null) return;

            _eventBus.Unsubscribe<ItemLearnedEvent>(OnItemLearned);
            _eventBus.Unsubscribe<ItemWrongEvent>(OnItemWrong);
            _eventBus.Unsubscribe<FeynmanCompletedEvent>(OnFeynmanCompleted);
            _eventBus.Unsubscribe<PomodoroCompletedEvent>(OnPomodoroCompleted);
            _eventBus.Unsubscribe<NoteAddedEvent>(OnNoteAdded);
        }

        private void OnItemLearned(ItemLearnedEvent evt)
        {
            if (evt.UserId != _userId) return;

            try
            {
                // 深夜学习检测（00:00-05:00）
                int hour = DateTime.Now.Hour;
                if (hour >= 0 && hour < 5)
                {
                    _nightStudyCount++;
                }

                // 速度学习检测：5分钟内连续学习计数
                DateTime now = DateTime.Now;
                if (_speedLearnBurstCount == 0 || (now - _speedLearnBurstStart).TotalMinutes >= 5)
                {
                    _speedLearnBurstStart = now;
                    _speedLearnBurstCount = 1;
                }
                else
                {
                    _speedLearnBurstCount++;
                    if (_speedLearnBurstCount > _speedLearnMaxCount)
                    {
                        _speedLearnMaxCount = _speedLearnBurstCount;
                    }
                }

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
                _quizTotalCount++;
                CheckBadgesAndChallenges();

                _logger?.LogInformation("学习项答错事件处理: {ItemContent}, 累计错题数: {WrongCount}", 
                    evt.ItemContent, _wrongCount);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "处理学习项答错事件失败");
            }
        }

        private void OnFeynmanCompleted(FeynmanCompletedEvent evt)
        {
            if (evt.UserId != _userId) return;

            try
            {
                AddXP(50);
                AddScore(100);
                CheckBadgesAndChallenges();

                _logger?.LogInformation("费曼学习完成事件处理: {ItemContent}, XP+50, Score+100", evt.ItemContent);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "处理费曼学习完成事件失败");
            }
        }

        private void OnPomodoroCompleted(PomodoroCompletedEvent evt)
        {
            if (evt.UserId != _userId) return;

            try
            {
                AddXP(25);
                AddScore(50);
                _studyDuration += TimeSpan.FromMinutes(evt.DurationMinutes);
                CheckBadgesAndChallenges();

                _logger?.LogInformation("番茄钟完成事件处理: Task={TaskName}, XP+25, Score+50, Duration={Duration}min", 
                    evt.TaskName, evt.DurationMinutes);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "处理番茄钟完成事件失败");
            }
        }

        private void OnNoteAdded(NoteAddedEvent evt)
        {
            if (evt.UserId != _userId) return;

            try
            {
                AddXP(15);
                AddScore(30);
                _noteCount++;
                CheckBadgesAndChallenges();

                _logger?.LogInformation("笔记添加事件处理: {NoteTitle}, XP+15, Score+30", evt.NoteTitle);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "处理笔记添加事件失败");
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

        public Task AddXpAsync(int xp, string reason = "")
        {
            AddXP(xp);
            _logger?.LogDebug("添加经验值: {XP}, 原因: {Reason}", xp, reason);
            return Task.CompletedTask;
        }

        public Task AddScoreAsync(int points, string reason = "")
        {
            AddScore(points);
            _logger?.LogDebug("添加积分: {Points}, 原因: {Reason}", points, reason);
            return Task.CompletedTask;
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
            _quizTotalCount++;
            CheckBadgesAndChallenges();
        }

        public void RecordQuizWrong()
        {
            _quizTotalCount++;
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

        public void RecordWrongReview()
        {
            _wrongCount++;
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
                _noteCount,
                (int)_studyDuration.TotalMinutes,
                _speedLearnMaxCount,
                _nightStudyCount);

            _challengeManager.SetLearningData(
                _statsManager.TodayLearnedCount,
                _quizCorrectCount,
                _favoriteCount,
                (int)_studyDuration.TotalMinutes,
                _statsManager.StreakDays,
                _wrongCount,
                _noteCount,
                _quizTotalCount);
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
                _noteCount,
                speedLearningCount: _speedLearnMaxCount,
                nightStudyCount: _nightStudyCount);
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
                        try
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
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "发布成就解锁事件失败");
                        }
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

            _badgeManager.CheckUnlock(
                _statsManager.TotalLearnedCount,
                _statsManager.StreakDays,
                _statsManager.TodayLearnedCount,
                _quizCorrectCount,
                _favoriteCount,
                _noteCount,
                (int)_studyDuration.TotalMinutes,
                _speedLearnMaxCount,
                _nightStudyCount);
        }

        private void PublishStatsChangedEvent()
        {
            if (_eventBus != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _eventBus.PublishAsync(new UserProfileUpdatedEvent
                        {
                            UserId = _userId
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "发布用户统计更新事件失败");
                    }
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
