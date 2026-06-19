using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LearningAssistant.Common;
using LearningAssistant.Models.Config;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Cache;

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
                ConfigEncryptionHelper.DecryptSensitiveConfig(config);
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
                ConfigEncryptionHelper.EncryptSensitiveConfig(configToSave);

                var path = AppPaths.AppSettingsPath;
                JsonHelper.SaveToFile(path, configToSave);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存配置失败");
            }
        }

        public UserProfile LoadUserProfile(string userId)
        {
            try
            {
                var path = AppPaths.GetUserProgressPath(userId);
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
                var path = AppPaths.GetUserProgressPath(profile.UserId);
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
                var dir = AppPaths.UsersDir;
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
                var path = AppPaths.LastSessionPath;
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
                var path = AppPaths.LastSessionPath;
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
