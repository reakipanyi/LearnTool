using FluentAssertions;
using LearningAssistant.Services.Audio;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Media;

public class PlaybackRangeStateTests
{
    [Fact]
    public void 未设停止点_不停止()
    {
        var state = new PlaybackRangeState();
        state.IsArmed.Should().BeFalse();
        state.ShouldStop(TimeSpan.FromSeconds(5), out _).Should().BeFalse();
    }

    [Fact]
    public void 未到停止点_不停止()
    {
        var state = new PlaybackRangeState();
        state.Arm(TimeSpan.FromSeconds(4));

        state.ShouldStop(TimeSpan.FromSeconds(3), out _).Should().BeFalse();
    }

    [Fact]
    public void 到达或超过停止点_返回对齐目标()
    {
        var state = new PlaybackRangeState();
        state.Arm(TimeSpan.FromSeconds(4));

        state.ShouldStop(TimeSpan.FromSeconds(4), out var snapTo).Should().BeTrue();
        snapTo.Should().Be(TimeSpan.FromSeconds(4));

        state.ShouldStop(TimeSpan.FromSeconds(4.2), out snapTo).Should().BeTrue();
        snapTo.Should().Be(TimeSpan.FromSeconds(4));
    }

    [Fact]
    public void 清除后不再触发()
    {
        var state = new PlaybackRangeState();
        state.Arm(TimeSpan.FromSeconds(2));
        state.Disarm();

        state.IsArmed.Should().BeFalse();
        state.ShouldStop(TimeSpan.FromSeconds(5), out _).Should().BeFalse();
    }

    [Fact]
    public void 重复设置停止点_以后一次为准()
    {
        var state = new PlaybackRangeState();
        state.Arm(TimeSpan.FromSeconds(2));
        state.Arm(TimeSpan.FromSeconds(5));

        state.ShouldStop(TimeSpan.FromSeconds(3), out _).Should().BeFalse();
        state.ShouldStop(TimeSpan.FromSeconds(5), out var snapTo).Should().BeTrue();
        snapTo.Should().Be(TimeSpan.FromSeconds(5));
    }
}
