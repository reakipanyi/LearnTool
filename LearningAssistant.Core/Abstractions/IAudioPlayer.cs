namespace LearningAssistant.Abstractions
{
    /// <summary>
    /// 音频播放抽象：WinForms 端用 NAudio，MAUI 端用 MediaElement / Plugin.Audio Manager。
    /// </summary>
    public interface IAudioPlayer : IDisposable
    {
        Task PlayAsync(string audioFilePath, double speed = 1.0, CancellationToken ct = default);
        void Stop();
        bool IsPlaying { get; }
        event EventHandler? PlaybackFinished;
    }
}