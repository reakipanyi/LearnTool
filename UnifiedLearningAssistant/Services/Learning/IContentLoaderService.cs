using UnifiedLearningAssistant.Models.Learning;

namespace UnifiedLearningAssistant.Services.Learning
{
    public interface IContentLoaderService
    {
        List<LearningItem> LoadItems(string subCategory, string wordBankFile = "");
        void SaveItems(string subCategory, List<LearningItem> items, string wordBankFile = "");
        List<string> GetSubCategories(string language);
        List<string> GetWordBankFiles(string subCategory);
        string GetDefaultWordBankFile(string subCategory);
        Type GetItemType(string subCategory);
    }
}