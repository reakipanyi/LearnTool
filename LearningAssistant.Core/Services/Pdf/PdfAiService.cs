using LearningAssistant.Common;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.AI;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
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

        /// <summary>答案中的引号片段（中英文引号），用于与上下文原文比对</summary>
        private static readonly Regex QuoteSegmentRegex = new(@"[“""]([^”""\n]{6,200})[”""]", RegexOptions.Compiled);

        /// <summary>缓存 Key 中的语言标记（当前 PDF AI 统一使用中文提示词）</summary>
        private const string CacheLanguage = "zh";

        private readonly IAIService? _aiService;
        private readonly IAiChatCompletionService? _chatService;
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
            // 支持按次参数的 Provider 走新接口，不经过通用层 3 天缓存；否则回退旧方法
            _chatService = aiService as IAiChatCompletionService;
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
                // 超出单次上下文预算时自动分块摘要再归并，避免长文档被静默截断
                return await ExecuteDocumentWideAsync(
                    pdfPath,
                    startPageIndex,
                    endPageIndex,
                    PdfAiRequestProfiles.Summarize,
                    context => $"""
                        你是文档摘要助手。请只依据下面的页面内容进行总结。
                        要求：
                        1. 使用与文档一致的语言，按主题归纳要点；
                        2. 每条要点末尾必须标注来源页码，格式为 [[p12]]（页码从 1 开始）；
                        3. 不要编造页面中未出现的信息；
                        4. 最后给出 3-5 条整体结论。

                        {context.LanguageHint}

                        文档内容：
                        {context.Context}
                        """,
                    null,
                    cancellationToken).ConfigureAwait(false);
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

                    {context.LanguageHint}

                    问题：{question}

                    文档内容：
                    {context.Context}
                    """;

                return await ExecutePromptAsync(context, PdfAiRequestProfiles.Ask, context.IncludedPages, prompt,
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

                var context = new PdfAiContext(result.Context, result.IncludedPages, index.PageCount, index.Fingerprint,
                    BuildLanguageHint(index, result.IncludedPages));

                var prompt = $"""
                    你是文档问答助手。下面是根据问题检索到的文档页面，请只依据这些内容回答。
                    要求：
                    1. 每个结论后标注依据页码，格式为 [[p12]]（页码从 1 开始）；
                    2. 如果这些页面不足以回答，明确说明“未在文档中找到依据”，不要编造；
                    3. 回答简洁，必要时分点说明。

                    {context.LanguageHint}

                    问题：{question}

                    检索到的文档内容：
                    {context.Context}
                    """;

                var extra = $"retrieval|{question}|{string.Join(",", retrieval.Keywords)}";
                return await ExecutePromptAsync(context, PdfAiRequestProfiles.AskRetrieval, context.IncludedPages, prompt,
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

                var prompt = BuildScopedAskPrompt(context, question);

                var extra = $"{scope}|{selectedText}|{question}";
                return await ExecutePromptAsync(context, PdfAiRequestProfiles.AskScoped, context.IncludedPages, prompt,
                    null, extra, null, cancellationToken).ConfigureAwait(false);
            }, "问答", cancellationToken);

        /// <inheritdoc />
        public IAsyncEnumerable<string> AskScopedStreamAsync(
            string pdfPath,
            string question,
            PdfAiScope scope,
            int currentPageIndex,
            int startPageIndex,
            int endPageIndex,
            string? selectedText,
            Action<PdfAiStreamContext>? onContextReady,
            CancellationToken cancellationToken = default)
            => StreamScopedAsync(pdfPath, question, scope, currentPageIndex, startPageIndex,
                endPageIndex, selectedText, onContextReady, cancellationToken);

        /// <summary>流式问答主体：先准备上下文并回调覆盖范围，再逐段产出答案</summary>
        private async IAsyncEnumerable<string> StreamScopedAsync(
            string pdfPath,
            string question,
            PdfAiScope scope,
            int currentPageIndex,
            int startPageIndex,
            int endPageIndex,
            string? selectedText,
            Action<PdfAiStreamContext>? onContextReady,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (_aiService == null)
                throw new InvalidOperationException("AI 服务不可用，请先完成 AI 配置");
            if (string.IsNullOrWhiteSpace(question))
                throw new InvalidOperationException("问题内容为空");

            var context = await BuildScopedContextAsync(pdfPath, scope, currentPageIndex,
                startPageIndex, endPageIndex, selectedText, cancellationToken).ConfigureAwait(false);
            if (context.IncludedPages.Count == 0)
                throw new InvalidOperationException("所选作用域没有可用文本，请先完成 OCR 或切换范围");

            onContextReady?.Invoke(new PdfAiStreamContext(
                context.PageCount,
                context.IncludedPages,
                context.MissingPages ?? Array.Empty<int>(),
                ChatModelId ?? string.Empty,
                ChatProviderId ?? string.Empty));

            var prompt = BuildScopedAskPrompt(context, question);

            // 无流式能力的 Provider 退化为一次性返回，UI 仍按片段追加即可
            if (_chatService == null)
            {
                yield return await AskAsync(prompt, PdfAiRequestProfiles.AskScoped, cancellationToken)
                    .ConfigureAwait(false);
                yield break;
            }

            var options = PdfAiRequestProfiles.Get(PdfAiRequestProfiles.AskScoped);
            await foreach (var chunk in _chatService.StreamAsync(prompt, options, cancellationToken)
                               .ConfigureAwait(false))
            {
                yield return chunk;
            }
        }

        /// <summary>按作用域的问答提示词；流式与非流式共用，保证两者口径一致</summary>
        private static string BuildScopedAskPrompt(PdfAiContext context, string question) => $"""
            你是文档问答助手。请只依据下面给定范围内的内容回答问题。
            要求：
            1. 每个结论后标注依据页码，格式为 [[p12]]（页码从 1 开始）；
            2. 如果给定内容不足以回答，明确说明“未在文档中找到依据”，不要编造；
            3. 回答简洁，必要时分点说明。

            {context.LanguageHint}

            问题：{question}

            文档内容：
            {context.Context}
            """;

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

                    {context.LanguageHint}

                    选中内容：
                    {selectedText.Trim()}

                    所在页（第 {pageIndex + 1} 页）上下文：
                    {pageText}
                    """;

                return await ExecutePromptAsync(context, PdfAiRequestProfiles.Explain, new[] { pageIndex + 1 }, prompt,
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
                return await ExecuteDocumentWideAsync(
                    pdfPath,
                    startPageIndex,
                    endPageIndex,
                    PdfAiRequestProfiles.Outline,
                    context => $$"""
                        请为下面的文档内容生成层级大纲。
                        只输出严格 JSON，不要 Markdown 代码块，不要额外说明。
                        JSON 格式：
                        {"title":"文档标题","sections":[{"title":"章节标题","page":3,"points":["要点1","要点2"]}]}
                        其中 page 为页码数字（从 1 开始）。不要编造未出现的信息。

                        {{context.LanguageHint}}

                        文档内容：
                        {{context.Context}}
                        """,
                    JsonValueKind.Object,
                    cancellationToken).ConfigureAwait(false);
            }, "大纲生成", cancellationToken);

        /// <inheritdoc />
        public Task<PdfAiResponse> GenerateStudyNotesAsync(
            string pdfPath,
            int startPageIndex,
            int endPageIndex,
            CancellationToken cancellationToken = default)
            => ExecuteAsync(async () =>
            {
                return await ExecuteDocumentWideAsync(
                    pdfPath,
                    startPageIndex,
                    endPageIndex,
                    PdfAiRequestProfiles.Notes,
                    context => $"""
                        请把下面的文档内容整理成结构化学习笔记。
                        要求：
                        1. 使用 Markdown，按章节使用 ## / ### 标题；
                        2. 每段关键内容后标注来源页码，格式为 [[p12]]（页码从 1 开始）；
                        3. 包含“核心概念”“要点”“疑问”三个部分；
                        4. 只基于给定内容，不要编造。

                        {context.LanguageHint}

                        文档内容：
                        {context.Context}
                        """,
                    null,
                    cancellationToken).ConfigureAwait(false);
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

                    {{context.LanguageHint}}

                    文档内容：
                    {{context.Context}}
                    """;

                return await ExecutePromptAsync(context, PdfAiRequestProfiles.Flashcards, context.IncludedPages, prompt,
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

                var context = new PdfAiContext(digest, pages, pages.Length > 0 ? pages[^1] : 0, fingerprint, string.Empty);

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
                return await ExecutePromptAsync(context, PdfAiRequestProfiles.AnnotationDigest, pages, prompt,
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
                var raw = await AskAsync(prompt, PdfAiRequestProfiles.SummaryEntries, cancellationToken).ConfigureAwait(false);
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
            var (pageIndex, anchor) = ResolveAnchor(items, pageNumber, timeMs);
            if (pageIndex < 0) return null;

            return new PdfAiSummaryEntry
            {
                PdfPath = pdfPath,
                PageIndex = pageIndex,
                RecordingId = anchor?.AudioRecordingId,
                TimestampMs = anchor?.AudioTimestampMs,
                Text = ClampText(text)
            };
        }

        /// <summary>按页码 + 时间戳就近匹配原标注，返回页码与锚点</summary>
        private static (int PageIndex, PdfAnnotationDigestItem? Anchor) ResolveAnchor(
            IReadOnlyList<PdfAnnotationDigestItem> items,
            int pageNumber,
            long? timeMs)
        {
            int pageIndex = pageNumber - 1;
            var candidates = items.Where(i => i.PageIndex == pageIndex).ToList();

            // AI 页码不可信时，退回到最近一条带锚点的标注
            if (candidates.Count == 0)
            {
                var fallbackItem = items.FirstOrDefault(i => i.AudioTimestampMs.HasValue) ?? items.FirstOrDefault();
                if (fallbackItem == null) return (-1, null);

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

            return (Math.Max(0, pageIndex), anchor);
        }

        /// <inheritdoc />
        public async Task<PdfTopicClusterResult> ClusterAnnotationsAsync(
            string pdfPath,
            IReadOnlyList<PdfAnnotationDigestItem> items,
            CancellationToken cancellationToken = default)
        {
            if (_aiService == null)
                return PdfTopicClusterResult.Fail("AI 服务不可用，请先完成 AI 配置");
            if (items == null || items.Count == 0)
                return PdfTopicClusterResult.Fail("当前文档没有可聚合的标注");

            var digest = PdfAnnotationDigestBuilder.Build(items);
            if (string.IsNullOrWhiteSpace(digest))
                return PdfTopicClusterResult.Fail("标注内容为空，无法聚合");

            var prompt = $$"""
                你是学习内容整理助手。下面是用户从 PDF 各页做的标注条目（含页码、录音时间戳与文字）。
                请按主题把同一主题的标注聚合到一起，跨页的同主题标注必须归入同一个主题。
                只输出 JSON 数组，不要输出解释或代码围栏：
                [{"topic":"主题名称","items":[{"page":3,"timeMs":65000,"text":"标注内容"}]}]
                要求：
                1. topic 用简短名词短语概括该主题；
                2. page 为页码（从 1 开始），timeMs 为该标注的时间戳（毫秒，没有则省略）；
                3. 每条原始标注只能出现在一个主题下，不要重复；
                4. 主题数量控制在 3-8 个，无法归类的放入“其他”。

                标注条目：
                {{digest}}
                """;

            try
            {
                var raw = await AskAsync(prompt, PdfAiRequestProfiles.TopicCluster, cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(raw))
                    return PdfTopicClusterResult.Fail("AI 未返回内容");

                var json = StripCodeFence(raw);
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                    return PdfTopicClusterResult.Fail("AI 返回格式不正确");

                var clusters = new List<PdfTopicCluster>();
                foreach (var element in document.RootElement.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.Object) continue;

                    var topic = element.TryGetProperty("topic", out var topicElement)
                        ? (topicElement.GetString() ?? string.Empty).Trim()
                        : string.Empty;
                    if (string.IsNullOrWhiteSpace(topic)) continue;

                    var cluster = new PdfTopicCluster { Topic = topic };
                    if (element.TryGetProperty("items", out var itemsElement)
                        && itemsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var itemElement in itemsElement.EnumerateArray())
                        {
                            if (itemElement.ValueKind != JsonValueKind.Object) continue;

                            var text = itemElement.TryGetProperty("text", out var textElement)
                                ? (textElement.GetString() ?? string.Empty).Trim()
                                : string.Empty;

                            int pageNumber = itemElement.TryGetProperty("page", out var pageElement)
                                && pageElement.TryGetInt32(out var parsedPage)
                                    ? parsedPage
                                    : 0;

                            long? timeMs = itemElement.TryGetProperty("timeMs", out var timeElement)
                                && timeElement.ValueKind == JsonValueKind.Number
                                && timeElement.TryGetInt64(out var parsedTime)
                                    ? parsedTime
                                    : null;

                            var (pageIndex, anchor) = ResolveAnchor(items, pageNumber, timeMs);
                            if (pageIndex < 0) continue;

                            cluster.Items.Add(new PdfTopicClusterItem
                            {
                                PageIndex = pageIndex,
                                TimestampMs = anchor?.AudioTimestampMs ?? timeMs,
                                RecordingId = anchor?.AudioRecordingId,
                                Text = ClampText(text)
                            });
                        }
                    }

                    if (cluster.Items.Count > 0)
                    {
                        clusters.Add(cluster);
                    }
                }

                return clusters.Count == 0
                    ? PdfTopicClusterResult.Fail("AI 未返回有效的主题聚类")
                    : PdfTopicClusterResult.Ok(clusters);
            }
            catch (OperationCanceledException)
            {
                return PdfTopicClusterResult.Fail("AI 请求已取消或超时");
            }
            catch (JsonException ex)
            {
                _logger?.LogWarning(ex, "AI 主题聚类 JSON 解析失败");
                return PdfTopicClusterResult.Fail("AI 返回的聚类格式无法解析");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AI 标注主题聚类失败");
                return PdfTopicClusterResult.Fail(ex.Message);
            }
        }

        private static string ClampText(string text)
        {
            return text.Length > PdfAnnotationDigestBuilder.MaxTextLength
                ? text[..PdfAnnotationDigestBuilder.MaxTextLength] + "…"
                : text;
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

        /// <summary>供缓存键使用的模型身份：供应商 + 实际模型名</summary>
        private string ModelIdentity
            => $"{ChatProviderId ?? "unknown"}::{ChatModelId ?? "unknown"}";

        private string? ChatProviderId => _chatService?.ProviderId ?? _aiService?.ProviderId;
        private string? ChatModelId => _chatService?.ModelId ?? _aiService?.ModelId;

        /// <summary>
        /// 按功能参数调用 AI，并用本次请求的超时令牌约束等待；
        /// 超时与用户取消可区分，避免把二者混为同一条错误提示。
        /// </summary>
        private async Task<string> AskAsync(string prompt, string feature, CancellationToken cancellationToken)
        {
            if (_aiService == null)
                throw new InvalidOperationException("AI 服务不可用，请先完成 AI 配置");

            var options = PdfAiRequestProfiles.Get(feature);
            int timeoutSeconds = Math.Max(5, _options.TimeoutSeconds);

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                if (_chatService != null)
                    return await _chatService.CompleteAsync(prompt, options, timeoutSource.Token).ConfigureAwait(false);

                // 不支持按次参数的 Provider 退化为旧接口（仍受本次超时约束，且不再叠加 WaitAsync）
                return await _aiService
                    .AskQuestionAsync(prompt, string.Empty, timeoutSource.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"AI 请求超过 {timeoutSeconds} 秒未返回");
            }
        }

        /// <summary>
        /// 先查磁盘缓存，未命中再调用 AI；成功后回写缓存。
        /// 结构化输出解析失败时不写缓存，避免坏结果长期驻留。
        /// </summary>
        private async Task<PdfAiResponse> ExecutePromptAsync(
            PdfAiContext context,
            string feature,
            IReadOnlyList<int> cachePages,
            string prompt,
            JsonValueKind? expectedJsonKind,
            string? extra,
            IReadOnlyList<int>? forcedPages,
            CancellationToken cancellationToken,
            bool bypassCache = false,
            IReadOnlyList<int>? missingPages = null)
        {
            string? cacheKey = null;
            if (_cacheService != null && _aiService != null)
            {
                cacheKey = _cacheService.BuildKey(
                    context.Fingerprint,
                    cachePages,
                    feature,
                    CacheLanguage,
                    PdfAiRequestProfiles.GetVersion(feature),
                    ModelIdentity,
                    extra);

                if (!bypassCache && _cacheService.TryGet(cacheKey, out var cached))
                {
                    // 观测量：缓存命中意味着本次零调用零成本
                    _logger?.LogInformation("PDF AI 缓存命中: feature={Feature}", feature);
                    return BuildResponse(cached, context, expectedJsonKind, forcedPages, missingPages);
                }
            }

            // 成本上限：按文档 + 日期累计真实调用次数，超限时明确提示而不是静默烧额度
            if (!TryConsumeDailyBudget(context.Fingerprint, feature))
            {
                return PdfAiResponse.Fail(
                    $"该文档今日 AI 调用已达上限（{_options.MaxDailyCallsPerDocument} 次），明日自动重置；" +
                    "可改用“当前页/选中内容”范围减少调用量");
            }

            var content = await AskAsync(prompt, feature, cancellationToken).ConfigureAwait(false);

            bool structuredOk = !expectedJsonKind.HasValue || IsValidJson(content, expectedJsonKind.Value);
            if (cacheKey != null && structuredOk && !string.IsNullOrWhiteSpace(content))
            {
                _cacheService!.Set(cacheKey, content);
            }

            return BuildResponse(content, context, expectedJsonKind, forcedPages, missingPages);
        }

        private PdfAiResponse BuildResponse(
            string content,
            PdfAiContext context,
            JsonValueKind? expectedJsonKind,
            IReadOnlyList<int>? forcedPages,
            IReadOnlyList<int>? missingPages)
        {
            bool structured = expectedJsonKind.HasValue && IsValidJson(content, expectedJsonKind.Value);
            var pages = forcedPages ?? ExtractPageAnchors(content, context.PageCount);

            // 引用校验（P1-1）：页码必须落在本次上下文中，否则视为模型越界引用而丢弃
            if (forcedPages == null && context.IncludedPages.Count > 0)
            {
                var allowed = new HashSet<int>(context.IncludedPages);
                var filtered = pages.Where(allowed.Contains).ToArray();
                if (filtered.Length != pages.Count)
                    _logger?.LogDebug("丢弃上下文外页码引用 {Count} 条", pages.Count - filtered.Length);
                pages = filtered;
            }

            // 结构化输出（JSON 字段值）不做引号比对，避免把字段内容误判为引用
            var unverified = structured
                ? Array.Empty<string>()
                : ValidateQuotes(content, context.Context);
            if (unverified.Count > 0)
                _logger?.LogDebug("答案中存在 {Count} 处未匹配原文的引用", unverified.Count);

            var missing = missingPages ?? context.MissingPages ?? Array.Empty<int>();

            var response = PdfAiResponse.Ok(content, pages, structured);
            return response
                .WithCoverage(context.PageCount, context.IncludedPages, missing,
                    BuildCoverageWarning(context.IncludedPages.Count, missing))
                .WithModel(ChatModelId, ChatProviderId)
                .WithCitation(unverified);
        }

        /// <summary>校验答案中的引号片段能否在上下文中找到，返回未匹配片段</summary>
        private static IReadOnlyList<string> ValidateQuotes(string content, string contextText)
        {
            if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(contextText))
                return Array.Empty<string>();

            var normalizedContext = NormalizeForCitationMatch(contextText);
            if (normalizedContext.Length == 0) return Array.Empty<string>();

            var unverified = new List<string>();
            foreach (Match match in QuoteSegmentRegex.Matches(content))
            {
                var quote = match.Groups[1].Value.Trim();
                if (quote.Length < 6) continue;
                if (normalizedContext.Contains(NormalizeForCitationMatch(quote), StringComparison.Ordinal)) continue;

                unverified.Add(quote);
                if (unverified.Count >= 5) break;
            }
            return unverified;
        }

        /// <summary>引用比对用归一化：忽略大小写、空白与标点，避免排版差异造成误判</summary>
        private static string NormalizeForCitationMatch(string text)
        {
            var builder = new StringBuilder(text.Length);
            foreach (var ch in text.ToLowerInvariant())
            {
                if (char.IsWhiteSpace(ch) || char.IsPunctuation(ch) || char.IsSymbol(ch)) continue;
                builder.Append(ch);
            }
            return builder.ToString();
        }

        /// <summary>
        /// 按文档 + 日期累计真实调用次数；达到上限返回 false。
        /// 统计文件写失败时不阻断功能，避免因磁盘问题让 AI 完全不可用。
        /// </summary>
        private bool TryConsumeDailyBudget(string fingerprint, string feature)
        {
            int limit = _options.MaxDailyCallsPerDocument;
            if (limit <= 0 || string.IsNullOrWhiteSpace(fingerprint)) return true;

            try
            {
                AppPaths.EnsureDirectoryExists(AppPaths.PdfAiUsageDir);
                var tag = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)))[..12].ToLowerInvariant();
                var path = Path.Combine(AppPaths.PdfAiUsageDir, $"{tag}-{DateTime.Now:yyyyMMdd}.txt");

                int used = 0;
                if (File.Exists(path) && int.TryParse(File.ReadAllText(path).Trim(), out var parsed))
                    used = parsed;

                if (used >= limit)
                {
                    _logger?.LogWarning("PDF AI 当日调用达到上限: {Fingerprint}, feature={Feature}, used={Used}",
                        tag, feature, used);
                    return false;
                }

                File.WriteAllText(path, (used + 1).ToString());
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "记录 PDF AI 每日用量失败");
                return true;
            }
        }

        /// <summary>生成覆盖范围提示；完整覆盖时返回 null</summary>
        private static string? BuildCoverageWarning(int coveredCount, IReadOnlyList<int> missingPages)
        {
            if (missingPages.Count == 0) return null;

            int requested = coveredCount + missingPages.Count;
            return $"本次仅使用 {coveredCount}/{requested} 页，" +
                $"第 {FormatPageList(missingPages.Distinct().OrderBy(p => p).ToList())} 页未纳入" +
                "（无文本层或超出上下文预算）";
        }

        /// <summary>
        /// 长文档入口：预算内单次请求；超预算时先分块摘要再归并，并把覆盖页回传给 UI。
        /// </summary>
        private async Task<PdfAiResponse> ExecuteDocumentWideAsync(
            string pdfPath,
            int startPageIndex,
            int endPageIndex,
            string feature,
            Func<PdfAiContext, string> buildPrompt,
            JsonValueKind? expectedJsonKind,
            CancellationToken cancellationToken)
        {
            var index = await _textIndex
                .GetOrBuildAsync(pdfPath, progress: null, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            int pageCount = index.PageCount;
            if (pageCount <= 0)
                return PdfAiResponse.Fail("文档没有可用文本，请先完成 OCR");

            int start = Math.Max(0, startPageIndex);
            int end = endPageIndex == int.MaxValue || endPageIndex < 0
                ? pageCount - 1
                : Math.Min(pageCount - 1, endPageIndex);
            if (end < start)
                return PdfAiResponse.Fail("指定页码范围内没有可用文本");

            var requested = Enumerable.Range(start, end - start + 1).ToList();
            var textPages = requested
                .Where(i => !string.IsNullOrWhiteSpace(index.GetPage(i)?.Text))
                .ToList();
            if (textPages.Count == 0)
                return PdfAiResponse.Fail("指定页码范围内没有可用文本，请先完成 OCR 或检查文档");

            var missingPages = requested.Except(textPages).Select(i => i + 1).ToList();

            // 估算实际会进入上下文的字符数（每页按上限裁剪）
            long estimatedChars = textPages.Sum(i =>
                Math.Min(index.GetPage(i)!.Text.Trim().Length, _options.MaxCharsPerPage));

            if (!_options.EnableMapReduce || estimatedChars <= _options.MaxTotalChars)
            {
                var context = await BuildContextAsync(pdfPath, start, end, -1, cancellationToken)
                    .ConfigureAwait(false);
                return await ExecutePromptAsync(context, feature, context.IncludedPages, buildPrompt(context),
                    expectedJsonKind, null, null, cancellationToken, missingPages: missingPages)
                    .ConfigureAwait(false);
            }

            // 分块 map：按页边界聚合成块，每块摘要单独缓存，便于重复生成时直接命中
            var chunks = BuildChunks(index, textPages);
            var summaries = new List<ChunkSummary>(chunks.Count);
            foreach (var chunk in chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var chunkContext = BuildChunkContext(index, chunk);
                var chunkResponse = await ExecutePromptAsync(
                    chunkContext,
                    PdfAiRequestProfiles.ChunkSummary,
                    chunk,
                    BuildChunkSummaryPrompt(chunkContext),
                    null,
                    null,
                    null,
                    cancellationToken).ConfigureAwait(false);

                if (chunkResponse.Success && !string.IsNullOrWhiteSpace(chunkResponse.Content))
                    summaries.Add(new ChunkSummary(chunk, chunkResponse.Content));
            }

            if (summaries.Count == 0)
                return PdfAiResponse.Fail("分块摘要均失败，请检查 AI 配置后重试");

            var mergedText = await MergeChunkSummariesAsync(summaries, cancellationToken).ConfigureAwait(false);
            var coveredPages = textPages.Select(i => i + 1).OrderBy(p => p).ToList();

            var reduceContext = new PdfAiContext(
                mergedText,
                coveredPages,
                pageCount,
                index.Fingerprint,
                BuildLanguageHint(index, coveredPages),
                missingPages);

            return await ExecutePromptAsync(reduceContext, feature, coveredPages, buildPrompt(reduceContext),
                expectedJsonKind, null, null, cancellationToken, missingPages: missingPages)
                .ConfigureAwait(false);
        }

        /// <summary>按页边界切块：每块不超过 ChunkMaxPages 页且不超过 ChunkChars 字符</summary>
        private List<int[]> BuildChunks(PdfTextIndexDocument index, IReadOnlyList<int> textPages)
        {
            var chunks = new List<int[]>();
            var current = new List<int>();
            int currentChars = 0;
            int maxChars = Math.Max(1000, _options.ChunkChars);
            int maxPages = Math.Max(1, _options.ChunkMaxPages);

            foreach (int pageIndex in textPages)
            {
                int chars = Math.Min(index.GetPage(pageIndex)!.Text.Trim().Length, _options.MaxCharsPerPage);

                if (current.Count > 0 && (current.Count >= maxPages || currentChars + chars > maxChars))
                {
                    chunks.Add(current.ToArray());
                    current = new List<int>();
                    currentChars = 0;
                }

                current.Add(pageIndex);
                currentChars += chars;
            }

            if (current.Count > 0) chunks.Add(current.ToArray());
            return chunks;
        }

        private PdfAiContext BuildChunkContext(PdfTextIndexDocument index, IReadOnlyList<int> chunk)
        {
            var result = PdfAiContextBuilder.BuildForPages(
                index,
                chunk,
                _options.MaxCharsPerPage,
                Math.Max(_options.ChunkChars + 500, _options.MaxTotalChars / 3));

            return new PdfAiContext(result.Context, result.IncludedPages, index.PageCount, index.Fingerprint,
                BuildLanguageHint(index, result.IncludedPages));
        }

        /// <summary>块摘要提示词：只要要点与页码锚点，控制输出长度</summary>
        private static string BuildChunkSummaryPrompt(PdfAiContext context)
            => $"""
                你是文档分段摘要助手。请只依据下面这部分页面内容归纳要点。
                要求：
                1. 按主题归纳，每条要点末尾标注 [[p12]] 页码锚点（页码从 1 开始）；
                2. 不要编造页面中未出现的信息；
                3. 控制在 300 字以内，只输出摘要正文。

                {context.LanguageHint}

                页面内容：
                {context.Context}
                """;

        /// <summary>
        /// 归并块摘要：块摘要总量超预算时再做一层压缩，避免最终请求再次超出上下文。
        /// </summary>
        private async Task<string> MergeChunkSummariesAsync(
            IReadOnlyList<ChunkSummary> summaries,
            CancellationToken cancellationToken)
        {
            var combined = JoinChunkSummaries(summaries);
            if (combined.Length <= _options.MaxTotalChars)
                return combined;

            var groups = new List<string>();
            var buffer = new StringBuilder();
            foreach (var summary in summaries)
            {
                buffer.AppendLine($"【第 {summary.Pages[0] + 1}-{summary.Pages[^1] + 1} 页】");
                buffer.AppendLine(summary.Text.Trim());
                buffer.AppendLine();

                if (buffer.Length >= _options.ChunkChars)
                {
                    groups.Add(buffer.ToString());
                    buffer.Clear();
                }
            }
            if (buffer.Length > 0) groups.Add(buffer.ToString());

            var merged = new List<string>();
            for (int i = 0; i < groups.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var prompt = $"""
                    你是文档归并助手。下面是同一文档若干分段的摘要，请合并成一段不超过 400 字的总摘要。
                    要求：
                    1. 保留关键结论与页码锚点，格式 [[p12]]；
                    2. 合并重复内容，不要编造。

                    分段摘要：
                    {groups[i]}
                    """;

                var raw = await AskAsync(prompt, PdfAiRequestProfiles.ChunkSummary, cancellationToken)
                    .ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(raw)) merged.Add(raw.Trim());
            }

            if (merged.Count == 0) return combined;

            var result = string.Join("\n\n", merged);
            return result.Length > _options.MaxTotalChars
                ? result[.._options.MaxTotalChars] + "\n…（分段摘要过多，已按上下文预算截断）"
                : result;
        }

        private static string JoinChunkSummaries(IEnumerable<ChunkSummary> summaries)
        {
            var builder = new StringBuilder();
            foreach (var summary in summaries)
            {
                if (summary.Pages.Count == 0)
                {
                    builder.AppendLine(summary.Text.Trim()).AppendLine();
                    continue;
                }

                builder.AppendLine($"【第 {summary.Pages[0] + 1}-{summary.Pages[^1] + 1} 页】");
                builder.AppendLine(summary.Text.Trim());
                builder.AppendLine();
            }
            return builder.ToString().TrimEnd();
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
            // 选中内容作用域只需选中文本，跳过整份文档索引，避免首次提问时长时间等待
            if (scope == PdfAiScope.Selection && !string.IsNullOrWhiteSpace(selectedText))
            {
                int pageNumber = Math.Max(0, currentPageIndex) + 1;
                string fingerprint;
                try
                {
                    fingerprint = _textIndex.GetFingerprint(pdfPath);
                }
                catch (FileNotFoundException)
                {
                    fingerprint = pdfPath;
                }

                return new PdfAiContext(
                    $"[[p{pageNumber}]]\n{selectedText.Trim()}\n\n",
                    new[] { pageNumber },
                    // 上下文中只包含当前选中页，页码锚点不应超出该页；同时保留按原文语言作答的要求
                    pageNumber,
                    fingerprint,
                    BuildSelectionLanguageHint(selectedText));
            }

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

            // 作用域内没有进入上下文的页（无文本或超出预算）单独记录，供 UI 提示覆盖率
            var includedSet = result.IncludedPages.ToHashSet();
            var missing = ResolveRequestedPageIndexes(scope, currentPageIndex, startPageIndex, endPageIndex, index.PageCount)
                .Where(i => i >= 0 && i < index.PageCount && !includedSet.Contains(i + 1))
                .Select(i => i + 1)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            return new PdfAiContext(result.Context, result.IncludedPages, index.PageCount, index.Fingerprint,
                BuildLanguageHint(index, result.IncludedPages), missing);
        }

        /// <summary>按作用域解析请求范围内的页索引（从 0 开始），与 ContextBuilder 的取页口径保持一致</summary>
        private static IEnumerable<int> ResolveRequestedPageIndexes(
            PdfAiScope scope,
            int currentPageIndex,
            int startPageIndex,
            int endPageIndex,
            int pageCount)
        {
            if (pageCount <= 0) return Array.Empty<int>();

            int current = Math.Clamp(currentPageIndex, 0, pageCount - 1);
            int start = Math.Clamp(startPageIndex, 0, pageCount - 1);
            int end = Math.Clamp(endPageIndex, 0, pageCount - 1);

            return scope switch
            {
                PdfAiScope.CurrentPage => Enumerable.Range(Math.Max(0, current - 1),
                    Math.Min(pageCount - 1, current + 1) - Math.Max(0, current - 1) + 1),
                PdfAiScope.SpecifiedPages => end >= start
                    ? Enumerable.Range(start, end - start + 1)
                    : Enumerable.Empty<int>(),
                PdfAiScope.WholeDocument => Enumerable.Range(0, pageCount),
                _ => new[] { current }
            };
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
                return new PdfAiContext(string.Empty, Array.Empty<int>(), 0, string.Empty, string.Empty);

            int start = Math.Max(0, startPageIndex);
            int end = endPageIndex == int.MaxValue ? pageCount - 1 : Math.Min(pageCount - 1, endPageIndex);
            if (end < start)
                return new PdfAiContext(string.Empty, Array.Empty<int>(), pageCount, index.Fingerprint, string.Empty);

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

            var includedSet = includedPages.ToHashSet();
            var missing = Enumerable.Range(start, end - start + 1)
                .Where(i => !includedSet.Contains(i + 1))
                .Select(i => i + 1)
                .ToList();

            return new PdfAiContext(builder.ToString(), includedPages, pageCount, index.Fingerprint,
                BuildLanguageHint(index, includedPages), missing);
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

        /// <summary>
        /// 根据参与上下文的页面语言生成提示词语言要求；中英混排时按页说明，避免整篇强行统一语言。
        /// </summary>
        private static string BuildLanguageHint(PdfTextIndexDocument index, IReadOnlyList<int> includedPages)
        {
            if (includedPages == null || includedPages.Count == 0) return string.Empty;

            var chinesePages = new List<int>();
            var englishPages = new List<int>();
            var mixedPages = new List<int>();
            var unknownPages = new List<int>();

            foreach (int pageNumber in includedPages.Distinct().OrderBy(p => p))
            {
                switch (ResolvePageLanguage(index.GetPage(pageNumber - 1)))
                {
                    case LanguageType.Chinese:
                        chinesePages.Add(pageNumber);
                        break;
                    case LanguageType.English:
                        englishPages.Add(pageNumber);
                        break;
                    case LanguageType.Mixed:
                        mixedPages.Add(pageNumber);
                        break;
                    default:
                        unknownPages.Add(pageNumber);
                        break;
                }
            }

            if (chinesePages.Count == 0 && englishPages.Count == 0 && mixedPages.Count == 0)
                return "语言要求：文档语言未知，请按原文语言作答，并保留原文表述。";

            if (englishPages.Count == 0 && mixedPages.Count == 0 && unknownPages.Count == 0)
                return "语言要求：文档主要为中文，请使用中文作答，引用原文时保留原文语言。";

            if (chinesePages.Count == 0 && mixedPages.Count == 0 && unknownPages.Count == 0)
                return "语言要求：文档主要为英文，请使用英文作答，引用原文时保留原文语言。";

            var parts = new List<string>();
            if (chinesePages.Count > 0) parts.Add($"第 {FormatPageList(chinesePages)} 页为中文");
            if (englishPages.Count > 0) parts.Add($"第 {FormatPageList(englishPages)} 页为英文");
            if (mixedPages.Count > 0) parts.Add($"第 {FormatPageList(mixedPages)} 页为中英混排");

            return $"语言要求：文档为中英混排（{string.Join("，", parts)}）。请按各页原文语言分别处理，"
                + "引用原文时保留原文语言，不要整篇强行统一语言。";
        }

        /// <summary>选中内容没有页级语言信息，按选中文本本身检测作答语言</summary>
        private static string BuildSelectionLanguageHint(string selectedText)
        {
            return StringLanguageDetector.DetectLanguage(selectedText) switch
            {
                LanguageType.English => "语言要求：选中的内容主要为英文，请使用英文作答，引用原文时保留原文语言。",
                LanguageType.Chinese => "语言要求：选中的内容主要为中文，请使用中文作答，引用原文时保留原文语言。",
                LanguageType.Mixed => "语言要求：选中的内容为中英混排，请按原文语言分别处理，引用原文时保留原文语言。",
                _ => "语言要求：选中内容的语言未知，请按原文语言作答，并保留原文表述。"
            };
        }

        /// <summary>优先使用索引中已记录的页级语言，缺失时按页面文本重新检测</summary>
        private static LanguageType ResolvePageLanguage(PdfTextIndexPage? page)
        {
            if (page == null) return LanguageType.Unknown;
            if (Enum.TryParse<LanguageType>(page.DetectedLanguage, out var parsed) && parsed != LanguageType.Unknown)
                return parsed;

            return StringLanguageDetector.DetectLanguage(page.Text);
        }

        /// <summary>页码列表过长时只列前 8 页，避免提示词膨胀</summary>
        private static string FormatPageList(List<int> pages)
        {
            const int maxListed = 8;
            var listed = pages.Take(maxListed).Select(p => p.ToString()).ToList();
            var text = string.Join("、", listed);
            return pages.Count > maxListed ? text + " 等" : text;
        }

        private sealed record PdfAiContext(
            string Context,
            IReadOnlyList<int> IncludedPages,
            int PageCount,
            string Fingerprint,
            string LanguageHint,
            IReadOnlyList<int>? MissingPages = null);

        /// <summary>分块摘要：Pages 为该块覆盖的页索引（从 0 开始）</summary>
        private sealed record ChunkSummary(IReadOnlyList<int> Pages, string Text);
    }
}
