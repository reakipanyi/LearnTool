using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// 高亮服务接口 - 提供PDF高亮标注的增删改查功能
    /// 支持按页面、按目录获取高亮，以及添加笔记
    /// </summary>
    public interface IHighlightService
    {
        /// <summary>
        /// 获取PDF的所有高亮列表
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <returns>高亮列表</returns>
        List<PdfHighlight> GetHighlights(string pdfPath);

        /// <summary>
        /// 获取PDF的所有高亮（完整信息）
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <returns>高亮列表（包含详细位置信息）</returns>
        List<PdfHighlight> GetAllHighlights(string pdfPath);

        /// <summary>
        /// 获取指定页面的高亮列表
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="pageIndex">页码索引</param>
        /// <returns>该页面的高亮列表</returns>
        List<PdfHighlight> GetHighlightsForPage(string pdfPath, int pageIndex);

        /// <summary>
        /// 按目录获取所有PDF的高亮
        /// 遍历目录下所有PDF文件收集高亮
        /// </summary>
        /// <param name="folderPath">目录路径</param>
        /// <returns>所有高亮列表</returns>
        List<PdfHighlight> GetHighlightsForFolder(string folderPath);

        /// <summary>
        /// 添加高亮
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="pageIndex">页码索引</param>
        /// <param name="normalizedX">归一化X坐标（0-1）</param>
        /// <param name="normalizedY">归一化Y坐标（0-1）</param>
        /// <param name="normalizedWidth">归一化宽度（0-1）</param>
        /// <param name="normalizedHeight">归一化高度（0-1）</param>
        /// <param name="text">高亮选中的文本</param>
        /// <param name="color">高亮颜色</param>
        /// <returns>添加的高亮的Id</returns>
        string AddHighlight(string pdfPath, int pageIndex, float normalizedX, float normalizedY, float normalizedWidth, float normalizedHeight, string text = "", HighlightColor color = HighlightColor.Yellow);

        /// <summary>
        /// 异步添加高亮（推荐使用，避免阻塞UI线程）
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="pageIndex">页码索引</param>
        /// <param name="normalizedX">归一化X坐标（0-1）</param>
        /// <param name="normalizedY">归一化Y坐标（0-1）</param>
        /// <param name="normalizedWidth">归一化宽度（0-1）</param>
        /// <param name="normalizedHeight">归一化高度（0-1）</param>
        /// <param name="text">高亮选中的文本</param>
        /// <param name="color">高亮颜色</param>
        /// <returns>添加的高亮的Id</returns>
        Task<string> AddHighlightAsync(string pdfPath, int pageIndex, float normalizedX, float normalizedY, float normalizedWidth, float normalizedHeight, string text = "", HighlightColor color = HighlightColor.Yellow);

        /// <summary>
        /// 批量添加高亮（自动框选使用）：只计算一次文件哈希、只写一次磁盘。
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="highlights">待添加的高亮集合，Id 为空时自动生成</param>
        /// <returns>已落库的高亮集合（含最终 Id）</returns>
        Task<IReadOnlyList<PdfHighlight>> AddHighlightsAsync(
            string pdfPath,
            IReadOnlyList<PdfHighlight> highlights);

        /// <summary>
        /// 删除指定页面由自动高亮生成的高亮。
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="pageIndex">页码索引</param>
        /// <param name="autoGroupId">可选：只删除指定自动组</param>
        /// <returns>被删除的高亮集合</returns>
        List<PdfHighlight> RemoveAutoHighlightsForPage(string pdfPath, int pageIndex, string? autoGroupId = null);

        /// <summary>
        /// 删除指定文档的所有自动高亮。
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <returns>被删除的高亮集合</returns>
        List<PdfHighlight> RemoveAllAutoHighlights(string pdfPath);

        /// <summary>
        /// 添加带笔记的高亮
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="pageIndex">页码索引</param>
        /// <param name="normalizedX">归一化X坐标</param>
        /// <param name="normalizedY">归一化Y坐标</param>
        /// <param name="normalizedWidth">归一化宽度</param>
        /// <param name="normalizedHeight">归一化高度</param>
        /// <param name="text">高亮选中的文本</param>
        /// <param name="note">笔记内容</param>
        /// <param name="color">高亮颜色</param>
        void AddHighlightWithNote(string pdfPath, int pageIndex, float normalizedX, float normalizedY, float normalizedWidth, float normalizedHeight, string text, string note, HighlightColor color = HighlightColor.Yellow);

        /// <summary>
        /// 更新高亮的笔记内容
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="highlightId">高亮唯一ID</param>
        /// <param name="note">新的笔记内容</param>
        void UpdateHighlightNote(string pdfPath, string highlightId, string note);

        /// <summary>
        /// 更新高亮已缓存的内容（识别文本/译文），供再次选中时直接回显
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="highlightId">高亮唯一ID</param>
        /// <param name="text">识别文本，null 表示不修改</param>
        /// <param name="translation">译文，null 表示不修改</param>
        void UpdateHighlightContent(string pdfPath, string highlightId, string? text, string? translation);

        /// <summary>
        /// 更新高亮的位置和大小（用于 Select 模式拖拽移动和缩放调整后持久化）
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="highlight">已更新位置/大小的高亮对象</param>
        void UpdateHighlight(string pdfPath, PdfHighlight highlight);

        /// <summary>
        /// 删除高亮
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="highlightId">高亮唯一ID</param>
        void RemoveHighlight(string pdfPath, string highlightId);

        /// <summary>
        /// 批量删除指定PDF的所有高亮（一次磁盘写入，避免逐个删除时反复写文件导致UI卡顿）
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <returns>被删除的高亮列表（可用于撤销栈恢复）</returns>
        List<PdfHighlight> RemoveAllHighlights(string pdfPath);

        /// <summary>
        /// 删除指定页面的所有高亮
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="pageIndex">页码索引</param>
        void RemoveHighlightsForPage(string pdfPath, int pageIndex);

        /// <summary>
        /// 清空所有高亮缓存
        /// </summary>
        void ClearCache();

        /// <summary>
        /// 清空指定PDF的高亮缓存
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        void ClearCacheForPdf(string pdfPath);
    }
}
