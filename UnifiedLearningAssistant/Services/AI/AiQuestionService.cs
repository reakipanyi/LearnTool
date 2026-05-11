using UnifiedLearningAssistant.Services.Cache;

namespace UnifiedLearningAssistant.Services.AI
{
    public class AiQuestionService : IAiQuestionService
    {
        private readonly IAIService _aiService;
        private readonly ICacheService _cacheService;

        public AiQuestionService(IAIService aiService, ICacheService cacheService)
        {
            _aiService = aiService;
            _cacheService = cacheService;
        }

        public async Task<string> AskAsync(string text, string context = "")
        {
            return await _aiService.AskQuestionAsync(text, context);
        }

        public async Task<string> GenerateExerciseAsync(string text, string language)
        {
            var cacheKey = $"ex_{text}_{language}";
            if (_cacheService.TryGet(cacheKey, out string cached))
            {
                return cached;
            }

            var prompt = language == "中文"
                ? $"请针对以下内容生成练习题：\n\n{text}\n\n请生成3-5道练习题，包括选择题、填空题或问答题。"
                : $"Please generate exercises for the following content:\n\n{text}\n\nGenerate 3-5 exercises including multiple choice, fill-in-the-blank or short answer questions.";

            var response = await _aiService.AskQuestionAsync(prompt);
            
            if (!string.IsNullOrWhiteSpace(response))
            {
                _cacheService.Set(cacheKey, response, 60 * 24 * 3);
            }
            
            return response;
        }

        public async Task<string> SummarizeTextAsync(string text)
        {
            var cacheKey = $"sum_{text.GetHashCode()}";
            if (_cacheService.TryGet(cacheKey, out string cached))
            {
                return cached;
            }

            var prompt = $"请简要总结以下文本的主要内容：\n\n{text}\n\n总结要求：简洁明了，突出重点。";
            var response = await _aiService.AskQuestionAsync(prompt);
            
            if (!string.IsNullOrWhiteSpace(response))
            {
                _cacheService.Set(cacheKey, response, 60 * 60);
            }
            
            return response;
        }
    }
}