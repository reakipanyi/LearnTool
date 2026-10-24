using System.Text.Json.Nodes;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>导出结果：成功时 Json 为可直接写盘的档案内容。</summary>
    public sealed class PdfAudioExportResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public string? Json { get; init; }
        public string? SuggestedFileName { get; init; }

        public static PdfAudioExportResult Fail(string error) => new() { Error = error };

        public static PdfAudioExportResult Ok(string json, string fileName)
            => new() { Success = true, Json = json, SuggestedFileName = fileName };
    }

    /// <summary>
    /// 导入预览：解析与校验成功后给出摘要、逐项差异和警告；
    /// Root 保留原始 JsonObject，写入时据此按书合并并保留未知字段。
    /// </summary>
    public sealed class PdfAudioImportPreview
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        /// <summary>directory 或 book</summary>
        public string Kind { get; init; } = "";
        public string Title { get; init; } = "";
        public string Summary { get; init; } = "";
        public IReadOnlyList<string> Items { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public JsonObject? Root { get; init; }

        public static PdfAudioImportPreview Fail(string error) => new() { Error = error };
    }

    /// <summary>
    /// 疑似同一目录的旧档案：按 PDF 文件名集合的 Jaccard 重叠率找出，
    /// 供目录被移动/盘符变化后由用户确认重挂。
    /// </summary>
    public sealed class PdfAudioDirectoryReattachCandidate
    {
        public string ProfilePath { get; init; } = "";
        /// <summary>档案原记录目录；旧目录不可用时仅作展示</summary>
        public string DirectoryPath { get; init; } = "";
        /// <summary>当前目录与档案书名的 Jaccard 重叠率（0–1）</summary>
        public double OverlapRatio { get; init; }
        public int SharedBooks { get; init; }
        public int CurrentBookCount { get; init; }
    }
}
