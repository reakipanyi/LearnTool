using LearningAssistant.Models.Learning;
using LearningAssistant.Models.Learning.Status;
using LearningAssistant.Models.Learning.ValueObjects;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LearningAssistant.Common
{
    public class LearningItemJsonConverter : JsonConverter<LearningItem>
    {
        private const string TypePropertyName = "$type";

        public override LearningItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"Expected StartObject token, got {reader.TokenType}");

            var item = new LearningItem();
            string? typeName = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName?.ToLowerInvariant())
                {
                    case "id":
                        item.Id = reader.GetString() ?? Guid.NewGuid().ToString();
                        break;
                    case "createdat":
                        item.CreatedAt = reader.TryGetDateTime(out var createdAt) ? createdAt : DateTime.Now;
                        break;
                    case "updatedat":
                        item.UpdatedAt = reader.TryGetDateTime(out var updatedAt) ? updatedAt : DateTime.Now;
                        break;
                    case "subject":
                        if (Enum.TryParse(reader.GetString(), out SubjectType subject))
                            item.Subject = subject;
                        break;
                    case "subcategory":
                        if (Enum.TryParse(reader.GetString(), out SubCategoryType subCategory))
                            item.SubCategory = subCategory;
                        break;
                    case "maincontent":
                    case "word":
                    case "character":
                    case "phrase":
                    case "sentence":
                    case "idiom":
                    case "poem":
                    case "title":
                    case "rule":
                        if (string.IsNullOrEmpty(item.MainContent))
                            item.MainContent = reader.GetString() ?? string.Empty;
                        break;
                    case "meaning":
                    case "chinesemeaning":
                    case "content":
                    case "explanation":
                        var meaningContent = reader.GetString();
                        if (!string.IsNullOrWhiteSpace(meaningContent) && item.Meaning == null)
                            item.Meaning = Meaning.Create(meaningContent);
                        break;
                    case "example":
                        var exampleContent = reader.GetString();
                        if (!string.IsNullOrWhiteSpace(exampleContent))
                            item.Example = Example.Create(exampleContent);
                        break;
                    case "exampletranslation":
                        if (item.Example != null)
                            item.Example = Example.Create(item.Example.Content, reader.GetString());
                        break;
                    case "phonetic":
                    case "pinyin":
                        var pronunciation = reader.GetString();
                        if (!string.IsNullOrWhiteSpace(pronunciation) && item.Pronunciation == null)
                            item.Pronunciation = Pronunciation.Create(pronunciation);
                        break;
                    case "ukphonetic":
                        if (item.Pronunciation != null)
                            item.Pronunciation = Pronunciation.Create(item.Pronunciation.Main, reader.GetString());
                        break;
                    case "usphonetic":
                        if (item.Pronunciation != null)
                            item.Pronunciation = Pronunciation.Create(
                                item.Pronunciation.Main,
                                item.Pronunciation.UkPhonetic,
                                reader.GetString());
                        break;
                    case "strokecount":
                    case "radical":
                    case "structure":
                        if (item.CharacterFeatures == null)
                        {
                            item.CharacterFeatures = CharacterFeatures.Create(
                                propertyName.Equals("strokecount", StringComparison.OrdinalIgnoreCase) ? reader.GetString() ?? string.Empty : string.Empty,
                                propertyName.Equals("radical", StringComparison.OrdinalIgnoreCase) ? reader.GetString() ?? string.Empty : string.Empty,
                                propertyName.Equals("structure", StringComparison.OrdinalIgnoreCase) ? reader.GetString() ?? string.Empty : string.Empty);
                        }
                        else
                        {
                            if (propertyName.Equals("strokecount", StringComparison.OrdinalIgnoreCase))
                                item.CharacterFeatures = CharacterFeatures.Create(reader.GetString() ?? string.Empty, item.CharacterFeatures.Radical, item.CharacterFeatures.Structure);
                            else if (propertyName.Equals("radical", StringComparison.OrdinalIgnoreCase))
                                item.CharacterFeatures = CharacterFeatures.Create(item.CharacterFeatures.StrokeCount, reader.GetString() ?? string.Empty, item.CharacterFeatures.Structure);
                            else if (propertyName.Equals("structure", StringComparison.OrdinalIgnoreCase))
                                item.CharacterFeatures = CharacterFeatures.Create(item.CharacterFeatures.StrokeCount, item.CharacterFeatures.Radical, reader.GetString() ?? string.Empty);
                        }
                        break;
                    case "partofspeech":
                    case "wordforms":
                    case "collocations":
                    case "syllablebreakdown":
                        if (item.WordFeatures == null)
                        {
                            item.WordFeatures = WordFeatures.Create(
                                propertyName.Equals("partofspeech", StringComparison.OrdinalIgnoreCase) ? reader.GetString() ?? string.Empty : string.Empty,
                                propertyName.Equals("wordforms", StringComparison.OrdinalIgnoreCase) ? reader.GetString() ?? string.Empty : string.Empty,
                                propertyName.Equals("collocations", StringComparison.OrdinalIgnoreCase) ? reader.GetString() ?? string.Empty : string.Empty,
                                propertyName.Equals("syllablebreakdown", StringComparison.OrdinalIgnoreCase) ? reader.GetString() ?? string.Empty : string.Empty);
                        }
                        else
                        {
                            if (propertyName.Equals("partofspeech", StringComparison.OrdinalIgnoreCase))
                                item.WordFeatures = WordFeatures.Create(reader.GetString() ?? string.Empty, item.WordFeatures.WordForms, item.WordFeatures.Collocations, item.WordFeatures.SyllableBreakdown);
                            else if (propertyName.Equals("wordforms", StringComparison.OrdinalIgnoreCase))
                                item.WordFeatures = WordFeatures.Create(item.WordFeatures.PartOfSpeech, reader.GetString() ?? string.Empty, item.WordFeatures.Collocations, item.WordFeatures.SyllableBreakdown);
                            else if (propertyName.Equals("collocations", StringComparison.OrdinalIgnoreCase))
                                item.WordFeatures = WordFeatures.Create(item.WordFeatures.PartOfSpeech, item.WordFeatures.WordForms, reader.GetString() ?? string.Empty, item.WordFeatures.SyllableBreakdown);
                            else if (propertyName.Equals("syllablebreakdown", StringComparison.OrdinalIgnoreCase))
                                item.WordFeatures = WordFeatures.Create(item.WordFeatures.PartOfSpeech, item.WordFeatures.WordForms, item.WordFeatures.Collocations, reader.GetString() ?? string.Empty);
                        }
                        break;
                    case "status":
                        if (Enum.TryParse(reader.GetString(), out LearningStatus status))
                            item.Status = status;
                        break;
                    case "reviewcount":
                        item.ReviewCount = reader.TryGetInt32(out var reviewCount) ? reviewCount : 0;
                        break;
                    case "lastreviewedat":
                        if (reader.TryGetDateTime(out var lastReviewedAt))
                            item.LastReviewedAt = lastReviewedAt;
                        break;
                    case "$type":
                        typeName = reader.GetString();
                        break;
                    case "extendedproperties":
                        using (var doc = JsonDocument.ParseValue(ref reader))
                            item.ExtendedProperties = doc.RootElement.GetRawText();
                        break;
                }
            }

            if (!string.IsNullOrEmpty(typeName) && item.SubCategory == 0)
                item.SubCategory = InferSubCategoryFromTypeName(typeName);

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
            "$type", "Status", "ReviewCount", "LastReviewedAt", "ExtendedProperties"
        };

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

        public override void Write(Utf8JsonWriter writer, LearningItem? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            writer.WriteString("Id", value.Id);
            writer.WriteString("CreatedAt", value.CreatedAt);
            writer.WriteString("UpdatedAt", value.UpdatedAt);
            writer.WriteString("Subject", value.Subject.ToString());
            writer.WriteString("SubCategory", value.SubCategory.ToString());
            writer.WriteString("MainContent", value.MainContent);

            if (value.Meaning != null)
                writer.WriteString("Meaning", value.Meaning.Content);

            if (value.Example != null)
            {
                writer.WriteString("Example", value.Example.Content);
                if (!string.IsNullOrWhiteSpace(value.Example.Translation))
                    writer.WriteString("ExampleTranslation", value.Example.Translation);
            }

            if (value.Pronunciation != null)
            {
                writer.WriteString("Phonetic", value.Pronunciation.Main);
                if (!string.IsNullOrWhiteSpace(value.Pronunciation.UkPhonetic))
                    writer.WriteString("UkPhonetic", value.Pronunciation.UkPhonetic);
                if (!string.IsNullOrWhiteSpace(value.Pronunciation.UsPhonetic))
                    writer.WriteString("UsPhonetic", value.Pronunciation.UsPhonetic);
            }

            if (value.CharacterFeatures != null)
            {
                writer.WriteString("StrokeCount", value.CharacterFeatures.StrokeCount);
                writer.WriteString("Radical", value.CharacterFeatures.Radical);
                writer.WriteString("Structure", value.CharacterFeatures.Structure);
            }

            if (value.WordFeatures != null)
            {
                writer.WriteString("PartOfSpeech", value.WordFeatures.PartOfSpeech);
                writer.WriteString("WordForms", value.WordFeatures.WordForms);
                writer.WriteString("Collocations", value.WordFeatures.Collocations);
                writer.WriteString("SyllableBreakdown", value.WordFeatures.SyllableBreakdown);
            }

            writer.WriteString("Status", value.Status.ToString());
            writer.WriteNumber("ReviewCount", value.ReviewCount);
            if (value.LastReviewedAt.HasValue)
                writer.WriteString("LastReviewedAt", value.LastReviewedAt.Value);

            writer.WritePropertyName("ExtendedProperties");
            writer.WriteRawValue(value.ExtendedProperties);

            writer.WriteEndObject();
        }
    }
}