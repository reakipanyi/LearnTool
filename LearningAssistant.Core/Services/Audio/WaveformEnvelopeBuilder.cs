namespace LearningAssistant.Services.Audio;

/// <summary>
/// 流式波形包络：按总帧数与桶数把逐块单声道采样压成峰值数组（0..1），
/// 用于在时间轴上叠加深色能量条，便于判断断句落点。
/// </summary>
public sealed class WaveformEnvelopeBuilder
{
    private readonly long _totalFrames;
    private readonly float[] _peaks;
    private long _position;

    public WaveformEnvelopeBuilder(long totalFrames, int bucketCount)
    {
        if (bucketCount < 1) throw new ArgumentOutOfRangeException(nameof(bucketCount));
        if (totalFrames < 0) totalFrames = 0;

        _totalFrames = totalFrames;
        _peaks = new float[bucketCount];
    }

    public void Push(ReadOnlySpan<float> monoSamples)
    {
        if (monoSamples.IsEmpty) return;

        for (int i = 0; i < monoSamples.Length; i++)
        {
            int bucket = BucketAt(_position);
            if (bucket < 0)
            {
                _position++;
                continue;
            }

            float value = Math.Abs(monoSamples[i]);
            if (value > _peaks[bucket]) _peaks[bucket] = value;
            _position++;
        }
    }

    /// <summary>返回 0..1 的峰值数组；样本超出总帧数的部分丢弃。</summary>
    public float[] Complete()
    {
        for (int i = 0; i < _peaks.Length; i++)
            if (_peaks[i] > 1f) _peaks[i] = 1f;

        return _peaks;
    }

    private int BucketAt(long frame)
    {
        if (_totalFrames <= 0) return -1;
        if (frame >= _totalFrames) return -1;

        long index = frame * _peaks.Length / _totalFrames;
        if (index >= _peaks.Length) index = _peaks.Length - 1;
        return (int)index;
    }
}
