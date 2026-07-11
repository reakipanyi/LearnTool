using LearningAssistant.Common;
using LearningAssistant.Models.Learning;

namespace LearningAssistant.Services.Learning
{
    public static class LearningItemFormatter
    {
        public static List<ContentField> BuildFields(LearningItem item)
        {
            var fields = new List<ContentField>();
            int order = 0;

            string lang = item.Subject == SubjectType.Chinese ? "zh" : "en";

            switch (item.SubCategory)
            {
                case SubCategoryType.ChineseCharacter:
                    AddField(fields, ref order, "拼音", item.Pronunciation?.Main, item.Pronunciation?.Main, true, "zh");
                    AddField(fields, ref order, "释义", item.Meaning?.Content, language: "zh");
                    AddField(fields, ref order, "笔画", !string.IsNullOrWhiteSpace(item.CharacterFeatures?.StrokeCount) ? $"{item.CharacterFeatures.StrokeCount}画" : null);
                    AddField(fields, ref order, "部首", item.CharacterFeatures?.Radical);
                    AddField(fields, ref order, "结构", item.CharacterFeatures?.Structure);
                    AddField(fields, ref order, "组词", item.GetExtendedProperty<string>("Words"), language: "zh");
                    AddField(fields, ref order, "例句", item.Example?.Content, item.Example?.Content, language: "zh");
                    break;

                case SubCategoryType.ChinesePhrase:
                case SubCategoryType.ChineseIdiom:
                    AddField(fields, ref order, "拼音", item.Pronunciation?.Main, item.Pronunciation?.Main, true, "zh");
                    AddField(fields, ref order, "释义", item.Meaning?.Content, language: "zh");
                    AddField(fields, ref order, "例句", item.Example?.Content, item.Example?.Content, language: "zh");
                    break;

                case SubCategoryType.ChinesePoem:
                    AddField(fields, ref order, "作者", item.GetExtendedProperty<string>("Author"), language: "zh");
                    AddField(fields, ref order, "朝代", item.GetExtendedProperty<string>("Dynasty"));
                    AddField(fields, ref order, "内容", item.GetExtendedProperty<string>("Content"), language: "zh");
                    break;

                case SubCategoryType.EnglishWord:
                    AddField(fields, ref order, "词性", item.WordFeatures?.PartOfSpeech);
                    AddField(fields, ref order, "音标", item.Pronunciation?.Main, item.MainContent, true, "en");
                    if (!string.IsNullOrWhiteSpace(item.Pronunciation?.UkPhonetic))
                        AddField(fields, ref order, "英式", item.Pronunciation?.UkPhonetic, item.MainContent, true, "en");
                    if (!string.IsNullOrWhiteSpace(item.Pronunciation?.UsPhonetic))
                        AddField(fields, ref order, "美式", item.Pronunciation?.UsPhonetic, item.MainContent, true, "en");
                    AddField(fields, ref order, "拼读", item.WordFeatures?.SyllableBreakdown);
                    AddField(fields, ref order, "释义", item.Meaning?.Content);
                    AddField(fields, ref order, "词形", item.WordFeatures?.WordForms);
                    AddField(fields, ref order, "搭配", item.WordFeatures?.Collocations);
                    AddField(fields, ref order, "例句", item.Example?.Content, item.Example?.Content, language: "en");
                    AddField(fields, ref order, "例句翻译", item.Example?.Translation, language: "zh");
                    break;

                case SubCategoryType.EnglishPhrase:
                    AddField(fields, ref order, "音标", item.Pronunciation?.Main, item.MainContent, true, "en");
                    AddField(fields, ref order, "释义", item.Meaning?.Content);
                    AddField(fields, ref order, "例句", item.Example?.Content, item.Example?.Content, language: "en");
                    break;

                case SubCategoryType.EnglishSentence:
                    AddField(fields, ref order, "翻译", item.Meaning?.Content, language: "zh");
                    break;

                case SubCategoryType.ChineseComprehensive:
                    AddField(fields, ref order, "内容", item.MainContent, language: "zh");
                    AddField(fields, ref order, "释义", item.Meaning?.Content, language: "zh");
                    break;

                case SubCategoryType.EnglishComprehensive:
                    AddField(fields, ref order, "内容", item.MainContent, language: "en");
                    AddField(fields, ref order, "释义", item.Meaning?.Content);
                    break;

                case SubCategoryType.MathFormula:
                case SubCategoryType.MathExample:
                case SubCategoryType.MathConcept:
                case SubCategoryType.MathComprehensive:
                    AddField(fields, ref order, "内容", item.MainContent);
                    AddField(fields, ref order, "解释", item.Meaning?.Content);
                    AddField(fields, ref order, "示例", item.Example?.Content);
                    break;

                case SubCategoryType.PhysicsLaw:
                case SubCategoryType.PhysicsExperiment:
                case SubCategoryType.PhysicsDerivation:
                case SubCategoryType.PhysicsComprehensive:
                    AddField(fields, ref order, "内容", item.MainContent);
                    AddField(fields, ref order, "解释", item.Meaning?.Content);
                    AddField(fields, ref order, "公式", item.GetExtendedProperty<string>("Formula"));
                    AddField(fields, ref order, "示例", item.Example?.Content);
                    break;

                case SubCategoryType.ChemistryEquation:
                case SubCategoryType.ChemistryElement:
                case SubCategoryType.ChemistryExperiment:
                case SubCategoryType.ChemistryComprehensive:
                    AddField(fields, ref order, "内容", item.MainContent);
                    AddField(fields, ref order, "解释", item.Meaning?.Content);
                    AddField(fields, ref order, "方程式", item.GetExtendedProperty<string>("Equation"));
                    AddField(fields, ref order, "示例", item.Example?.Content);
                    break;

                case SubCategoryType.HistoryEvent:
                case SubCategoryType.HistoryPerson:
                case SubCategoryType.HistoryTimeline:
                case SubCategoryType.HistoryComprehensive:
                    AddField(fields, ref order, "内容", item.MainContent);
                    AddField(fields, ref order, "描述", item.Meaning?.Content);
                    AddField(fields, ref order, "时间", item.GetExtendedProperty<string>("Time"));
                    AddField(fields, ref order, "地点", item.GetExtendedProperty<string>("Location"));
                    break;

                case SubCategoryType.GeographyKnowledge:
                case SubCategoryType.GeographyMap:
                case SubCategoryType.GeographyClimate:
                case SubCategoryType.GeographyComprehensive:
                    AddField(fields, ref order, "内容", item.MainContent);
                    AddField(fields, ref order, "描述", item.Meaning?.Content);
                    AddField(fields, ref order, "位置", item.GetExtendedProperty<string>("Location"));
                    AddField(fields, ref order, "示例", item.Example?.Content);
                    break;

                case SubCategoryType.BiologyConcept:
                case SubCategoryType.BiologyExperiment:
                case SubCategoryType.BiologyPhenomenon:
                case SubCategoryType.BiologyComprehensive:
                    AddField(fields, ref order, "内容", item.MainContent);
                    AddField(fields, ref order, "解释", item.Meaning?.Content);
                    AddField(fields, ref order, "实验", item.GetExtendedProperty<string>("Experiment"));
                    AddField(fields, ref order, "示例", item.Example?.Content);
                    break;

                default:
                    AddField(fields, ref order, "内容", item.MainContent);
                    AddField(fields, ref order, "释义", item.Meaning?.Content);
                    break;
            }

            return fields.OrderBy(f => f.Order).ToList();
        }

