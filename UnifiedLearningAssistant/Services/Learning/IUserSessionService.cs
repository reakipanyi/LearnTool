using UnifiedLearningAssistant.Models.User;

namespace UnifiedLearningAssistant.Services.Learning
{
    public interface IUserSessionService
    {
        string CurrentUserId { get; }
        string LoadSession();
        void SaveSession(string userId);
        List<string> GetUserList();
        UserProfile LoadUserProfile(string userId);
    }
}