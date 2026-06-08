namespace LearningAssistant.Models.Config
{
    public class TtsConfig
    {
        public string Provider { get; set; }
        public string ApiKey { get; set; }
        public string Model { get; set; }
        public string Voice { get; set; }
        public float Speed { get; set; }
        public float Volume { get; set; }
        public string BaseUrl { get; set; }
    }
}