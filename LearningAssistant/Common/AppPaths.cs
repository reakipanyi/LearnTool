namespace LearningAssistant.Common
{
    /// <summary>
    /// 统一的应用路径管理器
    /// 所有用户数据统一存储在程序运行目录下的 AppData 文件夹中
    /// </summary>
    public static class AppPaths
    {
        static AppPaths()
        {
            EnsureDirectoriesExist();
        }

        #region 基础路径

        /// <summary>
        /// 程序集根目录（只读资源）
        /// </summary>
        public static string AssemblyDir => AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// 用户数据根目录 - 程序运行目录下的 AppData
        /// </summary>
        public static string DataRoot => Path.Combine(AssemblyDir, "AppData");


        /// <summary>
        /// OCR 数据目录（只读）
        /// </summary>
        public static string TesseractDataDir => Path.Combine(AssemblyDir, "tessdata");

        #endregion

        #region 子目录

        /// <summary>
        /// 配置目录
        /// </summary>
        public static string ConfigDir => Path.Combine(DataRoot, "config");

        /// <summary>
        /// 学习数据目录
        /// </summary>
        public static string DataDir => Path.Combine(DataRoot, "data");

        /// <summary>
        /// 用户数据目录
        /// </summary>
        public static string UsersDir => Path.Combine(DataRoot, "users");

        /// <summary>
        /// 当前用户数据目录（默认为 Guest）
        /// </summary>
        public static string CurrentUserDir => Path.Combine(UsersDir, _currentUserId);

        private static string _currentUserId = "Guest";

        /// <summary>
        /// 设置当前用户ID
        /// </summary>
        public static void SetCurrentUserId(string userId)
        {
            _currentUserId = string.IsNullOrWhiteSpace(userId) ? "Guest" : userId;
            EnsureUserDirectoriesExist();
        }

        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        public static string GetCurrentUserId() => _currentUserId;

        /// <summary>
        /// 日志目录
        /// </summary>
        public static string LogsDir => Path.Combine(DataRoot, "logs");

        /// <summary>
        /// 缓存目录
        /// </summary>
        public static string CacheDir => Path.Combine(DataRoot, "cache");

        /// <summary>
        /// 数据库目录
        /// </summary>
        public static string DatabaseDir => Path.Combine(DataRoot, "database");

        /// <summary>
        /// 会话目录
        /// </summary>
        public static string SessionDir => Path.Combine(DataRoot, "session");

        /// <summary>
        /// 导出目录
        /// </summary>
        public static string ExportsDir => Path.Combine(DataRoot, "Exports");

        /// <summary>
        /// PDF标注目录
        /// </summary>
        public static string AnnotationsDir => Path.Combine(DataRoot, "annotations");

        /// <summary>
        /// 翻译缓存目录
        /// </summary>
        public static string TranslationsDir => Path.Combine(DataRoot, "translations");

        /// <summary>
        /// 书签目录
        /// </summary>
        public static string BookmarksDir => Path.Combine(DataRoot, "bookmarks");

        /// <summary>
        /// 高亮目录
        /// </summary>
        public static string HighlightsDir => Path.Combine(DataRoot, "highlights");

        /// <summary>
        /// 缩略图目录
        /// </summary>
        public static string ThumbnailsDir => Path.Combine(DataDir, "Thumbnails");

        /// <summary>
        /// 音频目录
        /// </summary>
        public static string AudioDir => Path.Combine(DataDir, "Audio");

        /// <summary>
        /// TTS 缓存目录
        /// </summary>
        public static string TTSCacheDir => Path.Combine(CacheDir, "tts");

        public static string UserDataDir => Path.Combine(DataDir, "Users");
        public static string TempDir => Path.Combine(DataDir, "Temp");
        #endregion

        #region 文件路径

        /// <summary>
        /// 应用设置文件路径
        /// </summary>
        public static string AppSettingsPath => Path.Combine(ConfigDir, "appsettings.json");

        /// <summary>
        /// AI提示词配置路径
        /// </summary>
        public static string AiPromptsPath => Path.Combine(ConfigDir, "AIPrompts.json");

        /// <summary>
        /// 提示词模板配置路径
        /// </summary>
        public static string PromptTemplatesPath => Path.Combine(ConfigDir, "PromptTemplates.json");

        /// <summary>
        /// 网页书签路径
        /// </summary>
        public static string WebBookmarksPath => Path.Combine(ConfigDir, "WebBookmarks.json");

        /// <summary>
        /// 科目模板配置路径
        /// </summary>
        public static string SubjectTemplatesPath => Path.Combine(ConfigDir, "SubjectTemplates.json");

        /// <summary>
        /// 学习提醒文件路径
        /// </summary>
        public static string RemindersPath => Path.Combine(ConfigDir, "learning_reminders.json");

        /// <summary>
        /// 学习分析文件路径
        /// </summary>
        public static string AnalyticsPath => Path.Combine(ConfigDir, "learning_analytics.json");

        /// <summary>
        /// 鼓励配置文件路径
        /// </summary>
        public static string EncouragementConfigPath => Path.Combine(ConfigDir, "encouragement.json");

        /// <summary>
        /// 待处理内容文件路径
        /// </summary>
        public static string PendingContentPath => Path.Combine(ConfigDir, "pending_content.json");

        /// <summary>
        /// 上次会话文件路径
        /// </summary>
        public static string LastSessionPath => Path.Combine(SessionDir, "lastsession.json");

        /// <summary>
        /// 会话文件路径
        /// </summary>
        public static string SessionPath => Path.Combine(SessionDir, "session.json");

        /// <summary>
        /// 数据库文件路径
        /// </summary>
        public static string DatabasePath => Path.Combine(DatabaseDir, "learning_assistant.db");


        public static string UserBookmarksPath => Path.Combine(CurrentUserDir, "WebBookmarks.json");


        #endregion

        #region 方法

        /// <summary>
        /// 确保所有目录存在
        /// </summary>
        public static void EnsureDirectoriesExist()
        {
            EnsureDirectoryExists(DataRoot);
            EnsureDirectoryExists(ConfigDir);
            EnsureDirectoryExists(DataDir);
            EnsureDirectoryExists(UsersDir);
            EnsureDirectoryExists(LogsDir);
            EnsureDirectoryExists(CacheDir);
            EnsureDirectoryExists(DatabaseDir);
            EnsureDirectoryExists(SessionDir);
            EnsureDirectoryExists(ExportsDir);
            EnsureDirectoryExists(AnnotationsDir);
            EnsureDirectoryExists(TranslationsDir);
            EnsureDirectoryExists(BookmarksDir);
            EnsureDirectoryExists(HighlightsDir);
            EnsureDirectoryExists(ThumbnailsDir);
            EnsureDirectoryExists(AudioDir);
            EnsureDirectoryExists(TTSCacheDir);
            EnsureDirectoryExists(TesseractDataDir);
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        public static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// 获取用户进度文件路径
        /// </summary>
        public static string GetUserProgressPath(string userName)
        {
            return Path.Combine(UsersDir, $"{SanitizeFileName(userName)}.json");
        }

        /// <summary>
        /// 获取PDF标注文件路径
        /// </summary>
        public static string GetAnnotationPath(string pdfPath, int pageIndex)
        {
            var fileName = SanitizeFileName(Path.GetFileNameWithoutExtension(pdfPath));
            return Path.Combine(AnnotationsDir, $"{fileName}_page{pageIndex}.json");
        }

        /// <summary>
        /// 获取PDF书签文件路径
        /// </summary>
        public static string GetPdfBookmarkPath(string pdfPath)
        {
            var fileName = SanitizeFileName(Path.GetFileNameWithoutExtension(pdfPath));
            return Path.Combine(BookmarksDir, $"{fileName}_bookmarks.json");
        }

        /// <summary>
        /// 获取目录高亮文件路径
        /// </summary>
        public static string GetFolderHighlightPath(string folderPath)
        {
            var folderName = SanitizeFileName(Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar)));
            return Path.Combine(HighlightsDir, $"{folderName}_highlights.json");
        }

        #region 用户隔离路径

        /// <summary>
        /// 获取指定用户的根数据目录
        /// </summary>
        /// <param name="userId">用户ID，为空则使用当前用户</param>
        public static string GetUserDir(string? userId = null)
        {
            var actualUserId = string.IsNullOrWhiteSpace(userId) ? _currentUserId : userId;
            return Path.Combine(UsersDir, actualUserId);
        }

        /// <summary>
        /// 获取用户PDF标注目录
        /// </summary>
        public static string GetUserAnnotationsDir(string? userId = null)
        {
            return Path.Combine(GetUserDir(userId), "annotations");
        }

        /// <summary>
        /// 获取用户PDF标注文件路径
        /// </summary>
        public static string GetUserAnnotationPath(string pdfPath, int pageIndex, string? userId = null)
        {
            var fileName = SanitizeFileName(Path.GetFileNameWithoutExtension(pdfPath));
            return Path.Combine(GetUserAnnotationsDir(userId), $"{fileName}_page{pageIndex}.json");
        }

        /// <summary>
        /// 获取用户PDF书签目录
        /// </summary>
        public static string GetUserBookmarksDir(string? userId = null)
        {
            return Path.Combine(GetUserDir(userId), "bookmarks");
        }

        /// <summary>
        /// 获取用户PDF书签文件路径
        /// </summary>
        public static string GetUserPdfBookmarkPath(string pdfPath, string? userId = null)
        {
            var fileName = SanitizeFileName(Path.GetFileNameWithoutExtension(pdfPath));
            return Path.Combine(GetUserBookmarksDir(userId), $"{fileName}_bookmarks.json");
        }

        /// <summary>
        /// 获取用户高亮目录
        /// </summary>
        public static string GetUserHighlightsDir(string? userId = null)
        {
            return Path.Combine(GetUserDir(userId), "highlights");
        }

        /// <summary>
        /// 获取用户目录高亮文件路径
        /// </summary>
        public static string GetUserFolderHighlightPath(string folderPath, string? userId = null)
        {
            var folderName = SanitizeFileName(Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar)));
            return Path.Combine(GetUserHighlightsDir(userId), $"{folderName}_highlights.json");
        }

        /// <summary>
        /// 获取用户翻译缓存目录
        /// </summary>
        public static string GetUserTranslationsDir(string? userId = null)
        {
            return Path.Combine(GetUserDir(userId), "translations");
        }

        /// <summary>
        /// 获取用户TTS缓存目录
        /// </summary>
        public static string GetUserTtsCacheDir(string? userId = null)
        {
            return Path.Combine(GetUserDir(userId), "tts_cache");
        }

        /// <summary>
        /// 获取用户自动保存目录
        /// </summary>
        public static string GetUserAutoSaveDir(string? userId = null)
        {
            return Path.Combine(GetUserDir(userId), "autosave");
        }

        /// <summary>
        /// 获取用户备份目录
        /// </summary>
        public static string GetUserBackupDir(string? userId = null)
        {
            return Path.Combine(GetUserDir(userId), "backups");
        }

        /// <summary>
        /// 获取用户笔记文件路径
        /// </summary>
        public static string GetUserNotesPath(string? userId = null)
        {
            return Path.Combine(GetUserDir(userId), "notes.json");
        }

        /// <summary>
        /// 获取用户收藏夹文件路径
        /// </summary>
        public static string GetUserFavoritesPath(string? userId = null)
        {
            return Path.Combine(GetUserDir(userId), "favorites.json");
        }

        /// <summary>
        /// 获取用户错题本文件路径
        /// </summary>
        public static string GetUserWrongAnswersPath(string? userId = null)
        {
            return Path.Combine(GetUserDir(userId), "wrong_answers.json");
        }

        /// <summary>
        /// 获取用户学习目标文件路径
        /// </summary>
        public static string GetUserGoalsPath(string? userId = null)
        {
            return Path.Combine(GetUserDir(userId), "learning_goals.json");
        }

        /// <summary>
        /// 确保用户目录存在
        /// </summary>
        public static void EnsureUserDirectoriesExist(string? userId = null)
        {
            EnsureDirectoryExists(GetUserDir(userId));
            EnsureDirectoryExists(GetUserAnnotationsDir(userId));
            EnsureDirectoryExists(GetUserBookmarksDir(userId));
            EnsureDirectoryExists(GetUserHighlightsDir(userId));
            EnsureDirectoryExists(GetUserTranslationsDir(userId));
            EnsureDirectoryExists(GetUserTtsCacheDir(userId));
            EnsureDirectoryExists(GetUserAutoSaveDir(userId));
            EnsureDirectoryExists(GetUserBackupDir(userId));
        }

        #endregion

        /// <summary>
        /// 获取缓存文件路径
        /// </summary>
        public static string GetCacheFilePath(string key)
        {
            return Path.Combine(CacheDir, $"{SanitizeFileName(key)}.json");
        }

        /// <summary>
        /// 获取学习数据文件路径（内置数据优先）
        /// </summary>
        public static string GetDataFilePath(string fileName)
        {
            var userPath = Path.Combine(DataDir, fileName);
            if (File.Exists(userPath))
                return userPath;

            return userPath;
        }

        /// <summary>
        /// 查找配置文件（支持回退机制）
        /// </summary>
        public static string FindConfigFile(string fileName, string?[]? searchPaths = null)
        {
            var paths = new[]
            {
                Path.Combine(ConfigDir, fileName),
                Path.Combine(AssemblyDir, fileName),
                Path.Combine(AssemblyDir, "Models", "Config", fileName),
                Path.Combine(AssemblyDir, "Config", fileName),
            };

            if (searchPaths != null)
            {
                var customPaths = searchPaths.Where(p => !string.IsNullOrEmpty(p)).ToArray();
                paths = customPaths.Concat(paths).ToArray();
            }

            foreach (var path in paths)
            {
                if (File.Exists(path))
                    return path;
            }

            return Path.Combine(ConfigDir, fileName);
        }

        /// <summary>
        /// 清理文件名中的非法字符
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unnamed";

            var invalid = Path.GetInvalidFileNameChars();
            foreach (var c in invalid)
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }

        #endregion
    }
}
