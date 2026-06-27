using LearningAssistant.Models.Learning;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace LearningAssistant.Common
{
    /// <summary>
    /// LearningItem 多态序列化和反序列化转换器
    /// 替代不安全的 TypeNameHandling.Auto，防止远程代码执行攻击
    /// </summary>
    public class LearningItemJsonConverter : JsonConverter<LearningItem>
    {
        /// <summary>
        /// 类型标识属性名
        /// </summary>
        private const string TypePropertyName = "$type";

        /// <summary>
        /// 允许的类型白名单
        /// </summary>
        private static readonly Dictionary<string, Type> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            { "EnglishWord", typeof(EnglishWord) },
            { "EnglishPhrase", typeof(EnglishPhrase) },
            { "EnglishSentence", typeof(EnglishSentence) },
            { "EnglishComprehensive", typeof(EnglishComprehensive) },
            { "ChineseCharacter", typeof(ChineseCharacter) },
            { "ChinesePhrase", typeof(ChinesePhrase) },
            { "ChineseIdiom", typeof(ChineseIdiom) },
            { "ChinesePoem", typeof(ChinesePoem) },
            { "ChineseComprehensive", typeof(ChineseComprehensive) },
            { "GrammarRule", typeof(GrammarRule) },
            { "GeneralSubjectItem", typeof(GeneralSubjectItem) }
        };

        /// <summary>
        /// 注册的子类型（从外部程序集加载的类型）
        /// </summary>
        private static readonly Dictionary<string, Type> RegisteredSubTypes = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 注册自定义子类型
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <param name="type">类型</param>
        public static void RegisterSubType(string typeName, Type type)
        {
            if (typeof(LearningItem).IsAssignableFrom(type))
            {
                RegisteredSubTypes[typeName] = type;
            }
        }

        /// <summary>
        /// 清除注册的类型
        /// </summary>
        public static void ClearRegisteredSubTypes()
        {
            RegisteredSubTypes.Clear();
        }

        /// <summary>
        /// 获取所有允许的类型
        /// </summary>
        private static Dictionary<string, Type> GetAllAllowedTypes()
        {
            var result = new Dictionary<string, Type>(AllowedTypes, StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in RegisteredSubTypes)
            {
                result[kvp.Key] = kvp.Value;
            }
            return result;
        }

        /// <inheritdoc/>
        public override LearningItem? ReadJson(JsonReader reader, Type objectType, LearningItem? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new JsonException($"Expected StartObject token, got {reader.TokenType}");
            }

            JObject jsonObject = JObject.Load(reader);

            // 获取类型标识
            string? typeName = null;
            JToken? typeToken = jsonObject[TypePropertyName];

            if (typeToken != null)
            {
                typeName = typeToken.ToString();
            }
            else
            {
                // 尝试从 JSON 结构自动推断类型
                typeName = InferTypeFromProperties(jsonObject);
            }

            if (string.IsNullOrEmpty(typeName))
            {
                throw new JsonException("无法确定 LearningItem 的具体类型，缺少类型标识或可推断的属性");
            }

            var allTypes = GetAllAllowedTypes();

            // 尝试使用完整类型名（如 "LearningAssistant.Models.Learning.EnglishWord"）
            if (!allTypes.ContainsKey(typeName))
            {
                // 尝试从完整类型名中提取短名称
                var shortName = typeName;
                if (typeName.Contains('.'))
                {
                    shortName = typeName.Substring(typeName.LastIndexOf('.') + 1);
                }
                if (!allTypes.ContainsKey(shortName))
                {
                    throw new JsonException($"类型 '{typeName}' 不在允许的类型白名单中。允许的类型: {string.Join(", ", allTypes.Keys)}");
                }
                typeName = shortName;
            }

            if (!allTypes.TryGetValue(typeName, out var targetType))
            {
                throw new JsonException($"类型 '{typeName}' 不在允许的类型白名单中");
            }

            // 创建目标类型的实例
            var instance = Activator.CreateInstance(targetType);
            if (instance == null)
            {
                throw new JsonException($"无法创建类型 '{typeName}' 的实例");
            }

            // 使用 JsonSerializer 填充属性
            using var jsonReader = jsonObject.CreateReader();
            serializer.Populate(jsonReader, instance);

            return (LearningItem)instance;
        }

        /// <summary>
        /// 从 JSON 属性推断具体类型
        /// </summary>
        private static string? InferTypeFromProperties(JObject jsonObject)
        {
            // 根据特定属性推断类型
            if (jsonObject.ContainsKey("Word"))
                return "EnglishWord";
            if (jsonObject.ContainsKey("Character"))
                return "ChineseCharacter";
            if (jsonObject.ContainsKey("Phrase"))
                return "EnglishPhrase";
            if (jsonObject.ContainsKey("Sentence"))
                return "EnglishSentence";
            if (jsonObject.ContainsKey("Idiom"))
                return "ChineseIdiom";
            if (jsonObject.ContainsKey("Poem"))
                return "ChinesePoem";
            if (jsonObject.ContainsKey("Questions"))
                return "ChineseComprehensive"; // 或 EnglishComprehensive
            if (jsonObject.ContainsKey("Rule"))
                return "GrammarRule";
            if (jsonObject.ContainsKey("Title"))
                return "GeneralSubjectItem";

            return null;
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, LearningItem? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            // 创建 JObject 并添加类型标识
            JObject jsonObject = new JObject();

            // 获取类型短名称
            var typeName = value.GetType().Name;
            jsonObject[TypePropertyName] = typeName;

            // 序列化所有属性
            foreach (var property in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.Name == nameof(LearningItem.GetType) ||
                    property.Name == nameof(LearningItem.GetHashCode) ||
                    property.Name == nameof(LearningItem.Equals) ||
                    property.Name == nameof(LearningItem.ToString))
                {
                    continue;
                }

                var propertyValue = property.GetValue(value);
                if (propertyValue != null)
                {
                    jsonObject[property.Name] = JToken.FromObject(propertyValue, serializer);
                }
            }

            jsonObject.WriteTo(writer);
        }


    }

    /// <summary>
    /// LearningItem 列表的 JsonConverter
    /// </summary>
    public class LearningItemListJsonConverter : JsonConverter<List<LearningItem>>
    {
        private readonly LearningItemJsonConverter _itemConverter = new LearningItemJsonConverter();

        /// <inheritdoc/>
        public override List<LearningItem>? ReadJson(JsonReader reader, Type objectType, List<LearningItem>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType != JsonToken.StartArray)
            {
                throw new JsonException($"Expected StartArray token, got {reader.TokenType}");
            }

            var result = new List<LearningItem>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                    break;

                if (reader.TokenType == JsonToken.StartObject)
                {
                    var item = _itemConverter.ReadJson(reader, typeof(LearningItem), null, false, serializer);
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, List<LearningItem>? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartArray();

            foreach (var item in value)
            {
                _itemConverter.WriteJson(writer, item, serializer);
            }

            writer.WriteEndArray();
        }
    }
}
