using FluentAssertions;
using LearningAssistant.Models.Audio;
using LearningAssistant.Services.Audio;
using NAudio.Wave;
using Xunit;

namespace LearningAssistant.Tests
{
    /// <summary>
    /// 合成 WAV 端到端分析（设计 §14.2）：正弦 burst + 静音，
    /// 跑 <see cref="AudioDecodeAnalysisService"/> 断言句数与边界，不依赖真实素材与音频设备。
    /// </summary>
    public class AudioDecodeAnalysisServiceTests : IDisposable
    {
        private const int SampleRate = 8000;
        private const int BurstMs = 1000;
        private const int SilenceMs = 1000;
        private const int BurstCount = 3;

        private readonly string _directory = Path.Combine(
            Path.GetTempPath(), $"decode_analysis_{Guid.NewGuid():N}");

        public AudioDecodeAnalysisServiceTests() => Directory.CreateDirectory(_directory);

        public void Dispose()
        {
            try { Directory.Delete(_directory, recursive: true); } catch { /* 测试清理失败不影响断言 */ }
        }

        [Fact]
        public async Task AnalyzeAsync_SyntheticBursts_ReturnsSentenceCountAndBoundaries()
        {
            var path = WriteBurstWave();

            var result = await new AudioDecodeAnalysisService()
                .AnalyzeAsync(path, new AudioTimelineOptions());

            int expectedDurationMs = BurstCount * (BurstMs + SilenceMs);
            result.DurationMs.Should().BeInRange(expectedDurationMs - 5, expectedDurationMs + 5);
            result.Engine.Should().Be("energy-vad");
            result.UsedHoleScan.Should().BeFalse();

            result.Coarse.Should().HaveCount(BurstCount);
            result.Fine.Should().HaveCount(BurstCount);

            for (int i = 0; i < BurstCount; i++)
            {
                int expectedStart = i * (BurstMs + SilenceMs);
                int expectedEnd = expectedStart + BurstMs;

                result.Coarse[i].StartMs.Should().BeCloseTo(expectedStart, 40);
                result.Coarse[i].EndMs.Should().BeCloseTo(expectedEnd, 40);
                result.Fine[i].StartMs.Should().BeCloseTo(expectedStart, 40);
                result.Fine[i].EndMs.Should().BeCloseTo(expectedEnd, 40);
            }
        }

        /// <summary>写入三段 400Hz / 0.5 振幅的正弦 burst，burst 之间与结尾各 1 秒静音。</summary>
        private string WriteBurstWave()
        {
            int totalMs = BurstCount * (BurstMs + SilenceMs);
            int totalSamples = totalMs * SampleRate / 1000;
            var samples = new short[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                int ms = i * 1000 / SampleRate;
                int inBurst = ms % (BurstMs + SilenceMs);
                if (inBurst >= BurstMs) continue;

                double value = 0.5 * Math.Sin(2 * Math.PI * 400 * i / SampleRate);
                samples[i] = (short)Math.Round(value * short.MaxValue);
            }

            var bytes = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
                BitConverter.TryWriteBytes(bytes.AsSpan(i * 2, 2), samples[i]);

            var path = Path.Combine(_directory, "bursts.wav");
            using var writer = new WaveFileWriter(path, new WaveFormat(SampleRate, 16, 1));
            writer.Write(bytes, 0, bytes.Length);
            return path;
        }
    }
}
