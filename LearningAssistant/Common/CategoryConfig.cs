using LearningAssistant.Models.Learning;

namespace LearningAssistant.Common
{
    public static class CategoryConfig
    {
        /// <summary>
        /// 来自 SubjectTemplates.json 的字段显示名缓存，按 SubCategoryType 枚举名（如 MathFormula）索引。
        /// 由 SubjectTemplateService 启动时通过 <see cref="InitializeJsonFieldNames"/> 推送，作为字段中文显示名的权威来源。
        /// </summary>
        private static Dictionary<string, Dictionary<string, string>> _jsonFieldNamesByEnum =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 用 SubjectTemplates.json 的 fieldNames 初始化缓存，使列标题等显示名以 JSON 为准。
        /// </summary>
        /// <param name="fieldNamesByEnumName">键为 SubCategoryType 枚举名，值为该类别 字段键→中文名 映射。</param>
        public static void InitializeJsonFieldNames(Dictionary<string, Dictionary<string, string>> fieldNamesByEnumName)
        {
            _jsonFieldNamesByEnum = fieldNamesByEnumName ?? new(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// SubCategoryType 枚举名 → Constants.SubCategory 中文值 的映射。
        /// 调用方传入的 category 为枚举名（如 MathFormula），而 ColumnHeaders 以中文值（如 公式定理）为键，
        /// 需通过此映射转换后再查 ColumnHeaders，否则 fallback 永远不命中。
        /// </summary>
        private static readonly Dictionary<string, string> _enumToChineseSubCategory = BuildEnumToChineseSubCategory();

        private static Dictionary<string, string> BuildEnumToChineseSubCategory()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in typeof(Constants.SubCategory).GetFields())
            {
                if (f.IsLiteral && !f.IsInitOnly)
                {
                    var value = f.GetValue(null) as string;
                    if (value != null)
                        map[f.Name] = value;
                }
            }
            return map;
        }

        /// <summary>
        /// 尝试将 SubCategoryType 枚举名转换为 Constants.SubCategory 中文值。
        /// </summary>
        private static bool TryGetChineseSubCategory(string? enumName, out string chineseValue)
        {
            if (!string.IsNullOrEmpty(enumName) && _enumToChineseSubCategory.TryGetValue(enumName, out var v))
            {
                chineseValue = v;
                return true;
            }
            chineseValue = enumName ?? string.Empty;
            return false;
        }

