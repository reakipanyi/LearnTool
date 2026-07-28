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
        void InvalidateCache(SubCategoryType subCategory);
        void InvalidateAllCaches();
    }
}
