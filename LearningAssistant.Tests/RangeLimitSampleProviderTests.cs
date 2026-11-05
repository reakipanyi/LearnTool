using FluentAssertions;
using LearningAssistant.Platform.Media;
using NAudio.Wave;
using Xunit;

namespace LearningAssistant.Tests
{
    /// <summary>
    /// 采样级区间限位器测试（设计 §8.1.1 / §14.1）：
    /// 假源确定性输出，不碰音频设备；覆盖限位帧数、30ms 淡出、立体声整帧截断、重复 Arm、EOF 与 Disarm。
    /// </summary>
    public class RangeLimitSampleProviderTests
    {
        private sealed class FakeSource : ISampleProvider
        {
            private readonly Func<int, int, float> _sample;
            private long _remainingFrames;
            private long _framesEmitted;

            public FakeSource(int sampleRate, int channels, long frames, Func<int, int, float>? sample = null)
            {
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
                _remainingFrames = frames;
                _sample = sample ?? ((_, _) => 1f);
            }

            public WaveFormat WaveFormat { get; }

            public int Reads { get; private set; }

            public long FramesEmitted => _framesEmitted;

            public int Read(float[] buffer, int offset, int count)
            {
                Reads++;
                int channels = WaveFormat.Channels;
                long frames = Math.Min(count / channels, _remainingFrames);
                int samples = (int)frames * channels;

                for (int f = 0; f < frames; f++)
                {
                    for (int ch = 0; ch < channels; ch++)
                        buffer[offset + f * channels + ch] = _sample((int)(_framesEmitted + f), ch);
                }

                _framesEmitted += frames;
                _remainingFrames -= frames;
                return samples;
            }
        }

        [Fact]
        public void Read_StopsExactlyAtLimit_KeepsSourceUntouchedAfterwards()
        {
            var source = new FakeSource(8000, 1, 20000, (frame, _) => frame + 1);
            var provider = new RangeLimitSampleProvider(source);
            int completed = 0;
            provider.Completed = () => completed++;

            provider.Arm(4000, 0);

            var buffer = new float[1000];
            int total = 0;
            for (int i = 0; i < 5; i++)
                total += provider.Read(buffer, 0, buffer.Length);

            total.Should().Be(4000);
            source.FramesEmitted.Should().Be(4000);
            source.Reads.Should().Be(4);
            completed.Should().Be(1);
        }

        [Fact]
        public void Read_AppliesLinearFadeOverLastFrames_MonotonicToZero()
        {
            var source = new FakeSource(8000, 1, 2000);
            var provider = new RangeLimitSampleProvider(source);
            provider.Arm(1000, 240);

            var buffer = new float[1000];
            provider.Read(buffer, 0, buffer.Length).Should().Be(1000);

            // 淡出前恒为 1
            for (int i = 0; i < 760; i++)
                buffer[i].Should().Be(1f);

            var fade = buffer.Skip(760).ToArray();
            fade[0].Should().Be(1f);
            for (int i = 1; i < fade.Length; i++)
                fade[i].Should().BeLessThanOrEqualTo(fade[i - 1]);

            fade[^1].Should().BeApproximately(1f / 240f, 0.001f);
        }

        [Fact]
        public void Read_Stereo_TruncatesByWholeFrames_WithoutChannelShift()
        {
            var source = new FakeSource(8000, 2, 100, (frame, ch) => ch == 0 ? 100 + frame : 200 + frame);
            var provider = new RangeLimitSampleProvider(source);
            provider.Arm(3, 0);

            var buffer = new float[7];
            int read = provider.Read(buffer, 0, buffer.Length);

            read.Should().Be(6);
            buffer[0].Should().Be(100f);
            buffer[1].Should().Be(200f);
            buffer[2].Should().Be(101f);
            buffer[3].Should().Be(201f);
            buffer[4].Should().Be(102f);
            buffer[5].Should().Be(202f);
            buffer[6].Should().Be(0f);
        }

        [Fact]
        public void Arm_Again_RestartsCountingFromZero()
        {
            var source = new FakeSource(8000, 1, 2000);
            var provider = new RangeLimitSampleProvider(source);
            int completed = 0;
            provider.Completed = () => completed++;

            var buffer = new float[8];
            provider.Arm(10, 0);
            provider.Read(buffer, 0, buffer.Length).Should().Be(8);
            completed.Should().Be(0);

            provider.Arm(2, 0);
            provider.Read(buffer, 0, buffer.Length).Should().Be(2);
            completed.Should().Be(1);
            provider.Read(buffer, 0, buffer.Length).Should().Be(0);
            completed.Should().Be(1);
        }

        [Fact]
        public void Read_SourceEofEarly_ReturnsReadAmount_AndRaisesOnce()
        {
            var source = new FakeSource(8000, 1, 3, (_, _) => 0.5f);
            var provider = new RangeLimitSampleProvider(source);
            int completed = 0;
            provider.Completed = () => completed++;

            provider.Arm(100, 0);

            var buffer = new float[8];
            provider.Read(buffer, 0, buffer.Length).Should().Be(3);
            completed.Should().Be(0);

            provider.Read(buffer, 0, buffer.Length).Should().Be(0);
            provider.Read(buffer, 0, buffer.Length).Should().Be(0);
            completed.Should().Be(1);
        }

        [Fact]
        public void Disarm_RestoresPassThrough()
        {
            var source = new FakeSource(8000, 1, 2000);
            var provider = new RangeLimitSampleProvider(source);
            int completed = 0;
            provider.Completed = () => completed++;

            var buffer = new float[8];
            provider.Arm(10, 0);
            provider.Read(buffer, 0, buffer.Length).Should().Be(8);

            provider.Disarm();
            provider.Read(buffer, 0, buffer.Length).Should().Be(8);
            completed.Should().Be(0);
            buffer.Should().OnlyContain(v => v == 1f);
        }
    }
}
