using LearningAssistant.Common;
using System.Text.Json.Serialization;

namespace LearningAssistant.Models.Learning
{
    public record LearningContext(
        string UserId,
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        SubjectType Subject,
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        SubCategoryType SubCategory,
        string WordBankFile = "",
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        LearningModeType Mode = LearningModeType.Study,
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        SortOrderType SortOrder = SortOrderType.Sequential,
        string? KnowledgePointId = null,
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        AgeRange? AgeRange = null,
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        ImportanceLevel? Importance = null,
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        StudyContentSource? SourceType = null)
    {
        /// <summary>是否启用了分类筛选（无筛选时保持原有队列与进度行为）。</summary>
        [JsonIgnore]
        public bool HasClassificationFilter =>
            !string.IsNullOrWhiteSpace(KnowledgePointId) ||
            (AgeRange.HasValue && AgeRange.Value != LearningAssistant.Common.AgeRange.All) ||
            Importance.HasValue ||
            SourceType.HasValue;

        /// <summary>
        /// 筛选指纹：用于隔离不同筛选条件下的队列进度，避免共享同一断点索引。
        /// 无筛选返回空字符串，与历史进度完全兼容。
        /// </summary>
        public string GetFilterKey()
        {
            if (!HasClassificationFilter) return string.Empty;

            return string.Join('|',
                $"kp={KnowledgePointId?.Trim() ?? string.Empty}",
                $"age={(AgeRange.HasValue ? AgeRange.Value.ToString() : string.Empty)}",
                $"imp={(Importance.HasValue ? Importance.Value.ToString() : string.Empty)}",
                $"src={(SourceType.HasValue ? SourceType.Value.ToString() : string.Empty)}");
        }
    }
}
