using LearningAssistant.Services.AI;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Pdf
{
    public class PdfAiService : IPdfAiService
    {
        private readonly ILogger<PdfAiService> _logger;
        private readonly IAiQuestionService _aiQuestionService;

        public PdfAiService(ILogger<PdfAiService> logger, IAiQuestionService aiQuestionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiQuestionService = aiQuestionService ?? throw new ArgumentNullException(nameof(aiQuestionService));
        }

        public async Task<string> GetAnswerAsync(string question, string context = "", CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await _aiQuestionService.AskAsync(question, context);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("GetAnswerAsync was cancelled");
                return "操作已取消";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get AI answer");
                return "获取答案失败";
            }
        }
    }
}