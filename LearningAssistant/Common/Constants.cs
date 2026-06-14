using System;

namespace LearningAssistant.Common
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
            public const string Study = "练习";
            public const string Quick = "复习";
        }

        public static class SubCategory
        {
            public const string ChineseCharacter = "识字";
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

    /// <summary>
    /// 路径常量 - 已过时，请使用 AppPaths
    /// </summary>
    [Obsolete("请使用 AppPaths 类代替")]
    public static class Paths
    {
        [Obsolete("请使用 AppPaths.DataDir")]
        public const string DataDirectory = "Data";
        
        [Obsolete("请使用 AppPaths.UsersDir")]
        public const string UsersDirectory = "users";
        
        [Obsolete("请使用 AppPaths.CacheDir")]
        public const string CacheDirectory = "cache";
        
        [Obsolete("请使用 AppPaths.AnnotationsDir")]
        public const string AnnotationsDirectory = "annotations";
        
        [Obsolete("请使用 AppPaths.TranslationsDir")]
        public const string TranslationsDirectory = "translations";
        
        [Obsolete("请使用 AppPaths.SessionDir")]
        public const string SessionFile = "session.json";
        
        [Obsolete("请使用 AppPaths.ConfigDir")]
        public const string SettingsFile = "settings.json";
        
        [Obsolete("请使用 AppPaths.BookmarksDir")]
        public const string BookmarksDirectory = "bookmarks";
        
        [Obsolete("请使用 AppPaths.HighlightsDir")]
        public const string HighlightsDirectory = "highlights";
    }
}
