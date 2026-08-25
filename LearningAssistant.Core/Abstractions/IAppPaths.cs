namespace LearningAssistant.Abstractions
{
    /// <summary>
    /// 应用路径抽象，消除 AppPaths 静态类的桌面路径硬编码。
    /// WinForms 端用 AppDomain.CurrentDomain.BaseDirectory 实现；
    /// MAUI Android 端用 FileSystem.AppDataDirectory / FileSystem.CacheDirectory 实现。
    /// </summary>
    public interface IAppPaths
    {
        string AssemblyDir { get; }
        string DataRoot { get; }
        string ConfigDir { get; }
        string DataDir { get; }
        string UsersDir { get; }
        string CurrentUserDir { get; }
        string LogsDir { get; }
        string CacheDir { get; }
        string DatabaseDir { get; }
        string DatabasePath { get; }
        string SessionDir { get; }
        string ExportsDir { get; }
        string AnnotationsDir { get; }
        string TranslationsDir { get; }
        string FileBookmarksDir { get; }
        string HighlightsDir { get; }
        string WrongAnswersDir { get; }
        string RecommendationFeedbackDir { get; }
        string NotesDir { get; }
        string LearningPathsDir { get; }
        string ThumbnailsDir { get; }
        string AudioDir { get; }
        string TTSCacheDir { get; }
        string TesseractDataDir { get; }
        string UserDataDir { get; }
        string TempDir { get; }

        string AppSettingsPath { get; }
        string AiPromptsPath { get; }
        string PromptTemplatesPath { get; }
        string WebBookmarksPath { get; }
        string SubjectTemplatesPath { get; }
        string RemindersPath { get; }
        string EncouragementConfigPath { get; }
        string PendingContentPath { get; }
        string LastSessionPath { get; }
        string SessionPath { get; }
        string BrowserTabsPath { get; }
        string UserBookmarksPath { get; }

        void SetCurrentUserId(string userId);
        string GetCurrentUserId();
        void EnsureDirectoriesExist();
        void EnsureUserDirectoriesExist(string? userId = null);
        void EnsureDirectoryExists(string path);

        string GetUserDir(string? userId = null);
        string GetUserAnnotationsDir(string? userId = null);
        string GetUserAnnotationPath(string pdfPath, int pageIndex, string? userId = null);
        string GetUserBookmarksDir(string? userId = null);
        string GetUserPdfBookmarkPath(string pdfPath, string? userId = null);
        string GetUserHighlightsDir(string? userId = null);
        string GetUserFolderHighlightPath(string folderPath, string? userId = null);
        string GetUserTranslationsDir(string? userId = null);
        string GetTtsCacheDir();
        string GetUserAutoSaveDir(string? userId = null);
        string GetUserBackupDir(string? userId = null);
        string GetUserNotesPath(string? userId = null);
        string GetUserFavoritesPath(string? userId = null);
        string GetUserWrongAnswersPath(string? userId = null);
        string GetUserGoalsPath(string? userId = null);
        string GetUserAnalyticsPath(string? userId = null);
        string GetUserProgressPath(string userName);

        string GetAnnotationPath(string pdfPath, int pageIndex);
        string GetPdfBookmarkPath(string pdfPath);
        string GetFolderHighlightPath(string folderPath);
        string GetCacheFilePath(string key);
        string GetDataFilePath(string fileName);
        string FindConfigFile(string fileName, string?[]? searchPaths = null);
    }
}