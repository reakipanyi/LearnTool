using LearningAssistant.Models.Audio;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Models.Shadowing;
using LearningAssistant.Services.Audio;
using LearningAssistant.Services.Shadowing;

namespace LearningAssistant.Services.Audio;

/// <summary>
/// 把跟读切片作为句子边界的高优先级只读来源。
/// P0 只读取、不改写切片；源音频指纹不一致的旧切片直接跳过，避免把偏移边界当作句子。
/// </summary>
public sealed class ShadowingBoundaryLayerProvider
{
    public const string Source = "shadowing";

    private readonly ShadowingSegmentStore _store;

    public ShadowingBoundaryLayerProvider(ShadowingSegmentStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public Task<IReadOnlyList<SentenceBoundaryLayer>> GetLayersAsync(
        string? pdfPath,
        AudioTrack track,
        int durationMs,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pdfPath) || track == null || string.IsNullOrWhiteSpace(track.FilePath))
            return Task.FromResult<IReadOnlyList<SentenceBoundaryLayer>>(Array.Empty<SentenceBoundaryLayer>());

        return Task.Run(() => BuildLayers(pdfPath, track, durationMs, cancellationToken), cancellationToken);
    }

    private IReadOnlyList<SentenceBoundaryLayer> BuildLayers(
        string pdfPath,
        AudioTrack track,
        int durationMs,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ShadowingSegmentManifest> manifests;
        try
        {
            manifests = _store.LoadDocumentSegments(pdfPath);
        }
        catch (Exception)
        {
            return Array.Empty<SentenceBoundaryLayer>();
        }

        var spans = new List<AudioSentenceSpan>();
        foreach (var manifest in manifests)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var source = manifest.Source;
            if (source == null || manifest.Segments.Count == 0) continue;
            if (!string.Equals(source.TrackPath, track.FilePath, StringComparison.OrdinalIgnoreCase)) continue;

            // 指纹按切片记录自身的时长计算：旧切片只要求源文件未变化，不要求本次解码时长完全一致
            string expected = ShadowingSegmentStore.BuildFingerprint(
                track.FilePath,
                source.DurationMs > 0 ? source.DurationMs : durationMs);
            if (!string.IsNullOrWhiteSpace(source.Fingerprint)
                && !string.IsNullOrWhiteSpace(expected)
                && !string.Equals(source.Fingerprint, expected, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var segment in manifest.Segments)
            {
                if (segment.EndMs <= segment.StartMs) continue;

                spans.Add(new AudioSentenceSpan
                {
                    StartMs = segment.StartMs,
                    EndMs = segment.EndMs,
                    PageIndex = manifest.PageIndex,
                    BoundarySource = Source,
                    Confidence = segment.AlignmentConfidence ?? 0.9,
                    SegmentId = segment.Id,
                    HighlightId = segment.HighlightId,
                    Text = segment.Text,
                    TextSource = segment.TextSource
                });
            }
        }

        if (spans.Count == 0)
            return Array.Empty<SentenceBoundaryLayer>();

        return new[]
        {
            new SentenceBoundaryLayer(Source, SentenceBoundaryResolver.ShadowingPriority, spans)
        };
    }
}
