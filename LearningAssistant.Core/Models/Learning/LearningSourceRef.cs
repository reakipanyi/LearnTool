namespace LearningAssistant.Models.Learning
{
    /// <summary>
    /// 学习项来源锚点：支持从学习窗口回跳到 PDF 原页/高亮/跟读句。
    /// 仅在对应来源存在时写入字段，其余保持为空。
    /// </summary>
    public class LearningSourceRef
    {
        public string? PdfPath { get; set; }
        public string? PdfHash { get; set; }
        public int PageIndex { get; set; } = -1;
        public string? HighlightId { get; set; }
        public string? AnnotationId { get; set; }
        public string? SentenceId { get; set; }

        public bool HasPdfAnchor => !string.IsNullOrWhiteSpace(PdfPath) && PageIndex >= 0;

        public bool HasAnyAnchor =>
            HasPdfAnchor ||
            !string.IsNullOrWhiteSpace(HighlightId) ||
            !string.IsNullOrWhiteSpace(AnnotationId) ||
            !string.IsNullOrWhiteSpace(SentenceId);
    }
}
