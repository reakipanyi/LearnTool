using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using LearningAssistant.Common;
using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    public class HighlightService : IHighlightService
    {
        private readonly Dictionary<string, PdfHighlightCollection> _highlightsCache = new();
        private readonly ILogger<HighlightService>? _logger;

        public HighlightService(ILogger<HighlightService>? logger = null)
        {
            _logger = logger;
        }

        public List<PdfHighlight> GetHighlights(string pdfPath)
        {
            var key = GetCacheKey(pdfPath);
            if (_highlightsCache.TryGetValue(key, out var cached))
            {
                _logger?.LogDebug("从缓存获取高亮: {Path}, 数量: {Count}", pdfPath, cached.Highlights.Count);
                return cached.Highlights.ToList();
            }

            var collection = LoadHighlightsFromFile(pdfPath);
            _highlightsCache[key] = collection;
            _logger?.LogDebug("加载高亮: {Path}, 数量: {Count}", pdfPath, collection.Highlights.Count);
            return collection.Highlights.ToList();
        }

        public List<PdfHighlight> GetAllHighlights(string pdfPath)
        {
            _logger?.LogDebug("获取所有高亮: {Path}", pdfPath);
            return GetHighlights(pdfPath);
        }

        public List<PdfHighlight> GetHighlightsForPage(string pdfPath, int pageIndex)
        {
            var highlights = GetHighlights(pdfPath).Where(h => h.PageIndex == pageIndex).ToList();
            _logger?.LogDebug("获取页面高亮: {Path}, 页码: {Page}, 数量: {Count}", pdfPath, pageIndex, highlights.Count);
            return highlights;
        }

        public void AddHighlight(string pdfPath, int pageIndex, float normalizedX, float normalizedY, float normalizedWidth, float normalizedHeight, string text = "", HighlightColor color = HighlightColor.Yellow)
        {
            var collection = GetOrCreateCollection(pdfPath);
            var highlight = new PdfHighlight
            {
                PdfPath = pdfPath,
                PdfHash = ComputeFileHash(pdfPath),
                PageIndex = pageIndex,
                NormalizedX = normalizedX,
                NormalizedY = normalizedY,
                NormalizedWidth = normalizedWidth,
                NormalizedHeight = normalizedHeight,
                Text = text,
                Color = color,
                CreatedAt = DateTime.Now
            };

            collection.Highlights.Add(highlight);
            SaveHighlightsToFile(pdfPath, collection);
            _logger?.LogInformation("添加高亮成功: {Path}, 页码: {Page}, 颜色: {Color}", pdfPath, pageIndex, color);
        }

        public void AddHighlightWithNote(string pdfPath, int pageIndex, float normalizedX, float normalizedY, float normalizedWidth, float normalizedHeight, string text, string note, HighlightColor color = HighlightColor.Yellow)
        {
            var collection = GetOrCreateCollection(pdfPath);
            var highlight = new PdfHighlight
            {
                PdfPath = pdfPath,
                PdfHash = ComputeFileHash(pdfPath),
                PageIndex = pageIndex,
                NormalizedX = normalizedX,
                NormalizedY = normalizedY,
                NormalizedWidth = normalizedWidth,
                NormalizedHeight = normalizedHeight,
                Text = text,
                Note = note,
                Color = color,
                CreatedAt = DateTime.Now
            };

            collection.Highlights.Add(highlight);
            SaveHighlightsToFile(pdfPath, collection);
            _logger?.LogInformation("添加带注释的高亮成功: {Path}, 页码: {Page}, 注释长度: {Length}", pdfPath, pageIndex, note?.Length ?? 0);
        }

        public void UpdateHighlightNote(string pdfPath, string highlightId, string note)
        {
            var collection = GetOrCreateCollection(pdfPath);
            var highlight = collection.Highlights.FirstOrDefault(h => h.Id == highlightId);
            if (highlight != null)
            {
                highlight.Note = note;
                SaveHighlightsToFile(pdfPath, collection);
                _logger?.LogInformation("更新高亮注释: {HighlightId}", highlightId);
            }
            else
            {
                _logger?.LogWarning("未找到要高亮注释: {HighlightId}", highlightId);
            }
        }

        public void RemoveHighlight(string pdfPath, string highlightId)
        {
            var collection = GetOrCreateCollection(pdfPath);
            var highlight = collection.Highlights.FirstOrDefault(h => h.Id == highlightId);
            if (highlight != null)
            {
                collection.Highlights.Remove(highlight);
                SaveHighlightsToFile(pdfPath, collection);
                _logger?.LogInformation("删除高亮: {HighlightId}", highlightId);
            }
            else
            {
                _logger?.LogWarning("未找到要删除的高亮: {HighlightId}", highlightId);
            }
        }

        public void RemoveHighlightsForPage(string pdfPath, int pageIndex)
        {
            var collection = GetOrCreateCollection(pdfPath);
            var count = collection.Highlights.Count(h => h.PageIndex == pageIndex);
            collection.Highlights.RemoveAll(h => h.PageIndex == pageIndex);
            SaveHighlightsToFile(pdfPath, collection);
            _logger?.LogInformation("删除页面高亮: {Path}, 页码: {Page}, 数量: {Count}", pdfPath, pageIndex, count);
        }

        public void ClearCache()
        {
            _highlightsCache.Clear();
            _logger?.LogDebug("清除所有高亮缓存");
        }

        public void ClearCacheForPdf(string pdfPath)
        {
            var key = GetCacheKey(pdfPath);
            if (_highlightsCache.ContainsKey(key))
            {
                _highlightsCache.Remove(key);
                _logger?.LogDebug("清除PDF高亮缓存: {Path}", pdfPath);
            }
        }

        private PdfHighlightCollection GetOrCreateCollection(string pdfPath)
        {
            var key = GetCacheKey(pdfPath);
            if (!_highlightsCache.TryGetValue(key, out var collection))
            {
                collection = LoadHighlightsFromFile(pdfPath);
                _highlightsCache[key] = collection;
            }
            return collection;
        }

        private PdfHighlightCollection LoadHighlightsFromFile(string pdfPath)
        {
            try
            {
                var path = GetHighlightPath(pdfPath);
                if (File.Exists(path))
                {
                    var collection = JsonHelper.LoadFromFile<PdfHighlightCollection>(path) ?? new PdfHighlightCollection { PdfPath = pdfPath };
                    MigrateOldHighlights(collection, pdfPath);
                    _logger?.LogDebug("从文件加载高亮: {Path}, 数量: {Count}", path, collection.Highlights.Count);
                    return collection;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载高亮文件失败: {Path}", pdfPath);
            }
            return new PdfHighlightCollection { PdfPath = pdfPath };
        }

        private void MigrateOldHighlights(PdfHighlightCollection collection, string pdfPath)
        {
            var hash = ComputeFileHash(pdfPath);
            foreach (var highlight in collection.Highlights)
            {
                if (string.IsNullOrEmpty(highlight.PdfHash))
                {
                    highlight.PdfHash = hash;
                }
                if (highlight.NormalizedWidth <= 0 && highlight.Width > 0)
                {
                    highlight.NormalizedX = highlight.X;
                    highlight.NormalizedY = highlight.Y;
                    highlight.NormalizedWidth = highlight.Width;
                    highlight.NormalizedHeight = highlight.Height;
                }
            }
        }

        private void SaveHighlightsToFile(string pdfPath, PdfHighlightCollection collection)
        {
            try
            {
                collection.UpdatedAt = DateTime.Now;
                var path = GetHighlightPath(pdfPath);
                JsonHelper.SaveToFile(path, collection);
                _logger?.LogDebug("保存高亮到文件: {Path}, 数量: {Count}", path, collection.Highlights.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存高亮文件失败: {Path}", pdfPath);
            }
        }

        private string GetCacheKey(string pdfPath)
        {
            return ComputeFileHash(pdfPath) ?? pdfPath;
        }

        private string GetHighlightPath(string pdfPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(pdfPath);
            var hash = ComputeFileHash(pdfPath);
            if (!string.IsNullOrEmpty(hash))
            {
                fileName = $"{fileName}_{hash.Substring(0, Math.Min(8, hash.Length))}";
            }
            return Path.Combine(FileHelper.GetHighlightsDirectory(), $"{fileName}_highlights.json");
        }

        private string? ComputeFileHash(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;
                
                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                var hashBytes = sha256.ComputeHash(stream);
                var sb = new StringBuilder();
                foreach (var b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
            catch
            {
                return null;
            }
        }

        public static Color GetHighlightColor(HighlightColor highlightColor)
        {
            return highlightColor switch
            {
                HighlightColor.Yellow => Color.FromArgb(120, 255, 255, 0),
                HighlightColor.Green => Color.FromArgb(120, 0, 255, 0),
                HighlightColor.Blue => Color.FromArgb(120, 0, 191, 255),
                HighlightColor.Pink => Color.FromArgb(120, 255, 192, 203),
                HighlightColor.Orange => Color.FromArgb(120, 255, 165, 0),
                _ => Color.FromArgb(120, 255, 255, 0)
            };
        }

        public static (float x, float y, float width, float height) NormalizeToScreen(RectangleF normalizedRect, float screenWidth, float screenHeight)
        {
            return (
                normalizedRect.X * screenWidth,
                normalizedRect.Y * screenHeight,
                normalizedRect.Width * screenWidth,
                normalizedRect.Height * screenHeight
            );
        }

        public static (float x, float y, float width, float height) NormalizeFromScreen(RectangleF screenRect, float screenWidth, float screenHeight)
        {
            return (
                screenRect.X / screenWidth,
                screenRect.Y / screenHeight,
                screenRect.Width / screenWidth,
                screenRect.Height / screenHeight
            );
        }
    }
}
