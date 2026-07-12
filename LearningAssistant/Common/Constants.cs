namespace LearningAssistant.Common
{
    public static class Constants
    {
        public static string DefaultUserId = "Default";

        public static class Subject
        {
            public const string Chinese = "语文";
            public const string English = "英语";
            public const string Math = "数学";
            public const string Physics = "物理";
            public const string Chemistry = "化学";
            public const string History = "历史";
            public const string Geography = "地理";
            public const string Biology = "生物";
        }

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
            public const string MathFormula = "公式定理";
            public const string MathExample = "例题解析";
            public const string MathConcept = "概念定义";
            public const string MathComprehensive = "数学综合";
            public const string PhysicsLaw = "物理定律";
            public const string PhysicsExperiment = "实验原理";
            public const string PhysicsDerivation = "公式推导";
            public const string PhysicsComprehensive = "物理综合";
            public const string ChemistryEquation = "化学方程式";
            public const string ChemistryElement = "元素性质";
            public const string ChemistryExperiment = "实验操作";
            public const string ChemistryComprehensive = "化学综合";
            public const string HistoryEvent = "历史事件";
            public const string HistoryPerson = "人物传记";
            public const string HistoryTimeline = "年代记忆";
            public const string HistoryComprehensive = "历史综合";
            public const string GeographyKnowledge = "地理知识";
            public const string GeographyMap = "地图解读";
            public const string GeographyClimate = "气候类型";
            public const string GeographyComprehensive = "地理综合";
            public const string BiologyConcept = "生物概念";
            public const string BiologyExperiment = "实验方法";
            public const string BiologyPhenomenon = "生命现象";
            public const string BiologyComprehensive = "生物综合";
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
            public const string MathFormula = "公式定理.json";
            public const string MathExample = "例题解析.json";
            public const string MathConcept = "概念定义.json";
            public const string MathComprehensive = "数学综合.json";
            public const string PhysicsLaw = "物理定律.json";
            public const string PhysicsExperiment = "实验原理.json";
            public const string PhysicsDerivation = "公式推导.json";
            public const string PhysicsComprehensive = "物理综合.json";
            public const string ChemistryEquation = "化学方程式.json";
            public const string ChemistryElement = "元素性质.json";
            public const string ChemistryExperiment = "实验操作.json";
            public const string ChemistryComprehensive = "化学综合.json";
            public const string HistoryEvent = "历史事件.json";
            public const string HistoryPerson = "人物传记.json";
            public const string HistoryTimeline = "年代记忆.json";
            public const string HistoryComprehensive = "历史综合.json";
            public const string GeographyKnowledge = "地理知识.json";
            public const string GeographyMap = "地图解读.json";
            public const string GeographyClimate = "气候类型.json";
            public const string GeographyComprehensive = "地理综合.json";
            public const string BiologyConcept = "生物概念.json";
            public const string BiologyExperiment = "实验方法.json";
            public const string BiologyPhenomenon = "生命现象.json";
            public const string BiologyComprehensive = "生物综合.json";
        }

        public static class ExcludedFileKeywords
        {
            public static readonly string[] WordBankExclusions = 
            {
                "learning_paths",
                "recommendation",
                "feedback",
                "wrong_answers",
                "_history",
                "_progress"
            };
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
