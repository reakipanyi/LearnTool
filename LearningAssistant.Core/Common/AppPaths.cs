namespace LearningAssistant.Common
{
    /// <summary>
    /// 统一的应用路径管理器。目录按“是否参与同步”分为两类：
    /// 1. 需同步的用户数据（配置、数据库、学习数据等）——桌面端位于“文档\LearningAssistant”；
    /// 2. 不需同步的运行时产物（缓存、日志、会话、临时文件）——位于程序目录下的 AppCache。
    /// </summary>
    public static class AppPaths
    {
        static AppPaths()
        {
            TryMigrateLegacyLayout();
            EnsureDirectoriesExist();
        }

        #region 基础路径

        /// <summary>
        /// 程序集根目录（只读资源）
        /// </summary>
        public static string AssemblyDir => AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// 需同步的数据根目录（配置/数据库/学习数据等）。
        /// 桌面端默认位于“文档\LearningAssistant”，不随构建输出目录变化；测试/工具进程回退到程序目录下的 AppData。
        /// 可用环境变量 LEARNING_ASSISTANT_DATA_ROOT 覆盖。
        /// </summary>
        public static string DataRoot => _dataRoot;

        private static readonly string _dataRoot = ResolveDataRoot();

        /// <summary>
        /// 不需同步的缓存根目录（缓存/日志/会话/临时文件），位于程序目录下的 AppCache。
        /// </summary>
        public static string CacheRoot => Path.Combine(AssemblyDir, "AppCache");

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

        private static string _currentUserId = Constants.DefaultUserId;

        /// <summary>
        /// 设置当前用户ID
        /// </summary>
        public static void SetCurrentUserId(string userId)
        {
            _currentUserId = string.IsNullOrWhiteSpace(userId) ? Constants.DefaultUserId : userId;
            EnsureUserDirectoriesExist();
        }

        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        public static string GetCurrentUserId() => _currentUserId;

        /// <summary>
        /// 日志目录（不同步）
        /// </summary>
        public static string LogsDir => Path.Combine(CacheRoot, "logs");

        /// <summary>
        /// 缓存目录（不同步）
        /// </summary>
        public static string CacheDir => CacheRoot;

        /// <summary>
        /// 数据库目录（同步）
        /// </summary>
        public static string DatabaseDir => Path.Combine(DataRoot, "database");

        /// <summary>
        /// 会话目录（不同步）
        /// </summary>
        public static string SessionDir => Path.Combine(CacheRoot, "session");

        /// <summary>
        /// 用户级缓存根目录（不同步）：按用户隔离的 PDF 索引/AI/翻译等可再生成的缓存。
        /// </summary>
        public static string UserCacheRoot => Path.Combine(CacheRoot, "users");

        /// <summary>
        /// 当前用户缓存目录（不同步）
        /// </summary>
        public static string CurrentUserCacheDir => Path.Combine(UserCacheRoot, _currentUserId);

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
        public static string FileBookmarksDir => Path.Combine(DataRoot, "FileBookmarks");

        /// <summary>
        /// 高亮目录
        /// </summary>
        public static string HighlightsDir => Path.Combine(DataRoot, "highlights");

        /// <summary>
        /// 推荐反馈目录（全局共享，按用户分文件）
        /// </summary>
        public static string RecommendationFeedbackDir => Path.Combine(DataDir, "recommendation_feedback");

        /// <summary>
        /// 缩略图目录
        /// </summary>
        public static string ThumbnailsDir => Path.Combine(DataDir, "Thumbnails");

        /// <summary>
        /// 音频目录
        /// </summary>
        public static string AudioDir => Path.Combine(DataDir, "Audio");

        /// <summary>
        /// PDF 配套音频 profile 目录（按 PDF 哈希存 sidecar）
        /// </summary>
        public static string AudioProfilesDir => Path.Combine(AudioDir, "Profiles");

        /// <summary>
        /// 句子跟读清单目录（按用户 + PDF 哈希存分句结果）
        /// </summary>
        public static string ShadowingDir => Path.Combine(DataDir, "Shadowings");

        /// <summary>
        /// PDF 文本索引目录（按用户 + 文件指纹缓存页级文本，不同步）
        /// </summary>
        public static string PdfTextIndexDir => Path.Combine(CurrentUserCacheDir, "pdf_text_index");

        /// <summary>
        /// PDF AI 结果磁盘缓存目录（按用户隔离，LRU 淘汰，不同步）
        /// </summary>
        public static string PdfAiCacheDir => Path.Combine(CurrentUserCacheDir, "pdf_ai_cache");

        /// <summary>
        /// PDF AI 调用用量目录（按文档指纹 + 日期记录当日调用次数，用于成本上限）
        /// </summary>
        public static string PdfAiUsageDir => Path.Combine(CurrentUserDir, "pdf_ai_usage");

        /// <summary>
        /// PDF AI 摘要条目目录（按用户 + 文档哈希存页码/录音锚点）
        /// </summary>
        public static string PdfAiSummaryDir => Path.Combine(CurrentUserDir, "pdf_ai_summaries");

        /// <summary>
        /// PDF 翻译术语表目录（按用户 + 文档指纹缓存，不同步）
        /// </summary>
        public static string PdfTranslationGlossaryDir => Path.Combine(CurrentUserCacheDir, "pdf_translation_glossary");

        /// <summary>
        /// PDF 选区翻译缓存目录（按用户 + 文档指纹 + 页码 + OCR 文本存放，不同步）
        /// </summary>
        public static string PdfSelectionTranslationDir => Path.Combine(CurrentUserCacheDir, "pdf_selection_translations");

        /// <summary>
        /// PDF 自动框选版面缓存目录（按用户 + 文档指纹 + 页码存放段落块，不同步）
        /// </summary>
        public static string PdfAutoLayoutDir => Path.Combine(CurrentUserCacheDir, "pdf_auto_layout");

        /// <summary>
        /// PDF 连续朗读断点目录（按用户 + 文档路径与修改时间存放页号/句号）
        /// </summary>
        public static string PdfReadingPositionDir => Path.Combine(CurrentUserDir, "pdf_reading_positions");

        /// <summary>
        /// 音频句子时间轴缓存目录（按音频指纹存放，可重建，不同步）
        /// </summary>
        public static string AudioTimelinesDir => Path.Combine(CurrentUserCacheDir, "audio_timelines");

        /// <summary>
        /// PDF 阅读时长统计目录（按用户 + 文档 + 日期累计）
        /// </summary>
        public static string PdfReadingStatsDir => Path.Combine(CurrentUserDir, "pdf_reading_stats");

        /// <summary>
        /// TTS 缓存目录
        /// </summary>
        public static string TTSCacheDir => Path.Combine(CacheDir, "tts");

        public static string UserDataDir => Path.Combine(DataDir, "Users");
        /// <summary>
        /// 临时文件目录（不同步）
        /// </summary>
        public static string TempDir => Path.Combine(CacheDir, "temp");
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
        /// 网页书签路径（全局共享，不按用户隔离）
        /// </summary>
        public static string WebBookmarksPath => Path.Combine(ConfigDir, "WebBookmarks.json");

        /// <summary>
        /// 科目模板配置路径
        /// </summary>
        public static string SubjectTemplatesPath => Path.Combine(ConfigDir, "SubjectTemplates.json");

        /// <summary>
        /// 鼓励配置文件路径
        /// </summary>
        public static string EncouragementConfigPath => Path.Combine(ConfigDir, "encouragement.json");

        /// <summary>
        /// 待处理内容文件路径
        /// </summary>
        public static string PendingContentPath => Path.Combine(ConfigDir, "pending_content.json");

        /// <summary>
        /// 浏览器标签页状态文件路径
        /// </summary>
        public static string BrowserTabsPath => Path.Combine(SessionDir, "browser_tabs.json");

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
            EnsureDirectoryExists(CacheRoot);
            EnsureDirectoryExists(ConfigDir);
            EnsureDirectoryExists(DataDir);
            EnsureDirectoryExists(UsersDir);
            EnsureDirectoryExists(UserCacheRoot);
            EnsureDirectoryExists(CurrentUserCacheDir);
            EnsureDirectoryExists(LogsDir);
            EnsureDirectoryExists(CacheDir);
            EnsureDirectoryExists(DatabaseDir);
            EnsureDirectoryExists(SessionDir);
            EnsureDirectoryExists(TempDir);
            EnsureDirectoryExists(ExportsDir);
            EnsureDirectoryExists(AnnotationsDir);
            EnsureDirectoryExists(TranslationsDir);
            EnsureDirectoryExists(FileBookmarksDir);
            EnsureDirectoryExists(HighlightsDir);
            EnsureDirectoryExists(ThumbnailsDir);
            EnsureDirectoryExists(AudioDir);
            EnsureDirectoryExists(AudioProfilesDir);
            EnsureDirectoryExists(ShadowingDir);
            EnsureDirectoryExists(PdfTextIndexDir);
            EnsureDirectoryExists(PdfAiCacheDir);
            EnsureDirectoryExists(PdfAiUsageDir);
            EnsureDirectoryExists(PdfAiSummaryDir);
            EnsureDirectoryExists(PdfTranslationGlossaryDir);
            EnsureDirectoryExists(PdfSelectionTranslationDir);
            EnsureDirectoryExists(PdfAutoLayoutDir);
            EnsureDirectoryExists(PdfReadingPositionDir);
            EnsureDirectoryExists(PdfReadingStatsDir);
            EnsureDirectoryExists(AudioTimelinesDir);
            EnsureDirectoryExists(TTSCacheDir);
            EnsureDirectoryExists(TesseractDataDir);
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        public static void EnsureDirectoryExists(string path)
        {
            path = path.Replace("\r\n", "");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        #region 旧目录迁移

        /// <summary>旧布局中属于“不同步”的目录名。</summary>
        private static readonly string[] LegacyRuntimeDirNames = { "cache", "logs", "session" };

        /// <summary>旧布局中位于用户目录下、实际属于“不同步缓存”的子目录名。</summary>
        private static readonly string[] LegacyUserCacheDirNames =
        {
            "pdf_text_index", "pdf_ai_cache", "pdf_translation_glossary",
            "pdf_selection_translations", "pdf_auto_layout"
        };

        /// <summary>
        /// 桌面端数据根目录：默认“文档\LearningAssistant”；
        /// 测试/工具进程（非主程序入口）回退到程序目录下的 AppData，避免污染真实用户数据。
        /// </summary>
        private static string ResolveDataRoot()
        {
            var overridePath = Environment.GetEnvironmentVariable("LEARNING_ASSISTANT_DATA_ROOT");
            if (!string.IsNullOrWhiteSpace(overridePath))
                return Path.GetFullPath(overridePath);

            var entryName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
            if (string.Equals(entryName, "LearningAssistant.WinApp", StringComparison.OrdinalIgnoreCase))
            {
                var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (!string.IsNullOrWhiteSpace(documents))
                    return Path.Combine(documents, "LearningAssistant");
            }

            return Path.Combine(AssemblyDir, "AppData");
        }

        /// <summary>
        /// 把旧布局（程序目录\AppData 内混放全部数据）迁移到新布局：
        /// 需同步的内容移入 DataRoot，日志/会话移入 CacheRoot，旧 cache 与旧临时目录异步清理。
        /// 迁移是幂等的，且失败时不阻塞启动。
        /// </summary>
        private static void TryMigrateLegacyLayout()
        {
            try
            {
                var legacyRoot = Path.Combine(AssemblyDir, "AppData");
                if (!Directory.Exists(legacyRoot) || AreSamePath(legacyRoot, DataRoot))
                    return;

                EnsureDirectoryExists(DataRoot);
                EnsureDirectoryExists(CacheRoot);

                // 1. 同步类数据整体迁移（cache/logs/session 除外）
                foreach (var entry in Directory.GetFileSystemEntries(legacyRoot))
                {
                    var name = Path.GetFileName(entry);
                    if (IsLegacyRuntimeDirectory(name))
                        continue;

                    MergeInto(entry, Path.Combine(DataRoot, name));
                }

                // 2. 日志与会话属于不同步的运行时产物
                MergeInto(Path.Combine(legacyRoot, "logs"), LogsDir);
                MergeInto(Path.Combine(legacyRoot, "session"), SessionDir);

                // 3. 用户目录里混放的 PDF 缓存移到专门的缓存根
                MigrateUserCaches();

                // 4. 旧缓存与旧临时目录不再使用，后台清理避免阻塞启动
                var legacyCache = Path.Combine(legacyRoot, "cache");
                if (Directory.Exists(legacyCache))
                    Task.Run(() => DeleteDirectoryQuietly(legacyCache));

                DeleteDirectoryQuietly(Path.Combine(DataRoot, "data", "Temp"));
            }
            catch
            {
                // 迁移失败时保留旧目录，下次启动继续尝试
            }
        }

        private static void MigrateUserCaches()
        {
            var usersRoot = Path.Combine(DataRoot, "users");
            if (!Directory.Exists(usersRoot))
                return;

            foreach (var userDir in Directory.GetDirectories(usersRoot))
            {
                var userId = Path.GetFileName(userDir);
                foreach (var cacheName in LegacyUserCacheDirNames)
                {
                    var source = Path.Combine(userDir, cacheName);
                    if (Directory.Exists(source))
                        MergeInto(source, Path.Combine(UserCacheRoot, userId, cacheName));
                }
            }
        }

        /// <summary>
        /// 把 source（文件或目录）并入 target：target 不存在时直接移动，存在时逐文件补充缺失项。
        /// 目标已存在同名文件时保留目标版本；SQLite 的 -wal/-shm/-journal 属瞬态文件，不迁移。
        /// </summary>
        private static void MergeInto(string source, string target)
        {
            try
            {
                if (Directory.Exists(source))
                {
                    if (!Directory.Exists(target))
                    {
                        EnsureDirectoryExists(Path.GetDirectoryName(target) ?? DataRoot);
                        MoveDirectory(source, target);
                        return;
                    }

                    foreach (var file in Directory.GetFiles(source))
                    {
                        var targetFile = Path.Combine(target, Path.GetFileName(file));
                        if (File.Exists(targetFile) || IsSqliteTransientFile(file))
                            continue;

                        MoveFile(file, targetFile);
                    }

                    foreach (var directory in Directory.GetDirectories(source))
                        MergeInto(directory, Path.Combine(target, Path.GetFileName(directory)));

                    TryDeleteEmptyDirectory(source);
                    return;
                }

                if (File.Exists(source) && !File.Exists(target) && !IsSqliteTransientFile(source))
                {
                    EnsureDirectoryExists(Path.GetDirectoryName(target) ?? DataRoot);
                    MoveFile(source, target);
                }
            }
            catch
            {
                // 单个条目迁移失败时跳过，保留旧文件
            }
        }

        /// <summary>移动文件；跨盘时退化为“复制 + 删除”，复制成功后才删除源文件。</summary>
        private static void MoveFile(string source, string target)
        {
            try
            {
                File.Move(source, target);
            }
            catch (IOException)
            {
                File.Copy(source, target);
                File.Delete(source);
            }
        }

        /// <summary>移动目录；跨盘时退化为“递归复制 + 删除”，复制成功后才删除源目录。</summary>
        private static void MoveDirectory(string source, string target)
        {
            try
            {
                Directory.Move(source, target);
                return;
            }
            catch (IOException)
            {
                // 跨卷移动会以 IOException 失败，改用复制
            }

            CopyDirectory(source, target);
            Directory.Delete(source, true);
        }

        private static void CopyDirectory(string source, string target)
        {
            EnsureDirectoryExists(target);
            foreach (var file in Directory.GetFiles(source))
            {
                var targetFile = Path.Combine(target, Path.GetFileName(file));
                if (!File.Exists(targetFile))
                    File.Copy(file, targetFile);
            }

            foreach (var directory in Directory.GetDirectories(source))
                CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory)));
        }

        private static bool IsLegacyRuntimeDirectory(string name)
        {
            foreach (var runtimeName in LegacyRuntimeDirNames)
            {
                if (string.Equals(name, runtimeName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool IsSqliteTransientFile(string path)
        {
            return path.EndsWith("-wal", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith("-shm", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith("-journal", StringComparison.OrdinalIgnoreCase);
        }

        private static bool AreSamePath(string left, string right)
        {
            return string.Equals(
                Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }

        private static void TryDeleteEmptyDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path) && Directory.GetFileSystemEntries(path).Length == 0)
                    Directory.Delete(path, false);
            }
            catch
            {
                // 忽略删除失败
            }
        }

        private static void DeleteDirectoryQuietly(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch
            {
                // 忽略删除失败
            }
        }

        #endregion

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
            return Path.Combine(FileBookmarksDir, $"{fileName}_bookmarks.json");
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
        /// 获取用户 PDF 文本索引目录
        /// </summary>
        public static string GetUserPdfTextIndexDir(string? userId = null)
        {
            return Path.Combine(GetUserCacheDir(userId), "pdf_text_index");
        }

        /// <summary>
        /// 获取用户 PDF AI 结果缓存目录
        /// </summary>
        public static string GetUserPdfAiCacheDir(string? userId = null)
        {
            return Path.Combine(GetUserCacheDir(userId), "pdf_ai_cache");
        }

        /// <summary>
        /// 获取用户 PDF AI 摘要条目目录
        /// </summary>
        public static string GetUserPdfAiSummaryDir(string? userId = null)
        {
            return Path.Combine(GetUserDir(userId), "pdf_ai_summaries");
        }

        /// <summary>
        /// 获取用户 PDF 翻译术语表目录
        /// </summary>
        public static string GetUserPdfTranslationGlossaryDir(string? userId = null)
        {
            return Path.Combine(GetUserCacheDir(userId), "pdf_translation_glossary");
        }

        /// <summary>
        /// 获取用户 PDF 选区翻译缓存目录
        /// </summary>
        public static string GetUserPdfSelectionTranslationDir(string? userId = null)
        {
            return Path.Combine(GetUserCacheDir(userId), "pdf_selection_translations");
        }

        /// <summary>
        /// 获取用户缓存根目录（不同步）
        /// </summary>
        public static string GetUserCacheDir(string? userId = null)
        {
            var actualUserId = string.IsNullOrWhiteSpace(userId) ? _currentUserId : userId;
            return Path.Combine(UserCacheRoot, actualUserId);
        }

        /// <summary>
        /// 获取TTS缓存目录
        /// </summary>
        public static string GetTtsCacheDir()
        {
            return Path.Combine(CacheDir, "tts");
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
        /// 获取用户收藏夹文件路径
        /// </summary>
        public static string GetUserFavoritesPath(string? userId = null)
        {
            return Path.Combine(GetUserDir(userId), "favorites.json");
        }

        /// <summary>
        /// 确保用户目录存在
        /// </summary>
        public static void EnsureUserDirectoriesExist(string? userId = null)
        {
            EnsureDirectoryExists(GetUserDir(userId));
            EnsureDirectoryExists(GetUserCacheDir(userId));
            EnsureDirectoryExists(GetUserAnnotationsDir(userId));
            EnsureDirectoryExists(GetUserBookmarksDir(userId));
            EnsureDirectoryExists(GetUserHighlightsDir(userId));
            EnsureDirectoryExists(GetUserTranslationsDir(userId));
            EnsureDirectoryExists(GetUserPdfTextIndexDir(userId));
            EnsureDirectoryExists(GetUserPdfAiCacheDir(userId));
            EnsureDirectoryExists(GetUserPdfAiSummaryDir(userId));
            EnsureDirectoryExists(GetUserPdfTranslationGlossaryDir(userId));
            EnsureDirectoryExists(GetUserPdfSelectionTranslationDir(userId));
            EnsureDirectoryExists(Path.Combine(GetUserDir(userId), "pdf_auto_layout"));
            EnsureDirectoryExists(Path.Combine(GetUserDir(userId), "pdf_reading_positions"));
            EnsureDirectoryExists(Path.Combine(GetUserDir(userId), "pdf_reading_stats"));
            EnsureDirectoryExists(GetTtsCacheDir());
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
