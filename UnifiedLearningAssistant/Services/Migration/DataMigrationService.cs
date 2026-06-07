using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LearningAssistant.Data.Database;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Persistence;

namespace LearningAssistant.Services.Migration
{
    /// <summary>
    /// 数据迁移服务，负责将 JSON 格式数据迁移到 SQLite 数据库
    /// </summary>
    public class DataMigrationService
    {
        private readonly IDataPersistenceService _jsonDataService;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<DataMigrationService>? _logger;

        public event EventHandler<MigrationProgressEventArgs>? ProgressChanged;

        public DataMigrationService(
            IDataPersistenceService jsonDataService,
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<DataMigrationService>? logger = null)
        {
            _jsonDataService = jsonDataService ?? throw new ArgumentNullException(nameof(jsonDataService));
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger;
        }

        /// <summary>
        /// 检查是否需要迁移
        /// </summary>
        public bool NeedsMigration()
        {
            try
            {
                // 检查是否存在 JSON 用户数据
                var userIds = _jsonDataService.GetUserIds();
                if (userIds.Count == 0)
                    return false;

                // 检查是否有未迁移的用户
                using var db = _dbContextFactory.CreateDbContext();
                var existingUserIds = new HashSet<string>(db.UserProfiles.Select(u => u.UserId));
                
                // 检查是否有任何 JSON 用户不在 SQLite 中
                foreach (var userId in userIds)
                {
                    if (!existingUserIds.Contains(userId))
                    {
                        _logger?.LogInformation("Found user {UserId} not in SQLite, migration needed", userId);
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to check migration status");
                return false;
            }
        }

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

                var userIds = _jsonDataService.GetUserIds();
                result.TotalUsers = userIds.Count;

                for (int i = 0; i < userIds.Count; i++)
                {
                    var userId = userIds[i];
                    // 安全计算进度百分比，避免除以零
                    var progress = result.TotalUsers > 0 ? (i + 1) * 100 / result.TotalUsers : 100;
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

        /// <summary>
        /// 迁移单个用户
        /// </summary>
        private bool MigrateUser(string userId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));
            
            using var db = _dbContextFactory.CreateDbContext();
            
            // 检查用户是否已存在
            if (db.UserProfiles.Any(u => u.UserId == userId))
            {
                _logger?.LogInformation("User {UserId} already exists, skipping", userId);
                return true; // 视为成功
            }

            // 从 JSON 加载用户
            var profile = _jsonDataService.LoadUserProfile(userId);
            if (profile == null)
            {
                _logger?.LogWarning("Failed to load user {UserId} from JSON", userId);
                return false;
            }

            // 保存到 SQLite
            var userEntity = profile.ToEntity();
            db.UserProfiles.Add(userEntity);

            // 添加分类进度
            foreach (var categoryProgress in profile.LearningProgress.CategoryProgresses.Values)
            {
                userEntity.CategoryProgresses.Add(categoryProgress.ToEntity(userId));
            }

            db.SaveChanges();
            _logger?.LogInformation("Successfully migrated user {UserId}", userId);
            return true;
        }

        private void ReportProgress(int percentage, string message)
        {
            ProgressChanged?.Invoke(this, new MigrationProgressEventArgs
            {
                Percentage = percentage,
                Message = message
            });
        }
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
        public List<string> Errors { get; set; } = new List<string>();
    }
}
