using LearningAssistant.Common;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.AI;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// 术语表构建与翻译润色：术语表按文件指纹落盘缓存，同一文档所有页复用同一套译名。
    /// </summary>
    public sealed class PdfTranslationGlossaryService : IPdfTranslationGlossaryService
    {
        /// <summary>构建术语表时最多读取的页数</summary>
        public const int MaxPagesForGlossary = 12;

        /// <summary>构建术语表时单页最大字符数</summary>
        public const int MaxCharsPerPage = 3000;

        /// <summary>术语表条目上限</summary>
        public const int MaxGlossaryEntries = 40;

        /// <summary>润色请求超时时间（秒）</summary>
        public const int PolishTimeoutSeconds = 90;

        private readonly IPdfDocumentTextIndex _textIndex;
        private readonly IAIService? _aiService;
        private readonly ILogger<PdfTranslationGlossaryService>? _logger;
        private readonly string _cacheDirectory;
        private readonly Dictionary<string, PdfTranslationGlossary?> _memoryCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim _buildGate = new(1, 1);

        public PdfTranslationGlossaryService(
            IPdfDocumentTextIndex textIndex,
            IAIService? aiService = null,
            ILogger<PdfTranslationGlossaryService>? logger = null,
            string? cacheDirectory = null)
        {
            _textIndex = textIndex ?? throw new ArgumentNullException(nameof(textIndex));
            _aiService = aiService;
            _logger = logger;
            _cacheDirectory = cacheDirectory ?? AppPaths.GetUserPdfTranslationGlossaryDir();
        }

        /// <inheritdoc />
        public async Task<PdfTranslationGlossary?> GetOrBuildAsync(string pdfPath, CancellationToken cancellationToken = default)
        {
            if (_aiService == null) return null;

            string fingerprint;
            try
            {
                fingerprint = _textIndex.GetFingerprint(pdfPath);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "无法计算 PDF 指纹，跳过术语表");
                return null;
            }

            if (_memoryCache.TryGetValue(fingerprint, out var cached)) return cached;

            await _buildGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_memoryCache.TryGetValue(fingerprint, out cached)) return cached;

                var fromDisk = TryLoadFromDisk(fingerprint);
                if (fromDisk != null)
                {
                    _memoryCache[fingerprint] = fromDisk;
                    return fromDisk;
                }

                var built = await BuildGlossaryAsync(pdfPath, fingerprint, cancellationToken).ConfigureAwait(false);
                _memoryCache[fingerprint] = built;
                return built;
            }
            finally
            {
                _buildGate.Release();
            }
        }

        /// <inheritdoc />
        public async Task<PdfTranslationPolishResult> PolishAsync(
            string pdfPath,
            string originalText,
            string machineTranslation,
            CancellationToken cancellationToken = default)
        {
            if (_aiService == null)
                return PdfTranslationPolishResult.Fail(machineTranslation, "AI 服务不可用");
            if (string.IsNullOrWhiteSpace(originalText) || string.IsNullOrWhiteSpace(machineTranslation))
                return PdfTranslationPolishResult.Fail(machineTranslation, "原文或译文为空");

            var glossary = await GetOrBuildAsync(pdfPath, cancellationToken).ConfigureAwait(false);
            var entries = glossary?.Entries ?? new List<PdfGlossaryEntry>();

            var glossaryBlock = entries.Count == 0
                ? "（本页未匹配到术语表，保持译文自然通顺即可）"
                : string.Join("\n", entries.Select(e => $"- {e.Source} → {e.Target}"));

            var prompt = $$"""
                你是专业译审。下面是原文与机器译文，请在不增删信息、不改变原意的前提下润色译文：
                1. 修正生硬表达、语序和标点，使其符合目标语言习惯；
                2. 严格使用术语表给出的译名，同一术语前后保持一致；
                3. 只输出润色后的译文正文，不要输出解释、对比或代码围栏。

                术语表：
                {{glossaryBlock}}

                原文：
                {{Truncate(originalText, 4000)}}

                机器译文：
                {{Truncate(machineTranslation, 4000)}}
                """;

            try
            {
                var raw = await _aiService
                    .AskQuestionAsync(prompt, string.Empty, cancellationToken)
                    .WaitAsync(TimeSpan.FromSeconds(PolishTimeoutSeconds), cancellationToken)
                    .ConfigureAwait(false);

                var polished = StripCodeFence(raw);
                if (string.IsNullOrWhiteSpace(polished))
                    return PdfTranslationPolishResult.Fail(machineTranslation, "AI 未返回润色结果");

                var missing = CheckMissingTerms(originalText, polished, entries);
                return PdfTranslationPolishResult.Ok(polished, missing);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "翻译润色失败，回退机器译文");
                return PdfTranslationPolishResult.Fail(machineTranslation, ex.Message);
            }
        }

        /// <summary>一致性检查：原文出现某术语，但译文中没有使用术语表指定译名</summary>
        private static IReadOnlyList<PdfGlossaryEntry> CheckMissingTerms(
            string originalText, string translation, IReadOnlyList<PdfGlossaryEntry> entries)
        {
            if (entries.Count == 0) return Array.Empty<PdfGlossaryEntry>();

            return entries
                .Where(e => !string.IsNullOrWhiteSpace(e.Source) && !string.IsNullOrWhiteSpace(e.Target))
                .Where(e => originalText.Contains(e.Source, StringComparison.OrdinalIgnoreCase))
                .Where(e => !translation.Contains(e.Target, StringComparison.Ordinal))
                .ToList();
        }

        private async Task<PdfTranslationGlossary?> BuildGlossaryAsync(
            string pdfPath, string fingerprint, CancellationToken cancellationToken)
        {
            try
            {
                var document = await _textIndex.GetOrBuildAsync(pdfPath, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                var pages = document.Pages
                    .Where(p => !string.IsNullOrWhiteSpace(p.Text))
                    .Take(MaxPagesForGlossary)
                    .ToList();
                if (pages.Count == 0) return null;

                var builder = new StringBuilder();
                foreach (var page in pages)
                {
                    builder.AppendLine($"[[p{page.PageIndex + 1}]]");
                    builder.AppendLine(Truncate(page.Text.Trim(), MaxCharsPerPage));
                    builder.AppendLine();
                }

                var documentName = Path.GetFileNameWithoutExtension(pdfPath);
                var prompt = $$"""
                    你是专业翻译术语助手。下面是 PDF《{{documentName}}》的部分内容。
                    请抽取其中反复出现、需要统一译法的专业术语或关键概念，给出中文译名。
                    只输出严格 JSON 数组，不要输出解释或代码围栏：
                    [{"source":"原文术语","target":"中文译名"}]
                    要求：
                    1. source 使用原文语言，target 使用中文；
                    2. 最多 {{MaxGlossaryEntries}} 条，按重要程度排序；
                    3. 不要收录无实际含义的虚词，同一术语只出现一次。

                    文档内容：
                    {{builder}}
                    """;

                var raw = await _aiService!
                    .AskQuestionAsync(prompt, string.Empty, cancellationToken)
                    .WaitAsync(TimeSpan.FromSeconds(PolishTimeoutSeconds), cancellationToken)
                    .ConfigureAwait(false);

                var entries = ParseGlossary(StripCodeFence(raw));
                if (entries.Count == 0) return null;

                var glossary = new PdfTranslationGlossary
                {
                    DocumentFingerprint = fingerprint,
                    Entries = entries
                };
                SaveToDisk(glossary);
                return glossary;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "构建翻译术语表失败");
                return null;
            }
        }

        private static List<PdfGlossaryEntry> ParseGlossary(string json)
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array) return new List<PdfGlossaryEntry>();

            var entries = new List<PdfGlossaryEntry>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object) continue;
                if (!element.TryGetProperty("source", out var sourceElement)) continue;
                if (!element.TryGetProperty("target", out var targetElement)) continue;

                var source = sourceElement.GetString()?.Trim();
                var target = targetElement.GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target)) continue;
                if (!seen.Add(source)) continue;

                entries.Add(new PdfGlossaryEntry { Source = source, Target = target });
                if (entries.Count >= MaxGlossaryEntries) break;
            }

            return entries;
        }

        private PdfTranslationGlossary? TryLoadFromDisk(string fingerprint)
        {
            try
            {
                var path = GetCacheFilePath(fingerprint);
                if (!File.Exists(path)) return null;

                var json = File.ReadAllText(path);
                var glossary = JsonSerializer.Deserialize<PdfTranslationGlossary>(json);
                if (glossary == null || glossary.Entries.Count == 0) return null;
                return glossary;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "读取术语表缓存失败，将重新构建");
                return null;
            }
        }

        private void SaveToDisk(PdfTranslationGlossary glossary)
        {
            try
            {
                Directory.CreateDirectory(_cacheDirectory);
                var path = GetCacheFilePath(glossary.DocumentFingerprint);
                var tempPath = path + ".tmp";
                File.WriteAllText(tempPath, JsonSerializer.Serialize(glossary), new UTF8Encoding(false));
                File.Move(tempPath, path, overwrite: true);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "保存术语表缓存失败");
            }
        }

        private string GetCacheFilePath(string fingerprint)
            => Path.Combine(_cacheDirectory, fingerprint + ".json");

        private static string Truncate(string text, int maxLength)
            => text.Length > maxLength ? text[..maxLength] + "…" : text;

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
