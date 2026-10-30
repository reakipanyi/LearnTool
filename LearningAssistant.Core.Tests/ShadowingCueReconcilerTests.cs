using LearningAssistant.Models.Shadowing;
using LearningAssistant.Services.Shadowing;
using Xunit;
using PdfHighlight = LearningAssistant.Models.Pdf.PdfHighlight;

namespace LearningAssistant.Core.Tests;

public class ShadowingCueReconcilerTests
{
    [Fact]
    public void Reconcile_ExactAnchorMatch_ReturnsMatchedInCueOrder()
    {
        var cues = new[]
        {
            Cue("s1", "First.", "a1"),
            Cue("s2", "Second.", "a2")
        };
        // 乱序传入 segments，bindings 顺序必须等于 cue 顺序
        var segments = new[]
        {
            Segment("seg-2", "a2", startMs: 2000),
            Segment("seg-1", "a1", startMs: 1000)
        };

        var result = ShadowingCueReconciler.Reconcile(cues, segments);

        Assert.Equal(2, result.Bindings.Count);
        Assert.Equal("seg-1", result.Bindings[0].Segment?.Id);
        Assert.Equal("seg-2", result.Bindings[1].Segment?.Id);
        Assert.All(result.Bindings, b => Assert.Equal(CueBindingStatus.Matched, b.Status));
        Assert.Empty(result.Orphans);
        Assert.False(result.HasDrift);
    }

    [Fact]
    public void Reconcile_SkippedCue_ReturnsUnboundNotAlignedPlaceholder()
    {
        var cues = new[]
        {
            Cue("s1", "First.", "a1"),
            Cue("s2", "Skipped.", "a2"),
            Cue("s3", "Third.", "a3")
        };
        var segments = new[]
        {
            Segment("seg-1", "a1", startMs: 1000),
            Segment("seg-3", "a3", startMs: 3000)
        };

        var result = ShadowingCueReconciler.Reconcile(cues, segments, alignedOnce: true);

        Assert.Equal(
            new[] { CueBindingStatus.Matched, CueBindingStatus.Unbound, CueBindingStatus.Matched },
            result.Bindings.Select(b => b.Status).ToArray());
        Assert.Equal(CueUnboundReason.Skipped, result.Bindings[1].UnboundReason);
        Assert.Equal(1, result.UnboundCount);
    }

    [Fact]
    public void Reconcile_EmptySegments_ReturnsAllNotAlignedYet()
    {
        var cues = new[] { Cue("s1", "First.", "a1"), Cue("s2", "Second.", "a2") };

        var result = ShadowingCueReconciler.Reconcile(cues, Array.Empty<ShadowingSegment>());

        Assert.All(result.Bindings, b =>
        {
            Assert.Equal(CueBindingStatus.Unbound, b.Status);
            Assert.Equal(CueUnboundReason.NotAlignedYet, b.UnboundReason);
        });
        Assert.False(result.HasDrift);
    }

    [Fact]
    public void Reconcile_StageMatching_PreventEarlierCueStealingExactMatch()
    {
        // 第 1 句仅有文本兜底、第 2 句有精确锚点；两条 cue 文本相同、只有一条切片 → 切片必须归第 2 句
        var cues = new[]
        {
            Cue("s1", "Same text.", "other-anchor"),
            Cue("s2", "Same text.", "exact-anchor")
        };
        var segments = new[] { Segment("seg-exact", "exact-anchor", startMs: 1000, text: "Same text.") };

        var result = ShadowingCueReconciler.Reconcile(cues, segments, alignedOnce: true);

        Assert.Equal(CueBindingStatus.Unbound, result.Bindings[0].Status);
        Assert.Null(result.Bindings[0].Segment);
        Assert.Equal(CueBindingStatus.Matched, result.Bindings[1].Status);
        Assert.Equal("seg-exact", result.Bindings[1].Segment?.Id);
    }

