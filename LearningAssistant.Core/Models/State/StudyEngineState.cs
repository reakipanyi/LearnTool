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
        public int TotalItemCount { get; set; }
        public Dictionary<string, int> ContinuousCorrectCount { get; set; } = new Dictionary<string, int>();
        public int SmartSkipThreshold { get; set; } = 3;
        public Dictionary<LearningModeType, int> ModeIndexCache { get; set; } = new Dictionary<LearningModeType, int>();

        // 分类筛选（P1）：透传给内容加载与进度隔离
        public string? KnowledgePointId { get; set; }
        public AgeRange? AgeRange { get; set; }
        public ImportanceLevel? Importance { get; set; }
        public StudyContentSource? SourceType { get; set; }

        /// <summary>筛选指纹，空字符串表示未启用筛选（沿用历史进度）。</summary>
        public string ProgressKey { get; set; } = string.Empty;
    }
}
