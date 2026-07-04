using LearningAssistant.Common;
using LearningAssistant.Models.Learning;

namespace LearningAssistant.Services.Learning
{
    public static class LearningItemFormatter
    {
        public static string FormatDisplayText(LearningItem item)
        {
            var parts = new List<string>();

            switch (item.SubCategory)
            {
                case SubCategoryType.ChineseCharacter:
                    AddIfNotEmpty(parts, "拼音", item.Pronunciation?.Main);
                    AddIfNotEmpty(parts, "释义", item.Meaning?.Content);
                    AddIfNotEmpty(parts, "笔画", item.CharacterFeatures?.StrokeCount + "画");
                    AddIfNotEmpty(parts, "部首", item.CharacterFeatures?.Radical);
                    AddIfNotEmpty(parts, "结构", item.CharacterFeatures?.Structure);
                    AddIfNotEmpty(parts, "组词", item.GetExtendedProperty<string>("Words"));
                    AddIfNotEmpty(parts, "例句", item.Example?.Content);
                    break;

                case SubCategoryType.ChinesePhrase:
                case SubCategoryType.ChineseIdiom:
                    AddIfNotEmpty(parts, "拼音", item.Pronunciation?.Main);
                    AddIfNotEmpty(parts, "释义", item.Meaning?.Content);
                    AddIfNotEmpty(parts, "例句", item.Example?.Content);
                    break;

                case SubCategoryType.ChinesePoem:
                    AddIfNotEmpty(parts, "作者", item.GetExtendedProperty<string>("Author"));
                    AddIfNotEmpty(parts, "朝代", item.GetExtendedProperty<string>("Dynasty"));
                    AddIfNotEmpty(parts, "内容", item.GetExtendedProperty<string>("Content"));
                    break;

                case SubCategoryType.EnglishWord:
                    AddIfNotEmpty(parts, "词性", item.WordFeatures?.PartOfSpeech);
                    AddIfNotEmpty(parts, "音标", item.Pronunciation?.Main);
                    AddIfNotEmpty(parts, "英式", item.Pronunciation?.UkPhonetic);
                    AddIfNotEmpty(parts, "美式", item.Pronunciation?.UsPhonetic);
                    AddIfNotEmpty(parts, "拼读", item.WordFeatures?.SyllableBreakdown);
                    AddIfNotEmpty(parts, "释义", item.Meaning?.Content);
                    AddIfNotEmpty(parts, "词形", item.WordFeatures?.WordForms);
                    AddIfNotEmpty(parts, "搭配", item.WordFeatures?.Collocations);
                    AddIfNotEmpty(parts, "例句", item.Example?.Content);
                    AddIfNotEmpty(parts, "例句翻译", item.Example?.Translation);
                    break;

                case SubCategoryType.EnglishPhrase:
                    AddIfNotEmpty(parts, "音标", item.Pronunciation?.Main);
                    AddIfNotEmpty(parts, "释义", item.Meaning?.Content);
                    AddIfNotEmpty(parts, "例句", item.Example?.Content);
                    break;

                case SubCategoryType.EnglishSentence:
                    AddIfNotEmpty(parts, "翻译", item.Meaning?.Content);
                    break;

                case SubCategoryType.ChineseComprehensive:
                case SubCategoryType.EnglishComprehensive:
                    AddIfNotEmpty(parts, "内容", item.MainContent);
                    AddIfNotEmpty(parts, "释义", item.Meaning?.Content);
                    break;
            }

            return string.Join("\n", parts);
        }

        public static string FormatDisplayStruct(LearningItem item)
        {
            var parts = new List<string>();

            switch (item.SubCategory)
            {
                case SubCategoryType.ChineseCharacter:
                    AddIfNotEmpty(parts, "拼音:?");
                    AddIfNotEmpty(parts, "释义:?");
                    AddIfNotEmpty(parts, "笔画:?");
                    AddIfNotEmpty(parts, "部首:?");
                    AddIfNotEmpty(parts, "结构:?");
                    AddIfNotEmpty(parts, "组词:?");
                    AddIfNotEmpty(parts, "例句:?");
                    break;

                case SubCategoryType.ChinesePhrase:
                case SubCategoryType.ChineseIdiom:
                    AddIfNotEmpty(parts, "拼音:?");
                    AddIfNotEmpty(parts, "释义:?");
                    AddIfNotEmpty(parts, "例句:?");
                    break;

                case SubCategoryType.EnglishWord:
                    AddIfNotEmpty(parts, "词性:?");
                    AddIfNotEmpty(parts, "音标:?");
                    AddIfNotEmpty(parts, "释义:?");
                    AddIfNotEmpty(parts, "词形:?");
                    AddIfNotEmpty(parts, "搭配:?");
                    AddIfNotEmpty(parts, "例句:?");
                    break;

                case SubCategoryType.EnglishPhrase:
                    AddIfNotEmpty(parts, "音标:?");
                    AddIfNotEmpty(parts, "释义:?");
                    AddIfNotEmpty(parts, "例句:?");
                    break;

                case SubCategoryType.EnglishSentence:
                    AddIfNotEmpty(parts, "翻译:?");
                    break;
            }

            return string.Join("\n", parts);
        }

        private static void AddIfNotEmpty(List<string> parts, string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                parts.Add($"{label}: {value}");
        }

        private static void AddIfNotEmpty(List<string> parts, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                parts.Add(value);
        }
    }
}