using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LearningAssistant.Common;
using LearningAssistant.Models.Config;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Cache;
using LearningAssistant.Services.Utils;

namespace LearningAssistant.Services.Persistence
{
    public class DataPersistenceService : IDataPersistenceService
    {
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cacheService;
        private readonly ILogger<DataPersistenceService> _logger;

        public DataPersistenceService(IConfiguration configuration, ICacheService cacheService, ILogger<DataPersistenceService> logger)
        {
            _configuration = configuration;
            _cacheService = cacheService;
            _logger = logger;
        }

        public AppConfig LoadConfig()
        {
            try
            {
                var config = _configuration.Get<AppConfig>() ?? new AppConfig();
                DecryptSensitiveConfig(config);
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "加载配置失败，使用默认配置");
                return new AppConfig();
            }
        }

        public void SaveConfig(AppConfig config)
        {
            try
            {
                var json = Common.JsonHelper.Serialize(config);
                var configToSave = Common.JsonHelper.Deserialize<AppConfig>(json) ?? new AppConfig();
                EncryptSensitiveConfig(configToSave);

                var path = Path.Combine(FileHelper.GetAppDirectory(), "appsettings.json");
                JsonHelper.SaveToFile(path, configToSave);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存配置失败");
            }
        }

        // 新增功能：配置安全优化 - 加密敏感配置
        private void EncryptSensitiveConfig(AppConfig config)
        {
            if (config.TtsConfig != null)
            {
                config.TtsConfig.ApiKey = SecureConfigManager.Encrypt(config.TtsConfig.ApiKey);
            }
            if (config.AiConfig != null)
            {
                config.AiConfig.ApiKey = SecureConfigManager.Encrypt(config.AiConfig.ApiKey);
            }
            if (config.TranslationConfig != null)
            {
                config.TranslationConfig.BaiduAppId = SecureConfigManager.Encrypt(config.TranslationConfig.BaiduAppId);
                config.TranslationConfig.BaiduSecret = SecureConfigManager.Encrypt(config.TranslationConfig.BaiduSecret);
            }
            if (config.CloudStorageConfig != null)
            {
                config.CloudStorageConfig.BaiduClientId = SecureConfigManager.Encrypt(config.CloudStorageConfig.BaiduClientId);
                config.CloudStorageConfig.BaiduClientSecret = SecureConfigManager.Encrypt(config.CloudStorageConfig.BaiduClientSecret);
                config.CloudStorageConfig.BaiduAccessToken = SecureConfigManager.Encrypt(config.CloudStorageConfig.BaiduAccessToken);
                config.CloudStorageConfig.BaiduRefreshToken = SecureConfigManager.Encrypt(config.CloudStorageConfig.BaiduRefreshToken);
            }
        }

        // 新增功能：配置安全优化 - 解密敏感配置
        private void DecryptSensitiveConfig(AppConfig config)
        {
            if (config.TtsConfig != null)
            {
                config.TtsConfig.ApiKey = SecureConfigManager.Decrypt(config.TtsConfig.ApiKey);
            }
            if (config.AiConfig != null)
            {
                config.AiConfig.ApiKey = SecureConfigManager.Decrypt(config.AiConfig.ApiKey);
            }
            if (config.TranslationConfig != null)
            {
                config.TranslationConfig.BaiduAppId = SecureConfigManager.Decrypt(config.TranslationConfig.BaiduAppId);
                config.TranslationConfig.BaiduSecret = SecureConfigManager.Decrypt(config.TranslationConfig.BaiduSecret);
            }
        }

        public UserProfile LoadUserProfile(string userId)
        {
            try
            {
                var path = FileHelper.GetUserProgressPath(userId);
                var profile = JsonHelper.LoadFromFile<UserProfile>(path);
                if (profile != null)
                    return profile;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "加载用户 {UserId} 的进度失败，创建默认进度", userId);
            }
            return CreateDefaultProfile(userId);
        }

        public void SaveUserProfile(UserProfile profile)
        {
            try
            {
                var path = FileHelper.GetUserProgressPath(profile.UserId);
                JsonHelper.SaveToFile(path, profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存用户 {UserId} 的进度失败", profile.UserId);
            }
        }

        public List<string> GetUserIds()
        {
            try
            {
                var dir = FileHelper.GetUsersDirectory();
                return Directory.EnumerateFiles(dir, "*.json")
                               .Select(f => Path.GetFileNameWithoutExtension(f))
                               .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取用户列表失败");
                return new List<string>();
            }
        }

        public void CreateUserProfile(string userId, string userName)
        {
            var profile = CreateDefaultProfile(userId);
            profile.UserName = userName;
            SaveUserProfile(profile);
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

        public void SaveSession(SessionData session)
        {
            try
            {
                var path = FileHelper.GetSessionPath();
                JsonHelper.SaveToFile(path, session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存会话数据失败");
            }
        }

        public SessionData LoadSession()
        {
            try
            {
                var path = FileHelper.GetSessionPath();
                return JsonHelper.LoadFromFile<SessionData>(path) ?? new SessionData();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "加载会话数据失败，使用默认会话");
                return new SessionData();
            }
        }

        public T? LoadJsonFile<T>(string filePath)
        {
            return JsonHelper.LoadFromFile<T>(filePath);
        }

        public void SaveJsonFile<T>(string filePath, T data)
        {
            JsonHelper.SaveToFile(filePath, data);
        }

        public void PersistCache()
        {
            try
            {
                _cacheService.Persist();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "持久化缓存失败");
            }
        }
    }
}
