namespace LearningAssistant.Models.UI
{
    /// <summary>
    /// AI面板快捷操作按钮配置
    /// </summary>
    public class QuickActionConfig
    {
        public string Key { get; set; } = "";
        public string Text { get; set; } = "";
        public string Emoji { get; set; } = "";
        public string PromptTemplate { get; set; } = "";
        public Color LightColor { get; set; }
        public Color DarkColor { get; set; } = Color.FromArgb(60, 60, 60);
        public int Width { get; set; } = 75;

        /// <summary>
        /// 获取完整的按钮显示文本
        /// </summary>
        public string DisplayText => $"{Emoji} {Text}";
    }

    /// <summary>
    /// AI面板快捷操作按钮预定义配置
    /// </summary>
    public static class QuickActionDefinitions
    {
        /// <summary>
        /// 默认快捷操作按钮配置列表
        /// </summary>
        public static readonly QuickActionConfig[] DefaultActions = new[]
        {
            new QuickActionConfig
            {
                Key = "explain",
                Text = "解释",
                Emoji = "📖",
                PromptTemplate = "请解释以下内容：\n{context}",
                LightColor = Color.FromArgb(100, 180, 100),
                Width = 75
            },
            new QuickActionConfig
            {
                Key = "translate",
                Text = "翻译",
                Emoji = "🌐",
                PromptTemplate = "请将以下内容翻译成中文：\n{context}",
                LightColor = Color.FromArgb(70, 150, 200),
                Width = 75
            },
            new QuickActionConfig
            {
                Key = "summarize",
                Text = "总结",
                Emoji = "📝",
                PromptTemplate = "请总结以下内容：\n{context}",
                LightColor = Color.FromArgb(180, 100, 180),
                Width = 75
            },
            new QuickActionConfig
            {
                Key = "exercise",
                Text = "生成练习",
                Emoji = "✏️",
                PromptTemplate = "请根据以下内容生成练习题（包括选择题和填空题）：\n{context}",
                LightColor = Color.FromArgb(200, 120, 100),
                Width = 90
            },
            new QuickActionConfig
            {
                Key = "grammar",
                Text = "语法分析",
                Emoji = "📚",
                PromptTemplate = "请分析以下文本的语法结构：\n{context}",
                LightColor = Color.FromArgb(120, 100, 200),
                Width = 90
            },
            new QuickActionConfig
            {
                Key = "writing",
                Text = "写作建议",
                Emoji = "✍️",
                PromptTemplate = "请对以下写作内容提供改进建议：\n{context}",
                LightColor = Color.FromArgb(100, 150, 180),
                Width = 90
            },
            new QuickActionConfig
            {
                Key = "expand",
                Text = "扩写",
                Emoji = "📈",
                PromptTemplate = "请扩写以下内容，使其更加丰富详细：\n{context}",
                LightColor = Color.FromArgb(180, 180, 100),
                Width = 75
            },
            new QuickActionConfig
            {
                Key = "simplify",
                Text = "简化",
                Emoji = "📉",
                PromptTemplate = "请简化以下内容，使其更加简洁明了：\n{context}",
                LightColor = Color.FromArgb(150, 150, 150),
                Width = 80
            }
        };

        /// <summary>
        /// 提示词模板字典（用于智能提示）
        /// </summary>
        public static readonly Dictionary<string, string> PromptTemplates = new()
        {
            { "解释", "请解释以下内容：" },
            { "翻译", "请将以下内容翻译成中文：" },
            { "总结", "请总结以下内容：" },
            { "练习", "请根据以下内容生成练习题：" },
            { "语法", "请分析以下文本的语法结构：" },
            { "写作", "请对以下写作内容提供改进建议：" },
            { "扩写", "请扩写以下内容：" },
            { "简化", "请简化以下内容：" },
            { "举例", "请举例说明以下概念：" },
            { "对比", "请对比分析以下内容：" },
            { "应用", "请说明以下内容在实际中的应用：" },
            { "原理", "请解释以下内容的原理：" },
            { "步骤", "请列出以下操作的步骤：" },
            { "原因", "请分析以下现象的原因：" },
            { "影响", "请分析以下内容的影响：" },
            { "优缺点", "请分析以下内容的优缺点：" },
            { "定义", "请给出以下概念的定义：" },
            { "关系", "请分析以下内容之间的关系：" },
            { "分类", "请对以下内容进行分类：" },
            { "推导", "请推导以下结论：" }
        };
    }
}
