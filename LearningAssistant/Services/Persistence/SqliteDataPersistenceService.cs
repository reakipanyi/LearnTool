using LearningAssistant.Common;
using LearningAssistant.Data.Database;
using LearningAssistant.Models.Config;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading;

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

        public SqliteDataPersistenceService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IConfiguration configuration,
            ICacheService cacheService,
            ILogger<SqliteDataPersistenceService>? logger = null)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger;
        }

        public void Initialize()
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                db.EnsureDatabaseCreated();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize database");
                throw;
            }
        }

        public AppConfig LoadConfig()
        {
            try
            {
                var config = _configuration.Get<AppConfig>() ?? new AppConfig();
                ConfigEncryptionHelper.DecryptSensitiveConfig(config);
                return config;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load configuration");
                return new AppConfig();
            }
        }

        public void SaveConfig(AppConfig config)
        {
            try
            {
                var configToSave = config;
                ConfigEncryptionHelper.EncryptSensitiveConfig(configToSave);

                var path = AppPaths.AppSettingsPath;
                Common.JsonHelper.SaveToFile(path, configToSave);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save configuration");
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
                    return userEntity.ToModel();
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

            const int maxRetries = 3;
            const int retryDelayMs = 500;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using var db = _dbContextFactory.CreateDbContext();
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

                    db.SaveChanges();
                    return;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to save user profile for {UserId} (attempt {Attempt}/{MaxRetries})", profile.UserId, attempt, maxRetries);

                    if (attempt >= maxRetries)
                    {
                        var errorMsg = $"保存用户配置失败: {profile.UserId}";
                        var innerEx = ex.InnerException;
                        while (innerEx != null)
                        {
                            errorMsg += $" - {innerEx.Message}";
                            innerEx = innerEx.InnerException;
                        }
                        throw new PersistenceException(errorMsg, ex);
                    }

                    Thread.Sleep(retryDelayMs);
                }
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
                        entity.LastAccessTime = DateTime.Now;
                        db.SaveChanges();
                        return session;
                    }
                }

                // 尝试从旧的 JSON 文件迁移
                var path = AppPaths.LastSessionPath;
                if (File.Exists(path))
                {
                    var oldSession = Common.JsonHelper.LoadFromFile<SessionData>(path);
                    if (oldSession != null)
                    {
                        SaveSession(oldSession);
                        _logger?.LogInformation("Migrated session data from JSON to SQLite");
                        return oldSession;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load session");
            }

            return new SessionData();
        }

        public T? LoadJsonFile<T>(string filePath)
        {
            return Common.JsonHelper.LoadFromFile<T>(filePath);
        }

        public void SaveJsonFile<T>(string filePath, T data)
        {
            Common.JsonHelper.SaveToFile(filePath, data);
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

        public void UpsertLearningItemState(string userId, SubCategoryType category, string content, bool isKnown)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            var categoryName = category.ToString();
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var existing = db.LearningItemStates
                    .FirstOrDefault(s => s.UserId == userId && s.CategoryName == categoryName && s.Content == content);

                if (existing != null)
                {
                    existing.IsKnown = isKnown;
                    existing.UpdatedAt = DateTime.Now;
                }
                else
                {
                    db.LearningItemStates.Add(new LearningItemStateEntity
                    {
                        UserId = userId,
                        CategoryName = categoryName,
                        Content = content,
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
                using var db = _dbContextFactory.CreateDbContext();
                var contentList = contents.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();

                if (contentList.Count == 0)
                    return;

                var existingContents = db.LearningItemStates
                    .Where(s => s.UserId == userId && s.CategoryName == categoryName && contentList.Contains(s.Content))
                    .Select(s => s.Content)
                    .ToHashSet();

                var now = DateTime.Now;
                var entitiesToAdd = new List<LearningItemStateEntity>();
                var entitiesToUpdate = new List<LearningItemStateEntity>();

                foreach (var content in contentList)
                {
                    if (existingContents.Contains(content))
                    {
                        entitiesToUpdate.Add(new LearningItemStateEntity
                        {
                            UserId = userId,
                            CategoryName = categoryName,
                            Content = content,
                            IsKnown = isKnown,
                            UpdatedAt = now
                        });
                    }
                    else
                    {
                        entitiesToAdd.Add(new LearningItemStateEntity
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

                if (entitiesToUpdate.Count > 0)
                {
                    foreach (var entity in entitiesToUpdate)
                    {
                        var existing = db.LearningItemStates
                            .FirstOrDefault(s => s.UserId == entity.UserId &&
                                               s.CategoryName == entity.CategoryName &&
                                               s.Content == entity.Content);
                        if (existing != null)
                        {
                            existing.IsKnown = entity.IsKnown;
                            existing.UpdatedAt = entity.UpdatedAt;
                        }
                    }
                }

                if (entitiesToAdd.Count > 0)
                {
                    db.LearningItemStates.AddRange(entitiesToAdd);
                }

                db.SaveChanges();
                _logger?.LogDebug("Batch upserted {Count} learning item states for user {UserId}, category {Category}",
                    contentList.Count, userId, category);
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

        public void SyncCategoryProgressToLearningItemStates(string userId, SubCategoryType category, List<string> knownItems, List<string> unknownItems)
        {
            var categoryName = category.ToString();
            try
            {
                using var db = _dbContextFactory.CreateDbContext();

                var existingStates = db.LearningItemStates
                    .Where(s => s.UserId == userId && s.CategoryName == categoryName)
                    .ToList();

                db.LearningItemStates.RemoveRange(existingStates);

                foreach (var content in knownItems.Where(c => !string.IsNullOrWhiteSpace(c)))
                {
                    db.LearningItemStates.Add(new LearningItemStateEntity
                    {
                        UserId = userId,
                        CategoryName = categoryName,
                        Content = content,
                        IsKnown = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                }

                foreach (var content in unknownItems.Where(c => !string.IsNullOrWhiteSpace(c)))
                {
                    db.LearningItemStates.Add(new LearningItemStateEntity
                    {
                        UserId = userId,
                        CategoryName = categoryName,
                        Content = content,
                        IsKnown = false,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                }

                db.SaveChanges();
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
