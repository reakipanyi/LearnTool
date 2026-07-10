namespace LearningAssistant.Services.TTS
{
    public interface ITTSService
    {

        Task<string?> SpeakAsync(string text, string? language = null, float? speed = null, CancellationToken cancellationToken = default);

        Task<byte[]?> SpeakStreamAsync(string text, string? language = null, float? speed = null, string? format = null);

        Task<string?> SpeakToCacheAsync(string text, string? language = null, float? speed = null);

        Task StopAsync();
        bool IsSpeaking { get; }
        bool Available { get; }
    }
}