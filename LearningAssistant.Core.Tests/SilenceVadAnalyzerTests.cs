using System;
using System.Collections.Generic;
using LearningAssistant.Services.Shadowing;
using Xunit;

namespace LearningAssistant.Core.Tests;

public class SilenceVadAnalyzerTests
{
    private const int SampleRate = 16000;

    [Fact]
    public void SingleBurst_ProducesOneSegmentWithPadding()
    {
        var samples = new List<float>();
        samples.AddRange(Silence(1000));
        samples.AddRange(Tone(1000));
        samples.AddRange(Silence(1000));

        var segments = Analyze(samples);

        var segment = Assert.Single(segments);
        Assert.InRange(segment.StartMs, 850, 920);
        Assert.InRange(segment.EndMs, 2080, 2150);
    }

    [Fact]
    public void ThreeBursts_ProduceThreeSegments()
    {
        var samples = new List<float>();
        samples.AddRange(Tone(500));
        samples.AddRange(Silence(600));
        samples.AddRange(Tone(500));
        samples.AddRange(Silence(600));
        samples.AddRange(Tone(500));

        var segments = Analyze(samples);

        Assert.Equal(3, segments.Count);
        // 500ms 语音 + 前后各 120ms 留白 = 740ms；中间片段两侧都留白约 800ms
        Assert.All(segments, s => Assert.InRange(s.DurationMs, 450, 820));
    }

    [Fact]
    public void ShortGap_DoesNotSplitSegment()
    {
        var samples = new List<float>();
        samples.AddRange(Tone(500));
        samples.AddRange(Silence(100));
        samples.AddRange(Tone(500));

        var segments = Analyze(samples);

        Assert.Single(segments);
    }

    [Fact]
    public void AllSilence_ReturnsNoSegments()
    {
        var segments = Analyze(Silence(1200));

        Assert.Empty(segments);
    }

    [Fact]
    public void ContinuousSpeech_ReturnsWholeSegment()
    {
        var segments = Analyze(Tone(1000));

        var segment = Assert.Single(segments);
        Assert.Equal(0, segment.StartMs);
        Assert.InRange(segment.EndMs, 1000, 1100);
    }

    [Fact]
    public void TooShortBurst_IsDropped()
    {
        var samples = new List<float>();
        samples.AddRange(Silence(500));
        samples.AddRange(Tone(100)); // 低于 250ms 的最短句长
        samples.AddRange(Silence(500));

        var segments = Analyze(samples);

        Assert.Empty(segments);
    }

    private static IReadOnlyList<VadSegment> Analyze(IEnumerable<float> samples)
    {
        var analyzer = new SilenceVadAnalyzer(SampleRate);
        var buffer = new List<float>(samples);
        analyzer.Push(buffer.ToArray());
        return analyzer.Complete();
    }

    private static IEnumerable<float> Silence(int milliseconds)
    {
        int count = SampleRate * milliseconds / 1000;
        for (int i = 0; i < count; i++)
            yield return 0f;
    }

    private static IEnumerable<float> Tone(int milliseconds)
    {
        int count = SampleRate * milliseconds / 1000;
        for (int i = 0; i < count; i++)
            yield return (float)(0.3 * Math.Sin(2 * Math.PI * 440 * i / SampleRate));
    }
}
