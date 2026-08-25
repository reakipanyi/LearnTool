using LearningAssistant.Common;
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
        void SaveLearningConfig(LearningConfig config);
        LearningConfig LoadLearningConfig();
    }

    public class LearningConfig
    {
        public SubjectType Subject { get; set; }
        public SubCategoryType SubCategory { get; set; }
        public LearningModeType Mode { get; set; } = LearningModeType.Study;
        public string WordBankFile { get; set; } = string.Empty;
        public SortOrderType SortOrder { get; set; } = SortOrderType.Sequential;
    }
}
