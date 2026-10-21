namespace LearningAssistant.Models.Pdf
{
    /// <summary>主题聚类下的一条标注（带页码与录音锚点）</summary>
    public sealed class PdfTopicClusterItem
    {
        public int PageIndex { get; init; }
        public long? TimestampMs { get; init; }
        public string? RecordingId { get; init; }
        public string Text { get; init; } = string.Empty;
    }

    /// <summary>标注主题聚类</summary>
    public sealed class PdfTopicCluster
    {
        public string Topic { get; init; } = string.Empty;
        public List<PdfTopicClusterItem> Items { get; init; } = new();
    }

    /// <summary>标注主题聚类结果</summary>
    public sealed class PdfTopicClusterResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public IReadOnlyList<PdfTopicCluster> Clusters { get; init; } = Array.Empty<PdfTopicCluster>();

        public static PdfTopicClusterResult Ok(IReadOnlyList<PdfTopicCluster> clusters)
            => new() { Success = true, Clusters = clusters };

        public static PdfTopicClusterResult Fail(string message)
            => new() { Success = false, ErrorMessage = message };
    }
}
