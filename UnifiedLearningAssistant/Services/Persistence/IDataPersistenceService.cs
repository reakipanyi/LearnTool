using UnifiedLearningAssistant.Models.Config;
using UnifiedLearningAssistant.Models.User;

namespace UnifiedLearningAssistant.Services.Persistence
{
    public interface IDataPersistenceService
    {
        AppConfig LoadConfig();
        void SaveConfig(AppConfig config);
        UserProfile LoadUserProfile(string userId);
        void SaveUserProfile(UserProfile profile);
        List<string> GetUserIds();
        void CreateUserProfile(string userId, string userName);
        void SaveSession(SessionData session);
        SessionData LoadSession();
        T? LoadJsonFile<T>(string filePath);
        void SaveJsonFile<T>(string filePath, T data);
        void PersistCache();
    }

    public class SessionData
    {
        public string CurrentUserId { get; set; } = string.Empty;
        public string LastTab { get; set; } = string.Empty;
        public string LastPdfPath { get; set; } = string.Empty;
        public int LastPageIndex { get; set; } = 0;
        public string LastFolderPath { get; set; } = string.Empty;
        public DateTime LastAccessTime { get; set; } = DateTime.Now;
        
        public string Language { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public string WordBankFile { get; set; } = string.Empty;
        public string SortOrder { get; set; } = string.Empty;
    }
}
