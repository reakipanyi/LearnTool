using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using LearningAssistant.Common;
using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    public class BookmarkService : IBookmarkService
    {
        private readonly Dictionary<string, List<PdfBookmark>> _bookmarksCache = new();
        private readonly ILogger<BookmarkService>? _logger;

        public BookmarkService(ILogger<BookmarkService>? logger = null)
        {
            _logger = logger;
        }

        public List<PdfBookmark> GetBookmarks(string pdfPath)
        {
            if (_bookmarksCache.TryGetValue(pdfPath, out var cached))
            {
                _logger?.LogDebug("从缓存获取书签: {Path}, 数量: {Count}", pdfPath, cached.Count);
                return cached.ToList();
            }

            var bookmarks = LoadBookmarksFromFile(pdfPath);
            _bookmarksCache[pdfPath] = bookmarks;
            _logger?.LogDebug("加载书签: {Path}, 数量: {Count}", pdfPath, bookmarks.Count);
            return bookmarks.ToList();
        }

        public void AddBookmark(string pdfPath, int pageIndex, string title)
        {
            var bookmarks = _bookmarksCache.TryGetValue(pdfPath, out var cached) ? cached : LoadBookmarksFromFile(pdfPath);

            string bookmarkTitle;
            if (string.IsNullOrWhiteSpace(title))
            {
                // 生成不重复的默认标题
                string baseTitle = $"第 {pageIndex + 1} 页";
                int count = bookmarks.Count(b => b.PageIndex == pageIndex && b.Title.StartsWith(baseTitle));
                bookmarkTitle = count > 0 ? $"{baseTitle} ({count + 1})" : baseTitle;
            }
            else
            {
                bookmarkTitle = title;
            }

            var bookmark = new PdfBookmark
            {
                PdfPath = pdfPath,
                PageIndex = pageIndex,
                Title = bookmarkTitle,
                CreatedAt = DateTime.Now
            };

            bookmarks.Add(bookmark);
            bookmarks.Sort((a, b) => a.PageIndex.CompareTo(b.PageIndex));
            SaveBookmarksToFile(pdfPath, bookmarks);
            _bookmarksCache[pdfPath] = bookmarks;
            
            _logger?.LogInformation("添加书签成功: {Path} - {Title}, 页码: {Page}", pdfPath, bookmark.Title, pageIndex);
        }

        public void RemoveBookmark(string pdfPath, int pageIndex, string title)
        {
            if (!_bookmarksCache.TryGetValue(pdfPath, out var bookmarks))
            {
                bookmarks = LoadBookmarksFromFile(pdfPath);
                _bookmarksCache[pdfPath] = bookmarks;
            }
            
            var bookmark = bookmarks.FirstOrDefault(b => b.PageIndex == pageIndex && b.Title == title);
            if (bookmark != null)
            {
                bookmarks.Remove(bookmark);
                SaveBookmarksToFile(pdfPath, bookmarks);
                _logger?.LogInformation("删除书签成功: {Path} - {Title}", pdfPath, title);
            }
            else
            {
                _logger?.LogWarning("未找到要删除的书签: {Path} - {Title}", pdfPath, title);
            }
        }

        public void RemoveBookmarkByIndex(string pdfPath, int pageIndex)
        {
            if (!_bookmarksCache.TryGetValue(pdfPath, out var bookmarks))
            {
                bookmarks = LoadBookmarksFromFile(pdfPath);
                _bookmarksCache[pdfPath] = bookmarks;
            }
            
            var bookmark = bookmarks.FirstOrDefault(b => b.PageIndex == pageIndex);
            if (bookmark != null)
            {
                bookmarks.Remove(bookmark);
                SaveBookmarksToFile(pdfPath, bookmarks);
                _logger?.LogInformation("删除书签成功: {Path}, 页码: {Page}", pdfPath, pageIndex);
            }
            else
            {
                _logger?.LogWarning("未找到要删除的书签: {Path}, 页码: {Page}", pdfPath, pageIndex);
            }
        }

        public bool HasBookmark(string pdfPath, int pageIndex)
        {
            var bookmarks = GetBookmarks(pdfPath);
            return bookmarks.Any(b => b.PageIndex == pageIndex);
        }

        public void ClearCache()
        {
            _bookmarksCache.Clear();
        }

        private List<PdfBookmark> LoadBookmarksFromFile(string pdfPath)
        {
            try
            {
                var path = GetBookmarkPath(pdfPath);
                if (File.Exists(path))
                {
                    var bookmarks = JsonHelper.LoadFromFile<List<PdfBookmark>>(path);
                    _logger?.LogDebug("从文件加载书签: {Path}", path);
                    return bookmarks ?? new List<PdfBookmark>();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载书签文件失败: {Path}", pdfPath);
            }
            return new List<PdfBookmark>();
        }

        private void SaveBookmarksToFile(string pdfPath, List<PdfBookmark> bookmarks)
        {
            try
            {
                var path = GetBookmarkPath(pdfPath);
                JsonHelper.SaveToFile(path, bookmarks);
                _logger?.LogDebug("保存书签到文件: {Path}, 数量: {Count}", path, bookmarks.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存书签文件失败: {Path}", pdfPath);
            }
        }

        private string GetBookmarkPath(string pdfPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(pdfPath);
            return Path.Combine(FileHelper.GetBookmarksDirectory(), $"{fileName}_bookmarks.json");
        }
    }
}
