namespace LearningAssistant.Common
{
    public static class SubjectSubCategoryMapping
    {
        private static readonly Dictionary<SubjectType, List<SubCategoryType>> _mapping = new()
        {
            { SubjectType.Chinese, new List<SubCategoryType> 
                { SubCategoryType.ChineseCharacter, SubCategoryType.ChinesePhrase, 
                  SubCategoryType.ChineseIdiom, SubCategoryType.ChinesePoem, 
                  SubCategoryType.ChineseComprehensive } },
            { SubjectType.English, new List<SubCategoryType> 
                { SubCategoryType.EnglishWord, SubCategoryType.EnglishPhrase, 
                  SubCategoryType.EnglishSentence, SubCategoryType.EnglishComprehensive } },
            { SubjectType.Math, new List<SubCategoryType> 
                { SubCategoryType.MathFormula, SubCategoryType.MathExample, 
                  SubCategoryType.MathConcept, SubCategoryType.MathComprehensive } },
            { SubjectType.Physics, new List<SubCategoryType> 
                { SubCategoryType.PhysicsLaw, SubCategoryType.PhysicsExperiment, 
                  SubCategoryType.PhysicsDerivation, SubCategoryType.PhysicsComprehensive } },
            { SubjectType.Chemistry, new List<SubCategoryType> 
                { SubCategoryType.ChemistryEquation, SubCategoryType.ChemistryElement, 
                  SubCategoryType.ChemistryExperiment, SubCategoryType.ChemistryComprehensive } },
            { SubjectType.History, new List<SubCategoryType> 
                { SubCategoryType.HistoryEvent, SubCategoryType.HistoryPerson, 
                  SubCategoryType.HistoryTimeline, SubCategoryType.HistoryComprehensive } },
            { SubjectType.Geography, new List<SubCategoryType> 
                { SubCategoryType.GeographyKnowledge, SubCategoryType.GeographyMap, 
                  SubCategoryType.GeographyClimate, SubCategoryType.GeographyComprehensive } },
            { SubjectType.Biology, new List<SubCategoryType> 
                { SubCategoryType.BiologyConcept, SubCategoryType.BiologyExperiment, 
                  SubCategoryType.BiologyPhenomenon, SubCategoryType.BiologyComprehensive } }
        };

        private static readonly Dictionary<string, SubjectType> _subjectStringMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Chinese", SubjectType.Chinese },
            { "English", SubjectType.English },
            { "Math", SubjectType.Math },
            { "Physics", SubjectType.Physics },
            { "Chemistry", SubjectType.Chemistry },
            { "History", SubjectType.History },
            { "Geography", SubjectType.Geography },
            { "Biology", SubjectType.Biology },
            { "中文", SubjectType.Chinese },
            { "英语", SubjectType.English },
            { "语文", SubjectType.Chinese },
            { "数学", SubjectType.Math },
            { "物理", SubjectType.Physics },
            { "化学", SubjectType.Chemistry },
            { "历史", SubjectType.History },
            { "地理", SubjectType.Geography },
            { "生物", SubjectType.Biology }
        };

