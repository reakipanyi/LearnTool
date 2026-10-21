using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LearningAssistant.Models.Shadowing;

namespace LearningAssistant.Services.Shadowing
{
    /// <summary>
    /// 纯逻辑英文分句器：输入一页文本，输出带稳定 Id 的句子列表。
    /// 不依赖任何 UI / PDF / 网络组件，便于单元测试。
    /// </summary>
    public static class SentenceSegmenter
    {
        /// <summary>
        /// 分句规则版本；规则调整时递增，清单可据此判断是否需要重新分句。
        /// </summary>
        public const string Version = "1.0";

        /// <summary>
        /// 单句最大长度，超过时优先在逗号/分号/冒号处二次切分。
        /// </summary>
        public const int MaxSentenceLength = 200;

        /// <summary>
        /// 单句最小长度，过短的片段会与相邻句合并，避免出现 “Yes.” 这类碎片。
        /// </summary>
        public const int MinSentenceLength = 12;

        private static readonly Regex NumericLine = new(
            @"^[\s\-\u2013\u2014]*\d+[\s\-\u2013\u2014]*$", RegexOptions.Compiled);

        private static readonly Regex PageFooterLine = new(
            @"^(page|p\.?)\s*\d+(\s*(of|/)\s*\d+)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RomanNumeralLine = new(
            @"^[IVXLCDM]+$", RegexOptions.Compiled);

        private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

        private static readonly HashSet<string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
        {
            "mr", "mrs", "ms", "dr", "prof", "sr", "jr", "st", "mt", "vs",
            "etc", "eg", "ie", "fig", "no", "vol", "pp", "inc", "ltd", "co",
            "corp", "dept", "est", "approx", "jan", "feb", "mar", "apr",
            "jun", "jul", "aug", "sep", "sept", "oct", "nov", "dec"
        };

        /// <summary>
        /// 将一段原始文本（含换行、页眉页脚）切分为句子。
        /// </summary>
        public static List<ShadowingSentence> Segment(string? rawText)
        {
            var result = new List<ShadowingSentence>();
            if (string.IsNullOrWhiteSpace(rawText))
                return result;

            string text = Normalize(rawText);
            if (text.Length == 0)
                return result;

            var spans = SplitSentences(text);
            spans = MergeShortSpans(text, spans);
            spans = SplitLongSpans(text, spans);

            var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var (rawStart, rawEnd) in spans)
            {
                var (start, end) = TrimSpan(text, rawStart, rawEnd);
                if (end <= start)
                    continue;

                string sentenceText = text.Substring(start, end - start);
                string normalized = NormalizeForId(sentenceText);
                if (normalized.Length == 0)
                    continue;

                occurrences.TryGetValue(normalized, out int occurrence);
                occurrences[normalized] = occurrence + 1;

                result.Add(new ShadowingSentence
                {
                    Id = ComputeId(normalized, occurrence),
                    Index = result.Count,
                    StartOffset = start,
                    Length = end - start,
                    Text = sentenceText
                });
            }

            return result;
        }

        /// <summary>
        /// 合并断行：行尾连字符拼接、普通换行替换为空格，并过滤纯数字/页码行。
        /// </summary>
        private static string Normalize(string raw)
        {
            var sb = new StringBuilder();
            var lines = raw.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0 || IsNoiseLine(trimmed))
                    continue;

                bool hyphenBreak = sb.Length > 0 && sb[^1] == '-' && char.IsLower(trimmed[0]);
                if (hyphenBreak)
                    sb.Length--;
                else if (sb.Length > 0)
                    sb.Append(' ');

