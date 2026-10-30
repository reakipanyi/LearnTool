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
    /// 轻量关键词检索：按 BM25 打分并扩展邻近页，供文档问答召回上下文。
    /// 不引入向量库，只依赖已构建的文本索引。
    /// </summary>
    public static class PdfKeywordRetriever
    {
        /// <summary>问题中最多使用的关键词数量</summary>
        private const int MaxKeywords = 12;

        /// <summary>单页单关键词命中次数上限，避免长页因重复词过度加权</summary>
        private const int MaxHitPerKeyword = 5;

        /// <summary>BM25 词频饱和参数</summary>
        private const double K1 = 1.2;

        /// <summary>BM25 文档长度归一参数</summary>
        private const double B = 0.75;

        /// <summary>短语连续命中时每字追加的分数权重</summary>
        private const double PhraseCharWeight = 0.2;

        /// <summary>标题行命中关键词时追加的分数，避免结构信息被正文淹没</summary>
        private const double HeadingHitBoost = 0.4;

        /// <summary>结构加权总分上限，防止标题密集的目录页过度抢占召回</summary>
        private const double MaxStructureBoost = 1.5;

        private static readonly Regex EnglishTokenRegex = new("[A-Za-z0-9_]{2,}", RegexOptions.Compiled);
        private static readonly Regex ChineseRunRegex = new(@"[\u4e00-\u9fff]+", RegexOptions.Compiled);
        private static readonly Regex EnglishPhraseRegex = new(
            @"\b[A-Za-z0-9_]{2,}(?:\s+[A-Za-z0-9_]{2,}){1,2}\b", RegexOptions.Compiled);
        private static readonly Regex HeadingPrefixRegex = new(
            @"^\s*(?:第[一二三四五六七八九十百千\d]+[章节篇部分]|[0-9]+(?:\.[0-9]+)*[、.．]|[（(][一二三四五六七八九十\d]+[)）]|[一二三四五六七八九十]+[、.．])",
            RegexOptions.Compiled);

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

            // 只把有文本的页纳入统计，空页既不计长度也不参与打分
            var textPages = new List<(int Index, string Text)>();
            for (int pageIndex = 0; pageIndex < document.PageCount; pageIndex++)
            {
                var text = document.GetPage(pageIndex)?.Text;
                if (!string.IsNullOrWhiteSpace(text)) textPages.Add((pageIndex, text));
            }
            if (textPages.Count == 0)
                return new PdfRetrievalResult { Keywords = keywords };

            double averageLength = textPages.Average(p => (double)p.Text.Length);
            var documentFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var keyword in keywords)
                documentFrequency[keyword] = Math.Max(1, textPages.Count(p => ContainsIgnoreCase(p.Text, keyword)));

            var phrases = ExtractPhrases(question);

            var scored = new List<(int PageIndex, double Score)>();
            foreach (var page in textPages)
            {
                double score = ScorePage(page.Text, averageLength, textPages.Count, keywords, documentFrequency, phrases);
                if (score > 0) scored.Add((page.Index, score));
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

        /// <summary>从问题中提取短语（连续中文片段、英文词组），用于连续命中加权</summary>
        private static IReadOnlyList<string> ExtractPhrases(string question)
        {
            var phrases = new List<string>();
            if (string.IsNullOrWhiteSpace(question)) return phrases;

            foreach (Match match in ChineseRunRegex.Matches(question))
            {
                if (match.Value.Length >= 3) phrases.Add(match.Value);
            }

            foreach (Match match in EnglishPhraseRegex.Matches(question))
            {
                phrases.Add(match.Value);
            }

            return phrases
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(4)
                .ToList();
        }

        /// <summary>
        /// BM25 打分：IDF 抑制常见词、词频饱和避免长页刷分、长度归一抵消页长差异；
        /// 再叠加短语连续命中与标题行命中，提升答案页与章节页的排序。
        /// </summary>
        private static double ScorePage(
            string text,
            double averageLength,
            int pageCount,
            IReadOnlyList<string> keywords,
            IReadOnlyDictionary<string, int> documentFrequency,
            IReadOnlyList<string> phrases)
        {
            double score = 0;
            double lengthFactor = 1 - B + B * (text.Length / Math.Max(1d, averageLength));

            foreach (var keyword in keywords)
            {
                int hits = CountOccurrences(text, keyword);
                if (hits <= 0) continue;

                int df = documentFrequency.TryGetValue(keyword, out var freq) ? freq : 1;
                double idf = Math.Log(1 + (pageCount - df + 0.5) / (df + 0.5));
                double tfWeight = hits * (K1 + 1) / (hits + K1 * lengthFactor);

                // 较长关键词更具区分度：中文二元词权重低、英文单词与术语权重高
                double specificity = keyword.Length >= 4 ? 1.3 : 1.0;
                score += idf * tfWeight * specificity;
            }

            // 短语连续命中：整句出现通常说明该页就是答案页
            foreach (var phrase in phrases)
            {
                if (ContainsIgnoreCase(text, phrase))
                    score += 1 + phrase.Length * PhraseCharWeight;
            }

            return score + StructureBoost(text, keywords);
        }

        /// <summary>标题行命中关键词时追加分数，并对标题密集页设上限</summary>
        private static double StructureBoost(string text, IReadOnlyList<string> keywords)
        {
            double boost = 0;
            foreach (var line in text.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.Length > 40) continue;
                if (!LooksLikeHeading(trimmed)) continue;

                foreach (var keyword in keywords)
                {
                    if (!ContainsIgnoreCase(trimmed, keyword)) continue;
                    boost += HeadingHitBoost;
                    break;
                }
            }
            return Math.Min(boost, MaxStructureBoost);
        }

        /// <summary>标题行特征：带章节/编号前缀，或短行且不以句末标点结束</summary>
        private static bool LooksLikeHeading(string line)
        {
            if (HeadingPrefixRegex.IsMatch(line)) return true;
            if (line.Length > 24) return false;
            return line[^1] is not ('。' or '.' or '；' or ';' or '，' or ',');
        }

        private static bool ContainsIgnoreCase(string text, string keyword)
            => text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;

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
