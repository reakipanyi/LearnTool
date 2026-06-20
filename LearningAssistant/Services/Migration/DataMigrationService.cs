using LearningAssistant.Common;
using LearningAssistant.Data.Database;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
    }

    /// <summary>
    /// 数据迁移服务，负责将 JSON 格式数据迁移到 SQLite 数据库
    /// </summary>
    public class DataMigrationService : IDataMigrationService
    {
        #region 字段与事件

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<DataMigrationService>? _logger;

        public event EventHandler<MigrationProgressEventArgs>? ProgressChanged;

        #endregion

        #region 构造函数

        public DataMigrationService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<DataMigrationService>? logger = null)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 检查是否需要迁移
        /// </summary>
        public bool NeedsMigration()
        {
            try
            {
                var needsMigration = false;

                // 检查用户数据
                if (JsonUserIds.Count > 0)
                {
                    using var db = _dbContextFactory.CreateDbContext();
                    var existingUserIds = new HashSet<string>(db.UserProfiles.Select(u => u.UserId));

                    foreach (var userId in JsonUserIds)
                    {
                        if (!existingUserIds.Contains(userId))
                        {
                            _logger?.LogInformation("Found user {UserId} not in SQLite, migration needed", userId);
                            needsMigration = true;
                            break;
                        }
                    }
                }

                // 检查间隔重复数据
                if (!needsMigration && File.Exists(SpacedRepetitionJsonPath))
                {
                    using var db = _dbContextFactory.CreateDbContext();
                    if (!db.SpacedRepetitionItems.Any())
                    {
                        _logger?.LogInformation("Spaced repetition data needs migration");
                        needsMigration = true;
                    }
                }

                // 检查会话数据
                if (!needsMigration && File.Exists(SessionJsonPath))
                {
                    using var db = _dbContextFactory.CreateDbContext();
                    if (!db.AppSessions.Any(s => s.SessionKey == "app_session"))
                    {
                        _logger?.LogInformation("Session data needs migration");
                        needsMigration = true;
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

        private List<string>? _jsonUserIds;
        private List<string> JsonUserIds
        {
            get
            {
                if (_jsonUserIds == null)
                {
                    try
                    {
                        var dir = AppPaths.UsersDir;
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

        private string SpacedRepetitionJsonPath => Path.Combine(AppPaths.DataDir, "spaced_repetition.json");
        private string SessionJsonPath => AppPaths.LastSessionPath;

        /// <summary>
        /// 执行迁移
        /// </summary>
        public MigrationResult PerformMigration()
        {
            var result = new MigrationResult();
            try
            {
                _logger?.LogInformation("Starting data migration...");
                ReportProgress(0, "开始迁移...");

                // 1. 迁移用户数据
                var userIds = JsonUserIds;
                result.TotalUsers = userIds.Count;

                for (int i = 0; i < userIds.Count; i++)
                {
                    var userId = userIds[i];
                    var progress = result.TotalUsers > 0 ? (i + 1) * 60 / result.TotalUsers : 60;
                    ReportProgress(progress, $"正在迁移用户: {userId}");

                    try
                    {
                        if (MigrateUser(userId))
                        {
                            result.SuccessfulMigrations++;
                        }
                        else
                        {
                            result.FailedMigrations++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to migrate user {UserId}", userId);
                        result.FailedMigrations++;
                        result.Errors.Add($"迁移用户 {userId} 时出错: {ex.Message}");
                    }
                }

                // 2. 迁移间隔重复数据
                ReportProgress(70, "正在迁移间隔重复数据...");
                try
                {
                    if (MigrateSpacedRepetitionData())
                    {
                        result.SpacedRepetitionMigrated = true;
                        _logger?.LogInformation("Spaced repetition data migrated successfully");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to migrate spaced repetition data");
                    result.Errors.Add($"迁移间隔重复数据失败: {ex.Message}");
                }

                // 3. 迁移会话数据
                ReportProgress(90, "正在迁移会话数据...");
                try
                {
                    if (MigrateSessionData())
                    {
                        result.SessionMigrated = true;
                        _logger?.LogInformation("Session data migrated successfully");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to migrate session data");
                    result.Errors.Add($"迁移会话数据失败: {ex.Message}");
                }

                ReportProgress(100, "迁移完成!");
                result.Success = result.FailedMigrations == 0;
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

        #region 私有迁移方法

        /// <summary>
        /// 迁移单个用户
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

            var userEntity = profile.ToEntity();
            db.UserProfiles.Add(userEntity);

            foreach (var categoryProgress in profile.LearningProgress.CategoryProgresses.Values)
            {
                userEntity.CategoryProgresses.Add(categoryProgress.ToEntity(userId));
            }

            db.SaveChanges();
            _logger?.LogInformation("Successfully migrated user {UserId}", userId);
            return true;
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
            _logger?.LogInformation("Migrated {Count} spaced repetition items", totalItems);
            return true;
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

            var sessionJson = Common.JsonHelper.Serialize(session);
            db.AppSessions.Add(new AppSessionEntity
            {
                SessionKey = "app_session",
                SessionDataJson = sessionJson,
                LastAccessTime = DateTime.Now,
                UpdatedAt = DateTime.Now
            });

            db.SaveChanges();
            _logger?.LogInformation("Session data migrated successfully");
            return true;
        }

        /// <summary>
        /// 从 JSON 文件加载用户配置
        /// </summary>
        private UserProfile? LoadUserProfileFromJson(string userId)
        {
            try
            {
                var path = AppPaths.GetUserProgressPath(userId);
                return Common.JsonHelper.LoadFromFile<UserProfile>(path);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load user profile from JSON: {UserId}", userId);
                return null;
            }
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
        public List<string> Errors { get; set; } = new List<string>();
    }
}
