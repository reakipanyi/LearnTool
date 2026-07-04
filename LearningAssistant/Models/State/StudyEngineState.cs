using LearningAssistant.Common;

namespace LearningAssistant.Services.Learning
{
    public class StudyEngineState
    {
        public string UserId { get; set; } = string.Empty;
        public SubjectType Subject { get; set; }
        public SubCategoryType SubCategory { get; set; }
        public string WordBankFile { get; set; } = string.Empty;
        public LearningModeType CurrentMode { get; set; } = LearningModeType.Study;
        public SortOrderType CurrentSortOrder { get; set; } = SortOrderType.Sequential;
        public List<string> KnownItems { get; set; } = new List<string>();
        public List<string> UnknownItems { get; set; } = new List<string>();
        public int StudyModeIndex { get; set; }
        public int QuickModeIndex { get; set; }
        public int CorrectCount { get; set; }
        public int TotalCount { get; set; }
    }
}
