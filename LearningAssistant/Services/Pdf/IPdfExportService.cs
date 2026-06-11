using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    public interface IPdfExportService
    {
        Task<bool> ExportHighlightsToExcelAsync(string outputPath, List<PdfHighlight> highlights, string sourcePath, bool isImageMode, List<string>? imageFiles = null, IPdfService? pdfService = null);
        Task<bool> ExportHighlightsToExcelAsync(string outputPath, string sourcePath, bool isImageMode, List<string>? imageFiles = null, IPdfService? pdfService = null);
    }
}