using LearningAssistant.Abstractions;
using Microsoft.Extensions.Logging;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Managers
{
    public class AnnotationLayerManager
    {
        private readonly ILogger _logger;
        private readonly IPdfReaderFormAccess _form;

        public AnnotationLayerManager(ILogger logger, IPdfReaderFormAccess form)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _form = form ?? throw new ArgumentNullException(nameof(form));
        }

        public Bitmap? AnnotationBitmap { get; private set; }
        public Graphics? AnnotationGraphics { get; private set; }
        public Bitmap? SecondAnnotationBitmap { get; private set; }
        public Graphics? SecondAnnotationGraphics { get; private set; }

        public void EnsureAnnotationBitmap()
        {
            try
            {
                if (_form.CurrentPageImage == null || _form.Presenter == null) return;

                int imgWidth = _form.CurrentPageImage.Width;
                int imgHeight = _form.CurrentPageImage.Height;

                if (AnnotationBitmap == null || AnnotationBitmap.Width != imgWidth || AnnotationBitmap.Height != imgHeight)
                {
                    AnnotationGraphics?.Dispose();
                    AnnotationBitmap?.Dispose();
                    AnnotationBitmap = new Bitmap(imgWidth, imgHeight);
                    AnnotationGraphics = Graphics.FromImage(AnnotationBitmap);
                    AnnotationGraphics.SmoothingMode = SmoothingMode.AntiAlias;
                    AnnotationGraphics.Clear(Color.Transparent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error ensuring annotation bitmap: {Message}", ex.Message);
            }
        }

        public void EnsureSecondAnnotationBitmap(int imgWidth, int imgHeight)
        {
            SecondAnnotationGraphics?.Dispose();
            SecondAnnotationBitmap?.Dispose();
            SecondAnnotationBitmap = new Bitmap(imgWidth, imgHeight);
            SecondAnnotationGraphics = Graphics.FromImage(SecondAnnotationBitmap);
            SecondAnnotationGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            SecondAnnotationGraphics.Clear(Color.Transparent);
        }

        public void CleanupAnnotationBitmap()
        {
            try
            {
                AnnotationGraphics?.Dispose();
                AnnotationBitmap?.Dispose();
                AnnotationGraphics = null;
                AnnotationBitmap = null;

                SecondAnnotationGraphics?.Dispose();
                SecondAnnotationBitmap?.Dispose();
                SecondAnnotationGraphics = null;
                SecondAnnotationBitmap = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up annotation bitmap");
            }
        }

        public void CleanupSecondAnnotationBitmap()
        {
            try
            {
                SecondAnnotationGraphics?.Dispose();
                SecondAnnotationBitmap?.Dispose();
                SecondAnnotationGraphics = null;
                SecondAnnotationBitmap = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up second annotation bitmap");
            }
        }

        public void LoadAnnotationsForCurrentPage()
        {
            try
            {
                if (_form.CurrentPageImage == null || _form.Presenter == null) return;

                int imgWidth = _form.CurrentPageImage.Width;
                int imgHeight = _form.CurrentPageImage.Height;

                var annotationBytes = _form.Presenter.LoadAnnotationForCurrentPage(imgWidth, imgHeight);
                ApplyLoadedAnnotationBitmap(annotationBytes != null ? new Bitmap(new MemoryStream(annotationBytes)) : null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error loading annotations for current page");
            }
        }

        public void ApplyLoadedAnnotationBitmap(Bitmap? annotationBitmap)
        {
            try
            {
                if (annotationBitmap != null)
                {
                    CleanupAnnotationBitmap();
                    AnnotationBitmap = new Bitmap(annotationBitmap);
                    AnnotationGraphics = Graphics.FromImage(AnnotationBitmap);
                    AnnotationGraphics.SmoothingMode = SmoothingMode.AntiAlias;
                }
                else
                {
                    EnsureAnnotationBitmap();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error applying loaded annotation bitmap");
            }
        }

        public void ApplySecondLoadedAnnotationBitmap(Bitmap? annotationBitmap)
        {
            try
            {
                if (annotationBitmap != null)
                {
                    SecondAnnotationGraphics?.Dispose();
                    SecondAnnotationBitmap?.Dispose();
                    SecondAnnotationBitmap = new Bitmap(annotationBitmap);
                    SecondAnnotationGraphics = Graphics.FromImage(SecondAnnotationBitmap);
                    SecondAnnotationGraphics.SmoothingMode = SmoothingMode.AntiAlias;
                }
                else
                {
                    if (_form.SecondPageImage != null)
                    {
                        SecondAnnotationBitmap?.Dispose();
                        SecondAnnotationGraphics?.Dispose();
                        SecondAnnotationBitmap = new Bitmap(_form.SecondPageImage.Width, _form.SecondPageImage.Height);
                        SecondAnnotationGraphics = Graphics.FromImage(SecondAnnotationBitmap);
                        SecondAnnotationGraphics.Clear(Color.Transparent);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error applying second annotation bitmap");
            }
        }

        }
}