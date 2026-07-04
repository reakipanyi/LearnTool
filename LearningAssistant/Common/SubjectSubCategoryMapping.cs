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
                  SubCategoryType.EnglishSentence, SubCategoryType.EnglishComprehensive } }
        };

        private static readonly Dictionary<string, SubjectType> _subjectStringMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Chinese", SubjectType.Chinese },
            { "English", SubjectType.English },
            { "中文", SubjectType.Chinese },
            { "英语", SubjectType.English },
            { "语文", SubjectType.Chinese }
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
            { "识字", SubCategoryType.ChineseCharacter },
            { "短语", SubCategoryType.ChinesePhrase },
            { "成语", SubCategoryType.ChineseIdiom },
            { "诗词", SubCategoryType.ChinesePoem },
            { "语文综合", SubCategoryType.ChineseComprehensive },
            { "英语单词", SubCategoryType.EnglishWord },
            { "英语短语", SubCategoryType.EnglishPhrase },
            { "英语句子", SubCategoryType.EnglishSentence },
            { "英语综合", SubCategoryType.EnglishComprehensive }
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