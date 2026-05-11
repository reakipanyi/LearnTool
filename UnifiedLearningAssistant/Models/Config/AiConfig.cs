namespace UnifiedLearningAssistant.Models.Config
{
    public class AiConfig
    {
        public string Provider { get; set; } = "SiliconFlow";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "Qwen/Qwen2-7B-Instruct";
        public string BaseUrl { get; set; } = "https://api.siliconflow.cn/v1/chat/completions";
        public int TimeoutSeconds { get; set; } = 30;
    }
}