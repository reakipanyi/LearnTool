using LearningAssistant.Abstractions;
using LearningAssistant.Common;
using LearningAssistant.Models.Pdf;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Pdf
{
    public class FileAnnotationService : IAnnotationService
    {
        private readonly ILogger<FileAnnotationService>? _logger;

        // 标注文件内存缓存（含负缓存：value 为 null 表示该页无标注文件）：
        // 翻页时高亮列表会遍历所有页读取标注，无缓存时每页都要 File.Exists + 读盘 + 反序列化，
        // 300 页文档单次翻页产生 600 次磁盘 IO，是翻页卡顿主因。
        // 外层 key 为 pdfPath（忽略大小写），内层 key 为页码。
        // 所有写入统一经过 SaveAnnotationData / ClearAnnotation，在其中同步更新缓存，保证读写一致。
        private readonly Dictionary<string, Dictionary<int, PdfAnnotation?>> _annotationCache = new(StringComparer.OrdinalIgnoreCase);
        // 已通过整目录扫描完成全量加载的文件：其缓存未命中的页可直接判定无标注，无需再探测磁盘
        private readonly HashSet<string> _fullyLoadedPdfs = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _cacheLock = new();

        public FileAnnotationService(ILogger<FileAnnotationService>? logger = null)
        {
            _logger = logger;
        }

        public byte[]? LoadAnnotation(string pdfPath, int pageIndex, int targetWidth, int targetHeight, SizeFInfo pageOriginalSize)
        {
            try
            {
                var annotation = LoadAnnotationData(pdfPath, pageIndex);
                if (annotation == null || (annotation.Strokes.Count == 0 && annotation.Texts.Count == 0))
                    return null;

                var bitmap = new Bitmap(targetWidth, targetHeight);
                using var g = Graphics.FromImage(bitmap);
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                float scaleX = (float)targetWidth / (pageOriginalSize.Width > 0 ? pageOriginalSize.Width : targetWidth);
                float scaleY = (float)targetHeight / (pageOriginalSize.Height > 0 ? pageOriginalSize.Height : targetHeight);

                foreach (var stroke in annotation.Strokes)
                {
                    using var pen = new Pen(Color.FromArgb(stroke.ColorArgb), stroke.Thickness);
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;

                    if (stroke.Points.Length >= 4)
                    {
                        var points = new List<PointF>();
                        for (int i = 0; i < stroke.Points.Length - 1; i += 2)
                        {
                            float x = stroke.Points[i] * pageOriginalSize.Width * scaleX;
                            float y = stroke.Points[i + 1] * pageOriginalSize.Height * scaleY;
                            points.Add(new PointF(x, y));
                        }
                        if (points.Count >= 2)
                        {
                            var shapeType = stroke.ShapeType?.ToString();
                            if (shapeType == "Rectangle" && points.Count >= 2)
                            {
                                var rect = new RectangleF(
                                    Math.Min(points[0].X, points[1].X),
                                    Math.Min(points[0].Y, points[1].Y),
                                    Math.Abs(points[1].X - points[0].X),
                                    Math.Abs(points[1].Y - points[0].Y));
                                if (stroke.DashStyle == "Dash") pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                                if (stroke.StrokeStyle == "DotLine") pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                            }
                            else if (shapeType == "Ellipse" && points.Count >= 2)
                            {
                                var rect = new RectangleF(
                                    Math.Min(points[0].X, points[1].X),
                                    Math.Min(points[0].Y, points[1].Y),
                                    Math.Abs(points[1].X - points[0].X),
                                    Math.Abs(points[1].Y - points[0].Y));
                                if (stroke.DashStyle == "Dash") pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                                if (stroke.StrokeStyle == "DotLine") pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                                g.DrawEllipse(pen, rect);
                            }
                            else if (shapeType == "Arrow" && points.Count >= 2)
                            {
                                if (stroke.StrokeStyle == "ArrowLine") pen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;
                                if (stroke.StrokeStyle == "DotLine") pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                                g.DrawLine(pen, points[0], points[1]);
                            }
                            else if (shapeType == "Mosaic" && points.Count >= 2)
                            {
                                var rect = new RectangleF(
                                    Math.Min(points[0].X, points[1].X),
                                    Math.Min(points[0].Y, points[1].Y),
                                    Math.Abs(points[1].X - points[0].X),
                                    Math.Abs(points[1].Y - points[0].Y));
                                using var brush = new SolidBrush(Color.FromArgb(80, 255, 255, 255));
                                g.FillRectangle(brush, rect);
                                if (stroke.DashStyle == "Dash") pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                                if (stroke.StrokeStyle == "DotLine") pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                            }
                            else
                            {
                                // 根据画笔类型应用透明度效果
                                string penType = stroke.PenType ?? "Pen";
                                int alpha = penType switch
                                {
                                    "Pencil" => 180,
                                    "Marker" => 120,
                                    _ => 255
                                };
                                var penColor = Color.FromArgb(stroke.ColorArgb);
                                pen.Color = Color.FromArgb(alpha, penColor.R, penColor.G, penColor.B);
                                if (stroke.StrokeStyle == "DotLine") pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                                g.DrawLines(pen, points.ToArray());
                            }
                        }
                    }
                }

                foreach (var text in annotation.Texts)
                {
                    float x = text.NormalizedX * pageOriginalSize.Width * scaleX;
                    float y = text.NormalizedY * pageOriginalSize.Height * scaleY;
                    
                    using var font = new Font(text.FontFamily, text.FontSize * scaleX);
                    using var brush = new SolidBrush(Color.FromArgb(text.ColorArgb));
                    g.DrawString(text.Content, font, brush, x, y);
                }

                return BitmapToBytes(bitmap);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load annotation for {PdfPath} page {PageIndex}", pdfPath, pageIndex);
                return null;
            }
        }

        public void SaveAnnotation(string pdfPath, int pageIndex)
        {
            try
            {
                var annotation = LoadAnnotationData(pdfPath, pageIndex) ?? new PdfAnnotation();
                annotation.PdfPath = pdfPath;
                annotation.PageIndex = pageIndex;
                annotation.UpdatedAt = DateTime.Now;

                SaveAnnotationData(pdfPath, pageIndex, annotation);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save annotation for {PdfPath} page {PageIndex}", pdfPath, pageIndex);
            }
        }

        public void ClearAnnotation(string pdfPath, int pageIndex)
        {
            try
            {
                var path = GetAnnotationPath(pdfPath, pageIndex);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                lock (_cacheLock)
                {
                    // 文件已删除，缓存置为负缓存，避免下次读取再次访问磁盘
                    GetOrCreatePageCache(pdfPath)[pageIndex] = null;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to clear annotation for {PdfPath} page {PageIndex}", pdfPath, pageIndex);
            }
        }

        public void AddStroke(string pdfPath, int pageIndex, AnnotationStroke stroke)
        {
            try
            {
                var annotation = LoadAnnotationData(pdfPath, pageIndex) ?? new PdfAnnotation();
                annotation.PdfPath = pdfPath;
                annotation.PageIndex = pageIndex;
                annotation.Strokes.Add(stroke);
                annotation.UpdatedAt = DateTime.Now;

                SaveAnnotationData(pdfPath, pageIndex, annotation);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to add stroke for {PdfPath} page {PageIndex}", pdfPath, pageIndex);
            }
        }

        public void AddText(string pdfPath, int pageIndex, AnnotationText text)
        {
            try
            {
                var annotation = LoadAnnotationData(pdfPath, pageIndex) ?? new PdfAnnotation();
                annotation.PdfPath = pdfPath;
                annotation.PageIndex = pageIndex;
                annotation.Texts.Add(text);
                annotation.UpdatedAt = DateTime.Now;

                SaveAnnotationData(pdfPath, pageIndex, annotation);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to add text annotation for {PdfPath} page {PageIndex}", pdfPath, pageIndex);
            }
        }

        public IEnumerable<AnnotationText> GetTexts(string pdfPath, int pageIndex)
        {
            var annotation = LoadAnnotationData(pdfPath, pageIndex);
            // 返回副本，避免调用方直接改动缓存中的列表而绕过 SaveAnnotationData
            return annotation?.Texts.ToList() ?? Enumerable.Empty<AnnotationText>();
        }

        public void ClearRedo(string pdfPath, int pageIndex)
        {
            try
            {
                var annotation = LoadAnnotationData(pdfPath, pageIndex);
                if (annotation != null && annotation.Strokes.Count > 0)
                {
                    annotation.Strokes.RemoveAt(annotation.Strokes.Count - 1);
                    SaveAnnotationData(pdfPath, pageIndex, annotation);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to clear redo for {PdfPath} page {PageIndex}", pdfPath, pageIndex);
            }
        }

        public AnnotationStroke? RemoveLastStroke(string pdfPath, int pageIndex)
        {
            try
            {
                var annotation = LoadAnnotationData(pdfPath, pageIndex);
                if (annotation != null && annotation.Strokes.Count > 0)
                {
                    var lastStroke = annotation.Strokes[annotation.Strokes.Count - 1];
                    annotation.Strokes.RemoveAt(annotation.Strokes.Count - 1);
                    annotation.UpdatedAt = DateTime.Now;
                    SaveAnnotationData(pdfPath, pageIndex, annotation);
                    return lastStroke;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to remove last stroke for {PdfPath} page {PageIndex}", pdfPath, pageIndex);
            }
            return null;
        }

        public void RemoveStrokeAt(string pdfPath, int pageIndex, int index)
        {
            try
            {
                var annotation = LoadAnnotationData(pdfPath, pageIndex);
                if (annotation != null && index >= 0 && index < annotation.Strokes.Count)
                {
                    annotation.Strokes.RemoveAt(index);
                    annotation.UpdatedAt = DateTime.Now;
                    SaveAnnotationData(pdfPath, pageIndex, annotation);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to remove stroke at index {Index} for {PdfPath} page {PageIndex}", index, pdfPath, pageIndex);
            }
        }

        public void ClearAllStrokes(string pdfPath, int pageIndex)
        {
            try
            {
                var annotation = LoadAnnotationData(pdfPath, pageIndex);
                if (annotation != null)
                {
                    annotation.Strokes.Clear();
                    annotation.UpdatedAt = DateTime.Now;
                    SaveAnnotationData(pdfPath, pageIndex, annotation);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to clear all strokes for {PdfPath} page {PageIndex}", pdfPath, pageIndex);
            }
        }

        public void RemoveTextAt(string pdfPath, int pageIndex, int index)
        {
            try
            {
                var annotation = LoadAnnotationData(pdfPath, pageIndex);
                if (annotation != null && index >= 0 && index < annotation.Texts.Count)
                {
                    annotation.Texts.RemoveAt(index);
                    annotation.UpdatedAt = DateTime.Now;
                    SaveAnnotationData(pdfPath, pageIndex, annotation);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to remove text at index {Index} for {PdfPath} page {PageIndex}", index, pdfPath, pageIndex);
            }
        }

        public void UpdateTextAt(string pdfPath, int pageIndex, int index, AnnotationText text)
        {
            try
            {
                var annotation = LoadAnnotationData(pdfPath, pageIndex);
                if (annotation != null && index >= 0 && index < annotation.Texts.Count)
                {
                    annotation.Texts[index] = text;
                    annotation.UpdatedAt = DateTime.Now;
                    SaveAnnotationData(pdfPath, pageIndex, annotation);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to update text at index {Index} for {PdfPath} page {PageIndex}", index, pdfPath, pageIndex);
            }
        }

        public void ClearAllTexts(string pdfPath, int pageIndex)
        {
            try
            {
                var annotation = LoadAnnotationData(pdfPath, pageIndex);
                if (annotation != null)
                {
                    annotation.Texts.Clear();
                    annotation.UpdatedAt = DateTime.Now;
                    SaveAnnotationData(pdfPath, pageIndex, annotation);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to clear all texts for {PdfPath} page {PageIndex}", pdfPath, pageIndex);
            }
        }

        public IEnumerable<AnnotationStroke> GetStrokes(string pdfPath, int pageIndex)
        {
            var annotation = LoadAnnotationData(pdfPath, pageIndex);
            // 返回副本，避免调用方直接改动缓存中的列表而绕过 SaveAnnotationData
            return annotation?.Strokes.ToList() ?? Enumerable.Empty<AnnotationStroke>();
        }

        public void UpdateStrokeAt(string pdfPath, int pageIndex, int index, AnnotationStroke stroke)
        {
            try
            {
                var annotation = LoadAnnotationData(pdfPath, pageIndex);
                if (annotation != null && index >= 0 && index < annotation.Strokes.Count)
                {
                    annotation.Strokes[index] = stroke;
                    annotation.UpdatedAt = DateTime.Now;
                    SaveAnnotationData(pdfPath, pageIndex, annotation);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to update stroke at index {Index} for {PdfPath} page {PageIndex}", index, pdfPath, pageIndex);
            }
        }

        public PdfAnnotation? GetAnnotation(string pdfPath, int pageIndex)
        {
            return LoadAnnotationData(pdfPath, pageIndex);
        }

        private PdfAnnotation? LoadAnnotationData(string pdfPath, int pageIndex)
        {
            lock (_cacheLock)
            {
                if (_annotationCache.TryGetValue(pdfPath, out var pages))
                {
                    if (pages.TryGetValue(pageIndex, out var cached))
                        return cached;
                    // 该文件已整目录扫描过，缓存没有即磁盘没有，避免重复 File.Exists 探测
                    if (_fullyLoadedPdfs.Contains(pdfPath))
                        return null;
                }
            }

            try
            {
                var path = GetAnnotationPath(pdfPath, pageIndex);
                var annotation = JsonHelper.LoadFromFile<PdfAnnotation>(path);
                lock (_cacheLock)
                {
                    GetOrCreatePageCache(pdfPath)[pageIndex] = annotation;
                }
                return annotation;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load annotation data from {Path}", GetAnnotationPath(pdfPath, pageIndex));
                return null;
            }
        }

        public IEnumerable<PdfAnnotation> GetAllAnnotations(string pdfPath)
        {
            lock (_cacheLock)
            {
                if (_fullyLoadedPdfs.Contains(pdfPath))
                    return GetCachedAnnotations(pdfPath);
            }

            try
            {
                // 通过第 0 页的路径推导该文件的标注目录与文件名前缀（{fileName}_page{N}.json）
                var samplePath = GetAnnotationPath(pdfPath, 0);
                var dir = Path.GetDirectoryName(samplePath);
                const string pageMarker = "_page";
                var fileName = Path.GetFileName(samplePath);
                var markerIndex = fileName.LastIndexOf(pageMarker, StringComparison.Ordinal);
                if (dir == null || markerIndex <= 0)
                    return Enumerable.Empty<PdfAnnotation>();
                var prefix = fileName.Substring(0, markerIndex);

                lock (_cacheLock)
                {
                    GetOrCreatePageCache(pdfPath);
                }

                if (Directory.Exists(dir))
                {
                    foreach (var file in Directory.EnumerateFiles(dir, prefix + pageMarker + "*.json"))
                    {
                        var name = Path.GetFileName(file);
                        var pagePart = name.Substring(prefix.Length + pageMarker.Length,
                            name.Length - prefix.Length - pageMarker.Length - ".json".Length);
                        if (!int.TryParse(pagePart, out var pageIndex))
                            continue;

                        lock (_cacheLock)
                        {
                            // 缓存中已有（可能刚被写入），以缓存为准，不重复读盘
                            if (_annotationCache[pdfPath].ContainsKey(pageIndex))
                                continue;
                        }

                        var annotation = JsonHelper.LoadFromFile<PdfAnnotation>(file);
                        lock (_cacheLock)
                        {
                            _annotationCache[pdfPath][pageIndex] = annotation;
                        }
                    }
                }

                lock (_cacheLock)
                {
                    _fullyLoadedPdfs.Add(pdfPath);
                    return GetCachedAnnotations(pdfPath);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load all annotations for {PdfPath}", pdfPath);
                return Enumerable.Empty<PdfAnnotation>();
            }
        }

        // 调用方须已持有 _cacheLock
        private Dictionary<int, PdfAnnotation?> GetOrCreatePageCache(string pdfPath)
        {
            if (!_annotationCache.TryGetValue(pdfPath, out var pages))
            {
                pages = new Dictionary<int, PdfAnnotation?>();
                _annotationCache[pdfPath] = pages;
            }
            return pages;
        }

        // 调用方须已持有 _cacheLock
        private List<PdfAnnotation> GetCachedAnnotations(string pdfPath)
        {
            if (!_annotationCache.TryGetValue(pdfPath, out var pages))
                return new List<PdfAnnotation>();
            return pages.Values.Where(a => a != null).Cast<PdfAnnotation>()
                .OrderBy(a => a.PageIndex).ToList();
        }

        private void SaveAnnotationData(string pdfPath, int pageIndex, PdfAnnotation annotation)
        {
            try
            {
                var path = GetAnnotationPath(pdfPath, pageIndex);
                JsonHelper.SaveToFile(path, annotation);
                lock (_cacheLock)
                {
                    GetOrCreatePageCache(pdfPath)[pageIndex] = annotation;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save annotation data to {Path}", GetAnnotationPath(pdfPath, pageIndex));
            }
        }

        private string GetAnnotationPath(string pdfPath, int pageIndex)
        {
            return AppPaths.GetUserAnnotationPath(pdfPath, pageIndex);
        }

        private static byte[]? BitmapToBytes(Bitmap? bmp)
        {
            if (bmp == null) return null;
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }

        private static Bitmap? BytesToBitmap(byte[]? data)
        {
            if (data == null || data.Length == 0) return null;
            using var ms = new MemoryStream(data);
            using var source = Image.FromStream(ms);
            return new Bitmap(source);
        }
    }
}