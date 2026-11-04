using LearningAssistant.Services.Shadowing;

namespace LearningAssistant.Abstractions.Audio;

/// <summary>
/// 流式语音区间分析器：逐块喂入单声道采样，完成时给出语音区间。
/// 默认实现为能量 VAD；外部可提供 WebRTC VAD / Silero VAD 等实现。
/// </summary>
public interface IAudioSegmentAnalyzer
{
    /// <summary>分析器名称，写入时间轴的 Engine 字段。</summary>
    string Name { get; }

    /// <summary>喂入单声道采样；实现内部自行缓存不足一帧的数据。</summary>
    void Push(ReadOnlySpan<float> monoSamples);

    /// <summary>结束分析并返回语音区间。</summary>
    IReadOnlyList<VadSegment> Complete();
}

/// <summary>
/// 分析器工厂：按采样率与参数创建实例。Name 必须与
/// <c>AudioTimelineOptions.AnalyzerName</c> 对应，才会被选中。
/// </summary>
public interface IAudioSegmentAnalyzerFactory
{
    string Name { get; }

    IAudioSegmentAnalyzer Create(int sampleRate, SilenceVadOptions options);
}
