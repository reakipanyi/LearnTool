using System;
using System.Security.Cryptography;
using System.Text;

namespace LearningAssistant.Services.Utils
{
    public static class SecureConfigManager
    {
        private static readonly Lazy<byte[]> _entropy = new Lazy<byte[]>(LoadEntropyFromSecureSource, true);
        private static byte[] Entropy => _entropy.Value;

        private static byte[] LoadEntropyFromSecureSource()
        {
            var envEntropy = Environment.GetEnvironmentVariable("LEARNING_ASSISTANT_ENTROPY");
            if (!string.IsNullOrEmpty(envEntropy))
            {
                try
                {
                    return Convert.FromBase64String(envEntropy);
                }
                catch (FormatException ex)
                {
                    System.Diagnostics.Trace.TraceWarning($"Invalid entropy format: {ex.Message}");
                }
            }

            var machineId = System.Security.Principal.WindowsIdentity.GetCurrent().User?.Value ?? 
                           Environment.MachineName;
            return SHA256.HashData(Encoding.UTF8.GetBytes(machineId));
        }

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
            catch (CryptographicException ex)
            {
                System.Diagnostics.Trace.TraceWarning($"Encryption failed: {ex.Message}");
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
            catch (CryptographicException ex)
            {
                System.Diagnostics.Trace.TraceWarning($"Decryption failed: {ex.Message}");
                return encryptedText;
            }
        }

        public static void EnsureEncryption()
        {
        }
    }
}
