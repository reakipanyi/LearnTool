using LearningAssistant.Models.Audio;
using LearningAssistant.Models.Shadowing;

namespace LearningAssistant.Services.Audio;

/// <summary>一条待补充的跟读切片（时间已裁剪到音频范围内）。</summary>
public sealed record ShadowingSliceAppendChange(
    int StartMs,
    int EndMs,
    string? Text,
    string? TextSource,
    string? BoundarySource,
    double Confidence);

/// <summary>补充计划：只新增不重叠的句子，绝不删除或修改既有切片与 take。</summary>
public sealed class ShadowingSliceAppendPlan
{
    public List<ShadowingSliceAppendChange> Changes { get; } = new();

    /// <summary>与既有切片重叠而被跳过的句子数。</summary>
    public int SkippedOverlap { get; set; }

    /// <summary>区间为空或超出音频范围而被跳过的句子数。</summary>
    public int SkippedInvalid { get; set; }

    public bool HasChanges => Changes.Count > 0;
}

/// <summary>
/// 把句子时间轴补充为跟读切片：既有切片（含录音）原样保留，
/// 只为没有重叠的句子新建切片，避免破坏已有 take。
/// </summary>
public static class ShadowingSliceAppendPlanner
{
    public static ShadowingSliceAppendPlan Plan(
        IReadOnlyList<AudioSentenceSpan>? sentences,
        IReadOnlyList<ShadowingSegment>? existing,
        int durationMs)
    {
        var plan = new ShadowingSliceAppendPlan();
        if (sentences == null || sentences.Count == 0) return plan;

        var accepted = new List<AudioTimeRange>();
        foreach (var segment in existing ?? Array.Empty<ShadowingSegment>())
        {
            if (segment.EndMs > segment.StartMs)
                accepted.Add(new AudioTimeRange(segment.StartMs, segment.EndMs));
        }

        var ordered = sentences
            .Where(span => span != null)
            .OrderBy(span => span.StartMs)
            .ThenBy(span => span.EndMs);

        foreach (var span in ordered)
        {
            int start = Math.Max(0, span.StartMs);
            int end = durationMs > 0 ? Math.Min(durationMs, span.EndMs) : span.EndMs;
            if (end <= start)
            {
                plan.SkippedInvalid++;
                continue;
            }

            if (accepted.Any(range => Overlaps(range, start, end)))
            {
                plan.SkippedOverlap++;
                continue;
            }

            accepted.Add(new AudioTimeRange(start, end));
            plan.Changes.Add(new ShadowingSliceAppendChange(
                start,
                end,
                span.Text,
                span.TextSource,
                span.BoundarySource,
                span.Confidence));
        }

        return plan;
    }

    private static bool Overlaps(AudioTimeRange range, int start, int end)
        => Math.Min(range.EndMs, end) > Math.Max(range.StartMs, start);
}
