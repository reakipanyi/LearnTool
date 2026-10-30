using LearningAssistant.Models.Config;
using LearningAssistant.Services.Cache;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace LearningAssistant.Services.AI
{
    /// <summary>
    /// 通用OpenAI兼容API服务 - 适用于所有使用 /chat/completions 接口的AI提供商
    /// </summary>
    public class OpenAICompatibleAIService : AbstractAIService, IAiChatCompletionService
    {
        /// <summary>旧功能沿用语言学习助手角色，保持原有行为</summary>
        private const string DefaultSystemPrompt = "你是一个专业的语言学习助手，请用简洁明了的方式解释词语和回答问题。";

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

        /// <inheritdoc />
        public async Task<string> CompleteAsync(
            string prompt,
            AiChatOptions options,
            CancellationToken cancellationToken = default)
        {
            options ??= new AiChatOptions();

            using var timeoutSource = CreateTimeoutSource(options.Timeout, cancellationToken);
            var token = timeoutSource?.Token ?? cancellationToken;

            try
            {
                return await SendCompletionAsync(prompt, options, token).ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (options.JsonMode && ex.StatusCode == HttpStatusCode.BadRequest)
            {
                // 部分兼容接口不支持 response_format，去掉该字段重试一次，避免结构化功能整体失败
                _logger.LogWarning("{Provider} 拒绝 response_format，退化为普通请求重试", _providerName);
                return await SendCompletionAsync(prompt, options with { JsonMode = false }, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"{_providerName} 请求超时");
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<string> StreamAsync(
            string prompt,
            AiChatOptions options,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            options ??= new AiChatOptions();

            using var timeoutSource = CreateTimeoutSource(options.Timeout, cancellationToken);
            var token = timeoutSource?.Token ?? cancellationToken;

            using var request = BuildRequest(prompt, options, stream: true);
            using var response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                await ThrowApiErrorAsync(response, token).ConfigureAwait(false);
            }

            using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (true)
            {
                string? line;
                try
                {
                    line = await reader.ReadLineAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException($"{_providerName} 流式输出超时");
                }

                if (line == null) break;
                if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

                var payload = line[5..].Trim();
                if (payload == "[DONE]") break;
                if (payload.Length == 0) continue;

                var delta = ExtractStreamDelta(payload);
                if (!string.IsNullOrEmpty(delta)) yield return delta;
            }
        }

        protected override async Task<string> CallApiAsync(string prompt, CancellationToken cancellationToken = default)
        {
            // 旧功能保持原参数：推理模型会先用掉一部分预算在 reasoning_content 上，4096 避免正文被截断
            return await CompleteAsync(
                prompt,
                new AiChatOptions { MaxTokens = 4096, Temperature = 0.7 },
                cancellationToken).ConfigureAwait(false);
        }

        private async Task<string> SendCompletionAsync(string prompt, AiChatOptions options, CancellationToken cancellationToken)
        {
            using var request = BuildRequest(prompt, options, stream: false);
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                await ThrowApiErrorAsync(response, cancellationToken).ConfigureAwait(false);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            using var resultDoc = JsonDocument.Parse(responseJson);
            var choices = resultDoc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() > 0)
            {
                var choice = choices[0];
                var message = choice.GetProperty("message");

                string content = string.Empty;
                if (message.TryGetProperty("content", out var contentElement))
                    content = contentElement.GetString() ?? string.Empty;

                // 推理模型（如 deepseek 系列）的正文可能落在 reasoning_content 上；
                // content 为空时回退使用，避免界面上什么都看不到。
                string reasoning = string.Empty;
                if (message.TryGetProperty("reasoning_content", out var reasoningElement))
                    reasoning = reasoningElement.GetString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(content) && !string.IsNullOrWhiteSpace(reasoning))
                {
                    _logger.LogWarning("{Provider} 返回 content 为空，改用 reasoning_content 作为回答内容", _providerName);
                    content = reasoning;
                }

                if (choice.TryGetProperty("finish_reason", out var finishElement) &&
                    finishElement.GetString() == "length")
                {
                    _logger.LogWarning("{Provider} 回答因达到 max_tokens 上限被截断", _providerName);
                }

                return content;
            }
            return string.Empty;
        }

        /// <summary>按 AiChatOptions 构造请求体，stream 为 true 时追加 SSE 开关</summary>
        private HttpRequestMessage BuildRequest(string prompt, AiChatOptions options, bool stream)
        {
            var messages = new[]
            {
                new { role = "system", content = options.SystemPrompt ?? DefaultSystemPrompt },
                new { role = "user", content = prompt }
            };

            var requestBody = new Dictionary<string, object?>
            {
                ["model"] = options.ModelOverride ?? _endpoint.Model,
                ["messages"] = messages,
                ["max_tokens"] = Math.Max(1, options.MaxTokens),
                ["temperature"] = options.Temperature
            };

            if (options.JsonMode)
                requestBody["response_format"] = new { type = "json_object" };
            if (stream)
                requestBody["stream"] = true;

            var json = JsonSerializer.Serialize(requestBody);

            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint.BaseUrl);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            string apiKey = ApiKey;
            if (!string.IsNullOrEmpty(apiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            return request;
        }

        /// <summary>解析 SSE 单条 data 中的增量文本，兼容 reasoning_content</summary>
        private string? ExtractStreamDelta(string payload)
        {
            try
            {
                using var document = JsonDocument.Parse(payload);
                if (!document.RootElement.TryGetProperty("choices", out var choices)
                    || choices.ValueKind != JsonValueKind.Array
                    || choices.GetArrayLength() == 0)
                {
                    return null;
                }

                var choice = choices[0];
                if (!choice.TryGetProperty("delta", out var delta)) return null;

                if (delta.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
                {
                    var text = content.GetString();
                    if (!string.IsNullOrEmpty(text)) return text;
                }

                if (delta.TryGetProperty("reasoning_content", out var reasoning)
                    && reasoning.ValueKind == JsonValueKind.String)
                {
                    var text = reasoning.GetString();
                    if (!string.IsNullOrEmpty(text)) return text;
                }

                return null;
            }
            catch (JsonException)
            {
                // 单条增量解析失败不阻断整条流
                return null;
            }
        }

        /// <summary>把非 2xx 响应转成带状态码的异常，供上层区分瞬时错误与配置错误</summary>
        private async Task ThrowApiErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

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

        private static CancellationTokenSource? CreateTimeoutSource(TimeSpan? timeout, CancellationToken cancellationToken)
        {
            if (!timeout.HasValue || timeout.Value <= TimeSpan.Zero) return null;

            var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            source.CancelAfter(timeout.Value);
            return source;
        }
    }
}
