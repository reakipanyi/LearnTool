namespace LearningAssistant.Models.AI
{
    /// <summary>
    /// 导师对话会话
    /// </summary>
    public class MentorSession
    {
        /// <summary>
        /// 会话ID
        /// </summary>
        public Guid SessionId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 当前导师角色
        /// </summary>
        public MentorPersona Persona { get; set; } = new();

        /// <summary>
        /// 对话历史
        /// </summary>
        public List<ConversationTurn> History { get; set; } = new();

        /// <summary>
        /// 学习上下文（如当前学习的内容）
        /// </summary>
        public string LearningContext { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后活跃时间
        /// </summary>
        public DateTime LastActiveAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 对话总轮次
        /// </summary>
        public int TurnCount => History.Count;

        /// <summary>
        /// 是否为空会话
        /// </summary>
        public bool IsEmpty => History.Count == 0;

        /// <summary>
        /// 主题（从首条对话提取）
        /// </summary>
        public string Topic
        {
            get
            {
                if (History.Count == 0) return "新对话";
                var first = History[0];
                if (first.UserMessage.Length <= 20)
                    return first.UserMessage;
                return first.UserMessage.Substring(0, 20) + "...";
            }
        }

        /// <summary>
        /// 获取最近N轮对话
        /// </summary>
        public List<ConversationTurn> GetRecentTurns(int count)
        {
            return History.OrderByDescending(t => t.Timestamp)
                         .Take(count)
                         .Reverse()
                         .ToList();
        }

        /// <summary>
        /// 构建上下文字符串（用于发送给AI）
        /// </summary>
        public string BuildContextString(int maxTurns = 10)
        {
            var recentTurns = GetRecentTurns(maxTurns);
            var context = new List<string>();

            if (!string.IsNullOrEmpty(LearningContext))
            {
                context.Add($"当前学习内容：{LearningContext}");
            }

            context.Add("对话历史：");
            foreach (var turn in recentTurns)
            {
                context.Add($"用户：{turn.UserMessage}");
                context.Add($"导师：{turn.AiResponse}");
            }

            return string.Join("\n", context);
        }

        /// <summary>
        /// 添加对话轮次
        /// </summary>
        public void AddTurn(ConversationTurn turn)
        {
            History.Add(turn);
            LastActiveAt = DateTime.Now;
        }

        /// <summary>
        /// 清空历史
        /// </summary>
        public void Clear()
        {
            History.Clear();
            LastActiveAt = DateTime.Now;
        }
    }
}
