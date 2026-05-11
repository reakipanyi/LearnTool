using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using UnifiedLearningAssistant.Models.Config;
using UnifiedLearningAssistant.Services.Cache;

namespace UnifiedLearningAssistant.Services.AI
{
    public class SiliconFlowAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly AiConfig _config;
        private readonly ICacheService _cacheService;
        private readonly ILogger<SiliconFlowAIService> _logger;

        public SiliconFlowAIService(AiConfig config, ICacheService cacheService, ILogger<SiliconFlowAIService> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
            _httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
        }

        public async Task<string> GetExplanationAsync(string text, string language, string subType)
        {
            var cacheKey = $"exp_{text}_{language}_{subType}";
            if (_cacheService.TryGet(cacheKey, out string cached))
            {
                return cached;
            }

            var prompt = BuildExplanationPrompt(text, language, subType);
            var response = await CallApi(prompt);

            if (!string.IsNullOrWhiteSpace(response))
            {
                _cacheService.Set(cacheKey, response, 60 * 24 * 7);
            }

            return response;
        }

        public async Task<string> AskQuestionAsync(string question, string context = "")
        {
            var cacheKey = $"q_{question}_{context.GetHashCode()}";
            if (_cacheService.TryGet(cacheKey, out string cached))
            {
                return cached;
            }

            var prompt = context != ""
                ? $"基于以下上下文回答问题：\n{context}\n\n问题：{question}"
                : question;

            var response = await CallApi(prompt);

            if (!string.IsNullOrWhiteSpace(response))
            {
                _cacheService.Set(cacheKey, response, 60 * 60);
            }

            return response;
        }

        private string BuildExplanationPrompt(string text, string language, string subType)
        {
            var typeName = subType switch
            {
                "识字" => "汉字",
                "组词" => "组词",
                "成语" => "成语",
                "短语" => "短语",
                "诗词" => "诗词",
                "英语单词" => "英语单词",
                "英语短语" => "英语短语",
                "英语句子" => "英语句子",
                _ => "词语"
            };

            if (language == "中文")
            {
                return $"请详细解释这个{typeName}：{text}\n\n包括：\n1. 读音/拼音\n2. 含义解释\n3. 用法示例\n4. 相关知识点";
            }
            else
            {
                return $"请详细解释这个{typeName}：{text}\n\n包括：\n1. 音标\n2. 中文释义\n3. 例句（中英对照）\n4. 用法搭配";
            }
        }

        private async Task<string> CallApi(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    model = _config.Model,
                    messages = new[]
                    {
                        new { role = "system", content = "你是一个专业的语言学习助手，请用简洁明了的方式解释词语和回答问题。" },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 500
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_config.BaseUrl, content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseJson);

                return result?.choices?[0]?.message?.content?.ToString() ?? string.Empty;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("API call was cancelled");
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call AI API");
                return string.Empty;
            }
        }
    }
}
