using LearningAssistant.Services.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>批量导出文本的 AI 整理结果：Pages 始终与输入页数一致，失败页保留原始文本</summary>
    public sealed class PdfBatchTextCleanResult
    {
        /// <summary>是否全部批次都成功整理</summary>
        public bool Success { get; init; }

        /// <summary>首条失败原因（整理成功时为 null）</summary>
        public string? ErrorMessage { get; init; }

        /// <summary>整理后的页面文本（与输入等长）</summary>
        public IReadOnlyList<string> Pages { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// 批量 OCR 导出后的 AI 智能整理：标题层级、去页眉页脚、段落合并、术语统一。
    /// 任何一批失败都只回退该批的原始文本，不影响导出流程。
    /// </summary>
    public sealed class PdfBatchTextCleaner
    {
        /// <summary>单次请求最多整理的页数，避免提示词过长</summary>
        public const int PagesPerBatch = 5;

        /// <summary>单页送入 AI 的最大字符数</summary>
        public const int MaxCharsPerPage = 4000;

        /// <summary>单次 AI 请求超时时间（秒）</summary>
        public const int TimeoutSeconds = 120;

        private readonly IAIService? _aiService;
        private readonly ILogger<PdfBatchTextCleaner>? _logger;

        public PdfBatchTextCleaner(IAIService? aiService, ILogger<PdfBatchTextCleaner>? logger = null)
        {
            _aiService = aiService;
            _logger = logger;
        }

        /// <summary>
        /// 对导出的页面文本逐批做 AI 整理；AI 不可用或解析失败时返回原始文本。
        /// </summary>
        /// <param name="documentName">文档名（仅用于提示词）</param>
        /// <param name="pages">页面文本（0 基，已按导出顺序排列）</param>
        /// <param name="startPageIndex">首页页码（0 基），用于生成正确的页码锚点</param>
        public async Task<PdfBatchTextCleanResult> CleanAsync(
            string documentName,
            IReadOnlyList<string> pages,
            int startPageIndex,
            CancellationToken cancellationToken = default)
        {
            if (pages == null || pages.Count == 0)
                return new PdfBatchTextCleanResult { Success = false, Pages = Array.Empty<string>(), ErrorMessage = "没有可整理的页面" };

            if (_aiService == null)
            {
                return new PdfBatchTextCleanResult
                {
                    Success = false,
                    ErrorMessage = "AI 服务不可用，已保留原始文本",
                    Pages = pages.ToArray()
                };
            }

            var result = pages.ToArray();
            string? firstError = null;

            for (int offset = 0; offset < pages.Count; offset += PagesPerBatch)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = pages.Skip(offset).Take(PagesPerBatch).ToList();
                try
                {
                    var cleaned = await CleanBatchAsync(documentName, batch, startPageIndex + offset, cancellationToken)
                        .ConfigureAwait(false);
                    for (int i = 0; i < cleaned.Count && i < batch.Count; i++)
                    {
                        result[offset + i] = cleaned[i];
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // 单批失败只回退该批原始文本，不中断整个导出
                    firstError ??= ex.Message;
                    _logger?.LogWarning(ex, "AI 文本整理失败，回退原始文本: 第 {Start} 页起", startPageIndex + offset + 1);
                }
            }

            return new PdfBatchTextCleanResult
            {
                Success = firstError == null,
                ErrorMessage = firstError,
                Pages = result
            };
        }

        private async Task<IReadOnlyList<string>> CleanBatchAsync(
            string documentName,
            IReadOnlyList<string> batch,
            int startPageIndex,
            CancellationToken cancellationToken)
        {
            int firstPageNumber = startPageIndex + 1;
            int lastPageNumber = startPageIndex + batch.Count;

            var contentBuilder = new System.Text.StringBuilder();
            for (int i = 0; i < batch.Count; i++)
            {
                var text = batch[i] ?? string.Empty;
                text = text.Trim();
                if (text.Length > MaxCharsPerPage)
                    text = text[..MaxCharsPerPage] + "…";

                contentBuilder.AppendLine($"[[p{startPageIndex + i + 1}]]");
                contentBuilder.AppendLine(text);
                contentBuilder.AppendLine();
            }

            var prompt = $$"""
                你是文档整理助手。下面是 PDF《{{documentName}}》第 {{firstPageNumber}}-{{lastPageNumber}} 页的原始文本。
                请在保持原意和顺序的前提下整理：
                1. 识别标题层级，用 Markdown 标题（# / ## / ###）标记，不要新增不存在的内容；
                2. 去除重复出现的页眉、页脚、页码等版面噪声；
                3. 合并被硬换行拆断的段落，同一段落内不要保留多余换行；
                4. 统一同一术语的不同写法，保持前后一致。
                只输出严格 JSON 数组，不要输出解释或代码围栏：
                [{"page":{{firstPageNumber}},"text":"整理后的该页文本"}]
                要求：
                1. page 必须与给定页码一致，范围 {{firstPageNumber}}-{{lastPageNumber}}；
                2. 没有正文的页面 text 返回空字符串；
                3. 不要编造原文没有的信息。

                页面文本：
                {{contentBuilder}}
                """;

            var raw = await _aiService!
                .AskQuestionAsync(prompt, string.Empty, cancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(TimeoutSeconds), cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("AI 未返回整理结果");

            var json = StripCodeFence(raw);
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                throw new JsonException("AI 整理结果不是 JSON 数组");

            var cleanedByPage = new Dictionary<int, string>();
            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object) continue;
                if (!element.TryGetProperty("page", out var pageElement) || !pageElement.TryGetInt32(out int pageNumber))
                    continue;
                if (!element.TryGetProperty("text", out var textElement)) continue;

                cleanedByPage[pageNumber] = (textElement.GetString() ?? string.Empty).Trim();
            }

            if (cleanedByPage.Count == 0)
                throw new JsonException("AI 整理结果没有可用的页面文本");

            var cleaned = new string[batch.Count];
            for (int i = 0; i < batch.Count; i++)
            {
                // AI 漏掉某页时保留原始文本
                cleaned[i] = cleanedByPage.TryGetValue(startPageIndex + i + 1, out var text) ? text : batch[i];
            }

            return cleaned;
        }

        /// <summary>去掉 AI 可能附带的代码块围栏</summary>
        private static string StripCodeFence(string content)
        {
            var text = (content ?? string.Empty).Trim();
            if (!text.StartsWith("```", StringComparison.Ordinal)) return text;

            int firstLineEnd = text.IndexOf('\n');
            int lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
            if (firstLineEnd < 0 || lastFence <= firstLineEnd) return text;
            return text[(firstLineEnd + 1)..lastFence].Trim();
        }
    }
}
