namespace LearningAssistant.Models.Learning
{
    /// <summary>
    /// 笔记项
    /// 支持富文本笔记，可关联学习内容
    /// </summary>
    public class NoteItem
    {
        /// <summary>
        /// 唯一标识符
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 笔记标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 笔记内容（支持纯文本或Markdown）
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 笔记分类
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 标签，多个用逗号分隔
        /// </summary>
        public string Tags { get; set; } = string.Empty;

        /// <summary>
        /// 关联的学习内容类型（汉字、单词、诗词等）
        /// </summary>
        public string RelatedType { get; set; } = string.Empty;

        /// <summary>
        /// 关联的学习内容ID
        /// </summary>
        public string RelatedItemId { get; set; } = string.Empty;

        /// <summary>
        /// 关联的学习内容标题/摘要
        /// </summary>
        public string RelatedItemTitle { get; set; } = string.Empty;

        /// <summary>
        /// 重要程度（1-5星）
        /// </summary>
        public int Importance { get; set; } = 3;

        /// <summary>
        /// 是否收藏
        /// </summary>
        public bool IsFavorite { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后复习时间
        /// </summary>
        public DateTime? LastReviewedAt { get; set; }

        /// <summary>
        /// 复习次数
        /// </summary>
        public int ReviewCount { get; set; }

        /// <summary>
        /// 颜色标记（用于视觉分类）
        /// </summary>
        public string Color { get; set; } = string.Empty;

        /// <summary>
        /// 笔记来源（手动创建、学习过程中添加等）
        /// </summary>
        public string Source { get; set; } = string.Empty;
    }
}
