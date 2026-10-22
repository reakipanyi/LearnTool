using LearningAssistant.Common;
using LearningAssistant.Models.Learning;

namespace LearningAssistant.Services.Learning
{
    public interface IContentLoaderService
    {
        List<LearningItem> LoadItems(LearningContext context);
        void SaveItems(LearningContext context, List<LearningItem> items);
        List<SubCategoryType> GetSubCategories(SubjectType subject);
        List<string> GetAllSubjects();
        List<string> GetWordBankFiles(SubCategoryType subCategory);
        string GetDefaultWordBankFile(SubCategoryType subCategory);
        void SaveUserContent(UserContent content);

        List<LearningItem> LoadItemsPaged(LearningContext context, int pageIndex, int pageSize);
        int GetItemCount(LearningContext context);

        /// <summary>为旧词库补齐缺失的稳定 Id，返回补写条数。可只扫描指定时间后修改过的文件。</summary>
        int EnsureAllItemsHaveIds(DateTime? modifiedSince = null);

        /// <summary>个人内容写入后失效对应文件缓存。</summary>
        void InvalidateFileCache(string filePath);
    }
}
