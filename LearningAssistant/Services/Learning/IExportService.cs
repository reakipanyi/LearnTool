namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 导出服务接口 - 提供学习数据的导出功能
    /// </summary>
    public interface IExportService
    {
        /// <summary>
        /// 导出错题本到指定文件路径
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="filePath">目标文件路径（如 CSV、TXT 格式）</param>
        /// <returns>导出成功返回文件路径，失败返回空字符串</returns>
        string ExportErrorBook(string userId, string filePath);

        /// <summary>
        /// 获取用户的错题本项目列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>错题内容列表</returns>
        List<string> GetErrorBookItems(string userId);

        /// <summary>
        /// 通过对话框导出错题本（带UI交互）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>导出结果消息</returns>
        string ExportErrorBookWithDialog(string userId);
    }
}
