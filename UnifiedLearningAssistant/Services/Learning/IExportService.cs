namespace UnifiedLearningAssistant.Services.Learning
{
    public interface IExportService
    {
        string ExportErrorBook(string userId, string filePath);
        List<string> GetErrorBookItems(string userId);
    }
}