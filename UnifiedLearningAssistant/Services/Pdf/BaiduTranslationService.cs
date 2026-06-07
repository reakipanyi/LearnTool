using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LearningAssistant.Models.Config;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// 百度翻译服务（整合优化版）
    /// </summary>
    public class BaiduTranslationService : ITranslationService, IDisposable
    {
        #region 配置与常量
        private readonly TranslationConfig _config;
        private readonly HttpClient _httpClient;
        private const int MaxTextLength = 6000;
        private bool _disposed;
        private string? _decryptedAppId;
        private string? _decryptedSecret;
        #endregion

        #region 构造函数
        public BaiduTranslationService(TranslationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // 性能优化 HttpClientHandler
            var handler = new HttpClientHandler
            {
                // 解决SSL验证问题
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                // 关闭自动代理检测，提升速度
                UseProxy = false,
                // 连接复用
                MaxConnectionsPerServer = 10,
                // 关闭自动重定向
                AllowAutoRedirect = false
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://fanyi-api.baidu.com/"),
                Timeout = TimeSpan.FromSeconds(35)
            };

            // 请求头优化
            _httpClient.DefaultRequestHeaders.ConnectionClose = false;
        }
        #endregion

        #region 公开属性
        /// <summary>
        /// 服务是否可用（配置是否完整）
        /// </summary>
        public bool IsAvailable =>
            !string.IsNullOrWhiteSpace(DecryptedAppId) &&
            !string.IsNullOrWhiteSpace(DecryptedSecret);

        private string DecryptedAppId => _decryptedAppId ??= Services.Utils.SecureConfigManager.Decrypt(_config.BaiduAppId);
        private string DecryptedSecret => _decryptedSecret ??= Services.Utils.SecureConfigManager.Decrypt(_config.BaiduSecret);
        #endregion

        #region 核心翻译方法
        /// <summary>
        /// 异步翻译文本（自动清理非法字符、超长校验）
        /// </summary>
        /// <param name="text">待翻译文本</param>
        /// <param name="from">源语种</param>
        /// <param name="to">目标语种</param>
        /// <returns>翻译结果</returns>
        public async Task<string?> TranslateAsync(string text, string from = "auto", string to = "zh")
        {
            // 1. 基础校验
            if (!IsAvailable)
            {
                Console.WriteLine("翻译失败：AppId 或 Secret 未配置");
                return null;
            }

            // 2. 清理非法字符 + 格式化
            text = ClearTranslateUnusableChars(text);
            if (string.IsNullOrWhiteSpace(text))
            {
                Console.WriteLine("待翻译文本为空");
                return null;
            }

            // 3. 超长校验
            if (text.Length > MaxTextLength)
            {
                Console.WriteLine($"文本超长：{text.Length}，最大支持 {MaxTextLength} 字符");
                return null;
            }

            try
            {
                // 4. 生成签名参数
                var salt = DateTime.Now.Ticks.ToString();
                var sign = CalculateSign(text, salt);

                var parameters = new Dictionary<string, string>
                {
                    { "q", text },
                    { "from", from },
                    { "to", to },
                    { "appid", DecryptedAppId },
                    { "salt", salt },
                    { "sign", sign }
                };

                // 5. 发送请求
                using var content = new FormUrlEncodedContent(parameters);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded")
                {
                    CharSet = "UTF-8"
                };

                Console.WriteLine("开始请求百度翻译API...");
                using var cts = new CancellationTokenSource(30000); // 30秒超时
                var response = await _httpClient.PostAsync(
                    "api/trans/vip/translate",
                    content,
                    cts.Token);

                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(cts.Token);
                Console.WriteLine($"API响应：{json}");

                // 6. 解析结果（原生 System.Text.Json）
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // 错误码判断
                if (root.TryGetProperty("error_code", out var errorCode))
                {
                    var errorMsg = root.TryGetProperty("error_msg", out var msg)
                        ? msg.GetString()
                        : "未知错误";
                    Console.WriteLine($"翻译API错误：{errorCode.GetString()} - {errorMsg}");
                    return null;
                }

                // 提取翻译结果
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

                Console.WriteLine("未获取到翻译结果");
                return null;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP请求异常：{ex.Message}");
                return null;
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("翻译请求超时");
                return null;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON解析失败：{ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"翻译异常：{ex.Message}");
                return null;
            }
        }
        #endregion

        #region 工具方法
        /// <summary>
        /// 清理翻译不可用字符，保留中英文、数字、标点、空格
        /// </summary>
        private static string ClearTranslateUnusableChars(string inputText)
        {
            if (string.IsNullOrEmpty(inputText))
                return string.Empty;

            // 保留：中文、英文、数字、中英文逗号句号、空格
            var pattern = @"[^\u4e00-\u9fa5a-zA-Z0-9，。,.\s]";
            var result = Regex.Replace(inputText, pattern, string.Empty);

            // 清除换行
            result = Regex.Replace(result, @"\r\n|\n|\r", string.Empty);

            // 合并连续空格
            result = Regex.Replace(result, @"\s+", " ");

            return result.Trim();
        }

        /// <summary>
        /// 计算百度翻译签名（MD5）
        /// </summary>
        private string CalculateSign(string text, string salt)
        {
            var signStr = $"{DecryptedAppId}{text}{salt}{DecryptedSecret}";
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