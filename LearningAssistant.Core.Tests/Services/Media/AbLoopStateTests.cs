using FluentAssertions;
using LearningAssistant.Services.Media;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Media;

public class AbLoopStateTests
{
    [Fact]
    public void 未设边界_不回跳()
    {
        var state = new AbLoopState();
        state.ShouldLoopBack(TimeSpan.FromSeconds(5), out _).Should().BeFalse();
    }

    [Fact]
    public void 位置小于B_不回跳()
    {
        var state = new AbLoopState { A = TimeSpan.FromSeconds(2), B = TimeSpan.FromSeconds(4) };
        state.ShouldLoopBack(TimeSpan.FromSeconds(3), out _).Should().BeFalse();
    }

    [Fact]
    public void 位置大于等于B_回跳到A()
    {
        var state = new AbLoopState { A = TimeSpan.FromSeconds(2), B = TimeSpan.FromSeconds(4) };
        state.ShouldLoopBack(TimeSpan.FromSeconds(4), out var target).Should().BeTrue();
        target.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void B小于等于A_视为未启用()
    {
        var state = new AbLoopState { A = TimeSpan.FromSeconds(4), B = TimeSpan.FromSeconds(2) };
        state.ShouldLoopBack(TimeSpan.FromSeconds(5), out _).Should().BeFalse();
    }
}