using LearningAssistant.Models.Audio;

namespace LearningAssistant.Services.Audio;

/// <summary>覆盖率决策：整轨分析 / 只分析空洞 / 跳过 VAD。</summary>
public enum AnalysisCoverageMode
{
    /// <summary>已有时长覆盖 &lt; 30%，直接整轨分析。</summary>
    None,

    /// <summary>覆盖 30%–90%，只分析未覆盖空洞。</summary>
    Partial,

    /// <summary>覆盖 ≥ 90%，不跑 VAD，直接用切片/手动边界。</summary>
    Full
}

/// <summary>
/// 纯函数覆盖率计划：把切片与手动修正当作已覆盖区间，算出未覆盖空洞与决策模式。
/// 只影响分析范围，不修改任何已有边界。
/// </summary>
public sealed class AnalysisCoveragePlan
{
    public const double NoneThreshold = 0.30;
    public const double FullThreshold = 0.90;

    public AnalysisCoverageMode Mode { get; private init; } = AnalysisCoverageMode.None;

    /// <summary>已覆盖比例 0–1。</summary>
    public double Coverage { get; private init; }

    /// <summary>未覆盖空洞；Full 时为空，None 时为空（调用方整轨分析）。</summary>
    public IReadOnlyList<AudioTimeRange> Holes { get; private init; } = Array.Empty<AudioTimeRange>();

    public static AnalysisCoveragePlan Plan(
        IReadOnlyList<AudioSentenceSpan>? slices,
        IReadOnlyList<AudioSentenceSpan>? manual,
        int durationMs)
    {
        if (durationMs <= 0)
            return new AnalysisCoveragePlan { Mode = AnalysisCoverageMode.None, Coverage = 0 };

        var coverageIntervals = MergeIntervals(CollectIntervals(slices, durationMs), durationMs);
        coverageIntervals = MergeIntervals(
            coverageIntervals.Concat(CollectIntervals(manual, durationMs)).ToList(),
            durationMs);

        long covered = 0;
        foreach (var interval in coverageIntervals)
            covered += interval.DurationMs;

        double coverage = Math.Min(1.0, (double)covered / durationMs);

        if (coverage >= FullThreshold)
        {
            return new AnalysisCoveragePlan
            {
                Mode = AnalysisCoverageMode.Full,
                Coverage = coverage
            };
        }

        if (coverage >= NoneThreshold)
        {
            return new AnalysisCoveragePlan
            {
                Mode = AnalysisCoverageMode.Partial,
                Coverage = coverage,
                Holes = ComputeHoles(coverageIntervals, durationMs)
            };
        }

        return new AnalysisCoveragePlan
        {
            Mode = AnalysisCoverageMode.None,
            Coverage = coverage
        };
    }

    private static List<AudioTimeRange> CollectIntervals(
        IReadOnlyList<AudioSentenceSpan>? spans,
        int durationMs)
    {
        var result = new List<AudioTimeRange>();
        if (spans == null) return result;

        foreach (var span in spans)
        {
            int start = Math.Max(0, span.StartMs);
            int end = Math.Min(durationMs, span.EndMs);
            if (end > start)
                result.Add(new AudioTimeRange(start, end));
        }
        return result;
    }

    private static List<AudioTimeRange> MergeIntervals(List<AudioTimeRange> intervals, int durationMs)
    {
        if (intervals.Count == 0) return intervals;

        intervals.Sort((a, b) =>
        {
            int byStart = a.StartMs.CompareTo(b.StartMs);
            return byStart != 0 ? byStart : a.EndMs.CompareTo(b.EndMs);
        });

        var merged = new List<AudioTimeRange>(intervals.Count);
        foreach (var interval in intervals)
        {
            if (merged.Count > 0 && interval.StartMs <= merged[^1].EndMs)
            {
                var last = merged[^1];
                merged[^1] = new AudioTimeRange(last.StartMs, Math.Max(last.EndMs, interval.EndMs));
            }
            else
            {
                merged.Add(interval);
            }
        }

        if (merged.Count > 0 && merged[^1].EndMs > durationMs)
            merged[^1] = new AudioTimeRange(merged[^1].StartMs, durationMs);

        merged.RemoveAll(interval => interval.EndMs <= interval.StartMs);
        return merged;
    }

    private static List<AudioTimeRange> ComputeHoles(List<AudioTimeRange> covered, int durationMs)
    {
        var holes = new List<AudioTimeRange>();
        int cursor = 0;
        foreach (var interval in covered)
        {
            if (interval.StartMs > cursor)
                holes.Add(new AudioTimeRange(cursor, interval.StartMs));
            cursor = Math.Max(cursor, interval.EndMs);
        }

        if (cursor < durationMs)
            holes.Add(new AudioTimeRange(cursor, durationMs));

        return holes;
    }
}
