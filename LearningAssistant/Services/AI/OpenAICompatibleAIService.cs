using LearningAssistant.Models.Config;
using LearningAssistant.Services.Cache;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LearningAssistant.Services.AI
{
    /// <summary>
    /// 通用OpenAI兼容API服务 - 适用于所有使用 /chat/completions 接口的AI提供商
    /// </summary>
    public class OpenAICompatibleAIService : AbstractAIService
    {
        private readonly string _providerName;
        private readonly string _modelName;

        public OpenAICompatibleAIService(
            AiConfig config,
            ICacheService cacheService,
            ILogger logger,
            HttpClient httpClient,
            string providerName,
            string modelName,
            AiEndpoint? endpoint = null)
            : base(config, cacheService, logger, httpClient, endpoint)
        {
            _providerName = providerName;
            _modelName = modelName;
        }

        public override string ModelName => _modelName;
        public override string ProviderName => _providerName;

        protected override string BuildExplanationPrompt(string text, string language, string subType)
        {
            var typeName = subType switch
            {
                "识字" => "汉字",
                "成语" => "成语",
                "短语" => "短语",
                "诗词" => "诗词",
                "语文综合" => "语文内容",
                "英语单词" => "英语单词",
                "英语短语" => "英语短语",
                "英语句子" => "英语句子",
                "英语综合" => "英语内容",
                _ => "词语"
            };

            if (language == "中文")
            {
                if (subType == "语文综合")
                {
                    return $"请简要解析这个内容：{text}\n要求：100字内，简洁解析，只输出解析内容，不要输出其他说明。";
                }
                return $"请简要解释这个{typeName}：{text}\n要求：100字内。只输出解释内容，包括读音、含义和简单用法示例。不要输出格式或其他说明文字。";
            }
            else
            {
                if (subType == "英语综合")
                {
                    return $"请简要解析这个内容：{text}\n要求：100字内，简洁解析。只输出解析内容，不要输出其他说明。";
                }
                return $"请简要解释这个{typeName}：{text}\n要求：100字内。只输出解释内容，包括音标、中文释义和简单例句。不要输出格式或其他说明文字。";
            }
        }

        protected override async Task<string> CallApiAsync(string prompt, CancellationToken cancellationToken = default)
        {
            try
            {
                var requestBody = new
                {
                    model = _endpoint.Model,
                    messages = new[]
                    {
                        new { role = "system", content = "你是一个专业的语言学习助手，请用简洁明了的方式解释词语和回答问题。" },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 1000,
                    temperature = 0.7
                };

                var json = JsonSerializer.Serialize(requestBody);

                using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint.BaseUrl);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                string apiKey = ApiKey;
                if (!string.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string errorDetail = $"{_providerName} API错误 ({response.StatusCode})";
                    try
                    {
                        using var doc = JsonDocument.Parse(responseJson);
                        if (doc.RootElement.TryGetProperty("error", out var errorElement))
                            errorDetail += $": {errorElement.ToString()}";
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning("Failed to parse error response: {Ex}", ex.Message);
                    }

                    _logger.LogError("{Provider} API调用失败: {Error}", _providerName, errorDetail);
                    // 附带状态码，便于上层（重试/fallback）区分瞬时与确定性错误
                    throw new HttpRequestException(errorDetail, null, response.StatusCode);
                }

                using var resultDoc = JsonDocument.Parse(responseJson);
                var choices = resultDoc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var message = choices[0].GetProperty("message");
                    return message.GetProperty("content").GetString() ?? string.Empty;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Provider} API调用异常", _providerName);
                throw;
            }
        }
    }
}
