using UnifiedLearningAssistant.Models.Pdf;

namespace UnifiedLearningAssistant.Services.Pdf
{
    public interface IBookmarkService
    {
        List<PdfBookmark> GetBookmarks(string pdfPath);
        void AddBookmark(string pdfPath, int pageIndex, string title);
        void RemoveBookmark(string pdfPath, int pageIndex, string title);
        void RemoveBookmarkByIndex(string pdfPath, int pageIndex);
        bool HasBookmark(string pdfPath, int pageIndex);
        void ClearCache();
    }
}