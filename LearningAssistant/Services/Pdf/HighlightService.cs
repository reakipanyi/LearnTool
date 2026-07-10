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
            // 获取目录路径
            var folderPath = Path.GetDirectoryName(pdfPath) ?? "";
            var collection = GetOrCreateFolderCollection(folderPath);

            // 返回属于该文件的高亮（使用不区分大小写的文件名比较）
            var fileName = Path.GetFileName(pdfPath).ToLowerInvariant();
            var result = collection.Highlights
                .Where(h => string.Equals(Path.GetFileName(h.PdfPath), fileName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            _logger?.LogDebug("获取文件高亮: {Path}, 文件名: {FileName}, 目录高亮总数: {FolderCount}, 文件高亮数: {Count}",
                pdfPath, fileName, collection.Highlights.Count, result.Count);
            return result;
        }

        public List<PdfHighlight> GetAllHighlights(string pdfPath)
        {
            _logger?.LogDebug("获取所有高亮: {Path}", pdfPath);
            return GetHighlights(pdfPath);
        }

        public List<PdfHighlight> GetHighlightsForPage(string pdfPath, int pageIndex)
        {
            var allHighlights = GetHighlights(pdfPath);
            var highlights = allHighlights.Where(h => h.PageIndex == pageIndex).ToList();
            _logger?.LogInformation("获取页面高亮: {Path}, 页码: {Page}, 总高亮数: {AllCount}, 页面高亮数: {Count}",
                pdfPath, pageIndex, allHighlights.Count, highlights.Count);
            return highlights;
        }

        /// <summary>
        /// 按目录获取所有高亮
        /// </summary>
        public List<PdfHighlight> GetHighlightsForFolder(string folderPath)
        {
            var collection = GetOrCreateFolderCollection(folderPath);
            _logger?.LogDebug("获取目录高亮: {Path}, 数量: {Count}", folderPath, collection.Highlights.Count);
            return collection.Highlights.ToList();
        }

        public string AddHighlight(string pdfPath, int pageIndex, float normalizedX, float normalizedY, float normalizedWidth, float normalizedHeight, string text = "", HighlightColor color = HighlightColor.Yellow)
        {
            return AddHighlightInternal(pdfPath, pageIndex, normalizedX, normalizedY, normalizedWidth, normalizedHeight, text, null, color);
        }

        public void AddHighlightWithNote(string pdfPath, int pageIndex, float normalizedX, float normalizedY, float normalizedWidth, float normalizedHeight, string text, string note, HighlightColor color = HighlightColor.Yellow)
        {
            AddHighlightInternal(pdfPath, pageIndex, normalizedX, normalizedY, normalizedWidth, normalizedHeight, text, note, color);
        }

        private string AddHighlightInternal(string pdfPath, int pageIndex, float normalizedX, float normalizedY, float normalizedWidth, float normalizedHeight, string text, string? note, HighlightColor color)
        {
            var folderPath = Path.GetDirectoryName(pdfPath) ?? "";
            var collection = GetOrCreateFolderCollection(folderPath);

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
                Note = note ?? "",
                Color = color,
                CreatedAt = DateTime.Now
            };

            collection.Highlights.Add(highlight);
            SaveHighlightsToFolder(folderPath, collection);

            // 验证保存成功
            var verificationHighlights = collection.Highlights.Where(h => h.PageIndex == pageIndex).ToList();
            _logger?.LogInformation("添加高亮成功: {Path}, 页码: {Page}, 颜色: {Color}, 验证数量: {Count}, HighlightId: {HighlightId}",
                pdfPath, pageIndex, color, verificationHighlights.Count, highlight.Id);

            return highlight.Id;
        }

        public void UpdateHighlightNote(string pdfPath, string highlightId, string note)
        {
            var folderPath = Path.GetDirectoryName(pdfPath) ?? "";
            var collection = GetOrCreateFolderCollection(folderPath);
            var highlight = collection.Highlights.FirstOrDefault(h => h.Id == highlightId);
            if (highlight != null)
            {
                highlight.Note = note;
                SaveHighlightsToFolder(folderPath, collection);
                _logger?.LogInformation("更新高亮注释: {HighlightId}", highlightId);
            }
            else
            {
                _logger?.LogWarning("未找到要高亮注释: {HighlightId}", highlightId);
            }
        }

        public void RemoveHighlight(string pdfPath, string highlightId)
        {
            var folderPath = Path.GetDirectoryName(pdfPath) ?? "";
            var collection = GetOrCreateFolderCollection(folderPath);
            var highlight = collection.Highlights.FirstOrDefault(h => h.Id == highlightId);
            if (highlight != null)
            {
                collection.Highlights.Remove(highlight);
                SaveHighlightsToFolder(folderPath, collection);
                _logger?.LogInformation("删除高亮: {HighlightId}", highlightId);
            }
            else
            {
                _logger?.LogWarning("未找到要删除的高亮: {HighlightId}", highlightId);
            }
        }

        /// <summary>
        /// 批量删除指定PDF的所有高亮。
        /// 相比逐个调用 RemoveHighlight，本方法只序列化并写入磁盘一次，
        /// 避免高亮数量较多时反复写文件导致UI线程长时间阻塞。
        /// </summary>
        public List<PdfHighlight> RemoveAllHighlights(string pdfPath)
        {
            var folderPath = Path.GetDirectoryName(pdfPath) ?? "";
            var collection = GetOrCreateFolderCollection(folderPath);

            var fileName = Path.GetFileName(pdfPath);
            var removed = collection.Highlights
                .Where(h => string.Equals(Path.GetFileName(h.PdfPath), fileName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (removed.Count == 0)
            {
                _logger?.LogInformation("批量删除高亮: 无高亮可删除, {Path}", pdfPath);
                return removed;
            }

            collection.Highlights.RemoveAll(h =>
                string.Equals(Path.GetFileName(h.PdfPath), fileName, StringComparison.OrdinalIgnoreCase));

            // 一次性写回磁盘，而不是每个高亮写一次
            SaveHighlightsToFolder(folderPath, collection);

            _logger?.LogInformation("批量删除高亮: {Path}, 数量: {Count}", pdfPath, removed.Count);
            return removed;
        }

        public void RemoveHighlightsForPage(string pdfPath, int pageIndex)
        {
            var folderPath = Path.GetDirectoryName(pdfPath) ?? "";
            var collection = GetOrCreateFolderCollection(folderPath);
            var count = collection.Highlights.Count(h => h.PageIndex == pageIndex && Path.GetFileName(h.PdfPath) == Path.GetFileName(pdfPath));
            collection.Highlights.RemoveAll(h => h.PageIndex == pageIndex && Path.GetFileName(h.PdfPath) == Path.GetFileName(pdfPath));
            SaveHighlightsToFolder(folderPath, collection);
            _logger?.LogInformation("删除页面高亮: {Path}, 页码: {Page}, 数量: {Count}", pdfPath, pageIndex, count);
        }

        public void ClearCache()
        {
            _highlightsCache.Clear();
            _logger?.LogDebug("清除所有高亮缓存");
        }

        public void ClearCacheForPdf(string pdfPath)
        {
            var folderPath = Path.GetDirectoryName(pdfPath) ?? "";
            var key = GetFolderCacheKey(folderPath);
            if (_highlightsCache.ContainsKey(key))
            {
                _highlightsCache.Remove(key);
                _logger?.LogDebug("清除目录高亮缓存: {Path}", folderPath);
            }
        }

        private PdfHighlightCollection GetOrCreateFolderCollection(string folderPath)
        {
            var key = GetFolderCacheKey(folderPath);
            if (!_highlightsCache.TryGetValue(key, out var collection))
            {
                collection = LoadHighlightsFromFolder(folderPath);
                _highlightsCache[key] = collection;
                _logger?.LogInformation("创建新的高亮集合缓存: {FolderPath}, 缓存键: {Key}, 高亮数: {Count}",
                    folderPath, key, collection.Highlights.Count);
            }
            else
            {
                _logger?.LogDebug("使用现有高亮集合缓存: {FolderPath}, 缓存键: {Key}, 高亮数: {Count}",
                    folderPath, key, collection.Highlights.Count);
            }
            return collection;
        }

        private PdfHighlightCollection LoadHighlightsFromFolder(string folderPath)
        {
            try
            {
                var path = GetFolderHighlightPath(folderPath);
                if (File.Exists(path))
                {
                    var collection = JsonHelper.LoadFromFile<PdfHighlightCollection>(path) ?? new PdfHighlightCollection { FolderPath = folderPath };
                    MigrateOldHighlights(collection);
                    _logger?.LogDebug("从文件加载目录高亮: {Path}, 数量: {Count}", path, collection.Highlights.Count);
                    return collection;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载目录高亮文件失败: {Path}", folderPath);
            }
            return new PdfHighlightCollection { FolderPath = folderPath };
        }

        private void MigrateOldHighlights(PdfHighlightCollection collection)
        {
            foreach (var highlight in collection.Highlights)
            {
                if (string.IsNullOrEmpty(highlight.PdfHash) && !string.IsNullOrEmpty(highlight.PdfPath))
                {
                    highlight.PdfHash = ComputeFileHash(highlight.PdfPath) ?? "";
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

        private void SaveHighlightsToFolder(string folderPath, PdfHighlightCollection collection)
        {
            try
            {
                collection.UpdatedAt = DateTime.Now;
                var path = GetFolderHighlightPath(folderPath);
                JsonHelper.SaveToFile(path, collection);
                _logger?.LogDebug("保存目录高亮到文件: {Path}, 数量: {Count}", path, collection.Highlights.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存目录高亮文件失败: {Path}", folderPath);
            }
        }

        private string GetFolderCacheKey(string folderPath)
        {
            return ComputeFolderHash(folderPath) ?? folderPath;
        }

        private string GetFolderHighlightPath(string folderPath)
        {
            var folderName = Path.GetFileName(folderPath);
            if (string.IsNullOrEmpty(folderName))
            {
                folderName = "root";
            }
            var hash = ComputeFolderHash(folderPath);
            if (!string.IsNullOrEmpty(hash))
            {
                folderName = $"{folderName}_{hash.Substring(0, Math.Min(8, hash.Length))}";
            }
            return Path.Combine(AppPaths.GetUserHighlightsDir(), $"{folderName}_highlights.json");
        }

        private string? ComputeFolderHash(string folderPath)
        {
            try
            {
                if (!Directory.Exists(folderPath)) return null;

                using var sha256 = SHA256.Create();
                var bytes = Encoding.UTF8.GetBytes(folderPath);
                var hashBytes = sha256.ComputeHash(bytes);
                var sb = new StringBuilder();
                foreach (var b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "计算文件夹哈希失败: {FolderPath}", folderPath);
                return null;
            }
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
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "计算文件哈希失败: {FilePath}", filePath);
                return null;
            }
        }

        public static Color GetHighlightColor(HighlightColor highlightColor)
        {
            return highlightColor switch
            {
                HighlightColor.Yellow => Color.FromArgb(70, 255, 255, 0),
                HighlightColor.Green => Color.FromArgb(70, 0, 255, 0),
                HighlightColor.Blue => Color.FromArgb(70, 0, 191, 255),
                HighlightColor.Pink => Color.FromArgb(70, 255, 192, 203),
                HighlightColor.Orange => Color.FromArgb(70, 255, 165, 0),
                _ => Color.FromArgb(70, 255, 255, 0)
            };
        }

        /// <summary>
        /// 将归一化坐标转换为屏幕坐标
        /// </summary>
        /// <param name="normalizedRect">归一化矩形（0-1范围）</param>
        /// <param name="screenWidth">屏幕宽度</param>
        /// <param name="screenHeight">屏幕高度</param>
        /// <returns>屏幕坐标矩形</returns>
        public static (float x, float y, float width, float height) NormalizeToScreen(RectangleF normalizedRect, float screenWidth, float screenHeight)
        {
            return (
                normalizedRect.X * screenWidth,
                normalizedRect.Y * screenHeight,
                normalizedRect.Width * screenWidth,
                normalizedRect.Height * screenHeight
            );
        }

        /// <summary>
        /// 将屏幕坐标转换为归一化坐标
        /// </summary>
        /// <param name="screenRect">屏幕坐标矩形</param>
        /// <param name="screenWidth">屏幕宽度</param>
        /// <param name="screenHeight">屏幕高度</param>
        /// <returns>归一化矩形（0-1范围）</returns>
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
