using LearningAssistant.Models.Config;
using LearningAssistant.Services.Cache;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace LearningAssistant.Services.AI
{
    public class DoubaoAIService : AbstractAIService
    {
        public DoubaoAIService(AiConfig config, ICacheService cacheService, ILogger<DoubaoAIService> logger, HttpClient httpClient)
            : base(config, cacheService, logger, httpClient)
        {
        }

        public override string ModelName => "Doubao";
        public override string ProviderName => "doubao";

        protected override string BuildExplanationPrompt(string text, string language, string subType)
        {
            var typeName = subType switch
            {
                "识字" => "汉字",
                "成语" => "成语",
                "短语" => "短语",
                "诗词" => "诗词",
                "语文综合" => "语文综合内容",
                "英语单词" => "英语单词",
                "英语短语" => "英语短语",
                "英语句子" => "英语句子",
                "英语综合" => "英语综合内容",
                _ => "词语"
            };

            if (language == "中文")
            {
                if (subType == "语文综合")
                {
                    return $"请简要解析：{text}\n要求：控制在100字以内，简洁明了，突出重点。格式：概要+要点。";
                }
                return $"请简要解释{typeName}：{text}\n要求：控制在100字以内。格式：读音+含义+简单用法示例。";
            }
            else
            {
                if (subType == "英语综合")
                {
                    return $"请简要解析：{text}\n要求：控制在100字以内，简洁明了。格式：概要+要点+学习提示。";
                }
                return $"请简要解释{typeName}：{text}\n要求：控制在100字以内。格式：音标+中文释义+简单例句。";
            }
        }

        protected override async Task<string> CallApiAsync(string prompt, CancellationToken cancellationToken = default)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();

                var requestBody = new
                {
                    model = _config.Model,
                    messages = new[]
                    {
                        new { role = "system", content = "你是一个专业的语言学习助手，请用简洁明了的方式解释词语和回答问题。" },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 1000
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                string apiKey = DecryptedApiKey;
                if (!string.IsNullOrEmpty(apiKey))
                {
                    content.Headers.Add("Authorization", $"Bearer {apiKey}");
                }

                var response = await _httpClient.PostAsync(_config.BaseUrl, content, cancellationToken);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string errorDetail = $"豆包 API错误 ({response.StatusCode})";
                    try
                    {
                        var errorObj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseJson);
                        if (errorObj != null && errorObj.error != null)
                            errorDetail += $": {errorObj.error}";
                    }
                    catch
                    {
                    }

                    _logger.LogError("豆包 API调用失败: {Error}", errorDetail);
                    throw new HttpRequestException(errorDetail);
                }

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseJson);
                return result?.choices?[0]?.message?.content?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "豆包 API调用异常");
                throw;
            }
        }
    }
}
