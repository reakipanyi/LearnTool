using LearningAssistant.Models.Config;
using LearningAssistant.Services.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.AI
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
                    _serviceProvider.GetRequiredService<ILogger<DeepseekAIService>>(),
                    _serviceProvider.GetRequiredService<HttpClient>()),

                "doubao" or "豆包" => new DoubaoAIService(
                    _serviceProvider.GetRequiredService<AiConfig>(),
                    _serviceProvider.GetRequiredService<ICacheService>(),
                    _serviceProvider.GetRequiredService<ILogger<DoubaoAIService>>(),
                    _serviceProvider.GetRequiredService<HttpClient>())
            };
        }
    }

    public class FallbackAIService : IAIService
    {
        private static readonly string[] FallbackProviders = { "deepseek", "doubao" };

        private readonly IAIService _currentService;
        private readonly AiConfig _config;
        private readonly IAIServiceFactory _factory;
        private readonly ILogger<FallbackAIService> _logger;

        public FallbackAIService(AiConfig config, IAIServiceFactory factory, ILogger<FallbackAIService> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _currentService = _factory.CreateService(config.Provider);
            _logger.LogInformation("当前AI服务提供商: {Provider}, 模型: {Model}", config.Provider, config.Model);
        }

        public async Task<string> GetExplanationAsync(string text, string language, string subType, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _currentService.GetExplanationAsync(text, language, subType, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI服务调用失败，尝试切换到备用服务");
                return await TryFallbackAsync(service =>
                    service.GetExplanationAsync(text, language, subType, cancellationToken), cancellationToken);
            }
        }

        public async Task<string> AskQuestionAsync(string question, string context = "", CancellationToken cancellationToken = default)
        {
            try
            {
                return await _currentService.AskQuestionAsync(question, context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI服务调用失败，尝试切换到备用服务");
                return await TryFallbackAsync(service =>
                    service.AskQuestionAsync(question, context, cancellationToken), cancellationToken);
            }
        }

        public async Task<string> GenerateExerciseAsync(string text, string language, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _currentService.GenerateExerciseAsync(text, language, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI服务调用失败，尝试切换到备用服务");
                return await TryFallbackAsync(service =>
                    service.GenerateExerciseAsync(text, language, cancellationToken), cancellationToken);
            }
        }

        public async Task<string> SummarizeAsync(string text, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _currentService.SummarizeAsync(text, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI服务调用失败，尝试切换到备用服务");
                return await TryFallbackAsync(service =>
                    service.SummarizeAsync(text, cancellationToken), cancellationToken);
            }
        }

        public string ModelName => _currentService.ModelName;
        public string ProviderName => _currentService.ProviderName;

        private async Task<string> TryFallbackAsync(Func<IAIService, Task<string>> callServiceMethod, CancellationToken cancellationToken = default)
        {
            var currentProvider = _config.Provider.ToLower();

            foreach (var provider in FallbackProviders)
            {
                if (provider == currentProvider) continue;

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _logger.LogInformation("尝试切换到: {Provider}", provider);
                    var service = _factory.CreateService(provider);
                    var result = await callServiceMethod(service);
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
