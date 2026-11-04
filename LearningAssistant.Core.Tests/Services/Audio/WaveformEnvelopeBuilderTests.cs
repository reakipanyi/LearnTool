using FluentAssertions;
using LearningAssistant.Services.Audio;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Audio;

public class WaveformEnvelopeBuilderTests
{
    [Fact]
    public void 恒定振幅_每个桶都记录峰值()
    {
        var builder = new WaveformEnvelopeBuilder(100, 10);
        var samples = Enumerable.Repeat(0.5f, 100).ToArray();

        builder.Push(samples);
        float[] peaks = builder.Complete();

        peaks.Should().HaveCount(10);
        peaks.Should().AllSatisfy(peak => peak.Should().BeApproximately(0.5f, 0.0001f));
    }

    [Fact]
    public void 只在前半段有声音_后半段为零()
    {
        var builder = new WaveformEnvelopeBuilder(100, 10);
        var first = Enumerable.Repeat(1f, 50).ToArray();
        var second = Enumerable.Repeat(0f, 50).ToArray();

        builder.Push(first);
        builder.Push(second);
        float[] peaks = builder.Complete();

        for (int i = 0; i < 5; i++) peaks[i].Should().BeApproximately(1f, 0.0001f);
        for (int i = 5; i < 10; i++) peaks[i].Should().Be(0f);
    }

    [Fact]
    public void 负峰值取绝对值_并裁剪到一()
    {
        var builder = new WaveformEnvelopeBuilder(10, 10);

        builder.Push(new[] { -1.2f, 0.3f, -0.4f, 0.2f, 0f, 0f, 0f, 0f, 0f, 0f });
        float[] peaks = builder.Complete();

        peaks[0].Should().Be(1f);
        peaks[1].Should().BeApproximately(0.3f, 0.0001f);
        peaks[2].Should().BeApproximately(0.4f, 0.0001f);
    }

    [Fact]
    public void 超出总帧数的样本被丢弃()
    {
        var builder = new WaveformEnvelopeBuilder(10, 5);
        var samples = Enumerable.Repeat(1f, 20).ToArray();

        builder.Push(samples);
        float[] peaks = builder.Complete();

        peaks.Sum().Should().BeApproximately(5f, 0.0001f);
    }

    [Fact]
    public void 空采样不改变包络()
    {
        var builder = new WaveformEnvelopeBuilder(10, 5);

        builder.Push(ReadOnlySpan<float>.Empty);
        float[] peaks = builder.Complete();

        peaks.Should().AllSatisfy(peak => peak.Should().Be(0f));
    }
}
