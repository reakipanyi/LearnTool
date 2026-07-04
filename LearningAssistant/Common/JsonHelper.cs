using System.Text.Json;
using System.Text.Json.Serialization;
using LearningAssistant.Models.Learning;
using Newtonsoft.Json;

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

        private static readonly JsonSerializerSettings _learningItemSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = { new LearningItemListJsonConverter() }
        };

        public static string Serialize<T>(T obj)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj, _options);
        }

        public static T? Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default;
            
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, _options);
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
                return JsonConvert.DeserializeObject<List<LearningItem>>(json, _learningItemSettings) 
                       ?? new List<LearningItem>();
            }
            catch (Exception)
            {
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
            
            var json = JsonConvert.SerializeObject(items, _learningItemSettings);
            File.WriteAllText(filePath, json);
        }
    }
}