    [Fact]
    public void Reconcile_V2Anchor_MatchesAfterCoordinateChange()
    {
        // 同一高亮坐标变化、文本不变 → V2 仍命中并复用旧切片
        var oldHighlight = new PdfHighlight
        {
            Id = "h1",
            PageIndex = 0,
            Text = "Hello world.",
            NormalizedX = 0.1f,
            NormalizedY = 0.2f,
            NormalizedWidth = 0.5f,
            NormalizedHeight = 0.1f
        };
        var oldCues = ShadowingHighlightSource.BuildPageCues(
            new[] { new ShadowingSentence { Id = "s1", Text = "Hello world." } },
            new[] { oldHighlight },
            0);
        var oldSegment = Segment("sg_000_old", oldCues[0].AnchorKey, startMs: 100, text: "Hello world.");

        var rebuiltHighlight = new PdfHighlight
        {
            Id = "h-rebuilt",
            PageIndex = 0,
            Text = "Hello world.",
            NormalizedX = 0.9f,
            NormalizedY = 0.8f,
            NormalizedWidth = 0.5f,
            NormalizedHeight = 0.1f
        };
        var newCues = ShadowingHighlightSource.BuildPageCues(
            new[] { new ShadowingSentence { Id = "s-new", Text = "Hello world." } },
            new[] { rebuiltHighlight },
            0);

        var result = ShadowingCueReconciler.Reconcile(newCues, new[] { oldSegment });

        Assert.Equal(CueBindingStatus.Matched, result.Bindings[0].Status);
        Assert.Equal("sg_000_old", result.Bindings[0].Segment?.Id);
    }

    [Fact]
    public void Reconcile_LegacyV1Anchor_MatchesAndReusesSegment()
    {
        // 只有 V1 锚点的历史切片 → P2 命中，ID 不变
        var cues = new[] { Cue("s1", "First.", "v2-anchor", legacyAnchor: "hl:0:legacy:0|s1") };
        var segments = new[] { Segment("sg_000_legacy", "hl:0:legacy:0|s1", startMs: 500) };

        var result = ShadowingCueReconciler.Reconcile(cues, segments);

        Assert.Equal(CueBindingStatus.Matched, result.Bindings[0].Status);
        Assert.Equal("sg_000_legacy", result.Bindings[0].Segment?.Id);
    }

    [Fact]
    public void Reconcile_MissingSliceFile_ReturnsMissingSliceAndKeepsSegment()
    {
        var cues = new[] { Cue("s1", "First.", "a1") };
        var segments = new[] { Segment("seg-1", "a1", startMs: 1000) };

        var result = ShadowingCueReconciler.Reconcile(cues, segments, _ => false);

        Assert.Equal(CueBindingStatus.MissingSlice, result.Bindings[0].Status);
        Assert.NotNull(result.Bindings[0].Segment);
        Assert.True(result.HasDrift);
    }

    [Fact]
    public void Reconcile_EmptySlicePath_ReturnsMissingSlice()
    {
        var cues = new[] { Cue("s1", "First.", "a1") };
        var segments = new[] { Segment("seg-1", "a1", startMs: 1000, slicePath: "") };

        var result = ShadowingCueReconciler.Reconcile(cues, segments);

        Assert.Equal(CueBindingStatus.MissingSlice, result.Bindings[0].Status);
    }

    [Fact]
    public void Reconcile_UnmatchedSegment_GoesToOrphansAndIsNotDeleted()
    {
        var cues = new[] { Cue("s1", "First.", "a1") };
        var segments = new[]
        {
            Segment("seg-1", "a1", startMs: 1000),
            Segment("seg-orphan", "no-such-anchor", startMs: 2000, text: "Other.")
        };

        var result = ShadowingCueReconciler.Reconcile(cues, segments, alignedOnce: true);

        Assert.Single(result.Orphans);
        Assert.Equal("seg-orphan", result.Orphans[0].Id);
        Assert.Equal(2, segments.Length); // 纯函数，入参不变
        Assert.True(result.HasDrift);
    }

