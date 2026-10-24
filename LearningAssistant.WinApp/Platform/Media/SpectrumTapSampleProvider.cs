using NAudio.Wave;

namespace LearningAssistant.Platform.Media;

/// <summary>
/// 采样旁路：把最终输出的音频混成单声道写入环形缓冲，供实时频谱读取，
/// 自身不改变播放数据与时长。
/// </summary>
internal sealed class SpectrumTapSampleProvider : ISampleProvider
{
    /// <summary>环形缓冲可保存的采样数，约 46ms（44.1kHz）</summary>
    private const int Capacity = 2048;

    private readonly ISampleProvider _source;
    private readonly int _channels;
    private readonly object _sync = new();
    private readonly float[] _mono = new float[Capacity];
    private int _writeIndex;
    private int _filled;

    public SpectrumTapSampleProvider(ISampleProvider source)
    {
        _source = source;
        _channels = source.WaveFormat.Channels;
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        int read = _source.Read(buffer, offset, count);
        if (read <= 0) return read;

        lock (_sync)
        {
            int frames = read / _channels;
            for (int i = 0; i < frames; i++)
            {
                int baseIndex = offset + i * _channels;
                float sum = 0;
                for (int c = 0; c < _channels; c++)
                    sum += buffer[baseIndex + c];

                _mono[_writeIndex] = sum / _channels;
                _writeIndex = (_writeIndex + 1) % Capacity;
                if (_filled < Capacity) _filled++;
            }
        }

        return read;
    }

    /// <summary>把最近 <paramref name="destination"/>.Length 个单声道采样按时间顺序复制出来。</summary>
    public bool TryGetLatest(float[] destination)
    {
        lock (_sync)
        {
            if (_filled < destination.Length) return false;

            int start = (_writeIndex - destination.Length + Capacity) % Capacity;
            for (int i = 0; i < destination.Length; i++)
                destination[i] = _mono[(start + i) % Capacity];
            return true;
        }
    }
}
