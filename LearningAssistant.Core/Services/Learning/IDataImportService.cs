using LearningAssistant.Models.Learning;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习数据导入导出服务接口
    /// 支持从 CSV、Anki 等格式导入学习数据
    /// </summary>
    public interface IDataImportService
    {
        /// <summary>
        /// 从 CSV 文件导入
        /// </summary>
        /// <param name="filePath">CSV文件路径</param>
        /// <param name="options">导入选项</param>
        /// <returns>导入结果</returns>
        ImportResult ImportFromCsv(string filePath, ImportOptions options);

        /// <summary>
        /// 从 Anki 导出文件导入（支持 .txt 格式）
        /// </summary>
        /// <param name="filePath">Anki导出文件路径</param>
        /// <param name="options">导入选项</param>
        /// <returns>导入结果</returns>
        ImportResult ImportFromAnki(string filePath, ImportOptions options);

        /// <summary>
        /// 从 JSON 文件导入
        /// </summary>
        /// <param name="filePath">JSON文件路径</param>
        /// <param name="options">导入选项</param>
        /// <returns>导入结果</returns>
        ImportResult ImportFromJson(string filePath, ImportOptions options);

        /// <summary>
        /// 导出到 CSV 文件
        /// </summary>
        /// <param name="filePath">目标文件路径</param>
        /// <param name="items">要导出的学习项列表</param>
        /// <param name="options">导出选项</param>
        /// <returns>是否成功</returns>
        bool ExportToCsv(string filePath, List<LearningItem> items, ExportOptions options);

        /// <summary>
        /// 导出到 JSON 文件
        /// </summary>
        /// <param name="filePath">目标文件路径</param>
        /// <param name="items">要导出的学习项列表</param>
        /// <param name="options">导出选项</param>
        /// <returns>是否成功</returns>
        bool ExportToJson(string filePath, List<LearningItem> items, ExportOptions options);

        /// <summary>
        /// 预览 CSV 文件内容（前几行）
        /// </summary>
        /// <param name="filePath">CSV文件路径</param>
        /// <param name="rowCount">预览行数</param>
        /// <returns>预览数据</returns>
        List<string[]> PreviewCsv(string filePath, int rowCount = 5);

        /// <summary>
        /// 获取支持的内容类型列表
        /// </summary>
        /// <returns>内容类型列表</returns>
        List<string> GetSupportedContentTypes();

        /// <summary>
        /// 获取指定内容类型的字段列表
        /// </summary>
        /// <param name="contentType">内容类型</param>
        /// <returns>字段列表</returns>
        List<string> GetContentTypeFields(string contentType);
    }
}
