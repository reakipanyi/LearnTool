using FluentAssertions;
using LearningAssistant.Models.Audio;
using LearningAssistant.Services.Audio;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Audio;

/// <summary>句子刻度抽稀（设计 §14.1 MarkerBinnerTests）。</summary>
public class MarkerBinnerTests
{
    private static AudioSentenceSpan Span(int startMs, int endMs) =>
        new() { StartMs = startMs, EndMs = endMs };

    [Fact]
    public void Build_KeepsOnlyRepresentativeSentencePerColumn()
    {
        var sentences = new[]
        {
            Span(0, 200),
            Span(200, 400),
            Span(240, 500),
            Span(800, 1000)
        };

        var buckets = MarkerBinner.Build(sentences, columns: 4, durationMs: 1000);

        buckets.Should().HaveCount(4);
        buckets[0].Should().Be(new MarkerBucket(0, 1));
        buckets[1].Should().Be(new MarkerBucket(1, 2));
        buckets[2].IsEmpty.Should().BeTrue();
        buckets[3].Should().Be(new MarkerBucket(3, 1));
    }

    [Fact]
    public void ForcedIndices_AlwaysKeepFirstLastCurrentAndHover()
    {
        MarkerBinner.ForcedIndices(count: 5, currentIndex: 2, hoverIndex: 3)
            .Should().Equal(0, 4, 2, 3);

        MarkerBinner.ForcedIndices(count: 1, currentIndex: -1, hoverIndex: -1)
            .Should().Equal(0);

        MarkerBinner.ForcedIndices(count: 0, currentIndex: 0, hoverIndex: 0)
            .Should().BeEmpty();
    }

    [Fact]
    public void Build_RebuildsWhenWidthChanges()
    {
        var sentences = new[] { Span(0, 100), Span(250, 400), Span(500, 600) };

        var narrow = MarkerBinner.Build(sentences, columns: 4, durationMs: 1000);
        var wide = MarkerBinner.Build(sentences, columns: 8, durationMs: 1000);

        narrow.Should().HaveCount(4);
        wide.Should().HaveCount(8);
        MarkerBinner.ColumnOf(250, columns: 4, durationMs: 1000).Should().Be(1);
        MarkerBinner.ColumnOf(250, columns: 8, durationMs: 1000).Should().Be(2);
    }

    [Fact]
    public void Build_TwelveThousandSentences_BucketCountEqualsVisibleColumns()
    {
        var sentences = Enumerable.Range(0, 12000)
            .Select(i => Span(i * 10, i * 10 + 10))
            .ToArray();

        var buckets = MarkerBinner.Build(sentences, columns: 300, durationMs: 120000);

        buckets.Should().HaveCount(300);
    }

    [Fact]
    public void Build_EmptyOrInvalidInput_ReturnsEmptyBuckets()
    {
        MarkerBinner.Build(null, columns: 3, durationMs: 1000).Should().HaveCount(3);
        MarkerBinner.Build(Array.Empty<AudioSentenceSpan>(), columns: 3, durationMs: 1000).Should().HaveCount(3);
        MarkerBinner.Build(new[] { Span(0, 10) }, columns: 3, durationMs: 0).Should().HaveCount(3);
        MarkerBinner.Build(new[] { Span(0, 10) }, columns: 0, durationMs: 1000).Should().BeEmpty();
    }
}
