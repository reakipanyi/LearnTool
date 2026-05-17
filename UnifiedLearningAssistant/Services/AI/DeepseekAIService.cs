using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using UnifiedLearningAssistant.Models.Config;
using UnifiedLearningAssistant.Services.Cache;

namespace UnifiedLearningAssistant.Services.AI
{
    public class DeepseekAIService : AbstractAIService
    {
        public DeepseekAIService(AiConfig config, ICacheService cacheService, ILogger<DeepseekAIService> logger)
            : base(config, cacheService, logger)
        {
        }

        public override async Task<string> GetExplanationAsync(string text, string language, string subType)
        {
            var cacheKey = $"deepseek_exp_{text}_{language}_{subType}";
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

        public override async Task<string> AskQuestionAsync(string question, string context = "")
        {
            var cacheKey = $"deepseek_q_{question.GetHashCode()}";
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

        protected override async Task<string> CallApiAsync(string prompt)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                if (!string.IsNullOrEmpty(_config.ApiKey))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _config.ApiKey);
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

                var response = await _httpClient.PostAsync(_config.BaseUrl, content);
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
                    catch { }

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

        private string BuildExplanationPrompt(string text, string language, string subType)
        {
            var typeName = subType switch
            {
                "识字" => "汉字",
                "组词" => "组词",
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
                    return $"请详细解析这段语文综合内容：{text}\n\n包括：\n1. 内容概要\n2. 重点难点解析\n3. 相关考点\n4. 学习建议";
                }
                return $"请详细解释这个{typeName}：{text}\n\n包括：\n1. 读音/拼音\n2. 含义解释\n3. 用法示例\n4. 相关知识点";
            }
            else
            {
                if (subType == "英语综合")
                {
                    return $"请详细解析这段英语综合内容：{text}\n\n包括：\n1. 内容概要\n2. 重点难点解析\n3. 词汇和语法点\n4. 学习建议";
                }
                return $"请详细解释这个{typeName}：{text}\n\n包括：\n1. 音标\n2. 中文释义\n3. 例句（中英对照）\n4. 用法搭配";
            }
        }
    }
}
