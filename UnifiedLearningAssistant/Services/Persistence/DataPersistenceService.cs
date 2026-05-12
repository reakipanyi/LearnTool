using Microsoft.Extensions.Configuration;
using UnifiedLearningAssistant.Common;
using UnifiedLearningAssistant.Models.Config;
using UnifiedLearningAssistant.Models.User;
using UnifiedLearningAssistant.Services.Cache;
using UnifiedLearningAssistant.Services.Utils;

namespace UnifiedLearningAssistant.Services.Persistence
{
    public class DataPersistenceService : IDataPersistenceService
    {
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cacheService;

        public DataPersistenceService(IConfiguration configuration, ICacheService cacheService)
        {
            _configuration = configuration;
            _cacheService = cacheService;
        }

        public AppConfig LoadConfig()
        {
            try
            {
                var config = _configuration.Get<AppConfig>() ?? new AppConfig();
                // 新增功能：配置安全优化 - 解密敏感信息
                DecryptSensitiveConfig(config);
                return config;
            }
            catch
            {
                return new AppConfig();
            }
        }

        public void SaveConfig(AppConfig config)
        {
            try
            {
                // 新增功能：配置安全优化 - 保存时加密敏感信息
                // MemberwiseClone is protected; perform a deep clone via JSON serialization
                var json = Common.JsonHelper.Serialize(config);
                var configToSave = Common.JsonHelper.Deserialize<AppConfig>(json) ?? new AppConfig();
                EncryptSensitiveConfig(configToSave);

                var path = Path.Combine(FileHelper.GetAppDirectory(), "appsettings.json");
                JsonHelper.SaveToFile(path, configToSave);
            }
            catch
            {
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
                config.TranslationConfig.AppKey = SecureConfigManager.Encrypt(config.TranslationConfig.AppKey);
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
                config.TranslationConfig.AppKey = SecureConfigManager.Decrypt(config.TranslationConfig.AppKey);
                config.TranslationConfig.AppSecret = SecureConfigManager.Decrypt(config.TranslationConfig.AppSecret);
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
            catch
            {
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
            catch
            {
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
            catch
            {
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
            catch
            {
            }
        }

        public SessionData LoadSession()
        {
            try
            {
                var path = FileHelper.GetSessionPath();
                return JsonHelper.LoadFromFile<SessionData>(path) ?? new SessionData();
            }
            catch
            {
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
            catch
            {
            }
        }
    }
}
