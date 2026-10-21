namespace LearningAssistant.Models.Pdf
{
    /// <summary>PDF AI 调用结果</summary>
    public sealed class PdfAiResponse
    {
        /// <summary>调用是否成功</summary>
        public bool Success { get; init; }

        /// <summary>AI 返回文本</summary>
        public string Content { get; init; } = string.Empty;

        /// <summary>答案中引用的页码（从 1 开始）</summary>
        public IReadOnlyList<int> ReferencedPages { get; init; } = Array.Empty<int>();

        /// <summary>是否为有效结构化内容（JSON）</summary>
        public bool IsStructured { get; init; }

        /// <summary>失败原因</summary>
        public string? ErrorMessage { get; init; }

        /// <summary>构造成功结果</summary>
        public static PdfAiResponse Ok(string content, IReadOnlyList<int>? referencedPages = null, bool isStructured = false)
            => new()
            {
                Success = true,
                Content = content ?? string.Empty,
                ReferencedPages = referencedPages ?? Array.Empty<int>(),
                IsStructured = isStructured
            };

        /// <summary>构造失败结果</summary>
        public static PdfAiResponse Fail(string errorMessage)
            => new() { Success = false, ErrorMessage = errorMessage };
    }

    /// <summary>PDF AI 编排参数</summary>
    public sealed class PdfAiOptions
    {
        /// <summary>每页参与上下文的字符上限</summary>
        public int MaxCharsPerPage { get; set; } = 4000;

        /// <summary>单次请求的上下文总字符预算</summary>
        public int MaxTotalChars { get; set; } = 24000;

        /// <summary>单次 AI 调用超时（秒）</summary>
        public int TimeoutSeconds { get; set; } = 90;
    }
}
