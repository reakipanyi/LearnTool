using System.Text;
using System.Text.Json;

namespace LearningAssistant.Services.TTS
{
    public class QwenTtsClient : IDisposable
    {
        private static readonly HttpClient _sharedHttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60),
            DefaultRequestHeaders = { { "User-Agent", "LearningAssistant/1.0" } }
        };

        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;
        private readonly string _endpoint;
        private readonly string _defaultVoice;
        private readonly float _defaultPitch;
        private bool _disposed = false;

        public bool Available => !string.IsNullOrWhiteSpace(_apiKey);

        public QwenTtsClient(string? apiKey, string? endpoint)
            : this(apiKey, endpoint, useSharedClient: true, "Cherry", 1.1f)
        {
        }

        public QwenTtsClient(string? apiKey, string? endpoint, bool useSharedClient)
            : this(apiKey, endpoint, useSharedClient, "Cherry", 1.1f)
        {
        }

        public QwenTtsClient(string? apiKey, string? endpoint, bool useSharedClient, string defaultVoice, float defaultPitch)
        {
            _apiKey = apiKey ?? Environment.GetEnvironmentVariable("QWEN_TTS_KEY") ?? Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY");
            _endpoint = endpoint ?? "https://dashscope-intl.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation";
            _httpClient = useSharedClient ? _sharedHttpClient : new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            _defaultVoice = defaultVoice;
            _defaultPitch = defaultPitch;
        }

        public async Task<byte[]> SynthesizeAsync(
            string text,
            string? voice = null,
            string language = "English",
            float speed = 1.0f,
            string format = "wav",
            float? pitch = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<byte>();

            if (!Available)
                throw new InvalidOperationException("QwenTtsClient 不可用: 未提供 API Key。请在环境变量中设置 QWEN_TTS_KEY 或 DASHSCOPE_API_KEY，或通过构造函数传入。");

            if (language == "zh") language = "Chinese";
            if (language == "en") language = "English";

            var actualVoice = voice ?? _defaultVoice;
            var actualPitch = pitch ?? _defaultPitch;

            var requestBody = new
            {
                model = "qwen3-tts-instruct-flash",
                input = new
                {
                    text = text,
                    voice = actualVoice,
                    language_type = language,
                    speed = speed,
                    pitch = actualPitch,
                    instructions = "用清晰、缓慢、标准的发音进行教学，每个单词之间留有轻微停顿，便于跟读",
                },
                parameters = new
                {
                    format = format
                }
            };

            string jsonPayload = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = content
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new HttpRequestException($"Qwen TTS 请求失败 ({(int)response.StatusCode}): {errorBody}");
            }

            string responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(responseJson);

            if (doc.RootElement.TryGetProperty("output", out var output) &&
                output.TryGetProperty("audio", out var audio) &&
                audio.TryGetProperty("url", out var urlElement))
            {
                string audioUrl = urlElement.GetString();
                if (!string.IsNullOrEmpty(audioUrl))
                {
                    using var audioResponse = await _httpClient.GetAsync(audioUrl).ConfigureAwait(false);
                    audioResponse.EnsureSuccessStatusCode();
                    return await audioResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                }
            }

            throw new InvalidDataException("无法从 API 响应中解析出音频 URL。");
        }

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
                if (_httpClient != _sharedHttpClient)
                {
                    _httpClient?.Dispose();
                }
            }

            _disposed = true;
        }
    }
}