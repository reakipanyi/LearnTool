using LearningAssistant.Common;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

using LearningAssistant.Abstractions;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 收藏服务实现
    /// 提供学习项收藏功能,包括增删改查和缓存管理
    /// 从 LearningForm 中提取的收藏管理逻辑
    /// </summary>
    public class FavoriteService : IFavoriteService
    {
        private readonly ILogger<FavoriteService> _logger;
        private readonly ConcurrentDictionary<string, CachedFavorites> _favoritesCache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(10);
        private readonly IAppPaths _appPaths;

        public FavoriteService(ILogger<FavoriteService> logger, IAppPaths appPaths)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appPaths = appPaths;
            _favoritesCache = new ConcurrentDictionary<string, CachedFavorites>();
        }

        public async Task<bool> IsFavoriteAsync(string userId, SubCategoryType subCategory, string content)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(content))
                return false;

            try
            {
                var favorites = await GetUserFavoritesAsync(userId);
                string key = GenerateFavoriteKey(subCategory, content);
                return favorites.Contains(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查收藏状态失败, UserId: {UserId}, SubCategory: {SubCategory}, Content: {Content}",
                    userId, subCategory, content);
                return false;
            }
        }

        public async Task AddFavoriteAsync(string userId, SubCategoryType subCategory, string content)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("添加收藏失败: 参数为空");
                return;
            }

            try
            {
                var favorites = await LoadFavoritesFromFileAsync(userId);
                string key = GenerateFavoriteKey(subCategory, content);

                if (!favorites.Contains(key))
                {
                    favorites.Add(key);
                    await SaveFavoritesToFileAsync(userId, favorites);
                    InvalidateCache(userId);

                    _logger.LogInformation("添加收藏成功: UserId={UserId}, Key={Key}", userId, key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加收藏失败, UserId: {UserId}, SubCategory: {SubCategory}, Content: {Content}",
                    userId, subCategory, content);
                throw;
            }
        }

        public async Task RemoveFavoriteAsync(string userId, SubCategoryType subCategory, string content)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("移除收藏失败: 参数为空");
                return;
            }

            try
            {
                var favorites = await LoadFavoritesFromFileAsync(userId);
                string key = GenerateFavoriteKey(subCategory, content);

                if (favorites.Remove(key))
                {
                    await SaveFavoritesToFileAsync(userId, favorites);
                    InvalidateCache(userId);

                    _logger.LogInformation("移除收藏成功: UserId={UserId}, Key={Key}", userId, key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除收藏失败, UserId: {UserId}, SubCategory: {SubCategory}, Content: {Content}",
                    userId, subCategory, content);
                throw;
            }
        }

        public async Task<HashSet<string>> GetUserFavoritesAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new HashSet<string>();

            try
            {
                // 检查缓存
                if (_favoritesCache.TryGetValue(userId, out var cached) &&
                    DateTime.Now - cached.Timestamp < CacheDuration)
                {
                    return cached.Favorites;
                }

                // 从文件加载
                var favorites = await LoadFavoritesFromFileAsync(userId);

                // 更新缓存
                _favoritesCache[userId] = new CachedFavorites(favorites, DateTime.Now);

                return favorites;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户收藏失败, UserId: {UserId}", userId);
                return new HashSet<string>();
            }
        }

        public void InvalidateCache(string userId)
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                _favoritesCache.TryRemove(userId, out _);
                _logger.LogDebug("收藏缓存已失效, UserId: {UserId}", userId);
            }
        }

        #region === 私有方法 ===

        /// <summary>
        /// 从文件加载收藏列表
        /// </summary>
        private async Task<HashSet<string>> LoadFavoritesFromFileAsync(string userId)
        {
            string favoritesPath = GetFavoritesPath(userId);

            if (!File.Exists(favoritesPath))
            {
                return new HashSet<string>();
            }

            try
            {
                string json = await File.ReadAllTextAsync(favoritesPath);
                var favorites = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                return new HashSet<string>(favorites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载收藏文件失败: {Path}", favoritesPath);
                return new HashSet<string>();
            }
        }

        /// <summary>
        /// 保存收藏列表到文件
        /// </summary>
        private async Task SaveFavoritesToFileAsync(string userId, HashSet<string> favorites)
        {
            string favoritesPath = GetFavoritesPath(userId);
            string? directory = Path.GetDirectoryName(favoritesPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(favorites.ToList(), new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(favoritesPath, json);
        }

        /// <summary>
        /// 获取收藏文件路径
        /// 从 LearningForm.GetUserFavoritesPath() 迁移
        /// </summary>
        private string GetFavoritesPath(string userId)
        {
            var userDir = Path.Combine(_appPaths.UsersDir, userId);
            if (!Directory.Exists(userDir))
                Directory.CreateDirectory(userDir);
            return Path.Combine(userDir, "favorites.json");
        }

        /// <summary>
        /// 生成收藏项的唯一键
        /// 从 LearningForm.GetItemKey() 迁移
        /// 格式: [子类别]内容
        /// </summary>
        private string GenerateFavoriteKey(SubCategoryType subCategory, string content)
        {
            string subCategoryStr = subCategory.ToString();
            return $"[{subCategoryStr}]{content}";
        }

        #endregion

        #region === 内部类 ===

        /// <summary>
        /// 缓存的收藏数据
        /// </summary>
        private class CachedFavorites
        {
            public HashSet<string> Favorites { get; }
            public DateTime Timestamp { get; }

            public CachedFavorites(HashSet<string> favorites, DateTime timestamp)
            {
                Favorites = favorites;
                Timestamp = timestamp;
            }
        }

        #endregion
    }
}