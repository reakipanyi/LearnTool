using FluentAssertions;
using LearningAssistant.Models.Audio;
using LearningAssistant.Models.Shadowing;
using LearningAssistant.Services.Audio;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Audio;

public class ShadowingSliceAppendPlannerTests
{
    private static AudioSentenceSpan Span(int start, int end, string? text = null)
        => new() { StartMs = start, EndMs = end, Text = text };

    private static ShadowingSegment Segment(int start, int end, bool hasTake = false)
    {
        var segment = new ShadowingSegment { Id = "s", StartMs = start, EndMs = end };
        if (hasTake)
            segment.Takes.Add(new ShadowingTake { Id = "t", Path = "takes/t.wav" });
        return segment;
    }

    [Fact]
    public void 无既有切片_全部句子转为新切片()
    {
        var plan = ShadowingSliceAppendPlanner.Plan(
            new[] { Span(0, 2000, "一"), Span(2000, 4000, "二") },
            Array.Empty<ShadowingSegment>(),
            5000);

        plan.Changes.Should().HaveCount(2);
        plan.Changes[0].Text.Should().Be("一");
        plan.SkippedOverlap.Should().Be(0);
    }

    [Fact]
    public void 与既有切片重叠的句子_跳过而不破坏录音()
    {
        var plan = ShadowingSliceAppendPlanner.Plan(
            new[] { Span(0, 2000), Span(2500, 4500) },
            new[] { Segment(1000, 3000, hasTake: true) },
            5000);

        plan.Changes.Should().HaveCount(0);
        plan.SkippedOverlap.Should().Be(2);
    }

    [Fact]
    public void 句子超出音频范围_被裁剪与丢弃()
    {
        var plan = ShadowingSliceAppendPlanner.Plan(
            new[] { Span(-500, 1000), Span(4000, 6000) },
            Array.Empty<ShadowingSegment>(),
            5000);

        plan.Changes.Should().HaveCount(2);
        plan.Changes[0].StartMs.Should().Be(0);
        plan.Changes[0].EndMs.Should().Be(1000);
        plan.Changes[1].EndMs.Should().Be(5000);
    }

    [Fact]
    public void 空区间句子_计入无效并跳过()
    {
        var plan = ShadowingSliceAppendPlanner.Plan(
            new[] { Span(3000, 3000), Span(6000, 8000) },
            Array.Empty<ShadowingSegment>(),
            5000);

        plan.SkippedInvalid.Should().Be(2);
        plan.Changes.Should().BeEmpty();
    }

    [Fact]
    public void 句子彼此重叠_后者跳过()
    {
        var plan = ShadowingSliceAppendPlanner.Plan(
            new[] { Span(0, 3000), Span(2000, 4000) },
            Array.Empty<ShadowingSegment>(),
            5000);

        plan.Changes.Should().HaveCount(1);
        plan.SkippedOverlap.Should().Be(1);
    }
}
