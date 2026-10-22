using LearningAssistant.Common;
using LearningAssistant.Data.Database;
using LearningAssistant.Models.Config;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using LearningAssistant.Abstractions;

namespace LearningAssistant.Services.Persistence
{
    /// <summary>
    /// SQLite 版本的数据持久化服务
    /// </summary>
    public class SqliteDataPersistenceService : IDataPersistenceService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cacheService;
        private readonly ILogger<SqliteDataPersistenceService>? _logger;
        private readonly IAppPaths _appPaths;

        public SqliteDataPersistenceService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IConfiguration configuration,
            ICacheService cacheService,
            IAppPaths appPaths,
            ILogger<SqliteDataPersistenceService>? logger = null)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _appPaths = appPaths ?? throw new ArgumentNullException(nameof(appPaths));
            _logger = logger;
        }

        public void Initialize()
        {
            try
            {
                var dbPath = _appPaths.DatabasePath;
                var dbDir = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
                {
                    Directory.CreateDirectory(dbDir);
                }

                // 只有主库已包含真实数据（多于一页、有已提交表）时才视为可用；
                // 空页头/损坏/残留旧 WAL 的场景一律重建为修正后的 schema。
                if (!HasCommitedDatabase(dbPath))
                {
                    // 主库为空（仅页头）或结构异常：丢弃残留的 WAL/SHM，避免“空主库+旧WAL”
                    // 导致 RepairSchema 解析失败（SQLite Error: Failure to parse ... Expected an ASCII digit）。
                    CleanupStaleDatabaseFiles(dbPath);

                    // 单独一个 DbContext 执行 EnsureCreated，随其 Dispose 提交全部 DDL，
                    // 避免与后续 RepairSchema 复用同一连接时，SQLite Pooling/Shared 缓存导致
                    // “CREATE TABLE 已执行但紧随其后的 CREATE INDEX 找不到表（no such table）”的问题。
                    using (var db = _dbContextFactory.CreateDbContext())
                    {
                        db.EnsureDatabaseCreated();
                    }
                }

                // 再补全 Schema：使用全新连接的上下文，对已存在的数据库幂等补建缺失的表与索引
                // （含 DailyRollups 预聚合表），不影响启动。
                using (var db = _dbContextFactory.CreateDbContext())
                {
                    db.RepairSchema();
                }

                CleanupLegacyJsonStorage();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize database");
                throw;
            }
        }

        /// <summary>
        /// 清理已被 SQLite 取代的遗留 JSON 存储（笔记、错题、目标、路径、番茄钟、统计、徽章、挑战、会话、分析、提醒）。
        /// 这些数据现已全部以数据库为唯一权威源，JSON 文件不再读写，启动时直接删除。
        /// </summary>
        private void CleanupLegacyJsonStorage()
        {
            var deleted = 0;

            void TryDeleteFile(string path)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        deleted++;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to delete legacy JSON file: {Path}", path);
                }
            }

            void TryDeleteDirectory(string path)
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, recursive: true);
                        deleted++;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to delete legacy JSON directory: {Path}", path);
                }
            }

            // 全局遗留目录与文件
            TryDeleteDirectory(Path.Combine(_appPaths.DataDir, "notes"));
            TryDeleteDirectory(Path.Combine(_appPaths.DataDir, "wrong_answers"));
            TryDeleteDirectory(Path.Combine(_appPaths.DataDir, "learning_paths"));
            TryDeleteDirectory(Path.Combine(_appPaths.UsersDir, "notes"));
            TryDeleteDirectory(Path.Combine(_appPaths.UsersDir, "wrong_answers"));
            TryDeleteDirectory(Path.Combine(_appPaths.UsersDir, "learning_paths"));

            TryDeleteFile(Path.Combine(_appPaths.ConfigDir, "learning_analytics.json"));
            TryDeleteFile(Path.Combine(_appPaths.ConfigDir, "learning_reminders.json"));
            TryDeleteFile(Path.Combine(_appPaths.SessionDir, "lastsession.json"));
            TryDeleteFile(Path.Combine(_appPaths.SessionDir, "session.json"));
            TryDeleteFile(Path.Combine(_appPaths.DataDir, "spaced_repetition.json"));

            // 按用户遗留文件
            if (Directory.Exists(_appPaths.UsersDir))
            {
                try
                {
                    // 早期进度文件：users/{userId}.json
                    foreach (var file in Directory.EnumerateFiles(_appPaths.UsersDir, "*.json"))
                    {
                        TryDeleteFile(file);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to enumerate legacy user JSON files");
                }

                try
                {
                    foreach (var userDir in Directory.EnumerateDirectories(_appPaths.UsersDir))
                    {
                        TryDeleteDirectory(Path.Combine(userDir, "goals"));
                        TryDeleteDirectory(Path.Combine(userDir, "pomodoro_stats"));

                        foreach (var fileName in new[]
                        {
                            "notes.json",
                            "wrong_answers.json",
                            "learning_goals.json",
                            "learning_analytics.json",
                            "study_stats.json",
                            "badges.json",
                            "challenges.json",
                            "challenge_history.json",
                            "progress.json",
                            "pomodoro_records.json",
                            "pomodoro_settings.json",
                            ".stats_migrated",
                            ".badges_migrated",
                            ".challenges_migrated"
                        })
                        {
                            TryDeleteFile(Path.Combine(userDir, fileName));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to enumerate legacy user directories");
                }
            }

            if (deleted > 0)
            {
                _logger?.LogInformation("Removed {Count} legacy JSON storage entries now backed by SQLite", deleted);
            }
        }

        /// <summary>
        /// 判断数据库文件是否已包含真实数据。
        /// 有效 SQLite 文件头魔数为 "SQLite format 3\0"；若主库只有单个空页（无已提交表），
        /// 则视为空库并触发重建。文件缺失、损坏或为空时返回 false。
        /// </summary>
        private static bool HasCommitedDatabase(string dbPath)
        {
            if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
            {
                return false;
            }

            var info = new FileInfo(dbPath);
            if (info.Length < 100)
            {
                return false;
            }

            try
            {
                using var fs = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (fs.Length < 100)
                {
                    return false;
                }

                var magic = new byte[16];
                if (fs.Read(magic, 0, 16) != 16)
                {
                    return false;
                }

                // 校验文件头：必须是 SQLite 数据库
                if (!magic.AsSpan(0, 15).SequenceEqual("SQLite format 3"u8) || magic[15] != 0)
                {
                    return false;
                }

                // 页大小位于文件头字节 16-17（大端），最小为 512
                fs.Seek(16, SeekOrigin.Begin);
                var pageSizeBytes = new byte[2];
                if (fs.Read(pageSizeBytes, 0, 2) != 2)
                {
                    return false;
                }
                var pageSize = (pageSizeBytes[0] << 8) | pageSizeBytes[1];

                // 仅当页大小合法且文件超过一个页（存在已提交表）才算真实库
                return pageSize >= 512 && info.Length > pageSize;
            }
            catch
            {
                // 无法读取则视为不可用，交由上层重建
                return false;
            }
        }

        /// <summary>
        /// 清理数据库残留的 WAL/SHM 附属文件（上一次非正常关闭留下，内容已过期且与旧 schema 绑定）。
        /// </summary>
        private static void CleanupStaleDatabaseFiles(string dbPath)
        {
            foreach (var suffix in new[] { "-wal", "-shm" })
            {
                var path = dbPath + suffix;
                try
                {
                    if (File.Exists(path))
                    {
                        System.Diagnostics.Trace.TraceWarning($"清理残留的数据库附属文件: {path}");
                        File.Delete(path);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceWarning($"清理数据库附属文件失败: {path}: {ex.Message}");
                }
            }
        }

        public AppConfig LoadConfig()
        {
            AppConfig config;
            try
            {
                config = _configuration.Get<AppConfig>() ?? new AppConfig();
            }
            catch (Exception ex)
            {
                // 仅当配置源读取本身失败时才返回默认配置
                _logger?.LogError(ex, "Failed to load configuration from source");
                return new AppConfig();
            }

            // 解密按字段降级（见 ConfigEncryptionHelper），不会整体抛出，
            // 避免单字段问题导致返回默认配置并在后续 SaveConfig 时覆盖磁盘原值。
            ConfigEncryptionHelper.DecryptSensitiveConfig(config);
            return config;
        }

        public void SaveConfig(AppConfig config)
        {
            try
            {
                var configJson = Common.JsonHelper.Serialize(config);
                var configToSave = Common.JsonHelper.Deserialize<AppConfig>(configJson) ?? new AppConfig();
                ConfigEncryptionHelper.EncryptSensitiveConfig(configToSave);

                var path = _appPaths.AppSettingsPath;
                Common.JsonHelper.SaveToFile(path, configToSave);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save configuration");
                throw new PersistenceException("Failed to save configuration", ex);
            }
        }

        public UserProfile LoadUserProfile(string userId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var userEntity = db.UserProfiles
                    .Include(u => u.CategoryProgresses)
                    .FirstOrDefault(u => u.UserId == userId);

                if (userEntity != null)
                {
                    var itemStates = db.LearningItemStates
                        .Where(s => s.UserId == userId)
                        .ToList();
                    return userEntity.ToModel(itemStates);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load user profile for {UserId}", userId);
            }

            return CreateDefaultProfile(userId);
        }

        public void SaveUserProfile(UserProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile, nameof(profile));
            ArgumentException.ThrowIfNullOrWhiteSpace(profile.UserId, nameof(profile.UserId));

            // AppDbContext.SaveChanges 已内置 SQLITE_BUSY/LOCKED 指数退避重试 + 全局写互斥锁，
            // 此处不再做外层重试与 Thread.Sleep，避免对逻辑错误重试以及阻塞调用线程（多为 UI 线程）。
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                using var transaction = db.Database.BeginTransaction();

                var existingUser = db.UserProfiles
                    .Include(u => u.CategoryProgresses)
                    .FirstOrDefault(u => u.UserId == profile.UserId);

                if (existingUser == null)
                {
                    var userEntity = profile.ToEntity();
                    db.UserProfiles.Add(userEntity);

                    foreach (var categoryProgress in profile.LearningProgress.CategoryProgresses.Values)
                    {
                        userEntity.CategoryProgresses.Add(categoryProgress.ToEntity(profile.UserId));
                    }
                }
                else
                {
                    existingUser.UpdateEntity(profile);

                    var categoryNamesInProfile = new HashSet<string>(profile.LearningProgress.CategoryProgresses.Keys);
                    var categoriesToRemove = existingUser.CategoryProgresses
                        .Where(c => !categoryNamesInProfile.Contains(c.CategoryName))
                        .ToList();

                    foreach (var categoryToRemove in categoriesToRemove)
                    {
                        db.CategoryProgresses.Remove(categoryToRemove);
                    }

                    foreach (var categoryProgress in profile.LearningProgress.CategoryProgresses.Values)
                    {
                        var existingCategory = existingUser.CategoryProgresses
                            .FirstOrDefault(c => c.CategoryName == categoryProgress.CategoryName);

                        if (existingCategory != null)
                        {
                            existingCategory.UpdateEntity(categoryProgress);
                        }
                        else
                        {
                            existingUser.CategoryProgresses.Add(categoryProgress.ToEntity(profile.UserId));
                        }
                    }
                }

                SaveLearningItemStates(db, profile.UserId, profile.LearningProgress.CategoryProgresses.Values);

                db.SaveChanges();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save user profile for {UserId}", profile.UserId);

                var errorMsg = $"保存用户配置失败: {profile.UserId}";
                var innerEx = ex.InnerException;
                while (innerEx != null)
                {
                    errorMsg += $" - {innerEx.Message}";
                    innerEx = innerEx.InnerException;
                }
                throw new PersistenceException(errorMsg, ex);
            }
        }

        private void SaveLearningItemStates(AppDbContext db, string userId, IEnumerable<CategoryProgress> categoryProgresses)
        {
            var existingStates = db.LearningItemStates
                .Where(s => s.UserId == userId)
                .ToList();

            var stateLookup = existingStates.ToLookup(s => (s.CategoryName, s.Content));
            // 预建 CategoryName -> CategoryProgress 索引，避免在 .Where() 内 FirstOrDefault 造成 O(states×categories)。
            var progressByCategory = categoryProgresses.ToDictionary(p => p.CategoryName);
            var now = DateTime.Now;

            foreach (var categoryProgress in categoryProgresses)
            {
                foreach (var knownItem in categoryProgress.KnownItems)
                {
                    var key = (categoryProgress.CategoryName, knownItem);
                    var existingState = stateLookup[key].FirstOrDefault();

                    if (existingState != null)
                    {
                        existingState.IsKnown = true;
                        existingState.UpdatedAt = now;
                    }
                    else
                    {
                        db.LearningItemStates.Add(new LearningItemStateEntity
                        {
                            UserId = userId,
                            CategoryName = categoryProgress.CategoryName,
                            Content = knownItem,
                            IsKnown = true,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                    }
                }

                foreach (var unknownItem in categoryProgress.UnknownItems)
                {
                    var key = (categoryProgress.CategoryName, unknownItem);
                    var existingState = stateLookup[key].FirstOrDefault();

                    if (existingState != null)
                    {
                        existingState.IsKnown = false;
                        existingState.UpdatedAt = now;
                    }
                    else
                    {
                        db.LearningItemStates.Add(new LearningItemStateEntity
                        {
                            UserId = userId,
                            CategoryName = categoryProgress.CategoryName,
                            Content = unknownItem,
                            IsKnown = false,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                    }
                }
            }

            var statesToRemove = existingStates.Where(s =>
            {
                return !progressByCategory.TryGetValue(s.CategoryName, out var progress)
                    || (!progress.KnownItems.Contains(s.Content) && !progress.UnknownItems.Contains(s.Content));
            }).ToList();

            foreach (var stateToRemove in statesToRemove)
            {
                db.LearningItemStates.Remove(stateToRemove);
            }
        }

        public List<string> GetUserIds()
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                return db.UserProfiles
                    .Select(u => u.UserId)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get user IDs");
                return new List<string>();
            }
        }

        public void CreateUserProfile(string userId, string userName)
        {
            var profile = CreateDefaultProfile(userId);
            profile.UserName = userName;
            SaveUserProfile(profile);
        }

        public bool DeleteUserProfile(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var entity = db.UserProfiles.FirstOrDefault(u => u.UserId == userId);
                if (entity == null)
                    return false;

                db.UserProfiles.Remove(entity);
                db.SaveChanges();
                _logger?.LogInformation("Deleted user profile: {UserId}", userId);

                // 清理用户文件目录（书签/收藏/标注/笔记/设置等）
                TryCleanupUserDirectory(userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to delete user profile: {UserId}", userId);
                throw new PersistenceException($"Failed to delete user profile: {userId}", ex);
            }
        }

        private void TryCleanupUserDirectory(string userId)
        {
            try
            {
                var userDir = _appPaths.GetUserDir(userId);
                if (Directory.Exists(userDir))
                    Directory.Delete(userDir, recursive: true);
            }
            catch { /* 目录清理失败不影响 DB 删除结果 */ }
        }

        public void SaveSession(SessionData session)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var sessionJson = Common.JsonHelper.Serialize(session);
                var entity = db.AppSessions.FirstOrDefault(s => s.SessionKey == "app_session");

                if (entity != null)
                {
                    entity.SessionDataJson = sessionJson;
                    entity.LastAccessTime = DateTime.Now;
                    entity.UpdatedAt = DateTime.Now;
                }
                else
                {
                    db.AppSessions.Add(new AppSessionEntity
                    {
                        SessionKey = "app_session",
                        SessionDataJson = sessionJson,
                        LastAccessTime = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save session");
                throw new PersistenceException("Failed to save session", ex);
            }
        }

        public SessionData LoadSession()
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var entity = db.AppSessions.FirstOrDefault(s => s.SessionKey == "app_session");

                if (entity != null)
                {
                    var session = Common.JsonHelper.Deserialize<SessionData>(entity.SessionDataJson);
                    if (session != null)
                    {
                        // 读操作不再产生写副作用（LastAccessTime 的更新由 SaveSession 负责），
                        // 避免每次启动都获取写锁造成争用。
                        return session;
                    }

                    // 实体存在但数据损坏：删除损坏实体，否则每次加载都会反序列化失败且无法恢复，
                    // 导致“继续上次学习”功能永久失效。
                    db.AppSessions.Remove(entity);
                    db.SaveChanges();
                    _logger?.LogWarning("Detected corrupt app session entity, removed it to allow recovery");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load session");
            }

            return new SessionData();
        }

        public void PersistCache()
        {
            try
            {
                _cacheService.Persist();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to persist cache");
            }
        }

        private UserProfile CreateDefaultProfile(string userId)
        {
            return new UserProfile
            {
                UserId = userId,
                UserName = userId,
                CreatedAt = DateTime.Now,
                LastLoginTime = DateTime.Now,
                LearningProgress = new LearningProgress()
            };
        }

        // ========== LearningItemStates 表操作方法实现 ==========

        public List<string> GetKnownItems(string userId, SubCategoryType category)
        {
            var categoryName = category.ToString();
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                return db.LearningItemStates
                    .Where(s => s.UserId == userId && s.CategoryName == categoryName && s.IsKnown)
                    .Select(s => s.Content)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get known items for user {UserId}, category {Category}", userId, category);
                return new List<string>();
            }
        }

        public List<string> GetUnknownItems(string userId, SubCategoryType category)
        {
            var categoryName = category.ToString();
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                return db.LearningItemStates
                    .Where(s => s.UserId == userId && s.CategoryName == categoryName && !s.IsKnown)
                    .Select(s => s.Content)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get unknown items for user {UserId}, category {Category}", userId, category);
                return new List<string>();
            }
        }

        public void UpsertLearningItemState(string userId, SubCategoryType category, string content, bool isKnown, string? itemKey = null)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            var categoryName = category.ToString();
            try
            {
                using var db = _dbContextFactory.CreateDbContext();

                // 双键兼容：ItemKey 优先，旧数据 ItemKey 为空时按内容回退。
                var existing = !string.IsNullOrEmpty(itemKey)
                    ? db.LearningItemStates.FirstOrDefault(s =>
                        s.UserId == userId && s.CategoryName == categoryName && s.ItemKey == itemKey)
                    : null;
                existing ??= db.LearningItemStates.FirstOrDefault(s =>
                    s.UserId == userId && s.CategoryName == categoryName &&
                    s.ItemKey == null && s.Content == content);

                if (existing != null)
                {
                    existing.IsKnown = isKnown;
                    existing.Content = content;
                    if (string.IsNullOrEmpty(existing.ItemKey) && !string.IsNullOrEmpty(itemKey))
                    {
                        existing.ItemKey = itemKey;
                    }
                    existing.UpdatedAt = DateTime.Now;
                }
                else
                {
                    db.LearningItemStates.Add(new LearningItemStateEntity
                    {
                        UserId = userId,
                        CategoryName = categoryName,
                        Content = content,
                        ItemKey = itemKey,
                        IsKnown = isKnown,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                }

                db.SaveChanges();
                _logger?.LogDebug("Upserted learning item state: user {UserId}, category {Category}, content {Content}, isKnown {IsKnown}",
                    userId, category, content.Length > 20 ? content.Substring(0, 20) + "..." : content, isKnown);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to upsert learning item state for user {UserId}, content {Content}", userId, content);
            }
        }

        public void UpsertLearningItemStates(string userId, SubCategoryType category, IEnumerable<string> contents, bool isKnown)
        {
            var categoryName = category.ToString();
            try
            {
                var contentList = contents.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
                if (contentList.Count == 0)
                    return;

                // 分批处理，避免 IN 子句参数超过 SQLite 默认 999 限制。
                const int batchSize = 500;
                var now = DateTime.Now;
                var totalProcessed = 0;

                for (int i = 0; i < contentList.Count; i += batchSize)
                {
                    var batch = contentList.Skip(i).Take(batchSize).ToList();

                    using var db = _dbContextFactory.CreateDbContext();
                    var existingEntities = db.LearningItemStates
                        .Where(s => s.UserId == userId && s.CategoryName == categoryName && batch.Contains(s.Content))
                        .ToDictionary(s => s.Content);

                    foreach (var content in batch)
                    {
                        if (existingEntities.TryGetValue(content, out var existing))
                        {
                            existing.IsKnown = isKnown;
                            existing.UpdatedAt = now;
                        }
                        else
                        {
                            db.LearningItemStates.Add(new LearningItemStateEntity
                            {
                                UserId = userId,
                                CategoryName = categoryName,
                                Content = content,
                                IsKnown = isKnown,
                                CreatedAt = now,
                                UpdatedAt = now
                            });
                        }
                    }

                    db.SaveChanges();
                    totalProcessed += batch.Count;
                }

                _logger?.LogDebug("Batch upserted {Count} learning item states for user {UserId}, category {Category}",
                    totalProcessed, userId, category);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to batch upsert learning item states for user {UserId}, category {Category}",
                    userId, category);
            }
        }

        public void DeleteLearningItemState(string userId, SubCategoryType category, string content)
        {
            var categoryName = category.ToString();
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var existing = db.LearningItemStates
                    .FirstOrDefault(s => s.UserId == userId && s.CategoryName == categoryName && s.Content == content);

                if (existing != null)
                {
                    db.LearningItemStates.Remove(existing);
                    db.SaveChanges();
                    _logger?.LogDebug("Deleted learning item state: user {UserId}, category {Category}, content {Content}",
                        userId, category, content);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to delete learning item state for user {UserId}, content {Content}", userId, content);
            }
        }

        public void SyncCategoryProgressToLearningItemStates(
            string userId,
            SubCategoryType category,
            List<string> knownItems,
            List<string> unknownItems,
            IReadOnlyDictionary<string, string>? itemKeyMap = null)
        {
            var categoryName = category.ToString();
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                using var transaction = db.Database.BeginTransaction();

                var existingStates = db.LearningItemStates
                    .Where(s => s.UserId == userId && s.CategoryName == categoryName)
                    .ToList();

                // 改为 upsert：ItemKey 优先匹配，旧数据按 Content 回退，并补写 ItemKey。
                var stateByContent = existingStates
                    .GroupBy(s => s.Content, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
                var stateByKey = existingStates
                    .Where(s => !string.IsNullOrEmpty(s.ItemKey))
                    .GroupBy(s => s.ItemKey!, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

                var now = DateTime.Now;
                var matchedIds = new HashSet<int>();

                void Apply(string content, bool isKnown)
                {
                    if (string.IsNullOrWhiteSpace(content)) return;

                    string? itemKey = null;
                    itemKeyMap?.TryGetValue(content, out itemKey);

                    LearningItemStateEntity? state = null;
                    if (!string.IsNullOrEmpty(itemKey))
                    {
                        stateByKey.TryGetValue(itemKey, out state);
                    }
                    state ??= stateByContent.GetValueOrDefault(content);

                    if (state != null)
                    {
                        matchedIds.Add(state.Id);
                        if (!string.Equals(state.Content, content, StringComparison.Ordinal))
                        {
                            state.Content = content;
                        }
                        if (string.IsNullOrEmpty(state.ItemKey) && !string.IsNullOrEmpty(itemKey))
                        {
                            state.ItemKey = itemKey;
                        }
                        if (state.IsKnown != isKnown)
                        {
                            state.IsKnown = isKnown;
                        }
                        state.UpdatedAt = now;
                    }
                    else
                    {
                        var newState = new LearningItemStateEntity
                        {
                            UserId = userId,
                            CategoryName = categoryName,
                            Content = content,
                            ItemKey = itemKey,
                            IsKnown = isKnown,
                            CreatedAt = now,
                            UpdatedAt = now
                        };
                        db.LearningItemStates.Add(newState);
                        stateByContent[content] = newState;
                        if (!string.IsNullOrEmpty(itemKey))
                        {
                            stateByKey[itemKey] = newState;
                        }
                    }
                }

                foreach (var content in knownItems.Where(c => !string.IsNullOrWhiteSpace(c)))
                {
                    Apply(content, true);
                }

                foreach (var content in unknownItems.Where(c => !string.IsNullOrWhiteSpace(c)))
                {
                    Apply(content, false);
                }

                foreach (var stateToRemove in existingStates.Where(s => !matchedIds.Contains(s.Id)))
                {
                    db.LearningItemStates.Remove(stateToRemove);
                }

                db.SaveChanges();
                transaction.Commit();
                _logger?.LogInformation("Synced category progress to LearningItemStates: user {UserId}, category {Category}, known {KnownCount}, unknown {UnknownCount}",
                    userId, category, knownItems.Count, unknownItems.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to sync category progress for user {UserId}, category {Category}", userId, category);
            }
        }
    }
}
