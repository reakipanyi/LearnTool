using LearningAssistant.Models.User;

namespace LearningAssistant.Services.Learning
{
    public interface IUserSessionService
    {
        string CurrentUserId { get; }
        string LoadSession();
        void SaveSession(string userId);
        List<string> GetUserList();
        UserProfile LoadUserProfile(string userId);
        
        void SaveLearningConfig(string language, string subCategory, string mode, string wordBankFile, string sortOrder);
        (string Language, string SubCategory, string Mode, string WordBankFile, string SortOrder) LoadLearningConfig();
    }
}