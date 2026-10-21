using LearningAssistant.Models.Pdf;
using System.Text;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>PDF AI 上下文作用域</summary>
    public enum PdfAiScope
    {
        /// <summary>选中文本</summary>
        Selection = 0,
        /// <summary>当前页及前后各一页</summary>
        CurrentPage = 1,
        /// <summary>指定页码范围</summary>
        SpecifiedPages = 2,
        /// <summary>整份文档（受总字符预算限制）</summary>
        WholeDocument = 3
    }

    /// <summary>上下文构建结果</summary>
    public sealed class PdfAiContextResult
    {
        /// <summary>带 [[pN]] 页码锚点的上下文文本</summary>
        public string Context { get; init; } = string.Empty;

        /// <summary>实际纳入上下文的页码（从 1 开始）</summary>
        public IReadOnlyList<int> IncludedPages { get; init; } = Array.Empty<int>();
    }

    /// <summary>
    /// 按作用域从文本索引构建 AI 上下文，统一裁剪每页长度与总预算。
    /// </summary>
    public static class PdfAiContextBuilder
    {
        /// <summary>总预算剩余低于该值时停止追加页面</summary>
        private const int MinRemainingChars = 80;

        /// <summary>
        /// 构建指定作用域的上下文。
        /// </summary>
        public static PdfAiContextResult Build(
            PdfTextIndexDocument document,
            PdfAiScope scope,
            int currentPageIndex,
            int startPageIndex,
            int endPageIndex,
            string? selectedText,
            int maxCharsPerPage = 4000,
            int maxTotalChars = 24000)
        {
            if (document == null || document.PageCount <= 0)
                return new PdfAiContextResult();

            int pageCount = document.PageCount;
            int current = Math.Clamp(currentPageIndex, 0, pageCount - 1);
            maxCharsPerPage = Math.Max(200, maxCharsPerPage);
            maxTotalChars = Math.Max(500, maxTotalChars);

            if (scope == PdfAiScope.Selection && !string.IsNullOrWhiteSpace(selectedText))
            {
                var selected = selectedText.Trim();
                int limit = Math.Min(maxCharsPerPage, maxTotalChars);
                if (selected.Length > limit) selected = selected[..limit] + "…";
                return new PdfAiContextResult
                {
                    Context = $"[[p{current + 1}]]\n{selected}\n\n",
                    IncludedPages = new[] { current + 1 }
                };
            }

            var pageIndexes = scope switch
            {
                PdfAiScope.CurrentPage => Enumerable.Range(Math.Max(0, current - 1), Math.Min(pageCount, current + 2) - Math.Max(0, current - 1)),
                PdfAiScope.SpecifiedPages => BuildRange(startPageIndex, endPageIndex, pageCount),
                PdfAiScope.WholeDocument => Enumerable.Range(0, pageCount),
                _ => new[] { current }
            };

            var builder = new StringBuilder();
            var includedPages = new List<int>();

            foreach (int pageIndex in pageIndexes)
            {
                int remaining = maxTotalChars - builder.Length;
                if (remaining <= MinRemainingChars) break;

                var text = document.GetPage(pageIndex)?.Text;
                if (string.IsNullOrWhiteSpace(text)) continue;

                int pageLimit = Math.Min(maxCharsPerPage, remaining);
                var pageText = text.Trim();
                if (pageText.Length > pageLimit) pageText = pageText[..pageLimit] + "…";

                var block = $"[[p{pageIndex + 1}]]\n{pageText}\n\n";
                if (block.Length > remaining) block = block[..remaining];

                builder.Append(block);
                includedPages.Add(pageIndex + 1);
            }

            return new PdfAiContextResult
            {
                Context = builder.ToString(),
                IncludedPages = includedPages
            };
        }

        private static IEnumerable<int> BuildRange(int startPageIndex, int endPageIndex, int pageCount)
        {
            int start = Math.Max(0, startPageIndex);
            int end = Math.Min(pageCount - 1, endPageIndex);
            if (end < start) return Array.Empty<int>();
            return Enumerable.Range(start, end - start + 1);
        }
    }
}
