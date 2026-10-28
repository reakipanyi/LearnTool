namespace LearningAssistant.Models.Shadowing
{
    /// <summary>
    /// 某页的原声切片清单（P3 跟读闭环）：切片边界、跟读 take、合成记录都落在同一份文件里。
    /// 与旧的分句清单并存，切片按用户 + PDF 哈希隔离。
    /// </summary>
    public class ShadowingSegmentManifest
    {
        public int SchemaVersion { get; set; } = 3;

        public int PageIndex { get; set; }

        public ShadowingSegmentSource Source { get; set; } = new();

        public ShadowingSegmentSession Session { get; set; } = new();

        public List<ShadowingSegment> Segments { get; set; } = new();

        public List<ShadowingComposedAudio> Composed { get; set; } = new();
    }

    /// <summary>
    /// 切片来源的源音频信息；fingerprint 用于判断源文件是否变更。
    /// </summary>
    public class ShadowingSegmentSource
    {
        public string TrackPath { get; set; } = string.Empty;

        public string Fingerprint { get; set; } = string.Empty;

        public int DurationMs { get; set; }
    }

    /// <summary>
    /// 一次打点会话的元信息；captureSpeed 记录打点时的倍速，切片时按源时间轴换算。
    /// </summary>
    public class ShadowingSegmentSession
    {
        public string Id { get; set; } = string.Empty;

        public double CaptureSpeed { get; set; } = 1.0;

        public bool LockedPage { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 一条原声切片；[StartMs, EndMs) 左闭右开，单位为源音频毫秒。
    /// </summary>
    public class ShadowingSegment
    {
        public string Id { get; set; } = string.Empty;

        public int StartMs { get; set; }

        public int EndMs { get; set; }

        public string? Text { get; set; }

        /// <summary>srt / lrc / ocr / manual / highlight / auto-highlight / page-text；只作标签，不做强制对齐。</summary>
        public string? TextSource { get; set; }

        /// <summary>绑定的手动或自动高亮 ID；为空表示只有页面文字，没有高亮锚点。</summary>
        public string? HighlightId { get; set; }

        /// <summary>
        /// 稳定高亮锚点：页码 + 规范化文本 + 量化坐标 + 句内序号。
        /// 自动高亮整本重建后 ID 会变化，用该锚点仍可找回原声切片。
        /// </summary>
        public string? HighlightAnchor { get; set; }

        /// <summary>一条高亮拆成多句时，当前句在其中的序号；未拆分时为 0。</summary>
        public int CuePartIndex { get; set; }

        /// <summary>边界来源：tap（空格打点）/ vad / subtitle；用于区分切片精度。</summary>
        public string? BoundarySource { get; set; }

        /// <summary>自动对齐置信度 0..1；手动打点为 1。</summary>
        public double? AlignmentConfidence { get; set; }

        /// <summary>相对 Shadowings 根目录的切片路径，例 slices/sg_011_7c3a9e21.wav。</summary>
        public string SlicePath { get; set; } = string.Empty;

        public List<ShadowingTake> Takes { get; set; } = new();
    }

    /// <summary>
    /// 一条跟读录音；每段可保留多次，chosen 标记采用项。
    /// </summary>
    public class ShadowingTake
    {
        public string Id { get; set; } = string.Empty;

        /// <summary>相对 Shadowings 根目录的录音路径。</summary>
        public string Path { get; set; } = string.Empty;

        public int DurationMs { get; set; }

        /// <summary>未评分时为 null；UI 需区分“未评分”和 0 分。</summary>
        public int? Score { get; set; }

        /// <summary>pronunciation（逐词评分）/ reference（声学参考分）。</summary>
        public string ScoreKind { get; set; } = "reference";

        public bool Chosen { get; set; }

        /// <summary>用户手动“采用”的 take：后续新录音不再自动抢占。</summary>
        public bool ChosenManual { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 一次合成成品；segmentRefs / takeRefs 用于在源 take 被删除后仍能标注“源已变更”。
    /// </summary>
    public class ShadowingComposedAudio
    {
        public string Path { get; set; } = string.Empty;

        public int SegmentCount { get; set; }

        public List<string> SegmentRefs { get; set; } = new();

        public List<string> TakeRefs { get; set; } = new();

        public int DurationMs { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 跟读进度（按用户 + PDF）：记住每页练到哪一段、用什么模式。
    /// </summary>
    public class ShadowingProgress
    {
        public int SchemaVersion { get; set; } = 1;

        /// <summary>键为页码索引的字符串形式。</summary>
        public Dictionary<string, ShadowingPageProgress> Pages { get; set; } = new(StringComparer.Ordinal);
    }

    public class ShadowingPageProgress
    {
        public string? LastSegmentId { get; set; }

        /// <summary>single-loop / continuous / auto-next。</summary>
        public string? Mode { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
