namespace UnifiedLearningAssistant.Models.Config
{
    public class AiConfig
    {
        public string Provider { get; set; }
        public string ApiKey { get; set; }
        public string Model { get; set; }
        public string BaseUrl { get; set; }
        public int TimeoutSeconds { get; set; } = 30;
    }
}