using LearningAssistant.Models.Learning;

namespace LearningAssistant.Common
{
    public static class CategoryConfig
    {
        /// <summary>
        /// 来自 SubjectTemplates.json 各类别 fieldNames 的字段显示名缓存，按 SubCategoryType 枚举名（如 MathFormula）索引。
        /// 由 SubjectTemplateService 启动时通过 <see cref="InitializeJsonFieldNames"/> 推送，作为类别特定字段中文显示名的权威来源。
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
        /// 来自 SubjectTemplates.json 顶层 commonFieldNames 的跨类别通用字段显示名缓存（英文键→中文名）。
        /// 由 SubjectTemplateService 启动时通过 <see cref="InitializeCommonFieldNames"/> 推送，
        /// 作为类别特定 fieldNames 未命中时的兜底翻译（如 Id/CreatedAt/Meaning/Tags 等通用字段）。
        /// </summary>
        private static Dictionary<string, string> _commonFieldNames =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 用 SubjectTemplates.json 的 commonFieldNames 初始化通用字段翻译缓存。
        /// </summary>
        public static void InitializeCommonFieldNames(Dictionary<string, string> commonFieldNames)
        {
            _commonFieldNames = commonFieldNames ?? new(StringComparer.OrdinalIgnoreCase);
        }

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

        /// <summary>
        /// 获取字段的中文显示名。查询顺序：
        /// 1. 当前类别的 JSON fieldNames（类别特定翻译，权威来源）
        /// 2. 通用字段 commonFieldNames（跨类别兜底翻译）
        /// 3. 返回原英文字段名
        /// </summary>
        /// <param name="columnName">英文字段名（如 Meaning）</param>
        /// <param name="category">SubCategoryType 枚举名（如 MathFormula）</param>
        public static string GetChineseColumnName(string columnName, string? category)
        {
            // 1. 类别特定 fieldNames（SubjectTemplates.json）
            if (!string.IsNullOrEmpty(category) &&
                _jsonFieldNamesByEnum.TryGetValue(category, out var jsonNames) &&
                jsonNames.TryGetValue(columnName, out var jsonChineseName))
            {
                return jsonChineseName;
            }
            // 2. 跨类别通用字段（commonFieldNames）
            if (_commonFieldNames.TryGetValue(columnName, out var commonName))
            {
                return commonName;
            }
            // 3. 无翻译，返回原英文字段名
            return columnName;
        }

        /// <summary>
        /// 将中文显示名反向解析为英文字段名。查询顺序：
        /// 1. 当前类别的 JSON fieldNames 反向查找（中文值→英文键）
        /// 2. 通用字段 commonFieldNames 反向查找
        /// 3. 返回原中文输入
        /// </summary>
        public static string GetEnglishColumnName(string columnName, string? category)
        {
            // 1. 类别特定 fieldNames 反向查找
            if (!string.IsNullOrEmpty(category) &&
                _jsonFieldNamesByEnum.TryGetValue(category, out var jsonNames))
            {
                foreach (var pair in jsonNames)
                {
                    if (pair.Value == columnName)
                        return pair.Key;
                }
            }
            // 2. 通用字段反向查找
            foreach (var pair in _commonFieldNames)
            {
                if (pair.Value == columnName)
                    return pair.Key;
            }
            // 3. 无匹配，返回原输入
            return columnName;
        }
    }
}
