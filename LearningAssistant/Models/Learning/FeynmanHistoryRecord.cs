namespace LearningAssistant.Models.Learning
{
    /// <summary>
    /// 费曼学习法历史记录
    /// </summary>
    public class FeynmanHistoryRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        /// <summary>
        /// 学习内容唯一标识（如单词/成语内容）
        /// </summary>
        public string ContentId { get; set; } = string.Empty;
        /// <summary>
        /// 知识点显示文本
        /// </summary>
        public string ContentTitle { get; set; } = string.Empty;
        /// <summary>
        /// 用户教学解释
        /// </summary>
        public string TeachAnswer { get; set; } = string.Empty;
        /// <summary>
        /// AI反馈内容
        /// </summary>
        public string? AIFeedback { get; set; }
        /// <summary>
        /// 简化总结
        /// </summary>
        public string? SimplifiedText { get; set; }
        /// <summary>
        /// 类比/比喻
        /// </summary>
        public string? AnalogyText { get; set; }
        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.Now;
        /// <summary>
        /// 是否完成全部四步
        /// </summary>
        public bool IsCompleted { get; set; }
    }

    /// <summary>
    /// 费曼学习历史记录存储容器
    /// </summary>
    public class FeynmanHistoryStore
    {
        public List<FeynmanHistoryRecord> Records { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
