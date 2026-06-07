namespace LearningAssistant.Services.Pdf
{
    public interface IPdfService : IDisposable
    {
        void Load(string path);
        int PageCount { get; }
        Bitmap RenderPage(int pageIndex, int width, int height);
        SizeF GetPageSize(int pageIndex);
        string GetPdfText(int pageIndex);
        
        int GetPageCount(string pdfPath);
        string ExtractText(string pdfPath, int pageNumber);
    }
}