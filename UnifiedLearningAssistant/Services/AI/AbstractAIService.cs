using LearningAssistant.Models.Config;
using LearningAssistant.Services.Cache;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace LearningAssistant.Services.AI
{
    public abstract class AbstractAIService : IAIService
    {
        protected readonly HttpClient _httpClient;
        protected readonly AiConfig _config;
        protected readonly ICacheService _cacheService;
        protected readonly ILogger _logger;
        private string? _decryptedApiKey;

        protected AbstractAIService(AiConfig config, ICacheService cacheService, ILogger logger, HttpClient httpClient)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        protected string DecryptedApiKey => _decryptedApiKey ??= Services.Utils.SecureConfigManager.Decrypt(_config.ApiKey);

        public virtual async Task<string> GetExplanationAsync(string text, string language, string subType)
        {
            var cacheKey = $"exp_{text.GetHashCode()}_{language}_{subType}";
            if (_cacheService.TryGet(cacheKey, out string cached))
            {
                return cached;
            }

            var prompt = BuildExplanationPrompt(text, language, subType);
            var response = await CallApiWithRetryAsync(prompt);

            if (!string.IsNullOrWhiteSpace(response))
            {
                _cacheService.Set(cacheKey, response, 60 * 24 * 7);
            }

            return response;
        }

        public virtual async Task<string> AskQuestionAsync(string question, string context = "")
        {
            var cacheKey = $"q_{question.GetHashCode()}";
            if (_cacheService.TryGet(cacheKey, out string cached))
            {
                return cached;
            }

            var prompt = context != ""
                ? $"基于以下上下文回答问题：\n{context}\n\n问题：{question}"
                : question;

            var response = await CallApiWithRetryAsync(prompt);

            if (!string.IsNullOrWhiteSpace(response))
            {
                _cacheService.Set(cacheKey, response, 60 * 60);
            }

            return response;
        }

        public abstract string ModelName { get; }

        protected abstract string BuildExplanationPrompt(string text, string language, string subType);
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

            // 移除可能的前缀（如 "```json" 或其他markdown标记）
            if (response.StartsWith("```json"))
            {
                response = response.Substring(7);
            }
            if (response.StartsWith("```"))
            {
                response = response.Substring(3);
            }
            if (response.EndsWith("```"))
            {
                response = response.Substring(0, response.Length - 3);
            }

            response = response.Trim();

            // 检查是否是以 { 或 [ 开头的JSON
            int jsonStart = response.IndexOfAny(new[] { '[', '{' });
            if (jsonStart >= 0)
            {
                string potentialJson = response.Substring(jsonStart);
                
                // 尝试解析JSON
                try
                {
                    var obj = JsonConvert.DeserializeObject(potentialJson);
                    if (obj != null)
                    {
                        // 如果是包含 content 或 explanation 字段的对象，提取纯文本
                        if (obj is Newtonsoft.Json.Linq.JObject jObj)
                        {
                            // 尝试常见的文本字段
                            if (jObj["content"] != null)
                                return jObj["content"]!.ToString().Trim();
                            if (jObj["explanation"] != null)
                                return jObj["explanation"]!.ToString().Trim();
                            if (jObj["text"] != null)
                                return jObj["text"]!.ToString().Trim();
                        }
                        
                        // 如果是字符串数组，提取第一项或拼接
                        if (obj is Newtonsoft.Json.Linq.JArray jArr && jArr.Count > 0)
                        {
                            return jArr[0]!.ToString().Trim();
                        }
                        
                        // 其他情况尝试序列化为格式化JSON
                        var settings = new JsonSerializerSettings
                        {
                            Formatting = Formatting.Indented,
                            StringEscapeHandling = StringEscapeHandling.Default
                        };
                        return JsonConvert.SerializeObject(obj, settings);
                    }
                }
                catch
                {
                    // JSON解析失败，继续处理原始文本
                }
            }

            // 如果不是JSON或解析失败，返回清理后的纯文本
            return CleanInvalidChars(response);
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
