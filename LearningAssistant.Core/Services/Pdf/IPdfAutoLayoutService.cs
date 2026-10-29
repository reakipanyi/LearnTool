using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// PDF 自动框选版面服务：按页把文字层/OCR 行整理成段落块，并按文档指纹落盘缓存。
    /// 整本任务只生成版面缓存，高亮由界面按页懒加载，避免一次写入大量数据。
    /// </summary>
    public interface IPdfAutoLayoutService
    {
        /// <summary>OCR 是否可用；扫描页整本框选依赖该能力。</summary>
        bool IsOcrAvailable { get; }

        /// <summary>OCR 初始化失败信息。</summary>
        string? OcrInitErrorMessage { get; }

        /// <summary>计算文档指纹；缓存与指纹绑定，文件变化后自动失效。</summary>
        string GetFingerprint(string pdfPath);

        /// <summary>尝试读取已缓存的页面版面；未命中、指纹或分块模式不匹配时返回 null。</summary>
        /// <param name="mergeMultiLine">分块模式：true 合并多行成段，false 每行一块</param>
        PdfAutoLayoutPage? TryGetCachedPage(string pdfPath, int pageIndex, bool mergeMultiLine = true);

        /// <summary>
        /// 获取页面版面：命中缓存直接返回，否则提取文字层（必要时 OCR）并落盘。
        /// </summary>
        /// <param name="pdfPath">PDF 完整路径</param>
        /// <param name="pageIndex">页码索引（从 0 开始）</param>
        /// <param name="allowOcr">无文本层时是否回退 OCR（扫描页）</param>
        /// <param name="forceRebuild">true 时忽略缓存重建</param>
        /// <param name="mergeMultiLine">分块模式：true 合并多行成段，false 每行一块</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task<PdfAutoLayoutPage> GetOrBuildPageAsync(
            string pdfPath,
            int pageIndex,
            bool allowOcr,
            bool forceRebuild = false,
            bool mergeMultiLine = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 整本逐页生成版面缓存（不写高亮）。已完成页命中缓存，支持取消与断点续跑。
        /// </summary>
        /// <param name="pdfPath">PDF 完整路径</param>
        /// <param name="allowOcr">无文本层的页是否回退 OCR</param>
        /// <param name="progress">进度回调</param>
        /// <param name="mergeMultiLine">分块模式：true 合并多行成段，false 每行一块</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task<PdfAutoLayoutSummary> BuildDocumentAsync(
            string pdfPath,
            bool allowOcr,
            IProgress<PdfAutoLayoutProgress>? progress = null,
            bool mergeMultiLine = true,
            CancellationToken cancellationToken = default);

        /// <summary>读取文档级状态；无状态或指纹不匹配时返回默认状态。</summary>
        PdfAutoLayoutState LoadState(string pdfPath);

        /// <summary>保存文档级状态（整本模式开关、被手动清除的页）。</summary>
        void SaveState(string pdfPath, PdfAutoLayoutState state);

        /// <summary>删除单页版面缓存（用户清除本页自动高亮时调用）。</summary>
        void RemovePageCache(string pdfPath, int pageIndex);

        /// <summary>删除该文档所有指纹下的版面缓存与状态。</summary>
        void ClearDocument(string pdfPath);
    }
}
