using System.Reflection;
using System.Linq;

namespace LearningAssistant.Common
{
    public static class SubjectSubCategoryMapping
    {
        /// <summary>
        /// 科目 → 子类别映射，通过反射从枚举命名约定自动构建（单一数据源，无需手动维护）。
        /// SubCategoryType 枚举名以 SubjectType 枚举名开头（如 ChineseCharacter ↔ Chinese）。
        /// </summary>
        private static readonly Dictionary<SubjectType, List<SubCategoryType>> _mapping = BuildMapping();

        /// <summary>
        /// 科目枚举 → 中文显示名，通过反射从 Constants.Subject 自动构建（单一数据源，无需手动维护映射）。
        /// </summary>
        private static readonly Dictionary<SubjectType, string> _subjectDisplayNames =
            BuildDisplayNames<SubjectType>(typeof(Constants.Subject));

        /// <summary>
        /// 子类别枚举 → 中文显示名，通过反射从 Constants.SubCategory 自动构建。
        /// </summary>
        private static readonly Dictionary<SubCategoryType, string> _subCategoryDisplayNames =
            BuildDisplayNames<SubCategoryType>(typeof(Constants.SubCategory));

        /// <summary>
        /// 子类别旧版中文别名（与 Constants.SubCategory 不一致的历史名称），仅用于向后兼容解析已存储数据。
        /// </summary>
        private static readonly Dictionary<string, SubCategoryType> _legacySubCategoryAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            { "数学公式", SubCategoryType.MathFormula },
            { "数学例题", SubCategoryType.MathExample },
            { "数学概念", SubCategoryType.MathConcept },
            { "物理实验", SubCategoryType.PhysicsExperiment },
            { "化学实验", SubCategoryType.ChemistryExperiment },
            { "历史人物", SubCategoryType.HistoryPerson },
            { "历史时间线", SubCategoryType.HistoryTimeline },
            { "地理地图", SubCategoryType.GeographyMap },
            { "地理气候", SubCategoryType.GeographyClimate },
            { "生物实验", SubCategoryType.BiologyExperiment },
            { "生物现象", SubCategoryType.BiologyPhenomenon }
        };

        /// <summary>科目旧版中文别名。</summary>
        private static readonly Dictionary<string, SubjectType> _legacySubjectAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            { "中文", SubjectType.Chinese }
        };

        /// <summary>中文名 → 子类别枚举（显示名 + 旧版别名），供 TryParseSubCategory 使用。</summary>
        private static readonly Dictionary<string, SubCategoryType> _subCategoryParseMap =
            BuildParseMap(_subCategoryDisplayNames, _legacySubCategoryAliases);

        /// <summary>中文名 → 科目枚举（显示名 + 旧版别名），供 TryParseSubject 使用。</summary>
        private static readonly Dictionary<string, SubjectType> _subjectParseMap =
            BuildParseMap(_subjectDisplayNames, _legacySubjectAliases);

        /// <summary>
        /// 通过反射从枚举命名约定构建 科目→子类别 映射。
        /// 遍历 SubCategoryType，按枚举名是否以 SubjectType 名开头进行归类。
        /// </summary>
        private static Dictionary<SubjectType, List<SubCategoryType>> BuildMapping()
        {
            var map = new Dictionary<SubjectType, List<SubCategoryType>>();
            var subjects = Enum.GetValues<SubjectType>()
                .Where(s => s != SubjectType.Unknown)
                .OrderByDescending(s => s.ToString().Length)
                .ToList();

            foreach (var sub in Enum.GetValues<SubCategoryType>())
            {
                if (sub == SubCategoryType.Unknown)
                    continue;
                var name = sub.ToString();
                // 长前缀优先，避免短前缀（如 Math）误匹配到其他科目的子类别。
                var subject = subjects.FirstOrDefault(s => name.StartsWith(s.ToString(), StringComparison.Ordinal));
                if (subject == SubjectType.Unknown)
                    continue;
                if (!map.TryGetValue(subject, out var list))
                {
                    list = new List<SubCategoryType>();
                    map[subject] = list;
                }
                list.Add(sub);
            }
            return map;
        }

        /// <summary>
        /// 通过反射从 Constants 的嵌套类构建 枚举→中文名 字典。
        /// 字段名与枚举名一一对应（如 Constants.Subject.Chinese ↔ SubjectType.Chinese）。
        /// </summary>
        private static Dictionary<TEnum, string> BuildDisplayNames<TEnum>(Type constantsType) where TEnum : struct, Enum
        {
            var map = new Dictionary<TEnum, string>();
            foreach (var field in constantsType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType == typeof(string) && Enum.TryParse<TEnum>(field.Name, out var enumValue))
                    map[enumValue] = (string)field.GetValue(null)!;
            }
            return map;
        }

        /// <summary>
        /// 从显示名字典 + 旧版别名字典构建反向解析映射（中文名→枚举）。
        /// </summary>
        private static Dictionary<string, TEnum> BuildParseMap<TEnum>(
            Dictionary<TEnum, string> displayNames,
            Dictionary<string, TEnum> legacyAliases) where TEnum : struct, Enum
        {
            var map = new Dictionary<string, TEnum>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in displayNames)
                map[kv.Value] = kv.Key;
            foreach (var kv in legacyAliases)
                map.TryAdd(kv.Key, kv.Value);
            return map;
        }

        public static List<SubCategoryType> GetSubCategories(SubjectType subject)
            => _mapping.TryGetValue(subject, out var list) ? list : new List<SubCategoryType>();

        public static SubjectType GetSubject(SubCategoryType subCategory)
            => _mapping.FirstOrDefault(kv => kv.Value.Contains(subCategory)).Key;

        public static bool IsValidSubCategory(SubjectType subject, SubCategoryType subCategory)
            => GetSubCategories(subject).Contains(subCategory);

        public static SubjectType ParseSubject(string subjectString)
            => TryParseSubject(subjectString, out var subject) ? subject : SubjectType.Chinese;

        public static SubCategoryType ParseSubCategory(string subCategoryString)
            => TryParseSubCategory(subCategoryString, out var subCategory) ? subCategory : SubCategoryType.ChineseCharacter;

        public static bool TryParseSubject(string subjectString, out SubjectType subject)
        {
            if (Enum.TryParse<SubjectType>(subjectString, true, out subject))
                return true;
            return _subjectParseMap.TryGetValue(subjectString, out subject);
        }

        public static bool TryParseSubCategory(string subCategoryString, out SubCategoryType subCategory)
        {
            if (Enum.TryParse<SubCategoryType>(subCategoryString, true, out subCategory))
                return true;
            return _subCategoryParseMap.TryGetValue(subCategoryString, out subCategory);
        }

        /// <summary>
        /// 获取子类别的中文显示名（来自 Constants.SubCategory，与 SubjectTemplates.json 一致）。
        /// </summary>
        public static string GetSubCategoryDisplayName(SubCategoryType subCategory)
            => _subCategoryDisplayNames.TryGetValue(subCategory, out var name) ? name : subCategory.ToString();

        /// <summary>
        /// 获取科目的中文显示名（来自 Constants.Subject）。
        /// </summary>
        public static string GetSubjectDisplayName(SubjectType subject)
            => _subjectDisplayNames.TryGetValue(subject, out var name) ? name : subject.ToString();
    }
}
