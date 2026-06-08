namespace LearningAssistant.Services.Learning
{
    public interface IProgressService
    {
        string GetProgressSummary(string userId, string language, string subCategory);
        int GetKnownCount(string userId, string subCategory);
        int GetUnknownCount(string userId, string subCategory);
        double GetAccuracy(string userId, string subCategory);
        List<string> GetUnknownItems(string userId);
    }
}