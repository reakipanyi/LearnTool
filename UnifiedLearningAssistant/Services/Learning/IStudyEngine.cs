using UnifiedLearningAssistant.Models.Learning;

namespace UnifiedLearningAssistant.Services.Learning
{
    public interface IStudyEngine
    {
        void Initialize(string userId, string language, string subCategory, string wordBankFile, string mode, string sortOrder);
        LearningItem? GetCurrentItem();
        bool HasNext();
        void MoveNext();
        void MarkCurrentAsKnown();
        void MarkCurrentAsUnknown();
        StudyStatistics GetStatistics();
        void SaveProgress();
        void ResetProgress();
        List<LearningItem> GetUnknownItems();
        int CurrentIndex { get; }
        int TotalCount { get; }
        IReadOnlyList<string> KnownItems { get; }
        IReadOnlyList<string> UnknownItems { get; }
        string CurrentMode { get; }
    }

    public class StudyStatistics
    {
        public int TotalTestCount { get; set; }
        public int CorrectCount { get; set; }
        public DateTime LastTestDate { get; set; }
        public double AccuracyRate => TotalTestCount > 0 ? (double)CorrectCount / TotalTestCount * 100 : 0;
    }
}
