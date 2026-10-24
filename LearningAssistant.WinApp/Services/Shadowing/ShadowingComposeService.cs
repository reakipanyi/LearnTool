using System.Diagnostics;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace LearningAssistant.Services.Shadowing
{
    public sealed record ShadowingComposeItem(string SegmentId, string TakeId, string TakePath);

    public sealed record ShadowingComposeResult(
        bool Success,
        string? OutputPath,
        int DurationMs,
        int SegmentCount,
        IReadOnlyList<string> SegmentRefs,
        IReadOnlyList<string> TakeRefs,
        IReadOnlyList<string> MissingSegmentIds,
        string? Error);

    /// <summary>
    /// 跟读成品合成：按段序拼接已采用的 take，统一 44.1kHz/16bit/单声道，段间插入固定静音。
    /// 先写 .part 再原子改名；失败项只跳过并回报，不回滚已完成部分。
    /// </summary>
    public sealed class ShadowingComposeService
    {
        private const int TargetSampleRate = 44100;
        private const int DefaultGapMs = 400;

        public Task<ShadowingComposeResult> ComposeAsync(
            string outputPath,
            IReadOnlyList<ShadowingComposeItem> items,
            int gapMs = DefaultGapMs,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(items);
            return Task.Run(() => Compose(outputPath, items, Math.Clamp(gapMs, 0, 3000), cancellationToken), cancellationToken);
        }

        private static ShadowingComposeResult Compose(
            string outputPath,
            IReadOnlyList<ShadowingComposeItem> items,
            int gapMs,
            CancellationToken cancellationToken)
        {
            var segmentRefs = new List<string>();
            var takeRefs = new List<string>();
            var missing = new List<string>();
            string fullPath = Path.GetFullPath(outputPath);
            string partPath = fullPath + ".part";

            try
            {
                string? directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                int totalSamples = 0;
                using (var writer = new WaveFileWriter(partPath, new WaveFormat(TargetSampleRate, 16, 1)))
                {
                    foreach (var item in items)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (string.IsNullOrWhiteSpace(item.TakePath) || !File.Exists(item.TakePath))
                        {
                            missing.Add(item.SegmentId);
                            continue;
                        }

                        int written = 0;
                        try
                        {
                            written = AppendTake(writer, item.TakePath, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceWarning($"[ShadowingCompose] 追加 take 失败 {item.TakeId}: {ex.Message}");
                        }

                        if (written <= 0)
                        {
                            missing.Add(item.SegmentId);
                            continue;
                        }

                        totalSamples += written;
                        segmentRefs.Add(item.SegmentId);
                        takeRefs.Add(item.TakeId);

                        int gapSamples = gapMs * TargetSampleRate / 1000;
                        if (gapSamples > 0)
                        {
                            WriteSilence(writer, gapSamples);
                            totalSamples += gapSamples;
                        }
                    }
                }

                if (segmentRefs.Count == 0)
                {
                    TryDelete(partPath);
                    return new ShadowingComposeResult(false, null, 0, 0, segmentRefs, takeRefs, missing,
                        "没有可用于合成的跟读录音。");
                }

                File.Move(partPath, fullPath, overwrite: true);
                int durationMs = (int)Math.Round(totalSamples * 1000d / TargetSampleRate);
                return new ShadowingComposeResult(true, fullPath, durationMs, segmentRefs.Count,
                    segmentRefs, takeRefs, missing, null);
            }
            catch (OperationCanceledException)
            {
                TryDelete(partPath);
                throw;
            }
            catch (Exception ex)
            {
                TryDelete(partPath);
                return new ShadowingComposeResult(false, null, 0, 0, segmentRefs, takeRefs, missing, ex.Message);
            }
        }

        /// <summary>把一条 take 规范化后写入 writer，返回写入的采样点数。</summary>
        private static int AppendTake(WaveFileWriter writer, string takePath, CancellationToken cancellationToken)
        {
            using var reader = new WaveFileReader(takePath);
            ISampleProvider provider = reader.ToSampleProvider();

            if (reader.WaveFormat.Channels == 2)
                provider = new StereoToMonoSampleProvider(provider) { LeftVolume = 0.5f, RightVolume = 0.5f };
            else if (reader.WaveFormat.Channels != 1)
                return 0;

            var resampled = new WdlResamplingSampleProvider(provider, TargetSampleRate);
            var buffer = new float[16384];
            var bytes = new byte[buffer.Length * 2];
            int written = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int read = resampled.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                    break;

                for (int i = 0; i < read; i++)
                {
                    short value = (short)Math.Clamp((int)Math.Round(buffer[i] * 32767f), short.MinValue, short.MaxValue);
                    bytes[i * 2] = (byte)(value & 0xFF);
                    bytes[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
                }
                writer.Write(bytes, 0, read * 2);
                written += read;
            }

            return written;
        }

        private static void WriteSilence(WaveFileWriter writer, int sampleCount)
        {
            var silence = new byte[sampleCount * 2];
            writer.Write(silence, 0, silence.Length);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }
    }
}
