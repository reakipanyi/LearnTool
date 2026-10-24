using LearningAssistant.Services.Shadowing;
using NAudio.Wave;

namespace LearningAssistant.Services.Shadowing
{
    /// <summary>
    /// 对当前曲目做能量 VAD 断句：流式解码并混成单声道，逐块喂给分析器，
    /// 不把整轨音频读进内存。
    /// </summary>
    public sealed class VadSegmentService
    {
        public Task<IReadOnlyList<VadSegment>> AnalyzeAsync(
            string audioPath,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.Run<IReadOnlyList<VadSegment>>(() =>
            {
                using var reader = new AudioFileReader(audioPath);
                int channels = reader.WaveFormat.Channels;
                double totalMs = reader.TotalTime.TotalMilliseconds;
                var analyzer = new SilenceVadAnalyzer(reader.WaveFormat.SampleRate);

                var buffer = new float[8192];
                // 块内帧数上限，避免混音缓冲反复分配
                var mono = new float[buffer.Length];
                int read;
                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int frames = read / channels;
                    for (int f = 0; f < frames; f++)
                    {
                        int at = f * channels;
                        float sum = 0;
                        for (int c = 0; c < channels; c++)
                            sum += buffer[at + c];
                        mono[f] = sum / channels;
                    }

                    analyzer.Push(mono.AsSpan(0, frames));
                    if (totalMs > 0)
                        progress?.Report(Math.Clamp(reader.CurrentTime.TotalMilliseconds / totalMs, 0, 1));
                }

                progress?.Report(1);
                return analyzer.Complete();
            }, cancellationToken);
        }
    }
}
