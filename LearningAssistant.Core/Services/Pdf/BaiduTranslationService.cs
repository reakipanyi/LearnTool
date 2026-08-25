using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LearningAssistant.Models.Config;

namespace LearningAssistant.Services.Pdf
{
    public class BaiduTranslationService : ITranslationService, IDisposable
    {
        #region 配置与常量
        private readonly TranslationConfig _config;
        private readonly HttpClient _httpClient;
        private readonly ILogger<BaiduTranslationService>? _logger;
        private const int MaxTextLength = 6000;
        private const int DefaultSegmentLength = 5000;
        private bool _disposed;
        #endregion

        #region 构造函数
        public BaiduTranslationService(TranslationConfig config, ILogger<BaiduTranslationService>? logger = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger;

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                UseProxy = false,
                MaxConnectionsPerServer = 10,
                AllowAutoRedirect = false
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://fanyi-api.baidu.com/"),
                Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds > 0 ? _config.TimeoutSeconds : 35)
            };

            _httpClient.DefaultRequestHeaders.ConnectionClose = false;
        }
        #endregion

        #region 公开属性
        public bool IsAvailable =>
            !string.IsNullOrWhiteSpace(_config.BaiduAppId) &&
            !string.IsNullOrWhiteSpace(_config.BaiduSecret);

        private string AppId => _config.BaiduAppId;
        private string Secret => _config.BaiduSecret;
        #endregion

        #region 核心翻译方法
        public async Task<string?> TranslateAsync(string text, string from = "auto", string to = "zh")
        {
            if (!IsAvailable)
            {
                _logger?.LogWarning("翻译失败：AppId 或 Secret 未配置");
                return null;
            }

            text = ClearTranslateUnusableChars(text);
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger?.LogDebug("待翻译文本为空");
                return null;
            }

            if (text.Length > MaxTextLength)
            {
                _logger?.LogInformation("文本超长：{Length}，最大支持 {MaxLength} 字符，开始分段翻译", text.Length, MaxTextLength);
                return await TranslateLongTextAsync(text, from, to);
            }

            try
            {
                return await TranslateSingleSegmentAsync(text, from, to);
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "HTTP请求异常");
                return null;
            }
            catch (TaskCanceledException)
            {
                _logger?.LogWarning("翻译请求超时");
                return null;
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex, "JSON解析失败");
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "翻译异常");
                return null;
            }
        }

        private async Task<string?> TranslateLongTextAsync(string text, string from, string to)
        {
            var segments = SplitTextIntoSegments(text, DefaultSegmentLength);
            _logger?.LogInformation("文本分段完成，共 {Count} 段", segments.Count);

            var results = new StringBuilder();
            int successCount = 0;

            for (int i = 0; i < segments.Count; i++)
            {
                try
                {
                    var segmentResult = await TranslateSingleSegmentAsync(segments[i], from, to);
                    if (!string.IsNullOrWhiteSpace(segmentResult))
                    {
                        results.Append(segmentResult);
                        successCount++;
                    }

                    if (i < segments.Count - 1)
                    {
                        await Task.Delay(500);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "翻译第 {Index} 段失败", i + 1);
                }
            }

            _logger?.LogInformation("分段翻译完成，成功 {Success} / {Total} 段", successCount, segments.Count);
            return results.Length > 0 ? results.ToString().Trim() : null;
        }

        private List<string> SplitTextIntoSegments(string text, int maxLength)
        {
            var segments = new List<string>();
            int start = 0;

            while (start < text.Length)
            {
                int end = Math.Min(start + maxLength, text.Length);

                if (end < text.Length)
                {
                    int lastPunctuation = text.LastIndexOfAny(new[] { '。', '！', '？', '.', '!', '?', '；', ';', '\n', '\r' }, end, end - start);
                    if (lastPunctuation > start)
                    {
                        end = lastPunctuation + 1;
                    }
                }

                segments.Add(text.Substring(start, end - start).Trim());
                start = end;
            }

            return segments;
        }

        private async Task<string?> TranslateSingleSegmentAsync(string text, string from, string to)
        {
            var salt = DateTime.Now.Ticks.ToString();
            var sign = CalculateSign(text, salt);

            var parameters = new Dictionary<string, string>
            {
                { "q", text },
                { "from", from },
                { "to", to },
                { "appid", AppId },
                { "salt", salt },
                { "sign", sign }
            };

            using var content = new FormUrlEncodedContent(parameters);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded")
            {
                CharSet = "UTF-8"
            };

            _logger?.LogDebug("开始请求百度翻译API...");
            var timeoutSeconds = _config.TimeoutSeconds > 0 ? _config.TimeoutSeconds : 30;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            var response = await _httpClient.PostAsync(
                "api/trans/vip/translate",
                content,
                cts.Token);

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cts.Token);
            _logger?.LogTrace("API响应：{Json}", json);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("error_code", out var errorCode))
            {
                var errorMsg = root.TryGetProperty("error_msg", out var msg)
                    ? msg.GetString()
                    : "未知错误";
                _logger?.LogWarning("翻译API错误：{ErrorCode} - {ErrorMessage}", errorCode.GetString(), errorMsg);
                return null;
            }

            if (root.TryGetProperty("trans_result", out var transResult) &&
                transResult.ValueKind == JsonValueKind.Array)
            {
                var sb = new StringBuilder();
                foreach (var item in transResult.EnumerateArray())
                {
                    if (item.TryGetProperty("dst", out var dst))
                    {
                        sb.AppendLine(dst.GetString());
                    }
                }
                return sb.ToString().Trim();
            }

            _logger?.LogDebug("未获取到翻译结果");
            return null;
        }
        #endregion

        #region 工具方法
        private static string ClearTranslateUnusableChars(string inputText)
        {
            if (string.IsNullOrEmpty(inputText))
                return string.Empty;

            var pattern = @"[^\u4e00-\u9fa5a-zA-Z0-9，。,.\s]";
            var result = Regex.Replace(inputText, pattern, string.Empty);

            result = Regex.Replace(result, @"\r\n|\n|\r", string.Empty);
            result = Regex.Replace(result, @"\s+", " ");

            return result.Trim();
        }

        private string CalculateSign(string text, string salt)
        {
            var signStr = $"{AppId}{text}{salt}{Secret}";
            using var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(signStr));

            var sb = new StringBuilder();
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }
        #endregion

        #region 释放资源
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _httpClient?.Dispose();
            }

            _disposed = true;
        }

        ~BaiduTranslationService() => Dispose(false);
        #endregion
    }
}