                sb.Append(trimmed);
            }

            return Whitespace.Replace(sb.ToString(), " ").Trim();
        }

        private static bool IsNoiseLine(string line)
        {
            int letterCount = 0;
            foreach (char c in line)
            {
                if (char.IsLetter(c))
                    letterCount++;
            }

            if (letterCount < 2)
                return true;
            if (NumericLine.IsMatch(line))
                return true;
            if (PageFooterLine.IsMatch(line))
                return true;
            if (line.Length <= 8 && RomanNumeralLine.IsMatch(line))
                return true;

            return false;
        }

        private static List<(int Start, int End)> SplitSentences(string text)
        {
            var spans = new List<(int Start, int End)>();
            int length = text.Length;
            int start = 0;
            int i = 0;

            while (i < length)
            {
                char c = text[i];
                if (!IsTerminator(c))
                {
                    i++;
                    continue;
                }

                int end = i + 1;
                while (end < length && IsTerminator(text[end]))
                    end++;

                if (IsAsciiTerminator(c) && IsProtectedDot(text, i))
                {
                    i = end;
                    continue;
                }

                int after = end;
                while (after < length && IsClosingChar(text[after]))
                    after++;
                after = SkipWhitespace(text, after);

                bool boundary;
                if (IsCjkTerminator(c))
                {
                    // 中文句号后直接进入下一句，不需要空格
                    boundary = true;
                }
                else
                {
                    boundary = after >= length || IsSentenceStart(text, after);
                }

                if (boundary)
                {
                    spans.Add((start, after));
                    start = after;
                    i = after;
                }
                else
                {
                    i = end;
                }
            }

            if (start < length)
                spans.Add((start, length));

            return spans;
        }

        /// <summary>
        /// 保护小数点、缩写（Dr./e.g.）以及 URL/邮箱中的句点。
        /// </summary>
        private static bool IsProtectedDot(string text, int dotIndex)
        {
            if (dotIndex > 0 && dotIndex + 1 < text.Length
                && char.IsDigit(text[dotIndex - 1]) && char.IsDigit(text[dotIndex + 1]))
            {
                return true;
            }

            int start = dotIndex;
            while (start > 0 && (char.IsLetter(text[start - 1]) || text[start - 1] == '.'))
                start--;

            string token = text.Substring(start, dotIndex - start);
            if (token.Length > 0)
            {
                string normalized = token.Replace(".", string.Empty);
                if (Abbreviations.Contains(normalized))
                    return true;
                // 人名首字母缩写，如 J. K. Rowling
                if (token.Length == 1 && char.IsUpper(token[0]))
                    return true;
            }

            int tokenStart = dotIndex;
            while (tokenStart > 0 && !char.IsWhiteSpace(text[tokenStart - 1]))
                tokenStart--;
            int tokenEnd = dotIndex;
            while (tokenEnd < text.Length && !char.IsWhiteSpace(text[tokenEnd]))
                tokenEnd++;

            string fullToken = text.Substring(tokenStart, tokenEnd - tokenStart);
            return fullToken.Contains("://", StringComparison.Ordinal)
                   || fullToken.Contains('@')
                   || fullToken.StartsWith("www.", StringComparison.OrdinalIgnoreCase);
        }

        private static List<(int Start, int End)> MergeShortSpans(
            string text, List<(int Start, int End)> spans)
        {
            if (spans.Count <= 1)
                return spans;

            var merged = new List<(int Start, int End)>();
            foreach (var span in spans)
            {
                if (merged.Count > 0 && SpanText(text, merged[^1]).Length < MinSentenceLength)
                {
                    merged[^1] = (merged[^1].Start, span.End);
                }
                else
                {
                    merged.Add(span);
                }
            }

            if (merged.Count > 1 && SpanText(text, merged[^1]).Length < MinSentenceLength)
            {
                var last = merged[^1];
                merged.RemoveAt(merged.Count - 1);
                merged[^1] = (merged[^1].Start, last.End);
            }

            return merged;
        }

        private static List<(int Start, int End)> SplitLongSpans(
            string text, List<(int Start, int End)> spans)
        {
            var result = new List<(int Start, int End)>();
            foreach (var span in spans)
            {
                int cursor = span.Start;
                while (span.End - cursor > MaxSentenceLength)
                {
                    int limit = cursor + MaxSentenceLength;
                    int cut = -1;

                    for (int k = limit; k > cursor + MinSentenceLength; k--)
                    {
                        char ch = text[k];
                        if ((ch == ',' || ch == ';' || ch == ':')
                            && (k + 1 >= text.Length || char.IsWhiteSpace(text[k + 1])))
                        {
                            cut = k + 1;
                            break;
                        }
                    }

                    if (cut < 0)
                    {
                        for (int k = limit; k > cursor + MinSentenceLength; k--)
                        {
                            if (char.IsWhiteSpace(text[k]))
                            {
                                cut = k;
                                break;
                            }
                        }
                    }

                    if (cut <= cursor)
                        cut = limit;

                    result.Add((cursor, cut));
                    cursor = SkipWhitespace(text, cut);
                }

                if (cursor < span.End)
                    result.Add((cursor, span.End));
            }

            return result;
        }

        private static string SpanText(string text, (int Start, int End) span)
        {
            var (start, end) = TrimSpan(text, span.Start, span.End);
            return end <= start ? string.Empty : text.Substring(start, end - start);
        }

        private static (int Start, int End) TrimSpan(string text, int start, int end)
        {
            while (start < end && char.IsWhiteSpace(text[start]))
                start++;
            while (end > start && char.IsWhiteSpace(text[end - 1]))
                end--;
            return (start, end);
        }

        private static int SkipWhitespace(string text, int index)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
                index++;
            return index;
        }

        private static bool IsTerminator(char c) =>
            c == '.' || c == '!' || c == '?' || c == '。' || c == '！' || c == '？' || c == '…';

        private static bool IsAsciiTerminator(char c) => c == '.' || c == '!' || c == '?';

        private static bool IsCjkTerminator(char c) => c == '。' || c == '！' || c == '？';

        private static bool IsClosingChar(char c) =>
            c == '"' || c == '\'' || c == ')' || c == ']' || c == '}'
            || c == '\u201D' || c == '\u2019' || c == '\u300D' || c == '\u300F'
            || c == '\u3011' || c == '\u300B' || c == '\u3009' || c == '>';

        private static bool IsOpeningChar(char c) =>
            c == '"' || c == '\'' || c == '(' || c == '[' || c == '{'
            || c == '\u201C' || c == '\u2018' || c == '\u300C' || c == '\u300E'
            || c == '\u3010' || c == '\u300A' || c == '\u3008';

        private static bool IsSentenceStart(string text, int index)
        {
            char c = text[index];
            return char.IsUpper(c)
                   || char.IsDigit(c)
                   || IsOpeningChar(c)
                   || IsClosingChar(c)
                   || IsCjkChar(c);
        }

        private static bool IsCjkChar(char c) =>
            (c >= 0x4E00 && c <= 0x9FFF) || (c >= 0x3400 && c <= 0x4DBF);

        private static string NormalizeForId(string text)
        {
            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c) || c == '\'')
                    sb.Append(char.ToLowerInvariant(c));
                else
                    sb.Append(' ');
            }

            return Whitespace.Replace(sb.ToString(), " ").Trim();
        }

        /// <summary>
        /// 使用规范化文本 + 同文本出现序号生成稳定 Id。
        /// 相比直接用句序号，前文分句变化时后续相同句子的 Id 不会被连带改写。
        /// </summary>
        private static string ComputeId(string normalized, int occurrence)
        {
            string payload = normalized + "#" + occurrence.ToString(CultureInfo.InvariantCulture);
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash)[..16].ToLowerInvariant();
        }
    }
}
