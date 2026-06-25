using LearningAssistant.Common;
using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    public class FileAnnotationService : IAnnotationService
    {
        public Bitmap? LoadAnnotation(string pdfPath, int pageIndex, int targetWidth, int targetHeight, SizeF pageOriginalSize)
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
                            g.DrawLines(pen, points.ToArray());
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

                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public void SaveAnnotation(string pdfPath, int pageIndex, Bitmap overlayBitmap)
        {
            try
            {
                var annotation = LoadAnnotationData(pdfPath, pageIndex) ?? new PdfAnnotation();
                annotation.PdfPath = pdfPath;
                annotation.PageIndex = pageIndex;
                annotation.UpdatedAt = DateTime.Now;

                SaveAnnotationData(pdfPath, pageIndex, annotation);
            }
            catch
            {
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
            }
            catch
            {
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
            catch
            {
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
            catch
            {
            }
        }

        public IEnumerable<AnnotationText> GetTexts(string pdfPath, int pageIndex)
        {
            var annotation = LoadAnnotationData(pdfPath, pageIndex);
            return annotation?.Texts ?? Enumerable.Empty<AnnotationText>();
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
            catch
            {
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
            catch
            {
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
            catch
            {
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
            catch
            {
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
            catch
            {
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
            catch
            {
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
            catch
            {
            }
        }

        public IEnumerable<AnnotationStroke> GetStrokes(string pdfPath, int pageIndex)
        {
            var annotation = LoadAnnotationData(pdfPath, pageIndex);
            return annotation?.Strokes ?? Enumerable.Empty<AnnotationStroke>();
        }

        private PdfAnnotation? LoadAnnotationData(string pdfPath, int pageIndex)
        {
            try
            {
                var path = GetAnnotationPath(pdfPath, pageIndex);
                return JsonHelper.LoadFromFile<PdfAnnotation>(path);
            }
            catch
            {
                return null;
            }
        }

        private void SaveAnnotationData(string pdfPath, int pageIndex, PdfAnnotation annotation)
        {
            try
            {
                var path = GetAnnotationPath(pdfPath, pageIndex);
                JsonHelper.SaveToFile(path, annotation);
            }
            catch
            {
            }
        }

        private string GetAnnotationPath(string pdfPath, int pageIndex)
        {
            return AppPaths.GetUserAnnotationPath(pdfPath, pageIndex);
        }
    }
}