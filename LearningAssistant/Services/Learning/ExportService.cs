using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Learning
{
    public class ExportService : IExportService
    {
        private readonly ILogger<ExportService> _logger;
        private readonly IStudyEngine _studyEngine;

        public ExportService(ILogger<ExportService> logger, IStudyEngine studyEngine)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _studyEngine = studyEngine ?? throw new ArgumentNullException(nameof(studyEngine));
        }

        public string ExportErrorBook(string userId, string filePath)
        {
            var items = GetErrorBookItems(userId);

            if (items.Count == 0)
            {
                return "错题本为空，没有可导出的内容！";
            }

            try
            {
                var content = string.Join(Environment.NewLine, items);
                File.WriteAllText(filePath, content, System.Text.Encoding.UTF8);
                _logger.LogInformation($"错题本已导出到 {filePath}，共 {items.Count} 个项目");
                return $"错题本已成功导出到：\n{filePath}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出错题本失败");
                return $"导出错题本失败：{ex.Message}";
            }
        }

        public List<string> GetErrorBookItems(string userId)
        {
            return ((IStudyEngine)_studyEngine).GetUnknownItems(userId);
        }

        public string ExportErrorBookWithDialog(string userId)
        {
            try
            {
                var errorBookItems = GetErrorBookItems(userId);

                if (errorBookItems.Count == 0)
                {
                    return "错题本为空，没有可导出的内容！";
                }

                using var saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "文本文件 (*.txt)|*.txt|CSV文件 (*.csv)|*.csv";
                saveFileDialog.Title = "保存错题本";
                saveFileDialog.FileName = $"错题本_{userId}_{DateTime.Now:yyyyMMdd_HHmmss}";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    return ExportErrorBook(userId, saveFileDialog.FileName);
                }

                return "导出已取消";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export error book");
                return $"导出错题本失败：{ex.Message}";
            }
        }

    }
}