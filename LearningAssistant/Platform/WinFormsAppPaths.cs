using LearningAssistant.Abstractions;
using LearningAssistant.Common;

namespace LearningAssistant.Platform
{
    /// <summary>
    /// WinForms 端 IAppPaths 实现，委托给 AppPaths 静态类。
    /// </summary>
    public class WinFormsAppPaths : IAppPaths
    {
        public string AssemblyDir => AppPaths.AssemblyDir;
        public string DataRoot => AppPaths.DataRoot;
        public string ConfigDir => AppPaths.ConfigDir;
        public string DataDir => AppPaths.DataDir;
        public string UsersDir => AppPaths.UsersDir;
        public string CurrentUserDir => AppPaths.CurrentUserDir;
        public string LogsDir => AppPaths.LogsDir;
        public string CacheDir => AppPaths.CacheDir;
        public string DatabaseDir => AppPaths.DatabaseDir;
        public string DatabasePath => AppPaths.DatabasePath;
        public string SessionDir => AppPaths.SessionDir;
        public string ExportsDir => AppPaths.ExportsDir;
        public string AnnotationsDir => AppPaths.AnnotationsDir;
        public string TranslationsDir => AppPaths.TranslationsDir;
        public string FileBookmarksDir => AppPaths.FileBookmarksDir;
        public string HighlightsDir => AppPaths.HighlightsDir;
        public string WrongAnswersDir => AppPaths.WrongAnswersDir;
        public string RecommendationFeedbackDir => AppPaths.RecommendationFeedbackDir;
        public string NotesDir => AppPaths.NotesDir;
        public string LearningPathsDir => AppPaths.LearningPathsDir;
        public string ThumbnailsDir => AppPaths.ThumbnailsDir;
        public string AudioDir => AppPaths.AudioDir;
        public string TTSCacheDir => AppPaths.TTSCacheDir;
        public string TesseractDataDir => AppPaths.TesseractDataDir;
        public string UserDataDir => AppPaths.UserDataDir;
        public string TempDir => AppPaths.TempDir;

        public string AppSettingsPath => AppPaths.AppSettingsPath;
        public string AiPromptsPath => AppPaths.AiPromptsPath;
        public string PromptTemplatesPath => AppPaths.PromptTemplatesPath;
        public string WebBookmarksPath => AppPaths.WebBookmarksPath;
        public string SubjectTemplatesPath => AppPaths.SubjectTemplatesPath;
        public string RemindersPath => AppPaths.RemindersPath;
        public string EncouragementConfigPath => AppPaths.EncouragementConfigPath;
        public string PendingContentPath => AppPaths.PendingContentPath;
        public string LastSessionPath => AppPaths.LastSessionPath;
        public string SessionPath => AppPaths.SessionPath;
        public string BrowserTabsPath => AppPaths.BrowserTabsPath;
        public string UserBookmarksPath => AppPaths.UserBookmarksPath;

        public void SetCurrentUserId(string userId) => AppPaths.SetCurrentUserId(userId);
        public string GetCurrentUserId() => AppPaths.GetCurrentUserId();
        public void EnsureDirectoriesExist() => AppPaths.EnsureDirectoriesExist();
        public void EnsureUserDirectoriesExist(string? userId = null) => AppPaths.EnsureUserDirectoriesExist(userId);
        public void EnsureDirectoryExists(string path) => AppPaths.EnsureDirectoryExists(path);

        public string GetUserDir(string? userId = null) => AppPaths.GetUserDir(userId);
        public string GetUserAnnotationsDir(string? userId = null) => AppPaths.GetUserAnnotationsDir(userId);
        public string GetUserAnnotationPath(string pdfPath, int pageIndex, string? userId = null) => AppPaths.GetUserAnnotationPath(pdfPath, pageIndex, userId);
        public string GetUserBookmarksDir(string? userId = null) => AppPaths.GetUserBookmarksDir(userId);
        public string GetUserPdfBookmarkPath(string pdfPath, string? userId = null) => AppPaths.GetUserPdfBookmarkPath(pdfPath, userId);
        public string GetUserHighlightsDir(string? userId = null) => AppPaths.GetUserHighlightsDir(userId);
        public string GetUserFolderHighlightPath(string folderPath, string? userId = null) => AppPaths.GetUserFolderHighlightPath(folderPath, userId);
        public string GetUserTranslationsDir(string? userId = null) => AppPaths.GetUserTranslationsDir(userId);
        public string GetTtsCacheDir() => AppPaths.GetTtsCacheDir();
        public string GetUserAutoSaveDir(string? userId = null) => AppPaths.GetUserAutoSaveDir(userId);
        public string GetUserBackupDir(string? userId = null) => AppPaths.GetUserBackupDir(userId);
        public string GetUserNotesPath(string? userId = null) => AppPaths.GetUserNotesPath(userId);
        public string GetUserFavoritesPath(string? userId = null) => AppPaths.GetUserFavoritesPath(userId);
        public string GetUserWrongAnswersPath(string? userId = null) => AppPaths.GetUserWrongAnswersPath(userId);
        public string GetUserGoalsPath(string? userId = null) => AppPaths.GetUserGoalsPath(userId);
        public string GetUserAnalyticsPath(string? userId = null) => AppPaths.GetUserAnalyticsPath(userId);
        public string GetUserProgressPath(string userName) => AppPaths.GetUserProgressPath(userName);

        public string GetAnnotationPath(string pdfPath, int pageIndex) => AppPaths.GetAnnotationPath(pdfPath, pageIndex);
        public string GetPdfBookmarkPath(string pdfPath) => AppPaths.GetPdfBookmarkPath(pdfPath);
        public string GetFolderHighlightPath(string folderPath) => AppPaths.GetFolderHighlightPath(folderPath);
        public string GetCacheFilePath(string key) => AppPaths.GetCacheFilePath(key);
        public string GetDataFilePath(string fileName) => AppPaths.GetDataFilePath(fileName);
        public string FindConfigFile(string fileName, string?[]? searchPaths = null) => AppPaths.FindConfigFile(fileName, searchPaths);
    }
}