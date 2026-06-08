namespace LearningAssistant.Models.Config
{
    public class AppConfig
    {
        public AiConfig AiConfig { get; set; } = new AiConfig();
        public TtsConfig TtsConfig { get; set; } = new TtsConfig();
        public TranslationConfig TranslationConfig { get; set; } = new TranslationConfig();
        public OcrConfig OcrConfig { get; set; } = new OcrConfig();
        public AppSettings AppSettings { get; set; } = new AppSettings();
        public VlcConfig VlcConfig { get; set; } = new VlcConfig();
        public CloudStorageConfig CloudStorageConfig { get; set; } = new CloudStorageConfig();
    }
}