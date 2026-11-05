using FluentAssertions;
using LearningAssistant.Models.Audio;
using LearningAssistant.Services.Audio;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Audio;

public class MarkerHitTesterTests
{
    private static AudioSentenceSpan Span(int start, int end, int index = 0)
        => new() { StartMs = start, EndMs = end, Index = index };

    private static readonly AudioSentenceSpan[] Three =
    {
        Span(0, 4000, 1),
        Span(5000, 9000, 2),
        Span(10000, 14000, 3)
    };

    [Fact]
    public void 空列表返回空()
    {
        MarkerHitTester.Nearest(null, 0).Should().BeNull();
        MarkerHitTester.Nearest(Array.Empty<AudioSentenceSpan>(), 500).Should().BeNull();
    }

    [Fact]
    public void 落在句内直接命中本句()
    {
        MarkerHitTester.Nearest(Three, 2000)!.Index.Should().Be(1);
        MarkerHitTester.Nearest(Three, 7000)!.Index.Should().Be(2);
        MarkerHitTester.Nearest(Three, 12000)!.Index.Should().Be(3);
    }

    [Fact]
    public void 落在空白处取更近的一侧()
    {
        MarkerHitTester.Nearest(Three, 4200)!.Index.Should().Be(1);
        MarkerHitTester.Nearest(Three, 4900)!.Index.Should().Be(2);
    }

    [Fact]
    public void 空白中点相等时取前一句()
    {
        MarkerHitTester.Nearest(Three, 4500)!.Index.Should().Be(1);
    }

    [Fact]
    public void 首句之前返回第一句_末句之后返回最后一句()
    {
        MarkerHitTester.Nearest(Three, -100)!.Index.Should().Be(1);
        MarkerHitTester.Nearest(Three, 60000)!.Index.Should().Be(3);
    }

    [Fact]
    public void 大量句子仍返回正确结果()
    {
        var many = Enumerable.Range(0, 12000)
            .Select(i => Span(i * 3000, i * 3000 + 2000, i + 1))
            .ToArray();

        MarkerHitTester.Nearest(many, 3000 * 6000 + 1000)!.Index.Should().Be(6001);
        MarkerHitTester.Nearest(many, 3000 * 6000 - 100)!.Index.Should().Be(6001);
    }
}