        public static string FormatDisplayText(LearningItem item)
        {
            var fields = BuildFields(item);
            return string.Join("\n", fields.Select(f => $"{f.Label}: {f.Value}"));
        }

        public static string FormatDisplayStruct(LearningItem item)
        {
            var fields = BuildFields(item);
            return string.Join("\n", fields.Select(f => $"{f.Label}:?"));
        }

        private static void AddField(List<ContentField> fields, ref int order, string label, string? value, string? speakText = null, bool isPronunciation = false, string language = "en")
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                string actualSpeakText = speakText ?? value;
                string filteredSpeakText = FilterTextByLanguage(actualSpeakText, language);
                fields.Add(new ContentField(label, value.Trim(), filteredSpeakText.Trim(), isPronunciation, order++, language));
            }
        }

        private static string FilterTextByLanguage(string text, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            if (targetLanguage == "en")
            {
                return new string(text.Where(c => !IsChineseChar(c)).ToArray());
            }
            else if (targetLanguage == "zh")
            {
                return new string(text.Where(c => IsChineseChar(c) || !IsLatinChar(c)).ToArray());
            }

            return text;
        }

        private static bool IsChineseChar(char c)
        {
            return (c >= 0x4E00 && c <= 0x9FFF) || (c >= 0x3400 && c <= 0x4DBF);
        }

        private static bool IsLatinChar(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }
    }
}