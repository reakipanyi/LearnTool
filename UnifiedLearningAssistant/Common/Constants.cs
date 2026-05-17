namespace UnifiedLearningAssistant.Common
{
    public static class Constants
    {
        public static class Language
        {
            public const string Chinese = "中文";
            public const string English = "英语";
        }

        public static class LearningMode
        {
            public const string Study = "学习模式";
            public const string Quick = "快速模式";
        }

        public static class SubCategory
        {
            public const string ChineseCharacter = "识字";
            public const string ChineseWordCombination = "组词";
            public const string ChinesePhrase = "短语";
            public const string ChineseIdiom = "成语";
            public const string ChinesePoem = "诗词";
            public const string ChineseComprehensive = "语文综合";
            public const string EnglishWord = "英语单词";
            public const string EnglishPhrase = "英语短语";
            public const string EnglishSentence = "英语句子";
            public const string EnglishComprehensive = "英语综合";
        }

        public static class SortOrder
        {
            public const string Sequential = "顺序";
            public const string Random = "Random";
        }

        public static class FileName
        {
            public const string ChineseCharacter = "识字.json";
            public const string ChineseWordCombination = "组词.json";
            public const string ChinesePhrase = "短语.json";
            public const string ChineseIdiom = "成语.json";
            public const string ChinesePoem = "诗词.json";
            public const string ChineseComprehensive = "语文综合.json";
            public const string EnglishWord = "英语单词.json";
            public const string EnglishPhrase = "英语短语.json";
            public const string EnglishSentence = "英语句子.json";
            public const string EnglishComprehensive = "英语综合.json";
        }

    }
    public static class Paths
    {
        public const string DataDirectory = "Data";
        public const string UsersDirectory = "Users";
        public const string CacheDirectory = "Cache";
        public const string AnnotationsDirectory = "Annotations";
        public const string TranslationsDirectory = "Translations";
        public const string SessionFile = "session.json";
        public const string SettingsFile = "settings.json";
    }
}
