namespace LearningAssistant.Services.Audio;

/// <summary>
/// 「播到 B 停一次」纯判定：只负责“到没到”，区间由播放器持有。
/// 与 AbLoopState 类似，可在 UI 线程安全复用。
/// </summary>
public sealed class PlaybackRangeState
{
    public TimeSpan? End { get; private set; }

    public bool IsArmed => End.HasValue;

    public void Arm(TimeSpan end)
    {
        End = end < TimeSpan.Zero ? TimeSpan.Zero : end;
    }

    public void Disarm()
    {
        End = null;
    }

    /// <summary>当前位置达到或超过停止点时返回 true，并给出对齐目标。</summary>
    public bool ShouldStop(TimeSpan position, out TimeSpan snapTo)
    {
        snapTo = End ?? TimeSpan.Zero;
        if (End is null) return false;
        return position >= End.Value;
    }
}
