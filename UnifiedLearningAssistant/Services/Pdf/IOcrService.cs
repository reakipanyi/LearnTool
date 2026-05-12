namespace UnifiedLearningAssistant.Services.Pdf
{
    public interface IOcrService
    {
        Task<string> RecognizeTextAsync(Bitmap image);
        Task<string> RecognizeTextAsync(Bitmap image, Rectangle region);
        bool IsAvailable { get; }
        string? InitErrorMessage { get; }
        string CurrentLanguage { get; }
        bool SetLanguage(string language);
    }
}