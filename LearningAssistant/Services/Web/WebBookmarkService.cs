using LearningAssistant.Common;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace LearningAssistant.Services.Web
{
    /// <summary>
    /// Web书签服务接口
    /// </summary>
    public interface IWebBookmarkService
    {
        /// <summary>
        /// 获取所有书签分类
        /// </summary>
        List<WebBookmarkCategory> GetAllCategories();

        /// <summary>
        /// 获取所有书签（扁平列表）
        /// </summary>
        List<WebBookmarkItem> GetAllBookmarks();

        /// <summary>
        /// 根据URL获取书签
        /// </summary>
        WebBookmarkItem? GetBookmarkByUrl(string url);

        /// <summary>
        /// 添加书签
        /// </summary>
        void AddBookmark(string categoryName, WebBookmarkItem bookmark);

        /// <summary>
        /// 删除书签
        /// </summary>
        void RemoveBookmark(string url);

        /// <summary>
        /// 更新书签
        /// </summary>
        void UpdateBookmark(string url, WebBookmarkItem updatedBookmark);

        /// <summary>
        /// 保存到文件
        /// </summary>
        void SaveToFile();

        /// <summary>
        /// 从文件加载
        /// </summary>
        void LoadFromFile();

        /// <summary>
        /// 当前用户ID
        /// </summary>
        string CurrentUserId { get; }

        /// <summary>
        /// 切换用户并重新加载书签
        /// </summary>
        void SwitchUser(string userId);

        /// <summary>
        /// 记录书签访问（增加访问次数并更新最后访问时间）
        /// </summary>
        void IncrementVisit(string url);
    }

    public class WebBookmarkCategory
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "📁";
        public List<WebBookmarkItem> Bookmarks { get; set; } = new();
    }

    public class WebBookmarkItem
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icon { get; set; } = "🔗";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int VisitCount { get; set; }
        public DateTime LastVisited { get; set; }
    }

    public class WebBookmarkData
    {
        public string UserId { get; set; } = string.Empty;
        public List<WebBookmarkCategory> Categories { get; set; } = new();
    }


    public class WebBookmarkService : IWebBookmarkService
    {
        private readonly ILogger<WebBookmarkService>? _logger;
        private readonly IUserSessionService? _userSessionService;
        private ConcurrentBag<WebBookmarkCategory> _categories = new();
        private string _currentUserId = string.Empty;
        private readonly object _lock = new();

        public string CurrentUserId => _currentUserId;

        public WebBookmarkService(ILogger<WebBookmarkService>? logger = null, IUserSessionService? userSessionService = null)
        {
            _logger = logger;
            _userSessionService = userSessionService;

            _currentUserId = _userSessionService?.CurrentUserId ?? Constants.DefaultUserId;
            AppPaths.SetCurrentUserId(_currentUserId);
            LoadFromFile();
        }

        public void SwitchUser(string userId)
        {
            lock (_lock)
            {
                _currentUserId = string.IsNullOrWhiteSpace(userId) ? Constants.DefaultUserId : userId;
                AppPaths.SetCurrentUserId(_currentUserId);
                _categories = new ConcurrentBag<WebBookmarkCategory>();
                LoadFromFile();
                _logger?.LogInformation("书签服务用户已切换: {UserId}", _currentUserId);
            }
        }

        public List<WebBookmarkCategory> GetAllCategories()
        {
            lock (_lock)
            {
                return _categories.ToList();
            }
        }

        public List<WebBookmarkItem> GetAllBookmarks()
        {
            lock (_lock)
            {
                var result = new List<WebBookmarkItem>();
                foreach (var category in _categories)
                {
                    result.AddRange(category.Bookmarks);
                }
                return result;
            }
        }

        public WebBookmarkItem? GetBookmarkByUrl(string url)
        {
            lock (_lock)
            {
                foreach (var category in _categories)
                {
                    var bookmark = category.Bookmarks.FirstOrDefault(b => b.Url.Equals(url, StringComparison.OrdinalIgnoreCase));
                    if (bookmark != null)
                        return bookmark;
                }
                return null;
            }
        }

        public void AddBookmark(string categoryName, WebBookmarkItem bookmark)
        {
            lock (_lock)
            {
                var category = _categories.FirstOrDefault(c => c.Name == categoryName);
                if (category == null)
                {
                    category = new WebBookmarkCategory { Name = categoryName };
                    _categories.Add(category);
                }

                if (category.Bookmarks.Any(b => b.Url.Equals(bookmark.Url, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger?.LogWarning("书签已存在: {Url}", bookmark.Url);
                    return;
                }

                bookmark.CreatedAt = DateTime.Now;
                category.Bookmarks.Add(bookmark);
                SaveToFile();
                _logger?.LogInformation("添加书签成功: {Category} - {Title}", categoryName, bookmark.Title);
            }
        }

        public void RemoveBookmark(string url)
        {
            lock (_lock)
            {
                foreach (var category in _categories)
                {
                    var bookmark = category.Bookmarks.FirstOrDefault(b => b.Url.Equals(url, StringComparison.OrdinalIgnoreCase));
                    if (bookmark != null)
                    {
                        category.Bookmarks.Remove(bookmark);
                        SaveToFile();
                        _logger?.LogInformation("删除书签成功: {Url}", url);
                        return;
                    }
                }
                _logger?.LogWarning("未找到要删除的书签: {Url}", url);
            }
        }

        public void UpdateBookmark(string url, WebBookmarkItem updatedBookmark)
        {
            lock (_lock)
            {
                foreach (var category in _categories)
                {
                    var bookmark = category.Bookmarks.FirstOrDefault(b => b.Url.Equals(url, StringComparison.OrdinalIgnoreCase));
                    if (bookmark != null)
                    {
                        bookmark.Title = updatedBookmark.Title;
                        bookmark.Url = updatedBookmark.Url;
                        bookmark.Icon = updatedBookmark.Icon;
                        SaveToFile();
                        _logger?.LogInformation("更新书签成功: {Url}", url);
                        return;
                    }
                }
                _logger?.LogWarning("未找到要更新的书签: {Url}", url);
            }
        }

        public void IncrementVisit(string url)
        {
            lock (_lock)
            {
                var bookmark = GetBookmarkByUrl(url);
                if (bookmark != null)
                {
                    bookmark.VisitCount++;
                    bookmark.LastVisited = DateTime.Now;
                    SaveToFile();
                }
            }
        }

        public void SaveToFile()
        {
            try
            {
                var filePath = AppPaths.UserBookmarksPath;
                var data = new WebBookmarkData
                {
                    UserId = _currentUserId,
                    Categories = _categories.ToList()
                };
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);

                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(filePath, json);
                _logger?.LogDebug("书签已保存到: {Path}", filePath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存书签文件失败");
            }
        }

        public void LoadFromFile()
        {
            try
            {
                var filePath = AppPaths.UserBookmarksPath;
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var data = JsonConvert.DeserializeObject<WebBookmarkData>(json);
                    if (data?.Categories != null)
                    {
                        _categories = new ConcurrentBag<WebBookmarkCategory>();
                        foreach (var category in data.Categories)
                        {
                            _categories.Add(category);
                        }
                        _logger?.LogInformation("从文件加载书签: 分类数: {Count}, 用户: {UserId}", data.Categories.Count, data.UserId);
                        return;
                    }
                }

                MigrateFromOldLocations();
                if (_categories.Count > 0)
                {
                    return;
                }

                _logger?.LogInformation("书签文件不存在，加载默认数据，用户: {UserId}", _currentUserId);
                LoadDefaultBookmarks();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载书签文件失败");
                LoadDefaultBookmarks();
            }
        }

        private void MigrateFromOldLocations()
        {
            var oldPaths = new[]
            {
                Path.Combine(AppPaths.DataRoot, "WebBookmarks.json"),
                AppPaths.UserBookmarksPath,
                AppPaths.WebBookmarksPath,
                Path.Combine(AppPaths.DataDir, "bookmarks", "WebBookmarks.json")
            };

            foreach (var oldPath in oldPaths)
            {
                if (!File.Exists(oldPath)) continue;
                // 避免从当前用户自己的路径再迁移一次
                if (string.Equals(oldPath, AppPaths.UserBookmarksPath, StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    var json = File.ReadAllText(oldPath);
                    var data = JsonConvert.DeserializeObject<WebBookmarkData>(json);
                    if (data?.Categories != null && data.Categories.Count > 0)
                    {
                        _categories = new ConcurrentBag<WebBookmarkCategory>();
                        foreach (var category in data.Categories)
                        {
                            _categories.Add(category);
                        }
                        SaveToFile();
                        _logger?.LogInformation("书签数据已从旧位置迁移: {OldPath} -> {NewPath}", oldPath, AppPaths.UserBookmarksPath);
                        try
                        {
                            File.Delete(oldPath);
                        }
                        catch (Exception delEx)
                        {
                            _logger?.LogWarning(delEx, "删除旧书签文件失败: {OldPath}", oldPath);
                        }
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "迁移旧书签数据失败: {OldPath}", oldPath);
                }
            }
        }


        private void LoadDefaultBookmarks()
        {
            _categories = new ConcurrentBag<WebBookmarkCategory>();
            _categories.Add(new WebBookmarkCategory
            {
                Name = "英文学习",
                Icon = "📚",
                Bookmarks = new List<WebBookmarkItem>
                {
                    new() { Title = "拼读树", Url = "https://pindushu.com/phonics", Icon = "🌳" }
                }
            });
            SaveToFile();
        }
    }
}
