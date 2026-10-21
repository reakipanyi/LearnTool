using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// PDF 文档文本索引：文本层优先，扫描页回退 OCR，并按文件指纹落盘缓存。
    /// </summary>
    public interface IPdfDocumentTextIndex
    {
        /// <summary>
        /// 计算文件指纹（路径 + 大小 + 修改时间）。文件不存在时抛出 FileNotFoundException。
        /// </summary>
        string GetFingerprint(string pdfPath);

        /// <summary>
        /// 尝试读取与当前文件指纹匹配的磁盘缓存，未命中或缓存损坏返回 null。
        /// </summary>
        PdfTextIndexDocument? TryLoadCached(string pdfPath);

        /// <summary>
        /// 获取索引：命中缓存直接返回，否则逐页构建并写入磁盘。
        /// </summary>
        /// <param name="pdfPath">PDF 完整路径</param>
        /// <param name="forceRebuild">true 时忽略缓存强制重建</param>
        /// <param name="progress">构建进度</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task<PdfTextIndexDocument> GetOrBuildAsync(
            string pdfPath,
            bool forceRebuild = false,
            IProgress<PdfTextIndexProgress>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除该文档的全部磁盘索引缓存。
        /// </summary>
        void Invalidate(string pdfPath);
    }
}
