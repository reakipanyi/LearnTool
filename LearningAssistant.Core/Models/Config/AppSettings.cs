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
        /// <summary>主页风格：Dashboard（完整卡片版）/ Simple（简洁版）</summary>
        public string HomeStyle { get; set; } = "Dashboard";

        /// <summary>是否启用统一学习入口（PDF/手动内容统一进入个人学习库）</summary>
        public bool EnableUnifiedStudyEntry { get; set; } = true;
    }
}