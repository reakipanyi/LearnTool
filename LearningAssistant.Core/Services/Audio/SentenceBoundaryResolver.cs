using LearningAssistant.Models.Audio;

namespace LearningAssistant.Services.Audio;

/// <summary>
/// 一层边界来源：手动修正 &gt; 跟读切片 &gt; 字幕 &gt; VAD。
/// </summary>
public sealed class SentenceBoundaryLayer
{
    public SentenceBoundaryLayer(string source, int priority, IReadOnlyList<AudioSentenceSpan>? spans)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Priority = priority;
        Spans = spans ?? Array.Empty<AudioSentenceSpan>();
    }

    public string Source { get; }

    public int Priority { get; }

    public IReadOnlyList<AudioSentenceSpan> Spans { get; }
}

/// <summary>
/// 来源合并与空洞填充：按时间轴把多层候选合并成一条句子时间轴，重叠处按优先级取高。
/// 只做纯时间运算，切片/字幕/手动来源由调用方准备。
/// </summary>
public static class SentenceBoundaryResolver
{
    public const int ManualPriority = 4;
    public const int ShadowingPriority = 3;
    public const int SubtitlePriority = 2;
    public const int VadPriority = 1;

    private sealed class ResolvedSpan
    {
        public int StartMs;
        public int EndMs;
        public int Priority;
        public string Source = "vad";
        public double Confidence = 1.0;
        public int? PageIndex;
        public string? SegmentId;
        public string? HighlightId;
        public string? Text;
        public string? TextSource;
    }

    public static IReadOnlyList<AudioSentenceSpan> Resolve(
        int durationMs,
        AudioTimelineOptions? options,
        params SentenceBoundaryLayer[] layers)
    {
        options ??= new AudioTimelineOptions();
        if (durationMs <= 0)
            return Array.Empty<AudioSentenceSpan>();

        int minSpeech = Math.Max(1, options.MinSpeechMs);
        var ordered = (layers ?? Array.Empty<SentenceBoundaryLayer>())
            .Where(layer => layer != null && layer.Spans.Count > 0)
            .OrderByDescending(layer => layer.Priority)
            .ToList();

        var occupied = new List<(int Start, int End)>();
        var resolved = new List<ResolvedSpan>();

        foreach (var layer in ordered)
        {
            foreach (var span in Normalize(layer.Spans, durationMs))
            {
                foreach (var piece in Subtract(span.StartMs, span.EndMs, occupied))
                {
                    if (piece.End - piece.Start < minSpeech) continue;

                    resolved.Add(new ResolvedSpan
                    {
                        StartMs = piece.Start,
                        EndMs = piece.End,
                        Priority = layer.Priority,
                        Source = layer.Source,
                        Confidence = span.Confidence,
                        PageIndex = span.PageIndex,
                        SegmentId = span.SegmentId,
                        HighlightId = span.HighlightId,
                        Text = span.Text,
                        TextSource = span.TextSource
                    });
                    AddInterval(occupied, piece.Start, piece.End);
                }
            }
        }

        ApplyPadding(resolved, durationMs, options.PaddingMs);
        DeOverlap(resolved);
        resolved.RemoveAll(span => span.EndMs <= span.StartMs);

        if (resolved.Count == 0)
        {
            var fallback = new AudioSentenceSpan
            {
                Index = 1,
                StartMs = 0,
                EndMs = durationMs,
                BoundarySource = "vad-fallback",
                Confidence = 0.4
            };
            return new[] { fallback };
        }

        resolved.Sort((a, b) => a.StartMs.CompareTo(b.StartMs));
        var result = new List<AudioSentenceSpan>(resolved.Count);
        for (int i = 0; i < resolved.Count; i++)
        {
            var span = resolved[i];
            result.Add(new AudioSentenceSpan
            {
                Index = i + 1,
                StartMs = span.StartMs,
                EndMs = span.EndMs,
                PageIndex = span.PageIndex,
                BoundarySource = span.Source,
                Confidence = span.Confidence,
                SegmentId = span.SegmentId,
                HighlightId = span.HighlightId,
                Text = span.Text,
                TextSource = span.TextSource
            });
        }
        return result;
    }

