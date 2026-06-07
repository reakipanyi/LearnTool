namespace LearningAssistant.Models.User
{
    public class Settings
    {
        public bool IsVoiceEnabled { get; set; } = false; // 默认不发音
        public int PronunciationScope { get; set; } = 0; // 0=原文, 1=释义, 2=两者
        public bool IsAIExplanationEnabled { get; set; } = false; // AI 释义开关，默认关闭
        public string LearningMode { get; set; } = "Study"; // 学习模式：Study 或 Quick
        public string SortOrder { get; set; } = "Sequential"; // 排序方式：Sequential 或 Random
        public string Language { get; set; } = "English"; // 语言：Chinese 或 English
        public string SubCategory { get; set; } = ""; // 学习分类
    }
}
