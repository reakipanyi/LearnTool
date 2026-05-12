using System.Text;
using System.Text.Json;

namespace UnifiedLearningAssistant.Services.TTS
{
    /// <summary>
    /// 用于调用 Qwen3-TTS 在线 API 的客户端 (基于 Alibaba Cloud Model Studio)
    /// </summary>
    public class QwenTtsClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _endpoint;

        /// <summary>
        /// 客户端是否可用（检查 API Key 是否配置）
        /// </summary>
        public bool Available => !string.IsNullOrWhiteSpace(_apiKey);

        /// <summary>
        /// 初始化 Qwen3-TTS 客户端
        /// </summary>
        /// <param name="apiKey">从阿里云 Model Studio 获取的 API Key。可以从环境变量 "DASHSCOPE_API_KEY" 获取 [citation:3]</param>
        /// <param name="endpoint">API 端点，默认为国际站地址，使用中国大陆 region 需修改 [citation:4]</param>
        public QwenTtsClient(string? apiKey, string? endpoint)
        {
            _apiKey = apiKey ?? Environment.GetEnvironmentVariable("QWEN_TTS_KEY");
            // 默认使用国际站（新加坡）Endpoint。如果你使用中国大陆资源，请替换为：https://dashscope.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation [citation:3]
            _endpoint = endpoint ?? "https://dashscope-intl.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation";
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) }; // 合成任务可能稍长
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

            // 1. 构建请求体 - 基于阿里云官方 DashScope API 格式 [citation:3]
            var requestBody = new
            {
                model = "qwen3-tts-instruct-flash", // 使用快速版模型，如需要指令控制可使用 qwen3-tts-instruct-flash [citation:4]
                input = new
                {
                    //enable_subtitle = true,
                    text = text,
                    voice = voice,
                    language_type = language, // 建议与文本语种一致 [citation:4]
                    speed = speed, // 调低此值以减慢语速
                    pitch = 1.1,   // 稍微调高音调，会让声音听起来更活泼、更像卡通角色
                    instructions = "语气幽默，语调活泼多变，充满童趣和笑意，节奏轻快，发音清晰，带有欢快的笑意，语速偏慢，声音响亮一些。", // 使用自然语言描述你想要的语音效果
                    volume = 2       // 新增此行，调大此值以增加音量
                                     // 可选参数: pitch (音高), volume (音量), sample_rate 等，根据需求添加 [citation:1]
                },
                parameters = new
                {
                    format = format // 输出音频格式
                }
            };

            string jsonPayload = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // 2. 添加认证头 (Bearer Token) [citation:3]
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

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
            try { _httpClient?.Dispose(); } catch { }
        }
    }
}
