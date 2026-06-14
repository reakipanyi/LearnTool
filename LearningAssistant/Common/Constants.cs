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
            public const string Random = "随机";
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

        /// <summary>
        /// 缓存持续时间（分钟）
        /// </summary>
        public static class CacheDuration
        {
            /// <summary>
            /// AI解释缓存：7天
            /// </summary>
            public const int ExplanationMinutes = 7 * 24 * 60;

            /// <summary>
            /// AI问答缓存：3天
            /// </summary>
            public const int QAMinutes = 3 * 24 * 60;

            /// <summary>
            /// 练习题缓存：3天
            /// </summary>
            public const int ExerciseMinutes = 3 * 24 * 60;

            /// <summary>
            /// 文本总结缓存：1小时
            /// </summary>
            public const int SummarizeMinutes = 60;
        }
    }

}