    [Fact]
    public void Reconcile_NeverBindsOneSegmentTwice()
    {
        // 两条 cue 文本相同且只有一条无锚点切片 → 第二条为 Unbound
        var cues = new[]
        {
            Cue("s1", "Same text.", "a1"),
            Cue("s2", "Same text.", "a2")
        };
        var segments = new[] { Segment("seg-1", anchor: null, startMs: 1000, text: "Same text.") };

        var result = ShadowingCueReconciler.Reconcile(cues, segments, alignedOnce: true);

        Assert.Equal(CueBindingStatus.Matched, result.Bindings[0].Status);
        Assert.Equal("seg-1", result.Bindings[0].Segment?.Id);
        Assert.Equal(CueBindingStatus.Unbound, result.Bindings[1].Status);
        Assert.Null(result.Bindings[1].Segment);
    }

    [Fact]
    public void Reconcile_P4TextFallback_PairsByPosition()
    {
        // 无锚点极旧数据：同文本 cue（按句序）与同文本切片（按 StartMs）按位置配对
        var cues = new[]
        {
            Cue("s1", "Repeat.", "a1"),
            Cue("s2", "Repeat.", "a2")
        };
        var segments = new[]
        {
            Segment("seg-late", anchor: null, startMs: 5000, text: "Repeat."),
            Segment("seg-early", anchor: null, startMs: 1000, text: "Repeat.")
        };

        var result = ShadowingCueReconciler.Reconcile(cues, segments, alignedOnce: true);

        Assert.Equal("seg-early", result.Bindings[0].Segment?.Id);
        Assert.Equal("seg-late", result.Bindings[1].Segment?.Id);
        Assert.Empty(result.Orphans);
    }

    [Fact]
    public void Reconcile_Ambiguous_WhenMultipleCandidates()
    {
        var cues = new[] { Cue("s1", "First.", "a1") };
        var segments = new[]
        {
            Segment("seg-a", "a1", startMs: 1000),
            Segment("seg-b", "a1", startMs: 2000)
        };

        var result = ShadowingCueReconciler.Reconcile(cues, segments);

        Assert.Equal(CueBindingStatus.Ambiguous, result.Bindings[0].Status);
        Assert.Equal(2, result.Bindings[0].CandidateCount);
        Assert.Single(result.Duplicates);
        // 主候选取 Id 字典序最小者，保证确定性
        Assert.Equal("seg-a", result.Bindings[0].Segment?.Id);
        Assert.Equal("seg-b", result.Duplicates[0].Id);
    }

    [Fact]
    public void Reconcile_IsPermutationInvariant()
    {
        var cues = new[]
        {
            Cue("s1", "First.", "a1"),
            Cue("s2", "Same.", "a2"),
            Cue("s3", "Same.", "a3")
        };
        var segments = new[]
        {
            Segment("seg-1", "a1", startMs: 1000),
            Segment("seg-2", null, startMs: 2000, text: "Same."),
            Segment("seg-3", "a3", startMs: 3000),
            Segment("seg-orphan", "zzz", startMs: 4000, text: "Other.")
        };

        var expected = ShadowingCueReconciler.Reconcile(cues, segments, alignedOnce: true);
        var random = new Random(42);
        for (int trial = 0; trial < 10; trial++)
        {
            var shuffled = segments.OrderBy(_ => random.Next()).ToArray();
            var actual = ShadowingCueReconciler.Reconcile(cues, shuffled, alignedOnce: true);
            AssertEquivalent(expected, actual);
        }
    }

    [Fact]
    public void Reconcile_IsIdempotent()
    {
        var cues = new[]
        {
            Cue("s1", "First.", "a1"),
            Cue("s2", "Skipped.", "a2")
        };
        var segments = new[]
        {
            Segment("seg-1", "a1", startMs: 1000),
            Segment("seg-orphan", "zzz", startMs: 2000, text: "Other.")
        };

        var first = ShadowingCueReconciler.Reconcile(cues, segments, alignedOnce: true);
        var second = ShadowingCueReconciler.Reconcile(cues, segments, alignedOnce: true);

        AssertEquivalent(first, second);
    }

