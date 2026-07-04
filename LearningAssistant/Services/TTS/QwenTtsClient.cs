using LearningAssistant.Services.Utils;
using System.Text;
using System.Text.Json;

namespace LearningAssistant.Services.TTS
{
    /// <summary>
    /// 用于调用 Qwen3-TTS 在线 API 的客户端 (基于 Alibaba Cloud Model Studio)
    /// </summary>
    public class QwenTtsClient : IDisposable
    {
        private static readonly HttpClient _sharedHttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60),
            DefaultRequestHeaders = { { "User-Agent", "LearningAssistant/1.0" } }
        };

        private readonly HttpClient _httpClient;
        private readonly string? _encryptedApiKey;
        private string? _decryptedApiKey;
        private readonly string _endpoint;
        private bool _disposed = false;

        /// <summary>
        /// 客户端是否可用（检查 API Key 是否配置）
        /// </summary>
        public bool Available => !string.IsNullOrWhiteSpace(DecryptedApiKey);

        private string? DecryptedApiKey
        {
            get
            {
                if (_decryptedApiKey != null) return _decryptedApiKey;

                try
                {
                    _decryptedApiKey = SecureConfigManager.Decrypt(_encryptedApiKey);
                }
                catch
                {
                    _decryptedApiKey = _encryptedApiKey;
                }

                return _decryptedApiKey;
            }
        }

        /// <summary>
        /// 初始化 Qwen3-TTS 客户端
        /// </summary>
        /// <param name="apiKey">从阿里云 Model Studio 获取的 API Key。可以从环境变量 "DASHSCOPE_API_KEY" 获取 [citation:3]</param>
        /// <param name="endpoint">API 端点，默认为国际站地址，使用中国大陆 region 需修改 [citation:4]</param>
        public QwenTtsClient(string? apiKey, string? endpoint)
            : this(apiKey, endpoint, useSharedClient: true)
        {
        }

        /// <summary>
        /// 初始化 Qwen3-TTS 客户端
        /// </summary>
        /// <param name="apiKey">API Key</param>
        /// <param name="endpoint">API端点</param>
        /// <param name="useSharedClient">是否使用共享HttpClient</param>
        public QwenTtsClient(string? apiKey, string? endpoint, bool useSharedClient)
        {
            _encryptedApiKey = apiKey ?? Environment.GetEnvironmentVariable("QWEN_TTS_KEY");
            _endpoint = endpoint ?? "https://dashscope-intl.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation";
            _httpClient = useSharedClient ? _sharedHttpClient : new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        }

        /// <summary>
        /// 合成语音
        /// </summary>
        /// <param name="text">要合成的文本</param>
        /// <param name="voice">音色，例如 "Cherry" (推荐女声), "Dylan", "Eric" 等 [citation:1][citation:9]</param>
        /// <param name="language">语言，例如 "Chinese", "English"，建议与文本语种一致以确保发音准确 [citation:4]</param>
        /// <param name="speed">语速，范围 0.5 到 2.0，默认 1.0 [citation:1]</param>
        /// <param name="format">输出格式，支持 "wav", "mp3", "ogg"，默认 "wav" [citation:1]</param>
        /// <returns>音频文件的字节数组 (WAV 或 MP3 格式)</returns>
        public async Task<byte[]> SynthesizeAsync(
            string text,
            string voice,
            string language,
            float speed,
            string format = "wav")
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<byte>();

            if (!Available)
                throw new InvalidOperationException("QwenTtsClient 不可用: 未提供 API Key。请在环境变量中设置 DASHSCOPE_API_KEY 或通过构造函数传入。");
            if (language == "zh") language = "Chinese";
            if (language == "en") language = "English";
            // 1. 构建请求体 - 基于阿里云官方 DashScope API 格式 [citation:3]
            var requestBody = new
            {
                model = "qwen3-tts-instruct-flash",
                input = new
                {
                    text = text,
                    voice = voice,
                    language_type = language,
                    speed = speed,
                    pitch = 1.1,
                    //instructions = "语气幽默，语调活泼多变，充满童趣和笑意，节奏轻快，发音清晰，带有欢快的笑意，语速偏慢，声音响亮一些。",
                    instructions = "用清晰、缓慢、标准的发音进行教学，每个单词之间留有轻微停顿，便于跟读",
                },
                parameters = new
                {
                    format = format // 支持 wav / mp3 / ogg
                }
            };
            string jsonPayload = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // 2. 添加认证头 (Bearer Token) [citation:3]
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {DecryptedApiKey}");

            // 3. 发送 POST 请求
            using var response = await _httpClient.PostAsync(_endpoint, content).ConfigureAwait(false);

            // 4. 处理响应
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new HttpRequestException($"Qwen TTS 请求失败 ({(int)response.StatusCode}): {errorBody}");
            }

            // 5. 解析响应 JSON 并下载音频
            string responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(responseJson);

            // 根据阿里云 DashScope API 响应格式提取音频 URL [citation:4][citation:9]
            // 响应结构: { "output": { "audio": { "url": "..." } } }
            if (doc.RootElement.TryGetProperty("output", out var output) &&
                output.TryGetProperty("audio", out var audio) &&
                audio.TryGetProperty("url", out var urlElement))
            {
                string audioUrl = urlElement.GetString();
                if (!string.IsNullOrEmpty(audioUrl))
                {
                    // 下载音频文件
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
                // 只释放自己创建的 HttpClient，不释放共享的
                if (_httpClient != _sharedHttpClient)
                {
                    _httpClient?.Dispose();
                }
            }

            _disposed = true;
        }
    }
}
