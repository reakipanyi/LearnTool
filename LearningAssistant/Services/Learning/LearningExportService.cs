using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Learning
{
    public interface ILearningExportService
    {
        string ExportErrorBook(IExportService exportService, string userId);
    }

    public class LearningExportService : ILearningExportService
    {
        private readonly ILogger<LearningExportService> _logger;

        public LearningExportService(ILogger<LearningExportService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string ExportErrorBook(IExportService exportService, string userId)
        {
            try
            {
                var errorBookItems = exportService.GetErrorBookItems(userId);

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
                    return exportService.ExportErrorBook(userId, saveFileDialog.FileName);
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