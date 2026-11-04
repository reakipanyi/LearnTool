using LearningAssistant.Models.Audio;
using LearningAssistant.Services.Audio;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Audio;

public class AudioPageAnchorBuilderTests
{
    private static AudioSentenceSpan Span(int startMs, int endMs, int? page)
        => new() { StartMs = startMs, EndMs = endMs, PageIndex = page };

    [Fact]
    public void Build_取每页最早切片并减留白()
    {
        var anchors = AudioPageAnchorBuilder.Build(
            60000,
            new[] { Span(2000, 5000, 2), Span(1000, 1500, 2), Span(6000, 7000, 3) },
            null,
            new AudioTimelineOptions { PaddingMs = 100 });

        Assert.Equal(2, anchors.Count);
        Assert.Equal(2, anchors[0].PageIndex);
        Assert.Equal(900, anchors[0].StartMs);
        Assert.True(anchors[0].IsStartEstimated);
        Assert.Equal(3, anchors[1].PageIndex);
        Assert.Equal(5900, anchors[1].StartMs);
    }

    [Fact]
    public void Build_无来源的页不生成锚点()
    {
        var anchors = AudioPageAnchorBuilder.Build(
            60000,
            new[] { Span(1000, 3000, 0), Span(9000, 12000, 2) },
            null,
            new AudioTimelineOptions());

        // 第 1 页没有切片来源，不能凭空生成
        Assert.Equal(new[] { 0, 2 }, anchors.Select(a => a.PageIndex).ToArray());
    }

    [Fact]
    public void Build_手动锚点覆盖切片()
    {
        var manual = new[]
        {
            new AudioPageAnchor { PageIndex = 2, StartMs = 500, Source = "manual" }
        };

        var anchors = AudioPageAnchorBuilder.Build(
            60000,
            new[] { Span(2000, 5000, 2) },
            manual,
            new AudioTimelineOptions { PaddingMs = 100 });

        Assert.Single(anchors);
        Assert.Equal(500, anchors[0].StartMs);
        Assert.Equal("manual", anchors[0].Source);
        Assert.False(anchors[0].IsStartEstimated);
        Assert.Equal(1.0, anchors[0].Confidence);
    }

    [Fact]
    public void Build_默认不插值()
    {
        var anchors = AudioPageAnchorBuilder.Build(
            60000,
            new[] { Span(1000, 3000, 0), Span(11000, 13000, 3) },
            null,
            new AudioTimelineOptions { PaddingMs = 0 },
            interpolateMissing: false);

        Assert.Equal(2, anchors.Count);
        Assert.DoesNotContain(anchors, a => a.Source == AudioPageAnchorBuilder.InterpolatedSource);
    }

    [Fact]
    public void Build_显式开启插值时生成低置信灰层()
    {
        var anchors = AudioPageAnchorBuilder.Build(
            60000,
            new[] { Span(1000, 3000, 0), Span(11000, 13000, 3) },
            null,
            new AudioTimelineOptions { PaddingMs = 0 },
            interpolateMissing: true);

        Assert.Equal(4, anchors.Count);
        var interpolated = anchors.Where(a => a.Source == AudioPageAnchorBuilder.InterpolatedSource).ToList();
        Assert.Equal(2, interpolated.Count);
        Assert.All(interpolated, a => Assert.True(a.Confidence < 0.5));
        Assert.All(interpolated, a => Assert.True(a.IsStartEstimated));
        Assert.Equal(new[] { 0, 1, 2, 3 }, anchors.Select(a => a.PageIndex).ToArray());
    }

    [Fact]
    public void Build_相邻页倒序时丢弃后者()
    {
        var manual = new[]
        {
            new AudioPageAnchor { PageIndex = 5, StartMs = 3000, Source = "manual" },
            new AudioPageAnchor { PageIndex = 6, StartMs = 2000, Source = "manual" }
        };

        var anchors = AudioPageAnchorBuilder.Build(60000, null, manual, new AudioTimelineOptions());

        Assert.Single(anchors);
        Assert.Equal(5, anchors[0].PageIndex);
    }

    [Fact]
    public void Build_时长无效时返回空()
    {
        var anchors = AudioPageAnchorBuilder.Build(
            0,
            new[] { Span(1000, 3000, 0) },
            null,
            new AudioTimelineOptions());

        Assert.Empty(anchors);
    }

    [Fact]
    public void Build_越界锚点被丢弃()
    {
        var manual = new[]
        {
            new AudioPageAnchor { PageIndex = 0, StartMs = 90000, Source = "manual" }
        };

        var anchors = AudioPageAnchorBuilder.Build(60000, null, manual, new AudioTimelineOptions());

        Assert.Empty(anchors);
    }
}
