using LearningAssistant.Abstractions;
using LearningAssistant.Common;
using LearningAssistant.Data.Database;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace LearningAssistant.Services.Migration
{
    /// <summary>
    /// 数据迁移服务接口
    /// </summary>
    public interface IDataMigrationService
    {
        event EventHandler<MigrationProgressEventArgs>? ProgressChanged;

        bool NeedsMigration();
        MigrationResult PerformMigration();
        bool BackupBeforeMigration();
        bool VerifyMigrationResult(MigrationResult result);
    }

    /// <summary>
    /// 迁移检查点状态。
    /// </summary>
    internal static class MigrationStepStatus
    {
        public const string Pending = "Pending";
        public const string Running = "Running";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
    }

    /// <summary>
    /// 数据迁移服务，负责将 JSON 格式数据迁移到 SQLite 数据库
    /// </summary>
    public class DataMigrationService : IDataMigrationService
    {
        #region 字段与事件

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<DataMigrationService>? _logger;
        private readonly IAppPaths _appPaths;

        /// <summary>迁移步骤常量（用于检查点）</summary>
        private static class MigrationSteps
        {
            public const string Backup = "Backup";
            public const string SpacedRepetition = "SpacedRepetition";
            public const string Session = "Session";
            public const string LearningItemStates = "LearningItemStates";
            public const string ReminderRepeatDays = "ReminderRepeatDays";
            public const string Analytics = "Analytics";
            public const string Verification = "Verification";
            public static string User(string userId) => $"User:{userId}";
            public static string AnalyticsUser(string userId) => $"Analytics:{userId}";
        }

        public event EventHandler<MigrationProgressEventArgs>? ProgressChanged;

        #endregion

        #region 构造函数

        public DataMigrationService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IAppPaths appPaths,
            ILogger<DataMigrationService>? logger = null)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _appPaths = appPaths ?? throw new ArgumentNullException(nameof(appPaths));
            _logger = logger;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 检查指定迁移步骤是否已经完成（断点续传）。
        /// </summary>
        private bool IsStepCompleted(string stepId)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var step = db.MigrationCheckpoints.FirstOrDefault(s => s.StepId == stepId);
                return step != null && step.Status == MigrationStepStatus.Completed;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to read checkpoint for step {StepId}", stepId);
                return false;
            }
        }

        /// <summary>
        /// 更新迁移步骤状态。失败时不抛异常（记录日志即可），保证检查点写入为 best-effort。
        /// </summary>
        private void MarkStepStatus(string stepId, string status, object? detail = null)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var step = db.MigrationCheckpoints.FirstOrDefault(s => s.StepId == stepId);
                string detailJson = detail != null ? JsonSerializer.Serialize(detail) : (step?.DetailJson ?? "{}");
                if (step == null)
                {
                    db.MigrationCheckpoints.Add(new MigrationCheckpointEntity
                    {
                        StepId = stepId,
                        Status = status,
                        DetailJson = detailJson,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                }
                else
                {
                    step.Status = status;
                    step.DetailJson = detailJson;
                    step.UpdatedAt = DateTime.Now;
                }
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to mark checkpoint {StepId}={Status}", stepId, status);
            }
        }

        /// <summary>
        /// 检查是否需要迁移
        /// </summary>
        public bool NeedsMigration()
        {
            try
            {
                var needsMigration = false;

                // 有任何用户未完成迁移则需要迁移
                foreach (var userId in JsonUserIds)
                {
                    if (IsStepCompleted(MigrationSteps.User(userId)))
                        continue;
                    using var db = _dbContextFactory.CreateDbContext();
                    if (!db.UserProfiles.Any(u => u.UserId == userId))
                    {
                        _logger?.LogInformation("Found user {UserId} not in SQLite, migration needed", userId);
                        needsMigration = true;
                        break;
                    }
                }

                if (!needsMigration && File.Exists(SpacedRepetitionJsonPath))
                {
                    if (!IsStepCompleted(MigrationSteps.SpacedRepetition))
                    {
                        using var db = _dbContextFactory.CreateDbContext();
                        if (!db.SpacedRepetitionItems.Any())
                        {
                            _logger?.LogInformation("Spaced repetition data needs migration");
                            needsMigration = true;
                        }
                    }
                }

                if (!needsMigration && File.Exists(SessionJsonPath))
                {
                    if (!IsStepCompleted(MigrationSteps.Session))
                    {
                        using var db = _dbContextFactory.CreateDbContext();
                        if (!db.AppSessions.Any(s => s.SessionKey == "app_session"))
                        {
                            _logger?.LogInformation("Session data needs migration");
                            needsMigration = true;
                        }
                    }
                }

                // 任一用户存在待迁移的学习分析 JSON（未完成 Analytics:user 检查点）则需迁移
                if (!needsMigration)
                {
                    foreach (var userId in JsonUserIds)
                    {
                        if (IsStepCompleted(MigrationSteps.AnalyticsUser(userId)))
                            continue;
                        if (File.Exists(AnalyticsJsonPath(userId)))
                        {
                            _logger?.LogInformation("Analytics data for user {UserId} needs migration", userId);
                            needsMigration = true;
                            break;
                        }
                    }
                }

                return needsMigration;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to check migration status");
                return false;
            }
        }

        /// <summary>
        /// 迁移前备份
        /// </summary>
        public bool BackupBeforeMigration()
        {
            try
            {
                var backupDir = Path.Combine(_appPaths.DataDir, "MigrationBackups", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                Directory.CreateDirectory(backupDir);

                _logger?.LogInformation("Starting backup to {BackupDir}", backupDir);
                ReportProgress(0, "备份中...");

                var backupSuccess = true;

                if (Directory.Exists(_appPaths.UsersDir))
                {
                    var userBackupDir = Path.Combine(backupDir, "Users");
                    Directory.CreateDirectory(userBackupDir);

                    foreach (var file in Directory.GetFiles(_appPaths.UsersDir, "*.json"))
                    {
                        try
                        {
                            var destFile = Path.Combine(userBackupDir, Path.GetFileName(file));
                            File.Copy(file, destFile, true);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Failed to backup user file: {File}", file);
                            backupSuccess = false;
                        }
                    }
                }

                if (File.Exists(SpacedRepetitionJsonPath))
                {
                    try
                    {
                        var destFile = Path.Combine(backupDir, "spaced_repetition.json");
                        File.Copy(SpacedRepetitionJsonPath, destFile, true);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to backup spaced repetition data");
                        backupSuccess = false;
                    }
                }

                if (File.Exists(SessionJsonPath))
                {
                    try
                    {
                        var destFile = Path.Combine(backupDir, "session.json");
                        File.Copy(SessionJsonPath, destFile, true);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to backup session data");
                        backupSuccess = false;
                    }
                }

                _logger?.LogInformation("Backup completed to {BackupDir}, success: {Success}", backupDir, backupSuccess);
                return backupSuccess;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Backup failed");
                return false;
            }
        }

        /// <summary>
        /// 验证迁移结果
        /// </summary>
        public bool VerifyMigrationResult(MigrationResult result)
        {
            if (!result.Success)
                return false;

            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var verificationErrors = new List<string>();

                var jsonUserIds = new HashSet<string>(JsonUserIds);
                var dbUserIds = new HashSet<string>(db.UserProfiles.Select(u => u.UserId));

                foreach (var userId in jsonUserIds)
                {
                    if (!dbUserIds.Contains(userId))
                    {
                        verificationErrors.Add($"用户 {userId} 迁移失败");
                    }
                }

                if (result.SpacedRepetitionMigrated)
                {
                    if (!db.SpacedRepetitionItems.Any())
                    {
                        verificationErrors.Add("间隔重复数据迁移验证失败");
                    }
                }

                if (result.SessionMigrated)
                {
                    if (!db.AppSessions.Any(s => s.SessionKey == "app_session"))
                    {
                        verificationErrors.Add("会话数据迁移验证失败");
                    }
                }

                if (verificationErrors.Count > 0)
                {
                    _logger?.LogError("Migration verification failed: {Errors}", string.Join(", ", verificationErrors));
                    result.Errors.AddRange(verificationErrors);
                    return false;
                }

                result.VerificationPassed = true;
                _logger?.LogInformation("Migration verification passed");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Migration verification failed with exception");
                return false;
            }
        }

        private List<string>? _jsonUserIds;
        private List<string> JsonUserIds
        {
            get
            {
                if (_jsonUserIds == null)
                {
                    try
                    {
                        var dir = _appPaths.UsersDir;
                        if (Directory.Exists(dir))
                        {
                            _jsonUserIds = Directory.EnumerateFiles(dir, "*.json")
                                .Select(f => Path.GetFileNameWithoutExtension(f))
                                .ToList();
                        }
                        else
                        {
                            _jsonUserIds = new List<string>();
                        }
                    }
                    catch
                    {
                        _jsonUserIds = new List<string>();
                    }
                }
                return _jsonUserIds;
            }
        }

        private string SpacedRepetitionJsonPath => Path.Combine(_appPaths.DataDir, "spaced_repetition.json");
        private string SessionJsonPath => _appPaths.LastSessionPath;
        private string AnalyticsJsonPath(string userId) => _appPaths.GetUserAnalyticsPath(userId);

        /// <summary>
        /// 执行迁移（支持断电/崩溃后断点续传）
        /// </summary>
        public MigrationResult PerformMigration()
        {
            var result = new MigrationResult();
            try
            {
                _logger?.LogInformation("Starting data migration...");

                // ---- 备份步骤 ----
                ReportProgress(5, "开始备份...");
                var backupDir = Path.Combine(_appPaths.DataDir, "MigrationBackups", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                result.BackupPath = backupDir;

                if (!IsStepCompleted(MigrationSteps.Backup))
                {
                    MarkStepStatus(MigrationSteps.Backup, MigrationStepStatus.Running, new { backupDir });
                    if (!BackupBeforeMigration())
                    {
                        _logger?.LogWarning("Backup completed with warnings, proceeding with migration");
                        result.Errors.Add("备份过程中出现警告，请检查备份文件");
                    }
                    MarkStepStatus(MigrationSteps.Backup, MigrationStepStatus.Completed, new { backupDir });
                }
                else
                {
                    _logger?.LogInformation("Backup step already completed, skipping");
                }

                ReportProgress(10, "开始迁移...");

                var userIds = JsonUserIds;
                result.TotalUsers = userIds.Count;

                // ---- 用户迁移步骤（每个用户独立检查点）----
                for (int i = 0; i < userIds.Count; i++)
                {
                    var userId = userIds[i];
                    var stepId = MigrationSteps.User(userId);
                    var progress = 10 + (result.TotalUsers > 0 ? (i + 1) * 50 / result.TotalUsers : 50);
                    ReportProgress(progress, $"正在迁移用户: {userId}");

                    if (IsStepCompleted(stepId))
                    {
                        result.SuccessfulMigrations++;
                        continue;
                    }

                    try
                    {
                        MarkStepStatus(stepId, MigrationStepStatus.Running);
                        if (MigrateUser(userId))
                        {
                            result.SuccessfulMigrations++;
                            MarkStepStatus(stepId, MigrationStepStatus.Completed);
                        }
                        else
                        {
                            result.FailedMigrations++;
                            MarkStepStatus(stepId, MigrationStepStatus.Failed,
                                new { error = "MigrateUser returned false" });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to migrate user {UserId}", userId);
                        result.FailedMigrations++;
                        result.Errors.Add($"迁移用户 {userId} 时出错: {ex.Message}");
                        MarkStepStatus(stepId, MigrationStepStatus.Failed, new { error = ex.Message });
                    }
                }

                // ---- 间隔重复数据迁移 ----
                ReportProgress(65, "正在迁移间隔重复数据...");
                if (!IsStepCompleted(MigrationSteps.SpacedRepetition))
                {
                    try
                    {
                        MarkStepStatus(MigrationSteps.SpacedRepetition, MigrationStepStatus.Running);
                        if (MigrateSpacedRepetitionData())
                        {
                            result.SpacedRepetitionMigrated = true;
                            MarkStepStatus(MigrationSteps.SpacedRepetition, MigrationStepStatus.Completed);
                            _logger?.LogInformation("Spaced repetition data migrated successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to migrate spaced repetition data");
                        result.Errors.Add($"迁移间隔重复数据失败: {ex.Message}");
                        MarkStepStatus(MigrationSteps.SpacedRepetition, MigrationStepStatus.Failed, new { error = ex.Message });
                    }
                }
                else
                {
                    result.SpacedRepetitionMigrated = true;
                }

                // ---- 会话数据迁移 ----
                ReportProgress(75, "正在迁移会话数据...");
                if (!IsStepCompleted(MigrationSteps.Session))
                {
                    try
                    {
                        MarkStepStatus(MigrationSteps.Session, MigrationStepStatus.Running);
                        if (MigrateSessionData())
                        {
                            result.SessionMigrated = true;
                            MarkStepStatus(MigrationSteps.Session, MigrationStepStatus.Completed);
                            _logger?.LogInformation("Session data migrated successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to migrate session data");
                        result.Errors.Add($"迁移会话数据失败: {ex.Message}");
                        MarkStepStatus(MigrationSteps.Session, MigrationStepStatus.Failed, new { error = ex.Message });
                    }
                }
                else
                {
                    result.SessionMigrated = true;
                }

                // ---- 学习项状态迁移 ----
                ReportProgress(80, "正在迁移学习项状态数据...");
                if (!IsStepCompleted(MigrationSteps.LearningItemStates))
                {
                    try
                    {
                        MarkStepStatus(MigrationSteps.LearningItemStates, MigrationStepStatus.Running);
                        var migrated = MigrateLearningItemStates();
                        MarkStepStatus(MigrationSteps.LearningItemStates, MigrationStepStatus.Completed, new { migrated });
                        _logger?.LogInformation("Learning item states migrated successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to migrate learning item states");
                        result.Errors.Add($"迁移学习项状态数据失败: {ex.Message}");
                        MarkStepStatus(MigrationSteps.LearningItemStates, MigrationStepStatus.Failed, new { error = ex.Message });
                    }
                }

                // ---- 提醒重复日期迁移 ----
                ReportProgress(90, "正在迁移提醒重复日期数据...");
                if (!IsStepCompleted(MigrationSteps.ReminderRepeatDays))
                {
                    try
                    {
                        MarkStepStatus(MigrationSteps.ReminderRepeatDays, MigrationStepStatus.Running);
                        var migrated = MigrateReminderRepeatDays();
                        MarkStepStatus(MigrationSteps.ReminderRepeatDays, MigrationStepStatus.Completed, new { migrated });
                        _logger?.LogInformation("Reminder repeat days migrated successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to migrate reminder repeat days");
                        result.Errors.Add($"迁移提醒重复日期数据失败: {ex.Message}");
                        MarkStepStatus(MigrationSteps.ReminderRepeatDays, MigrationStepStatus.Failed, new { error = ex.Message });
                    }
                }

                // ---- 学习分析数据迁移（JSON analytics → DailyRollup 物化表）----
                ReportProgress(87, "正在迁移学习分析数据...");
                if (!IsStepCompleted(MigrationSteps.Analytics))
                {
                    try
                    {
                        MarkStepStatus(MigrationSteps.Analytics, MigrationStepStatus.Running);
                        var analytics = MigrateAnalyticsData(result);
                        MarkStepStatus(MigrationSteps.Analytics, MigrationStepStatus.Completed,
                            new { users = analytics.users, entries = analytics.entries });
                        _logger?.LogInformation("Analytics data migrated: {Users} users, {Entries} entries",
                            analytics.users, analytics.entries);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to migrate analytics data");
                        result.Errors.Add($"迁移学习分析数据失败: {ex.Message}");
                        MarkStepStatus(MigrationSteps.Analytics, MigrationStepStatus.Failed, new { error = ex.Message });
                    }
                }

                // ---- 验证迁移 ----
                ReportProgress(95, "正在验证迁移结果...");
                bool verificationPassed;
                if (!IsStepCompleted(MigrationSteps.Verification))
                {
                    MarkStepStatus(MigrationSteps.Verification, MigrationStepStatus.Running);
                    verificationPassed = VerifyMigrationResult(result);
                    if (!verificationPassed)
                    {
                        _logger?.LogError("Migration verification failed");
                        result.Errors.Add("迁移验证失败，请检查数据完整性");
                        MarkStepStatus(MigrationSteps.Verification, MigrationStepStatus.Failed,
                            new { errors = result.Errors });
                    }
                    else
                    {
                        MarkStepStatus(MigrationSteps.Verification, MigrationStepStatus.Completed);
                    }
                }
                else
                {
                    verificationPassed = true;
                    result.VerificationPassed = true;
                }

                ReportProgress(100, verificationPassed ? "迁移完成!" : "迁移完成(部分数据可能需要检查)");
                result.Success = result.FailedMigrations == 0 && verificationPassed;
                _logger?.LogInformation("Migration completed: {SuccessCount} successful, {FailedCount} failed",
                    result.SuccessfulMigrations, result.FailedMigrations);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Migration failed");
                result.Success = false;
                result.Errors.Add($"迁移失败: {ex.Message}");
            }

            return result;
        }
        #endregion
        #region 私有迁移方法

        /// <summary>
        /// 迁移单个用户（使用事务保护）
        /// </summary>
        private bool MigrateUser(string userId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

            using var db = _dbContextFactory.CreateDbContext();

            if (db.UserProfiles.Any(u => u.UserId == userId))
            {
                _logger?.LogInformation("User {UserId} already exists, skipping", userId);
                return true;
            }

            var profile = LoadUserProfileFromJson(userId);
            if (profile == null)
            {
                _logger?.LogWarning("Failed to load user {UserId} from JSON", userId);
                return false;
            }

            try
            {
                using var transaction = db.Database.BeginTransaction();

                var userEntity = profile.ToEntity();
                db.UserProfiles.Add(userEntity);

                foreach (var categoryProgress in profile.LearningProgress.CategoryProgresses.Values)
                {
                    userEntity.CategoryProgresses.Add(categoryProgress.ToEntity(userId));
                }

                db.SaveChanges();
                transaction.Commit();

                _logger?.LogInformation("Successfully migrated user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to migrate user {UserId} within transaction", userId);
                return false;
            }
        }

        /// <summary>
        /// 迁移间隔重复数据
        /// </summary>
        private bool MigrateSpacedRepetitionData()
        {
            if (!File.Exists(SpacedRepetitionJsonPath))
            {
                _logger?.LogInformation("No spaced repetition JSON file found, skipping");
                return false;
            }

            using var db = _dbContextFactory.CreateDbContext();

            if (db.SpacedRepetitionItems.Any())
            {
                _logger?.LogInformation("Spaced repetition data already exists, skipping");
                return true;
            }

            var json = File.ReadAllText(SpacedRepetitionJsonPath);
            var userItems = Common.JsonHelper.Deserialize<Dictionary<string, List<ReviewItem>>>(json);

            if (userItems == null || userItems.Count == 0)
            {
                _logger?.LogInformation("No spaced repetition data to migrate");
                return false;
            }

            try
            {
                using var transaction = db.Database.BeginTransaction();
                var totalItems = 0;

                foreach (var kvp in userItems)
                {
                    foreach (var item in kvp.Value)
                    {
                        db.SpacedRepetitionItems.Add(item.ToEntity());
                        totalItems++;
                    }
                }

                db.SaveChanges();
                transaction.Commit();

                _logger?.LogInformation("Migrated {count} spaced repetition items", totalItems);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to migrate spaced repetition data within transaction");
                return false;
            }
        }

        /// <summary>
        /// 迁移会话数据
        /// </summary>
        private bool MigrateSessionData()
        {
            if (!File.Exists(SessionJsonPath))
            {
                _logger?.LogInformation("No session JSON file found, skipping");
                return false;
            }

            using var db = _dbContextFactory.CreateDbContext();

            if (db.AppSessions.Any(s => s.SessionKey == "app_session"))
            {
                _logger?.LogInformation("Session data already exists, skipping");
                return true;
            }

            var session = Common.JsonHelper.LoadFromFile<SessionData>(SessionJsonPath);
            if (session == null)
            {
                _logger?.LogWarning("Failed to load session data from JSON");
                return false;
            }

            try
            {
                using var transaction = db.Database.BeginTransaction();

                var sessionJson = Common.JsonHelper.Serialize(session);
                db.AppSessions.Add(new AppSessionEntity
                {
                    SessionKey = "app_session",
                    SessionDataJson = sessionJson,
                    LastAccessTime = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });

                db.SaveChanges();
                transaction.Commit();

                _logger?.LogInformation("Session data migrated successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to migrate session data within transaction");
                return false;
            }
        }

        /// <summary>
        /// 从 JSON 文件加载用户配置
        /// </summary>
        private UserProfile? LoadUserProfileFromJson(string userId)
        {
            try
            {
                var path = _appPaths.GetUserProgressPath(userId);
                return Common.JsonHelper.LoadFromFile<UserProfile>(path);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load user profile from JSON: {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// 迁移学习项状态数据（从 CategoryProgress 的 KnownItemsJson/UnknownItemsJson）
        /// </summary>
        private bool MigrateLearningItemStates()
        {
            using var db = _dbContextFactory.CreateDbContext();

            if (db.LearningItemStates.Any())
            {
                _logger?.LogInformation("Learning item states already exist, skipping");
                return true;
            }

            var categoryProgresses = db.CategoryProgresses.ToList();
            if (categoryProgresses.Count == 0)
            {
                _logger?.LogInformation("No category progress data to migrate");
                return false;
            }

            try
            {
                using var transaction = db.Database.BeginTransaction();
                var totalStates = 0;

                foreach (var cp in categoryProgresses)
                {
                    try
                    {
                        var knownItems = JsonHelper.Deserialize<List<string>>(cp.KnownItemsJson) ?? new List<string>();
                        var unknownItems = JsonHelper.Deserialize<List<string>>(cp.UnknownItemsJson) ?? new List<string>();

                        foreach (var content in knownItems)
                        {
                            if (!string.IsNullOrWhiteSpace(content))
                            {
                                db.LearningItemStates.Add(new LearningItemStateEntity
                                {
                                    UserId = cp.UserId,
                                    CategoryName = cp.CategoryName,
                                    Content = content,
                                    IsKnown = true,
                                    CreatedAt = DateTime.Now,
                                    UpdatedAt = DateTime.Now
                                });
                                totalStates++;
                            }
                        }

                        foreach (var content in unknownItems)
                        {
                            if (!string.IsNullOrWhiteSpace(content))
                            {
                                db.LearningItemStates.Add(new LearningItemStateEntity
                                {
                                    UserId = cp.UserId,
                                    CategoryName = cp.CategoryName,
                                    Content = content,
                                    IsKnown = false,
                                    CreatedAt = DateTime.Now,
                                    UpdatedAt = DateTime.Now
                                });
                                totalStates++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to migrate learning item states for category {Category}", cp.CategoryName);
                    }
                }

                if (totalStates > 0)
                {
                    db.SaveChanges();
                    transaction.Commit();
                    _logger?.LogInformation("Migrated {count} learning item states", totalStates);
                }

                return totalStates > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to migrate learning item states within transaction");
                return false;
            }
        }

        /// <summary>
        /// 迁移提醒重复日期数据（从 Reminder 的 RepeatDaysJson）
        /// </summary>
        private bool MigrateReminderRepeatDays()
        {
            using var db = _dbContextFactory.CreateDbContext();

            if (db.ReminderRepeatDays.Any())
            {
                _logger?.LogInformation("Reminder repeat days already exist, skipping");
                return true;
            }

            var reminders = db.Reminders.Where(r => !string.IsNullOrEmpty(r.RepeatDaysJson)).ToList();
            if (reminders.Count == 0)
            {
                _logger?.LogInformation("No reminder repeat days data to migrate");
                return false;
            }

            try
            {
                using var transaction = db.Database.BeginTransaction();
                var totalDays = 0;

                foreach (var reminder in reminders)
                {
                    try
                    {
                        var repeatDays = JsonHelper.Deserialize<List<DayOfWeek>>(reminder.RepeatDaysJson);
                        if (repeatDays != null)
                        {
                            foreach (var day in repeatDays)
                            {
                                db.ReminderRepeatDays.Add(new ReminderRepeatDayEntity
                                {
                                    ReminderId = reminder.Id,
                                    DayOfWeek = (int)day,
                                    CreatedAt = DateTime.Now
                                });
                                totalDays++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to migrate repeat days for reminder {Id}", reminder.Id);
                    }
                }

                if (totalDays > 0)
                {
                    db.SaveChanges();
                    transaction.Commit();
                    _logger?.LogInformation("Migrated {count} reminder repeat days", totalDays);
                }

                return totalDays > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to migrate reminder repeat days within transaction");
                return false;
            }
        }

        /// <summary>
        /// 迁移学习分析数据：每用户 JSON UserAnalyticsData 的 DailyRecords → DailyRollup 物化表。
        /// 幂等：按 用户+日期 唯一 upsert；若当日已存在则补充增量，避免覆盖实时写入的数据。
        /// 每用户以独立检查点（Analytics:userId）保证断点续传与可重跑不重复。
        /// </summary>
        private (int users, int entries) MigrateAnalyticsData(MigrationResult result)
        {
            int totalUsers = 0, totalEntries = 0;

            foreach (var userId in JsonUserIds)
            {
                var stepId = MigrationSteps.AnalyticsUser(userId);
                if (IsStepCompleted(stepId))
                    continue;

                var path = AnalyticsJsonPath(userId);
                if (!File.Exists(path))
                {
                    MarkStepStatus(stepId, MigrationStepStatus.Completed, new { skipped = "no-analytics-file" });
                    continue;
                }

                try
                {
                    MarkStepStatus(stepId, MigrationStepStatus.Running);
                    var userData = JsonHelper.LoadFromFile<UserAnalyticsData>(path);
                    if (userData?.DailyRecords == null || userData.DailyRecords.Count == 0)
                    {
                        MarkStepStatus(stepId, MigrationStepStatus.Completed, new { entries = 0 });
                        continue;
                    }

                    using var db = _dbContextFactory.CreateDbContext();
                    using var tx = db.Database.BeginTransaction();
                    var entries = 0;

                    foreach (var daily in userData.DailyRecords.Values.OrderBy(d => d.Date))
                    {
                        var date = daily.Date.Date;
                        var rollup = db.DailyRollups.FirstOrDefault(r => r.UserId == userId && r.Date.Date == date);
                        if (rollup == null)
                        {
                            rollup = new DailyRollupEntity { UserId = userId, Date = date };
                            db.DailyRollups.Add(rollup);
                        }

                        rollup.TimeSpentMinutes += Math.Max(0, daily.TotalMinutes);
                        var items = Math.Max(daily.TotalItems, daily.ItemsLearned + daily.ItemsReviewed);
                        rollup.ItemsStudied += Math.Max(0, items);
                        rollup.CorrectCount += Math.Max(0, daily.CorrectCount);
                        rollup.WrongCount += Math.Max(0, daily.WrongCount);
                        rollup.Accuracy = Models.Learning.StatsAggregation.ComputeAccuracy(rollup.CorrectCount, rollup.WrongCount);

                        if (daily.CategoryBreakdown != null && daily.CategoryBreakdown.Count > 0)
                        {
                            var top = daily.CategoryBreakdown.OrderByDescending(kv => kv.Value).First().Key;
                            if (!string.IsNullOrEmpty(top)) rollup.TopCategory = top;
                        }
                        rollup.Version++;
                        rollup.UpdatedAt = DateTime.Now;
                        entries++;
                    }

                    db.SaveChanges();
                    tx.Commit();

                    totalUsers++;
                    totalEntries += entries;
                    MarkStepStatus(stepId, MigrationStepStatus.Completed, new { entries });
                    _logger?.LogInformation("Migrated analytics for user {UserId}: {Entries} daily entries", userId, entries);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to migrate analytics for user {UserId}", userId);
                    result.Errors.Add($"迁移用户 {userId} 学习分析数据失败: {ex.Message}");
                    MarkStepStatus(stepId, MigrationStepStatus.Failed, new { error = ex.Message });
                }
            }

            return (totalUsers, totalEntries);
        }

        private void ReportProgress(int percentage, string message)
        {
            ProgressChanged?.Invoke(this, new MigrationProgressEventArgs
            {
                Percentage = percentage,
                Message = message
            });
        }

        #endregion
    }

    /// <summary>
    /// 迁移进度事件参数
    /// </summary>
    public class MigrationProgressEventArgs : EventArgs
    {
        public int Percentage { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 迁移结果
    /// </summary>
    public class MigrationResult
    {
        public bool Success { get; set; }
        public int TotalUsers { get; set; }
        public int SuccessfulMigrations { get; set; }
        public int FailedMigrations { get; set; }
        public bool SpacedRepetitionMigrated { get; set; }
        public bool SessionMigrated { get; set; }
        public bool VerificationPassed { get; set; }
        public string BackupPath { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new List<string>();
    }
}