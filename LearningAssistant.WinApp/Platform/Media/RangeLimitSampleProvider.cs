using NAudio.Wave;

namespace LearningAssistant.Platform.Media;

/// <summary>
/// 采样级区间结束限制：在输出链末端精确数到区间终点，最后 30ms 线性淡出，
/// 之后输出静音并只回调一次结束事件。放在最终输出端，保证变速/变调后依然精确。
/// </summary>
public sealed class RangeLimitSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly int _channels;
    private bool _armed;
    private long _framesRemaining;
    private long _fadeFrames;
    private bool _completedRaised;

    public RangeLimitSampleProvider(ISampleProvider source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _channels = Math.Max(1, source.WaveFormat.Channels);
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    /// <summary>区间终点到达时回调（在音频线程触发，调用方需自行回到 UI 线程）。</summary>
    public Action? Completed { get; set; }

    /// <summary>以“输出侧帧数”武装限制；fadeFrames 为淡出帧数（0 表示不淡出）。</summary>
    public void Arm(long framesRemaining, long fadeFrames)
    {
        _framesRemaining = Math.Max(0, framesRemaining);
        _fadeFrames = Math.Clamp(fadeFrames, 0, _framesRemaining);
        _armed = true;
        _completedRaised = false;
    }

    public void Disarm()
    {
        _armed = false;
        _framesRemaining = 0;
        _fadeFrames = 0;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int read = _source.Read(buffer, offset, count);
        if (!_armed || read <= 0) return read;

        int frames = read / _channels;
        int index = offset;

        for (int frame = 0; frame < frames; frame++)
        {
            if (_framesRemaining <= 0)
            {
                // 到达终点后整段输出静音，直到上层暂停
                for (int ch = 0; ch < _channels; ch++)
                    buffer[index + ch] = 0f;
                index += _channels;
                continue;
            }

            float gain = 1f;
            if (_fadeFrames > 0 && _framesRemaining <= _fadeFrames)
                gain = (float)(_framesRemaining / (double)_fadeFrames);

            for (int ch = 0; ch < _channels; ch++)
                buffer[index + ch] *= gain;

            index += _channels;
            _framesRemaining--;
        }

        if (_framesRemaining <= 0 && !_completedRaised)
        {
            _completedRaised = true;
            Completed?.Invoke();
        }
        return read;
    }
}