    [Fact]
    public void FindExisting_And_Reconciler_AgreeOnP1ToP3()
    {
        // 同样输入下 FindExisting 与 Reconcile 的 P1–P3 选择一致
        var cues = new[]
        {
            Cue("s1", "V2 hit.", "v2-anchor"),
            Cue("s2", "V1 hit.", "v2-other", legacyAnchor: "hl:0:legacy:0|s2"),
            Cue("s3", "Id hit.", "v2-none", highlightId: "h9")
        };
        var segments = new[]
        {
            Segment("seg-v2", "v2-anchor", startMs: 1000),
            Segment("seg-v1", "hl:0:legacy:0|s2", startMs: 2000),
            Segment("seg-id", anchor: null, startMs: 3000, highlightId: "h9")
        };

        var session = new ShadowingAlignmentSession(0, cues, segments);
        var result = ShadowingCueReconciler.Reconcile(cues, segments);

        for (int i = 0; i < cues.Length; i++)
        {
            Assert.Equal(CueBindingStatus.Matched, result.Bindings[i].Status);
            Assert.Equal(
                result.Bindings[i].Segment?.Id,
                session.FindExisting(cues[i])?.Id);
        }
    }

    [Fact]
    public void Alignment_RestartFromCue_UsesPreviousBoundEndMsAsStart()
    {
        // 从中间 cue 重对齐 → startMs = 前一条已绑定切片的 EndMs，无前序时为 0
        var cues = new[]
        {
            Cue("s1", "First.", "a1"),
            Cue("s2", "Skipped.", "a2"),
            Cue("s3", "Restart.", "a3")
        };
        var segments = new[] { Segment("seg-1", "a1", startMs: 500, endMs: 1200) };
        var result = ShadowingCueReconciler.Reconcile(cues, segments, alignedOnce: true);

        Assert.Equal(0, ShadowingCueReconciler.FindRestartStartMs(result.Bindings, 0));
        Assert.Equal(1200, ShadowingCueReconciler.FindRestartStartMs(result.Bindings, 1));
        Assert.Equal(1200, ShadowingCueReconciler.FindRestartStartMs(result.Bindings, 2));
    }

    private static void AssertEquivalent(ReconcileResult expected, ReconcileResult actual)
    {
        Assert.Equal(
            expected.Bindings.Select(b => (b.Cue.AnchorKey, b.Segment?.Id, b.Status, b.CandidateCount, b.UnboundReason)).ToArray(),
            actual.Bindings.Select(b => (b.Cue.AnchorKey, b.Segment?.Id, b.Status, b.CandidateCount, b.UnboundReason)).ToArray());
        Assert.Equal(expected.Orphans.Select(s => s.Id).ToArray(), actual.Orphans.Select(s => s.Id).ToArray());
        Assert.Equal(expected.Duplicates.Select(s => s.Id).ToArray(), actual.Duplicates.Select(s => s.Id).ToArray());
    }

    private static ShadowingCue Cue(
        string sentenceId,
        string text,
        string anchor,
        string? legacyAnchor = null,
        string? highlightId = null)
        => new()
        {
            PageIndex = 0,
            SentenceId = sentenceId,
            Text = text,
            AnchorKey = anchor,
            LegacyAnchorKey = legacyAnchor,
            HighlightId = highlightId,
            TextSource = highlightId == null ? "page-text" : "highlight"
        };

    private static ShadowingSegment Segment(
        string id,
        string? anchor,
        int startMs,
        int endMs = 0,
        string? text = null,
        string? highlightId = null,
        string slicePath = "slices/x.wav")
        => new()
        {
            Id = id,
            StartMs = startMs,
            EndMs = endMs == 0 ? startMs + 500 : endMs,
            Text = text,
            HighlightAnchor = anchor,
            HighlightId = highlightId,
            SlicePath = slicePath
        };
}
