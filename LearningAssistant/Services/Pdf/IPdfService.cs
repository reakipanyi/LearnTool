namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// PDF服务接口 - 提供PDF文档的加载、渲染、文本提取等功能
    /// 实现类需要继承IDisposable以确保资源正确释放
    /// </summary>
    public interface IPdfService : IDisposable
    {
        /// <summary>
        /// 加载PDF文件
        /// </summary>
        /// <param name="path">PDF文件的完整路径</param>
        void Load(string path);

        /// <summary>
        /// 卸载当前已加载的 PDF 文档并释放其文件句柄，但保留服务实例以便再次 Load。
        /// 用于在删除/重命名当前打开的 PDF 前释放文件锁。
        /// </summary>
        void Unload();

        /// <summary>
        /// PDF总页数
        /// </summary>
        int PageCount { get; }

        /// <summary>
        /// 渲染指定页面为Bitmap图像
        /// </summary>
        /// <param name="pageIndex">页码索引（从0开始）</param>
        /// <param name="width">渲染宽度（像素）</param>
        /// <param name="height">渲染高度（像素）</param>
        /// <returns>渲染后的Bitmap图像对象，失败时返回null</returns>
        Bitmap? RenderPage(int pageIndex, int width, int height);

        /// <summary>
        /// 获取指定页面的原始尺寸
        /// </summary>
        /// <param name="pageIndex">页码索引（从0开始）</param>
        /// <returns>页面的宽高尺寸（单位：点/pt）</returns>
        SizeF GetPageSize(int pageIndex);

        /// <summary>
        /// 提取指定页面的文本内容
        /// </summary>
        /// <param name="pageIndex">页码索引（从0开始）</param>
        /// <returns>页面文本内容</returns>
        string GetPdfText(int pageIndex);

        /// <summary>
        /// 获取PDF文件的总页数（无需加载整个文件）
        /// </summary>
        /// <param name="pdfPath">PDF文件的完整路径</param>
        /// <returns>页数</returns>
        int GetPageCount(string pdfPath);

        /// <summary>
        /// 提取指定页面的文本内容（静态方法，无需Load）
        /// </summary>
        /// <param name="pdfPath">PDF文件的完整路径</param>
        /// <param name="pageNumber">页码（从1开始）</param>
        /// <returns>页面文本内容</returns>
        string ExtractText(string pdfPath, int pageNumber);

        /// <summary>
        /// 打印PDF文档
        /// </summary>
        /// <param name="printDialog">是否显示打印对话框</param>
        /// <param name="fromPage">起始页码（从0开始）</param>
        /// <param name="toPage">结束页码（从0开始，-1表示到最后一页）</param>
        /// <returns>打印是否成功</returns>
        bool Print(bool printDialog = true, int fromPage = 0, int toPage = -1);
    }
}
