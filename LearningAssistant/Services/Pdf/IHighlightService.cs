using System.Drawing;
using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    public interface IHighlightService
    {
        List<PdfHighlight> GetHighlights(string pdfPath);
        List<PdfHighlight> GetAllHighlights(string pdfPath);
        List<PdfHighlight> GetHighlightsForPage(string pdfPath, int pageIndex);
        // 按目录获取所有高亮
        List<PdfHighlight> GetHighlightsForFolder(string folderPath);
        void AddHighlight(string pdfPath, int pageIndex, float normalizedX, float normalizedY, float normalizedWidth, float normalizedHeight, string text = "", HighlightColor color = HighlightColor.Yellow);
        void AddHighlightWithNote(string pdfPath, int pageIndex, float normalizedX, float normalizedY, float normalizedWidth, float normalizedHeight, string text, string note, HighlightColor color = HighlightColor.Yellow);
        void UpdateHighlightNote(string pdfPath, string highlightId, string note);
        void RemoveHighlight(string pdfPath, string highlightId);
        void RemoveHighlightsForPage(string pdfPath, int pageIndex);
        void ClearCache();
        void ClearCacheForPdf(string pdfPath);
    }
}