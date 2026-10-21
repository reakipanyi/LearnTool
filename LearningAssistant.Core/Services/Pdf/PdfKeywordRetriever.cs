using LearningAssistant.Models.Pdf;
using System.Text.RegularExpressions;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>文档问答的轻量检索结果</summary>
    public sealed class PdfRetrievalResult
    {
        /// <summary>召回页码（从 0 开始，按页码升序）</summary>
        public IReadOnlyList<int> PageIndexes { get; init; } = Array.Empty<int>();

        /// <summary>从问题中提取的关键词</summary>
        public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();

        /// <summary>是否找到了相关页面</summary>
        public bool HasMatch => PageIndexes.Count > 0;
    }

    /// <summary>
    /// 轻量关键词检索：按页码打分并扩展邻近页，供文档问答召回上下文。
    /// 不引入向量库，只依赖已构建的文本索引。
    /// </summary>
    public static class PdfKeywordRetriever
    {
        /// <summary>问题中最多使用的关键词数量</summary>
        private const int MaxKeywords = 12;

        /// <summary>单页单关键词命中次数上限，避免长页因重复词过度加权</summary>
        private const int MaxHitPerKeyword = 5;

        private static readonly Regex EnglishTokenRegex = new("[A-Za-z0-9_]{2,}", RegexOptions.Compiled);
        private static readonly Regex ChineseRunRegex = new(@"[\u4e00-\u9fff]+", RegexOptions.Compiled);

        /// <summary>常见疑问词/虚词，避免把问题本身当检索词</summary>
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "什么", "怎么", "如何", "为何", "哪些", "哪个", "是否", "请问", "这个", "那个",
            "一下", "可以", "文档", "内容", "说明", "解释", "总结", "主要", "关于", "以及",
            "the", "and", "for", "with", "what", "which", "how", "why", "does", "this", "that"
        };

        /// <summary>
        /// 检索与问题相关的页面。
        /// </summary>
        /// <param name="document">已构建的文档文本索引</param>
        /// <param name="question">用户问题</param>
        /// <param name="maxPages">最多召回页数</param>
        /// <param name="neighborRange">命中页向两侧扩展的页数</param>
        public static PdfRetrievalResult Retrieve(
            PdfTextIndexDocument document,
            string question,
            int maxPages = 5,
            int neighborRange = 1)
        {
            if (document == null || document.PageCount <= 0 || string.IsNullOrWhiteSpace(question))
                return new PdfRetrievalResult();

            var keywords = ExtractKeywords(question);
            if (keywords.Count == 0)
                return new PdfRetrievalResult();

            maxPages = Math.Max(1, maxPages);
            neighborRange = Math.Max(0, neighborRange);

            var scored = new List<(int PageIndex, int Score)>();
            for (int pageIndex = 0; pageIndex < document.PageCount; pageIndex++)
            {
                var text = document.GetPage(pageIndex)?.Text;
                if (string.IsNullOrWhiteSpace(text)) continue;

                int score = ScorePage(text, keywords);
                if (score > 0) scored.Add((pageIndex, score));
            }

            if (scored.Count == 0)
                return new PdfRetrievalResult { Keywords = keywords };

            var topPages = scored
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.PageIndex)
                .Take(maxPages)
                .Select(x => x.PageIndex);

            var recalled = new SortedSet<int>();
            foreach (int pageIndex in topPages)
            {
                recalled.Add(pageIndex);
                for (int offset = 1; offset <= neighborRange; offset++)
                {
                    AddIfHasText(document, recalled, pageIndex - offset);
                    AddIfHasText(document, recalled, pageIndex + offset);
                }
            }

            var pages = recalled.ToArray();

            return new PdfRetrievalResult
            {
                PageIndexes = pages,
                Keywords = keywords
            };
        }

        /// <summary>从问题中提取中英文关键词（中文按二元词切分）</summary>
        public static IReadOnlyList<string> ExtractKeywords(string question)
        {
            var keywords = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(question)) return keywords;

            foreach (Match match in ChineseRunRegex.Matches(question))
            {
                var run = match.Value;
                if (run.Length == 1) continue;

                // 短词组保留整体，长句按二元词切分，兼顾召回率与精度
                if (run.Length <= 4)
                {
                    AddKeyword(keywords, seen, run);
                }

                for (int i = 0; i + 2 <= run.Length; i++)
                {
                    AddKeyword(keywords, seen, run.Substring(i, 2));
                }

                if (keywords.Count >= MaxKeywords) return keywords;
            }

            foreach (Match match in EnglishTokenRegex.Matches(question))
            {
                AddKeyword(keywords, seen, match.Value);
                if (keywords.Count >= MaxKeywords) break;
            }

            return keywords;
        }

        private static void AddKeyword(List<string> keywords, HashSet<string> seen, string keyword)
        {
            if (keywords.Count >= MaxKeywords) return;
            if (string.IsNullOrWhiteSpace(keyword) || StopWords.Contains(keyword)) return;
            if (seen.Add(keyword)) keywords.Add(keyword);
        }

        private static int ScorePage(string text, IReadOnlyList<string> keywords)
        {
            int score = 0;
            foreach (var keyword in keywords)
            {
                int hits = CountOccurrences(text, keyword);
                if (hits <= 0) continue;

                int weight = keyword.Length >= 4 ? 2 : 1;
                score += Math.Min(hits, MaxHitPerKeyword) * weight;
            }
            return score;
        }

        private static int CountOccurrences(string text, string keyword)
        {
            int count = 0;
            int index = 0;
            while (true)
            {
                index = text.IndexOf(keyword, index, StringComparison.OrdinalIgnoreCase);
                if (index < 0) break;
                count++;
                index += keyword.Length;
                if (count >= MaxHitPerKeyword) break;
            }
            return count;
        }

        private static void AddIfHasText(PdfTextIndexDocument document, SortedSet<int> target, int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= document.PageCount) return;
            if (string.IsNullOrWhiteSpace(document.GetPage(pageIndex)?.Text)) return;
            target.Add(pageIndex);
        }
    }
}
