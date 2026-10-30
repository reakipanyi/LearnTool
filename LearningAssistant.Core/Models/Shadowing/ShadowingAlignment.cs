using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Models.Shadowing
{
    /// <summary>
    /// 一条可对齐的跟读句：文本来自页面分句，若匹配到高亮则携带高亮锚点。
    /// 跟读句按页面顺序排列，原声打点时按该顺序逐句绑定时间范围。
    /// </summary>
    public sealed class ShadowingCue
    {
        public int PageIndex { get; init; }

        /// <summary>页面分句 ID；同一页文本不变时保持稳定。</summary>
        public string SentenceId { get; init; } = string.Empty;

        public string Text { get; init; } = string.Empty;

        /// <summary>匹配到的高亮 ID；为空表示只使用页面文字。</summary>
        public string? HighlightId { get; init; }

        /// <summary>稳定锚点（V2）：匹配高亮时含高亮锚点，否则为页面文字锚点。</summary>
        public string AnchorKey { get; init; } = string.Empty;

        /// <summary>V1 兼容锚点（不持久化）：读取历史切片时双读使用；新打点不写。</summary>
        public string? LegacyAnchorKey { get; init; }

        /// <summary>highlight / auto-highlight / page-text。</summary>
        public string TextSource { get; init; } = "page-text";

        /// <summary>一条高亮拆成多句时的句内序号；页面分句默认 0。</summary>
        public int PartIndex { get; init; }
    }

    /// <summary>
    /// 本页原声对齐所需的音频与高亮上下文；由阅读器提供，跟读窗口只负责打点和切片。
    /// </summary>
    public sealed class ShadowingAlignmentContext
    {
        public string SourcePath { get; init; } = string.Empty;

        public string? SubtitlePath { get; init; }

        /// <summary>音频起始位置；分页音频为 0，整篇音频使用阅读器当前播放位置。</summary>
        public int StartMs { get; init; }

        public IReadOnlyList<PdfHighlight> Highlights { get; init; } = Array.Empty<PdfHighlight>();
    }
}
