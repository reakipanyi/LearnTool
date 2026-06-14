namespace LearningAssistant.Common
{
    /// <summary>
    /// 统一的应用路径管理器
    /// 遵循 Windows 最佳实践：
    /// - 用户数据存储在 %APPDATA%（可漫游）
    /// - 本地数据存储在 %LOCALAPPDATA%（不可漫游）
    /// - 程序集目录仅包含只读资源
    /// </summary>
    public static class AppPaths
    {
        static AppPaths()
        {
            // 首次访问时确保目录存在
            EnsureDirectoriesExist();
        }

        #region 基础路径

        /// <summary>
        /// 应用数据根目录 - 使用本地安装目录
        /// </summary>
        public static string AppDataRoot => AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// 用户数据目录 - 存储在安装目录下
        /// </summary>
        public static string DataRoot => Path.Combine(AppDataRoot, "AppData");

        /// <summary>
        /// 程序集目录（只读资源）
        /// </summary>
        public static string AssemblyDir => AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// 内置数据目录（只读）
        /// </summary>
        public static string BuiltInDataDir => Path.Combine(AssemblyDir, "Data");

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

        public static string TTSCacheDir => Path.Combine(CacheDir, "tts");

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
        /// 上次会话文件路径
        /// </summary>
        public static string LastSessionPath => Path.Combine(SessionDir, "lastsession.json");

        /// <summary>
        /// 数据库文件路径
        /// </summary>
        public static string DatabasePath => Path.Combine(DatabaseDir, "learning_assistant.db");

        #endregion

        #region 方法

        /// <summary>
        /// 确保所有目录存在
        /// </summary>
        public static void EnsureDirectoriesExist()
        {
            // 应用数据根目录
            EnsureDirectoryExists(DataRoot);

            // 子目录
            EnsureDirectoryExists(ConfigDir);
            EnsureDirectoryExists(DataDir);
            EnsureDirectoryExists(UsersDir);
            EnsureDirectoryExists(LogsDir);
            EnsureDirectoryExists(CacheDir);
            EnsureDirectoryExists(DatabaseDir);
            EnsureDirectoryExists(SessionDir);
            EnsureDirectoryExists(AnnotationsDir);
            EnsureDirectoryExists(TranslationsDir);
            EnsureDirectoryExists(BookmarksDir);
            EnsureDirectoryExists(HighlightsDir);
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
            //  用户数据目录
            var userPath = Path.Combine(DataDir, fileName);
            if (File.Exists(userPath))
                return userPath;

            // 3. 返回用户路径（用于创建新文件）
            return userPath;
        }

        /// <summary>
        /// 查找配置文件（支持回退机制）
        /// </summary>
        public static string FindConfigFile(string fileName, string?[] searchPaths = null)
        {
            // 搜索路径顺序（优先级从高到低）
            var paths = new[]
            {
                // 1. 用户配置目录（最高优先级）
                Path.Combine(ConfigDir, fileName),
                // 2. 程序集根目录
                Path.Combine(AssemblyDir, fileName),
                // 3. 程序集/Models/Config 目录
                Path.Combine(AssemblyDir, "Models", "Config", fileName),
                // 4. 程序集/Config 目录
                Path.Combine(AssemblyDir, "Config", fileName),
            };

            // 添加自定义搜索路径
            if (searchPaths != null)
            {
                var customPaths = searchPaths.Where(p => !string.IsNullOrEmpty(p)).ToArray();
                paths = customPaths.Concat(paths).ToArray();
            }

            // 返回第一个存在的文件
            foreach (var path in paths)
            {
                if (File.Exists(path))
                    return path;
            }

            // 不存在则返回用户配置目录路径
            return Path.Combine(ConfigDir, fileName);
        }

        /// <summary>
        /// 清理文件名中的非法字符
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unnamed";

            // 移除非法字符
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
