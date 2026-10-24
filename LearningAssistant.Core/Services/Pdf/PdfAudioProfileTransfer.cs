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
}
