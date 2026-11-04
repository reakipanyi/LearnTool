using FluentAssertions;
using LearningAssistant.Models.Audio;
using LearningAssistant.Services.Audio;
using LearningAssistant.Services.Shadowing;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Audio;

public class SentenceBoundaryBuilderTests
{
    private static AudioTimelineOptions Options(
        int sentenceGapMs = 350,
        int fineGapMs = 150,
        int minSpeechMs = 150,
        int minSentenceMs = 700,
        int maxSentenceMs = 8000,
        int paddingMs = 0)
        => new()
        {
            SentenceGapMs = sentenceGapMs,
            FineGapMs = fineGapMs,
            MinSpeechMs = minSpeechMs,
            MinSentenceMs = minSentenceMs,
            MaxSentenceMs = maxSentenceMs,
            PaddingMs = paddingMs
        };

    private static VadSegment Seg(int startMs, int endMs) => new(startMs, endMs);

    [Fact]
    public void 三段语音_350毫秒间隔_输出三句()
    {
        var result = SentenceBoundaryBuilder.Build(
            new[] { Seg(0, 2000), Seg(2350, 4000), Seg(4350, 6000) },
            Array.Empty<VadSegment>(),
            6000,
            Options());

        result.Should().HaveCount(3);
        result.Select(s => (s.StartMs, s.EndMs)).Should().Equal((0, 2000), (2350, 4000), (4350, 6000));
    }

    [Fact]
    public void 三段语音_200毫秒间隔_合并为一句()
    {
        var result = SentenceBoundaryBuilder.Build(
            new[] { Seg(0, 2000), Seg(2200, 4000), Seg(4200, 6000) },
            Array.Empty<VadSegment>(),
            6000,
            Options());

        result.Should().HaveCount(1);
        result[0].StartMs.Should().Be(0);
        result[0].EndMs.Should().Be(6000);
    }

    [Fact]
    public void 连续十五秒_有细遍边界_拆成不超过八秒()
    {
        var result = SentenceBoundaryBuilder.Build(
            new[] { Seg(0, 15000) },
            new[] { Seg(0, 7000), Seg(7300, 15000) },
            15000,
            Options());

        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.EndMs - s.StartMs <= 8000);
    }

    [Fact]
    public void 连续十五秒_无细遍边界_八秒硬切并降置信()
    {
        var result = SentenceBoundaryBuilder.Build(
            new[] { Seg(0, 15000) },
            Array.Empty<VadSegment>(),
            15000,
            Options());

        result.Should().HaveCount(2);
        result[0].StartMs.Should().Be(0);
        result[0].EndMs.Should().Be(8000);
        result[0].BoundarySource.Should().Be("vad-hard");
        result[0].Confidence.Should().Be(0.9);
        result[1].EndMs.Should().Be(15000);
    }

    [Fact]
    public void 四百毫秒短应答_不丢失_并入间隔更小的邻句()
    {
        var result = SentenceBoundaryBuilder.Build(
            new[] { Seg(0, 2000), Seg(2500, 2900), Seg(3400, 5400) },
            Array.Empty<VadSegment>(),
            5400,
            Options());

        result.Should().HaveCount(2);
        result[0].EndMs.Should().Be(2900);
        result[1].StartMs.Should().Be(3400);
    }

    [Fact]
    public void 一百五十毫秒极短语音_不丢失()
    {
        var result = SentenceBoundaryBuilder.Build(
            new[] { Seg(0, 2000), Seg(2350, 2500), Seg(2850, 4850) },
            Array.Empty<VadSegment>(),
            4850,
            Options());

        result.Should().HaveCount(2);
        result[0].EndMs.Should().Be(2500);
        result[1].StartMs.Should().Be(2850);
    }

    [Fact]
    public void 留白后相邻区间重叠_消重叠且单调递增()
    {
        var result = SentenceBoundaryBuilder.Build(
            new[] { Seg(0, 1000), Seg(1100, 2000) },
            Array.Empty<VadSegment>(),
            2000,
            Options(sentenceGapMs: 100, minSentenceMs: 50, maxSentenceMs: 5000, paddingMs: 100));

        result.Should().HaveCount(2);
        result[0].EndMs.Should().Be(result[1].StartMs);
        result[0].StartMs.Should().BeLessThan(result[0].EndMs);
        result[1].StartMs.Should().BeLessThan(result[1].EndMs);
    }

    [Fact]
    public void 全静音_兜底单句()
    {
        var result = SentenceBoundaryBuilder.Build(
            Array.Empty<VadSegment>(),
            Array.Empty<VadSegment>(),
            5000,
            Options());

        result.Should().HaveCount(1);
        result[0].StartMs.Should().Be(0);
        result[0].EndMs.Should().Be(5000);
        result[0].Confidence.Should().BeLessThan(0.5);
    }

    [Fact]
    public void 零时长_返回空()
    {
        SentenceBoundaryBuilder.Build(
            Array.Empty<VadSegment>(),
            Array.Empty<VadSegment>(),
            0,
            Options()).Should().BeEmpty();
    }

    [Fact]
    public void 单句_兜底为整轨单句()
    {
        var result = SentenceBoundaryBuilder.Build(
            new[] { Seg(1000, 4000) },
            Array.Empty<VadSegment>(),
            5000,
            Options());

        result.Should().HaveCount(1);
        result[0].StartMs.Should().Be(0);
        result[0].EndMs.Should().Be(5000);
        result[0].BoundarySource.Should().Be("vad-fallback");
    }

    [Fact]
    public void 输入乱序重复越界_输出有序且无重叠()
    {
        var result = SentenceBoundaryBuilder.Build(
            new[] { Seg(3000, 5000), Seg(0, 2000), Seg(4000, 6000), Seg(-100, 100) },
            Array.Empty<VadSegment>(),
            6000,
            Options());

        result.Should().HaveCount(2);
        result[0].StartMs.Should().Be(0);
        result[0].EndMs.Should().Be(2000);
        result[1].StartMs.Should().Be(3000);
        result[1].EndMs.Should().Be(6000);
        for (int i = 1; i < result.Count; i++)
            result[i].StartMs.Should().BeGreaterOrEqualTo(result[i - 1].EndMs);
    }

    [Fact]
    public void 无留白时_重复Build幂等()
    {
        var options = Options();
        var first = SentenceBoundaryBuilder.Build(
            new[] { Seg(0, 2000), Seg(2350, 4000), Seg(4350, 6000) },
            Array.Empty<VadSegment>(),
            6000,
            options);

        var second = SentenceBoundaryBuilder.Build(
            first.Select(s => Seg(s.StartMs, s.EndMs)).ToArray(),
            Array.Empty<VadSegment>(),
            6000,
            options);

        second.Select(s => (s.StartMs, s.EndMs))
            .Should().Equal(first.Select(s => (s.StartMs, s.EndMs)));
    }

    [Fact]
    public void 句长分布不佳_自评降低置信度()
    {
        var result = SentenceBoundaryBuilder.Build(
            new[]
            {
                Seg(0, 600), Seg(950, 1550), Seg(1900, 2500),
                Seg(2850, 3450), Seg(3800, 4400)
            },
            Array.Empty<VadSegment>(),
            4400,
            Options(minSentenceMs: 400));

        result.Should().HaveCount(5);
        result.Should().OnlyContain(s => s.Confidence <= 0.8);
    }
}
