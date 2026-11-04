using LearningAssistant.Models.Audio;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Shadowing;

namespace LearningAssistant.Services.Audio;

/// <summary>
/// 句子时间轴写回跟读切片：先给出变更计划供用户确认，再按页原子保存。
/// 只改 StartMs/EndMs；切片音频、文本、跟读 take 与合成记录保持不变。
/// </summary>
public sealed class ShadowingBoundaryApplyService
{
    private readonly ShadowingSegmentStore _store;

    public ShadowingBoundaryApplyService(ShadowingSegmentStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public ShadowingBoundaryApplyPlan Plan(
        string? pdfPath,
        AudioTrack? track,
        AudioSentenceTimeline? timeline)
    {
        if (string.IsNullOrWhiteSpace(pdfPath) || track == null || timeline == null
            || timeline.Sentences.Count == 0)
            return new ShadowingBoundaryApplyPlan();

        string fingerprint = ShadowingSegmentStore.BuildFingerprint(track.FilePath, timeline.DurationMs);
        var manifests = _store.LoadDocumentSegments(pdfPath);
        return ShadowingBoundaryAlignment.Plan(
            manifests,
            timeline.Sentences,
            track.FilePath,
            fingerprint);
    }

    /// <summary>按计划写回；返回实际更新的切片数。</summary>
    public async Task<int> ApplyAsync(
        string pdfPath,
        IReadOnlyList<ShadowingBoundaryChange> changes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pdfPath) || changes.Count == 0) return 0;

        int updated = 0;
        foreach (var group in changes.GroupBy(change => change.PageIndex).OrderBy(group => group.Key))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var manifest = _store.LoadSegments(pdfPath, group.Key);
            if (manifest == null) continue;

            bool dirty = false;
            foreach (var change in group)
            {
                var segment = manifest.Segments.FirstOrDefault(
                    item => string.Equals(item.Id, change.SegmentId, StringComparison.Ordinal));
                if (segment == null) continue;
                if (segment.StartMs == change.NewStartMs && segment.EndMs == change.NewEndMs) continue;

                segment.StartMs = change.NewStartMs;
                segment.EndMs = change.NewEndMs;
                dirty = true;
                updated++;
            }

            if (!dirty) continue;

            manifest.Session.UpdatedAt = DateTime.Now;
            // takes 保护：即使边界调整，已有录音列表原样保留
            await _store.SaveSegmentsAsync(pdfPath, manifest, cancellationToken, preserveExistingTakes: true)
                .ConfigureAwait(false);
        }

        return updated;
    }
}