        public static readonly Dictionary<string, Dictionary<string, string>> ColumnHeaders = new()
        {
            { Constants.SubCategory.ChineseCharacter, new Dictionary<string, string>
                { { "Character", "汉字" }, { "Pinyin", "拼音" }, { "Meaning", "释义" }, { "StrokeCount", "笔画数" }, { "Radical", "部首" }, { "StrokeOrder", "笔顺" }, { "Words", "组词" }, { "SimilarCharacters", "形近字" }, { "Synonyms", "近义词" }, { "Antonyms", "反义词" }, { "CommonMistakes", "易错点" }, { "ExampleSentence", "例句" }, { "CharacterLevel", "字级" }, { "Structure", "结构" }, { "CharacterFormation", "造字法" }, { "OtherPronunciations", "其他读音" }, { "Id", "ID" }, { "CreatedAt", "创建时间" }, { "UpdatedAt", "更新时间" } } },
            { Constants.SubCategory.ChineseIdiom, new Dictionary<string, string>
                { { "Idiom", "成语" }, { "Pinyin", "拼音" }, { "Meaning", "释义" }, { "Origin", "出处" }, { "Example", "例句" } } },
            { Constants.SubCategory.ChinesePhrase, new Dictionary<string, string>
                { { "Phrase", "短语" }, { "Pinyin", "拼音" }, { "Meaning", "释义" }, { "Example", "例句" } } },
            { Constants.SubCategory.ChinesePoem, new Dictionary<string, string>
                { { "Title", "诗名" }, { "Author", "作者" }, { "Dynasty", "朝代" }, { "Verses", "诗句" }, { "Annotation", "注释" } } },
            { Constants.SubCategory.ChineseComprehensive, new Dictionary<string, string>
                { { "Title", "课文标题" }, { "Content", "课文内容" }, { "Questions", "课后习题" }, { "Question", "题目" }, { "Answer", "答案" }, { "Analysis", "解析" } } },
            { Constants.SubCategory.EnglishWord, new Dictionary<string, string>
                { { "Word", "单词" }, { "Phonetic", "音标" }, { "PartOfSpeech", "词性" }, { "SyllableBreakdown", "音节拼读" }, { "Meaning", "中文释义" }, { "Example", "例句" }, { "Synonyms", "近义词" }, { "Antonyms", "反义词" }, { "Id", "ID" }, { "CreatedAt", "创建时间" }, { "UpdatedAt", "更新时间" } } },
            { Constants.SubCategory.EnglishPhrase, new Dictionary<string, string>
                { { "Phrase", "短语" }, { "Meaning", "中文释义" }, { "Example", "例句" } } },
            { Constants.SubCategory.EnglishSentence, new Dictionary<string, string>
                { { "Sentence", "句子" }, { "Translation", "中文翻译" }, { "Grammar", "语法点" } } },
            { Constants.SubCategory.EnglishComprehensive, new Dictionary<string, string>
                { { "Title", "文章标题" }, { "Content", "文章内容" }, { "Questions", "阅读理解题" }, { "Question", "题目" }, { "Answer", "答案" }, { "Analysis", "解析" } } },
            { Constants.SubCategory.MathFormula, new Dictionary<string, string>
                { { "Topic", "公式名称" }, { "Content", "公式表达式" }, { "KeyPoints", "公式说明" }, { "Principle", "适用条件" }, { "Example", "应用举例" }, { "Applications", "应用场景" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.MathExample, new Dictionary<string, string>
                { { "Topic", "例题标题" }, { "Content", "题目描述" }, { "Analysis", "解答过程" }, { "KeyPoints", "关键步骤" }, { "Example", "方法总结" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.MathConcept, new Dictionary<string, string>
                { { "Topic", "概念名称" }, { "Content", "定义" }, { "KeyPoints", "性质" }, { "Example", "举例说明" }, { "Note", "注意事项" }, { "Applications", "应用" }, { "Tags", "标签" } } },
            { Constants.SubCategory.MathComprehensive, new Dictionary<string, string>
                { { "Topic", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" }, { "Example", "典型例题" }, { "Analysis", "答案解析" }, { "Question", "问题" }, { "Answer", "答案" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.PhysicsLaw, new Dictionary<string, string>
                { { "Topic", "定律名称" }, { "Content", "定律内容" }, { "KeyPoints", "公式" }, { "Principle", "适用条件" }, { "Applications", "应用场景" }, { "Example", "实例" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.PhysicsExperiment, new Dictionary<string, string>
                { { "Topic", "实验名称" }, { "Content", "实验目的" }, { "KeyPoints", "实验器材" }, { "ExperimentSteps", "实验步骤" }, { "Analysis", "实验结论" }, { "Example", "实例" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.PhysicsDerivation, new Dictionary<string, string>
                { { "Topic", "公式名称" }, { "Content", "推导结果" }, { "KeyPoints", "推导步骤" }, { "Principle", "前提条件" }, { "Example", "应用实例" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.PhysicsComprehensive, new Dictionary<string, string>
                { { "Topic", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" }, { "Example", "典型例题" }, { "Analysis", "答案解析" }, { "Question", "问题" }, { "Answer", "答案" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.ChemistryEquation, new Dictionary<string, string>
                { { "Topic", "反应名称" }, { "Content", "化学方程式" }, { "KeyPoints", "反应条件" }, { "Principle", "反应原理" }, { "Example", "反应现象" }, { "Applications", "应用" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.ChemistryElement, new Dictionary<string, string>
                { { "Topic", "元素名称" }, { "Content", "元素符号" }, { "KeyPoints", "原子序数" }, { "Principle", "元素性质" }, { "Applications", "主要用途" }, { "Example", "实例" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.ChemistryExperiment, new Dictionary<string, string>
                { { "Topic", "实验名称" }, { "Content", "实验目的" }, { "KeyPoints", "实验器材" }, { "ExperimentSteps", "操作步骤" }, { "Analysis", "实验现象" }, { "Example", "实例" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.ChemistryComprehensive, new Dictionary<string, string>
                { { "Topic", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" }, { "Example", "典型例题" }, { "Analysis", "答案解析" }, { "Question", "问题" }, { "Answer", "答案" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.HistoryEvent, new Dictionary<string, string>
                { { "Topic", "事件名称" }, { "TimePeriod", "发生时间" }, { "RelatedPlaces", "发生地点" }, { "Background", "历史背景" }, { "Content", "事件经过" }, { "Impact", "历史影响" }, { "RelatedPeople", "相关人物" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.HistoryPerson, new Dictionary<string, string>
                { { "Topic", "人物姓名" }, { "TimePeriod", "所处朝代" }, { "Content", "生卒年月" }, { "KeyPoints", "主要成就" }, { "Analysis", "历史评价" }, { "Example", "代表作品" }, { "RelatedPlaces", "相关地点" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.HistoryTimeline, new Dictionary<string, string>
                { { "Topic", "时代名称" }, { "TimePeriod", "时间范围" }, { "KeyPoints", "重要事件" }, { "Content", "时代特征" }, { "RelatedPeople", "重要人物" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.HistoryComprehensive, new Dictionary<string, string>
                { { "Topic", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" }, { "Example", "典型例题" }, { "Analysis", "答案解析" }, { "Question", "问题" }, { "Answer", "答案" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.GeographyKnowledge, new Dictionary<string, string>
                { { "Topic", "地理名称" }, { "Category", "地理分类" }, { "Content", "地理描述" }, { "RelatedPlaces", "分布地区" }, { "KeyPoints", "主要特征" }, { "Example", "实例" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.GeographyMap, new Dictionary<string, string>
                { { "Topic", "地图名称" }, { "RelatedPlaces", "所属地区" }, { "Content", "地理特征" }, { "KeyPoints", "重要地点" }, { "Example", "读图技巧" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.GeographyClimate, new Dictionary<string, string>
                { { "Topic", "气候类型" }, { "RelatedPlaces", "分布地区" }, { "Content", "气候特征" }, { "Principle", "形成原因" }, { "KeyPoints", "植被类型" }, { "Example", "实例" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.GeographyComprehensive, new Dictionary<string, string>
                { { "Topic", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" }, { "Example", "典型例题" }, { "Analysis", "答案解析" }, { "Question", "问题" }, { "Answer", "答案" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.BiologyConcept, new Dictionary<string, string>
                { { "Topic", "概念名称" }, { "Content", "定义" }, { "Category", "分类" }, { "KeyPoints", "主要特征" }, { "Applications", "功能作用" }, { "Example", "实例" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.BiologyExperiment, new Dictionary<string, string>
                { { "Topic", "实验名称" }, { "Content", "实验目的" }, { "KeyPoints", "实验材料" }, { "ExperimentSteps", "实验步骤" }, { "Analysis", "实验结果" }, { "Example", "实例" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.BiologyPhenomenon, new Dictionary<string, string>
                { { "Topic", "现象名称" }, { "Content", "现象描述" }, { "Category", "现象类型" }, { "Principle", "产生原因" }, { "Example", "常见实例" }, { "Impact", "生物意义" }, { "Note", "备注" }, { "Tags", "标签" } } },
            { Constants.SubCategory.BiologyComprehensive, new Dictionary<string, string>
                { { "Topic", "知识点标题" }, { "Content", "知识讲解" }, { "KeyPoints", "要点归纳" }, { "Example", "典型例题" }, { "Analysis", "答案解析" }, { "Question", "问题" }, { "Answer", "答案" }, { "Note", "备注" }, { "Tags", "标签" } } }
        };

        public static readonly Dictionary<string, string> CommonColumnHeaders = new()
        {
            { "Id", "ID" }, { "CreatedAt", "创建时间" }, { "UpdatedAt", "更新时间" },
            { "Synonyms", "近义词" }, { "Antonyms", "反义词" }, { "CommonMistakes", "易错点" },
            { "ExampleSentence", "例句" }, { "OtherPronunciations", "其他读音" }
        };

        public static readonly Dictionary<string, string> CategoryTypeNames = new()
        {
            { Constants.SubCategory.ChineseCharacter, "识字" },
            { Constants.SubCategory.ChineseIdiom, "成语" },
            { Constants.SubCategory.ChinesePhrase, "短语" },
            { Constants.SubCategory.ChinesePoem, "诗词" },
            { Constants.SubCategory.ChineseComprehensive, "语文综合" },
            { Constants.SubCategory.EnglishWord, "英语单词" },
            { Constants.SubCategory.EnglishPhrase, "英语短语" },
            { Constants.SubCategory.EnglishSentence, "英语句子" },
            { Constants.SubCategory.EnglishComprehensive, "英语综合" },
            { Constants.SubCategory.MathFormula, "数学公式" },
            { Constants.SubCategory.MathExample, "数学例题" },
            { Constants.SubCategory.MathConcept, "数学概念" },
            { Constants.SubCategory.MathComprehensive, "数学综合" },
            { Constants.SubCategory.PhysicsLaw, "物理定律" },
            { Constants.SubCategory.PhysicsExperiment, "物理实验" },
            { Constants.SubCategory.PhysicsDerivation, "物理推导" },
            { Constants.SubCategory.PhysicsComprehensive, "物理综合" },
            { Constants.SubCategory.ChemistryEquation, "化学方程式" },
            { Constants.SubCategory.ChemistryElement, "化学元素" },
            { Constants.SubCategory.ChemistryExperiment, "化学实验" },
            { Constants.SubCategory.ChemistryComprehensive, "化学综合" },
            { Constants.SubCategory.HistoryEvent, "历史事件" },
            { Constants.SubCategory.HistoryPerson, "历史人物" },
            { Constants.SubCategory.HistoryTimeline, "历史时间线" },
            { Constants.SubCategory.HistoryComprehensive, "历史综合" },
            { Constants.SubCategory.GeographyKnowledge, "地理知识" },
            { Constants.SubCategory.GeographyMap, "地理地图" },
            { Constants.SubCategory.GeographyClimate, "地理气候" },
            { Constants.SubCategory.GeographyComprehensive, "地理综合" },
            { Constants.SubCategory.BiologyConcept, "生物概念" },
            { Constants.SubCategory.BiologyExperiment, "生物实验" },
            { Constants.SubCategory.BiologyPhenomenon, "生物现象" },
            { Constants.SubCategory.BiologyComprehensive, "生物综合" }
        };

        public static string GetChineseColumnName(string columnName, string? category)
        {
            // 优先查询 SubjectTemplates.json 的 fieldNames（以 JSON 为权威字段显示名来源）。
            // category 为 SubCategoryType 枚举名（如 MathFormula），由启动时初始化的映射提供。
            if (!string.IsNullOrEmpty(category) &&
                _jsonFieldNamesByEnum.TryGetValue(category, out var jsonNames) &&
                jsonNames.TryGetValue(columnName, out var jsonChineseName))
            {
                return jsonChineseName;
            }
            // ColumnHeaders 以 Constants.SubCategory 中文值（如 公式定理）为键，需将枚举名转换后再查。
            if (!string.IsNullOrEmpty(category) &&
                TryGetChineseSubCategory(category, out var chineseCat) &&
                ColumnHeaders.TryGetValue(chineseCat, out var headers) &&
                headers.TryGetValue(columnName, out var chineseName))
            {
                return chineseName;
            }
            if (CommonColumnHeaders.TryGetValue(columnName, out var commonName))
            {
                return commonName;
            }
            return columnName;
        }

        public static string GetEnglishColumnName(string columnName, string? category)
        {
            // ColumnHeaders 以 Constants.SubCategory 中文值为键，需将枚举名转换后再查。
            if (!string.IsNullOrEmpty(category) &&
                TryGetChineseSubCategory(category, out var chineseCat) &&
                ColumnHeaders.TryGetValue(chineseCat, out var headers))
            {
                foreach (var pair in headers)
                {
                    if (pair.Value == columnName)
                    {
                        return pair.Key;
                    }
                }
            }
            return columnName;
        }
    }
}