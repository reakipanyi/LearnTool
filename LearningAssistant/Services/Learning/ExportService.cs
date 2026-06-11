using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Learning
{
    public class ExportService : IExportService
    {
        private readonly ILogger<ExportService> _logger;
        private readonly IProgressService _progressService;

        public ExportService(ILogger<ExportService> logger, IProgressService progressService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _progressService = progressService ?? throw new ArgumentNullException(nameof(progressService));
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
            return _progressService.GetUnknownItems(userId);
        }
    }
}