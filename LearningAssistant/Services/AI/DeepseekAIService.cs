using LearningAssistant.Models.Config;
using LearningAssistant.Services.Cache;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace LearningAssistant.Services.AI
{
    public class DeepseekAIService : AbstractAIService
    {
        public DeepseekAIService(AiConfig config, ICacheService cacheService, ILogger<DeepseekAIService> logger, HttpClient httpClient)
            : base(config, cacheService, logger, httpClient)
        {
        }

        public override string ModelName => "Deepseek";
        public override string ProviderName => "deepseek";

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
                _httpClient.DefaultRequestHeaders.Clear();
                string apiKey = DecryptedApiKey;
                if (!string.IsNullOrEmpty(apiKey))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", apiKey);
                }

                var requestBody = new
                {
                    model = _config.Model,
                    messages = new[]
                    {
                        new { role = "system", content = "你是一个专业的语言学习助手，请用简洁明了的方式解释词语和回答问题。" },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 1000,
                    temperature = 0.7
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_config.BaseUrl, content, cancellationToken);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string errorDetail = $"Deepseek API错误 ({response.StatusCode})";
                    try
                    {
                        var errorObj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseJson);
                        if (errorObj != null && errorObj.error != null)
                            errorDetail += $": {errorObj.error}";
                    }
                    catch
                    {
                    }

                    _logger.LogError("Deepseek API调用失败: {Error}", errorDetail);
                    throw new HttpRequestException(errorDetail);
                }

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseJson);
                return result?.choices?[0]?.message?.content?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deepseek API调用异常");
                throw;
            }
        }
    }
}
