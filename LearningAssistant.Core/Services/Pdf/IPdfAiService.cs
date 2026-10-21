using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// PDF 专用 AI 编排：统一从文本索引取上下文、裁剪长度、约束页码锚点。
    /// 内部仅调用现有 IAIService，不新增 AI Provider。
    /// </summary>
    public interface IPdfAiService
    {
        /// <summary>
        /// 总结指定页码范围（页码从 0 开始，边界自动收敛）。
        /// </summary>
        Task<PdfAiResponse> SummarizePagesAsync(
            string pdfPath,
            int startPageIndex,
            int endPageIndex,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 文档问答：以当前页及邻近页为上下文，答案需带 [[pN]] 页码锚点。
        /// </summary>
        /// <param name="currentPageIndex">当前页（从 0 开始），-1 表示从头按预算取页</param>
        Task<PdfAiResponse> AskDocumentAsync(
            string pdfPath,
            string question,
            int currentPageIndex = -1,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 文档问答（轻量检索）：先按关键词召回相关页及邻近页，再基于召回内容回答。
        /// 无召回页时返回明确提示，不调用 AI。
        /// </summary>
        Task<PdfAiResponse> AskDocumentWithRetrievalAsync(
            string pdfPath,
            string question,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 按作用域问答：选中 / 本页（含前后页）/ 指定页 / 全文，答案带 [[pN]] 页码锚点。
        /// </summary>
        Task<PdfAiResponse> AskScopedAsync(
            string pdfPath,
            string question,
            PdfAiScope scope,
            int currentPageIndex,
            int startPageIndex,
            int endPageIndex,
            string? selectedText = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 解释选中文本：结合所在页上下文，而不是裸文本调用。
        /// </summary>
        Task<PdfAiResponse> ExplainSelectionAsync(
            string pdfPath,
            int pageIndex,
            string selectedText,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 生成结构化大纲（JSON）。
        /// </summary>
        Task<PdfAiResponse> GenerateOutlineAsync(
            string pdfPath,
            int startPageIndex,
            int endPageIndex,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 生成结构化学习笔记（Markdown，含页码锚点）。
        /// </summary>
        Task<PdfAiResponse> GenerateStudyNotesAsync(
            string pdfPath,
            int startPageIndex,
            int endPageIndex,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 生成闪卡（JSON 数组，含页码字段）。
        /// </summary>
        Task<PdfAiResponse> GenerateFlashcardsAsync(
            string pdfPath,
            int startPageIndex,
            int endPageIndex,
            CancellationToken cancellationToken = default);
    }
}
