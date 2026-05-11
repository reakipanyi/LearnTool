namespace UnifiedLearningAssistant.Services.Pdf
{
    public interface ITranslationService
    {
        Task<string?> TranslateAsync(string text, string from = "auto", string to = "zh");
        bool IsAvailable { get; }
    }
}