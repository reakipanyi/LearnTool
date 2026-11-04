using FluentAssertions;
using LearningAssistant.Models.Audio;
using LearningAssistant.Models.Shadowing;
using LearningAssistant.Services.Audio;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Audio;

public class ShadowingBoundaryAlignmentTests
{
    private const string TrackPath = "audio/lesson.mp3";
    private const string Fingerprint = "1024|123456|600000";

    private static AudioSentenceSpan Span(int startMs, int endMs)
        => new() { StartMs = startMs, EndMs = endMs, BoundarySource = "manual" };

    private static ShadowingSegmentManifest Manifest(params ShadowingSegment[] segments)
        => new()
        {
            PageIndex = 0,
            Source = new ShadowingSegmentSource
            {
                TrackPath = TrackPath,
                Fingerprint = Fingerprint,
                DurationMs = 600000
            },
            Segments = segments.ToList()
        };

    private static ShadowingSegment Segment(string id, int startMs, int endMs, bool withTake = false)
        => new()
        {
            Id = id,
            StartMs = startMs,
            EndMs = endMs,
            SlicePath = $"slices/{id}.wav",
            Takes = withTake
                ? new List<ShadowingTake> { new() { Id = id + "-take", Path = $"takes/{id}.wav" } }
                : new List<ShadowingTake>()
        };

    [Fact]
    public void 按中点吸附到所属句()
    {
        var manifest = Manifest(Segment("sg1", 1000, 4000));
        var plan = ShadowingBoundaryAlignment.Plan(
            new[] { manifest },
            new[] { Span(0, 2500), Span(2500, 6000) },
            TrackPath,
            Fingerprint);

        plan.Changes.Should().ContainSingle();
        var change = plan.Changes[0];
        change.SegmentId.Should().Be("sg1");
        change.NewStartMs.Should().Be(2500);
        change.NewEndMs.Should().Be(6000);
        change.MaxShiftMs.Should().Be(2000);
        change.ExceedsShift.Should().BeTrue();
    }

    [Fact]
    public void 曲目指纹不一致_整份清单被忽略()
    {
        var manifest = Manifest(Segment("sg1", 1000, 4000));
        manifest.Source.Fingerprint = "其他指纹";

        var plan = ShadowingBoundaryAlignment.Plan(
            new[] { manifest },
            new[] { Span(0, 5000) },
            TrackPath,
            Fingerprint);

        plan.Changes.Should().BeEmpty();
    }

    [Fact]
    public void 多段命中同一句_全部跳过避免重叠()
    {
        var manifest = Manifest(
            Segment("sg1", 1000, 3000),
            Segment("sg2", 3000, 5000));

        var plan = ShadowingBoundaryAlignment.Plan(
            new[] { manifest },
            new[] { Span(0, 8000) },
            TrackPath,
            Fingerprint);

        plan.Changes.Should().BeEmpty();
        plan.SkippedConflicts.Should().Be(2);
    }

    [Fact]
    public void 边界已一致_不产生变更()
    {
        var manifest = Manifest(Segment("sg1", 0, 5000));

        var plan = ShadowingBoundaryAlignment.Plan(
            new[] { manifest },
            new[] { Span(0, 5000) },
            TrackPath,
            Fingerprint);

        plan.Changes.Should().BeEmpty();
        plan.SkippedConflicts.Should().Be(0);
    }

    [Fact]
    public void 已有录音的段_标记需要提示()
    {
        var manifest = Manifest(Segment("sg1", 1000, 4000, withTake: true));

        var plan = ShadowingBoundaryAlignment.Plan(
            new[] { manifest },
            new[] { Span(900, 4100) },
            TrackPath,
            Fingerprint);

        plan.Changes.Should().ContainSingle();
        plan.Changes[0].HasTakes.Should().BeTrue();
        plan.Changes[0].ExceedsShift.Should().BeFalse();
    }

    [Fact]
    public void 位移未超过阈值_不标记()
    {
        var manifest = Manifest(Segment("sg1", 1000, 4000));

        var plan = ShadowingBoundaryAlignment.Plan(
            new[] { manifest },
            new[] { Span(800, 4200) },
            TrackPath,
            Fingerprint);

        plan.Changes.Should().ContainSingle();
        plan.Changes[0].MaxShiftMs.Should().Be(200);
        plan.Changes[0].ExceedsShift.Should().BeFalse();
    }

    [Fact]
    public void 空输入_返回空计划()
    {
        ShadowingBoundaryAlignment.Plan(null, null, TrackPath, Fingerprint)
            .Changes.Should().BeEmpty();
    }
}
