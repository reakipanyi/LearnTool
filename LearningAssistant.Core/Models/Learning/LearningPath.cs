namespace LearningAssistant.Models.Learning
{
    /// <summary>
    /// 学习路径项
    /// 表示学习路径中的一个学习节点
    /// </summary>
    public class LearningPathItem
    {
        /// <summary>
        /// 节点ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 节点标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 节点描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 学习内容类型（汉字、单词、诗词、语法等）
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// 关联的内容ID列表
        /// </summary>
        public List<string> ContentIds { get; set; } = new List<string>();

        /// <summary>
        /// 预计学习时长（分钟）
        /// </summary>
        public int EstimatedMinutes { get; set; }

        /// <summary>
        /// 难度等级（1-5）
        /// </summary>
        public int DifficultyLevel { get; set; } = 1;

        /// <summary>
        /// 前置依赖节点ID列表
        /// </summary>
        public List<string> Prerequisites { get; set; } = new List<string>();

        /// <summary>
        /// 顺序号（决定学习顺序）
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 是否完成
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// 学习进度（0-100）
        /// </summary>
        public int Progress { get; set; }
    }

    /// <summary>
    /// 学习路径
    /// 包含一系列有序的学习节点
    /// </summary>
    public class LearningPath
    {
        /// <summary>
        /// 路径ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 路径名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 路径描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 学习目标
        /// </summary>
        public string Goal { get; set; } = string.Empty;

        /// <summary>
        /// 路径类型（系统推荐、自定义、考试备考等）
        /// </summary>
        public string PathType { get; set; } = "custom";

        /// <summary>
        /// 学习领域（语文、英语、综合知识等）
        /// </summary>
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// 难度等级（入门、初级、中级、高级、专家）
        /// </summary>
        public string Level { get; set; } = "初级";

        /// <summary>
        /// 学习节点列表
        /// </summary>
        public List<LearningPathItem> Items { get; set; } = new List<LearningPathItem>();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 预计总时长（分钟）
        /// </summary>
        public int TotalEstimatedMinutes { get; set; }

        /// <summary>
        /// 是否已激活
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 目标完成日期
        /// </summary>
        public DateTime? TargetDate { get; set; }

        /// <summary>
        /// 总体进度（0-100）
        /// </summary>
        public int OverallProgress
        {
            get
            {
                if (Items == null || Items.Count == 0)
                    return 0;
                return (int)Math.Round(Items.Average(i => i.Progress));
            }
        }

        /// <summary>
        /// 已完成节点数
        /// </summary>
        public int CompletedCount => Items?.Count(i => i.IsCompleted) ?? 0;

        /// <summary>
        /// 总节点数
        /// </summary>
        public int TotalCount => Items?.Count ?? 0;
    }

    /// <summary>
    /// 学习推荐
    /// 基于用户学习情况的智能推荐
    /// </summary>
    public class LearningRecommendation
    {
        /// <summary>
        /// 推荐ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 推荐类型（复习、新内容、薄弱点、拓展等）
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 推荐标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 推荐理由
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// 推荐内容类型
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// 关联的内容ID
        /// </summary>
        public string ContentId { get; set; } = string.Empty;

        /// <summary>
        /// 推荐优先级（1-10，越高越优先）
        /// </summary>
        public int Priority { get; set; } = 5;

        /// <summary>
        /// 预计学习时长（分钟）
        /// </summary>
        public int EstimatedMinutes { get; set; }

        /// <summary>
        /// 生成时间
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
    }
}
