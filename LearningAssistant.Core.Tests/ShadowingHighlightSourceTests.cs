using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Shadowing;
using Xunit;

namespace LearningAssistant.Core.Tests;

public class ShadowingHighlightSourceTests
{
    [Fact]
    public void Build_OrdersByPageThenVerticalPosition()
    {
        var highlights = new[]
        {
            Highlight("page2-lower", page: 2, y: 0.6f, x: 0.1f, text: "Page two lower."),
            Highlight("page1-lower", page: 1, y: 0.5f, x: 0.1f, text: "Page one lower."),
            Highlight("page1-upper", page: 1, y: 0.2f, x: 0.4f, text: "Page one upper."),
            Highlight("page1-left", page: 1, y: 0.2f, x: 0.1f, text: "Page one left.")
        };

        var plan = ShadowingHighlightSource.Build(highlights);

        Assert.Equal(
            new[] { "Page one left.", "Page one upper.", "Page one lower.", "Page two lower." },
            plan.Sentences.Select(s => s.Sentence.Text));
        Assert.Equal(new[] { 1, 1, 1, 2 }, plan.Sentences.Select(s => s.PageIndex));
    }

    [Fact]
    public void Build_SkipsEmptyTextAndCountsSkippedHighlights()
    {
        var highlights = new[]
        {
            Highlight("empty", page: 0, y: 0.1f, text: "   "),
            Highlight("valid", page: 0, y: 0.2f, text: "Keep me."),
            Highlight("null-text", page: 0, y: 0.3f, text: null)
        };

        var plan = ShadowingHighlightSource.Build(highlights);

        Assert.Single(plan.Sentences);
        Assert.Equal("Keep me.", plan.Sentences[0].Sentence.Text);
        Assert.Equal(2, plan.SkippedCount);
    }

    [Fact]
    public void Build_NormalizesWhitespaceAndUsesStablePrefixedIds()
    {
        var highlight = Highlight("stable-id", page: 3, y: 0.1f, text: "  Hello \r\n world  ");

        var plan = ShadowingHighlightSource.Build(new[] { highlight, highlight });

        Assert.Single(plan.Sentences);
        Assert.Equal("Hello world", plan.Sentences[0].Sentence.Text);
        Assert.Equal(ShadowingHighlightSource.SentenceIdPrefix + "stable-id", plan.Sentences[0].Sentence.Id);
        Assert.Equal(0, plan.Sentences[0].Sentence.Index);
        Assert.Equal("Hello world".Length, plan.Sentences[0].Sentence.Length);
    }

    private static PdfHighlight Highlight(string id, int page, float y, string? text, float x = 0.1f)
    {
        return new PdfHighlight
        {
            Id = id,
            PageIndex = page,
            NormalizedX = x,
            NormalizedY = y,
            NormalizedWidth = 0.5f,
            NormalizedHeight = 0.05f,
            Text = text ?? string.Empty,
            CreatedAt = new DateTime(2026, 1, 1)
        };
    }
}
