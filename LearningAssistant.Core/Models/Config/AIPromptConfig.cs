using System.Collections.Generic;

namespace LearningAssistant.Models.Config
{
    /// <summary>
    /// AI提示词配置模型
    /// </summary>
    public class AIPromptConfig
    {
        public PromptCategories Prompts { get; set; } = new();
        public Dictionary<string, string> SystemPrompts { get; set; } = new();
    }

    /// <summary>
    /// 提示词类别
    /// </summary>
    public class PromptCategories
    {
        public LanguageExplanationPrompts Explanation { get; set; } = new();
        public Dictionary<string, string> QuickActions { get; set; } = new();
        public LanguagePrompts Exercise { get; set; } = new();
        public string Summarize { get; set; } = "";
        public string Qa { get; set; } = "";
    }

    /// <summary>
    /// 语言解释提示词
    /// </summary>
    public class LanguageExplanationPrompts
    {
        public Dictionary<string, string> Chinese { get; set; } = new();
        public Dictionary<string, string> English { get; set; } = new();
    }

    /// <summary>
    /// 语言练习提示词
    /// </summary>
    public class LanguagePrompts
    {
        public string Chinese { get; set; } = "";
        public string English { get; set; } = "";
    }
}
