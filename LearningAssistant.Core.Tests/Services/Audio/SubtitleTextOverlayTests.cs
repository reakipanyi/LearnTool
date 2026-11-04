using FluentAssertions;
using LearningAssistant.Abstractions.Media;
using LearningAssistant.Models.Audio;
using LearningAssistant.Services.Audio;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Audio;

public class SubtitleTextOverlayTests
{
    private static AudioSentenceSpan Span(int startMs, int endMs, string? text = null)
        => new()
        {
            StartMs = startMs,
            EndMs = endMs,
            BoundarySource = "vad",
            Text = text,
            TextSource = text == null ? null : "shadowing"
        };

    private static SubtitleCue Cue(int startMs, int endMs, string text)
        => new()
        {
            StartTime = TimeSpan.FromMilliseconds(startMs),
            EndTime = TimeSpan.FromMilliseconds(endMs),
            Text = text
        };

    [Fact]
    public void 无字幕_原样返回()
    {
        var spans = new[] { Span(0, 3000) };

        var result = SubtitleTextOverlay.Apply(spans, null);

        result.Should().BeEquivalentTo(spans);
        result[0].Text.Should().BeNull();
    }

    [Fact]
    public void 字幕落在句内_叠加文本()
    {
        var result = SubtitleTextOverlay.Apply(
            new[] { Span(1000, 5000) },
            new[] { Cue(1200, 4200, "hello world") });

        result[0].Text.Should().Be("hello world");
        result[0].TextSource.Should().Be("subtitle");
    }

    [Fact]
    public void 已有切片文本_不被字幕覆盖()
    {
        var result = SubtitleTextOverlay.Apply(
            new[] { Span(1000, 5000, "shadowing text") },
            new[] { Cue(1200, 4200, "subtitle text") });

        result[0].Text.Should().Be("shadowing text");
        result[0].TextSource.Should().Be("shadowing");
    }

    [Fact]
    public void 长句内多条字幕_最多拼接两行()
    {
        var result = SubtitleTextOverlay.Apply(
            new[] { Span(0, 12000) },
            new[]
            {
                Cue(500, 3000, "one"),
                Cue(3500, 6000, "two"),
                Cue(6500, 9000, "three")
            });

        result[0].Text.Should().Be("one two");
        result[0].TextSource.Should().Be("subtitle");
    }

    [Fact]
    public void 字幕跨句且主体在下一句_不叠加到前一句()
    {
        // 字幕 9000–13000 的中点 11000 落在下一句，前一句不应拿走这条文本
        var result = SubtitleTextOverlay.Apply(
            new[] { Span(0, 10000) },
            new[] { Cue(9000, 13000, "next sentence") });

        result[0].Text.Should().BeNull();
    }

    [Fact]
    public void 长字幕覆盖短句_即使中点在外也叠加()
    {
        var result = SubtitleTextOverlay.Apply(
            new[] { Span(10000, 12000) },
            new[] { Cue(9000, 30000, "a long caption") });

        result[0].Text.Should().Be("a long caption");
    }

    [Fact]
    public void 重复字幕文本_只保留一条()
    {
        var result = SubtitleTextOverlay.Apply(
            new[] { Span(0, 8000) },
            new[] { Cue(500, 3000, "same"), Cue(3500, 6000, "same") });

        result[0].Text.Should().Be("same");
    }

    [Fact]
    public void 空文本字幕_被忽略()
    {
        var result = SubtitleTextOverlay.Apply(
            new[] { Span(0, 5000) },
            new[] { Cue(500, 3000, "   ") });

        result[0].Text.Should().BeNull();
    }
}
