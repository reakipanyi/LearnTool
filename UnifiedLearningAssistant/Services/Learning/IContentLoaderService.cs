namespace UnifiedLearningAssistant.Services.Learning
{
    public interface IContentLoaderService
    {
        List<object> LoadItems(string subCategory, string wordBankFile = "");
        void SaveItems(string subCategory, List<object> items, string wordBankFile = "");
        List<string> GetSubCategories(string language);
        List<string> GetWordBankFiles(string subCategory);
        string GetDefaultWordBankFile(string subCategory);
        Type GetItemType(string subCategory);
    }
}