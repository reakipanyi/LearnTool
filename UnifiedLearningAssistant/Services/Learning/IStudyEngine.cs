using LearningAssistant.Models.Learning;

namespace LearningAssistant.Services.Learning
{
    public interface IStudyEngine
    {
        void Initialize(string userId, string language, string subCategory, string wordBankFile, string mode = "Study", string sortOrder = "Sequential", bool continueMode = true);
        LearningItem? GetCurrentItem();
        bool HasNext();
        void MoveNext();
        void SetCurrentIndex(int index);
        void MarkCurrentAsKnown();
        void MarkCurrentAsUnknown();
        StudyStatistics GetStatistics();
        void SaveProgress();
        void ResetProgress();
        List<LearningItem> GetUnknownItems();
        // 新增功能：PDF生词本联动 - 添加未掌握项
        void AddUnknownItem(string content, string subCategory);
        void ApplySettings(string mode, string sortOrder);
        int CurrentIndex { get; }
        int TotalCount { get; }
        IReadOnlyList<string> KnownItems { get; }
        IReadOnlyList<string> UnknownItems { get; }
        string CurrentMode { get; }
        string CurrentSortOrder { get; }
        bool HasSavedProgress { get; }
        List<LearningItem> GetAllItems();
    }

    public class StudyStatistics
    {
        public int TotalTestCount { get; set; }
        public int CorrectCount { get; set; }
        public DateTime LastTestDate { get; set; }
        public double AccuracyRate => TotalTestCount > 0 ? (double)CorrectCount / TotalTestCount * 100 : 0;
    }
}
