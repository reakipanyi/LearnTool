using PdfiumViewer;
using System.IO;

namespace UnifiedLearningAssistant.Services.Pdf
{
    public class PdfiumPdfService : IPdfService
    {
        private PdfDocument? _pdf;
        private readonly object _lockObj = new object();

        public void Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            if (!Path.IsPathRooted(path))
                throw new ArgumentException("Path must be an absolute path.", nameof(path));

            if (!File.Exists(path))
                throw new FileNotFoundException("PDF file not found.", path);

            lock (_lockObj)
            {
                _pdf?.Dispose();
                try
                {
                    _pdf = PdfDocument.Load(path);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Failed to load PDF file.", ex);
                }
            }
        }

        public int PageCount
        {
            get
            {
                lock (_lockObj)
                {
                    return _pdf?.PageCount ?? 0;
                }
            }
        }

        public Bitmap? RenderPage(int pageIndex, int width, int height)
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(pageIndex), "Page index cannot be negative.");
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than 0.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than 0.");

            lock (_lockObj)
            {
                if (_pdf == null)
                    return null;

                if (pageIndex >= _pdf.PageCount)
                    throw new ArgumentOutOfRangeException(nameof(pageIndex), "Page index exceeds page count.");

                try
                {
                    using var img = _pdf.Render(pageIndex, width, height, 96, 96, PdfRenderFlags.Annotations);
                    return img != null ? new Bitmap(img) : null;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to render page {pageIndex}.", ex);
                }
            }
        }

        public System.Drawing.SizeF GetPageSize(int pageIndex)
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(pageIndex), "Page index cannot be negative.");

            lock (_lockObj)
            {
                if (_pdf == null)
                    return System.Drawing.SizeF.Empty;

                if (pageIndex >= _pdf.PageCount)
                    return System.Drawing.SizeF.Empty;

                if (_pdf.PageSizes != null && _pdf.PageSizes.Count > pageIndex)
                    return _pdf.PageSizes[pageIndex];

                return System.Drawing.SizeF.Empty;
            }
        }

        public string GetPdfText(int pageIndex)
        {
            if (pageIndex < 0)
                throw new System.ArgumentOutOfRangeException(nameof(pageIndex), "Page index cannot be negative.");

            lock (_lockObj)
            {
                return _pdf?.GetPdfText(pageIndex) ?? string.Empty;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_lockObj)
                {
                    _pdf?.Dispose();
                    _pdf = null;
                }
            }
        }
    }
}
