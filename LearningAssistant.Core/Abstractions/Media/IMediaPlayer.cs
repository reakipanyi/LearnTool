namespace LearningAssistant.Abstractions.Media;

public interface IMediaPlayer : IDisposable
{
    MediaPlaybackState State { get; }
    TimeSpan Position { get; }
    TimeSpan Duration { get; }
    double Speed { get; set; }
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