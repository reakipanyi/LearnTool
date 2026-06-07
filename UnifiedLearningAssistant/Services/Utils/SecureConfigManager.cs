using System;
using System.Security.Cryptography;
using System.Text;

namespace LearningAssistant.Services.Utils
{
    // 新增功能：配置安全优化 - API密钥加密存储
    public static class SecureConfigManager
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("UnifiedLearningAssistant_2025");
        private static readonly object LockObj = new object();

        public static string? Encrypt(string? plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                var userData = Encoding.UTF8.GetBytes(plainText);
                var encryptedData = ProtectedData.Protect(
                    userData, Entropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedData);
            }
            catch (Exception)
            {
                // 如果加密失败，返回原始文本（降级处理）
                return plainText;
            }
        }

        public static string? Decrypt(string? encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            try
            {
                var encryptedData = Convert.FromBase64String(encryptedText);
                var decryptedData = ProtectedData.Unprotect(
                    encryptedData, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decryptedData);
            }
            catch (Exception)
            {
                // 如果解密失败，尝试直接返回原始文本（可能未加密）
                return encryptedText;
            }
        }

        public static void EnsureEncryption()
        {
            // 检查是否需要迁移旧配置
        }
    }
}
