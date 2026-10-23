using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// PDF 翻译术语表：从文档内容抽取术语并按指纹缓存，供逐页翻译润色复用。
    /// </summary>
    public interface IPdfTranslationGlossaryService
    {
        /// <summary>
        /// 读取或构建术语表；AI 不可用或构建失败时返回 null。
        /// </summary>
        Task<PdfTranslationGlossary?> GetOrBuildAsync(string pdfPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// 在不改变原意的前提下润色机器译文，并强制使用术语表译名。
        /// 失败时返回原始机器译文，保证调用方可直接使用。
        /// </summary>
        Task<PdfTranslationPolishResult> PolishAsync(
            string pdfPath,
            string originalText,
            string machineTranslation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 保存用户编辑后的术语表，覆盖同文档已有缓存；条目为空时清除缓存。
        /// </summary>
        Task<bool> SaveAsync(
            string pdfPath,
            IEnumerable<PdfGlossaryEntry> entries,
            CancellationToken cancellationToken = default);
    }
}
