using LearningAssistant.Models.Learning;
using LearningAssistant.Models.Learning.Status;
using LearningAssistant.Models.Learning.ValueObjects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LearningAssistant.Common
{
    public class LearningItemJsonConverter : JsonConverter<LearningItem>
    {
        private const string TypePropertyName = "$type";

        public override LearningItem? ReadJson(JsonReader reader, Type objectType, LearningItem? existingValue, 
                                                bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException($"Expected StartObject token, got {reader.TokenType}");

            JObject jsonObject = JObject.Load(reader);
            var item = new LearningItem();

            item.Id = jsonObject["Id"]?.ToString() ?? Guid.NewGuid().ToString();
            item.CreatedAt = jsonObject["CreatedAt"]?.ToObject<DateTime>() ?? DateTime.Now;
            item.UpdatedAt = jsonObject["UpdatedAt"]?.ToObject<DateTime>() ?? DateTime.Now;

            if (Enum.TryParse(jsonObject["Subject"]?.ToString(), out SubjectType subject))
                item.Subject = subject;

            if (Enum.TryParse(jsonObject["SubCategory"]?.ToString(), out SubCategoryType subCategory))
                item.SubCategory = subCategory;

            item.MainContent = jsonObject["MainContent"]?.ToString() ?? 
                              jsonObject["Word"]?.ToString() ?? 
                              jsonObject["Character"]?.ToString() ?? 
                              jsonObject["Phrase"]?.ToString() ?? 
                              jsonObject["Sentence"]?.ToString() ??
                              jsonObject["Idiom"]?.ToString() ??
                              jsonObject["Poem"]?.ToString() ??
                              jsonObject["Title"]?.ToString() ??
                              jsonObject["Rule"]?.ToString() ?? string.Empty;

            var meaningContent = jsonObject["Meaning"]?.ToString() ?? 
                                jsonObject["ChineseMeaning"]?.ToString() ?? 
                                jsonObject["Content"]?.ToString() ?? 
                                jsonObject["Explanation"]?.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(meaningContent))
                item.Meaning = Models.Learning.ValueObjects.Meaning.Create(meaningContent);

            var exampleContent = jsonObject["Example"]?.ToString() ?? string.Empty;
            var exampleTranslation = jsonObject["ExampleTranslation"]?.ToString();
            if (!string.IsNullOrWhiteSpace(exampleContent))
                item.Example = Models.Learning.ValueObjects.Example.Create(exampleContent, exampleTranslation);

            var pronunciation = jsonObject["Phonetic"]?.ToString() ?? 
                               jsonObject["Pinyin"]?.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(pronunciation))
            {
                item.Pronunciation = Models.Learning.ValueObjects.Pronunciation.Create(
                    pronunciation,
                    jsonObject["UkPhonetic"]?.ToString(),
                    jsonObject["UsPhonetic"]?.ToString()
                );
            }

            if (jsonObject.ContainsKey("StrokeCount") || jsonObject.ContainsKey("Radical"))
            {
                item.CharacterFeatures = Models.Learning.ValueObjects.CharacterFeatures.Create(
                    jsonObject["StrokeCount"]?.ToString() ?? string.Empty,
                    jsonObject["Radical"]?.ToString() ?? string.Empty,
                    jsonObject["Structure"]?.ToString() ?? string.Empty
                );
            }

            if (jsonObject.ContainsKey("PartOfSpeech") || jsonObject.ContainsKey("WordForms"))
            {
                item.WordFeatures = Models.Learning.ValueObjects.WordFeatures.Create(
                    jsonObject["PartOfSpeech"]?.ToString() ?? string.Empty,
                    jsonObject["WordForms"]?.ToString() ?? string.Empty,
                    jsonObject["Collocations"]?.ToString() ?? string.Empty,
                    jsonObject["SyllableBreakdown"]?.ToString() ?? string.Empty
                );
            }

            if (Enum.TryParse(jsonObject["Status"]?.ToString(), out LearningStatus status))
                item.Status = status;

            item.ReviewCount = jsonObject["ReviewCount"]?.ToObject<int>() ?? 0;
            item.LastReviewedAt = jsonObject["LastReviewedAt"]?.ToObject<DateTime?>();

            var extendedProps = new Dictionary<string, object>();
            foreach (var prop in jsonObject)
            {
                if (!IsStandardProperty(prop.Key))
                    extendedProps[prop.Key] = prop.Value.ToObject<object>() ?? string.Empty;
            }
            item.ExtendedProperties = JsonConvert.SerializeObject(extendedProps);

            var typeName = jsonObject[TypePropertyName]?.ToString();
            if (!string.IsNullOrEmpty(typeName) && item.SubCategory == 0)
            {
                item.SubCategory = InferSubCategoryFromTypeName(typeName);
            }

            return item;
        }

        private static readonly HashSet<string> StandardProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            "Id", "CreatedAt", "UpdatedAt", "Subject", "SubCategory",
            "MainContent", "Meaning", "Example", "Phonetic", "Pinyin",
            "UkPhonetic", "UsPhonetic", "StrokeCount", "Radical",
            "Structure", "PartOfSpeech", "WordForms", "Collocations",
            "SyllableBreakdown", "Word", "Character", "Phrase", "Sentence",
            "Idiom", "Poem", "Title", "Rule", "Questions",
            "ChineseMeaning", "ExampleTranslation", "Content", "Explanation",
            "$type", "Status", "ReviewCount", "LastReviewedAt"
        };

        private static bool IsStandardProperty(string key)
        {
            return StandardProperties.Contains(key);
        }

        private static SubCategoryType InferSubCategoryFromTypeName(string typeName)
        {
            return typeName switch
            {
                "EnglishWord" => SubCategoryType.EnglishWord,
                "EnglishPhrase" => SubCategoryType.EnglishPhrase,
                "EnglishSentence" => SubCategoryType.EnglishSentence,
                "EnglishComprehensive" => SubCategoryType.EnglishComprehensive,
                "ChineseCharacter" => SubCategoryType.ChineseCharacter,
                "ChinesePhrase" => SubCategoryType.ChinesePhrase,
                "ChineseIdiom" => SubCategoryType.ChineseIdiom,
                "ChinesePoem" => SubCategoryType.ChinesePoem,
                "ChineseComprehensive" => SubCategoryType.ChineseComprehensive,
                _ => SubCategoryType.ChineseCharacter
            };
        }

        public override void WriteJson(JsonWriter writer, LearningItem? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("Id");
            writer.WriteValue(value.Id);
            writer.WritePropertyName("CreatedAt");
            writer.WriteValue(value.CreatedAt);
            writer.WritePropertyName("UpdatedAt");
            writer.WriteValue(value.UpdatedAt);
            writer.WritePropertyName("Subject");
            writer.WriteValue(value.Subject.ToString());
            writer.WritePropertyName("SubCategory");
            writer.WriteValue(value.SubCategory.ToString());
            writer.WritePropertyName("MainContent");
            writer.WriteValue(value.MainContent);

            if (value.Meaning != null)
            {
                writer.WritePropertyName("Meaning");
                writer.WriteValue(value.Meaning.Content);
            }

            if (value.Example != null)
            {
                writer.WritePropertyName("Example");
                writer.WriteValue(value.Example.Content);
                if (!string.IsNullOrWhiteSpace(value.Example.Translation))
                {
                    writer.WritePropertyName("ExampleTranslation");
                    writer.WriteValue(value.Example.Translation);
                }
            }

            if (value.Pronunciation != null)
            {
                writer.WritePropertyName("Phonetic");
                writer.WriteValue(value.Pronunciation.Main);
                if (!string.IsNullOrWhiteSpace(value.Pronunciation.UkPhonetic))
                {
                    writer.WritePropertyName("UkPhonetic");
                    writer.WriteValue(value.Pronunciation.UkPhonetic);
                }
                if (!string.IsNullOrWhiteSpace(value.Pronunciation.UsPhonetic))
                {
                    writer.WritePropertyName("UsPhonetic");
                    writer.WriteValue(value.Pronunciation.UsPhonetic);
                }
            }

            if (value.CharacterFeatures != null)
            {
                writer.WritePropertyName("StrokeCount");
                writer.WriteValue(value.CharacterFeatures.StrokeCount);
                writer.WritePropertyName("Radical");
                writer.WriteValue(value.CharacterFeatures.Radical);
                writer.WritePropertyName("Structure");
                writer.WriteValue(value.CharacterFeatures.Structure);
            }

            if (value.WordFeatures != null)
            {
                writer.WritePropertyName("PartOfSpeech");
                writer.WriteValue(value.WordFeatures.PartOfSpeech);
                writer.WritePropertyName("WordForms");
                writer.WriteValue(value.WordFeatures.WordForms);
                writer.WritePropertyName("Collocations");
                writer.WriteValue(value.WordFeatures.Collocations);
                writer.WritePropertyName("SyllableBreakdown");
                writer.WriteValue(value.WordFeatures.SyllableBreakdown);
            }

            writer.WritePropertyName("Status");
            writer.WriteValue(value.Status.ToString());
            writer.WritePropertyName("ReviewCount");
            writer.WriteValue(value.ReviewCount);
            if (value.LastReviewedAt.HasValue)
            {
                writer.WritePropertyName("LastReviewedAt");
                writer.WriteValue(value.LastReviewedAt.Value);
            }

            writer.WritePropertyName("ExtendedProperties");
            writer.WriteRawValue(value.ExtendedProperties);

            writer.WriteEndObject();
        }
    }

    public class LearningItemListJsonConverter : JsonConverter<List<LearningItem>>
    {
        private readonly LearningItemJsonConverter _itemConverter = new LearningItemJsonConverter();

        public override List<LearningItem>? ReadJson(JsonReader reader, Type objectType, List<LearningItem>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonException($"Expected StartArray token, got {reader.TokenType}");

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