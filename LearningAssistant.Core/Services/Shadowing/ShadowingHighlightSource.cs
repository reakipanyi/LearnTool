using System.Text;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Models.Shadowing;

namespace LearningAssistant.Services.Shadowing
{
    /// <summary>整本跟读中的一条高亮句子及其所属页码。</summary>
    public sealed class ShadowingHighlightSentence
    {
        public int PageIndex { get; init; }
        public ShadowingSentence Sentence { get; init; } = new();
    }

    /// <summary>全书高亮整理结果：跟读句列表与被跳过的空文本高亮数量。</summary>
    public sealed class ShadowingHighlightPlan
    {
        public IReadOnlyList<ShadowingHighlightSentence> Sentences { get; init; } = Array.Empty<ShadowingHighlightSentence>();
        public int SkippedCount { get; init; }
    }

    /// <summary>
    /// 把全书高亮整理成稳定的跟读句：按页、纵向位置排序，过滤空文本，
    /// 并使用带前缀的高亮 ID，避免与按页分句产生的录音键冲突。
    /// </summary>
    public static class ShadowingHighlightSource
    {
        public const string SentenceIdPrefix = "hl:";

        public static ShadowingHighlightPlan Build(IEnumerable<PdfHighlight>? highlights)
        {
            var sentences = new List<ShadowingHighlightSentence>();
            if (highlights == null)
                return new ShadowingHighlightPlan { Sentences = sentences, SkippedCount = 0 };

            int skipped = 0;
            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var highlight in highlights
                         .Where(h => h != null)
                         .OrderBy(h => h.PageIndex)
                         .ThenBy(h => h.NormalizedY)
                         .ThenBy(h => h.NormalizedX)
                         .ThenBy(h => h.CreatedAt)
                         .ThenBy(h => h.Id, StringComparer.Ordinal))
            {
                string text = NormalizeText(highlight.Text);
                if (text.Length == 0)
                {
                    skipped++;
                    continue;
                }

                string sentenceId = SentenceIdPrefix + highlight.Id;
                if (!seenIds.Add(sentenceId))
                    continue;

                sentences.Add(new ShadowingHighlightSentence
                {
                    PageIndex = highlight.PageIndex,
                    Sentence = new ShadowingSentence
                    {
                        Id = sentenceId,
                        Index = sentences.Count,
                        StartOffset = 0,
                        Length = text.Length,
                        Text = text
                    }
                });
            }

            return new ShadowingHighlightPlan
            {
                Sentences = sentences,
                SkippedCount = skipped
            };
        }

        private static string NormalizeText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var builder = new StringBuilder(text.Length);
            bool pendingSpace = false;
            foreach (char ch in text.Trim())
            {
                if (char.IsWhiteSpace(ch))
                {
                    pendingSpace = builder.Length > 0;
                    continue;
                }

                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }

                builder.Append(ch);
            }

            return builder.ToString();
        }
    }
}
