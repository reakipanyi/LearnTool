namespace LearningAssistant.Models.Config
{
    public static class TtsProviders
    {
        public const string Qwen = "qwen";
        public const string KokoroSharp = "kokorosharp";
    }

    public class TtsConfig
    {
        public string Provider { get; set; } = TtsProviders.KokoroSharp;
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Voice { get; set; } = string.Empty;
        public float Speed { get; set; } = 1.0f;
        public float Volume { get; set; } = 1.0f;
        public string BaseUrl { get; set; } = string.Empty;
    }
}