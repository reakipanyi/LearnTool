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
    }

    public class WebBookmarkData
    {
        public List<WebBookmarkCategory> Categories { get; set; } = new();
    }

    public class WebBookmarkService : IWebBookmarkService
    {
        private readonly ILogger<WebBookmarkService>? _logger;
        private readonly ConcurrentBag<WebBookmarkCategory> _categories = new();
        private readonly string _filePath;

        public WebBookmarkService(ILogger<WebBookmarkService>? logger = null)
        {
            _logger = logger;
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LearningAssistant", "Data");
            Directory.CreateDirectory(appDataPath);
            _filePath = Path.Combine(appDataPath, "WebBookmarks.json");

            // 同时尝试从程序目录加载
            var programDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "WebBookmarks.json");
            if (File.Exists(programDataPath))
            {
                _filePath = programDataPath;
            }

            LoadFromFile();
        }

        public List<WebBookmarkCategory> GetAllCategories()
        {
            return _categories.ToList();
        }

        public List<WebBookmarkItem> GetAllBookmarks()
        {
            var result = new List<WebBookmarkItem>();
            foreach (var category in _categories)
            {
                result.AddRange(category.Bookmarks);
            }
            return result;
        }

        public WebBookmarkItem? GetBookmarkByUrl(string url)
        {
            foreach (var category in _categories)
            {
                var bookmark = category.Bookmarks.FirstOrDefault(b => b.Url.Equals(url, StringComparison.OrdinalIgnoreCase));
                if (bookmark != null)
                    return bookmark;
            }
            return null;
        }

        public void AddBookmark(string categoryName, WebBookmarkItem bookmark)
        {
            var category = _categories.FirstOrDefault(c => c.Name == categoryName);
            if (category == null)
            {
                category = new WebBookmarkCategory { Name = categoryName };
                _categories.Add(category);
            }

            // 检查是否已存在相同URL的书签
            if (category.Bookmarks.Any(b => b.Url.Equals(bookmark.Url, StringComparison.OrdinalIgnoreCase)))
            {
                _logger?.LogWarning("书签已存在: {Url}", bookmark.Url);
                return;
            }

            category.Bookmarks.Add(bookmark);
            SaveToFile();
            _logger?.LogInformation("添加书签成功: {Category} - {Title}", categoryName, bookmark.Title);
        }

        public void RemoveBookmark(string url)
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

        public void UpdateBookmark(string url, WebBookmarkItem updatedBookmark)
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

        public void SaveToFile()
        {
            try
            {
                var data = new WebBookmarkData { Categories = _categories.ToList() };
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(_filePath, json);
                _logger?.LogDebug("书签已保存到: {Path}", _filePath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存书签文件失败: {Path}", _filePath);
            }
        }

        public void LoadFromFile()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    var data = JsonConvert.DeserializeObject<WebBookmarkData>(json);
                    if (data?.Categories != null)
                    {
                        _categories.Clear();
                        foreach (var category in data.Categories)
                        {
                            _categories.Add(category);
                        }
                        _logger?.LogInformation("从文件加载书签: {Path}, 分类数: {Count}", _filePath, data.Categories.Count);
                    }
                }
                else
                {
                    _logger?.LogInformation("书签文件不存在，加载默认数据");
                    LoadDefaultBookmarks();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载书签文件失败: {Path}", _filePath);
                LoadDefaultBookmarks();
            }
        }

        private void LoadDefaultBookmarks()
        {
            _categories.Clear();
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
