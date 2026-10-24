using System.Diagnostics;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace LearningAssistant.Services.Shadowing
{
    /// <summary>一条待切片的原声区间，[StartMs, EndMs) 为源音频时间。</summary>
    public sealed record AudioSliceRequest(string SegmentId, string SourcePath, int StartMs, int EndMs, string OutputPath);

    public sealed record AudioSliceResult(string SegmentId, bool Success, string? OutputPath, int DurationMs, string? Error);

    /// <summary>
    /// 原声切片服务：解码源音频为 44.1kHz/16bit/单声道 WAV，按采样点裁剪并做 5–10ms 淡入淡出。
    /// 只读源文件，切片先写 .part 再原子改名；已存在的输出文件直接复用（缓存）。
    /// </summary>
    public sealed class AudioSliceService
    {
        private const int TargetSampleRate = 44100;
        private const int PreReadMs = 50;
        private const int PostReadMs = 50;
        private const int FadeMs = 8;
        private const int MinSliceMs = 300;

        public Task<AudioSliceResult> SliceAsync(AudioSliceRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Task.Run(() => Slice(request, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// 逐条切片；单条失败不影响其他条目（已成功的切片不回滚）。
        /// progress 报告“已完成/总数”。
        /// </summary>
        public async Task<IReadOnlyList<AudioSliceResult>> SliceAllAsync(
            IReadOnlyList<AudioSliceRequest> requests,
            IProgress<(int Done, int Total)>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var results = new List<AudioSliceResult>(requests.Count);
            for (int i = 0; i < requests.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                results.Add(await SliceAsync(requests[i], cancellationToken).ConfigureAwait(false));
                progress?.Report((i + 1, requests.Count));
            }
            return results;
        }

        private static AudioSliceResult Slice(AudioSliceRequest request, CancellationToken cancellationToken)
        {
            string output = Path.GetFullPath(request.OutputPath);
            if (File.Exists(output) && new FileInfo(output).Length > 44)
                return new AudioSliceResult(request.SegmentId, true, output, ReadDurationMs(output), null);

            int startMs = Math.Max(0, request.StartMs);
            int endMs = Math.Max(startMs, request.EndMs);
            if (endMs - startMs < MinSliceMs)
                return new AudioSliceResult(request.SegmentId, false, null, 0, $"切片过短（{endMs - startMs}ms），已忽略。");
            if (!File.Exists(request.SourcePath))
                return new AudioSliceResult(request.SegmentId, false, null, 0, "源音频不存在，无法切片。");

            try
            {
                string? directory = Path.GetDirectoryName(output);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                float[] samples = DecodeRange(request.SourcePath, startMs, endMs, cancellationToken);
                if (samples.Length == 0)
                    return new AudioSliceResult(request.SegmentId, false, null, 0, "切片区间内没有可用音频。");

                ApplyFade(samples);
                string partPath = output + ".part";
                WritePcm16(partPath, samples);
                File.Move(partPath, output, overwrite: true);

                int durationMs = (int)Math.Round(samples.Length * 1000d / TargetSampleRate);
                return new AudioSliceResult(request.SegmentId, true, output, durationMs, null);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[AudioSlice] 切片失败 {request.SegmentId}: {ex.Message}");
                return new AudioSliceResult(request.SegmentId, false, null, 0, ex.Message);
            }
        }

        /// <summary>读取 [start-50ms, end+50ms]，裁剪掉预读/后读部分，返回从 start 开始的单声道采样。</summary>
        private static float[] DecodeRange(string sourcePath, int startMs, int endMs, CancellationToken cancellationToken)
        {
            int preStartMs = Math.Max(0, startMs - PreReadMs);
            int readEndMs = endMs + PostReadMs;
            double totalReadMs = readEndMs - preStartMs;

            using var reader = new AudioFileReader(sourcePath);
            reader.CurrentTime = TimeSpan.FromMilliseconds(preStartMs);

            ISampleProvider provider = reader;
            if (reader.WaveFormat.Channels == 2)
                provider = new StereoToMonoSampleProvider(reader) { LeftVolume = 0.5f, RightVolume = 0.5f };
            else if (reader.WaveFormat.Channels != 1)
                throw new NotSupportedException($"不支持 {reader.WaveFormat.Channels} 声道音频。");

            var resampled = new WdlResamplingSampleProvider(provider, TargetSampleRate);
            int totalFrames = Math.Max(0, (int)Math.Round(totalReadMs * TargetSampleRate / 1000d));
            var buffer = new float[totalFrames];
            int read = 0;
            while (read < totalFrames)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int got = resampled.Read(buffer, read, totalFrames - read);
                if (got <= 0)
                    break;
                read += got;
            }

            int leadFrames = (int)Math.Round((startMs - preStartMs) * TargetSampleRate / 1000d);
            int wantedFrames = (int)Math.Round((endMs - startMs) * TargetSampleRate / 1000d);
            int available = Math.Max(0, read - leadFrames);
            int take = Math.Min(wantedFrames, available);
            if (take <= 0)
                return Array.Empty<float>();

            var trimmed = new float[take];
            Array.Copy(buffer, leadFrames, trimmed, 0, take);
            return trimmed;
        }

        /// <summary>首尾各做 8ms 淡入淡出，消除裁剪造成的爆音。</summary>
        private static void ApplyFade(float[] samples)
        {
            int fade = Math.Min(FadeMs * TargetSampleRate / 1000, samples.Length / 4);
            if (fade <= 0)
                return;

            for (int i = 0; i < fade; i++)
            {
                float factor = i / (float)fade;
                samples[i] *= factor;
                samples[samples.Length - 1 - i] *= factor;
            }
        }

        private static void WritePcm16(string path, float[] samples)
        {
            var format = new WaveFormat(TargetSampleRate, 16, 1);
            using var writer = new WaveFileWriter(path, format);
            var bytes = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short value = (short)Math.Clamp((int)Math.Round(samples[i] * 32767f), short.MinValue, short.MaxValue);
                bytes[i * 2] = (byte)(value & 0xFF);
                bytes[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
            }
            writer.Write(bytes, 0, bytes.Length);
        }

        public static int ReadDurationMs(string path)
        {
            try
            {
                using var reader = new AudioFileReader(path);
                return (int)reader.TotalTime.TotalMilliseconds;
            }
            catch
            {
                return 0;
            }
        }
    }
}
