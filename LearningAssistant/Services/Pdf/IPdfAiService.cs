namespace LearningAssistant.Services.Pdf
{
    public interface IPdfAiService
    {
        Task<string> GetAnswerAsync(string question, string context = "", CancellationToken cancellationToken = default);
    }
}