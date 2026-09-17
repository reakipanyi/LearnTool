using LearningAssistant.Common;
using LearningAssistant.Models.Config;
using LearningAssistant.Services.Cache;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace LearningAssistant.Services.AI
{
    public abstract class AbstractAIService : IAIService
    {
        protected readonly AiConfig _config;
        protected readonly ICacheService _cacheService;
        protected readonly ILogger _logger;
        protected readonly HttpClient _httpClient;

        /// <summary>
        /// 当前服务实际使用的端点配置（BaseUrl / Model / ApiKey）。
        /// 由工厂按 provider 解析注入，保证 fallback 切换到其他 provider 时使用各自独立的配置。
        /// </summary>
        protected readonly AiEndpoint _endpoint;

        protected AbstractAIService(
            AiConfig config,
            ICacheService cacheService,
            ILogger logger,
            HttpClient httpClient,
            AiEndpoint? endpoint = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            // 未显式注入端点时，按当前全局 Provider 解析（兼容直接 new 的用法）
            _endpoint = endpoint ?? config.ResolveEndpoint(config.Provider);
        }

        public abstract string ModelName { get; }
        public abstract string ProviderName { get; }

        public virtual async Task<string> GetExplanationAsync(string text, string language, string subType, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var cacheKey = $"expl_{GetHash(text)}_{language}_{subType}";
            if (_cacheService.TryGet(cacheKey, out string cached))
            {
                return cached;
            }

            var prompt = BuildExplanationPrompt(text, language, subType);
            if (string.IsNullOrEmpty(prompt))
                return string.Empty;

            var explanation = await CallApiWithRetryAsync(prompt, cancellationToken);
            if (!string.IsNullOrWhiteSpace(explanation))
            {
                _cacheService.Set(cacheKey, explanation, Constants.CacheDuration.ExplanationMinutes);
            }
            return explanation;
        }

        public virtual async Task<string> AskQuestionAsync(string question, string context = "", CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question))
                return string.Empty;

            var cacheKey = $"qa_{GetHash(question)}_{GetHash(context)}";
            if (_cacheService.TryGet(cacheKey, out string cached))
            {
                return cached;
            }

            var prompt = string.IsNullOrWhiteSpace(context)
                ? question
                : $"Context: {context}\n\nQuestion: {question}";

            var answer = await CallApiWithRetryAsync(prompt, cancellationToken);
            if (!string.IsNullOrWhiteSpace(answer))
            {
                _cacheService.Set(cacheKey, answer, Constants.CacheDuration.QAMinutes);
            }
            return answer;
        }

        public virtual async Task<string> GenerateExerciseAsync(string text, string language, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var cacheKey = $"exercise_{GetHash(text)}_{language}";
            if (_cacheService.TryGet(cacheKey, out string cached))
            {
                return cached;
            }

            language = language ?? "中文";
            var prompt = language == "中文"
                ? $"请针对以下内容生成练习题：\n\n{text}\n\n请生成3-5道练习题，包括选择题、填空题或问答题。"
                : $"Please generate exercises for the following content:\n\n{text}\n\nGenerate 3-5 exercises including multiple choice, fill-in-the-blank or short answer questions.";

            var response = await CallApiWithRetryAsync(prompt, cancellationToken);
            if (!string.IsNullOrWhiteSpace(response))
            {
                _cacheService.Set(cacheKey, response, Constants.CacheDuration.ExerciseMinutes);
            }
            return response;
        }

        public virtual async Task<string> SummarizeAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var cacheKey = $"summarize_{GetHash(text)}";
            if (_cacheService.TryGet(cacheKey, out string cached))
            {
                return cached;
            }

            var prompt = $"请简要总结以下文本的主要内容：\n\n{text}\n\n总结要求：简洁明了，突出重点。";

            var response = await CallApiWithRetryAsync(prompt, cancellationToken);
            if (!string.IsNullOrWhiteSpace(response))
            {
                _cacheService.Set(cacheKey, response, Constants.CacheDuration.SummarizeMinutes);
            }
            return response;
        }

        protected abstract string BuildExplanationPrompt(string text, string language, string subType);
        protected abstract Task<string> CallApiAsync(string prompt, CancellationToken cancellationToken = default);

        protected virtual async Task<string> CallApiWithRetryAsync(string prompt, CancellationToken cancellationToken = default)
        {
            const int maxRetries = 3;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return await CallApiAsync(prompt, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // 用户主动取消，不再重试
                    throw;
                }
                catch (Exception ex) when (i < maxRetries - 1 && IsTransientError(ex))
                {
                    _logger.LogWarning(ex, "AI API瞬时错误，正在重试 ({Attempt}/{MaxRetries})", i + 1, maxRetries);
                    await Task.Delay(1000 * (i + 1), cancellationToken);
                }
            }
            throw new InvalidOperationException("AI API调用多次重试后仍然失败");
        }

        /// <summary>
        /// 判断是否为可重试的瞬时错误：
        /// - 超时 / 连接失败等网络层异常（无状态码的 HttpRequestException、TaskCanceledException）
        /// - 服务端错误（5xx）
        /// - 限流（429）
        /// 4xx 客户端错误（认证失败、参数错误等）为确定性错误，直接抛出不重试。
        /// </summary>
        private static bool IsTransientError(Exception ex)
        {
            if (ex is TaskCanceledException)
                return true;

            if (ex is HttpRequestException httpEx && httpEx.StatusCode.HasValue)
            {
                var code = (int)httpEx.StatusCode.Value;
                // 429 Too Many Requests 或 5xx 服务端错误为瞬时；其余（尤其 4xx）不重试
                return code == 429 || code >= 500;
            }

            // 无状态码的 HttpRequestException 通常为连接层错误（DNS、连接被拒绝、超时等），视为瞬时
            return ex is HttpRequestException;
        }

        protected string GetHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        protected string ApiKey => _endpoint.ApiKey;

        /// <summary>
        /// 检查API密钥是否有效
        /// </summary>
        protected bool IsApiKeyValid
        {
            get
            {
                var key = ApiKey;
                return !string.IsNullOrEmpty(key) && key.Length > 10;
            }
        }
    }
}
