namespace LearningAssistant.Models.Config
{
    public class AppSettings
    {
        public int DefaultFontSize { get; set; } = 12;
        public string Theme { get; set; } = "Light";
        public int AutoSaveIntervalSeconds { get; set; } = 30;
        public int MaxCacheSize { get; set; } = 1000;
        public int CacheExpirationDays { get; set; } = 7;
        public bool AutoPlayPronunciation { get; set; } = true;
        public bool ShowAiPanel { get; set; } = true;
        public bool IsVoiceEnabled { get; set; } = false;
        public int PronunciationScope { get; set; } = 1;
        public bool IsAIExplanationEnabled { get; set; } = false;
    }
}