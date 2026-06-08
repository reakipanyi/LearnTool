using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using LearningAssistant.Services.Cache;

namespace LearningAssistant.Services.AI
{
    public class AiQuestionService : IAiQuestionService
    {
        private readonly IAIService _aiService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<AiQuestionService> _logger;

        public AiQuestionService(IAIService aiService, ICacheService cacheService, ILogger<AiQuestionService> logger)
        {
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> AskAsync(string text, string context = "")
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            try
            {
                return await _aiService.AskQuestionAsync(text, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI提问失败");
                return "获取答案失败";
            }
        }

        public async Task<string> GenerateExerciseAsync(string text, string language)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            language = language ?? "中文";

            var cacheKey = GenerateCacheKey("ex", text, language);
            if (_cacheService.TryGet(cacheKey, out string cached))
            {
                return cached;
            }

            var prompt = language == "中文"
                ? $"请针对以下内容生成练习题：\n\n{text}\n\n请生成3-5道练习题，包括选择题、填空题或问答题。"
                : $"Please generate exercises for the following content:\n\n{text}\n\nGenerate 3-5 exercises including multiple choice, fill-in-the-blank or short answer questions.";

            try
            {
                var response = await _aiService.AskQuestionAsync(prompt);
                
                if (!string.IsNullOrWhiteSpace(response))
                {
                    _cacheService.Set(cacheKey, response, 60 * 24 * 3);
                }
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成练习题失败");
                return "生成练习题失败";
            }
        }

        public async Task<string> SummarizeTextAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var cacheKey = GenerateCacheKey("sum", text);
            if (_cacheService.TryGet(cacheKey, out string cached))
            {
                return cached;
            }

            var prompt = $"请简要总结以下文本的主要内容：\n\n{text}\n\n总结要求：简洁明了，突出重点。";

            try
            {
                var response = await _aiService.AskQuestionAsync(prompt);
                
                if (!string.IsNullOrWhiteSpace(response))
                {
                    _cacheService.Set(cacheKey, response, 60 * 60);
                }
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文本总结失败");
                return "总结失败";
            }
        }

        private string GenerateCacheKey(params string[] parts)
        {
            var combined = string.Join("_", parts);
            using var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(combined));
            return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}