namespace LearningAssistant.Models.KnowledgeGraph
{
    /// <summary>
    /// 知识图谱节点（概念）
    /// </summary>
    public class KGNode
    {
        /// <summary>
        /// 节点ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 节点名称（概念）
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// 分类（如：识字/单词/公式/人物）
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 掌握程度（0-1）
        /// </summary>
        public double MasteryLevel { get; set; }

        /// <summary>
        /// 复习次数
        /// </summary>
        public int ReviewCount { get; set; }

        /// <summary>
        /// 正确率
        /// </summary>
        public double AccuracyRate { get; set; }

        /// <summary>
        /// 重要度（用于节点大小）
        /// </summary>
        public double Importance { get; set; } = 1.0;

        /// <summary>
        /// 相关内容ID
        /// </summary>
        public string ContentId { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 是否是高亮显示
        /// </summary>
        public bool IsHighlighted { get; set; }

        /// <summary>
        /// 获取颜色（基于掌握程度）
        /// </summary>
        public string GetColor()
        {
            // 红色(0-0.3) -> 黄色(0.3-0.7) -> 绿色(0.7-1.0)
            if (MasteryLevel >= 0.7)
                return "#4CAF50"; // 绿色
            if (MasteryLevel >= 0.3)
                return "#FFC107"; // 黄色
            return "#F44336";    // 红色
        }

        /// <summary>
        /// 获取节点大小（基于重要度）
        /// </summary>
        public double GetSize()
        {
            return 10 + Importance * 20;
        }
    }

    /// <summary>
    /// 知识图谱边（关系）
    /// </summary>
    public class KGEdge
    {
        /// <summary>
        /// 源节点ID
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// 目标节点ID
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// 关系类型
        /// </summary>
        public KGRelationType RelationType { get; set; }

        /// <summary>
        /// 关联强度（0-1）
        /// </summary>
        public double Strength { get; set; } = 1.0;

        /// <summary>
        /// 关系标签
        /// </summary>
        public string Label => RelationType switch
        {
            KGRelationType.Prerequisite => "前置",
            KGRelationType.Related => "相关",
            KGRelationType.Example => "示例",
            KGRelationType.PartOf => "包含",
            KGRelationType.Similar => "相似",
            KGRelationType.Opposite => "对立",
            _ => ""
        };

        /// <summary>
        /// 获取颜色（基于关系类型）
        /// </summary>
        public string GetColor()
        {
            return RelationType switch
            {
                KGRelationType.Prerequisite => "#2196F3", // 蓝色 - 前置知识
                KGRelationType.Related => "#9C27B0",      // 紫色 - 相关
                KGRelationType.Example => "#4CAF50",      // 绿色 - 示例
                KGRelationType.PartOf => "#FF9800",       // 橙色 - 包含
                KGRelationType.Similar => "#00BCD4",      // 青色 - 相似
                KGRelationType.Opposite => "#F44336",     // 红色 - 对立
                _ => "#757575"
            };
        }
    }

    /// <summary>
    /// 关系类型
    /// </summary>
    public enum KGRelationType
    {
        /// <summary>
        /// 前置知识
        /// </summary>
        Prerequisite,

        /// <summary>
        /// 相关知识
        /// </summary>
        Related,

        /// <summary>
        /// 示例关系
        /// </summary>
        Example,

        /// <summary>
        /// 包含关系
        /// </summary>
        PartOf,

        /// <summary>
        /// 相似关系
        /// </summary>
        Similar,

        /// <summary>
        /// 对立关系
        /// </summary>
        Opposite
    }

    /// <summary>
    /// 知识图谱
    /// </summary>
    public class KnowledgeGraph
    {
        /// <summary>
        /// 图谱ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 图谱名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 节点列表
        /// </summary>
        public List<KGNode> Nodes { get; set; } = new();

        /// <summary>
        /// 边列表
        /// </summary>
        public List<KGEdge> Edges { get; set; } = new();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 节点数量
        /// </summary>
        public int NodeCount => Nodes.Count;

        /// <summary>
        /// 边数量
        /// </summary>
        public int EdgeCount => Edges.Count;

        /// <summary>
        /// 添加节点
        /// </summary>
        public KGNode AddNode(string label, string category)
        {
            var node = new KGNode
            {
                Id = Guid.NewGuid().ToString(),
                Label = label,
                Category = category
            };
            Nodes.Add(node);
            return node;
        }

        /// <summary>
        /// 添加边
        /// </summary>
        public void AddEdge(string sourceId, string targetId, KGRelationType relationType, double strength = 1.0)
        {
            // 检查是否已存在相同的边
            if (Edges.Any(e => e.Source == sourceId && e.Target == targetId))
                return;

            Edges.Add(new KGEdge
            {
                Source = sourceId,
                Target = targetId,
                RelationType = relationType,
                Strength = strength
            });
        }

        /// <summary>
        /// 获取节点的邻居节点
        /// </summary>
        public List<KGNode> GetNeighborNodes(string nodeId)
        {
            var neighborIds = new HashSet<string>();

            foreach (var edge in Edges)
            {
                if (edge.Source == nodeId)
                    neighborIds.Add(edge.Target);
                else if (edge.Target == nodeId)
                    neighborIds.Add(edge.Source);
            }

            return Nodes.Where(n => neighborIds.Contains(n.Id)).ToList();
        }

        /// <summary>
        /// 获取节点的关联边
        /// </summary>
        public List<KGEdge> GetNodeEdges(string nodeId)
        {
            return Edges.Where(e => e.Source == nodeId || e.Target == nodeId).ToList();
        }

        /// <summary>
        /// 转换为D3.js格式
        /// </summary>
        public KnowledgeGraphDto ToDto()
        {
            var dto = new KnowledgeGraphDto();
            foreach (var n in Nodes)
            {
                dto.Nodes.Add(new
                {
                    id = n.Id,
                    label = n.Label,
                    category = n.Category,
                    color = n.GetColor(),
                    size = n.GetSize(),
                    masteryLevel = n.MasteryLevel,
                    description = n.Description
                });
            }
            foreach (var e in Edges)
            {
                dto.Links.Add(new
                {
                    source = e.Source,
                    target = e.Target,
                    type = e.RelationType.ToString(),
                    label = e.Label,
                    color = e.GetColor(),
                    strength = e.Strength
                });
            }
            return dto;
        }
    }

    /// <summary>
    /// 知识图谱DTO（用于前端渲染）
    /// </summary>
    public class KnowledgeGraphDto
    {
        public List<object> Nodes { get; set; } = new();
        public List<object> Links { get; set; } = new();
    }
}
