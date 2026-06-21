using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// 标注服务接口 - 提供PDF页面标注的加载、保存和管理功能
    /// 支持笔划、文字、橡皮擦等标注操作
    /// </summary>
    public interface IAnnotationService
    {
        /// <summary>
        /// 加载指定页面的标注图像
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="pageIndex">页码索引（从0开始）</param>
        /// <param name="targetWidth">目标渲染宽度</param>
        /// <param name="targetHeight">目标渲染高度</param>
        /// <param name="pageOriginalSize">页面原始尺寸（用于坐标转换）</param>
        /// <returns>标注图层Bitmap，若无标注则返回null</returns>
        Bitmap? LoadAnnotation(string pdfPath, int pageIndex, int targetWidth, int targetHeight, SizeF pageOriginalSize);

        /// <summary>
        /// 保存页面标注
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="pageIndex">页码索引</param>
        /// <param name="overlayBitmap">标注图层Bitmap</param>
        void SaveAnnotation(string pdfPath, int pageIndex, Bitmap overlayBitmap);

        /// <summary>
        /// 清除指定页面的所有标注
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="pageIndex">页码索引</param>
        void ClearAnnotation(string pdfPath, int pageIndex);

        /// <summary>
        /// 添加笔划到指定页面
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="pageIndex">页码索引</param>
        /// <param name="stroke">笔划数据对象</param>
        void AddStroke(string pdfPath, int pageIndex, AnnotationStroke stroke);

        /// <summary>
        /// 添加文字注解到指定页面
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="pageIndex">页码索引</param>
        /// <param name="text">文字注解对象</param>
        void AddText(string pdfPath, int pageIndex, AnnotationText text);

        /// <summary>
        /// 获取指定页面的所有文字注解
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="pageIndex">页码索引</param>
        /// <returns>文字注解集合</returns>
        IEnumerable<AnnotationText> GetTexts(string pdfPath, int pageIndex);

        /// <summary>
        /// 清除指定页面的重做栈（撤销操作时使用）
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="pageIndex">页码索引</param>
        void ClearRedo(string pdfPath, int pageIndex);

        /// <summary>
        /// 获取指定页面的所有笔划列表
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="pageIndex">页码索引</param>
        /// <returns>笔划集合</returns>
        IEnumerable<AnnotationStroke> GetStrokes(string pdfPath, int pageIndex);
    }
}
