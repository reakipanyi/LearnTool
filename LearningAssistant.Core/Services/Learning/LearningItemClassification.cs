using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习项分类维度的解析与兼容处理。
    /// 旧数据（P0 之前）来源只写在 ExtendedProperties 中，此处统一回退读取，避免筛选漏项。
    /// </summary>
    public static class LearningItemClassification
    {
        public static bool IsClassificationEnumColumn(string columnName)
        {
            return string.Equals(columnName, nameof(LearningItem.AgeRange), StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, nameof(LearningItem.Importance), StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, nameof(LearningItem.SourceType), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>读取学习项的有效来源：优先一等字段，未设置时回退到 ExtendedProperties。</summary>
        public static StudyContentSource GetEffectiveSourceType(LearningItem item)
        {
            if (item.SourceType != StudyContentSource.Manual)
                return item.SourceType;

            var raw = item.GetExtendedProperty<string>("SourceType", string.Empty);
            return ParseSourceType(raw);
        }

        /// <summary>解析来源字符串（兼容英文枚举名、旧版 Pdf* 字符串与中文显示名）。</summary>
        public static StudyContentSource ParseSourceType(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return StudyContentSource.Manual;

            var value = raw.Trim();
            if (Enum.TryParse<StudyContentSource>(value, true, out var parsed)
                && Enum.IsDefined(typeof(StudyContentSource), parsed))
            {
                return parsed;
            }

            if (TryParseChineseSource(value, out var chineseParsed))
                return chineseParsed;

            // 旧版来源字符串：PdfWord / PdfExport 等 PDF 派生内容统一归入 PDF 高亮来源。
            if (value.StartsWith("Pdf", StringComparison.OrdinalIgnoreCase))
            {
                if (value.Contains("Annotation", StringComparison.OrdinalIgnoreCase))
                    return StudyContentSource.PdfAnnotation;
                if (value.Contains("Shadowing", StringComparison.OrdinalIgnoreCase)
                    || value.Contains("Sentence", StringComparison.OrdinalIgnoreCase))
                    return StudyContentSource.PdfShadowing;
                if (value.Contains("Flashcard", StringComparison.OrdinalIgnoreCase))
                    return StudyContentSource.PdfAiFlashcard;
                return StudyContentSource.PdfHighlight;
            }

            if (value.Contains("Import", StringComparison.OrdinalIgnoreCase))
                return StudyContentSource.Import;
            if (value.Contains("Game", StringComparison.OrdinalIgnoreCase))
                return StudyContentSource.Game;

            return StudyContentSource.Manual;
        }

        public static bool TryParseAgeRange(string? raw, out AgeRange value)
        {
            value = AgeRange.All;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            var text = raw.Trim();
            if (Enum.TryParse<AgeRange>(text, true, out var parsed)
                && Enum.IsDefined(typeof(AgeRange), parsed))
            {
                value = parsed;
                return true;
            }

            switch (text)
            {
                case "通用": value = AgeRange.All; return true;
                case "学前": value = AgeRange.Preschool; return true;
                case "小学低年级": value = AgeRange.PrimaryLow; return true;
                case "小学高年级": value = AgeRange.PrimaryHigh; return true;
                case "初中": value = AgeRange.JuniorHigh; return true;
                case "高中": value = AgeRange.SeniorHigh; return true;
                case "成人": value = AgeRange.Adult; return true;
                default: return false;
            }
        }

        /// <summary>
        /// 高亮颜色 → 默认重要等级（P0-2）：红色=核心、橙/蓝=重点、绿/黄=一般、粉色=拓展。
        /// 仅作为“加入学习”时的默认值，用户后续可在编辑器中调整。
        /// </summary>
        public static ImportanceLevel ResolveImportanceFromHighlightColor(HighlightColor color)
        {
            return color switch
            {
                HighlightColor.Red => ImportanceLevel.Core,
                HighlightColor.Orange => ImportanceLevel.Key,
                HighlightColor.Blue => ImportanceLevel.Key,
                HighlightColor.Pink => ImportanceLevel.Extension,
                _ => ImportanceLevel.Normal
            };
        }

        public static bool TryParseImportance(string? raw, out ImportanceLevel value)
        {
            value = ImportanceLevel.Normal;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            var text = raw.Trim();
            if (Enum.TryParse<ImportanceLevel>(text, true, out var parsed)
                && Enum.IsDefined(typeof(ImportanceLevel), parsed))
            {
                value = parsed;
                return true;
            }

            switch (text)
            {
                case "拓展": value = ImportanceLevel.Extension; return true;
                case "一般": value = ImportanceLevel.Normal; return true;
                case "重点": value = ImportanceLevel.Key; return true;
                case "核心": value = ImportanceLevel.Core; return true;
                default: return false;
            }
        }

        private static bool TryParseChineseSource(string text, out StudyContentSource value)
        {
            value = StudyContentSource.Manual;
            switch (text)
            {
                case "手动": value = StudyContentSource.Manual; return true;
                case "PDF高亮": value = StudyContentSource.PdfHighlight; return true;
                case "PDF标注": value = StudyContentSource.PdfAnnotation; return true;
                case "跟读": value = StudyContentSource.PdfShadowing; return true;
                case "PDF闪卡": value = StudyContentSource.PdfAiFlashcard; return true;
                case "导入": value = StudyContentSource.Import; return true;
                case "游戏": value = StudyContentSource.Game; return true;
                default: return false;
            }
        }

        /// <summary>
        /// 编辑器以中文显示枚举值；保存回 JSON 前转回枚举名，保证反序列化往返不丢值。
        /// </summary>
        public static string NormalizeEnumValue(string columnName, string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw ?? string.Empty;

            if (string.Equals(columnName, nameof(LearningItem.AgeRange), StringComparison.OrdinalIgnoreCase)
                && TryParseAgeRange(raw, out var ageRange))
            {
                return ageRange.ToString();
            }

            if (string.Equals(columnName, nameof(LearningItem.Importance), StringComparison.OrdinalIgnoreCase)
                && TryParseImportance(raw, out var importance))
            {
                return importance.ToString();
            }

            if (string.Equals(columnName, nameof(LearningItem.SourceType), StringComparison.OrdinalIgnoreCase)
                && TryParseSourceTypeForEditor(raw, out var source))
            {
                return source.ToString();
            }

            return raw;
        }

        private static bool TryParseSourceTypeForEditor(string raw, out StudyContentSource value)
        {
            if (Enum.TryParse<StudyContentSource>(raw, true, out var parsed)
                && Enum.IsDefined(typeof(StudyContentSource), parsed))
            {
                value = parsed;
                return true;
            }

            if (TryParseChineseSource(raw, out value))
                return true;

            value = ParseSourceType(raw);
            return value != StudyContentSource.Manual || string.Equals(raw.Trim(), "Manual", StringComparison.OrdinalIgnoreCase);
        }
    }
}
