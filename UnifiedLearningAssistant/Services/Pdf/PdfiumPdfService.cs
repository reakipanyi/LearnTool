using PdfiumViewer;

namespace UnifiedLearningAssistant.Services.Pdf
{
    public class PdfiumPdfService : IPdfService
    {
        private PdfDocument _pdf;
        private readonly object _lockObj = new object();

        public void Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new System.ArgumentException("Path cannot be null or empty.", nameof(path));

            lock (_lockObj)
            {
                _pdf?.Dispose();
                _pdf = PdfDocument.Load(path);
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

        public Bitmap RenderPage(int pageIndex, int width, int height)
        {
            if (pageIndex < 0)
                throw new System.ArgumentOutOfRangeException(nameof(pageIndex), "Page index cannot be negative.");
            if (width <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(width), "Width must be greater than 0.");
            if (height <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(height), "Height must be greater than 0.");

            lock (_lockObj)
            {
                if (_pdf == null)
                    return null;

                using var img = _pdf.Render(pageIndex, width, height, 96, 96, PdfRenderFlags.Annotations);
                return img != null ? new Bitmap(img) : null;
            }
        }

        public System.Drawing.SizeF GetPageSize(int pageIndex)
        {
            if (pageIndex < 0)
                throw new System.ArgumentOutOfRangeException(nameof(pageIndex), "Page index cannot be negative.");

            lock (_lockObj)
            {
                if (_pdf == null)
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
