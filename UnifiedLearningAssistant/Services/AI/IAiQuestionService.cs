namespace UnifiedLearningAssistant.Services.AI
{
    public interface IAiQuestionService
    {
        Task<string> AskAsync(string text, string context = "");
        Task<string> GenerateExerciseAsync(string text, string language);
        Task<string> SummarizeTextAsync(string text);
    }
}