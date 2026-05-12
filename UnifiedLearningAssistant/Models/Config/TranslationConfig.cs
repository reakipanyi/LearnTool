namespace UnifiedLearningAssistant.Models.Config
{
    public class TranslationConfig
    {
        public string AppKey { get; set; } = string.Empty;
        public string AppSecret { get; set; } = string.Empty;
        public string DefaultFrom { get; set; } = "auto";
        public string DefaultTo { get; set; } = "zh";
    }
}