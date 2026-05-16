using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
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

            // 只初始化HttpClient，不在构造函数设置Header（避免配置未加载完成）
            _httpClient = new HttpClient();
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
                // ====================== 修复 403 核心代码 ======================
                // 每次请求前 正确设置请求头（和你原版正常代码保持一致）
                _httpClient.DefaultRequestHeaders.Clear();
                if (!string.IsNullOrEmpty(_config.ApiKey))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _config.ApiKey);
                }
                // =============================================================

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

                // 使用完整接口地址（原版正常逻辑）
                var response = await _httpClient.PostAsync(_config.BaseUrl, content);

                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string errorDetail = $"API错误 ({response.StatusCode})";

                    try
                    {
                        var errorObj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseJson);
                        if (errorObj != null)
                        {
                            if (errorObj.error != null)
                                errorDetail += $": {errorObj.error}";
                            else if (errorObj.message != null)
                                errorDetail += $": {errorObj.message}";
                        }
                    }
                    catch
                    {
                        if (responseJson.Length > 0 && responseJson.Length < 500)
                            errorDetail += $": {responseJson}";
                    }

                    _logger.LogError("AI API调用失败: {Error}", errorDetail);
                    throw new HttpRequestException(errorDetail);
                }

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseJson);
                return result?.choices?[0]?.message?.content?.ToString() ?? string.Empty;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("API call was cancelled");
                return string.Empty;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "AI API HTTP错误");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call AI API");
                throw new HttpRequestException($"AI服务调用失败: {ex.Message}", ex);
            }
        }
    }
}
