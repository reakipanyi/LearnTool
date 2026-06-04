using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using UnifiedLearningAssistant.Models.Config;
using UnifiedLearningAssistant.Services.Cache;

namespace UnifiedLearningAssistant.Services.AI
{
    public abstract class AbstractAIService : IAIService
    {
        protected readonly HttpClient _httpClient;
        protected readonly AiConfig _config;
        protected readonly ICacheService _cacheService;
        protected readonly ILogger _logger;

        protected AbstractAIService(AiConfig config, ICacheService cacheService, ILogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
        }

        public abstract Task<string> GetExplanationAsync(string text, string language, string subType);
        public abstract Task<string> AskQuestionAsync(string question, string context = "");

        protected abstract Task<string> CallApiAsync(string prompt);

        protected async Task<string> CallApiWithRetryAsync(string prompt, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var response = await CallApiAsync(prompt);
                    if (!string.IsNullOrWhiteSpace(response))
                    {
                        return CleanJsonResponse(response);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI API调用失败 (尝试 {Attempt}/{MaxRetries})", i + 1, maxRetries);
                    if (i == maxRetries - 1)
                    {
                        throw;
                    }
                    await Task.Delay(1000 * (i + 1));
                }
            }
            return string.Empty;
        }

        protected string CleanJsonResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return response;

            response = response.Trim();

            int jsonStart = response.IndexOfAny(new[] { '[', '{' });
            if (jsonStart >= 0)
            {
                response = response.Substring(jsonStart);
            }

            try
            {
                var obj = JsonConvert.DeserializeObject(response);
                return obj != null ? JsonConvert.SerializeObject(obj, Formatting.Indented) : response;
            }
            catch
            {
                // 如果JSON解析失败，手动清理
                return CleanInvalidChars(response);
            }
        }

        private static string CleanInvalidChars(string jsonContent)
        {
            StringBuilder cleaned = new StringBuilder();
            bool inString = false;
            bool escapeNext = false;

            foreach (char c in jsonContent)
            {
                if (escapeNext)
                {
                    cleaned.Append(c);
                    escapeNext = false;
                    continue;
                }

                if (c == '\\')
                {
                    escapeNext = true;
                    cleaned.Append(c);
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    cleaned.Append(c);
                    continue;
                }

                if (!inString && (c < 32 || c >= 127))
                {
                    continue;
                }

                cleaned.Append(c);
            }

            return cleaned.ToString();
        }
    }
}
