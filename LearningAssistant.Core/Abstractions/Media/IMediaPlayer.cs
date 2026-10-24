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
    /// <summary>在指定毫秒内淡出到静音；blocking=true 时同步完成，用于切歌前收尾</summary>
    void FadeOut(int milliseconds, bool blocking = false);
    /// <summary>从静音在指定毫秒内淡入到当前 Volume</summary>
    void FadeIn(int milliseconds);
    void LoadSubtitle(string? subtitlePath);
    IReadOnlyList<SubtitleCue>? SubtitleCues { get; }

    event EventHandler<TimeSpan>? PositionChanged;
    event EventHandler<MediaPlaybackState>? StateChanged;
    event EventHandler? PlaybackFinished;
}