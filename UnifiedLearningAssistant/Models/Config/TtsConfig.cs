namespace UnifiedLearningAssistant.Models.Config
{
    public class TtsConfig
    {
        public string Provider { get; set; } = "QwenTts";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "qwen-tts";
        public string Voice { get; set; } = "Cherry";
        public float Speed { get; set; } = 1.0f;
        public float Volume { get; set; } = 1.0f;
        public string BaseUrl { get; set; } = "https://dashscope.aliyuncs.com/api/v1/services/audio/tts/text-to-audio";
    }
}