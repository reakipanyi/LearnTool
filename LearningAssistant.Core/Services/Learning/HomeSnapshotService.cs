using LearningAssistant.Models.Home;
using LearningAssistant.Services.Gamification;
using LearningAssistant.Services.Persistence;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 首页快照聚合服务。
    /// 各数据源独立降级：单个来源异常只记日志，不影响整页展示。
    /// </summary>
    public class HomeSnapshotService : IHomeSnapshotService
    {
        private readonly IGamificationService _gamificationService;
        private readonly INoteService? _noteService;
        private readonly ISpacedRepetitionService? _spacedRepetitionService;
        private readonly ILearningGoalService? _goalService;
        private readonly ILearningStatsAggregator? _aggregator;
        private readonly IDataPersistenceService? _persistenceService;
        private readonly ILogger<HomeSnapshotService>? _logger;

        public HomeSnapshotService(
            IGamificationService gamificationService,
            INoteService? noteService = null,
            ISpacedRepetitionService? spacedRepetitionService = null,
            ILearningGoalService? goalService = null,
            ILearningStatsAggregator? aggregator = null,
            IDataPersistenceService? persistenceService = null,
            ILogger<HomeSnapshotService>? logger = null)
        {
            _gamificationService = gamificationService ?? throw new ArgumentNullException(nameof(gamificationService));
            _noteService = noteService;
            _spacedRepetitionService = spacedRepetitionService;
            _goalService = goalService;
            _aggregator = aggregator;
            _persistenceService = persistenceService;
            _logger = logger;
        }

        public HomeSnapshot GetSnapshot(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return HomeSnapshot.Empty;

            int todayMinutes = 0;
            int streakDays = 0;
            int totalXp = 0;
            int level = 0;
            int xpToNext = 0;
            int completedChallenges = 0;
            int totalChallenges = 0;
            int noteCount = 0;
            int todayNewNotes = 0;
            int dueReviewCount = 0;
            int goalCompleted = 0;
            int goalTarget = 0;

            // 经验/等级：游戏化服务是首页核心指标，失败时保持 0
            try
            {
                totalXp = _gamificationService.XP;
                level = _gamificationService.CurrentLevel;
                xpToNext = _gamificationService.XPToNextLevel;

                var challenges = _gamificationService.GetDailyChallenges()?.ToList() ?? new();
                totalChallenges = challenges.Count;
                completedChallenges = challenges.Count(c => c.Completed);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "首页快照：读取游戏化数据失败，用户 {UserId}", userId);
            }

            // 今日时长/连续天数：优先统计底座，降级到用户资料缓存
            try
            {
                if (_aggregator != null)
                {
                    var today = _aggregator.GetDailyOverview(userId, DateTime.Today);
                    todayMinutes = today.TimeSpentMinutes;
                    streakDays = today.StreakDays;
                    if (today.XP > 0) totalXp = today.XP;
                    if (today.Level > 0) level = today.Level;
                }
                else if (_persistenceService != null)
                {
                    var profile = _persistenceService.LoadUserProfile(userId);
                    if (profile != null)
                    {
                        todayMinutes = profile.TodayStudyTimeMinutes;
                        streakDays = profile.ConsecutiveStudyDays;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "首页快照：读取学习统计失败，用户 {UserId}", userId);
            }

            // 笔记：总数与今日新增
            try
            {
                if (_noteService != null)
                {
                    noteCount = _noteService.GetNoteCount(userId);
                    var notes = _noteService.GetNotes(userId);
                    todayNewNotes = notes.Count(n => n.CreatedAt.Date == DateTime.Today);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "首页快照：读取笔记数据失败，用户 {UserId}", userId);
            }

            // 待复习：间隔重复待复习项
            try
            {
                if (_spacedRepetitionService != null)
                {
                    var dueItems = _spacedRepetitionService.GetItemsDueForReview(userId);
                    dueReviewCount = dueItems?.Count ?? 0;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "首页快照：读取待复习数据失败，用户 {UserId}", userId);
            }

            // 今日目标：默认目标由目标服务兜底（无自定义时通常为 20 项）
            try
            {
                if (_goalService != null)
                {
                    var goal = _goalService.GetDailyGoal(userId);
                    if (goal != null)
                    {
                        goalCompleted = goal.CompletedItems;
                        goalTarget = goal.TargetItems;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "首页快照：读取学习目标失败，用户 {UserId}", userId);
            }

            return new HomeSnapshot(
                todayMinutes,
                streakDays,
                totalXp,
                level,
                xpToNext,
                completedChallenges,
                totalChallenges,
                noteCount,
                todayNewNotes,
                dueReviewCount,
                goalCompleted,
                goalTarget);
        }
    }
}
