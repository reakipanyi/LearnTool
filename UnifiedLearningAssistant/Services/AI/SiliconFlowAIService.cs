using LearningAssistant.Models.Config;
using LearningAssistant.Services.Cache;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace LearningAssistant.Services.AI
{
    public class SiliconFlowAIService : AbstractAIService
    {
        public SiliconFlowAIService(AiConfig config, ICacheService cacheService, ILogger<SiliconFlowAIService> logger, HttpClient httpClient)
            : base(config, cacheService, logger, httpClient)
        {
        }

        public override string ModelName => "SiliconFlow";

        protected override string BuildExplanationPrompt(string text, string language, string subType)
        {
            // 1. 输入清理：移除有害控制字符，保留换行/制表符
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // 移除零宽字符等特殊不可见字符
            text = Regex.Replace(text, @"[\u200B-\u200D\uFEFF]", "");
            // 保留 \n \r \t，移除其他控制字符
            text = new string(text.Where(c => !char.IsControl(c) || c == '\n' || c == '\r' || c == '\t').ToArray());
            text = text.Trim();

            // 2. 类型映射（统一称呼）
            var typeName = subType switch
            {
                "识字" => "汉字",
                "成语" => "成语",
                "短语" => "短语",
                "诗词" => "诗词",
                "语文综合" => "语文内容",
                "英语单词" => "单词",
                "英语短语" => "短语",
                "英语句子" => "句子",
                "英语综合" => "英语内容",
                _ => "内容"
            };

            // 3. 结构化提示词（强制指定输出格式）
            if (language == "中文")
            {
                if (subType == "语文综合")
                {
                    return $"""
你是专业语文助手。
请解析内容：{text}
要求：300字内，简洁易懂，只输出解析，不要有任何开场白或额外解释。
直接输出结果：
""";
                }

                return $"""
你是专业语文助手，必须严格按以下格式输出，不要有任何额外内容（如“好的”、“以下是”等）：
【读音】{typeName}的拼音
【含义】简明解释（20字内）
【例句】一个简单例句

现在请解释【{text}】：
""";
            }
            else // 英文
            {
                if (subType == "英语综合")
                {
                    return $"""
You are a professional English teacher.
Explain the content: {text}
Rule: Within 300 words, output only explanation, no extra text.
Direct answer:
""";
                }

                return $"""
You are an English teacher. Output exactly in this format (no extra words, no markdown):
IPA: /pronunciation/
Meaning: Chinese translation (simplified)
Example: one simple sentence

Explain this {typeName}: {text}
""";
            }
        }

        protected override async Task<string> CallApiAsync(string prompt)
        {
            try
            {
                // 每个请求独立构建 HttpRequestMessage，避免 DefaultRequestHeaders 被修改
                var requestBody = new
                {
                    model = _config.Model,
                    messages = new[]
                    {
                        new { role = "system", content = "你是一个专业的语言学习助手，请用简洁明了的方式解释词语和回答问题。只输出结果，不要输出任何格式说明或额外话语。" },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 500
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, _config.BaseUrl);
                request.Content = content;
                // 设置 Authorization 头
                string apiKey = DecryptedApiKey;
                if (!string.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                var response = await _httpClient.SendAsync(request);

                // 强制以 UTF-8 读取响应，避免乱码
                var responseBytes = await response.Content.ReadAsByteArrayAsync();
                var responseJson = Encoding.UTF8.GetString(responseBytes);

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
                var rawContent = result?.choices?[0]?.message?.content?.ToString() ?? string.Empty;

                // 后处理清洗：去除乱码、Markdown 标记、多余前缀
                rawContent = CleanAiResponse(rawContent);
                return rawContent;
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

        private string CleanAiResponse(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // 1. 移除所有非法控制字符（保留换行和制表符，但最终结果通常不需要内部换行）
            text = new string(text.Where(c => !char.IsControl(c) || c == '\n' || c == '\r' || c == '\t').ToArray());

            // 2. 移除常见的 Markdown 标记（粗体、斜体、代码块标记）
            text = Regex.Replace(text, @"(\*|_|`){1,3}", "");

            // 3. 移除模型常添加的多余前缀（中文/英文）
            text = Regex.Replace(text, @"^(当然|好的|以下是|解释：|Answer:|Explanation:|Here is|直接输出：|结果：)\s*", "", RegexOptions.IgnoreCase);

            // 4. 移除零宽字符再次确保
            text = Regex.Replace(text, @"[\u200B-\u200D\uFEFF]", "");

            // 5. 合并多余空白行
            text = Regex.Replace(text, @"\n{3,}", "\n\n");
            return text.Trim();
        }
    }
}
