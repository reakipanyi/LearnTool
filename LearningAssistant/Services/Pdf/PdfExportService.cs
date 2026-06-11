using LearningAssistant.Models.Pdf;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Pdf
{
    public class PdfExportService : IPdfExportService
    {
        private readonly ILogger<PdfExportService> _logger;
        private readonly IHighlightService _highlightService;

        public PdfExportService(ILogger<PdfExportService> logger, IHighlightService highlightService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _highlightService = highlightService ?? throw new ArgumentNullException(nameof(highlightService));
        }

        public async Task<bool> ExportHighlightsToExcelAsync(string outputPath, List<PdfHighlight> highlights, string sourcePath, bool isImageMode, List<string>? imageFiles = null, IPdfService? pdfService = null)
        {
            if (highlights == null || highlights.Count == 0)
            {
                _logger.LogWarning("No highlights to export");
                return false;
            }

            try
            {
                var exportLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<HighlightExportService>.Instance;
                var exportService = new HighlightExportService(exportLogger);

                if (isImageMode)
                {
                    var folderPath = Path.GetDirectoryName(sourcePath) ?? "";
                    return await exportService.ExportHighlightsToExcelAsync(highlights, folderPath, outputPath, null, imageFiles?.ToList() ?? new List<string>());
                }
                else
                {
                    return await exportService.ExportHighlightsToExcelAsync(highlights, sourcePath, outputPath, pdfService);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export highlights");
                return false;
            }
        }

        public async Task<bool> ExportHighlightsToExcelAsync(string outputPath, string sourcePath, bool isImageMode, List<string>? imageFiles = null, IPdfService? pdfService = null)
        {
            var folderPath = Path.GetDirectoryName(sourcePath) ?? "";
            List<PdfHighlight> highlights;

            if (isImageMode)
            {
                highlights = _highlightService.GetHighlightsForFolder(folderPath);
            }
            else
            {
                highlights = _highlightService.GetHighlights(sourcePath);
            }

            if (highlights == null || highlights.Count == 0)
            {
                _logger.LogWarning("No highlights to export");
                return false;
            }

            return await ExportHighlightsToExcelAsync(outputPath, highlights, sourcePath, isImageMode, imageFiles, pdfService);
        }
    }
}