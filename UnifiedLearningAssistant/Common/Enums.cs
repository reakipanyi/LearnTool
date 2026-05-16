
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
                LanguageType.Chinese =&gt; "中文",
                LanguageType.English =&gt; "英语",
                _ =&gt; language.ToString()
            };
        }

        public static string ToDisplayString(this LearningModeType mode)
        {
            return mode switch
            {
                LearningModeType.Study =&gt; "学习模式",
                LearningModeType.Quick =&gt; "快速模式",
                _ =&gt; mode.ToString()
            };
        }

        public static string ToDisplayString(this SubCategoryType category)
        {
            return category switch
            {
                SubCategoryType.ChineseCharacter =&gt; "识字",
                SubCategoryType.ChineseWordCombination =&gt; "组词",
                SubCategoryType.ChinesePhrase =&gt; "短语",
                SubCategoryType.ChineseIdiom =&gt; "成语",
                SubCategoryType.ChinesePoem =&gt; "诗词",
                SubCategoryType.ChineseComprehensive =&gt; "语文综合",
                SubCategoryType.EnglishWord =&gt; "英语单词",
                SubCategoryType.EnglishPhrase =&gt; "英语短语",
                SubCategoryType.EnglishSentence =&gt; "英语句子",
                SubCategoryType.EnglishComprehensive =&gt; "英语综合",
                _ =&gt; category.ToString()
            };
        }

        public static string ToDisplayString(this SortOrderType sortOrder)
        {
            return sortOrder switch
            {
                SortOrderType.Sequential =&gt; "顺序",
                SortOrderType.Random =&gt; "Random",
                _ =&gt; sortOrder.ToString()
            };
        }

        public static LanguageType ToLanguageType(this string displayString)
        {
            return displayString switch
            {
                "中文" =&gt; LanguageType.Chinese,
                "英语" =&gt; LanguageType.English,
                _ =&gt; throw new ArgumentOutOfRangeException(nameof(displayString))
            };
        }

        public static LearningModeType ToLearningModeType(this string displayString)
        {
            return displayString switch
            {
                "学习模式" =&gt; LearningModeType.Study,
                "快速模式" =&gt; LearningModeType.Quick,
                _ =&gt; throw new ArgumentOutOfRangeException(nameof(displayString))
            };
        }

        public static SubCategoryType ToSubCategoryType(this string displayString)
        {
            return displayString switch
            {
                "识字" =&gt; SubCategoryType.ChineseCharacter,
                "组词" =&gt; SubCategoryType.ChineseWordCombination,
                "短语" =&gt; SubCategoryType.ChinesePhrase,
                "成语" =&gt; SubCategoryType.ChineseIdiom,
                "诗词" =&gt; SubCategoryType.ChinesePoem,
                "语文综合" =&gt; SubCategoryType.ChineseComprehensive,
                "英语单词" =&gt; SubCategoryType.EnglishWord,
                "英语短语" =&gt; SubCategoryType.EnglishPhrase,
                "英语句子" =&gt; SubCategoryType.EnglishSentence,
                "英语综合" =&gt; SubCategoryType.EnglishComprehensive,
                _ =&gt; throw new ArgumentOutOfRangeException(nameof(displayString))
            };
        }

        public static SortOrderType ToSortOrderType(this string displayString)
        {
            return displayString switch
            {
                "顺序" =&gt; SortOrderType.Sequential,
                "Random" =&gt; SortOrderType.Random,
                _ =&gt; throw new ArgumentOutOfRangeException(nameof(displayString))
            };
        }
    }
}

