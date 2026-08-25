namespace LearningAssistant.Models.User
{
    public class Settings
    {
        public bool IsVoiceEnabled { get; set; } = true; // 默认发音
        public int PronunciationScope { get; set; } = 0; // 0=原文, 1=释义, 2=两者
        public bool IsAIExplanationEnabled { get; set; } = false; // AI 释义开关，默认关闭
        public string LearningMode { get; set; } = "Study"; // 学习模式：Study 或 Quick
        public string SortOrder { get; set; } = "Sequential"; // 排序方式：Sequential 或 Random
        public string Language { get; set; } = "English"; // 语言：Chinese 或 English（兼容旧版）
        public string Subject { get; set; } = "英语"; // 学科：语文、英语、数学、物理、化学、历史、地理、生物
        public string SubCategory { get; set; } = ""; // 学习分类
        public int DailyGoal { get; set; } = 30; // 每日学习目标

        // 游戏设置
        public int GameRows { get; set; } = 5; // 游戏每组展示行数
        public int GameColumns { get; set; } = 8; // 游戏每组展示列数
        public bool SkipKnown { get; set; } = true; // 游戏换组时是否跳过已知项（false=加载所有）
    }
}
