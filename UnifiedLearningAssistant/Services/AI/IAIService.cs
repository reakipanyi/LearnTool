namespace UnifiedLearningAssistant.Services.AI
{
    public interface IAIService
    {
        Task<string> GetExplanationAsync(string text, string language, string subType);
        Task<string> AskQuestionAsync(string question, string context = "");
    }

    public class AIResponse
    {
        public string Text { get; set; } = string.Empty;
        public string Phonetic { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;
        public string Grammar { get; set; } = string.Empty;
    }
}