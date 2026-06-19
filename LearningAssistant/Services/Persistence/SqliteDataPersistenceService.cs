using LearningAssistant.Common;
using LearningAssistant.Data.Database;
using LearningAssistant.Models.Config;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Persistence
{
    /// <summary>
    /// SQLite 版本的数据持久化服务
    /// </summary>
    public class SqliteDataPersistenceService : IDataPersistenceService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cacheService;
        private readonly ILogger<SqliteDataPersistenceService>? _logger;

        public SqliteDataPersistenceService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IConfiguration configuration,
            ICacheService cacheService,
            ILogger<SqliteDataPersistenceService>? logger = null)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger;
        }

        public void Initialize()
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                db.EnsureDatabaseCreated();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize database");
                throw;
            }
        }

        public AppConfig LoadConfig()
        {
            try
            {
                var config = _configuration.Get<AppConfig>() ?? new AppConfig();
                ConfigEncryptionHelper.DecryptSensitiveConfig(config);
                return config;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load configuration");
                return new AppConfig();
            }
        }

        public void SaveConfig(AppConfig config)
        {
            try
            {
                var configToSave = config;
                ConfigEncryptionHelper.EncryptSensitiveConfig(configToSave);

                var path = AppPaths.AppSettingsPath;
                Common.JsonHelper.SaveToFile(path, configToSave);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save configuration");
            }
        }

        public UserProfile LoadUserProfile(string userId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var userEntity = db.UserProfiles
                    .Include(u => u.CategoryProgresses)
                    .FirstOrDefault(u => u.UserId == userId);

                if (userEntity != null)
                {
                    return userEntity.ToModel();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load user profile for {UserId}", userId);
            }

            return CreateDefaultProfile(userId);
        }

        public void SaveUserProfile(UserProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile, nameof(profile));
            ArgumentException.ThrowIfNullOrWhiteSpace(profile.UserId, nameof(profile.UserId));

            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var existingUser = db.UserProfiles
                    .Include(u => u.CategoryProgresses)
                    .FirstOrDefault(u => u.UserId == profile.UserId);

                if (existingUser == null)
                {
                    // 创建新用户
                    var userEntity = profile.ToEntity();
                    db.UserProfiles.Add(userEntity);

                    // 添加分类进度
                    foreach (var categoryProgress in profile.LearningProgress.CategoryProgresses.Values)
                    {
                        userEntity.CategoryProgresses.Add(categoryProgress.ToEntity(profile.UserId));
                    }
                }
                else
                {
                    // 更新用户信息
                    existingUser.UpdateEntity(profile);

                    // 删除不再存在的分类进度
                    var categoryNamesInProfile = new HashSet<string>(profile.LearningProgress.CategoryProgresses.Keys);
                    var categoriesToRemove = existingUser.CategoryProgresses
                        .Where(c => !categoryNamesInProfile.Contains(c.CategoryName))
                        .ToList();

                    foreach (var categoryToRemove in categoriesToRemove)
                    {
                        db.CategoryProgresses.Remove(categoryToRemove);
                    }

                    // 更新或添加分类进度
                    foreach (var categoryProgress in profile.LearningProgress.CategoryProgresses.Values)
                    {
                        var existingCategory = existingUser.CategoryProgresses
                            .FirstOrDefault(c => c.CategoryName == categoryProgress.CategoryName);

                        if (existingCategory != null)
                        {
                            existingCategory.UpdateEntity(categoryProgress);
                        }
                        else
                        {
                            existingUser.CategoryProgresses.Add(categoryProgress.ToEntity(profile.UserId));
                        }
                    }
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save user profile for {UserId}", profile.UserId);
                throw new PersistenceException($"保存用户配置失败: {profile.UserId}", ex);
            }
        }

        public List<string> GetUserIds()
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                return db.UserProfiles
                    .Select(u => u.UserId)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get user IDs");
                return new List<string>();
            }
        }

        public void CreateUserProfile(string userId, string userName)
        {
            var profile = CreateDefaultProfile(userId);
            profile.UserName = userName;
            SaveUserProfile(profile);
        }

        public void SaveSession(SessionData session)
        {
            try
            {
                var path = AppPaths.LastSessionPath;
                Common.JsonHelper.SaveToFile(path, session);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save session");
            }
        }

        public SessionData LoadSession()
        {
            try
            {
                var path = AppPaths.LastSessionPath;
                return Common.JsonHelper.LoadFromFile<SessionData>(path) ?? new SessionData();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load session");
                return new SessionData();
            }
        }

        public T? LoadJsonFile<T>(string filePath)
        {
            return Common.JsonHelper.LoadFromFile<T>(filePath);
        }

        public void SaveJsonFile<T>(string filePath, T data)
        {
            Common.JsonHelper.SaveToFile(filePath, data);
        }

        public void PersistCache()
        {
            try
            {
                _cacheService.Persist();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to persist cache");
            }
        }

        private UserProfile CreateDefaultProfile(string userId)
        {
            return new UserProfile
            {
                UserId = userId,
                UserName = userId,
                CreatedAt = DateTime.Now,
                LastLoginTime = DateTime.Now,
                LearningProgress = new LearningProgress()
            };
        }
    }
}
