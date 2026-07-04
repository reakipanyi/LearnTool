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
        SortOrderType SortOrder = SortOrderType.Sequential
    );
}