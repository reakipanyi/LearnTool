using System.Security.Cryptography;
using System.Text;
using UnifiedLearningAssistant.Models.Config;

namespace UnifiedLearningAssistant.Services.Pdf
{
    public class BaiduTranslationService : ITranslationService
    {
        private readonly TranslationConfig _config;
        private readonly HttpClient _httpClient;

        public BaiduTranslationService(TranslationConfig config)
        {
            _config = config;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://fanyi-api.baidu.com/api/trans/vip/translate");
        }

        public bool IsAvailable => !string.IsNullOrWhiteSpace(_config.AppKey) && !string.IsNullOrWhiteSpace(_config.AppSecret);

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
                    { "appid", _config.AppKey },
                    { "salt", salt },
                    { "sign", sign }
                };

                var content = new FormUrlEncodedContent(parameters);
                var response = await _httpClient.PostAsync("", content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);

                return result?.trans_result?[0]?.dst?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private string CalculateSign(string text, string salt)
        {
            var input = _config.AppKey + text + salt + _config.AppSecret;
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