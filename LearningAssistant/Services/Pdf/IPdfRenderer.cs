namespace LearningAssistant.Services.Pdf
{
    public interface IPdfRenderer : IDisposable
    {
        Task<Bitmap?> RenderPageAsync(int pageIndex, int width, int height);
        Task<Bitmap?> GetThumbnailAsync(int pageIndex);
        void ClearCache();
        void SetNightMode(bool enabled);
        bool IsNightMode { get; }
        int PageCount { get; }
        string CurrentFilePath { get; }
        void Initialize(IPdfService pdfService, string filePath);
        void InitializeImageMode(List<string> imageFiles);
        Task GenerateThumbnailsAsync();
        Bitmap ApplyNightMode(Bitmap bitmap);
        event EventHandler<ThumbnailGeneratedEventArgs>? ThumbnailGenerated;
    }

    public class ThumbnailGeneratedEventArgs : EventArgs
    {
        public int PageIndex { get; set; }
        public Bitmap? Thumbnail { get; set; }
    }
}