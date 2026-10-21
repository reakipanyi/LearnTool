using LearningAssistant.Services.Shadowing;
using Xunit;

namespace LearningAssistant.Core.Tests;

public class SentenceSegmenterTests
{
    [Fact]
    public void Segment_SplitsTwoSimpleSentences()
    {
        var sentences = SentenceSegmenter.Segment("The cat sat on the mat. The dog ran in the yard.");

        Assert.Equal(2, sentences.Count);
        Assert.Equal("The cat sat on the mat.", sentences[0].Text);
        Assert.Equal("The dog ran in the yard.", sentences[1].Text);
    }

    [Fact]
    public void Segment_JoinsHyphenatedLineBreak()
    {
        var sentences = SentenceSegmenter.Segment("The inter-\nesting story was quite good.");

        Assert.Single(sentences);
        Assert.Contains("interesting", sentences[0].Text);
    }

    [Fact]
    public void Segment_DoesNotSplitAfterAbbreviation()
    {
        var sentences = SentenceSegmenter.Segment("Dr. Smith arrived early. He was tired.");

        Assert.Equal(2, sentences.Count);
        Assert.StartsWith("Dr. Smith", sentences[0].Text);
    }

    [Fact]
    public void Segment_DoesNotSplitAtDecimalPoint()
    {
        var sentences = SentenceSegmenter.Segment("The value is 3.14 meters. It is precise.");

        Assert.Equal(2, sentences.Count);
        Assert.Contains("3.14", sentences[0].Text);
    }

    [Fact]
    public void Segment_DoesNotSplitInsideUrl()
    {
        var sentences = SentenceSegmenter.Segment("Visit https://example.com for details. Then continue reading.");

        Assert.Equal(2, sentences.Count);
        Assert.Contains("https://example.com", sentences[0].Text);
    }

    [Fact]
    public void Segment_ProtectsNameInitials()
    {
        var sentences = SentenceSegmenter.Segment("J. K. Rowling wrote the book. It sold well.");

        Assert.Equal(2, sentences.Count);
        Assert.StartsWith("J. K. Rowling", sentences[0].Text);
    }

    [Fact]
    public void Segment_MergesTooShortFragment()
    {
        var sentences = SentenceSegmenter.Segment("Yes. I completely agree with you.");

        Assert.Single(sentences);
        Assert.Equal("Yes. I completely agree with you.", sentences[0].Text);
    }

    [Fact]
    public void Segment_FiltersPageNumbersAndFooters()
    {
        var sentences = SentenceSegmenter.Segment("Page 3 of 10\nActual content starts here today.\n42");

        Assert.Single(sentences);
        Assert.Equal("Actual content starts here today.", sentences[0].Text);
    }

    [Fact]
    public void Segment_SplitsOverlongSentenceAtComma()
    {
        string raw = string.Join(", ", Enumerable.Repeat("some meaningful words", 20)) + ".";
        var sentences = SentenceSegmenter.Segment(raw);

        Assert.True(sentences.Count > 1);
        Assert.All(sentences, s => Assert.True(s.Length <= SentenceSegmenter.MaxSentenceLength));
    }

    [Fact]
    public void Segment_ProducesStableIdsForUnchangedSentences()
    {
        string first = "The first sentence stays the same. The second sentence is here.";
        string second = "The first sentence stays the same. A completely different second sentence appears.";

        var a = SentenceSegmenter.Segment(first);
        var b = SentenceSegmenter.Segment(second);

        Assert.Equal(a[0].Id, b[0].Id);
        Assert.NotEqual(a[1].Id, b[1].Id);
    }

    [Fact]
    public void Segment_LengthMatchesTextAndIndexIsSequential()
    {
        var sentences = SentenceSegmenter.Segment("Alpha beta gamma. Delta epsilon zeta. Eta theta iota.");

        Assert.Equal(3, sentences.Count);
        for (int i = 0; i < sentences.Count; i++)
        {
            Assert.Equal(i, sentences[i].Index);
            Assert.Equal(sentences[i].Length, sentences[i].Text.Length);
        }
    }

    [Fact]
    public void Segment_EmptyInputReturnsEmptyList()
    {
        Assert.Empty(SentenceSegmenter.Segment(null));
        Assert.Empty(SentenceSegmenter.Segment("   \n  "));
    }
}
