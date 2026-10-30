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

        /// <summary>本次请求涉及的文档总页数</summary>
        public int TotalPages { get; init; }

        /// <summary>实际纳入上下文的页码（从 1 开始）</summary>
        public IReadOnlyList<int> CoveredPages { get; init; } = Array.Empty<int>();

        /// <summary>请求范围内未纳入上下文的页码（无文本层且 OCR 失败、或超出预算）</summary>
        public IReadOnlyList<int> MissingPages { get; init; } = Array.Empty<int>();

        /// <summary>是否为部分覆盖（存在未纳入的页）</summary>
        public bool IsPartial { get; init; }

        /// <summary>覆盖范围提示，供 UI 直接展示</summary>
        public string? CoverageWarning { get; init; }

        /// <summary>本次实际使用的模型名</summary>
        public string? ModelId { get; init; }

        /// <summary>本次实际使用的供应商标识</summary>
        public string? ProviderId { get; init; }

        /// <summary>答案中未能与上下文原文匹配的引用片段（可能被模型改写或编造）</summary>
        public IReadOnlyList<string> UnverifiedQuotes { get; init; } = Array.Empty<string>();

        /// <summary>引用校验提示，供 UI 直接展示；全部通过时为 null</summary>
        public string? CitationWarning { get; init; }

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

        /// <summary>复制一份并附加覆盖信息与模型信息</summary>
        public PdfAiResponse WithCoverage(
            int totalPages,
            IReadOnlyList<int> coveredPages,
            IReadOnlyList<int> missingPages,
            string? coverageWarning)
            => new()
            {
                Success = Success,
                Content = Content,
                ReferencedPages = ReferencedPages,
                IsStructured = IsStructured,
                ErrorMessage = ErrorMessage,
                TotalPages = Math.Max(0, totalPages),
                CoveredPages = coveredPages ?? Array.Empty<int>(),
                MissingPages = missingPages ?? Array.Empty<int>(),
                IsPartial = missingPages?.Count > 0,
                CoverageWarning = coverageWarning,
                ModelId = ModelId,
                ProviderId = ProviderId,
                UnverifiedQuotes = UnverifiedQuotes,
                CitationWarning = CitationWarning
            };

        /// <summary>复制一份并附加本次使用的模型/供应商</summary>
        public PdfAiResponse WithModel(string? modelId, string? providerId)
            => new()
            {
                Success = Success,
                Content = Content,
                ReferencedPages = ReferencedPages,
                IsStructured = IsStructured,
                ErrorMessage = ErrorMessage,
                TotalPages = TotalPages,
                CoveredPages = CoveredPages,
                MissingPages = MissingPages,
                IsPartial = IsPartial,
                CoverageWarning = CoverageWarning,
                ModelId = modelId,
                ProviderId = providerId,
                UnverifiedQuotes = UnverifiedQuotes,
                CitationWarning = CitationWarning
            };

        /// <summary>复制一份并附加引用校验结果</summary>
        public PdfAiResponse WithCitation(IReadOnlyList<string>? unverifiedQuotes)
        {
            var quotes = unverifiedQuotes ?? Array.Empty<string>();
            return new()
            {
                Success = Success,
                Content = Content,
                ReferencedPages = ReferencedPages,
                IsStructured = IsStructured,
                ErrorMessage = ErrorMessage,
                TotalPages = TotalPages,
                CoveredPages = CoveredPages,
                MissingPages = MissingPages,
                IsPartial = IsPartial,
                CoverageWarning = CoverageWarning,
                ModelId = ModelId,
                ProviderId = ProviderId,
                UnverifiedQuotes = quotes,
                CitationWarning = quotes.Count == 0
                    ? null
                    : $"有 {quotes.Count} 处引用未在原文中找到，可能为模型改写"
            };
        }
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

        /// <summary>分块 map-reduce 时每块的目标字符上限</summary>
        public int ChunkChars { get; set; } = 6000;

        /// <summary>分块时每块最多页数（保证页边界完整）</summary>
        public int ChunkMaxPages { get; set; } = 10;

        /// <summary>是否对超预算的长文档启用 map-reduce</summary>
        public bool EnableMapReduce { get; set; } = true;

        /// <summary>单文档每日 AI 调用次数上限；小于等于 0 表示不限制</summary>
        public int MaxDailyCallsPerDocument { get; set; } = 150;
    }

    /// <summary>流式问答的上下文元信息：上下文准备完成、开始生成时回调一次</summary>
    public sealed record PdfAiStreamContext(
        int TotalPages,
        IReadOnlyList<int> IncludedPages,
        IReadOnlyList<int> MissingPages,
        string ModelId,
        string ProviderId)
    {
        /// <summary>覆盖范围提示；完整覆盖时为 null</summary>
        public string? CoverageWarning => MissingPages.Count == 0
            ? null
            : $"本次仅使用 {IncludedPages.Count}/{IncludedPages.Count + MissingPages.Count} 页，" +
              $"第 {string.Join("、", MissingPages.Distinct().OrderBy(p => p).Take(8))} 页未纳入" +
              (MissingPages.Count > 8 ? " 等" : string.Empty) + "（无文本层或超出上下文预算）";
    }
}
