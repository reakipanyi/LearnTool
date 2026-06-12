using LearningAssistant.Models.Config;
using LearningAssistant.Services.Utils;

namespace LearningAssistant.Services.Persistence
{
    public static class ConfigEncryptionHelper
    {
        public static void EncryptSensitiveConfig(AppConfig config)
        {
            if (config == null) return;

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

        public static void DecryptSensitiveConfig(AppConfig config)
        {
            if (config == null) return;

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
            if (config.CloudStorageConfig != null)
            {
                config.CloudStorageConfig.BaiduClientId = SecureConfigManager.Decrypt(config.CloudStorageConfig.BaiduClientId);
                config.CloudStorageConfig.BaiduClientSecret = SecureConfigManager.Decrypt(config.CloudStorageConfig.BaiduClientSecret);
                config.CloudStorageConfig.BaiduAccessToken = SecureConfigManager.Decrypt(config.CloudStorageConfig.BaiduAccessToken);
                config.CloudStorageConfig.BaiduRefreshToken = SecureConfigManager.Decrypt(config.CloudStorageConfig.BaiduRefreshToken);
            }
        }
    }
}