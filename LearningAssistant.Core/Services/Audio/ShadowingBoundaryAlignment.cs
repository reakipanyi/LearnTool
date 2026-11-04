using LearningAssistant.Models.Audio;
using LearningAssistant.Models.Shadowing;

namespace LearningAssistant.Services.Audio;

/// <summary>一条切片边界变更（只改时间，不改文本、take 与切片路径）。</summary>
public sealed class ShadowingBoundaryChange
{
    public int PageIndex { get; init; }

    public string SegmentId { get; init; } = "";

    public int OldStartMs { get; init; }

    public int OldEndMs { get; init; }

    public int NewStartMs { get; init; }

    public int NewEndMs { get; init; }

    public bool HasTakes { get; init; }

    /// <summary>起点或终点位移是否超过阈值（默认 300ms），供 UI 提示。</summary>
    public bool ExceedsShift { get; init; }

    public int MaxShiftMs => Math.Max(
        Math.Abs(NewStartMs - OldStartMs),
        Math.Abs(NewEndMs - OldEndMs));
}

public sealed class ShadowingBoundaryApplyPlan
{
    public IReadOnlyList<ShadowingBoundaryChange> Changes { get; init; } = Array.Empty<ShadowingBoundaryChange>();

    /// <summary>多段切片映射到同一句而被跳过的段数（避免产生重叠切片）。</summary>
    public int SkippedConflicts { get; init; }
}

/// <summary>
/// 把句子时间轴写回跟读切片：按切片中点找到所属句，只对齐起止时间。
/// 保守策略：同一句被多个切片命中时全部跳过，绝不合并/删除切片与 take。
/// </summary>
public static class ShadowingBoundaryAlignment
{
    public const int DefaultShiftWarnMs = 300;

    public static ShadowingBoundaryApplyPlan Plan(
        IReadOnlyList<ShadowingSegmentManifest>? manifests,
        IReadOnlyList<AudioSentenceSpan>? sentences,
        string? trackPath,
        string? fingerprint,
        int shiftWarnMs = DefaultShiftWarnMs)
    {
        if (manifests == null || manifests.Count == 0 || sentences == null || sentences.Count == 0)
            return new ShadowingBoundaryApplyPlan();

        var ordered = sentences
            .Where(span => span.EndMs > span.StartMs)
            .OrderBy(span => span.StartMs)
            .ToList();
        if (ordered.Count == 0)
            return new ShadowingBoundaryApplyPlan();

        var matchedManifests = manifests
            .Where(manifest => manifest != null && manifest.Segments.Count > 0)
            .Where(manifest => SentenceBoundaryResolver.MatchesTrack(
                manifest.Source.TrackPath,
                manifest.Source.Fingerprint,
                trackPath,
                fingerprint))
            .ToList();

        // 每个切片先找目标句；同一句被多个切片命中时全部跳过，避免写出重叠区间
        var candidates = new List<(ShadowingSegmentManifest Manifest, ShadowingSegment Segment, int Target)>();
        foreach (var manifest in matchedManifests)
        {
            foreach (var segment in manifest.Segments)
            {
                if (segment.StartMs >= segment.EndMs) continue;
                int midpoint = (int)(((long)segment.StartMs + segment.EndMs) / 2);
                int target = FindContaining(ordered, midpoint);
                if (target < 0) continue;
                candidates.Add((manifest, segment, target));
            }
        }

        var targetCounts = candidates
            .GroupBy(candidate => candidate.Target)
            .ToDictionary(group => group.Key, group => group.Count());

        var changes = new List<ShadowingBoundaryChange>();
        int conflicts = 0;
        foreach (var (manifest, segment, target) in candidates)
        {
            if (targetCounts[target] > 1)
            {
                conflicts++;
                continue;
            }

            var span = ordered[target];
            if (segment.StartMs == span.StartMs && segment.EndMs == span.EndMs)
                continue;

            int shift = Math.Max(
                Math.Abs(span.StartMs - segment.StartMs),
                Math.Abs(span.EndMs - segment.EndMs));

            changes.Add(new ShadowingBoundaryChange
            {
                PageIndex = manifest.PageIndex,
                SegmentId = segment.Id,
                OldStartMs = segment.StartMs,
                OldEndMs = segment.EndMs,
                NewStartMs = span.StartMs,
                NewEndMs = span.EndMs,
                HasTakes = segment.Takes.Count > 0,
                ExceedsShift = shift > shiftWarnMs
            });
        }

        return new ShadowingBoundaryApplyPlan
        {
            Changes = changes.OrderBy(change => change.PageIndex).ThenBy(change => change.NewStartMs).ToList(),
            SkippedConflicts = conflicts
        };
    }

    private static int FindContaining(IReadOnlyList<AudioSentenceSpan> ordered, int position)
    {
        int low = 0;
        int high = ordered.Count - 1;
        int found = -1;
        while (low <= high)
        {
            int mid = (low + high) / 2;
            var span = ordered[mid];
            if (position < span.StartMs)
            {
                high = mid - 1;
            }
            else if (position >= span.EndMs)
            {
                low = mid + 1;
            }
            else
            {
                found = mid;
                break;
            }
        }
        return found;
    }
}
