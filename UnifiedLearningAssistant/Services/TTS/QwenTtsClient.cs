using System.Net.Http.Headers;
using System.Text;

namespace UnifiedLearningAssistant.Services.TTS
{
    public class QwenTtsClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public QwenTtsClient(string apiKey, string baseUrl = "https://dashscope.aliyuncs.com/api/v1/services/audio/tts/text-to-audio")
        {
            _apiKey = apiKey;
            _baseUrl = baseUrl;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<byte[]?> SynthesizeAsync(string text, string model = "qwen-tts", string voice = "Cherry", float speed = 1.0f, float volume = 1.0f)
        {
            try
            {
                var requestBody = new
                {
                    model = model,
                    input = new { text = text },
                    parameters = new
                    {
                        voice = voice,
                        rate = speed,
                        volume = volume
                    }
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_baseUrl, content);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch
            {
                return null;
            }
        }
    }
}