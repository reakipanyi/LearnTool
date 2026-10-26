namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// PDF 选区翻译本地缓存：按文档指纹、页码与 OCR 文本哈希存放，支持翻回页面时回显。
    /// </summary>
    public interface IPdfSelectionTranslationCache
    {
        /// <summary>生成稳定缓存键，供上层去重与命中判断。</summary>
        string BuildKey(string pdfPath, int pageIndex, string ocrText, string targetLanguage);

        /// <summary>尝试命中指定选区缓存。</summary>
        bool TryGet(
            string pdfPath,
            int pageIndex,
            string ocrText,
            string targetLanguage,
            out string translation);

        /// <summary>写入选区翻译结果；空译文不落盘。</summary>
        void Set(
            string pdfPath,
            int pageIndex,
            string ocrText,
            string targetLanguage,
            string translation);

        /// <summary>取指定页最近一条缓存，供翻页自动回显。</summary>
        PdfSelectionTranslationEntry? GetLatest(
            string pdfPath,
            int pageIndex,
            string targetLanguage);
    }

    /// <summary>选区翻译缓存条目。</summary>
    public sealed class PdfSelectionTranslationEntry
    {
        /// <summary>OCR 原文。</summary>
        public string OriginalText { get; set; } = string.Empty;

        /// <summary>译文。</summary>
        public string Translation { get; set; } = string.Empty;

        /// <summary>目标语言代码。</summary>
        public string TargetLanguage { get; set; } = string.Empty;

        /// <summary>页码（0 基）。</summary>
        public int PageIndex { get; set; }

        /// <summary>规范化文本哈希。</summary>
        public string TextHash { get; set; } = string.Empty;

        /// <summary>写入时间（UTC）。</summary>
        public DateTime CreatedAtUtc { get; set; }
    }
}
