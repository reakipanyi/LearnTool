namespace LearningAssistant.Models.AI
{
    /// <summary>
    /// 对话轮次 - 记录单轮对话
    /// </summary>
    public class ConversationTurn
    {
        /// <summary>
        /// 轮次ID
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 用户消息
        /// </summary>
        public string UserMessage { get; set; } = string.Empty;

        /// <summary>
        /// AI回复
        /// </summary>
        public string AiResponse { get; set; } = string.Empty;

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 置信度（0-1）
        /// </summary>
        public double Confidence { get; set; } = 1.0;

        /// <summary>
        /// 是否包含追问
        /// </summary>
        public bool HasFollowUp { get; set; }

        /// <summary>
        /// 对话类型
        /// </summary>
        public ConversationType Type { get; set; } = ConversationType.QA;
    }

    /// <summary>
    /// 对话类型
    /// </summary>
    public enum ConversationType
    {
        /// <summary>
        /// 问答
        /// </summary>
        QA,

        /// <summary>
        /// 苏格拉底引导
        /// </summary>
        SocraticGuiding,

        /// <summary>
        /// 费曼检验
        /// </summary>
        FeynmanCheck,

        /// <summary>
        /// 诊断分析
        /// </summary>
        Diagnosis,

        /// <summary>
        /// 激励鼓励
        /// </summary>
        Motivation,

        /// <summary>
        /// 学习规划
        /// </summary>
        Planning,

        /// <summary>
        /// 知识关联
        /// </summary>
        KnowledgeLink,

        /// <summary>
        /// 错题分析
        /// </summary>
        ErrorAnalysis
    }
}
