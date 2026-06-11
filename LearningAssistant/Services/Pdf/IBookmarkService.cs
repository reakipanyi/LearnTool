using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// 书签服务接口 - 提供PDF书签的增删改查功能
    /// </summary>
    public interface IBookmarkService
    {
        /// <summary>
        /// 获取PDF的所有书签列表
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <returns>书签列表</returns>
        List<PdfBookmark> GetBookmarks(string pdfPath);

        /// <summary>
        /// 添加书签
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="pageIndex">页码索引</param>
        /// <param name="title">书签标题</param>
        void AddBookmark(string pdfPath, int pageIndex, string title);

        /// <summary>
        /// 移除指定书签
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="pageIndex">页码索引</param>
        /// <param name="title">书签标题</param>
        void RemoveBookmark(string pdfPath, int pageIndex, string title);

        /// <summary>
        /// 移除指定页面的所有书签
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="pageIndex">页码索引</param>
        void RemoveBookmarkByIndex(string pdfPath, int pageIndex);

        /// <summary>
        /// 检查指定页面是否有书签
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="pageIndex">页码索引</param>
        /// <returns>存在返回true</returns>
        bool HasBookmark(string pdfPath, int pageIndex);

        /// <summary>
        /// 清空书签缓存
        /// </summary>
        void ClearCache();
    }
}