    /// <summary>
    /// 校验跟读切片是否属于当前曲目：指纹必须一致，路径按文件名比较（兼容相对/绝对路径）。
    /// </summary>
    public static bool MatchesTrack(
        string? sourceTrackPath,
        string? sourceFingerprint,
        string? trackPath,
        string? fingerprint)
    {
        if (string.IsNullOrWhiteSpace(sourceFingerprint)
            || string.IsNullOrWhiteSpace(fingerprint)
            || string.IsNullOrWhiteSpace(sourceTrackPath)
            || string.IsNullOrWhiteSpace(trackPath))
            return false;

        if (!string.Equals(sourceFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase))
            return false;

        return string.Equals(
            Path.GetFileName(sourceTrackPath),
            Path.GetFileName(trackPath),
            StringComparison.OrdinalIgnoreCase);
    }

    #region 区间运算

    private static List<AudioSentenceSpan> Normalize(IReadOnlyList<AudioSentenceSpan> spans, int durationMs)
    {
        var result = new List<AudioSentenceSpan>();
        foreach (var span in spans)
        {
            int start = Math.Max(0, span.StartMs);
            int end = Math.Min(durationMs, span.EndMs);
            if (end <= start) continue;

            result.Add(new AudioSentenceSpan
            {
                StartMs = start,
                EndMs = end,
                PageIndex = span.PageIndex,
                BoundarySource = span.BoundarySource,
                Confidence = span.Confidence,
                SegmentId = span.SegmentId,
                HighlightId = span.HighlightId,
                Text = span.Text,
                TextSource = span.TextSource
            });
        }

        result.Sort((a, b) =>
        {
            int byStart = a.StartMs.CompareTo(b.StartMs);
            return byStart != 0 ? byStart : a.EndMs.CompareTo(b.EndMs);
        });

        var merged = new List<AudioSentenceSpan>(result.Count);
        foreach (var span in result)
        {
            if (merged.Count > 0 && span.StartMs <= merged[^1].EndMs)
            {
                merged[^1].EndMs = Math.Max(merged[^1].EndMs, span.EndMs);
            }
            else
            {
                merged.Add(span);
            }
        }
        return merged;
    }

    /// <summary>从 [start,end) 中扣除已占用区间，返回剩余片段（按时间升序）。</summary>
    private static List<(int Start, int End)> Subtract(
        int start,
        int end,
        List<(int Start, int End)> occupied)
    {
        var result = new List<(int Start, int End)>();
        int cursor = start;
        foreach (var interval in occupied)
        {
            if (interval.End <= cursor) continue;
            if (interval.Start >= end) break;

            if (interval.Start > cursor)
                result.Add((cursor, Math.Min(interval.Start, end)));

            cursor = Math.Max(cursor, interval.End);
            if (cursor >= end) break;
        }

        if (cursor < end)
            result.Add((cursor, end));

        return result;
    }

    private static void AddInterval(List<(int Start, int End)> intervals, int start, int end)
    {
        if (end <= start) return;

        intervals.Add((start, end));
        intervals.Sort((a, b) => a.Start.CompareTo(b.Start));

        var merged = new List<(int Start, int End)>(intervals.Count);
        foreach (var interval in intervals)
        {
            if (merged.Count > 0 && interval.Start <= merged[^1].End)
            {
                merged[^1] = (merged[^1].Start, Math.Max(merged[^1].End, interval.End));
            }
            else
            {
                merged.Add(interval);
            }
        }

        intervals.Clear();
        intervals.AddRange(merged);
    }

    #endregion

    #region 补白与消重叠

    private static void ApplyPadding(List<ResolvedSpan> spans, int durationMs, int paddingMs)
    {
        if (paddingMs > 0)
        {
            foreach (var span in spans)
            {
                span.StartMs = Math.Max(0, span.StartMs - paddingMs);
                span.EndMs = Math.Min(durationMs, span.EndMs + paddingMs);
            }
        }
        // 多层合并后原本是无序的，消重叠前必须先按时间排序
        spans.Sort((a, b) => a.StartMs.CompareTo(b.StartMs));
    }

    /// <summary>相邻重叠：同优先级按中点回退，不同优先级让高优先级保留边界。</summary>
    private static void DeOverlap(List<ResolvedSpan> spans)
    {
        for (int i = 1; i < spans.Count; i++)
        {
            var prev = spans[i - 1];
            var current = spans[i];
            if (prev.EndMs <= current.StartMs) continue;

            if (prev.Priority == current.Priority)
            {
                int mid = (int)(((long)prev.EndMs + current.StartMs) / 2);
                prev.EndMs = mid;
                current.StartMs = mid;
            }
            else if (prev.Priority > current.Priority)
            {
                current.StartMs = prev.EndMs;
            }
            else
            {
                prev.EndMs = current.StartMs;
            }
        }
    }

    #endregion
}
