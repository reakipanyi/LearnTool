using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UnifiedLearningAssistant.Data.Database;
using UnifiedLearningAssistant.Models.Config;
using UnifiedLearningAssistant.Models.User;
using UnifiedLearningAssistant.Services.Cache;

namespace UnifiedLearningAssistant.Services.Persistence
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

            // 确保数据库已创建
            using var db = _dbContextFactory.CreateDbContext();
            db.EnsureDatabaseCreated();
        }

        public AppConfig LoadConfig()
        {
            try
            {
                // 配置仍然从 JSON 文件加载，保持原样
                var config = _configuration.Get<AppConfig>() ?? new AppConfig();
                DecryptSensitiveConfig(config);
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
                var configToSave = config; // 直接使用原对象，无需序列化/反序列化
                EncryptSensitiveConfig(configToSave);

                var path = Path.Combine(Common.FileHelper.GetAppDirectory(), "appsettings.json");
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
                            // 更新现有分类
                            var updatedEntity = categoryProgress.ToEntity(profile.UserId);
                            existingCategory.KnownItemsJson = updatedEntity.KnownItemsJson;
                            existingCategory.UnknownItemsJson = updatedEntity.UnknownItemsJson;
                            existingCategory.TotalTestCount = updatedEntity.TotalTestCount;
                            existingCategory.CorrectCount = updatedEntity.CorrectCount;
                            existingCategory.LastTestDate = updatedEntity.LastTestDate;
                            existingCategory.LastResumeIndex = updatedEntity.LastResumeIndex;
                            existingCategory.QuickTestResumeIndex = updatedEntity.QuickTestResumeIndex;
                            existingCategory.LastStudyMode = updatedEntity.LastStudyMode;
                        }
                        else
                        {
                            // 添加新分类
                            existingUser.CategoryProgresses.Add(categoryProgress.ToEntity(profile.UserId));
                        }
                    }
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save user profile for {UserId}", profile.UserId);
                throw; // 可选：重新抛出或静默处理，取决于业务需求
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
                // 会话数据仍然保存到 JSON 文件，保持原样
                var path = Common.FileHelper.GetSessionPath();
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
                var path = Common.FileHelper.GetSessionPath();
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

        private void EncryptSensitiveConfig(AppConfig config)
        {
            if (config.TtsConfig != null)
            {
                config.TtsConfig.ApiKey = Utils.SecureConfigManager.Encrypt(config.TtsConfig.ApiKey);
            }
            if (config.AiConfig != null)
            {
                config.AiConfig.ApiKey = Utils.SecureConfigManager.Encrypt(config.AiConfig.ApiKey);
            }
            if (config.TranslationConfig != null)
            {
                config.TranslationConfig.BaiduAppId = Utils.SecureConfigManager.Encrypt(config.TranslationConfig.BaiduAppId);
            }
        }

        private void DecryptSensitiveConfig(AppConfig config)
        {
            if (config.TtsConfig != null)
            {
                config.TtsConfig.ApiKey = Utils.SecureConfigManager.Decrypt(config.TtsConfig.ApiKey);
            }
            if (config.AiConfig != null)
            {
                config.AiConfig.ApiKey = Utils.SecureConfigManager.Decrypt(config.AiConfig.ApiKey);
            }
            if (config.TranslationConfig != null)
            {
                config.TranslationConfig.BaiduAppId = Utils.SecureConfigManager.Decrypt(config.TranslationConfig.BaiduAppId);
                config.TranslationConfig.BaiduSecret = Utils.SecureConfigManager.Decrypt(config.TranslationConfig.BaiduSecret);
            }
        }
    }
}
