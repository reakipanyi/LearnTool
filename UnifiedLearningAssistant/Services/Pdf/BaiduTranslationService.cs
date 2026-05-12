using System.Security.Cryptography;
using System.Text;
using UnifiedLearningAssistant.Models.Config;

namespace UnifiedLearningAssistant.Services.Pdf
{
    public class BaiduTranslationService : ITranslationService, IDisposable
    {
        private readonly TranslationConfig _config;
        private readonly HttpClient _httpClient;
        private bool _disposed = false;

        public BaiduTranslationService(TranslationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://fanyi-api.baidu.com/api/trans/vip/translate"),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public bool IsAvailable => !string.IsNullOrWhiteSpace(_config.BaiduAppId) && !string.IsNullOrWhiteSpace(_config.BaiduSecret);

        public async Task<string?> TranslateAsync(string text, string from = "auto", string to = "zh")
        {
            if (!IsAvailable || string.IsNullOrWhiteSpace(text))
                return null;

            try
            {
                var salt = DateTime.Now.Ticks.ToString();
                var sign = CalculateSign(text, salt);

                var parameters = new Dictionary<string, string>
                {
                    { "q", text },
                    { "from", from },
                    { "to", to },
                    { "appid", _config.BaiduAppId },
                    { "salt", salt },
                    { "sign", sign }
                };

                var content = new FormUrlEncodedContent(parameters);
                var response = await _httpClient.PostAsync("", content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);

                if (result?.trans_result != null)
                {
                    var translationResults = new List<string>();
                    foreach (var item in result.trans_result)
                    {
                        var dst = item?.dst?.ToString();
                        if (!string.IsNullOrWhiteSpace(dst))
                        {
                            translationResults.Add(dst);
                        }
                    }
                    return string.Join("\n", translationResults);
                }

                return result?.trans_result?[0]?.dst?.ToString();
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"翻译请求失败: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"翻译请求超时: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"翻译异常: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _httpClient.Dispose();
            }

            _disposed = true;
        }

        private string CalculateSign(string text, string salt)
        {
            var input = _config.BaiduAppId + text + salt + _config.BaiduSecret;
            using var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}