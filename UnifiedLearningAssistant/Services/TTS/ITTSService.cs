namespace UnifiedLearningAssistant.Services.TTS
{
    public interface ITTSService
    {

        Task<string?> SpeakAsync(string text, string? language = null, float? speed = null);

        Task<byte[]?> SpeakSteamAsync(string text, string? language = null, float? speed = null, string? format = null);

        Task StopAsync();
        bool IsSpeaking { get; }
        bool Available { get; }
    }
}