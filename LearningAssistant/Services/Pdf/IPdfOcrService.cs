namespace LearningAssistant.Services.Pdf
{
    public interface IPdfOcrService
    {
        bool IsAvailable { get; }
        string? InitErrorMessage { get; }
        Task<string?> RecognizeTextAsync(Bitmap image);
        Task<string?> RecognizeTextAsync(Bitmap image, Rectangle region);
        bool SetLanguage(string language);
    }
}