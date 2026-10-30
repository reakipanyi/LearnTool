using LearningAssistant.Models.Config;
using LearningAssistant.Services.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http;

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

            var config = _serviceProvider.GetRequiredService<AiConfig>();
            var cacheService = _serviceProvider.GetRequiredService<ICacheService>();
            var httpClient = _serviceProvider.GetRequiredService<HttpClient>();

            // 关键：按 provider 解析独立的 BaseUrl/Model/ApiKey，
            // 使 fallback 切换到其他 provider 时使用各自配置，而非共享全局配置。
            var endpoint = config.ResolveEndpoint(provider);

            return provider.ToLower() switch
            {
                "doubao" or "豆包" => new DoubaoAIService(
                    config, cacheService,
                    _serviceProvider.GetRequiredService<ILogger<DoubaoAIService>>(),
                    httpClient, endpoint),
                "deepseek" => new DeepseekAIService(
                    config, cacheService,
                    _serviceProvider.GetRequiredService<ILogger<DeepseekAIService>>(),
                    httpClient, endpoint),
                "zhipu" or "glm" => new OpenAICompatibleAIService(
                    config, cacheService,
                    _serviceProvider.GetRequiredService<ILogger<OpenAICompatibleAIService>>(),
                    httpClient, "zhipu", "GLM", endpoint),
                "qwen" or "dashscope" => new OpenAICompatibleAIService(
                    config, cacheService,
                    _serviceProvider.GetRequiredService<ILogger<OpenAICompatibleAIService>>(),
                    httpClient, "qwen", "Qwen", endpoint),
                "spark" => new OpenAICompatibleAIService(
                    config, cacheService,
                    _serviceProvider.GetRequiredService<ILogger<OpenAICompatibleAIService>>(),
                    httpClient, "spark", "Spark", endpoint),
                "wenxin" or "ernie" => new OpenAICompatibleAIService(
                    config, cacheService,
                    _serviceProvider.GetRequiredService<ILogger<OpenAICompatibleAIService>>(),
                    httpClient, "wenxin", "ERNIE", endpoint),
                _ => throw new ArgumentException($"不支持的AI服务提供商: {provider}", nameof(provider))
            };
        }
    }

    public class FallbackAIService : IAIService, IAiChatCompletionService
    {
        private static readonly string[] FallbackProviders = { "deepseek", "doubao", "zhipu", "qwen", "spark", "wenxin" };

        private IAIService _currentService;
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
                    service.GetExplanationAsync(text, language, subType, cancellationToken), ex, cancellationToken);
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
                    service.AskQuestionAsync(question, context, cancellationToken), ex, cancellationToken);
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
                    service.GenerateExerciseAsync(text, language, cancellationToken), ex, cancellationToken);
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
                    service.SummarizeAsync(text, cancellationToken), ex, cancellationToken);
            }
        }

        public string ModelName => _currentService.ModelName;
        public string ProviderName => _currentService.ProviderName;

        /// <inheritdoc />
        public string ModelId => _currentService.ModelId;

        /// <inheritdoc />
        public string ProviderId => _currentService.ProviderId;

        /// <inheritdoc />
        public async Task<string> CompleteAsync(
            string prompt,
            AiChatOptions options,
            CancellationToken cancellationToken = default)
        {
            var chatService = _currentService as IAiChatCompletionService;
            if (chatService == null)
                return await AskQuestionAsync(prompt, string.Empty, cancellationToken).ConfigureAwait(false);

            var startedUtc = DateTime.UtcNow;
            try
            {
                var result = await chatService.CompleteAsync(prompt, options, cancellationToken).ConfigureAwait(false);
                LogAiUsage("complete", prompt.Length, result?.Length ?? 0, options.MaxTokens, startedUtc, fallback: false);
                return result;
            }
            catch (Exception ex) when (IsFallbackEligible(ex))
            {
                _logger.LogWarning(ex, "按次参数 AI 调用失败，尝试切换到备用服务");
                var result = await TryFallbackChatAsync(prompt, options, ex, cancellationToken).ConfigureAwait(false);
                LogAiUsage("complete", prompt.Length, result?.Length ?? 0, options.MaxTokens, startedUtc, fallback: true);
                return result;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<string> StreamAsync(
            string prompt,
            AiChatOptions options,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var chatService = _currentService as IAiChatCompletionService;
            if (chatService == null)
            {
                var text = await AskQuestionAsync(prompt, string.Empty, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(text)) yield return text;
                yield break;
            }

            var enumerator = chatService.StreamAsync(prompt, options, cancellationToken).GetAsyncEnumerator(cancellationToken);
            bool emitted = false;
            Exception? firstError = null;
            int emittedChars = 0;
            var startedUtc = DateTime.UtcNow;

            try
            {
                while (true)
                {
                    bool hasItem;
                    try
                    {
                        hasItem = await enumerator.MoveNextAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex) when (!emitted && IsFallbackEligible(ex))
                    {
                        // 首个 token 之前失败才能安全切换供应商；已开始输出就不再重放，避免内容重复
                        firstError = ex;
                        break;
                    }

                    if (!hasItem) yield break;

                    emitted = true;
                    var item = enumerator.Current;
                    if (!string.IsNullOrEmpty(item))
                    {
                        emittedChars += item.Length;
                        yield return item;
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
                // 观测量：流式输出的字符规模与耗时，用于估算成本与首字延迟
                if (emittedChars > 0)
                    LogAiUsage("stream", prompt.Length, emittedChars, options.MaxTokens, startedUtc, firstError != null);
            }

            if (firstError != null)
            {
                var fallbackText = await TryFallbackChatAsync(prompt, options, firstError, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(fallbackText))
                {
                    LogAiUsage("stream-fallback", prompt.Length, fallbackText.Length, options.MaxTokens, DateTime.UtcNow, fallback: true);
                    yield return fallbackText;
                }
            }
        }

        /// <summary>调用观测日志：供应商/模型、耗时、输入输出规模，便于追踪成本与延迟</summary>
        private void LogAiUsage(string operation, int promptChars, int outputChars, int maxTokens,
            DateTime startedUtc, bool fallback)
        {
            var elapsedMs = (int)Math.Max(0, (DateTime.UtcNow - startedUtc).TotalMilliseconds);
            _logger.LogInformation(
                "AI 调用 {Operation}: provider={Provider}, model={Model}, fallback={Fallback}, elapsedMs={ElapsedMs}, promptChars={PromptChars}, maxTokens={MaxTokens}, outputChars={OutputChars}",
                operation, ProviderId, ModelId, fallback, elapsedMs, promptChars, maxTokens, outputChars);
        }

        /// <summary>
        /// 保存设置后调用：丢弃旧的 provider 与端点快照，按最新 AiConfig 重建当前服务。
        /// </summary>
        public void Reload()
        {
            var provider = _config.Provider;
            _currentService = _factory.CreateService(provider);
            var endpoint = _config.ResolveEndpoint(provider);
            _logger.LogInformation("AI服务配置已重新加载: {Provider}, 模型: {Model}", provider, endpoint.Model);
        }

        private async Task<string> TryFallbackAsync(Func<IAIService, Task<string>> callServiceMethod, Exception originalException, CancellationToken cancellationToken = default)
        {
            var currentProvider = _config.Provider.ToLower();
            Exception? lastException = null;

            // 只尝试真正配置了 API Key 的备用服务商，避免向未配置的服务发无鉴权请求
            var candidates = new List<string>();
            foreach (var provider in FallbackProviders)
            {
                if (string.Equals(provider, currentProvider, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.IsNullOrWhiteSpace(_config.ResolveEndpoint(provider).ApiKey))
                    continue;
                candidates.Add(provider);
            }

            if (candidates.Count == 0)
            {
                _logger.LogWarning(originalException, "AI服务调用失败，且没有其他已配置 API Key 的备用服务商");
                throw new InvalidOperationException(
                    $"AI服务（{currentProvider}）调用失败：{originalException.Message}", originalException);
            }

            foreach (var provider in candidates)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _logger.LogInformation("尝试切换到: {Provider}", provider);
                    var service = _factory.CreateService(provider);
                    var result = await callServiceMethod(service);
                    _logger.LogInformation("切换成功: {Provider}", provider);
                    _currentService = service;
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "{Provider} 也不可用", provider);
                    lastException = ex;
                }
            }

            throw new InvalidOperationException(
                $"所有已配置的备用AI服务都不可用：{lastException?.Message}", lastException ?? originalException);
        }

        /// <summary>
        /// 仅对网络类异常、429 与 5xx 启用 fallback；
        /// 401/403/400 属于配置或参数问题，切换供应商也无法解决，直接失败并提示检查 API Key/模型名。
        /// </summary>
        private static bool IsFallbackEligible(Exception exception)
        {
            if (exception is OperationCanceledException) return false;
            if (exception is TimeoutException) return true;

            if (exception is HttpRequestException httpEx)
            {
                if (!httpEx.StatusCode.HasValue) return true;
                int code = (int)httpEx.StatusCode.Value;
                return code == 429 || code >= 500;
            }

            return false;
        }

        /// <summary>按次参数调用的 fallback：只尝试已配置 API Key 的供应商，避免无鉴权请求</summary>
        private async Task<string> TryFallbackChatAsync(
            string prompt,
            AiChatOptions options,
            Exception originalException,
            CancellationToken cancellationToken)
        {
            var currentProvider = _config.Provider.ToLower();
            Exception? lastException = null;

            var candidates = new List<string>();
            foreach (var provider in FallbackProviders)
            {
                if (string.Equals(provider, currentProvider, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.IsNullOrWhiteSpace(_config.ResolveEndpoint(provider).ApiKey))
                    continue;
                candidates.Add(provider);
            }

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    $"AI服务（{currentProvider}）调用失败：{originalException.Message}", originalException);
            }

            foreach (var provider in candidates)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var service = _factory.CreateService(provider);
                    var chatService = service as IAiChatCompletionService;
                    if (chatService == null) continue;

                    _logger.LogInformation("尝试切换到: {Provider}", provider);
                    var result = await chatService.CompleteAsync(prompt, options, cancellationToken).ConfigureAwait(false);
                    _currentService = service;
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "{Provider} 也不可用", provider);
                    lastException = ex;
                }
            }

            throw new InvalidOperationException(
                $"所有已配置的备用AI服务都不可用：{lastException?.Message}", lastException ?? originalException);
        }
    }
}
