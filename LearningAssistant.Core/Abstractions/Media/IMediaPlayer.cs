namespace LearningAssistant.Abstractions.Media;

public interface IMediaPlayer : IDisposable
{
    MediaPlaybackState State { get; }
    TimeSpan Position { get; }
    TimeSpan Duration { get; }
    double Speed { get; set; }
    /// <summary>音量 0..1；由播放器持有，换曲后保持</summary>
    double Volume { get; set; }
    MediaLoopMode LoopMode { get; set; }
    (TimeSpan? A, TimeSpan? B) AbLoop { get; set; }

    void Load(string mediaPath);
    Task PlayAsync(CancellationToken ct = default);
    void Pause();
    void Resume();
    void Stop();
    void Seek(TimeSpan position);
    void LoadSubtitle(string? subtitlePath);
    IReadOnlyList<SubtitleCue>? SubtitleCues { get; }

    event EventHandler<TimeSpan>? PositionChanged;
    event EventHandler<MediaPlaybackState>? StateChanged;
    event EventHandler? PlaybackFinished;
}