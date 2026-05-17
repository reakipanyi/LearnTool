namespace UnifiedLearningAssistant.Models.User
{
    public class LearningProgress
    {
        public Dictionary<string, CategoryProgress> CategoryProgresses { get; set; } = new Dictionary<string, CategoryProgress>();
        public DateTime LastStudyTime { get; set; } = DateTime.MinValue;
        public int TotalStudyMinutes { get; set; } = 0;
        public int TotalItemsStudied { get; set; } = 0;
        public int TotalItemsMastered { get; set; } = 0;
        public int PerfectSessions { get; set; } = 0;
    }

    public class CategoryProgress
    {
        public string CategoryName { get; set; } = string.Empty;
        public List<string> KnownItems { get; set; } = new List<string>();
        public List<string> UnknownItems { get; set; } = new List<string>();
        public int TotalTestCount { get; set; } = 0;
        public int CorrectCount { get; set; } = 0;
        public DateTime LastTestDate { get; set; } = DateTime.MinValue;
        public int LastResumeIndex { get; set; } = 0;
        public int QuickTestResumeIndex { get; set; } = 0;
        public string LastStudyMode { get; set; } = string.Empty;
    }
}