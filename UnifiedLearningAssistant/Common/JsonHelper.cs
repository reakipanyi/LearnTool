using System.Text.Json;

namespace UnifiedLearningAssistant.Common
{
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static string Serialize<T>(T obj)
        {
            return JsonSerializer.Serialize(obj, _options);
        }

        public static T? Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default;
            
            return JsonSerializer.Deserialize<T>(json, _options);
        }

        public static T? LoadFromFile<T>(string filePath)
        {
            if (!File.Exists(filePath))
                return default;
            
            try
            {
                var json = File.ReadAllText(filePath);
                return Deserialize<T>(json);
            }
            catch
            {
                return default;
            }
        }

        public static void SaveToFile<T>(string filePath, T obj)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                FileHelper.EnsureDirectoryExists(directory);
            }
            
            var json = Serialize(obj);
            File.WriteAllText(filePath, json);
        }

        public static byte[] SerializeToBytes<T>(T obj)
        {
            var json = Serialize(obj);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public static T? DeserializeFromBytes<T>(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return default;
            
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            return Deserialize<T>(json);
        }
    }
}