using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 导出服务接口 - 提供学习数据的导出功能
    /// </summary>
    public interface IExportService
    {


        /// <summary>
        /// 导出高亮列表到Excel文件
        /// </summary>
        /// <param name="outputPath">输出Excel文件路径</param>
        /// <param name="highlights">高亮列表</param>
        /// <param name="sourcePath">源PDF/文件夹路径</param>
        /// <param name="isImageMode">是否为图片模式</param>
        /// <param name="imageFiles">图片模式下的文件列表</param>
        /// <param name="pdfService">PDF服务实例（PDF模式时使用）</param>
        /// <returns>导出成功返回true</returns>
        Task<bool> ExportHighlightsToExcelAsync(string outputPath, List<PdfHighlight> highlights, string sourcePath, bool isImageMode, List<string>? imageFiles = null, IPdfService? pdfService = null);

        /// <summary>
        /// 导出PDF的所有高亮到Excel文件
        /// </summary>
        /// <param name="outputPath">输出Excel文件路径</param>
        /// <param name="sourcePath">源PDF/文件夹路径</param>
        /// <param name="isImageMode">是否为图片模式</param>
        /// <param name="imageFiles">图片模式下的文件列表</param>
        /// <param name="pdfService">PDF服务实例（PDF模式时使用）</param>
        /// <param name="includeAnnotations">是否包含标注</param>
        /// <returns>导出成功返回true</returns>
        Task<bool> ExportHighlightsToExcelAsync(string outputPath, string sourcePath, bool isImageMode, List<string>? imageFiles = null, IPdfService? pdfService = null, bool includeAnnotations = false);

        /// <summary>
        /// 导出标注笔画到Excel文件
        /// </summary>
        /// <param name="strokes">标注笔画列表</param>
        /// <param name="pdfPath">PDF路径</param>
        /// <param name="outputPath">输出Excel文件路径</param>
        /// <param name="pdfService">PDF服务实例</param>
        /// <param name="pageCount">页数</param>
        /// <returns>导出成功返回true</returns>
        Task<bool> ExportAnnotationsToExcelAsync(List<AnnotationStroke> strokes, string pdfPath, string outputPath, IPdfService? pdfService = null, int pageCount = 0);

    }
}
