using PdfiumViewer;
using System.Drawing.Printing;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Pdf
{
    public class PdfiumPdfService : IPdfService
    {
        private PdfDocument? _pdf;
        private string? _filePath;
        private readonly object _lockObj = new object();
        private readonly ILogger<PdfiumPdfService> _logger;

        public PdfiumPdfService(ILogger<PdfiumPdfService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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
                    _logger.LogInformation("Loading PDF file: {Path}", path);
                    _pdf = PdfDocument.Load(path);
                    _filePath = path;
                    _logger.LogInformation("PDF loaded successfully: {Path}, Pages: {PageCount}", path, _pdf.PageCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load PDF file: {Path}", path);
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
                    _logger.LogDebug("Rendering page {PageIndex} with size {Width}x{Height}", pageIndex, width, height);
                    using var img = _pdf.Render(pageIndex, width, height, 96, 96, PdfRenderFlags.Annotations);
                    var result = img != null ? new Bitmap(img) : null;
                    _logger.LogDebug("Page {PageIndex} rendered successfully", pageIndex);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to render page {PageIndex}", pageIndex);
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

        public int GetPageCount(string pdfPath)
        {
            if (string.IsNullOrWhiteSpace(pdfPath))
                throw new ArgumentException("PDF path cannot be null or empty.", nameof(pdfPath));

            try
            {
                using var doc = PdfDocument.Load(pdfPath);
                return doc.PageCount;
            }
            catch
            {
                return 0;
            }
        }

        public string ExtractText(string pdfPath, int pageNumber)
        {
            if (string.IsNullOrWhiteSpace(pdfPath))
                throw new ArgumentException("PDF path cannot be null or empty.", nameof(pdfPath));
            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than 0.");

            try
            {
                using var doc = PdfDocument.Load(pdfPath);
                if (pageNumber > doc.PageCount)
                    return string.Empty;
                return doc.GetPdfText(pageNumber - 1) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public bool Print(bool printDialog = true, int fromPage = 0, int toPage = -1)
        {
            lock (_lockObj)
            {
                if (_pdf == null)
                    return false;

                try
                {
                    _logger.LogInformation("Printing PDF: {FilePath}, PrintDialog={PrintDialog}, FromPage={FromPage}, ToPage={ToPage}",
                        _filePath, printDialog, fromPage, toPage);
                    using var printDoc = _pdf.CreatePrintDocument();
                    printDoc.DocumentName = "PDF Document";

                    if (fromPage > 0 || (toPage >= 0 && toPage < PageCount))
                    {
                        printDoc.PrinterSettings.FromPage = fromPage + 1;
                        printDoc.PrinterSettings.ToPage = (toPage < 0 ? PageCount : toPage + 1);
                        printDoc.PrinterSettings.PrintRange = PrintRange.SomePages;
                    }

                    if (printDialog)
                    {
                        using var dialog = new PrintDialog();
                        dialog.Document = printDoc;
                        dialog.UseEXDialog = true;
                        dialog.AllowSomePages = true;

                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            printDoc.Print();
                            _logger.LogInformation("PDF print job sent successfully");
                            return true;
                        }
                        _logger.LogInformation("Print dialog cancelled");
                        return false;
                    }
                    else
                    {
                        printDoc.Print();
                        _logger.LogInformation("PDF print job sent successfully");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to print PDF");
                    throw new InvalidOperationException("Failed to print PDF.", ex);
                }
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
