using System.Text.Json;
using System.Text.Json.Serialization;
using LearningAssistant.Models.Learning;

namespace LearningAssistant.Common
{
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new JsonStringEnumConverter() }
        };

        private static readonly JsonSerializerOptions _learningItemOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new JsonStringEnumConverter(), new LearningItemJsonConverter() }
        };

        public static string Serialize<T>(T obj)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj, _options);
        }

        public static T? Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default;
            
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(json, _options);
            }
            catch (System.Text.Json.JsonException)
            {
                return default;
            }
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
            catch (FileNotFoundException)
            {
                return default;
            }
            catch (System.Text.Json.JsonException ex)
            {
                System.Diagnostics.Trace.TraceWarning($"JSON parse error in {filePath}: {ex.Message}");
                return default;
            }
            catch (IOException ex)
            {
                System.Diagnostics.Trace.TraceWarning($"IO error reading {filePath}: {ex.Message}");
                return default;
            }
        }

        public static void SaveToFile<T>(string filePath, T obj)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                AppPaths.EnsureDirectoryExists(directory);
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

        public static List<LearningItem> DeserializeLearningItems(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<LearningItem>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<LearningItem>>(json, _learningItemOptions) 
                       ?? new List<LearningItem>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"反序列化学习项失败: {ex.Message}");
                return new List<LearningItem>();
            }
        }

        public static void SaveToFile(string filePath, List<LearningItem> items)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                AppPaths.EnsureDirectoryExists(directory);
            }
            
            var json = System.Text.Json.JsonSerializer.Serialize(items, _learningItemOptions);
            File.WriteAllText(filePath, json);
        }
    }
}