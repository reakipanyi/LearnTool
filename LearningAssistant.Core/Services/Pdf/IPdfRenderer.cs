using LearningAssistant.Abstractions;
using System.Drawing;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// PDF渲染服务接口 - 提供PDF页面渲染、缩略图生成和夜间模式
    /// 实现类需要继承IDisposable以确保资源正确释放
    /// </summary>
    public interface IPdfRenderer : IDisposable
    {
        /// <summary>
        /// 异步渲染指定页面（直接返回位图，避免 PNG 编解码）
        /// 返回值由调用方接管并负责释放；渲染器内部的缓存为独立深拷贝。
        /// </summary>
        /// <param name="pageIndex">页码索引（从0开始）</param>
        /// <param name="width">渲染宽度（像素）</param>
        /// <param name="height">渲染高度（像素）</param>
        /// <param name="cancellationToken">取消令牌，快速翻页/缩放时取消上一次请求</param>
        /// <param name="schedulePreRender">是否在渲染完成后调度邻近页预渲染（OCR 等辅助渲染应设为 false）</param>
        /// <returns>渲染后的位图，若失败或已取消返回null</returns>
        Task<Bitmap?> RenderPageAsync(int pageIndex, int width, int height,
            CancellationToken cancellationToken = default, bool schedulePreRender = true);

        /// <summary>
        /// 获取页面缩略图
        /// </summary>
        /// <param name="pageIndex">页码索引（从0开始）</param>
        /// <returns>缩略图byte[]</returns>
        Task<byte[]?> GetThumbnailAsync(int pageIndex);

        /// <summary>
        /// 清空渲染缓存
        /// </summary>
        void ClearCache();

        /// <summary>
        /// 设置夜间模式
        /// </summary>
        /// <param name="enabled">是否启用夜间模式</param>
        void SetNightMode(bool enabled);

        /// <summary>
        /// 是否启用夜间模式
        /// </summary>
        bool IsNightMode { get; }

        /// <summary>
        /// PDF总页数
        /// </summary>
        int PageCount { get; }

        /// <summary>
        /// 当前渲染的文件路径
        /// </summary>
        string CurrentFilePath { get; }

        /// <summary>
        /// 初始化PDF渲染模式
        /// </summary>
        /// <param name="pdfService">PDF服务实例</param>
        /// <param name="filePath">PDF文件路径</param>
        void Initialize(IPdfService pdfService, string filePath);

        /// <summary>
        /// 初始化图片渲染模式（用于扫描版PDF）
        /// </summary>
        /// <param name="imageFiles">图片文件路径列表</param>
        void InitializeImageMode(List<string> imageFiles);

        /// <summary>
        /// 异步生成所有页面的缩略图
        /// </summary>
        Task GenerateThumbnailsAsync();

        /// <summary>
        /// 异步生成当前页面附近的可见缩略图
        /// </summary>
        /// <param name="currentPage">当前页面索引</param>
        /// <param name="visibleCount">当前页面前后可见的页数</param>
        Task GenerateVisibleThumbnailsAsync(int currentPage, int visibleCount = 5);

        /// <summary>
        /// 对byte[]应用夜间模式滤镜（导出/兼容用途，热路径请用 Bitmap 重载）
        /// </summary>
        /// <param name="bitmap">原始byte[]</param>
        /// <returns>应用夜间模式后的byte[]</returns>
        byte[] ApplyNightMode(byte[] bitmap);

        /// <summary>
        /// 对位图应用夜间模式滤镜，返回新的位图（源位图不被修改）
        /// 返回值由调用方接管并负责释放。
        /// </summary>
        /// <param name="source">原始位图</param>
        /// <returns>应用夜间模式后的新位图</returns>
        Bitmap ApplyNightMode(Bitmap source);

        /// <summary>
        /// 缩略图生成完成事件
        /// </summary>
        event EventHandler<ThumbnailGeneratedEventArgs>? ThumbnailGenerated;
    }

    /// <summary>
    /// 缩略图生成事件参数
    /// </summary>
    public class ThumbnailGeneratedEventArgs : EventArgs
    {
        /// <summary>
        /// 页面索引
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// 生成的缩略图
        /// </summary>
        public byte[]? Thumbnail { get; set; }

        /// <summary>
        /// 缩略图所属目录路径（图片模式下用于按目录分组；PDF 模式为空字符串）
        /// </summary>
        public string DirectoryPath { get; set; } = string.Empty;
    }
}
