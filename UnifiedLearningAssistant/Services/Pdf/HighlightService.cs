using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnifiedLearningAssistant.Common;
using UnifiedLearningAssistant.Models.Pdf;

namespace UnifiedLearningAssistant.Services.Pdf
{
    public class HighlightService : IHighlightService
    {
        private readonly Dictionary<string, PdfHighlightCollection> _highlightsCache = new();

        public List<PdfHighlight> GetHighlights(string pdfPath)
        {
            var key = GetCacheKey(pdfPath);
            if (_highlightsCache.TryGetValue(key, out var cached))
            {
                return cached.Highlights;
            }

            var collection = LoadHighlightsFromFile(pdfPath);
            _highlightsCache[key] = collection;
            return collection.Highlights;
        }

        public List<PdfHighlight> GetAllHighlights(string pdfPath)
        {
            return GetHighlights(pdfPath);
        }

        public List<PdfHighlight> GetHighlightsForPage(string pdfPath, int pageIndex)
        {
            return GetHighlights(pdfPath).Where(h => h.PageIndex == pageIndex).ToList();
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
        }

        public void UpdateHighlightNote(string pdfPath, string highlightId, string note)
        {
            var collection = GetOrCreateCollection(pdfPath);
            var highlight = collection.Highlights.FirstOrDefault(h => h.Id == highlightId);
            if (highlight != null)
            {
                highlight.Note = note;
                SaveHighlightsToFile(pdfPath, collection);
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
            }
        }

        public void RemoveHighlightsForPage(string pdfPath, int pageIndex)
        {
            var collection = GetOrCreateCollection(pdfPath);
            collection.Highlights.RemoveAll(h => h.PageIndex == pageIndex);
            SaveHighlightsToFile(pdfPath, collection);
        }

        public void ClearCache()
        {
            _highlightsCache.Clear();
        }

        public void ClearCacheForPdf(string pdfPath)
        {
            var key = GetCacheKey(pdfPath);
            if (_highlightsCache.ContainsKey(key))
            {
                _highlightsCache.Remove(key);
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
                    return collection;
                }
            }
            catch
            {
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
            }
            catch
            {
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
