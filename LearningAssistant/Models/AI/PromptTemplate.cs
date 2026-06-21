namespace LearningAssistant.Models.AI
{
    /// <summary>
    /// 提示词模板
    /// </summary>
    public class PromptTemplate
    {
        /// <summary>
        /// 模板ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 模板名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 模板描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 模板分类
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 系统提示词（System Prompt）
        /// </summary>
        public string SystemPrompt { get; set; } = string.Empty;

        /// <summary>
        /// 用户提示词模板（User Prompt），支持变量替换
        /// 变量格式: {{变量名}}
        /// </summary>
        public string UserPromptTemplate { get; set; } = string.Empty;

        /// <summary>
        /// 变量定义列表
        /// </summary>
        public List<PromptVariable> Variables { get; set; } = new();

        /// <summary>
        /// 模板图标
        /// </summary>
        public string Icon { get; set; } = "💡";

        /// <summary>
        /// 颜色标识
        /// </summary>
        public string Color { get; set; } = "#2196F3";

        /// <summary>
        /// 使用次数
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// 是否为内置模板（不可删除）
        /// </summary>
        public bool IsBuiltIn { get; set; }

        /// <summary>
        /// 是否收藏
        /// </summary>
        public bool IsFavorite { get; set; }

        /// <summary>
        /// 排序号
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后使用时间
        /// </summary>
        public DateTime? LastUsedAt { get; set; }
    }

    /// <summary>
    /// 提示词变量定义
    /// </summary>
    public class PromptVariable
    {
        /// <summary>
        /// 变量名
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 变量描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 默认值
        /// </summary>
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>
        /// 变量类型（text、number、select、textarea）
        /// </summary>
        public string Type { get; set; } = "text";

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 选项列表（当Type为select时使用）
        /// </summary>
        public List<string> Options { get; set; } = new();
    }

    /// <summary>
    /// 提示词模板分类
    /// </summary>
    public class PromptTemplateCategory
    {
        /// <summary>
        /// 分类名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 分类图标
        /// </summary>
        public string Icon { get; set; } = "📁";

        /// <summary>
        /// 排序号
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 模板列表
        /// </summary>
        public List<PromptTemplate> Templates { get; set; } = new();
    }
}
