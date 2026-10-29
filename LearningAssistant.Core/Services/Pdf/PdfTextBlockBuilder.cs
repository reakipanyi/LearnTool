using System.Text.RegularExpressions;
using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf;

/// <summary>自动框选段落分块参数</summary>
public sealed class PdfTextBlockBuildOptions
{
    /// <summary>是否排除页边带内的页码</summary>
    public bool RemovePageNumbers { get; init; } = true;

    /// <summary>页码检测使用的顶部/底部页边带比例</summary>
    public float PageNumberBandRatio { get; init; } = 0.10f;

    /// <summary>小字号页眉页脚剔除使用的窄页边带比例</summary>
    public float SmallTextBandRatio { get; init; } = 0.06f;

    /// <summary>窄页边带内低于该行高中位数倍数的行视为页眉页脚</summary>
    public float SmallTextHeightFactor { get; init; } = 0.65f;

    /// <summary>同一视觉行要求的最小垂直重叠率</summary>
    public float RowVerticalOverlap { get; init; } = 0.5f;

    /// <summary>同一视觉行允许的最大水平间距（相对行高）</summary>
    public float RowHorizontalGapFactor { get; init; } = 2.0f;

    /// <summary>段落合并允许的最大垂直间距（相对行高）</summary>
    public float ParagraphGapFactor { get; init; } = 0.85f;

    /// <summary>段落左边缘对齐容差</summary>
    public float ParagraphAlignTolerance { get; init; } = 0.05f;

    /// <summary>字号超过该倍数视为标题，强制断块</summary>
    public float HeadingFontScale { get; init; } = 1.3f;

    /// <summary>是否把连续多行合并为一个段落块；关闭后每个视觉行各自成一个高亮块</summary>
    public bool MergeMultiLine { get; init; } = true;
}

