using Microsoft.Extensions.Logging;
using UnifiedLearningAssistant.Models.Config;
using UnifiedLearningAssistant.Services.Cache;

namespace UnifiedLearningAssistant.Services.AI
{
    public interface IAIServiceFactory
    {
        IAIService CreateService(string provider);
    }

    public class AIServiceFactory : IAIServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AIServiceFactory> _logger;

        public AIServiceFactory(IServiceProvider serviceProvider, ILogger<AIServiceFactory> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IAIService CreateService(string provider)
        {
            _logger.LogInformation("创建AI服务: {Provider}", provider);

            return provider.ToLower() switch
            {
                "deepseek" => new DeepseekAIService(
                    _serviceProvider.GetRequiredService<AiConfig>(),
                    _serviceProvider.GetRequiredService<ICacheService>(),
                    _serviceProvider.GetRequiredService<ILogger<DeepseekAIService>>()),
                    
                "doubao" or "豆包" => new DoubaoAIService(
                    _serviceProvider.GetRequiredService<AiConfig>(),
                    _serviceProvider.GetRequiredService<ICacheService>(),
                    _serviceProvider.GetRequiredService<ILogger<DoubaoAIService>>()),
                    
                "siliconflow" or "千问" => new SiliconFlowAIService(
                    _serviceProvider.GetRequiredService<AiConfig>(),
                    _serviceProvider.GetRequiredService<ICacheService>(),
                    _serviceProvider.GetRequiredService<ILogger<SiliconFlowAIService>>()),
                    
                _ => new SiliconFlowAIService(
                    _serviceProvider.GetRequiredService<AiConfig>(),
                    _serviceProvider.GetRequiredService<ICacheService>(),
                    _serviceProvider.GetRequiredService<ILogger<SiliconFlowAIService>>())
            };
        }
    }

    public class AIServiceProvider : IAIService
    {
        private readonly IAIService _currentService;
        private readonly AiConfig _config;
        private readonly IAIServiceFactory _factory;
        private readonly ILogger<AIServiceProvider> _logger;

        public AIServiceProvider(AiConfig config, IAIServiceFactory factory, ILogger<AIServiceProvider> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _currentService = _factory.CreateService(config.Provider);
            _logger.LogInformation("当前AI服务提供商: {Provider}, 模型: {Model}", config.Provider, config.Model);
        }

        public async Task<string> GetExplanationAsync(string text, string language, string subType)
        {
            try
            {
                return await _currentService.GetExplanationAsync(text, language, subType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI服务调用失败，尝试切换到备用服务");
                return await TryFallbackService(text, language, subType);
            }
        }

        public async Task<string> AskQuestionAsync(string question, string context = "")
        {
            try
            {
                return await _currentService.AskQuestionAsync(question, context);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI服务调用失败，尝试切换到备用服务");
                return await TryFallbackQuestion(question, context);
            }
        }

        private async Task<string> TryFallbackService(string text, string language, string subType)
        {
            var providers = new[] { "deepseek", "doubao", "siliconflow" };
            var currentProvider = _config.Provider.ToLower();

            foreach (var provider in providers)
            {
                if (provider == currentProvider) continue;

                try
                {
                    _logger.LogInformation("尝试切换到: {Provider}", provider);
                    var service = _factory.CreateService(provider);
                    var result = await service.GetExplanationAsync(text, language, subType);
                    _logger.LogInformation("切换成功: {Provider}", provider);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "{Provider} 也不可用", provider);
                }
            }

            throw new Exception("所有AI服务都不可用");
        }

        private async Task<string> TryFallbackQuestion(string question, string context)
        {
            var providers = new[] { "deepseek", "doubao", "siliconflow" };
            var currentProvider = _config.Provider.ToLower();

            foreach (var provider in providers)
            {
                if (provider == currentProvider) continue;

                try
                {
                    _logger.LogInformation("尝试切换到: {Provider}", provider);
                    var service = _factory.CreateService(provider);
                    var result = await service.AskQuestionAsync(question, context);
                    _logger.LogInformation("切换成功: {Provider}", provider);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "{Provider} 也不可用", provider);
                }
            }

            throw new Exception("所有AI服务都不可用");
        }
    }
}
