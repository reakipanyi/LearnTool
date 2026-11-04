using System.Text.Json.Serialization;

namespace LearningAssistant.Models.Audio;

/// <summary>
/// 一条曲目的句子时间轴：自动断句缓存与手动覆盖层的共用模型。
/// 自动缓存按音频指纹存储，可重建；手动覆盖层按书存储，不随缓存清理。
/// </summary>
public sealed class AudioSentenceTimeline
{
    public int SchemaVersion { get; set; } = 1;

    /// <summary>相对路径优先，外部目录用绝对路径；仅用于展示与排错。</summary>
    public string TrackKey { get; set; } = "";

    /// <summary>音频指纹：大小|mtime|时长；自动缓存唯一键。</summary>
    public string Fingerprint { get; set; } = "";

    public int DurationMs { get; set; }

    public AudioTimelineAnalysis Analysis { get; set; } = new();

    public List<AudioSentenceSpan> Sentences { get; set; } = new();
}

/// <summary>时间轴分析元信息：引擎、参数快照、来源统计与自评置信度。</summary>
public sealed class AudioTimelineAnalysis
{
    /// <summary>energy-vad / silero / subtitle / shadowing / manual。</summary>
    public string Engine { get; set; } = "energy-vad";

    public string EngineVersion { get; set; } = "1";

    public DateTime AnalyzedAt { get; set; } = DateTime.Now;

    public AudioTimelineOptions Options { get; set; } = new();

    /// <summary>结果自评，低分提示重新断句。</summary>
    public double Confidence { get; set; } = 1.0;

    /// <summary>各来源贡献：shadowing 123 / vad 45 / subtitle 0，便于解释“为什么这句在这里”。</summary>
    public Dictionary<string, int> SourceCounts { get; set; } = new();
}

/// <summary>断句参数快照；默认值即“精听短句”预设。</summary>
public sealed class AudioTimelineOptions
{
    /// <summary>句间停顿阈值（粗遍唯一阈值）。</summary>
    public int SentenceGapMs { get; set; } = 350;

    /// <summary>长句内部切分（细遍）。</summary>
    public int FineGapMs { get; set; } = 150;

    /// <summary>保留短应答；对应 SilenceVadOptions.MinSegmentMilliseconds。</summary>
    public int MinSpeechMs { get; set; } = 150;

    /// <summary>过短并入相邻句；不再丢弃。</summary>
    public int MinSentenceMs { get; set; } = 700;

    /// <summary>精听短句上限，超长强制切。</summary>
    public int MaxSentenceMs { get; set; } = 8000;

    /// <summary>句首尾留白。</summary>
    public int PaddingMs { get; set; } = 100;

    public double NoiseMultiplier { get; set; } = 2.5;

    public double MinAbsoluteRms { get; set; } = 0.0005;

    public AudioTimelineOptions Clone() => (AudioTimelineOptions)MemberwiseClone();
}

/// <summary>一段源音频时间区间；[StartMs, EndMs) 左闭右开。</summary>
public readonly record struct AudioTimeRange(int StartMs, int EndMs)
{
    public int DurationMs => EndMs - StartMs;
}

/// <summary>
/// 一句的时间跨度；[StartMs, EndMs) 左闭右开，单位为源音频毫秒。
/// Index 与 Text 只在运行期赋值，不落盘。
/// </summary>
public sealed class AudioSentenceSpan
{
    /// <summary>1-based，运行期按 StartMs 排序后赋值，不落盘。</summary>
    [JsonIgnore]
    public int Index { get; set; }

    public int StartMs { get; set; }

    public int EndMs { get; set; }

    /// <summary>来自跟读切片/单页音频时，便于页码层与页面同步。</summary>
    public int? PageIndex { get; set; }

    /// <summary>manual / shadowing / subtitle / vad / vad-hard / vad-fallback。</summary>
    public string? BoundarySource { get; set; }

    public double Confidence { get; set; } = 1.0;

    /// <summary>跟读切片的 Segment.Id；P0 只读关联。</summary>
    public string? SegmentId { get; set; }

    public string? HighlightId { get; set; }

    /// <summary>运行期从字幕/切片叠加，缓存不写。</summary>
    [JsonIgnore]
    public string? Text { get; set; }

    [JsonIgnore]
    public string? TextSource { get; set; }
}

/// <summary>页码层锚点：只存“有来源”的锚点，runtime 计算，不写缓存。</summary>
public sealed class AudioPageAnchor
{
    public int PageIndex { get; set; }

    public int StartMs { get; set; }

    /// <summary>shadowing / manual。</summary>
    public string Source { get; set; } = "shadowing";

    /// <summary>起点是按“最早切片 - PaddingMs”估算的（切片从本页第 2 句才开始打点时会漏页首）。</summary>
    public bool IsStartEstimated { get; set; }

    /// <summary>插值层才 &lt; 0.5，且默认不生成。</summary>
    public double Confidence { get; set; } = 1.0;
}

/// <summary>手动覆盖层：按稳定句 ID（或 PageIndex + 起始时间）定位，存用户目录。</summary>
public sealed class AudioBoundaryOverlay
{
    public int SchemaVersion { get; set; } = 1;

    public string TrackKey { get; set; } = "";

    public string Fingerprint { get; set; } = "";

    public List<AudioSentenceEdit> Edits { get; set; } = new();

    public List<AudioPageAnchor> PageAnchors { get; set; } = new();
}

/// <summary>一条手动修正：拖边界 / 合并 / 拆分。</summary>
public sealed class AudioSentenceEdit
{
    /// <summary>稳定 ID，优先用 SegmentId / anchorKey。</summary>
    public string Id { get; set; } = "";

    public int StartMs { get; set; }

    public int EndMs { get; set; }

    /// <summary>adjust / merge / split（页码锚点单独存在 PageAnchors）。</summary>
    public string Kind { get; set; } = "adjust";
}
