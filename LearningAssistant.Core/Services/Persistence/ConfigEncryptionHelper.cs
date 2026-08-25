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

            // 按字段降级解密：单个字段解密失败（跨用户/跨机器 DPAPI 不匹配、明文、损坏）时
            // 保留原值并告警，避免整体抛出导致 LoadConfig 返回默认配置并在后续保存时覆盖磁盘原值。
            if (config.TtsConfig != null)
            {
                config.TtsConfig.ApiKey = TryDecrypt(config.TtsConfig.ApiKey);
            }
            if (config.AiConfig != null)
            {
                config.AiConfig.ApiKey = TryDecrypt(config.AiConfig.ApiKey);
            }
            if (config.TranslationConfig != null)
            {
                config.TranslationConfig.BaiduAppId = TryDecrypt(config.TranslationConfig.BaiduAppId);
                config.TranslationConfig.BaiduSecret = TryDecrypt(config.TranslationConfig.BaiduSecret);
            }
            if (config.CloudStorageConfig != null)
            {
                config.CloudStorageConfig.BaiduClientId = TryDecrypt(config.CloudStorageConfig.BaiduClientId);
                config.CloudStorageConfig.BaiduClientSecret = TryDecrypt(config.CloudStorageConfig.BaiduClientSecret);
                config.CloudStorageConfig.BaiduAccessToken = TryDecrypt(config.CloudStorageConfig.BaiduAccessToken);
                config.CloudStorageConfig.BaiduRefreshToken = TryDecrypt(config.CloudStorageConfig.BaiduRefreshToken);
            }
        }

        /// <summary>
        /// 容错解密：失败时保留原值，避免单个字段问题导致整体配置不可用。
        /// </summary>
        private static string? TryDecrypt(string? value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            try
            {
                return SecureConfigManager.Decrypt(value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"配置项解密失败，保留原值: {ex.Message}");
                return value;
            }
        }
    }
}