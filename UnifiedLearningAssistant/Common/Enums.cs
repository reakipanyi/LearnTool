
namespace UnifiedLearningAssistant.Common
{
    public enum LanguageType
    {
        Chinese,
        English
    }

    public enum LearningModeType
    {
        Study,
        Quick
    }

    public enum SubCategoryType
    {
        ChineseCharacter,
        ChineseWordCombination,
        ChinesePhrase,
        ChineseIdiom,
        ChinesePoem,
        ChineseComprehensive,
        EnglishWord,
        EnglishPhrase,
        EnglishSentence,
        EnglishComprehensive
    }

    public enum SortOrderType
    {
        Sequential,
        Random
    }

    public static class EnumExtensions
    {
        public static string ToDisplayString(this LanguageType language)
        {
            return language switch
            {
                LanguageType.Chinese => "中文",
                LanguageType.English => "英语",
                _ => language.ToString()
            };
        }

        public static string ToDisplayString(this LearningModeType mode)
        {
            return mode switch
            {
                LearningModeType.Study => "学习模式",
                LearningModeType.Quick => "快速模式",
                _ => mode.ToString()
            };
        }

        public static string ToDisplayString(this SubCategoryType category)
        {
            return category switch
            {
                SubCategoryType.ChineseCharacter => "识字",
                SubCategoryType.ChineseWordCombination => "组词",
                SubCategoryType.ChinesePhrase => "短语",
                SubCategoryType.ChineseIdiom => "成语",
                SubCategoryType.ChinesePoem => "诗词",
                SubCategoryType.ChineseComprehensive => "语文综合",
                SubCategoryType.EnglishWord => "英语单词",
                SubCategoryType.EnglishPhrase => "英语短语",
                SubCategoryType.EnglishSentence => "英语句子",
                SubCategoryType.EnglishComprehensive => "英语综合",
                _ => category.ToString()
            };
        }

        public static string ToDisplayString(this SortOrderType sortOrder)
        {
            return sortOrder switch
            {
                SortOrderType.Sequential => "顺序",
                SortOrderType.Random => "Random",
                _ => sortOrder.ToString()
            };
        }

        public static LanguageType ToLanguageType(this string displayString)
        {
            return displayString switch
            {
                "中文" => LanguageType.Chinese,
                "英语" => LanguageType.English,
                _ => throw new ArgumentOutOfRangeException(nameof(displayString))
            };
        }

        public static LearningModeType ToLearningModeType(this string displayString)
        {
            return displayString switch
            {
                "学习模式" => LearningModeType.Study,
                "快速模式" => LearningModeType.Quick,
                _ => throw new ArgumentOutOfRangeException(nameof(displayString))
            };
        }

        public static SubCategoryType ToSubCategoryType(this string displayString)
        {
            return displayString switch
            {
                "识字" => SubCategoryType.ChineseCharacter,
                "组词" => SubCategoryType.ChineseWordCombination,
                "短语" => SubCategoryType.ChinesePhrase,
                "成语" => SubCategoryType.ChineseIdiom,
                "诗词" => SubCategoryType.ChinesePoem,
                "语文综合" => SubCategoryType.ChineseComprehensive,
                "英语单词" => SubCategoryType.EnglishWord,
                "英语短语" => SubCategoryType.EnglishPhrase,
                "英语句子" => SubCategoryType.EnglishSentence,
                "英语综合" => SubCategoryType.EnglishComprehensive,
                _ => throw new ArgumentOutOfRangeException(nameof(displayString))
            };
        }

        public static SortOrderType ToSortOrderType(this string displayString)
        {
            return displayString switch
            {
                "顺序" => SortOrderType.Sequential,
                "Random" => SortOrderType.Random,
                _ => throw new ArgumentOutOfRangeException(nameof(displayString))
            };
        }
    }
}

