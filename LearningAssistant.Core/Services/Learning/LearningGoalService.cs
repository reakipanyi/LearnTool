using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Data.Database;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using LearningAssistant.Abstractions;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习目标服务实现
    /// 提供目标设置、进度追踪、连续达成统计等功能
    /// </summary>
    public class LearningGoalService : ILearningGoalService, IDisposable
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IDataPersistenceService _persistenceService;
        private readonly ILogger<LearningGoalService> _logger;
        private readonly IEventBus? _eventBus;
        private readonly IAppPaths _appPaths;
        private readonly HashSet<string> _completedGoalsToday = new();
        private string _userId = Constants.DefaultUserId;

        public event EventHandler<GoalType>? GoalCompleted;
        public event EventHandler? AllGoalsCompleted;

        public LearningGoalService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IDataPersistenceService persistenceService,
            ILogger<LearningGoalService> logger,
            IAppPaths appPaths,
            IEventBus? eventBus = null)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appPaths = appPaths ?? throw new ArgumentNullException(nameof(appPaths));
            _eventBus = eventBus;

            MigrateFromJsonToDb();
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (_eventBus == null) return;

            _eventBus.Subscribe<PomodoroCompletedEvent>(OnPomodoroCompleted);
            _eventBus.Subscribe<ItemLearnedEvent>(OnItemLearned);
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventBus == null) return;

            _eventBus.Unsubscribe<PomodoroCompletedEvent>(OnPomodoroCompleted);
            _eventBus.Unsubscribe<ItemLearnedEvent>(OnItemLearned);
        }

        private void OnPomodoroCompleted(PomodoroCompletedEvent evt)
        {
            _userId = evt.UserId;
            UpdateStudyMinutes(evt.UserId, evt.DurationMinutes);
            _logger?.LogInformation("番茄钟完成事件处理: 用户 {UserId}, 时长 {Duration} 分钟", evt.UserId, evt.DurationMinutes);
        }

        private void OnItemLearned(ItemLearnedEvent evt)
        {
            _userId = evt.UserId;
            IncrementStudyItems(evt.UserId, 1);
            _logger?.LogInformation("学习项完成事件处理: 用户 {UserId}", evt.UserId);
        }

        private void MigrateFromJsonToDb()
        {
            try
            {
                var goalsDir = Path.Combine(_appPaths.UsersDir, "goals");
                if (!Directory.Exists(goalsDir)) return;

                var migratedMarker = Path.Combine(goalsDir, ".migrated_to_db");
                if (File.Exists(migratedMarker)) return;

                foreach (var file in Directory.EnumerateFiles(goalsDir, "*_settings.json"))
                {
                    var fileName = Path.GetFileName(file);
                    var userId = fileName.Replace("_settings.json", "");

                    var json = File.ReadAllText(file);
                    var goals = System.Text.Json.JsonSerializer.Deserialize<List<LearningGoal>>(json) ?? new List<LearningGoal>();

                    if (goals.Count == 0) continue;

                    using var db = _dbContextFactory.CreateDbContext();
                    var existingTypes = db.LearningGoals.Where(g => g.UserId == userId).Select(g => g.GoalType).ToHashSet();

                    foreach (var goal in goals)
                    {
                        if (existingTypes.Contains(goal.Type.ToString())) continue;

                        db.LearningGoals.Add(new LearningGoalEntity
                        {
                            UserId = goal.UserId,
                            GoalType = goal.Type.ToString(),
                            TargetValue = goal.TargetValue,
                            Unit = goal.Unit,
                            Enabled = goal.Enabled
                        });
                    }

                    db.SaveChanges();
                }

                foreach (var file in Directory.EnumerateFiles(goalsDir, "*_records.json"))
                {
                    var fileName = Path.GetFileName(file);
                    var userId = fileName.Replace("_records.json", "");

                    var json = File.ReadAllText(file);
                    var records = System.Text.Json.JsonSerializer.Deserialize<List<DailyGoalRecord>>(json) ?? new List<DailyGoalRecord>();

                    if (records.Count == 0) continue;

                    using var db = _dbContextFactory.CreateDbContext();
                    var existingDates = db.DailyGoalRecords.Where(r => r.UserId == userId).Select(r => r.Date).ToHashSet();

                    foreach (var record in records)
                    {
                        if (existingDates.Contains(record.Date.Date)) continue;

                        db.DailyGoalRecords.Add(new DailyGoalRecordEntity
                        {
                            UserId = record.UserId,
                            Date = record.Date,
                            ProgressJson = System.Text.Json.JsonSerializer.Serialize(record.Progress),
                            CompletedJson = System.Text.Json.JsonSerializer.Serialize(record.Completed),
                            AllCompleted = record.AllCompleted
                        });
                    }

                    db.SaveChanges();
                }

                File.Create(migratedMarker).Dispose();
                _logger.LogInformation("迁移学习目标数据从JSON到数据库完成");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "迁移学习目标数据失败");
            }
        }

        #region 旧接口兼容

        public void SetDailyGoal(string userId, int itemsPerDay)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("用户ID不能为空", nameof(userId));
            if (itemsPerDay <= 0)
                throw new ArgumentException("每日目标必须大于0", nameof(itemsPerDay));

            try
            {
                var goals = LoadGoals(userId);
                var studyItemsGoal = goals.FirstOrDefault(g => g.Type == GoalType.DailyStudyItems);

                if (studyItemsGoal != null)
                {
                    studyItemsGoal.TargetValue = itemsPerDay;
                    studyItemsGoal.Enabled = true;
                    studyItemsGoal.UpdatedAt = DateTime.Now;
                }
                else
                {
                    goals.Add(new LearningGoal
                    {
                        UserId = userId,
                        Type = GoalType.DailyStudyItems,
                        TargetValue = itemsPerDay,
                        Unit = "个",
                        Enabled = true,
                        CreatedAt = DateTime.Now
                    });
                }

                SaveGoals(userId, goals);
                _logger.LogInformation("用户 {UserId} 设置每日学习数量目标为 {Count} 个", userId, itemsPerDay);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置每日目标失败: {UserId}", userId);
                throw;
            }
        }

        public DailyGoal? GetDailyGoal(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            try
            {
                var progressList = GetTodayProgressList(userId);
                var studyItemsProgress = progressList.FirstOrDefault(p => p.Type == GoalType.DailyStudyItems);

                var targetItems = studyItemsProgress?.TargetValue ?? 20;
                var completedItems = studyItemsProgress?.CurrentValue ?? 0;

                return new DailyGoal
                {
                    UserId = userId,
                    Date = DateTime.Today,
                    TargetItems = targetItems,
                    CompletedItems = completedItems
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取每日目标失败: {UserId}", userId);
                return null;
            }
        }

        public int GetTodayProgress(string userId)
        {
            var goal = GetDailyGoal(userId);
            if (goal == null || goal.TargetItems == 0)
                return 0;
            return Math.Min(100, (int)(goal.CompletedItems * 100.0 / goal.TargetItems));
        }

        public bool IsDailyGoalCompleted(string userId)
        {
            var goal = GetDailyGoal(userId);
            return goal?.IsCompleted ?? false;
        }

        public List<DailyGoal> GetGoalHistory(string userId, int days = 30)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new List<DailyGoal>();

            try
            {
                var records = LoadRecords(userId);
                var goals = LoadGoals(userId);
                var studyItemsGoal = goals.FirstOrDefault(g => g.Type == GoalType.DailyStudyItems);
                var targetItems = studyItemsGoal?.TargetValue ?? 20;

                var result = new List<DailyGoal>();
                for (int i = days - 1; i >= 0; i--)
                {
                    var date = DateTime.Today.AddDays(-i);
                    var record = records.FirstOrDefault(r => r.Date.Date == date.Date);

                    var completedItems = record?.Progress.TryGetValue(GoalType.DailyStudyItems, out var val) == true
                        ? val
                        : 0;

                    result.Add(new DailyGoal
                    {
                        UserId = userId,
                        Date = date,
                        TargetItems = targetItems,
                        CompletedItems = completedItems
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取目标历史失败: {UserId}", userId);
                return new List<DailyGoal>();
            }
        }

        #endregion

        #region 目标管理

        public List<LearningGoal> GetGoals(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new List<LearningGoal>();

            try
            {
                var goals = LoadGoals(userId);
                if (goals.Count == 0)
                {
                    goals = GetDefaultGoals(userId);
                    SaveGoals(userId, goals);
                }
                return goals;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取目标设置失败: {UserId}", userId);
                return new List<LearningGoal>();
            }
        }

        public void UpdateGoal(string userId, LearningGoal goal)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("用户ID不能为空", nameof(userId));
            if (goal == null)
                throw new ArgumentNullException(nameof(goal));

            try
            {
                var goals = LoadGoals(userId);
                var existing = goals.FirstOrDefault(g => g.Type == goal.Type);

                if (existing != null)
                {
                    existing.TargetValue = goal.TargetValue;
                    existing.Unit = goal.Unit;
                    existing.Enabled = goal.Enabled;
                    existing.UpdatedAt = DateTime.Now;
                }
                else
                {
                    goal.UserId = userId;
                    goal.CreatedAt = DateTime.Now;
                    goals.Add(goal);
                }

                SaveGoals(userId, goals);
                _logger.LogInformation("用户 {UserId} 更新目标 {GoalType}: {TargetValue}", userId, goal.Type, goal.TargetValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新目标失败: {UserId}", userId);
                throw;
            }
        }

        public void SetGoalEnabled(string userId, GoalType type, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            try
            {
                var goals = LoadGoals(userId);
                var goal = goals.FirstOrDefault(g => g.Type == type);

                if (goal != null)
                {
                    goal.Enabled = enabled;
                    goal.UpdatedAt = DateTime.Now;
                    SaveGoals(userId, goals);
                    _logger.LogInformation("用户 {UserId} 目标 {GoalType} 启用状态: {Enabled}", userId, type, enabled);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置目标启用状态失败: {UserId}", userId);
            }
        }

        #endregion

        #region 进度追踪

        public List<GoalProgress> GetTodayProgressList(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new List<GoalProgress>();

            try
            {
                var goals = GetGoals(userId).Where(g => g.Enabled).ToList();
                var todayRecord = GetTodayRecord(userId);
                var profile = _persistenceService.LoadUserProfile(userId);

                var result = new List<GoalProgress>();

                foreach (var goal in goals)
                {
                    var currentValue = GetProgressValue(goal.Type, todayRecord, profile);
                    result.Add(new GoalProgress
                    {
                        Type = goal.Type,
                        Goal = goal,
                        CurrentValue = currentValue
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取今日进度列表失败: {UserId}", userId);
                return new List<GoalProgress>();
            }
        }

        public void UpdateStudyMinutes(string userId, int minutes)
        {
            if (string.IsNullOrWhiteSpace(userId) || minutes < 0)
                return;

            try
            {
                var record = GetTodayRecord(userId);
                record.Progress[GoalType.DailyStudyMinutes] = minutes;
                UpdateRecordCompletion(record, userId);
                SaveTodayRecord(userId, record);
                CheckGoalCompletion(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新学习时长进度失败: {UserId}", userId);
            }
        }

        public void IncrementStudyItems(string userId, int count = 1)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            try
            {
                var record = GetTodayRecord(userId);
                if (!record.Progress.ContainsKey(GoalType.DailyStudyItems))
                    record.Progress[GoalType.DailyStudyItems] = 0;
                record.Progress[GoalType.DailyStudyItems] += count;
                UpdateRecordCompletion(record, userId);
                SaveTodayRecord(userId, record);
                CheckGoalCompletion(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "增加学习数量进度失败: {UserId}", userId);
            }
        }

        public void IncrementReviewItems(string userId, int count = 1)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            try
            {
                var record = GetTodayRecord(userId);
                if (!record.Progress.ContainsKey(GoalType.DailyReviewItems))
                    record.Progress[GoalType.DailyReviewItems] = 0;
                record.Progress[GoalType.DailyReviewItems] += count;
                UpdateRecordCompletion(record, userId);
                SaveTodayRecord(userId, record);
                CheckGoalCompletion(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "增加复习数量进度失败: {UserId}", userId);
            }
        }

        public void CheckGoalCompletion(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            try
            {
                var progressList = GetTodayProgressList(userId);
                bool allCompleted = true;

                foreach (var progress in progressList)
                {
                    if (progress.IsCompleted)
                    {
                        var key = $"{DateTime.Today:yyyyMMdd}_{progress.Type}";
                        if (!_completedGoalsToday.Contains(key))
                        {
                            _completedGoalsToday.Add(key);
                            GoalCompleted?.Invoke(this, progress.Type);
                            _logger.LogInformation("用户 {UserId} 达成目标: {GoalType}", userId, progress.Type);
                        }
                    }
                    else
                    {
                        allCompleted = false;
                    }
                }

                if (allCompleted && progressList.Count > 0)
                {
                    var allKey = $"{DateTime.Today:yyyyMMdd}_all";
                    if (!_completedGoalsToday.Contains(allKey))
                    {
                        _completedGoalsToday.Add(allKey);
                        AllGoalsCompleted?.Invoke(this, EventArgs.Empty);
                        _logger.LogInformation("用户 {UserId} 达成今日全部目标", userId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查目标完成情况失败: {UserId}", userId);
            }
        }

        #endregion

        #region 历史记录与统计

        public List<DailyGoalRecord> GetDailyRecords(string userId, int days = 30)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new List<DailyGoalRecord>();

            try
            {
                var records = LoadRecords(userId);
                var goals = GetGoals(userId).Where(g => g.Enabled).ToList();

                var result = new List<DailyGoalRecord>();
                for (int i = days - 1; i >= 0; i--)
                {
                    var date = DateTime.Today.AddDays(-i);
                    var record = records.FirstOrDefault(r => r.Date.Date == date.Date);

                    if (record == null)
                    {
                        record = new DailyGoalRecord
                        {
                            UserId = userId,
                            Date = date
                        };
                        foreach (var goal in goals)
                        {
                            record.Progress[goal.Type] = 0;
                            record.Completed[goal.Type] = false;
                        }
                    }

                    result.Add(record);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取每日记录失败: {UserId}", userId);
                return new List<DailyGoalRecord>();
            }
        }

        public StreakInfo GetStreakInfo(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new StreakInfo();

            try
            {
                var records = LoadRecords(userId);
                var goals = GetGoals(userId).Where(g => g.Enabled).ToList();

                int currentStreak = 0;
                int longestStreak = 0;
                int tempStreak = 0;
                int totalCompleted = 0;
                int totalRecorded = 0;

                for (int i = 0; i < 365; i++)
                {
                    var date = DateTime.Today.AddDays(-i);
                    var record = records.FirstOrDefault(r => r.Date.Date == date.Date);

                    if (record != null)
                    {
                        totalRecorded++;
                        if (record.AllCompleted)
                        {
                            totalCompleted++;
                            tempStreak++;
                            if (tempStreak > longestStreak)
                                longestStreak = tempStreak;
                            if (i == 0 || currentStreak > 0)
                                currentStreak = tempStreak;
                        }
                        else
                        {
                            if (i == 0)
                                currentStreak = 0;
                            tempStreak = 0;
                        }
                    }
                    else
                    {
                        if (i == 0)
                            currentStreak = 0;
                        tempStreak = 0;
                    }
                }

                return new StreakInfo
                {
                    CurrentStreak = currentStreak,
                    LongestStreak = longestStreak,
                    TotalCompletedDays = totalCompleted,
                    TotalRecordedDays = Math.Max(totalRecorded, 1)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取连续达成统计失败: {UserId}", userId);
                return new StreakInfo();
            }
        }

        #endregion

        #region 私有方法

        private List<LearningGoal> LoadGoals(string userId)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var entities = db.LearningGoals
                    .Where(g => g.UserId == userId)
                    .ToList();

                return entities.Select(e => new LearningGoal
                {
                    UserId = e.UserId,
                    Type = ParseGoalType(e.GoalType),
                    TargetValue = e.TargetValue,
                    Unit = e.Unit ?? string.Empty,
                    Enabled = e.Enabled,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "加载目标设置失败，使用默认值: {UserId}", userId);
                return new List<LearningGoal>();
            }
        }

        private GoalType ParseGoalType(string goalType)
        {
            if (Enum.TryParse<GoalType>(goalType, out var type))
                return type;
            return GoalType.DailyStudyItems;
        }

        private void SaveGoals(string userId, List<LearningGoal> goals)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();

                foreach (var goal in goals)
                {
                    var existing = db.LearningGoals.FirstOrDefault(e => e.UserId == userId && e.GoalType == goal.Type.ToString());
                    if (existing != null)
                    {
                        existing.TargetValue = goal.TargetValue;
                        existing.Unit = goal.Unit;
                        existing.Enabled = goal.Enabled;
                        existing.UpdatedAt = goal.UpdatedAt ?? DateTime.Now;
                    }
                    else
                    {
                        db.LearningGoals.Add(new LearningGoalEntity
                        {
                            UserId = goal.UserId,
                            GoalType = goal.Type.ToString(),
                            TargetValue = goal.TargetValue,
                            Unit = goal.Unit,
                            Enabled = goal.Enabled,
                            CreatedAt = goal.CreatedAt,
                            UpdatedAt = goal.UpdatedAt ?? DateTime.Now
                        });
                    }
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存目标设置失败: {UserId}", userId);
                throw;
            }
        }

        private List<DailyGoalRecord> LoadRecords(string userId)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                return db.DailyGoalRecords
                    .Where(r => r.UserId == userId)
                    .Select(e => new DailyGoalRecord
                    {
                        UserId = e.UserId,
                        Date = e.Date,
                        Progress = !string.IsNullOrEmpty(e.ProgressJson)
                            ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<GoalType, int>>(e.ProgressJson) ?? new Dictionary<GoalType, int>()
                            : new Dictionary<GoalType, int>(),
                        Completed = !string.IsNullOrEmpty(e.CompletedJson)
                            ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<GoalType, bool>>(e.CompletedJson) ?? new Dictionary<GoalType, bool>()
                            : new Dictionary<GoalType, bool>(),
                        AllCompleted = e.AllCompleted
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "加载目标记录失败，使用空列表: {UserId}", userId);
                return new List<DailyGoalRecord>();
            }
        }

        private void SaveRecords(string userId, List<DailyGoalRecord> records)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();

                foreach (var record in records)
                {
                    var existing = db.DailyGoalRecords.FirstOrDefault(e => e.UserId == userId && e.Date.Date == record.Date.Date);
                    if (existing != null)
                    {
                        existing.ProgressJson = System.Text.Json.JsonSerializer.Serialize(record.Progress);
                        existing.CompletedJson = System.Text.Json.JsonSerializer.Serialize(record.Completed);
                        existing.AllCompleted = record.AllCompleted;
                    }
                    else
                    {
                        db.DailyGoalRecords.Add(new DailyGoalRecordEntity
                        {
                            UserId = record.UserId,
                            Date = record.Date,
                            ProgressJson = System.Text.Json.JsonSerializer.Serialize(record.Progress),
                            CompletedJson = System.Text.Json.JsonSerializer.Serialize(record.Completed),
                            AllCompleted = record.AllCompleted
                        });
                    }
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存目标记录失败: {UserId}", userId);
                throw;
            }
        }

        private DailyGoalRecord GetTodayRecord(string userId)
        {
            var records = LoadRecords(userId);
            var today = DateTime.Today;
            var record = records.FirstOrDefault(r => r.Date.Date == today.Date);

            if (record == null)
            {
                record = new DailyGoalRecord
                {
                    UserId = userId,
                    Date = today
                };
                records.Add(record);
                SaveRecords(userId, records);
            }

            return record;
        }

        private void SaveTodayRecord(string userId, DailyGoalRecord record)
        {
            var records = LoadRecords(userId);
            var today = DateTime.Today;
            var existing = records.FirstOrDefault(r => r.Date.Date == today.Date);

            if (existing != null)
            {
                existing.Progress = record.Progress;
                existing.Completed = record.Completed;
                existing.AllCompleted = record.AllCompleted;
            }
            else
            {
                records.Add(record);
            }

            if (records.Count > 365)
            {
                records = records.OrderByDescending(r => r.Date).Take(365).ToList();
            }

            SaveRecords(userId, records);
        }

        private int GetProgressValue(GoalType type, DailyGoalRecord? todayRecord, UserProfile? profile)
        {
            if (todayRecord?.Progress.TryGetValue(type, out var recordValue) == true)
                return recordValue;

            return type switch
            {
                GoalType.DailyStudyItems => profile?.TodayItemsStudied ?? 0,
                _ => 0
            };
        }

        private void UpdateRecordCompletion(DailyGoalRecord record, string userId)
        {
            var goals = GetGoals(userId).Where(g => g.Enabled).ToList();
            bool allCompleted = true;

            foreach (var goal in goals)
            {
                var hasProgress = record.Progress.TryGetValue(goal.Type, out var progress);
                var isCompleted = hasProgress && progress >= goal.TargetValue;
                record.Completed[goal.Type] = isCompleted;
                if (!isCompleted)
                    allCompleted = false;
            }

            record.AllCompleted = allCompleted && goals.Count > 0;
        }

        private static List<LearningGoal> GetDefaultGoals(string userId)
        {
            return new List<LearningGoal>
            {
                new()
                {
                    UserId = userId,
                    Type = GoalType.DailyStudyItems,
                    TargetValue = 20,
                    Unit = "个",
                    Enabled = true,
                    CreatedAt = DateTime.Now
                },
                new()
                {
                    UserId = userId,
                    Type = GoalType.DailyStudyMinutes,
                    TargetValue = 30,
                    Unit = "分钟",
                    Enabled = false,
                    CreatedAt = DateTime.Now
                },
                new()
                {
                    UserId = userId,
                    Type = GoalType.DailyReviewItems,
                    TargetValue = 10,
                    Unit = "个",
                    Enabled = false,
                    CreatedAt = DateTime.Now
                }
            };
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            UnsubscribeFromEvents();
        }

        #endregion
    }
}
