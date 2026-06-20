namespace LearningAssistant.Services.Learning
{
    public class StudyEngineState
    {
        public string UserId { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public string WordBankFile { get; set; } = string.Empty;
        public string CurrentMode { get; set; } = "Study";
        public string CurrentSortOrder { get; set; } = "Sequential";
        public List<string> KnownItems { get; set; } = new List<string>();
        public List<string> UnknownItems { get; set; } = new List<string>();
        public int StudyModeIndex { get; set; }
        public int QuickModeIndex { get; set; }
        public int CorrectCount { get; set; }
        public int TotalCount { get; set; }
    }
}
