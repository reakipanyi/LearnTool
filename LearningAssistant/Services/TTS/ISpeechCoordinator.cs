using System.Collections.Concurrent;

namespace LearningAssistant.Services.TTS
{
    public interface ISpeechCoordinator
    {
        Task SpeakAsync(string text, string language, CancellationToken cancellationToken = default, string? speakKey = null);
        
        Task SpeakAsync(string text, string language, string? explanation, CancellationToken cancellationToken = default, string? speakKey = null);
        
        Task StopAsync();
        
        bool IsSpeaking { get; }
        
        string CurrentSpeakKey { get; }
        
        event EventHandler<SpeakStateChangedEventArgs>? SpeakStateChanged;
        
        Task PreloadAsync(string text, string language);
    }
}