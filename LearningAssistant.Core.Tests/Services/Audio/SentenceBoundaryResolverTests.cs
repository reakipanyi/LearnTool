using FluentAssertions;
using LearningAssistant.Models.Audio;
using LearningAssistant.Services.Audio;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Audio;

public class SentenceBoundaryResolverTests
{
    private static AudioTimelineOptions Options(int paddingMs = 0, int minSpeechMs = 150)
        => new()
        {
            PaddingMs = paddingMs,
            MinSpeechMs = minSpeechMs
        };

    private static AudioSentenceSpan Span(
        int startMs,
        int endMs,
        string source = "vad",
        int? pageIndex = null,
        string? segmentId = null)
        => new()
        {
            StartMs = startMs,
            EndMs = endMs,
            BoundarySource = source,
            PageIndex = pageIndex,
            SegmentId = segmentId,
            Confidence = 1.0
        };

    [Fact]
    public void 手动修正覆盖VAD_重叠处取高()
    {
        var result = SentenceBoundaryResolver.Resolve(
            3000,
            Options(),
            new SentenceBoundaryLayer("manual", SentenceBoundaryResolver.ManualPriority,
                new[] { Span(1000, 2000, "manual") }),
            new SentenceBoundaryLayer("vad", SentenceBoundaryResolver.VadPriority,
                new[] { Span(0, 3000) }));

        result.Should().HaveCount(3);
        result.Select(s => (s.StartMs, s.EndMs)).Should().Equal((0, 1000), (1000, 2000), (2000, 3000));
        result[1].BoundarySource.Should().Be("manual");
    }

    [Fact]
    public void 切片优先于字幕与VAD()
    {
        var result = SentenceBoundaryResolver.Resolve(
            5000,
            Options(),
            new SentenceBoundaryLayer("shadowing", SentenceBoundaryResolver.ShadowingPriority,
                new[] { Span(0, 2000, "shadowing", pageIndex: 0, segmentId: "sg-1") }),
            new SentenceBoundaryLayer("subtitle", SentenceBoundaryResolver.SubtitlePriority,
                new[] { Span(0, 2500, "subtitle") }),
            new SentenceBoundaryLayer("vad", SentenceBoundaryResolver.VadPriority,
                new[] { Span(0, 5000) }));

        result[0].BoundarySource.Should().Be("shadowing");
        result[0].SegmentId.Should().Be("sg-1");
        result[0].PageIndex.Should().Be(0);
        result.Should().OnlyContain(s => s.StartMs >= 0 && s.EndMs <= 5000);
    }

    [Fact]
    public void 切片只覆盖部分区间_空洞用VAD填充()
    {
        var result = SentenceBoundaryResolver.Resolve(
            8000,
            Options(),
            new SentenceBoundaryLayer("shadowing", SentenceBoundaryResolver.ShadowingPriority,
                new[] { Span(0, 2000, "shadowing"), Span(4000, 6000, "shadowing") }),
            new SentenceBoundaryLayer("vad", SentenceBoundaryResolver.VadPriority,
                new[] { Span(0, 8000) }));

        result.Should().HaveCount(4);
        result.Select(s => (s.StartMs, s.EndMs))
            .Should().Equal((0, 2000), (2000, 4000), (4000, 6000), (6000, 8000));
        result.Select(s => s.BoundarySource)
            .Should().Equal("shadowing", "vad", "shadowing", "vad");
    }

    [Fact]
    public void 低优先级整段被高优先级完全覆盖时被丢弃()
    {
        var result = SentenceBoundaryResolver.Resolve(
            2000,
            Options(),
            new SentenceBoundaryLayer("shadowing", SentenceBoundaryResolver.ShadowingPriority,
                new[] { Span(0, 2000, "shadowing") }),
            new SentenceBoundaryLayer("vad", SentenceBoundaryResolver.VadPriority,
                new[] { Span(0, 2000) }));

        result.Should().HaveCount(1);
        result[0].BoundarySource.Should().Be("shadowing");
    }

    [Fact]
    public void 无任何候选_兜底整轨单句()
    {
        var result = SentenceBoundaryResolver.Resolve(5000, Options());

        result.Should().HaveCount(1);
        result[0].StartMs.Should().Be(0);
        result[0].EndMs.Should().Be(5000);
        result[0].Confidence.Should().BeLessThan(0.5);
    }

    [Fact]
    public void 输入越界与乱序_输出恒有序且不越界()
    {
        var result = SentenceBoundaryResolver.Resolve(
            5000,
            Options(),
            new SentenceBoundaryLayer("vad", SentenceBoundaryResolver.VadPriority,
                new[] { Span(3000, 6000), Span(-100, 1000), Span(2000, 3000) }));

        result.Should().OnlyContain(s => s.StartMs >= 0 && s.EndMs <= 5000);
        for (int i = 1; i < result.Count; i++)
            result[i].StartMs.Should().BeGreaterOrEqualTo(result[i - 1].EndMs);
    }

    [Fact]
    public void 跟读切片指纹或路径不匹配_返回false()
    {
        SentenceBoundaryResolver.MatchesTrack("a.mp3", "1|2|3", @"C:\book\a.mp3", "1|2|3").Should().BeTrue();
        SentenceBoundaryResolver.MatchesTrack("a.mp3", "1|2|3", @"C:\book\a.mp3", "9|2|3").Should().BeFalse();
        SentenceBoundaryResolver.MatchesTrack("a.mp3", "1|2|3", @"C:\book\b.mp3", "1|2|3").Should().BeFalse();
        SentenceBoundaryResolver.MatchesTrack("a.mp3", "", @"C:\book\a.mp3", "1|2|3").Should().BeFalse();
    }
}
