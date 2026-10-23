using System.Text;
using System.Text.RegularExpressions;

namespace LearningAssistant.Services.Pdf;

/// <summary>PDF 文本层版面规范化选项</summary>
public sealed class PdfTextLayoutOptions
{
    /// <summary>合并被硬换行拆断的段落</summary>
    public bool MergeWrappedLines { get; init; } = true;

    /// <summary>修复行尾连字符断词</summary>
    public bool RepairHyphenation { get; init; } = true;

    /// <summary>移除页眉/页脚区域内的纯页码行</summary>
    public bool RemovePageNumbers { get; init; } = true;

    /// <summary>仅在页面顶部/底部各多少行内匹配页眉页脚</summary>
    public int HeaderFooterScanLines { get; init; } = 3;

    /// <summary>需要剔除的页眉/页脚模板行（由 DetectHeaderFooterPatterns 生成）</summary>
    public IReadOnlyCollection<string> HeaderFooterPatterns { get; init; } = Array.Empty<string>();
}

/// <summary>
/// 本地规则版的 PDF 文本层整理：合并硬换行段落、修复连字符断词、去除重复页眉页脚与页码。
/// 纯文本规则、无外部依赖，便于单元测试；AI 精修仍由 PdfBatchTextCleaner 负责。
/// </summary>
public sealed class PdfTextLayoutNormalizer
{
    /// <summary>段落之间的分隔符</summary>
    public const string ParagraphSeparator = "\n\n";

    /// <summary>少于该页数时不推断页眉页脚，避免误删正文</summary>
    public const int MinPagesForHeaderDetection = 3;

    /// <summary>页眉页脚模板的最大长度，超过视为正文</summary>
    public const int MaxHeaderTemplateLength = 120;

    private static readonly Regex MultiSpaceRegex = new(@"[ \t\u00A0]+", RegexOptions.Compiled);

    private static readonly Regex PageNumberLineRegex = new(
        @"^[\s\-–—]*(?:第\s*)?\d{1,4}\s*(?:页)?(?:\s*(?:/|of)\s*\d{1,4})?\s*[\-–—]*\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ListMarkerRegex = new(
        @"^(?:[•·▪◦●○■□►▶]|[-–—*]\s|\d{1,3}[.)、．]\s*|\(\d{1,3}\)\s*|第[一二三四五六七八九十百零〇\d]+[章节页条部分])",
        RegexOptions.Compiled);

    /// <summary>
    /// 从多页文本中推断重复出现的页眉页脚模板行；少于 3 页时返回空集合。
    /// 模板会把数字统一替换为 #，因此 “第 3 页” 与 “第 12 页” 视为同一模板。
    /// </summary>
    /// <param name="pages">页面文本集合</param>
    /// <param name="minPageRatio">模板需要覆盖的最小页比例</param>
    /// <param name="scanLines">每页顶部/底部参与检测的行数</param>
    public IReadOnlyList<string> DetectHeaderFooterPatterns(
        IReadOnlyList<string>? pages,
        double minPageRatio = 0.3,
        int scanLines = 3)
    {
        if (pages == null || pages.Count < MinPagesForHeaderDetection)
            return Array.Empty<string>();

        scanLines = Math.Clamp(scanLines, 1, 10);
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var page in pages)
        {
            if (string.IsNullOrWhiteSpace(page)) continue;

            var lines = SplitLines(page)
                .Where(l => l.Length > 0)
                .ToList();
            if (lines.Count == 0) continue;

            // 同一页内重复出现的行只计一次，避免表格页把计数拉高
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in lines.Take(scanLines)
                         .Concat(lines.Skip(Math.Max(0, lines.Count - scanLines))))
            {
                // 纯页码行交给 RemovePageNumbers，不参与模板统计
                if (PageNumberLineRegex.IsMatch(line)) continue;

                var template = BuildLineTemplate(line);
                if (!IsHeaderFooterCandidate(template)) continue;
                seen.Add(template);
            }

