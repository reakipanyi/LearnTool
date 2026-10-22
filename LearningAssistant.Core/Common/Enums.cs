
namespace LearningAssistant.Common
{
    public enum SubjectType
    {
        Unknown,
        Chinese,
        English,
        Math,
        Physics,
        Chemistry,
        History,
        Geography,
        Biology
    }

    public enum LearningModeType
    {
        Study,
        Quick,
        QuickReview
    }

    public enum SubCategoryType
    {
        Unknown,
        ChineseCharacter,
        ChinesePhrase,
        ChineseIdiom,
        ChinesePoem,
        ChineseComprehensive,
        EnglishWord,
        EnglishPhrase,
        EnglishSentence,
        EnglishComprehensive,
        MathFormula,
        MathExample,
        MathConcept,
        MathComprehensive,
        PhysicsLaw,
        PhysicsExperiment,
        PhysicsDerivation,
        PhysicsComprehensive,
        ChemistryEquation,
        ChemistryElement,
        ChemistryExperiment,
        ChemistryComprehensive,
        HistoryEvent,
        HistoryPerson,
        HistoryTimeline,
        HistoryComprehensive,
        GeographyKnowledge,
        GeographyMap,
        GeographyClimate,
        GeographyComprehensive,
        BiologyConcept,
        BiologyExperiment,
        BiologyPhenomenon,
        BiologyComprehensive
    }

    public enum SortOrderType
    {
        Sequential,
        Random
    }

    /// <summary>适龄段（All 表示通用，不限定年龄段）。</summary>
    public enum AgeRange
    {
        All = 0,
        Preschool = 1,
        PrimaryLow = 2,
        PrimaryHigh = 3,
        JuniorHigh = 4,
        SeniorHigh = 5,
        Adult = 6
    }

    /// <summary>重要等级，数值越大复习优先级越高。</summary>
    public enum ImportanceLevel
    {
        Extension = 1,
        Normal = 2,
        Key = 3,
        Core = 4
    }

    /// <summary>学习内容来源，用于来源筛选与回跳原文。</summary>
    public enum StudyContentSource
    {
        Manual = 0,
        PdfHighlight = 1,
        PdfAnnotation = 2,
        PdfShadowing = 3,
        PdfAiFlashcard = 4,
        Import = 5,
        Game = 6
    }


    public enum ThemeMode
    {
        Light,
        Dark,
        EyeCare,
        HighContrast
    }

    public static class EnumExtensions
    {
        public static string ToDisplayString(this AgeRange ageRange)
        {
            return ageRange switch
            {
                AgeRange.All => "通用",
                AgeRange.Preschool => "学前",
                AgeRange.PrimaryLow => "小学低年级",
                AgeRange.PrimaryHigh => "小学高年级",
                AgeRange.JuniorHigh => "初中",
                AgeRange.SeniorHigh => "高中",
                AgeRange.Adult => "成人",
                _ => ageRange.ToString()
            };
        }

        public static string ToDisplayString(this ImportanceLevel importance)
        {
            return importance switch
            {
                ImportanceLevel.Extension => "拓展",
                ImportanceLevel.Normal => "一般",
                ImportanceLevel.Key => "重点",
                ImportanceLevel.Core => "核心",
                _ => importance.ToString()
            };
        }

        public static string ToDisplayString(this StudyContentSource source)
        {
            return source switch
            {
                StudyContentSource.Manual => "手动",
                StudyContentSource.PdfHighlight => "PDF高亮",
                StudyContentSource.PdfAnnotation => "PDF标注",
                StudyContentSource.PdfShadowing => "跟读",
                StudyContentSource.PdfAiFlashcard => "PDF闪卡",
                StudyContentSource.Import => "导入",
                StudyContentSource.Game => "游戏",
                _ => source.ToString()
            };
        }

        public static string ToDisplayString(this SubjectType language)
        {
            return language switch
            {
                SubjectType.Chinese => "中文",
                SubjectType.English => "英语",
                _ => language.ToString()
            };
        }

        public static string ToLanguageCode(this SubjectType subject)
        {
            return subject switch
            {
                SubjectType.Chinese => "zh",
                SubjectType.English => "en",
                _ => "en"
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

        public static SubjectType ToLanguageType(this string displayString)
        {
            return displayString switch
            {
                "中文" => SubjectType.Chinese,
                "英语" => SubjectType.English,
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

