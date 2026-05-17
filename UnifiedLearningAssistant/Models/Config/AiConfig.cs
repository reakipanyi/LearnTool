namespace UnifiedLearningAssistant.Models.Config
{
    public class AiConfig
    {
        public string Provider { get; set; } = "siliconflow";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;

        public static readonly Dictionary<string, AiProviderInfo> Providers = new()
        {
            {
                "siliconflow", new AiProviderInfo
                {
                    Name = "千问 (SiliconFlow)",
                    BaseUrl = "https://api.siliconflow.cn/v1/chat/completions",
                    DefaultModel = "qwen/qwen-2.5-7b-instruct"
                }
            },
            {
                "deepseek", new AiProviderInfo
                {
                    Name = "Deepseek",
                    BaseUrl = "https://api.deepseek.com/v1/chat/completions",
                    DefaultModel = "deepseek-chat"
                }
            },
            {
                "doubao", new AiProviderInfo
                {
                    Name = "豆包 (Doubao)",
                    BaseUrl = "https://ark.cn-beijing.volces.com/api/v3/chat/completions",
                    DefaultModel = "doubao-pro-32k"
                }
            }
        };
    }

    public class AiProviderInfo
    {
        public string Name { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string DefaultModel { get; set; } = string.Empty;
    }
}
