using NAudio.Wave;

namespace LearningAssistant.Platform.Media;

/// <summary>
/// 变速采样提供器：按 <see cref="PlaybackRate"/> 从源采样中线性插值取样，
/// 只改变时长不改采样率。音高会随速度一起变化；
/// 需要变速不变调时，由调用方在其后再串接音高修正提供器。
/// </summary>
internal sealed class VarispeedSampleProvider : ISampleProvider
{
    /// <summary>单次向源读取的最小帧数，避免逐帧读取导致大量小 IO</summary>
    private const int ReadChunkFrames = 2048;

    private readonly ISampleProvider _source;
    private readonly int _channels;
    /// <summary>音频线程取样与 UI 线程 Seek/Reset 互斥，避免缓冲游标被并发改写</summary>
    private readonly object _sync = new();

    private float[] _buffer = Array.Empty<float>();
    /// <summary>缓冲内有效帧数</summary>
    private int _bufferedFrames;
    /// <summary>下一次输出在缓冲内的位置（帧为单位，含小数）</summary>
    private double _cursor;
    private double _playbackRate;

    public VarispeedSampleProvider(ISampleProvider source, double playbackRate)
    {
        _source = source;
        _channels = source.WaveFormat.Channels;
        _playbackRate = ClampRate(playbackRate);
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public double PlaybackRate
    {
        get => _playbackRate;
        set => _playbackRate = ClampRate(value);
    }

    /// <summary>外部改变源位置（Seek / Stop）后必须调用，丢弃旧缓冲并从头重新定位。</summary>
    public void Reset()
    {
        lock (_sync)
        {
            _bufferedFrames = 0;
            _cursor = 0;
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        lock (_sync)
        {
            int framesRequested = count / _channels;
            int written = 0;
            while (written < framesRequested)
            {
                int frame = (int)_cursor;
                // 线性插值需要 frame 与 frame + 1 两帧同时可用
                if (!EnsureFrames(frame + 2))
                    break;

                float fraction = (float)(_cursor - frame);
                int at = frame * _channels;
                int next = at + _channels;
                int target = offset + written * _channels;
                for (int c = 0; c < _channels; c++)
                    buffer[target + c] = _buffer[at + c] + (_buffer[next + c] - _buffer[at + c]) * fraction;

                written++;
                _cursor += _playbackRate;

                // 已消费过半时前移缓冲，避免缓冲随播放无限增长
                if ((int)_cursor >= _bufferedFrames / 2)
                    Compact();
            }

            return written * _channels;
        }
    }

    private bool EnsureFrames(int required)
    {
        while (_bufferedFrames < required)
        {
            int sampleIndex = _bufferedFrames * _channels;
            int wanted = Math.Max(required - _bufferedFrames, ReadChunkFrames) * _channels;
            if (_buffer.Length < sampleIndex + wanted)
                Array.Resize(ref _buffer, Math.Max(sampleIndex + wanted, Math.Max(ReadChunkFrames, _buffer.Length) * 2));

            int read = _source.Read(_buffer, sampleIndex, wanted);
            if (read <= 0)
                return false;
            _bufferedFrames += read / _channels;
        }
        return true;
    }

    private void Compact()
    {
        int drop = (int)_cursor;
        if (drop <= 0)
            return;

        int remaining = _bufferedFrames - drop;
        if (remaining > 0)
            Array.Copy(_buffer, drop * _channels, _buffer, 0, remaining * _channels);
        _bufferedFrames = Math.Max(0, remaining);
        _cursor -= drop;
    }

    private static double ClampRate(double rate) => Math.Clamp(rate, 0.5, 2.0);
}
