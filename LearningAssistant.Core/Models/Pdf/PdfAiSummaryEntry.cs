namespace LearningAssistant.Models.Pdf
{
    /// <summary>
    /// AI 摘要条目：带页码与录音锚点，可在「我的录音」页点击跳转并从时间戳回放。
    /// </summary>
    public class PdfAiSummaryEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PdfPath { get; set; } = string.Empty;
        /// <summary>页码（从 0 开始）</summary>
        public int PageIndex { get; set; }
        /// <summary>对应录音 Id，无录音锚点时为空</summary>
        public string? RecordingId { get; set; }
        /// <summary>录音时间戳（毫秒），无锚点时为空</summary>
        public long? TimestampMs { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>单个 PDF 的 AI 摘要集合（落盘结构）</summary>
    public class PdfAiSummaryCollection
    {
        public string PdfPath { get; set; } = string.Empty;
        public List<PdfAiSummaryEntry> Entries { get; set; } = new();
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>AI 摘要条目生成结果</summary>
    public sealed class PdfAiSummaryEntriesResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public IReadOnlyList<PdfAiSummaryEntry> Entries { get; init; } = Array.Empty<PdfAiSummaryEntry>();

        public static PdfAiSummaryEntriesResult Ok(IReadOnlyList<PdfAiSummaryEntry> entries)
            => new() { Success = true, Entries = entries };

        public static PdfAiSummaryEntriesResult Fail(string message)
            => new() { Success = false, ErrorMessage = message };
    }
}
