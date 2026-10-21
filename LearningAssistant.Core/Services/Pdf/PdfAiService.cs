using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.AI;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// PDF 专用 AI 编排实现：文本索引取料 → 上下文裁剪 → 现有 IAIService 调用 → 页码锚点解析。
    /// </summary>
    public sealed class PdfAiService : IPdfAiService
    {
        /// <summary>答案中的页码锚点，例如 [[p12]]</summary>
        private static readonly Regex PageAnchorRegex = new(@"\[\[p(\d+)\]\]", RegexOptions.Compiled);

        /// <summary>缓存 Key 中的提示词版本，提示词调整后递增可让旧缓存自然失效</summary>
        private const string CachePromptVersion = "pdf-ai-v1";

        /// <summary>缓存 Key 中的语言标记（当前 PDF AI 统一使用中文提示词）</summary>
        private const string CacheLanguage = "zh";

        private readonly IAIService? _aiService;
        private readonly IPdfDocumentTextIndex _textIndex;
        private readonly IPdfAiCacheService? _cacheService;
        private readonly PdfAiOptions _options;
        private readonly ILogger<PdfAiService>? _logger;

        public PdfAiService(
            IAIService? aiService,
            IPdfDocumentTextIndex textIndex,
            IPdfAiCacheService? cacheService = null,
            PdfAiOptions? options = null,
            ILogger<PdfAiService>? logger = null)
        {
            _aiService = aiService;
            _textIndex = textIndex ?? throw new ArgumentNullException(nameof(textIndex));
            _cacheService = cacheService;
            _options = options ?? new PdfAiOptions();
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<PdfAiResponse> SummarizePagesAsync(
            string pdfPath,
            int startPageIndex,
            int endPageIndex,
            CancellationToken cancellationToken = default)
            => ExecuteAsync(async () =>
            {
                var context = await BuildContextAsync(pdfPath, startPageIndex, endPageIndex, -1, cancellationToken)
                    .ConfigureAwait(false);
                if (context.IncludedPages.Count == 0)
                    return PdfAiResponse.Fail("指定页码范围内没有可用文本，请先完成 OCR 或检查文档");

                var prompt = $"""
                    你是文档摘要助手。请只依据下面的页面内容进行总结。
                    要求：
                    1. 使用简洁中文，按主题归纳要点；
                    2. 每条要点末尾必须标注来源页码，格式为 [[p12]]（页码从 1 开始）；
                    3. 不要编造页面中未出现的信息；
                    4. 最后给出 3-5 条整体结论。

                    文档内容：
                    {context.Context}
                    """;

                return await ExecutePromptAsync(context, "summarize", context.IncludedPages, prompt,
                    null, null, null, cancellationToken).ConfigureAwait(false);
            }, "总结", cancellationToken);

        /// <inheritdoc />
        public Task<PdfAiResponse> AskDocumentAsync(
            string pdfPath,
            string question,
            int currentPageIndex = -1,
            CancellationToken cancellationToken = default)
            => ExecuteAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(question))
                    return PdfAiResponse.Fail("问题内容为空");

                var context = await BuildContextAsync(pdfPath, 0, int.MaxValue, currentPageIndex, cancellationToken)
                    .ConfigureAwait(false);
                if (context.IncludedPages.Count == 0)
                    return PdfAiResponse.Fail("文档没有可用文本，请先完成 OCR");

                var prompt = $"""
                    你是文档问答助手。请只依据下面的页面内容回答问题。
                    要求：
                    1. 每个结论后标注依据页码，格式为 [[p12]]（页码从 1 开始）；
                    2. 如果页面内容不足以回答，明确说明“未在文档中找到依据”，不要编造；
                    3. 回答简洁，必要时分点说明。

                    问题：{question}

                    文档内容：
                    {context.Context}
                    """;

                return await ExecutePromptAsync(context, "ask", context.IncludedPages, prompt,
                    null, question, null, cancellationToken).ConfigureAwait(false);
            }, "问答", cancellationToken);

        /// <inheritdoc />
        public Task<PdfAiResponse> AskDocumentWithRetrievalAsync(
            string pdfPath,
            string question,
            CancellationToken cancellationToken = default)
            => ExecuteAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(question))
                    return PdfAiResponse.Fail("问题内容为空");

                var index = await _textIndex.GetOrBuildAsync(pdfPath, progress: null,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                if (index.PageCount <= 0)
                    return PdfAiResponse.Fail("文档没有可用文本，请先完成 OCR");

                var retrieval = PdfKeywordRetriever.Retrieve(index, question);
                if (!retrieval.HasMatch)
                {
                    return PdfAiResponse.Ok("未在文档中找到依据（已检索全文，未发现与该问题相关的页面）。");
                }

                var result = PdfAiContextBuilder.BuildForPages(
                    index,
                    retrieval.PageIndexes,
                    _options.MaxCharsPerPage,
                    _options.MaxTotalChars);

                if (result.IncludedPages.Count == 0)
                    return PdfAiResponse.Fail("召回页面没有可用文本，请先完成 OCR");

                var context = new PdfAiContext(result.Context, result.IncludedPages, index.PageCount, index.Fingerprint);

                var prompt = $"""
                    你是文档问答助手。下面是根据问题检索到的文档页面，请只依据这些内容回答。
                    要求：
                    1. 每个结论后标注依据页码，格式为 [[p12]]（页码从 1 开始）；
                    2. 如果这些页面不足以回答，明确说明“未在文档中找到依据”，不要编造；
                    3. 回答简洁，必要时分点说明。

                    问题：{question}

                    检索到的文档内容：
                    {context.Context}
                    """;

                var extra = $"retrieval|{question}|{string.Join(",", retrieval.Keywords)}";
                return await ExecutePromptAsync(context, "ask-retrieval", context.IncludedPages, prompt,
                    null, extra, null, cancellationToken).ConfigureAwait(false);
            }, "文档问答", cancellationToken);

        /// <inheritdoc />
        public Task<PdfAiResponse> AskScopedAsync(
            string pdfPath,
            string question,
            PdfAiScope scope,
            int currentPageIndex,
            int startPageIndex,
            int endPageIndex,
            string? selectedText = null,
            CancellationToken cancellationToken = default)
            => ExecuteAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(question))
                    return PdfAiResponse.Fail("问题内容为空");

                var context = await BuildScopedContextAsync(pdfPath, scope, currentPageIndex,
                    startPageIndex, endPageIndex, selectedText, cancellationToken).ConfigureAwait(false);
                if (context.IncludedPages.Count == 0)
                    return PdfAiResponse.Fail("所选作用域没有可用文本，请先完成 OCR 或切换范围");

                var prompt = $"""
                    你是文档问答助手。请只依据下面给定范围内的内容回答问题。
                    要求：
                    1. 每个结论后标注依据页码，格式为 [[p12]]（页码从 1 开始）；
                    2. 如果给定内容不足以回答，明确说明“未在文档中找到依据”，不要编造；
                    3. 回答简洁，必要时分点说明。

                    问题：{question}

                    文档内容：
                    {context.Context}
                    """;

                var extra = $"{scope}|{selectedText}|{question}";
                return await ExecutePromptAsync(context, "ask-scoped", context.IncludedPages, prompt,
                    null, extra, null, cancellationToken).ConfigureAwait(false);
            }, "问答", cancellationToken);

        /// <inheritdoc />
        public Task<PdfAiResponse> ExplainSelectionAsync(
            string pdfPath,
            int pageIndex,
            string selectedText,
            CancellationToken cancellationToken = default)
            => ExecuteAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(selectedText))
                    return PdfAiResponse.Fail("选中文本为空");

                var context = await BuildContextAsync(pdfPath, pageIndex, pageIndex, -1, cancellationToken)
                    .ConfigureAwait(false);
                var pageText = context.IncludedPages.Count > 0 ? context.Context : string.Empty;

                var prompt = $"""
                    请结合页面上下文解释下面的选中内容。
                    要求：
                    1. 说明含义、作用和上下文关系，必要时给出例子；
                    2. 只输出解释正文，不要重复粘贴整页原文；
                    3. 控制在 300 字以内。

                    选中内容：
                    {selectedText.Trim()}

                    所在页（第 {pageIndex + 1} 页）上下文：
                    {pageText}
                    """;

                return await ExecutePromptAsync(context, "explain", new[] { pageIndex + 1 }, prompt,
                    null, selectedText, new[] { pageIndex + 1 }, cancellationToken).ConfigureAwait(false);
            }, "解释", cancellationToken);

        /// <inheritdoc />
        public Task<PdfAiResponse> GenerateOutlineAsync(
            string pdfPath,
            int startPageIndex,
            int endPageIndex,
            CancellationToken cancellationToken = default)
            => ExecuteAsync(async () =>
            {
                var context = await BuildContextAsync(pdfPath, startPageIndex, endPageIndex, -1, cancellationToken)
                    .ConfigureAwait(false);
                if (context.IncludedPages.Count == 0)
                    return PdfAiResponse.Fail("指定页码范围内没有可用文本");

                var prompt = $$"""
                    请为下面的文档内容生成层级大纲。
                    只输出严格 JSON，不要 Markdown 代码块，不要额外说明。
                    JSON 格式：
                    {"title":"文档标题","sections":[{"title":"章节标题","page":3,"points":["要点1","要点2"]}]}
                    其中 page 为页码数字（从 1 开始）。不要编造未出现的信息。

                    文档内容：
                    {{context.Context}}
                    """;

                return await ExecutePromptAsync(context, "outline", context.IncludedPages, prompt,
                    JsonValueKind.Object, null, null, cancellationToken).ConfigureAwait(false);
            }, "大纲生成", cancellationToken);

        /// <inheritdoc />
        public Task<PdfAiResponse> GenerateStudyNotesAsync(
            string pdfPath,
            int startPageIndex,
            int endPageIndex,
            CancellationToken cancellationToken = default)
            => ExecuteAsync(async () =>
            {
                var context = await BuildContextAsync(pdfPath, startPageIndex, endPageIndex, -1, cancellationToken)
                    .ConfigureAwait(false);
                if (context.IncludedPages.Count == 0)
                    return PdfAiResponse.Fail("指定页码范围内没有可用文本");

                var prompt = $"""
                    请把下面的文档内容整理成结构化学习笔记。
                    要求：
                    1. 使用 Markdown，按章节使用 ## / ### 标题；
                    2. 每段关键内容后标注来源页码，格式为 [[p12]]（页码从 1 开始）；
                    3. 包含“核心概念”“要点”“疑问”三个部分；
                    4. 只基于给定内容，不要编造。

                    文档内容：
                    {context.Context}
                    """;

                return await ExecutePromptAsync(context, "notes", context.IncludedPages, prompt,
                    null, null, null, cancellationToken).ConfigureAwait(false);
            }, "笔记生成", cancellationToken);

        /// <inheritdoc />
        public Task<PdfAiResponse> GenerateFlashcardsAsync(
            string pdfPath,
            int startPageIndex,
            int endPageIndex,
            CancellationToken cancellationToken = default)
            => ExecuteAsync(async () =>
            {
                var context = await BuildContextAsync(pdfPath, startPageIndex, endPageIndex, -1, cancellationToken)
                    .ConfigureAwait(false);
                if (context.IncludedPages.Count == 0)
                    return PdfAiResponse.Fail("指定页码范围内没有可用文本");

                var prompt = $$"""
                    请根据下面的文档内容生成闪卡。
                    只输出严格 JSON 数组，不要 Markdown 代码块，不要额外说明。
                    JSON 格式：
                    [{"front":"问题或概念","back":"答案或解释","page":3}]
                    其中 page 为页码数字（从 1 开始）。每张卡片聚焦一个知识点，不要编造。

                    文档内容：
                    {{context.Context}}
                    """;

                return await ExecutePromptAsync(context, "flashcards", context.IncludedPages, prompt,
                    JsonValueKind.Array, null, null, cancellationToken).ConfigureAwait(false);
            }, "闪卡生成", cancellationToken);

        /// <inheritdoc />
        public Task<PdfAiResponse> SummarizeAnnotationsAsync(
            string pdfPath,
            IReadOnlyList<PdfAnnotationDigestItem> items,
            CancellationToken cancellationToken = default)
            => ExecuteAsync(async () =>
            {
                if (items == null || items.Count == 0)
                    return PdfAiResponse.Fail("当前页码范围没有可整理的标注");

                var digest = PdfAnnotationDigestBuilder.Build(items);
                if (string.IsNullOrWhiteSpace(digest))
                    return PdfAiResponse.Fail("标注内容为空，无法整理");

                var pages = items
                    .Select(i => i.PageIndex + 1)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToArray();

                string fingerprint;
                try
                {
                    fingerprint = _textIndex.GetFingerprint(pdfPath);
                }
                catch (FileNotFoundException)
                {
                    fingerprint = pdfPath;
                }

                var context = new PdfAiContext(digest, pages, pages.Length > 0 ? pages[^1] : 0, fingerprint);

                var prompt = $"""
                    你是学习笔记整理助手。下面是用户从 PDF 中做的标注条目（含页码与录音时间戳）。
                    请将它们整理成 Markdown，固定输出三个小节：
                    ### 要点
                    ### 待办
                    ### 疑问
                    要求：
                    1. 每条以 [[pN]] 开头（页码从 1 开始），条目带时间戳时追加 [mm:ss]；
                    2. 只有明确的任务才放进“待办”，不确定的放“要点”；
                    3. 没有某一类内容时写“- 无”；
                    4. 不要编造标注中不存在的信息。

                    标注条目：
                    {digest}
                    """;

                var extra = "annotations|" + ShortHash(digest);
                return await ExecutePromptAsync(context, "annotation-digest", pages, prompt,
                    null, extra, null, cancellationToken).ConfigureAwait(false);
            }, "标注整理", cancellationToken);

        /// <inheritdoc />
        public async Task<PdfAiSummaryEntriesResult> GenerateSummaryEntriesAsync(
            string pdfPath,
            IReadOnlyList<PdfAnnotationDigestItem> items,
            CancellationToken cancellationToken = default)
        {
            if (_aiService == null)
                return PdfAiSummaryEntriesResult.Fail("AI 服务不可用，请先完成 AI 配置");
            if (items == null || items.Count == 0)
                return PdfAiSummaryEntriesResult.Fail("当前文档没有可整理的标注");

            var digest = PdfAnnotationDigestBuilder.Build(items);
            if (string.IsNullOrWhiteSpace(digest))
                return PdfAiSummaryEntriesResult.Fail("标注内容为空，无法生成摘要");

            var prompt = $$"""
                你是学习摘要助手。下面是用户从 PDF 中做的标注条目（含页码与录音时间戳）。
                请把标注归纳为 3-8 条摘要，每条尽量简短，只依据标注内容，不要编造。
                只输出 JSON 数组，不要输出任何解释或代码围栏：
                [{"text":"摘要内容","page":3,"timeMs":65000}]
                说明：
                1. page 为页码（从 1 开始）；
                2. timeMs 为该条对应的标注时间戳（毫秒），标注没有时间戳时省略该字段；
                3. 同一标注不要重复输出。

                标注条目：
                {{digest}}
                """;

            try
            {
                var raw = await AskAsync(prompt, cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(raw))
                    return PdfAiSummaryEntriesResult.Fail("AI 未返回内容");

                var json = StripCodeFence(raw);
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                    return PdfAiSummaryEntriesResult.Fail("AI 返回格式不正确");

                var entries = new List<PdfAiSummaryEntry>();
                foreach (var element in document.RootElement.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.Object) continue;

                    var text = element.TryGetProperty("text", out var textElement)
                        ? (textElement.GetString() ?? string.Empty).Trim()
                        : string.Empty;
                    if (string.IsNullOrWhiteSpace(text)) continue;

                    int pageNumber = element.TryGetProperty("page", out var pageElement)
                        && pageElement.TryGetInt32(out var parsedPage)
                            ? parsedPage
                            : 0;

                    long? timeMs = element.TryGetProperty("timeMs", out var timeElement)
                        && timeElement.ValueKind == JsonValueKind.Number
                        && timeElement.TryGetInt64(out var parsedTime)
                            ? parsedTime
                            : null;

                    var entry = BuildSummaryEntry(pdfPath, items, pageNumber, timeMs, text);
                    if (entry != null) entries.Add(entry);
                }

                return entries.Count == 0
                    ? PdfAiSummaryEntriesResult.Fail("AI 未返回有效的摘要条目")
                    : PdfAiSummaryEntriesResult.Ok(entries);
            }
            catch (OperationCanceledException)
            {
                return PdfAiSummaryEntriesResult.Fail("AI 请求已取消或超时");
            }
            catch (JsonException ex)
            {
                _logger?.LogWarning(ex, "AI 摘要条目 JSON 解析失败");
                return PdfAiSummaryEntriesResult.Fail("AI 返回的摘要格式无法解析");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成 AI 摘要条目失败");
                return PdfAiSummaryEntriesResult.Fail(ex.Message);
            }
        }

        /// <summary>把 AI 摘要条目映射回原标注的页码/录音锚点</summary>
        private static PdfAiSummaryEntry? BuildSummaryEntry(
            string pdfPath,
            IReadOnlyList<PdfAnnotationDigestItem> items,
            int pageNumber,
            long? timeMs,
            string text)
        {
            int pageIndex = pageNumber - 1;
            var candidates = items.Where(i => i.PageIndex == pageIndex).ToList();

            // AI 页码不可信时，退回到最近一条带锚点的标注
            if (candidates.Count == 0)
            {
                var fallbackItem = items.FirstOrDefault(i => i.AudioTimestampMs.HasValue) ?? items.FirstOrDefault();
                if (fallbackItem == null) return null;

                pageIndex = fallbackItem.PageIndex;
                candidates = items.Where(i => i.PageIndex == pageIndex).ToList();
            }

            PdfAnnotationDigestItem? anchor = null;
            if (timeMs.HasValue)
            {
                anchor = candidates
                    .Where(i => i.AudioTimestampMs.HasValue)
                    .OrderBy(i => Math.Abs(i.AudioTimestampMs!.Value - timeMs.Value))
                    .FirstOrDefault();
            }

            anchor ??= candidates.FirstOrDefault(i => i.AudioTimestampMs.HasValue);
            anchor ??= candidates.FirstOrDefault();

            return new PdfAiSummaryEntry
            {
                PdfPath = pdfPath,
                PageIndex = Math.Max(0, pageIndex),
                RecordingId = anchor?.AudioRecordingId,
                TimestampMs = anchor?.AudioTimestampMs,
                Text = text.Length > PdfAnnotationDigestBuilder.MaxTextLength
                    ? text[..PdfAnnotationDigestBuilder.MaxTextLength] + "…"
                    : text
            };
        }

        private static string ShortHash(string text)
        {
            var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(text));
            return Convert.ToHexString(bytes)[..16];
        }

        private async Task<PdfAiResponse> ExecuteAsync(
            Func<Task<PdfAiResponse>> action,
            string operationName,
            CancellationToken cancellationToken)
        {
            if (_aiService == null)
                return PdfAiResponse.Fail("AI 服务不可用，请先完成 AI 配置");

            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return PdfAiResponse.Fail($"{operationName}已取消");
            }
            catch (TimeoutException)
            {
                return PdfAiResponse.Fail($"{operationName}超时，请稍后重试");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "PDF AI {Operation}失败", operationName);
                return PdfAiResponse.Fail($"{operationName}失败：{ex.Message}");
            }
        }

        private async Task<string> AskAsync(string prompt, CancellationToken cancellationToken)
        {
            int timeoutSeconds = Math.Max(5, _options.TimeoutSeconds);
            return await _aiService!
                .AskQuestionAsync(prompt, string.Empty, cancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// 先查磁盘缓存，未命中再调用 AI；成功后回写缓存。
        /// </summary>
        private async Task<PdfAiResponse> ExecutePromptAsync(
            PdfAiContext context,
            string feature,
            IReadOnlyList<int> cachePages,
            string prompt,
            JsonValueKind? expectedJsonKind,
            string? extra,
            IReadOnlyList<int>? forcedPages,
            CancellationToken cancellationToken)
        {
            string? cacheKey = null;
            if (_cacheService != null && _aiService != null)
            {
                cacheKey = _cacheService.BuildKey(
                    context.Fingerprint,
                    cachePages,
                    feature,
                    CacheLanguage,
                    CachePromptVersion,
                    _aiService.ModelName,
                    extra);

                if (_cacheService.TryGet(cacheKey, out var cached))
                {
                    return BuildResponse(cached, context.PageCount, expectedJsonKind, forcedPages);
                }
            }

            var content = await AskAsync(prompt, cancellationToken).ConfigureAwait(false);
            if (cacheKey != null && !string.IsNullOrWhiteSpace(content))
            {
                _cacheService!.Set(cacheKey, content);
            }

            return BuildResponse(content, context.PageCount, expectedJsonKind, forcedPages);
        }

        private static PdfAiResponse BuildResponse(
            string content,
            int pageCount,
            JsonValueKind? expectedJsonKind,
            IReadOnlyList<int>? forcedPages)
        {
            bool structured = expectedJsonKind.HasValue && IsValidJson(content, expectedJsonKind.Value);
            var pages = forcedPages ?? ExtractPageAnchors(content, pageCount);
            return PdfAiResponse.Ok(content, pages, structured);
        }

        /// <summary>按作用域构造上下文（选中 / 本页 / 指定页 / 全文）</summary>
        private async Task<PdfAiContext> BuildScopedContextAsync(
            string pdfPath,
            PdfAiScope scope,
            int currentPageIndex,
            int startPageIndex,
            int endPageIndex,
            string? selectedText,
            CancellationToken cancellationToken)
        {
            var index = await _textIndex.GetOrBuildAsync(pdfPath, progress: null, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var result = PdfAiContextBuilder.Build(
                index,
                scope,
                currentPageIndex,
                startPageIndex,
                endPageIndex,
                selectedText,
                _options.MaxCharsPerPage,
                _options.MaxTotalChars);

            return new PdfAiContext(result.Context, result.IncludedPages, index.PageCount, index.Fingerprint);
        }

        /// <summary>按页面顺序或当前页优先构造裁剪后的上下文</summary>
        private async Task<PdfAiContext> BuildContextAsync(
            string pdfPath,
            int startPageIndex,
            int endPageIndex,
            int priorityPageIndex,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var index = await _textIndex.GetOrBuildAsync(pdfPath, progress: null, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            int pageCount = index.PageCount;
            if (pageCount <= 0)
                return new PdfAiContext(string.Empty, Array.Empty<int>(), 0, string.Empty);

            int start = Math.Max(0, startPageIndex);
            int end = endPageIndex == int.MaxValue ? pageCount - 1 : Math.Min(pageCount - 1, endPageIndex);
            if (end < start)
                return new PdfAiContext(string.Empty, Array.Empty<int>(), pageCount, index.Fingerprint);

            var order = priorityPageIndex >= start && priorityPageIndex <= end
                ? BuildPriorityOrder(priorityPageIndex, start, end)
                : Enumerable.Range(start, end - start + 1);

            var builder = new StringBuilder();
            var includedPages = new List<int>();
            foreach (int pageIndex in order)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var text = index.GetPage(pageIndex)?.Text;
                if (string.IsNullOrWhiteSpace(text)) continue;

                int remaining = _options.MaxTotalChars - builder.Length;
                if (remaining <= 80) break;

                int pageLimit = Math.Min(_options.MaxCharsPerPage, remaining);
                var pageText = text.Trim();
                if (pageText.Length > pageLimit)
                {
                    pageText = pageText[..pageLimit] + "…";
                }

                var block = $"[[p{pageIndex + 1}]]\n{pageText}\n\n";
                if (block.Length > remaining)
                {
                    block = block[..remaining];
                }

                builder.Append(block);
                includedPages.Add(pageIndex + 1);
            }

            return new PdfAiContext(builder.ToString(), includedPages, pageCount, index.Fingerprint);
        }

        /// <summary>当前页优先，再向两侧扩展的取页顺序</summary>
        private static IEnumerable<int> BuildPriorityOrder(int priority, int start, int end)
        {
            yield return priority;
            for (int distance = 1; distance <= Math.Max(priority - start, end - priority); distance++)
            {
                int after = priority + distance;
                if (after <= end) yield return after;

                int before = priority - distance;
                if (before >= start) yield return before;
            }
        }

        /// <summary>提取并去重答案中的页码锚点（过滤越界页码）</summary>
        private static IReadOnlyList<int> ExtractPageAnchors(string content, int pageCount)
        {
            if (string.IsNullOrWhiteSpace(content)) return Array.Empty<int>();

            var pages = new SortedSet<int>();
            foreach (Match match in PageAnchorRegex.Matches(content))
            {
                if (int.TryParse(match.Groups[1].Value, out int page) && page >= 1 && page <= pageCount)
                {
                    pages.Add(page);
                }
            }
            return pages.ToArray();
        }

        /// <summary>去除代码块围栏后校验 JSON 根节点类型</summary>
        private static bool IsValidJson(string content, JsonValueKind expectedKind)
        {
            var json = StripCodeFence(content);
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                using var document = JsonDocument.Parse(json);
                return document.RootElement.ValueKind == expectedKind;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>去掉 AI 可能附带的 ```json 代码块围栏</summary>
        private static string StripCodeFence(string content)
        {
            var text = (content ?? string.Empty).Trim();
            if (!text.StartsWith("```", StringComparison.Ordinal)) return text;

            int firstLineEnd = text.IndexOf('\n');
            int lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
            if (firstLineEnd < 0 || lastFence <= firstLineEnd) return text;
            return text[(firstLineEnd + 1)..lastFence].Trim();
        }

        private sealed record PdfAiContext(string Context, IReadOnlyList<int> IncludedPages, int PageCount, string Fingerprint);
    }
}
