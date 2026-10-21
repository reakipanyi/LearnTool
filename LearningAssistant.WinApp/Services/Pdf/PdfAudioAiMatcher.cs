using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.AI;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LearningAssistant.Services.Pdf;

/// <summary>AI 为单个音频文件给出的页码匹配建议</summary>
public sealed class PdfAudioAiMatchSuggestion
{
    public string FilePath { get; init; } = "";
    public string FileName { get; init; } = "";
    /// <summary>0 基页码</summary>
    public int PageIndex { get; init; }
    public double Confidence { get; init; }
    public string Reason { get; init; } = "";
}

/// <summary>
/// 利用 AI 把音频文件映射到 PDF 页码：先提取页面文本摘要，再让模型结合文件名给出对应关系。
/// 结果仅作为建议，由用户在预览对话框中确认后再写入手动匹配。
/// </summary>
public sealed class PdfAudioAiMatcher
{
    // 输出 token 有限，按小批次请求避免 JSON 被截断
    private const int PageChunkSize = 25;
    private const int MaxPageTextLength = 240;
    private const int MaxTracksPerRequest = 400;

    private readonly IAIService _aiService;
    private readonly IPdfService _pdfService;
    private readonly ILogger _logger;

    public PdfAudioAiMatcher(IAIService aiService, IPdfService pdfService, ILogger logger)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<PdfAudioAiMatchSuggestion>> SuggestAsync(
        string pdfPath, int pageCount, IReadOnlyList<AudioTrack> tracks,
        IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pdfPath) || pageCount <= 0 || tracks.Count == 0)
            return new List<PdfAudioAiMatchSuggestion>();

        // 单次请求的文件列表不宜过长，超出部分不参与本轮流式匹配
        var usableTracks = tracks.Take(MaxTracksPerRequest).ToList();

        progress?.Report("正在读取 PDF 页面文本…");
        var pageTexts = await Task.Run(
                () => ExtractPageTexts(pdfPath, pageCount, progress, cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);

        var merged = new Dictionary<string, PdfAudioAiMatchSuggestion>(StringComparer.OrdinalIgnoreCase);
        var chunkCount = (pageCount + PageChunkSize - 1) / PageChunkSize;
        for (int start = 0; start < pageCount; start += PageChunkSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var end = Math.Min(pageCount, start + PageChunkSize);
            progress?.Report($"AI 正在匹配第 {start / PageChunkSize + 1}/{chunkCount} 批页面…");

            var prompt = BuildPrompt(pageTexts, start, end, pageCount, usableTracks);
            var answer = await _aiService
                .AskQuestionAsync(prompt, "PDF配套音频自动匹配", cancellationToken)
                .ConfigureAwait(false);

            foreach (var suggestion in ParseAnswer(answer, usableTracks, pageCount))
            {
                // 同一文件在多批结果中出现时保留置信度更高的一条
                if (!merged.TryGetValue(suggestion.FilePath, out var existing) ||
                    suggestion.Confidence > existing.Confidence)
                    merged[suggestion.FilePath] = suggestion;
            }
        }

        return merged.Values
            .OrderBy(s => s.PageIndex)
            .ThenByDescending(s => s.Confidence)
            .ToList();
    }

    private List<string> ExtractPageTexts(string pdfPath, int pageCount,
        IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var texts = new List<string>(pageCount);
        try
        {
            // 一次性加载文档，避免逐页 ExtractText 反复打开 PDF
            _pdfService.Load(pdfPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI 匹配加载 PDF 失败：{Path}", pdfPath);
            for (int i = 0; i < pageCount; i++) texts.Add(string.Empty);
            return texts;
        }

        try
        {
            for (int i = 0; i < pageCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string text;
                try
                {
                    text = _pdfService.GetPdfText(i);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI 匹配读取页面文本失败：{Path} 第 {Page} 页", pdfPath, i + 1);
                    text = string.Empty;
                }
                texts.Add(NormalizeText(text));
                if ((i + 1) % 10 == 0 || i == pageCount - 1)
                    progress?.Report($"正在读取 PDF 页面文本 {i + 1}/{pageCount}…");
            }
        }
        finally
        {
            // 释放文件句柄，不干扰阅读器与其它实例
            try { _pdfService.Unload(); } catch (Exception ex) { _logger.LogDebug(ex, "AI 匹配卸载 PDF 失败"); }
        }
        return texts;
    }

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var normalized = Regex.Replace(text, @"\s+", " ").Trim();
        return normalized.Length > MaxPageTextLength
            ? normalized[..MaxPageTextLength]
            : normalized;
    }

    private static string BuildPrompt(IReadOnlyList<string> pageTexts, int start, int end,
        int pageCount, IReadOnlyList<AudioTrack> tracks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("你是 PDF 配套音频匹配助手。请根据页面文本和音频文件名，判断每个音频最可能对应哪一页。");
        sb.AppendLine($"PDF 总页数：{pageCount}。本次只提供第 {start + 1} 页到第 {end} 页的文本摘要。");
        sb.AppendLine("要求：");
        sb.AppendLine("1. 页码从 1 开始，每个音频最多给出一个页码；无法判断的音频不要输出。");
        sb.AppendLine("2. 文件名中常含页码、章节号、标题等线索；页面无文本时只能依据文件名推测，请降低 confidence。");
        sb.AppendLine("3. 只输出严格 JSON 数组，不要输出 markdown 代码块、解释或多余文字。");
        sb.AppendLine("4. reason 不超过 15 个字，避免输出过长。");
        sb.AppendLine("格式：[{\"fileIndex\":1,\"page\":3,\"confidence\":0.9,\"reason\":\"第3课标题一致\"}]");
        sb.AppendLine();
        sb.AppendLine("页面摘要：");
        for (int i = start; i < end; i++)
        {
            var text = string.IsNullOrWhiteSpace(pageTexts[i])
                ? "（无可提取文本，可能是扫描页）"
                : pageTexts[i];
            sb.AppendLine($"第{i + 1}页：{text}");
        }
        sb.AppendLine();
        sb.AppendLine("音频列表（fileIndex 从 1 开始）：");
        for (int i = 0; i < tracks.Count; i++)
            sb.AppendLine($"[{i + 1}] {tracks[i].FileName}");
        return sb.ToString();
    }

    private List<PdfAudioAiMatchSuggestion> ParseAnswer(string answer,
        IReadOnlyList<AudioTrack> tracks, int pageCount)
    {
        var suggestions = new List<PdfAudioAiMatchSuggestion>();
        if (string.IsNullOrWhiteSpace(answer)) return suggestions;

        var json = ExtractJson(answer);
        if (json == null)
        {
            _logger.LogWarning("AI 音频匹配返回内容中未找到 JSON：{Answer}", Truncate(answer, 200));
            return suggestions;
        }

        try
        {
            using var document = ParseJson(json);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("matches", out var matches))
                root = matches;
            if (root.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("AI 音频匹配返回的 JSON 不是数组：{Json}", Truncate(json, 200));
                return suggestions;
            }

            foreach (var item in root.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                var fileIndex = ReadInt(item, "fileIndex") ?? ReadInt(item, "index");
                var page = ReadInt(item, "page") ?? ReadInt(item, "pageNumber");
                if (fileIndex is not int fi || page is not int p) continue;
                if (fi < 1 || fi > tracks.Count || p < 1 || p > pageCount) continue;

                var track = tracks[fi - 1];
                suggestions.Add(new PdfAudioAiMatchSuggestion
                {
                    FilePath = track.FilePath,
                    FileName = track.FileName,
                    PageIndex = p - 1,
                    Confidence = Math.Clamp(ReadDouble(item, "confidence") ?? 0.5, 0, 1),
                    Reason = ReadString(item, "reason")
                });
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "AI 音频匹配返回的 JSON 解析失败：{Json}", Truncate(json, 200));
        }
        return suggestions;
    }

    private static string? ExtractJson(string text)
    {
        var content = text.Trim();
        // 兼容模型把 JSON 包在 ```json ... ``` 中的情况
        var fence = content.IndexOf("```", StringComparison.Ordinal);
        if (fence >= 0)
        {
            var firstLineEnd = content.IndexOf('\n', fence);
            if (firstLineEnd > 0)
            {
                var endFence = content.IndexOf("```", firstLineEnd + 1, StringComparison.Ordinal);
                content = (endFence > 0 ? content[(firstLineEnd + 1)..endFence] : content[(firstLineEnd + 1)..]).Trim();
            }
        }

        var start = content.IndexOfAny(new[] { '[', '{' });
        if (start < 0) return null;
        var end = content.LastIndexOfAny(new[] { ']', '}' });
        return end > start ? content[start..(end + 1)] : null;
    }

    /// <summary>解析 JSON；模型输出被 token 上限截断时，尝试补齐到最后一个完整对象</summary>
    private static JsonDocument ParseJson(string json)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            if (!json.TrimStart().StartsWith('[')) throw;
            var lastClose = json.LastIndexOf('}');
            if (lastClose < 0) throw;
            return JsonDocument.Parse(json[..(lastClose + 1)] + "]");
        }
    }

    private static int? ReadInt(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value)) return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            return number;
        return value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed)
            ? parsed
            : null;
    }

    private static double? ReadDouble(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value)) return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
            return number;
        return value.ValueKind == JsonValueKind.String &&
               double.TryParse(value.GetString(), out var parsed)
            ? parsed
            : null;
    }

    private static string ReadString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    private static string Truncate(string text, int length)
        => text.Length <= length ? text : text[..length];
}
