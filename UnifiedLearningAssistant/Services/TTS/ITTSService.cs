namespace UnifiedLearningAssistant.Services.TTS
{
    public interface ITTSService
    {
        Task SpeakAsync(string text, string language = "zh", float speed = 1.0f);
        Task StopAsync();
        bool IsSpeaking { get; }
        bool Available { get; }
    }
}