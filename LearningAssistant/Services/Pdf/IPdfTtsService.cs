namespace LearningAssistant.Services.Pdf
{
    public interface IPdfTtsService
    {
        bool IsAvailable { get; }
        Task SpeakTextAsync(string text, string language, float speed);
        Task SpeakTextAsync(string text, float speed = 1.0f);
    }
}