namespace LearningAssistant.Services.Pdf
{
    public interface IPdfTranslationService
    {
        bool IsAvailable { get; }
        Task<string?> TranslateAsync(string text);
        Task<(string? Original, string? Translation)> OcrAndTranslateAsync(Bitmap image);
        Task<(string? Original, string? Translation)> OcrAndTranslateAsync(Bitmap image, Rectangle region);
    }
}