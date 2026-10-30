using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Shadowing;
using Xunit;

namespace LearningAssistant.Core.Tests;

public class ShadowingAnchorKeysTests
{
    [Fact]
    public void BuildHighlightV2_IgnoresCoordinates()
    {
        var a = Highlight("Hello world.", 0.1f, 0.2f);
        var b = Highlight("Hello world.", 0.9f, 0.8f);

        Assert.Equal(
            ShadowingAnchorKeys.BuildHighlightV2(a, 0, 0),
            ShadowingAnchorKeys.BuildHighlightV2(b, 0, 0));
    }

    [Fact]
    public void BuildHighlightV2_DiffersByTextOrdinal()
    {
        var highlight = Highlight("Same text.", 0.1f, 0.2f);

        Assert.NotEqual(
            ShadowingAnchorKeys.BuildHighlightV2(highlight, 0, 0),
            ShadowingAnchorKeys.BuildHighlightV2(highlight, 0, 1));
    }

    [Fact]
    public void BuildPageTextV2_HasTxt2Prefix_AndIgnoresSentenceId()
    {
        string anchor = ShadowingAnchorKeys.BuildPageTextV2(3, "Hello world.", 0);

        Assert.StartsWith("txt2:3:", anchor);
    }

    [Fact]
    public void BuildHighlightV1_MatchesLegacyFormat()
    {
        var highlight = Highlight("Hello world.", 0.1f, 0.2f);

        Assert.Equal(
            ShadowingHighlightSource.BuildAnchorKey(highlight, 1),
            ShadowingAnchorKeys.BuildHighlightV1(highlight, 1));
    }

    [Fact]
    public void BuildCueV2_AppendsGroupOrdinal()
    {
        Assert.Equal("base|0", ShadowingAnchorKeys.BuildCueV2("base", 0));
        Assert.Equal("base|3", ShadowingAnchorKeys.BuildCueV2("base", 3));
    }

    [Fact]
    public void AnchorKeys_V2_StableWhenSentenceIdChanges()
    {
        // 文本相同、sentence.Id 变化 → V2 cue 锚点不变
        var first = ShadowingHighlightSource.BuildPageCues(
            new[] { Sentence("s1", "Hello world.") }, null, 0);
        var second = ShadowingHighlightSource.BuildPageCues(
            new[] { Sentence("zzz-changed", "Hello world.") }, null, 0);

        Assert.Single(first);
        Assert.Single(second);
        Assert.Equal(first[0].AnchorKey, second[0].AnchorKey);
        Assert.NotEqual(first[0].LegacyAnchorKey, second[0].LegacyAnchorKey);
    }

    [Fact]
    public void AnchorKeys_V2_CueAnchorUnique_WhenHighlightCoversSameTextSentences()
    {
        // 一条高亮覆盖两条文本相同的句子 → 两条 cue 锚点不同（组内序号 0/1）
        var highlight = Highlight("same sentence. same sentence.", 0.1f, 0.2f);
        var cues = ShadowingHighlightSource.BuildPageCues(
            new[]
            {
                Sentence("s1", "same sentence."),
                Sentence("s2", "same sentence.")
            },
            new[] { highlight },
            0);

        Assert.Equal(2, cues.Count);
        Assert.NotEqual(cues[0].AnchorKey, cues[1].AnchorKey);
        Assert.EndsWith("|0", cues[0].AnchorKey);
        Assert.EndsWith("|1", cues[1].AnchorKey);
        Assert.Equal(cues[0].HighlightId, cues[1].HighlightId);
    }

    private static LearningAssistant.Models.Shadowing.ShadowingSentence Sentence(string id, string text)
        => new() { Id = id, Text = text };

    private static PdfHighlight Highlight(string text, float x, float y)
        => new()
        {
            Id = "h1",
            PageIndex = 0,
            Text = text,
            NormalizedX = x,
            NormalizedY = y,
            NormalizedWidth = 0.5f,
            NormalizedHeight = 0.1f
        };
}
