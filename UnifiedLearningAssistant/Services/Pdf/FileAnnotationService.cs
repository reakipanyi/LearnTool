using UnifiedLearningAssistant.Common;
using UnifiedLearningAssistant.Models.Pdf;

namespace UnifiedLearningAssistant.Services.Pdf
{
    public class FileAnnotationService : IAnnotationService
    {
        public Bitmap? LoadAnnotation(string pdfPath, int pageIndex, int targetWidth, int targetHeight, SizeF pageOriginalSize)
        {
            try
            {
                var annotation = LoadAnnotationData(pdfPath, pageIndex);
                if (annotation == null || annotation.Strokes.Count == 0)
                    return null;

                var bitmap = new Bitmap(targetWidth, targetHeight);
                using var g = Graphics.FromImage(bitmap);
                g.Clear(Color.Transparent);

                float scaleX = (float)targetWidth / (pageOriginalSize.Width > 0 ? pageOriginalSize.Width : targetWidth);
                float scaleY = (float)targetHeight / (pageOriginalSize.Height > 0 ? pageOriginalSize.Height : targetHeight);

                foreach (var stroke in annotation.Strokes)
                {
                    using var pen = new Pen(Color.FromArgb(stroke.ColorArgb), stroke.Thickness);
                    for (int i = 0; i < stroke.Points.Length - 3; i += 2)
                    {
                        float x1 = stroke.Points[i] * pageOriginalSize.Width * scaleX;
                        float y1 = stroke.Points[i + 1] * pageOriginalSize.Height * scaleY;
                        float x2 = stroke.Points[i + 2] * pageOriginalSize.Width * scaleX;
                        float y2 = stroke.Points[i + 3] * pageOriginalSize.Height * scaleY;
                        g.DrawLine(pen, x1, y1, x2, y2);
                    }
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
            var fileName = Path.GetFileNameWithoutExtension(pdfPath);
            return Path.Combine(FileHelper.GetAnnotationsDirectory(), $"{fileName}_page{pageIndex}.json");
        }
    }
}