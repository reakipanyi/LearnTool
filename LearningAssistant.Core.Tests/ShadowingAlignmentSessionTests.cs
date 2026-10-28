using LearningAssistant.Models.Shadowing;
using LearningAssistant.Services.Shadowing;
using Xunit;

namespace LearningAssistant.Core.Tests;

public class ShadowingAlignmentSessionTests
{
    [Fact]
    public void Mark_SequentialCues_CreatesTextAnchorsAndAdvancesCursor()
    {
        var cues = new[]
        {
            Cue("s1", "First sentence.", "hl:1:abc:0|s1", "h1"),
            Cue("s2", "Second sentence.", "hl:1:def:0|s2", "h2")
        };
        var session = new ShadowingAlignmentSession(1, cues);
        session.Start(startMs: 500);

        var first = session.Mark(1500);
        var second = session.Mark(2700);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(500, first!.StartMs);
        Assert.Equal(1500, first.EndMs);
        Assert.Equal("First sentence.", first.Text);
        Assert.Equal("highlight", first.TextSource);
        Assert.Equal("h1", first.HighlightId);
        Assert.Equal("hl:1:abc:0|s1", first.HighlightAnchor);
        Assert.Equal("tap", first.BoundarySource);
        Assert.Equal(1.0, first.AlignmentConfidence);

        Assert.Equal(1500, second!.StartMs);
        Assert.Equal(2700, second.EndMs);
        Assert.Equal(2, session.CurrentIndex);
        Assert.True(session.IsCompleted);
        Assert.Equal(2700, session.CursorStartMs);
        Assert.Equal(2, session.CreatedSegments.Count);
    }

    [Fact]
    public void Mark_TooShort_DoesNotAdvanceOrCreateSegment()
    {
        var session = new ShadowingAlignmentSession(0, new[] { Cue("s1", "Hello.", "txt:0:s1", null) });
        session.Start(0);

        var segment = session.Mark(ShadowingAlignmentSession.MinimumSegmentMs - 1);

        Assert.Null(segment);
        Assert.Equal(0, session.CurrentIndex);
        Assert.Empty(session.CreatedSegments);
        Assert.Equal(0, session.CursorStartMs);
    }

    [Fact]
    public void Skip_AdvancesWithoutChangingCursor()
    {
        var cues = new[]
        {
            Cue("s1", "Skipped.", "txt:0:s1", null),
            Cue("s2", "Kept.", "txt:0:s2", null)
        };
        var session = new ShadowingAlignmentSession(0, cues);
        session.Start(1000);

        Assert.True(session.Skip());
        Assert.Equal(1, session.CurrentIndex);
        Assert.Equal(1000, session.CursorStartMs);

        var segment = session.Mark(2000);
        Assert.NotNull(segment);
        Assert.Equal(1000, segment!.StartMs);
        Assert.Equal("Kept.", segment.Text);
    }

    [Fact]
    public void Undo_RemovesLastSegmentAndRestoresCueCursor()
    {
        var cues = new[]
        {
            Cue("s1", "First.", "txt:0:s1", null),
            Cue("s2", "Second.", "txt:0:s2", null)
        };
        var session = new ShadowingAlignmentSession(0, cues);
        session.Start(0);
        session.Mark(1000);
        session.Mark(2200);

        Assert.True(session.Undo());
        Assert.Equal(1, session.CurrentIndex);
        Assert.Single(session.CreatedSegments);
        Assert.Equal(1000, session.CursorStartMs);

        Assert.True(session.Undo());
        Assert.Equal(0, session.CurrentIndex);
        Assert.Empty(session.CreatedSegments);
        Assert.Equal(0, session.CursorStartMs);
        Assert.False(session.Undo());
    }

    [Fact]
    public void Mark_ReusesExistingSegmentIdAndTakesByAnchor()
    {
        var cue = Cue("s1", "Same sentence.", "hl:3:abc:0|s1", "h1");
        var existing = new ShadowingSegment
        {
            Id = "sg_003_aaaaaaaa",
            StartMs = 100,
            EndMs = 900,
            Text = "Old text.",
            HighlightAnchor = cue.AnchorKey,
            HighlightId = cue.HighlightId,
            SlicePath = "slices/sg_003_aaaaaaaa.wav",
            Takes =
            {
                new ShadowingTake
                {
                    Id = "tk_003_aaaaaaaa",
                    Path = "takes/tk_003_aaaaaaaa.wav",
                    Chosen = true
                }
            }
        };
        var session = new ShadowingAlignmentSession(3, new[] { cue }, new[] { existing });
        session.Start(1000);

        var segment = session.Mark(2100);

        Assert.NotNull(segment);
        Assert.Equal("sg_003_aaaaaaaa", segment!.Id);
        Assert.Equal("slices/sg_003_aaaaaaaa.wav", segment.SlicePath);
        Assert.Single(segment.Takes);
        Assert.True(segment.Takes[0].Chosen);
        Assert.Equal(1000, segment.StartMs);
        Assert.Equal(2100, segment.EndMs);
    }

    [Fact]
    public void Mark_NewAnchoredCue_UsesDeterministicSegmentId()
    {
        var cue = Cue("s1", "New sentence.", "hl:5:abc:0|s1", "h1");
        var session = new ShadowingAlignmentSession(5, new[] { cue });
        session.Start(0);

        var segment = session.Mark(1200);

        Assert.NotNull(segment);
        Assert.Equal(
            ShadowingSegmentStore.CreateAnchoredSegmentId(5, cue.AnchorKey),
            segment!.Id);
    }

    private static ShadowingCue Cue(string sentenceId, string text, string anchor, string? highlightId)
    {
        return new ShadowingCue
        {
            PageIndex = 0,
            SentenceId = sentenceId,
            Text = text,
            AnchorKey = anchor,
            HighlightId = highlightId,
            TextSource = highlightId == null ? "page-text" : "highlight"
        };
    }
}