            foreach (var template in seen)
                counts[template] = counts.GetValueOrDefault(template) + 1;
        }

        int required = Math.Max(MinPagesForHeaderDetection,
            (int)Math.Ceiling(pages.Count * Math.Clamp(minPageRatio, 0d, 1d)));

        return counts
            .Where(kv => kv.Value >= required)
            .OrderByDescending(kv => kv.Value)
            .Select(kv => kv.Key)
            .ToList();
    }

    /// <summary>对单页文本做版面规范化</summary>
    public string NormalizePage(string? text, PdfTextLayoutOptions? options = null)
    {
        options ??= new PdfTextLayoutOptions();
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var rawLines = SplitLines(text);
        var patterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pattern in options.HeaderFooterPatterns)
        {
            if (!string.IsNullOrWhiteSpace(pattern))
                patterns.Add(BuildLineTemplate(pattern));
        }

        var paragraphs = new List<string>();
        var current = new StringBuilder();
        string? lastLine = null;

        void Flush()
        {
            if (current.Length == 0) return;
            paragraphs.Add(current.ToString().Trim());
            current.Clear();
            lastLine = null;
        }

        for (int i = 0; i < rawLines.Length; i++)
        {
            var line = MultiSpaceRegex.Replace(rawLines[i].Trim(), " ").Trim();
            if (line.Length == 0)
            {
                Flush();
                continue;
            }

            bool inHeaderFooter = i < options.HeaderFooterScanLines ||
                                  i >= rawLines.Length - options.HeaderFooterScanLines;
            if (inHeaderFooter)
            {
                if (patterns.Count > 0 && patterns.Contains(BuildLineTemplate(line)))
                    continue;
                if (options.RemovePageNumbers && PageNumberLineRegex.IsMatch(line))
                    continue;
            }

            if (lastLine != null)
            {
                var join = AnalyzeJoin(lastLine, line, options, out var joiner, out bool trimHyphen);
                if (join)
                {
                    if (trimHyphen && current.Length > 0)
                        current.Length--;
                    current.Append(joiner).Append(line);
                    lastLine = line;
                    continue;
                }
                Flush();
            }

            current.Append(line);
            lastLine = line;
        }

        Flush();
        return string.Join(ParagraphSeparator, paragraphs);
    }

    /// <summary>整篇规范化：先推断页眉页脚模板，再逐页处理</summary>
    public IReadOnlyList<string> NormalizeDocument(
        IReadOnlyList<string>? pages,
        PdfTextLayoutOptions? options = null)
    {
        if (pages == null || pages.Count == 0)
            return Array.Empty<string>();

        options ??= new PdfTextLayoutOptions();

        var patterns = new HashSet<string>(options.HeaderFooterPatterns, StringComparer.OrdinalIgnoreCase);
        foreach (var pattern in DetectHeaderFooterPatterns(pages, 0.3, options.HeaderFooterScanLines))
            patterns.Add(pattern);

        var merged = new PdfTextLayoutOptions
        {
            MergeWrappedLines = options.MergeWrappedLines,
            RepairHyphenation = options.RepairHyphenation,
            RemovePageNumbers = options.RemovePageNumbers,
            HeaderFooterScanLines = options.HeaderFooterScanLines,
            HeaderFooterPatterns = patterns.ToArray()
        };

        return pages.Select(page => NormalizePage(page, merged)).ToArray();
    }

    /// <summary>判断当前行与下一行是否应合并，并给出连接符（CJK 直接拼接，拉丁文补空格）</summary>
    private static bool AnalyzeJoin(
        string previous,
        string next,
        PdfTextLayoutOptions options,
        out string joiner,
        out bool trimHyphen)
    {
        joiner = string.Empty;
        trimHyphen = false;
        if (!options.MergeWrappedLines) return false;

        // 列表项、编号项、章节标题一律另起一段
        if (ListMarkerRegex.IsMatch(next)) return false;

        char previousLast = previous[^1];
        char nextFirst = next[0];

        // 行尾连字符断词：exam- / ple → example，需要连同上一行末尾的连字符一起去掉
        if (options.RepairHyphenation &&
            (previousLast == '-' || previousLast == '‐' || previousLast == '‑' || previousLast == '\u00AD') &&
            char.IsLetter(nextFirst))
        {
            trimHyphen = true;
            return true;
        }

        // 硬句末。中文句末一律断段
        if ("。！？".IndexOf(previousLast) >= 0) return false;
        if (previousLast == '!' || previousLast == '?') return false;

        if (previousLast == '.')
        {
            // 英文句号后接大写或引号视为新句；接数字多为 Fig. 3 / No. 5 这类句中引用，继续合并
            if (char.IsUpper(nextFirst) ||
                nextFirst == '"' || nextFirst == '\'' || nextFirst == '“' || nextFirst == '‘')
                return false;
        }

        joiner = IsCjk(previousLast) || IsCjk(nextFirst) ? string.Empty : " ";
        return true;
    }

    /// <summary>把行规范化为模板：数字替换为 #、字母小写、压缩空白，用于跨页重复检测</summary>
    public static string BuildLineTemplate(string line)
    {
        if (string.IsNullOrEmpty(line)) return string.Empty;

        var builder = new StringBuilder(line.Length);
        foreach (char c in line)
        {
            if (char.IsDigit(c))
                builder.Append('#');
            else if (char.IsWhiteSpace(c))
                builder.Append(' ');
            else
                builder.Append(char.ToLowerInvariant(c));
        }

        return MultiSpaceRegex.Replace(builder.ToString(), " ").Trim();
    }

    private static bool IsHeaderFooterCandidate(string template)
    {
        if (template.Length < 2 || template.Length > MaxHeaderTemplateLength) return false;

        // 纯数字/页码类模板没有区分度，由 RemovePageNumbers 处理
        if (!template.Any(char.IsLetter)) return false;

        return true;
    }

    private static string[] SplitLines(string text)
    {
        return text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
    }

    private static bool IsCjk(char c)
    {
        return (c >= '\u4E00' && c <= '\u9FFF') ||
               (c >= '\u3400' && c <= '\u4DBF') ||
               (c >= '\u3040' && c <= '\u30FF') ||
               (c >= '\uAC00' && c <= '\uD7AF') ||
               (c >= '\u3000' && c <= '\u303F') ||
               (c >= '\uFF00' && c <= '\uFFEF');
    }
}
