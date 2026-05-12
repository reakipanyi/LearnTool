namespace UnifiedLearningAssistant.Models.Config
{
    public class TranslationConfig
    {
        public string BaiduAppId { get; set; } = string.Empty;
        public string BaiduSecret { get; set; } = string.Empty;
        public string DefaultFrom { get; set; } = "auto";
        public string DefaultTo { get; set; } = "zh";
    }
}