using Microsoft.Extensions.Logging;
using LearningAssistant.Services.Cache;

namespace LearningAssistant.Services.AI
{
    /// <summary>
    /// AI问题服务 - 作为IAIService的代理，保持向后兼容
    /// 所有方法现在直接调用IAIService的对应方法，异常由调用方处理
    /// </summary>
    public class AiQuestionService : IAiQuestionService
    {
        private readonly IAIService _aiService;
        private readonly ILogger<AiQuestionService> _logger;

        public AiQuestionService(IAIService aiService, ILogger<AiQuestionService> logger)
        {
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 异步提问 - 调用IAIService.AskQuestionAsync
        /// </summary>
        public async Task<string> AskAsync(string text, string context = "", CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return await _aiService.AskQuestionAsync(text, context, cancellationToken);
        }

        /// <summary>
        /// 异步生成练习题 - 调用IAIService.GenerateExerciseAsync
        /// </summary>
        public async Task<string> GenerateExerciseAsync(string text, string language, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return await _aiService.GenerateExerciseAsync(text, language, cancellationToken);
        }

        /// <summary>
        /// 异步摘要文本 - 调用IAIService.SummarizeAsync
        /// </summary>
        public async Task<string> SummarizeTextAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return await _aiService.SummarizeAsync(text, cancellationToken);
        }
    }
}
