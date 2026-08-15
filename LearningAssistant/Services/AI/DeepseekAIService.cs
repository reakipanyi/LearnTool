using LearningAssistant.Models.Config;
using LearningAssistant.Services.Cache;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.AI
{
    /// <summary>
    /// DeepSeek AI 服务 - 提示词与通用实现完全一致，直接复用 OpenAICompatibleAIService。
    /// </summary>
    public class DeepseekAIService : OpenAICompatibleAIService
    {
        public DeepseekAIService(
            AiConfig config,
            ICacheService cacheService,
            ILogger<DeepseekAIService> logger,
            HttpClient httpClient,
            AiEndpoint? endpoint = null)
            : base(config, cacheService, logger, httpClient, "deepseek", "Deepseek", endpoint)
        {
        }
    }
}
