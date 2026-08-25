using LearningAssistant.Common;
using LearningAssistant.Models.User;
using Microsoft.Extensions.Logging;
using System.Text.Json;

using LearningAssistant.Abstractions;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 用户设置服务实现
    /// 提供用户设置的加载、保存和管理功能
    /// 从 LearningForm 中提取的设置管理逻辑
    /// </summary>
    public class UserSettingsService : IUserSettingsService
    {
        private readonly ILogger<UserSettingsService> _logger;
        private Settings? _cachedSettings;
        private string? _cachedUserId;

        public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
        private readonly IAppPaths _appPaths;

        public UserSettingsService(ILogger<UserSettingsService> logger, IAppPaths appPaths)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appPaths = appPaths;
        }

        public async Task<Settings> LoadSettingsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("加载设置失败: 用户ID为空");
                return new Settings();
            }

            try
            {
                // 检查缓存
                if (_cachedSettings != null && _cachedUserId == userId)
                {
                    return _cachedSettings;
                }

                string settingsPath = GetSettingsPath(userId);

                if (!File.Exists(settingsPath))
                {
                    var defaultSettings = new Settings();
                    _cachedSettings = defaultSettings;
                    _cachedUserId = userId;
                    return defaultSettings;
                }

                string json = await File.ReadAllTextAsync(settingsPath);
                var settings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();

                // 从旧版设置迁移：Language -> Subject
                MigrateLegacySettings(settings);

                _cachedSettings = settings;
                _cachedUserId = userId;

                _logger.LogInformation("成功加载用户设置: UserId={UserId}", userId);
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载用户设置失败, UserId: {UserId}", userId);
                return new Settings();
            }
        }

        public async Task SaveSettingsAsync(string userId, Settings settings)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("保存设置失败: 用户ID为空");
                return;
            }

            if (settings == null)
            {
                _logger.LogWarning("保存设置失败: 设置对象为空");
                return;
            }

            try
            {
                var oldSettings = _cachedSettings;
                string settingsPath = GetSettingsPath(userId);
                string? directory = Path.GetDirectoryName(settingsPath);

                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(settingsPath, json);

                // 更新缓存
                _cachedSettings = settings;
                _cachedUserId = userId;

                // 触发设置变更事件
                OnSettingsChanged(userId, oldSettings ?? new Settings(), settings);

                _logger.LogInformation("成功保存用户设置: UserId={UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存用户设置失败, UserId: {UserId}", userId);
                throw;
            }
        }

        public string GetSettingsPath(string userId)
        {
            var userDir = Path.Combine(_appPaths.UsersDir, userId);
            if (!Directory.Exists(userDir))
                Directory.CreateDirectory(userDir);
            return Path.Combine(userDir, "settings.json");
        }

        /// <summary>
        /// 使设置缓存失效
        /// </summary>
        public void InvalidateCache()
        {
            _cachedSettings = null;
            _cachedUserId = null;
        }

        #region === 私有方法 ===

        /// <summary>
        /// 迁移旧版设置
        /// 从 LearningForm.LoadSettings() 迁移
        /// </summary>
        private void MigrateLegacySettings(Settings settings)
        {
            if (string.IsNullOrEmpty(settings.Subject))
            {
                // 从旧版Language字段迁移到Subject
                if (settings.Language == "Chinese")
                    settings.Subject = Constants.Subject.Chinese;
                else if (settings.Language == "English")
                    settings.Subject = Constants.Subject.English;
                else
                    settings.Subject = Constants.Subject.English;
            }
        }

        /// <summary>
        /// 触发设置变更事件
        /// </summary>
        protected virtual void OnSettingsChanged(string userId, Settings oldSettings, Settings newSettings)
        {
            try
            {
                SettingsChanged?.Invoke(this, new SettingsChangedEventArgs
                {
                    UserId = userId,
                    OldSettings = oldSettings,
                    NewSettings = newSettings
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "触发设置变更事件失败");
            }
        }

        #endregion
    }
}