        private static readonly Dictionary<string, SubCategoryType> _subCategoryStringMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "ChineseCharacter", SubCategoryType.ChineseCharacter },
            { "ChinesePhrase", SubCategoryType.ChinesePhrase },
            { "ChineseIdiom", SubCategoryType.ChineseIdiom },
            { "ChinesePoem", SubCategoryType.ChinesePoem },
            { "ChineseComprehensive", SubCategoryType.ChineseComprehensive },
            { "EnglishWord", SubCategoryType.EnglishWord },
            { "EnglishPhrase", SubCategoryType.EnglishPhrase },
            { "EnglishSentence", SubCategoryType.EnglishSentence },
            { "EnglishComprehensive", SubCategoryType.EnglishComprehensive },
            { "MathFormula", SubCategoryType.MathFormula },
            { "MathExample", SubCategoryType.MathExample },
            { "MathConcept", SubCategoryType.MathConcept },
            { "MathComprehensive", SubCategoryType.MathComprehensive },
            { "PhysicsLaw", SubCategoryType.PhysicsLaw },
            { "PhysicsExperiment", SubCategoryType.PhysicsExperiment },
            { "PhysicsDerivation", SubCategoryType.PhysicsDerivation },
            { "PhysicsComprehensive", SubCategoryType.PhysicsComprehensive },
            { "ChemistryEquation", SubCategoryType.ChemistryEquation },
            { "ChemistryElement", SubCategoryType.ChemistryElement },
            { "ChemistryExperiment", SubCategoryType.ChemistryExperiment },
            { "ChemistryComprehensive", SubCategoryType.ChemistryComprehensive },
            { "HistoryEvent", SubCategoryType.HistoryEvent },
            { "HistoryPerson", SubCategoryType.HistoryPerson },
            { "HistoryTimeline", SubCategoryType.HistoryTimeline },
            { "HistoryComprehensive", SubCategoryType.HistoryComprehensive },
            { "GeographyKnowledge", SubCategoryType.GeographyKnowledge },
            { "GeographyMap", SubCategoryType.GeographyMap },
            { "GeographyClimate", SubCategoryType.GeographyClimate },
            { "GeographyComprehensive", SubCategoryType.GeographyComprehensive },
            { "BiologyConcept", SubCategoryType.BiologyConcept },
            { "BiologyExperiment", SubCategoryType.BiologyExperiment },
            { "BiologyPhenomenon", SubCategoryType.BiologyPhenomenon },
            { "BiologyComprehensive", SubCategoryType.BiologyComprehensive },
            { "识字", SubCategoryType.ChineseCharacter },
            { "短语", SubCategoryType.ChinesePhrase },
            { "成语", SubCategoryType.ChineseIdiom },
            { "诗词", SubCategoryType.ChinesePoem },
            { "语文综合", SubCategoryType.ChineseComprehensive },
            { "英语单词", SubCategoryType.EnglishWord },
            { "英语短语", SubCategoryType.EnglishPhrase },
            { "英语句子", SubCategoryType.EnglishSentence },
            { "英语综合", SubCategoryType.EnglishComprehensive },
            { "数学公式", SubCategoryType.MathFormula },
            { "数学例题", SubCategoryType.MathExample },
            { "数学概念", SubCategoryType.MathConcept },
            { "数学综合", SubCategoryType.MathComprehensive },
            { "物理定律", SubCategoryType.PhysicsLaw },
            { "物理实验", SubCategoryType.PhysicsExperiment },
            { "物理推导", SubCategoryType.PhysicsDerivation },
            { "物理综合", SubCategoryType.PhysicsComprehensive },
            { "化学方程式", SubCategoryType.ChemistryEquation },
            { "化学元素", SubCategoryType.ChemistryElement },
            { "化学实验", SubCategoryType.ChemistryExperiment },
            { "化学综合", SubCategoryType.ChemistryComprehensive },
            { "历史事件", SubCategoryType.HistoryEvent },
            { "历史人物", SubCategoryType.HistoryPerson },
            { "历史时间线", SubCategoryType.HistoryTimeline },
            { "历史综合", SubCategoryType.HistoryComprehensive },
            { "地理知识", SubCategoryType.GeographyKnowledge },
            { "地理地图", SubCategoryType.GeographyMap },
            { "地理气候", SubCategoryType.GeographyClimate },
            { "地理综合", SubCategoryType.GeographyComprehensive },
            { "生物概念", SubCategoryType.BiologyConcept },
            { "生物实验", SubCategoryType.BiologyExperiment },
            { "生物现象", SubCategoryType.BiologyPhenomenon },
            { "生物综合", SubCategoryType.BiologyComprehensive }
        };

        public static List<SubCategoryType> GetSubCategories(SubjectType subject)
            => _mapping.TryGetValue(subject, out var list) ? list : new List<SubCategoryType>();

        public static SubjectType GetSubject(SubCategoryType subCategory)
            => _mapping.FirstOrDefault(kv => kv.Value.Contains(subCategory)).Key;

        public static bool IsValidSubCategory(SubjectType subject, SubCategoryType subCategory)
            => GetSubCategories(subject).Contains(subCategory);

        public static SubjectType ParseSubject(string subjectString)
        {
            if (Enum.TryParse<SubjectType>(subjectString, true, out var result))
                return result;

            return _subjectStringMap.TryGetValue(subjectString, out var mapped) 
                ? mapped 
                : SubjectType.Chinese;
        }

        public static SubCategoryType ParseSubCategory(string subCategoryString)
        {
            if (Enum.TryParse<SubCategoryType>(subCategoryString, true, out var result))
                return result;

            return _subCategoryStringMap.TryGetValue(subCategoryString, out var mapped) 
                ? mapped 
                : SubCategoryType.ChineseCharacter;
        }

        public static bool TryParseSubject(string subjectString, out SubjectType subject)
        {
            if (Enum.TryParse<SubjectType>(subjectString, true, out subject))
                return true;

            return _subjectStringMap.TryGetValue(subjectString, out subject);
        }

        public static bool TryParseSubCategory(string subCategoryString, out SubCategoryType subCategory)
        {
            if (Enum.TryParse<SubCategoryType>(subCategoryString, true, out subCategory))
                return true;

            return _subCategoryStringMap.TryGetValue(subCategoryString, out subCategory);
        }
    }
}