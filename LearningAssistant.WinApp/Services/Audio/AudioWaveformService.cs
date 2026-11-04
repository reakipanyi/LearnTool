using System.Collections.Concurrent;
using LearningAssistant.Models.Pdf;
using NAudio.Wave;

namespace LearningAssistant.Services.Audio;

/// <summary>
/// 波形包络加载：后台解码单声道并压缩为固定桶数峰值，按曲目指纹进程内缓存。
/// 只用于时间轴可视化，失败时静默返回 null。
/// </summary>
public sealed class AudioWaveformService
{
    public const int DefaultBucketCount = 2000;

    private const int BlockSize = 8192;

    private readonly ConcurrentDictionary<string, float[]> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<float[]?> LoadAsync(AudioTrack? track, CancellationToken cancellationToken = default)
    {
        if (track == null || string.IsNullOrWhiteSpace(track.FilePath) || !File.Exists(track.FilePath))
            return Task.FromResult<float[]?>(null);

        string key = $"{track.FilePath}|{AudioTimelineStore.ComputeFingerprint(track.FilePath, 0)}";
        if (_cache.TryGetValue(key, out var cached))
            return Task.FromResult<float[]?>(cached);

        return Task.Run(() => Load(track.FilePath, key, cancellationToken), cancellationToken);
    }

    private float[]? Load(string path, string key, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(key, out var cached)) return cached;

        try
        {
            using var reader = new AudioFileReader(path);
            long totalFrames = (long)Math.Round(reader.TotalTime.TotalSeconds * reader.WaveFormat.SampleRate);
            if (totalFrames <= 0) return null;

            var builder = new WaveformEnvelopeBuilder(totalFrames, DefaultBucketCount);
            var buffer = new float[BlockSize];
            var mono = new float[BlockSize];
            int channels = Math.Max(1, reader.WaveFormat.Channels);

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

                builder.Push(mono.AsSpan(0, frames));
            }

            var peaks = builder.Complete();
            _cache[key] = peaks;
            return peaks;
        }
        catch (Exception)
        {
            // 波形只是辅助显示，解码失败不影响播放与断句
            return null;
        }
    }
}
