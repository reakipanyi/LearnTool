using System.Collections.Generic;
using System.Linq;
using UnifiedLearningAssistant.Common;
using UnifiedLearningAssistant.Models.Pdf;

namespace UnifiedLearningAssistant.Services.Pdf
{
    public class BookmarkService : IBookmarkService
    {
        private readonly Dictionary<string, List<PdfBookmark>> _bookmarksCache = new();

        public List<PdfBookmark> GetBookmarks(string pdfPath)
        {
            if (_bookmarksCache.TryGetValue(pdfPath, out var cached))
            {
                return cached;
            }

            var bookmarks = LoadBookmarksFromFile(pdfPath);
            _bookmarksCache[pdfPath] = bookmarks;
            return bookmarks;
        }

        public void AddBookmark(string pdfPath, int pageIndex, string title)
        {
            var bookmarks = GetBookmarks(pdfPath);
            if (bookmarks.Any(b => b.PageIndex == pageIndex && b.Title == title))
            {
                return;
            }

            var bookmark = new PdfBookmark
            {
                PdfPath = pdfPath,
                PageIndex = pageIndex,
                Title = string.IsNullOrWhiteSpace(title) ? $"第 {pageIndex + 1} 页" : title,
                CreatedAt = DateTime.Now
            };

            bookmarks.Add(bookmark);
            bookmarks.Sort((a, b) => a.PageIndex.CompareTo(b.PageIndex));
            SaveBookmarksToFile(pdfPath, bookmarks);
        }

        public void RemoveBookmark(string pdfPath, int pageIndex, string title)
        {
            var bookmarks = GetBookmarks(pdfPath);
            var bookmark = bookmarks.FirstOrDefault(b => b.PageIndex == pageIndex && b.Title == title);
            if (bookmark != null)
            {
                bookmarks.Remove(bookmark);
                SaveBookmarksToFile(pdfPath, bookmarks);
            }
        }

        public void RemoveBookmarkByIndex(string pdfPath, int pageIndex)
        {
            var bookmarks = GetBookmarks(pdfPath);
            var bookmark = bookmarks.FirstOrDefault(b => b.PageIndex == pageIndex);
            if (bookmark != null)
            {
                bookmarks.Remove(bookmark);
                SaveBookmarksToFile(pdfPath, bookmarks);
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
                    return JsonHelper.LoadFromFile<List<PdfBookmark>>(path) ?? new List<PdfBookmark>();
                }
            }
            catch
            {
            }
            return new List<PdfBookmark>();
        }

        private void SaveBookmarksToFile(string pdfPath, List<PdfBookmark> bookmarks)
        {
            try
            {
                var path = GetBookmarkPath(pdfPath);
                JsonHelper.SaveToFile(path, bookmarks);
            }
            catch
            {
            }
        }

        private string GetBookmarkPath(string pdfPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(pdfPath);
            return Path.Combine(FileHelper.GetBookmarksDirectory(), $"{fileName}_bookmarks.json");
        }
    }
}
