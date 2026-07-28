using LearningAssistant.Common;
using LearningAssistant.Models.Learning;

namespace LearningAssistant.Services.Learning
{
    public interface IStudyEngine
    {
        void Initialize(LearningContext context, bool continueMode = true, bool loadAllItems = true);
        LearningItem? GetCurrentItem();
        bool HasNext();
        void MoveNext();
        void SetCurrentIndex(int index);
        void MarkCurrentAsKnown();
        void MarkCurrentAsUnknown();
        int MarkItemsAsKnown(IEnumerable<string> contents);
        int MarkItemsAsUnknown(IEnumerable<string> contents);
        StudyStatistics GetStatistics();
        void SaveProgress();
        void ResetProgress();
        List<LearningItem> GetUnknownItems();
        void AddUnknownItem(string content, SubCategoryType subCategory);
        void ApplySettings(LearningModeType mode, SortOrderType sortOrder);

        int CurrentIndex { get; }
        int TotalCount { get; }
        int TotalItemCount { get; }
        IReadOnlyList<string> KnownItems { get; }
        IReadOnlyList<string> UnknownItems { get; }
        LearningModeType CurrentMode { get; }
        SortOrderType CurrentSortOrder { get; }
        bool HasSavedProgress { get; }
        List<LearningItem> GetAllItems();

        string GetProgressSummary(string userId, SubjectType subject, SubCategoryType subCategory);
        int GetKnownCount(string userId, SubCategoryType subCategory);
        int GetUnknownCount(string userId, SubCategoryType subCategory);
        double GetAccuracy(string userId, SubCategoryType subCategory);
        List<string> GetUnknownItems(string userId);
        void EnsureItemsLoaded(int pageSize = 100);
    }

    /// <summary>
    /// 学习统计数据结构
    /// </summary>
    public class StudyStatistics
    {
        /// <summary>
        /// 总测试次数
        /// </summary>
        public int TotalTestCount { get; set; }

        /// <summary>
        /// 正确次数
        /// </summary>
        public int CorrectCount { get; set; }

        /// <summary>
        /// 最后测试日期时间
        /// </summary>
        public DateTime LastTestDate { get; set; }

        /// <summary>
        /// 计算属性：准确率（百分比）
        /// 计算公式：(正确次数 / 总次数) * 100
        /// 当总次数为0时返回0避免除零错误
        /// </summary>
        public double AccuracyRate => TotalTestCount > 0 ? (double)CorrectCount / TotalTestCount * 100 : 0;
    }
}
