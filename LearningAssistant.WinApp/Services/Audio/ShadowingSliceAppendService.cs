using LearningAssistant.Models.Audio;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Models.Shadowing;
using LearningAssistant.Services.Shadowing;

namespace LearningAssistant.Services.Audio;

/// <summary>
/// 把本页句子补充为跟读切片。既有切片与录音原样保留，只新增不重叠的句子，
/// 新建切片可随后由跟读流程生成切片音频。
/// </summary>
public sealed class ShadowingSliceAppendService
{
    private readonly ShadowingSegmentStore _store;

    public ShadowingSliceAppendService(ShadowingSegmentStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public ShadowingSliceAppendPlan Plan(
        string? pdfPath,
        int pageIndex,
        IReadOnlyList<AudioSentenceSpan>? sentences,
        int durationMs)
    {
        if (string.IsNullOrWhiteSpace(pdfPath) || pageIndex < 0)
            return new ShadowingSliceAppendPlan();

        var manifest = _store.LoadSegments(pdfPath, pageIndex);
        var pageSentences = sentences?.Where(span => span.PageIndex == pageIndex).ToList();
        return ShadowingSliceAppendPlanner.Plan(pageSentences, manifest?.Segments, durationMs);
    }

    /// <summary>按计划新增切片；返回新增条数。</summary>
    public async Task<int> ApplyAsync(
        string? pdfPath,
        AudioTrack track,
        int pageIndex,
        int durationMs,
        ShadowingSliceAppendPlan plan,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pdfPath) || pageIndex < 0 || plan == null || !plan.HasChanges)
            return 0;

        var manifest = _store.LoadSegments(pdfPath, pageIndex);
        if (manifest == null)
        {
            manifest = new ShadowingSegmentManifest
            {
                PageIndex = pageIndex,
                Source = new ShadowingSegmentSource
                {
                    TrackPath = track.FilePath,
                    Fingerprint = ShadowingSegmentStore.BuildFingerprint(track.FilePath, durationMs),
                    DurationMs = durationMs
                },
                Session = new ShadowingSegmentSession
                {
                    Id = ShadowingSegmentStore.CreateSessionId(DateTime.Now),
                    UpdatedAt = DateTime.Now
                }
            };
        }

        foreach (var change in plan.Changes)
        {
            string id = ShadowingSegmentStore.CreateSegmentId(pageIndex);
            manifest.Segments.Add(new ShadowingSegment
            {
                Id = id,
                StartMs = change.StartMs,
                EndMs = change.EndMs,
                Text = change.Text,
                TextSource = change.TextSource,
                BoundarySource = change.BoundarySource,
                AlignmentConfidence = change.Confidence,
                SlicePath = _store.GetSliceRelativePath(id)
            });
        }

        manifest.Segments.Sort((a, b) => a.StartMs.CompareTo(b.StartMs));
        manifest.Session.UpdatedAt = DateTime.Now;
        await _store.SaveSegmentsAsync(pdfPath, manifest, cancellationToken, preserveExistingTakes: true)
            .ConfigureAwait(false);
        return plan.Changes.Count;
    }
}
