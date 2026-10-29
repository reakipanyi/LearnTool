using System.Text;
using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// 把页面文本行合并为朗读句子：跨行拼接、去掉跨行连字符，并在句末标点处断句。
    /// 纯逻辑实现，不依赖 PDF 渲染，便于单元测试与连续朗读复用。
    /// </summary>
    public static class PdfSentenceSegmenter
    {
        /// <summary>句末标点（中英文）</summary>
        private const string SentenceEnds = ".?!。？！…";

        /// <summary>句末标点之后仍属于同一句的收尾符号</summary>
        private const string ClosingChars = "\"'”’」』）)】]》〉";

        /// <summary>英文常见缩写：其后的句点不是句末（统一按小写去点比较）</summary>
        private static readonly HashSet<string> Abbreviations = new(StringComparer.Ordinal)
        {
            "mr", "mrs", "ms", "dr", "prof", "sr", "jr", "st", "vs", "fig", "eq", "no", "inc", "ltd", "co", "dept", "est"
        };

        /// <summary>
        /// 按页内文本行构建句子集合；行按从上到下、从左到右排序，避免 PDF 输出顺序错乱。
        /// </summary>
        public static List<PdfSentence> BuildSentences(int pageIndex, IReadOnlyList<PdfTextLine>? lines)
        {
            var result = new List<PdfSentence>();
            if (lines == null || lines.Count == 0)
                return result;

            var ordered = lines
                .Where(line => line != null && !string.IsNullOrWhiteSpace(line.Text))
                .OrderBy(line => Math.Round(line.Y, 3))
                .ThenBy(line => line.X)
                .ToList();

            var builder = new StringBuilder();
            var currentLines = new List<PdfTextLine>();

            foreach (var line in ordered)
            {
                string text = line.Text.Trim();
                if (text.Length == 0)
                    continue;

                AppendLine(builder, text);
                currentLines.Add(line);

                if (IsSentenceEnd(text))
                    Flush(result, pageIndex, builder, currentLines);
            }

            Flush(result, pageIndex, builder, currentLines);
            AssignIndexes(result);
            return result;
        }

        /// <summary>
        /// 扫描页无文本层时，用已缓存的 OCR 段落块生成句子；每句沿用所属块的矩形。
        /// </summary>
        public static List<PdfSentence> BuildSentencesFromBlocks(int pageIndex, IReadOnlyList<PdfAutoTextBlock>? blocks)
        {
            var result = new List<PdfSentence>();
            if (blocks == null || blocks.Count == 0)
                return result;

            var ordered = blocks
                .Where(block => block != null && !string.IsNullOrWhiteSpace(block.Text))
                .OrderBy(block => Math.Round(block.Y, 3))
                .ThenBy(block => block.X)
                .ToList();

            foreach (var block in ordered)
            {
                foreach (string part in SplitText(block.Text))
                {
                    result.Add(new PdfSentence
                    {
                        PageIndex = pageIndex,
                        Text = part,
                        Lines = new List<PdfTextLine>
                        {
                            new()
                            {
                                Text = part,
                                X = block.X,
                                Y = block.Y,
                                Width = block.Width,
                                Height = block.Height,
                                FontSize = block.FontSize
                            }
                        }
                    });
                }
            }

            AssignIndexes(result);
            return result;
        }

        /// <summary>只切句、不生成坐标；用于整页 OCR 文本。</summary>
        public static IReadOnlyList<string> SplitText(string? text)
        {
            var parts = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return parts;

            string value = text!;
            var builder = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                builder.Append(c);
                if (!IsSentenceEndChar(c))
                    continue;

                // 缩写（Mr./Dr.）与小数（3.14）后的句点不断句，继续累积
                if (IsFalsePeriodEnd(value, i))
                    continue;

                // 吸收紧跟的句末符号（如 ?!、...）和右引号/右括号
                while (i + 1 < value.Length && (IsSentenceEndChar(value[i + 1]) || IsClosingChar(value[i + 1])))
                    builder.Append(value[++i]);

                string part = builder.ToString().Trim();
                if (part.Length > 0)
                    parts.Add(part);
                builder.Clear();
            }

            string tail = builder.ToString().Trim();
            if (tail.Length > 0)
                parts.Add(tail);

            return parts;
        }

        /// <summary>
        /// 在句子集合中定位被点击的自动框：优先整句包含，其次用块首片段匹配跨句块。
        /// </summary>
        public static PdfSentence? FindSentenceForBlock(IReadOnlyList<PdfSentence>? sentences, string? blockText)
        {
            if (sentences == null || sentences.Count == 0 || string.IsNullOrWhiteSpace(blockText))
                return null;

            string block = Normalize(blockText);
            if (block.Length == 0)
                return null;

            foreach (var sentence in sentences)
            {
                string text = Normalize(sentence.Text);
                if (text.Length == 0)
                    continue;

                if (text.Contains(block, StringComparison.Ordinal) || block.Contains(text, StringComparison.Ordinal))
                    return sentence;
            }

            // 一个自动框可能包含多句：用块首片段定位起始句
            string probe = block.Length > 12 ? block[..12] : block;
            foreach (var sentence in sentences)
            {
                if (Normalize(sentence.Text).Contains(probe, StringComparison.Ordinal))
                    return sentence;
            }

            return null;
        }

        /// <summary>去掉空白并转小写，用于跨行拼接差异下的文本匹配。</summary>
        public static string Normalize(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var builder = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c) || c == '\u200B')
                    continue;
                builder.Append(char.ToLowerInvariant(c));
            }
            return builder.ToString();
        }

        private static void AppendLine(StringBuilder builder, string text)
        {
            if (builder.Length == 0)
            {
                builder.Append(text);
                return;
            }

            char prev = builder[builder.Length - 1];
            if (prev == '-' && builder.Length > 1)
            {
                // 英文跨行连字符：去掉连字符后直接拼接
                builder.Length--;
                builder.Append(text);
                return;
            }

            // 中文与中文/中英混排不加空格，其余按空格拼接
            if (IsCjk(prev) || IsCjk(text[0]))
                builder.Append(text);
            else
                builder.Append(' ').Append(text);
        }

        private static void Flush(List<PdfSentence> result, int pageIndex, StringBuilder builder, List<PdfTextLine> lines)
        {
            string text = builder.ToString().Trim();
            builder.Clear();
            if (text.Length == 0)
            {
                lines.Clear();
                return;
            }

            result.Add(new PdfSentence
            {
                PageIndex = pageIndex,
                Text = text,
                Lines = new List<PdfTextLine>(lines)
            });
            lines.Clear();
        }

        private static void AssignIndexes(List<PdfSentence> sentences)
        {
            for (int i = 0; i < sentences.Count; i++)
                sentences[i].IndexInPage = i;
        }

        private static bool IsSentenceEnd(string text)
        {
            int i = text.Length - 1;
            while (i >= 0 && IsClosingChar(text[i]))
                i--;
            if (i < 0 || !IsSentenceEndChar(text[i]))
                return false;

            return !IsFalsePeriodEnd(text, i);
        }

        /// <summary>
        /// 判断句点是否为“假句末”：小数（3.14）、缩写（Mr./Dr./e.g.）、
        /// 以及单字母人名首字母（J. Smith）后的句点都应继续累积，不在此断句。
        /// </summary>
        private static bool IsFalsePeriodEnd(string text, int index)
        {
            if (index < 0 || index >= text.Length || text[index] != '.')
                return false;

            // 小数：数字.数字
            if (index > 0 && index + 1 < text.Length
                && char.IsDigit(text[index - 1]) && char.IsDigit(text[index + 1]))
            {
                return true;
            }

            int start = index - 1;
            while (start >= 0 && char.IsLetter(text[start]))
                start--;

            string word = text.Substring(start + 1, index - start - 1);
            // 单字母（人名首字母、e.g. 的 e/g）不断句
            if (word.Length <= 1)
                return word.Length == 1;

            return Abbreviations.Contains(word.ToLowerInvariant());
        }

        private static bool IsSentenceEndChar(char c) => SentenceEnds.IndexOf(c) >= 0;

        private static bool IsClosingChar(char c) => ClosingChars.IndexOf(c) >= 0;

        private static bool IsCjk(char c) =>
            (c >= 0x4E00 && c <= 0x9FFF) ||
            (c >= 0x3400 && c <= 0x4DBF) ||
            (c >= 0x3000 && c <= 0x303F) ||
            (c >= 0xFF00 && c <= 0xFFEF);
    }
}