/// <summary>
/// 把带坐标的 PDF 文本行整理成自动框选用的段落块。
/// 纯规则实现，不依赖 PDF 渲染，便于单元测试。
/// </summary>
public static class PdfTextBlockBuilder
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    /// <summary>扩展页码识别：兼容 “第 3 页”“3 / 10”“Page 3 of 10”“3·10” 等页脚格式</summary>
    private static readonly Regex PageChromeRegex = new(
        @"^[\s\-–—]*(?:(?:第\s*)?\d{1,4}\s*页?|page\s*\d{1,4})(?:\s*(?:/|of|共|,|·)\s*\d{1,4}\s*页?)?\s*[\-–—]*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HyphenEndRegex = new(@"[\-‐‑\u00AD]$", RegexOptions.Compiled);

    /// <summary>把文本行整理为段落块。输入顺序沿用 PDF 提取顺序，避免双栏被全局排序打散。</summary>
    public static IReadOnlyList<PdfAutoTextBlock> Build(
        IReadOnlyList<PdfTextLine>? lines,
        PdfTextBlockBuildOptions? options = null)
    {
        if (lines == null || lines.Count == 0)
            return Array.Empty<PdfAutoTextBlock>();

        options ??= new PdfTextBlockBuildOptions();

        var cleaned = CleanLines(lines);
        if (cleaned.Count == 0)
            return Array.Empty<PdfAutoTextBlock>();

        float medianHeight = ComputeMedianHeight(cleaned);
        if (options.RemovePageNumbers)
        {
            cleaned = RemovePageChrome(cleaned, medianHeight, options);
            if (cleaned.Count == 0)
                return Array.Empty<PdfAutoTextBlock>();
        }

        var rows = BuildRows(cleaned, options);
        if (rows.Count == 0)
            return Array.Empty<PdfAutoTextBlock>();

        var blocks = BuildBlocks(rows, options);
        return ExpandAndValidate(blocks);
    }

    private static List<PdfTextLine> CleanLines(IReadOnlyList<PdfTextLine> lines)
    {
        var result = new List<PdfTextLine>(lines.Count);
        foreach (var line in lines)
        {
            if (line == null) continue;

            var text = WhitespaceRegex.Replace(line.Text ?? string.Empty, " ").Trim();
            if (text.Length == 0) continue;

            if (line.Width <= 0 || line.Height <= 0) continue;
            if (line.X < -0.02f || line.Y < -0.02f || line.X > 1.02f || line.Y > 1.02f) continue;

            result.Add(new PdfTextLine
            {
                Text = text,
                X = Clamp01(line.X),
                Y = Clamp01(line.Y),
                Width = Math.Min(line.Width, 1f - Clamp01(line.X)),
                Height = Math.Min(line.Height, 1f - Clamp01(line.Y)),
                FontSize = line.FontSize
            });
        }

        return result;
    }

    /// <summary>只有位于页边带且符合页码/小字号特征的文本才删除，正文纯数字保留。</summary>
    private static List<PdfTextLine> RemovePageChrome(
        List<PdfTextLine> lines,
        float medianHeight,
        PdfTextBlockBuildOptions options)
    {
        var result = new List<PdfTextLine>(lines.Count);
        float wideBand = Math.Clamp(options.PageNumberBandRatio, 0.02f, 0.3f);
        float narrowBand = Math.Clamp(options.SmallTextBandRatio, 0.02f, wideBand);

        foreach (var line in lines)
        {
            bool inWideBand = line.Bottom <= wideBand || line.Y >= 1f - wideBand;
            bool inNarrowBand = line.Bottom <= narrowBand || line.Y >= 1f - narrowBand;

            // 页码：仅在页边带内删除，避免正文里的年份/编号被误删
            if (inWideBand &&
                (PageChromeRegex.IsMatch(line.Text) || PdfTextLayoutNormalizer.IsPageNumberLine(line.Text)))
            {
                continue;
            }

            // 页眉页脚：窄边带 + 明显小于正文字号
            if (inNarrowBand &&
                medianHeight > 0 &&
                line.Height < medianHeight * Math.Clamp(options.SmallTextHeightFactor, 0.2f, 1f) &&
                line.Text.Length <= 80)
            {
                continue;
            }

            result.Add(line);
        }

        return result;
    }

    private static List<TextRow> BuildRows(List<PdfTextLine> lines, PdfTextBlockBuildOptions options)
    {
        var rows = new List<TextRow>();
        TextRow? last = null;

        foreach (var line in lines)
        {
            if (last != null && TryAppendToRow(last, line, options))
            {
                last.Append(line);
                continue;
            }

            last = new TextRow(line);
            rows.Add(last);
        }

        return rows;
    }

    /// <summary>同一视觉行判定：垂直重叠足够，且水平间距不超过行高的若干倍。</summary>
    private static bool TryAppendToRow(TextRow row, PdfTextLine line, PdfTextBlockBuildOptions options)
    {
        float overlap = Math.Min(row.Bottom, line.Bottom) - Math.Max(row.Y, line.Y);
        float minHeight = Math.Min(row.Height, line.Height);
        if (minHeight <= 0) return false;

        if (overlap < minHeight * Math.Clamp(options.RowVerticalOverlap, 0.1f, 0.95f))
            return false;

        float gap = line.X >= row.Right ? line.X - row.Right :
                    row.X >= line.Right ? row.X - line.Right : 0f;

        float maxGap = Math.Max(row.Height, line.Height) * Math.Max(0.1f, options.RowHorizontalGapFactor);
        return gap <= maxGap;
    }

    private static List<PdfAutoTextBlock> BuildBlocks(List<TextRow> rows, PdfTextBlockBuildOptions options)
    {
        // 关闭多行合并时，每个视觉行直接独立成块，只保留行内拼接
        if (!options.MergeMultiLine)
            return rows.Select(CreateBlock).ToList();

        var blocks = new List<PdfAutoTextBlock>();
        PdfAutoTextBlock? current = null;
        float currentLineHeight = 0f;

        foreach (var row in rows)
        {
            if (current == null)
            {
                current = CreateBlock(row);
                currentLineHeight = row.Height;
                continue;
            }

            if (!ShouldMerge(current, currentLineHeight, row, options))
            {
                blocks.Add(current);
                current = CreateBlock(row);
                currentLineHeight = row.Height;
                continue;
            }

            MergeRow(current, row);
            currentLineHeight = Math.Max(currentLineHeight, row.Height);
        }

        if (current != null)
            blocks.Add(current);

        return blocks;
    }

    private static PdfAutoTextBlock CreateBlock(TextRow row)
    {
        return new PdfAutoTextBlock
        {
            Text = row.Text,
            X = row.X,
            Y = row.Y,
            Width = row.Width,
            Height = row.Height,
            LineCount = row.LineCount,
            FontSize = row.FontSize
        };
    }

    private static bool ShouldMerge(
        PdfAutoTextBlock current,
        float currentLineHeight,
        TextRow row,
        PdfTextBlockBuildOptions options)
    {
        float referenceHeight = Math.Max(row.Height, currentLineHeight);
        if (referenceHeight <= 0) return false;

        float gap = row.Y - (current.Y + current.Height);

        // 行向上跳变：通常已换栏或换页，必须断块
        if (gap < -referenceHeight * 0.75f)
            return false;

        float maxGap = Math.Max(referenceHeight * Math.Max(0.1f, options.ParagraphGapFactor), 0.012f);
        if (gap > maxGap)
            return false;

        // 跨栏：下一行从当前块右侧之外开始
        if (row.X > current.X + current.Width + 0.02f)
            return false;

        // 左边缘对齐或居中对齐才继续合并
        float currentCenterX = current.X + current.Width / 2f;
        float leftTolerance = Math.Max(options.ParagraphAlignTolerance, referenceHeight * 0.6f);
        bool aligned = Math.Abs(row.X - current.X) <= leftTolerance ||
                       Math.Abs(row.CenterX - currentCenterX) <= 0.03f;
        if (!aligned)
            return false;

        // 列表项、标题字号突变都独立成块
        if (PdfTextLayoutNormalizer.IsListMarkerLine(row.Text))
            return false;

        float blockFont = current.FontSize > 0 ? current.FontSize : currentLineHeight;
        float rowFont = row.FontSize > 0 ? row.FontSize : row.Height;
        float headingScale = Math.Max(1.1f, options.HeadingFontScale);
        if (blockFont > 0 && rowFont > 0 &&
            (rowFont > blockFont * headingScale || rowFont < blockFont / headingScale))
        {
            return false;
        }

        // 上一行已到句末，下一行视为新段落
        return !EndsSentence(current.Text);
    }

    private static void MergeRow(PdfAutoTextBlock block, TextRow row)
    {
        var previous = block.Text.TrimEnd();
        var next = row.Text.Trim();
        string joiner;

        if (HyphenEndRegex.IsMatch(previous) && next.Length > 0 && char.IsLetter(next[0]))
        {
            previous = previous[..^1];
            joiner = string.Empty;
        }
        else
        {
            joiner = ShouldInsertSpace(previous, next) ? " " : string.Empty;
        }

        block.Text = previous + joiner + next;

        float right = Math.Max(block.X + block.Width, row.Right);
        float bottom = Math.Max(block.Y + block.Height, row.Bottom);
        block.X = Math.Min(block.X, row.X);
        block.Y = Math.Min(block.Y, row.Y);
        block.Width = Math.Max(0, right - block.X);
        block.Height = Math.Max(0, bottom - block.Y);
        block.LineCount += row.LineCount;
        block.FontSize = Math.Max(block.FontSize, row.FontSize);
    }

    private static bool ShouldInsertSpace(string previous, string next)
    {
        if (previous.Length == 0 || next.Length == 0) return false;

        char last = previous[^1];
        char first = next[0];
        return !(IsCjk(last) || IsCjk(first));
    }

    private static bool EndsSentence(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return true;

        var trimmed = text.TrimEnd().TrimEnd('"', '\'', '”', '’', '」', '』', ')', '）');
        if (trimmed.Length == 0) return true;

        char last = trimmed[^1];
        return last is '。' or '！' or '？' or '!' or '?' or '…' or '.' ||
               last == ';' || last == '；';
    }

    private static IReadOnlyList<PdfAutoTextBlock> ExpandAndValidate(List<PdfAutoTextBlock> blocks)
    {
        const float paddingX = 0.004f;
        const float paddingY = 0.003f;

        var result = new List<PdfAutoTextBlock>(blocks.Count);
        foreach (var block in blocks)
        {
            var text = block.Text.Trim();
            if (text.Length == 0) continue;
            if (!text.Any(char.IsLetterOrDigit)) continue;

            float x = Math.Max(0, block.X - paddingX);
            float y = Math.Max(0, block.Y - paddingY);
            float right = Math.Min(1f, block.X + block.Width + paddingX);
            float bottom = Math.Min(1f, block.Y + block.Height + paddingY);

            if (right <= x || bottom <= y) continue;

            result.Add(new PdfAutoTextBlock
            {
                Text = text,
                X = x,
                Y = y,
                Width = right - x,
                Height = bottom - y,
                LineCount = block.LineCount,
                FontSize = block.FontSize
            });
        }

        return result;
    }

    private static float ComputeMedianHeight(List<PdfTextLine> lines)
    {
        var heights = lines
            .Select(l => l.Height)
            .Where(h => h > 0.0005f)
            .OrderBy(h => h)
            .ToArray();

        if (heights.Length == 0) return 0f;
        return heights[heights.Length / 2];
    }

    private static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);

    private static bool IsCjk(char c)
    {
        return (c >= '\u4E00' && c <= '\u9FFF') ||
               (c >= '\u3400' && c <= '\u4DBF') ||
               (c >= '\u3040' && c <= '\u30FF') ||
               (c >= '\uAC00' && c <= '\uD7AF') ||
               (c >= '\u3000' && c <= '\u303F') ||
               (c >= '\uFF00' && c <= '\uFFEF');
    }

    /// <summary>同一视觉行的文本累加器：保留各部分 X 坐标，拼接时按阅读顺序排列。</summary>
    private sealed class TextRow
    {
        private readonly List<(float X, string Text)> _parts = new();

        public TextRow(PdfTextLine line)
        {
            X = line.X;
            Y = line.Y;
            Width = line.Width;
            Height = line.Height;
            FontSize = line.FontSize;
            LineCount = 1;
            _parts.Add((line.X, line.Text));
        }

        public float X { get; private set; }
        public float Y { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
        public float FontSize { get; private set; }
        public int LineCount { get; private set; }

        public float Right => X + Width;
        public float Bottom => Y + Height;
        public float CenterX => X + Width / 2f;

        public string Text
        {
            get
            {
                if (_parts.Count == 0) return string.Empty;
                if (_parts.Count == 1) return _parts[0].Text;

                var ordered = _parts.OrderBy(p => p.X).ToList();
                var builder = new System.Text.StringBuilder();
                for (int i = 0; i < ordered.Count; i++)
                {
                    if (i > 0 && ShouldInsertSpace(ordered[i - 1].Text, ordered[i].Text))
                        builder.Append(' ');
                    builder.Append(ordered[i].Text);
                }
                return builder.ToString();
            }
        }

        public void Append(PdfTextLine line)
        {
            _parts.Add((line.X, line.Text));

            float right = Math.Max(X + Width, line.X + line.Width);
            float bottom = Math.Max(Y + Height, line.Y + line.Height);
            X = Math.Min(X, line.X);
            Y = Math.Min(Y, line.Y);
            Width = Math.Max(0, right - X);
            Height = Math.Max(0, bottom - Y);
            FontSize = Math.Max(FontSize, line.FontSize);
            LineCount++;
        }
    }
}
