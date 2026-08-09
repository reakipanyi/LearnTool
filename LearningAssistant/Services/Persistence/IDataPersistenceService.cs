using LearningAssistant.Common;
using LearningAssistant.Models.Config;
using LearningAssistant.Models.User;

namespace LearningAssistant.Services.Persistence
{
    public interface IDataPersistenceService
    {
        void Initialize();
        AppConfig LoadConfig();
        void SaveConfig(AppConfig config);
        UserProfile LoadUserProfile(string userId);
        void SaveUserProfile(UserProfile profile);
        List<string> GetUserIds();
        void CreateUserProfile(string userId, string userName);
        bool DeleteUserProfile(string userId);
        void SaveSession(SessionData session);
        SessionData LoadSession();
        T? LoadJsonFile<T>(string filePath);
        void SaveJsonFile<T>(string filePath, T data);
        void PersistCache();

        List<string> GetKnownItems(string userId, SubCategoryType category);
        List<string> GetUnknownItems(string userId, SubCategoryType category);
        void UpsertLearningItemState(string userId, SubCategoryType category, string content, bool isKnown);
        void UpsertLearningItemStates(string userId, SubCategoryType category, IEnumerable<string> contents, bool isKnown);
        void DeleteLearningItemState(string userId, SubCategoryType category, string content);
        void SyncCategoryProgressToLearningItemStates(string userId, SubCategoryType category, List<string> knownItems, List<string> unknownItems);
    }

    /// <summary>
    /// 会话数据类 - 存储用户最后一次使用应用时的状态
    /// 用于实现"继续上次学习"功能
    /// </summary>
    public class SessionData
    {
        public string CurrentUserId { get; set; } = string.Empty;
        public string LastTab { get; set; } = string.Empty;
        public string LastPdfPath { get; set; } = string.Empty;
        public int LastPageIndex { get; set; } = 0;
        public string LastFolderPath { get; set; } = string.Empty;
        public DateTime LastAccessTime { get; set; } = DateTime.Now;

        public SubjectType LastSubject { get; set; }
        public SubCategoryType LastSubCategory { get; set; }
        public LearningModeType LastMode { get; set; } = LearningModeType.Study;
        public string WordBankFile { get; set; } = string.Empty;
        public SortOrderType LastSortOrder { get; set; } = SortOrderType.Sequential;
    }
}
