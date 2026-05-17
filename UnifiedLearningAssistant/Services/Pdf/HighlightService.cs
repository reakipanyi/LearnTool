using UnifiedLearningAssistant.Common;
using UnifiedLearningAssistant.Models.Pdf;

namespace UnifiedLearningAssistant.Services.Pdf
{
    public class HighlightService
    {
        private readonly Dictionary<string, PdfHighlightCollection> _highlightsCache = new();

        public List<PdfHighlight> GetHighlights(string pdfPath)
        {
            if (_highlightsCache.TryGetValue(pdfPath, out var cached))
            {
                return cached.Highlights;
            }

            var collection = LoadHighlightsFromFile(pdfPath);
            _highlightsCache[pdfPath] = collection;
            return collection.Highlights;
        }

        public List<PdfHighlight> GetHighlightsForPage(string pdfPath, int pageIndex)
        {
            return GetHighlights(pdfPath).Where(h => h.PageIndex == pageIndex).ToList();
        }

        public void AddHighlight(string pdfPath, int pageIndex, float x, float y, float width, float height, string text = "", HighlightColor color = HighlightColor.Yellow)
        {
            var collection = GetOrCreateCollection(pdfPath);
            var highlight = new PdfHighlight
            {
                PdfPath = pdfPath,
                PageIndex = pageIndex,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Text = text,
                Color = color,
                CreatedAt = DateTime.Now
            };

            collection.Highlights.Add(highlight);
            SaveHighlightsToFile(pdfPath, collection);
        }

        public void AddHighlightWithNote(string pdfPath, int pageIndex, float x, float y, float width, float height, string text, string note, HighlightColor color = HighlightColor.Yellow)
        {
            var collection = GetOrCreateCollection(pdfPath);
            var highlight = new PdfHighlight
            {
                PdfPath = pdfPath,
                PageIndex = pageIndex,
                X = x,
                Y = y,
                Width = width,
                Height = height,
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
            if (_highlightsCache.ContainsKey(pdfPath))
            {
                _highlightsCache.Remove(pdfPath);
            }
        }

        private PdfHighlightCollection GetOrCreateCollection(string pdfPath)
        {
            if (!_highlightsCache.TryGetValue(pdfPath, out var collection))
            {
                collection = LoadHighlightsFromFile(pdfPath);
                _highlightsCache[pdfPath] = collection;
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
                    return JsonHelper.LoadFromFile<PdfHighlightCollection>(path) ?? new PdfHighlightCollection { PdfPath = pdfPath };
                }
            }
            catch
            {
            }
            return new PdfHighlightCollection { PdfPath = pdfPath };
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

        private string GetHighlightPath(string pdfPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(pdfPath);
            return Path.Combine(FileHelper.GetHighlightsDirectory(), $"{fileName}_highlights.json");
        }

        public static Color GetHighlightColor(HighlightColor highlightColor)
        {
            return highlightColor switch
            {
                HighlightColor.Yellow => Color.FromArgb(180, 255, 255, 0),
                HighlightColor.Green => Color.FromArgb(180, 0, 255, 0),
                HighlightColor.Blue => Color.FromArgb(180, 0, 191, 255),
                HighlightColor.Pink => Color.FromArgb(180, 255, 192, 203),
                HighlightColor.Orange => Color.FromArgb(180, 255, 165, 0),
                _ => Color.FromArgb(180, 255, 255, 0)
            };
        }
    }
}
