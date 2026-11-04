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

    /// <summary>
    /// 设定并启动一次播放区间：内部 Seek 到 start、武装 end 停止点。
    /// start/end 为源时间；end 超过 Duration 时按 Duration 处理；start &gt;= end 返回 false。
    /// 已有区间时按 Replace 处理：先清旧区间（旧区间收到 Replaced），再武装新区间。
    /// </summary>
    bool SetPlaybackRange(TimeSpan start, TimeSpan end);

    /// <summary>取消当前区间（Esc / 切曲 / 离开预览）。会触发 Cancelled。</summary>
    void ClearPlaybackRange();

    event EventHandler<TimeSpan>? PositionChanged;
    event EventHandler<MediaPlaybackState>? StateChanged;
    event EventHandler? PlaybackFinished;
    /// <summary>系统默认播放设备变化时触发，供上层重新加载到新设备</summary>
    event EventHandler? OutputDeviceChanged;
    /// <summary>区间结束（UI 线程）；原因见 PlaybackRangeEndReason。</summary>
    event EventHandler<PlaybackRangeCompletedEventArgs>? PlaybackRangeCompleted;
}

public enum PlaybackRangeEndReason
{
    /// <summary>播放到 end 自动停止。</summary>
    Completed,

    /// <summary>调用方主动清除（Esc / 切曲 / 离开预览）。</summary>
    Cancelled,

    /// <summary>外部 Seek 接管，原区间作废。</summary>
    SeekedAway,

    /// <summary>设置新区间替换了旧区间。</summary>
    Replaced
}

public sealed class PlaybackRangeCompletedEventArgs
{
    public PlaybackRangeEndReason Reason { get; init; }

    public TimeSpan End { get; init; }
}