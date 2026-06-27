using LearningAssistant.Models.KnowledgeGraph;
using LearningAssistant.Services.AI;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.KnowledgeGraph
{
    public class KnowledgeGraphService : IKnowledgeGraphService
    {
        private readonly IAiQuestionService _aiService;
        private readonly ILogger<KnowledgeGraphService>? _logger;

        private readonly Dictionary<string, Models.KnowledgeGraph.KnowledgeGraph> _graphs = new();

        public KnowledgeGraphService(
            IAiQuestionService aiService,
            ILogger<KnowledgeGraphService>? logger = null)
        {
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _logger = logger;
        }

        public Task<Models.KnowledgeGraph.KnowledgeGraph> GetGraphAsync(string userId)
        {
            if (!_graphs.TryGetValue(userId, out var graph))
            {
                graph = new Models.KnowledgeGraph.KnowledgeGraph
                {
                    UserId = userId,
                    Name = $"{userId} 的知识图谱"
                };
                _graphs[userId] = graph;
            }

            return Task.FromResult(graph);
        }

        public async Task<Models.KnowledgeGraph.KnowledgeGraph> BuildFromContentAsync(
            string userId, List<string> contents, string category)
        {
            var graph = await GetGraphAsync(userId);

            foreach (var content in contents)
            {
                try
                {
                    // 使用AI分析内容，提取概念和关系
                    var prompt = $"请分析以下内容，提取知识点和它们之间的关系。\n" +
                                 "请以以下格式返回：\n" +
                                 "概念1 | 概念2 | 关系类型\n" +
                                 "关系类型包括：前置(Prerequisite)、相关(Related)、示例(Example)、包含(PartOf)、相似(Similar)、对立(Opposite)\n\n" +
                                 $"内容：{content}\n\n" +
                                 "例如：\n" +
                                 "加法 | 乘法 | 前置\n" +
                                 "减法 | 加法 | 相关";

                    var response = await _aiService.AskAsync(prompt, "", CancellationToken.None);

                    if (string.IsNullOrWhiteSpace(response))
                        continue;

                    // 解析AI返回的内容
                    var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    var nodeMap = new Dictionary<string, KGNode>(StringComparer.OrdinalIgnoreCase);

                    foreach (var line in lines)
                    {
                        var parts = line.Split('|').Select(p => p.Trim()).ToArray();
                        if (parts.Length < 3) continue;

                        var label1 = parts[0];
                        var label2 = parts[1];
                        var relationStr = parts[2];

                        // 获取或创建节点
                        if (!nodeMap.TryGetValue(label1, out var node1))
                        {
                            node1 = graph.AddNode(label1, category);
                            node1.ContentId = content;
                            nodeMap[label1] = node1;
                        }

                        if (!nodeMap.TryGetValue(label2, out var node2))
                        {
                            node2 = graph.AddNode(label2, category);
                            node2.ContentId = content;
                            nodeMap[label2] = node2;
                        }

                        // 解析关系类型
                        var relationType = ParseRelationType(relationStr);
                        if (relationType.HasValue)
                        {
                            graph.AddEdge(node1.Id, node2.Id, relationType.Value);
                        }
                    }

                    _logger?.LogDebug("从内容构建图谱: 内容长度={Length}, 新增节点={Count}",
                        content.Length, nodeMap.Count);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "从内容构建图谱失败: {Content}",
                        content.Length > 50 ? content.Substring(0, 50) + "..." : content);
                }
            }

            graph.LastUpdatedAt = DateTime.Now;
            return graph;
        }

        /// <summary>
        /// 添加节点
        /// </summary>
        public async Task<KGNode> AddNodeAsync(string userId, string label, string category)
        {
            var graph = await GetGraphAsync(userId);

            // 检查是否已存在
            var existing = graph.Nodes.FirstOrDefault(n =>
                n.Label.Equals(label, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
                return existing;

            var node = graph.AddNode(label, category);
            _logger?.LogInformation("添加图谱节点: 用户={UserId}, 标签={Label}", userId, label);

            return node;
        }

        /// <summary>
        /// 添加关系
        /// </summary>
        public async Task AddRelationAsync(
            string userId, string sourceLabel, string targetLabel, KGRelationType relationType)
        {
            var graph = await GetGraphAsync(userId);

            var sourceNode = graph.Nodes.FirstOrDefault(n =>
                n.Label.Equals(sourceLabel, StringComparison.OrdinalIgnoreCase));
            var targetNode = graph.Nodes.FirstOrDefault(n =>
                n.Label.Equals(targetLabel, StringComparison.OrdinalIgnoreCase));

            if (sourceNode == null || targetNode == null)
            {
                _logger?.LogWarning("添加关系失败: 节点不存在");
                return;
            }

            graph.AddEdge(sourceNode.Id, targetNode.Id, relationType);
            _logger?.LogInformation("添加图谱关系: {Source} -> {Target}, 类型={Type}",
                sourceLabel, targetLabel, relationType);
        }

        /// <summary>
        /// 更新节点掌握程度
        /// </summary>
        public Task UpdateMasteryAsync(string userId, string nodeId, double masteryLevel)
        {
            var graph = _graphs.GetValueOrDefault(userId);
            if (graph == null) return Task.CompletedTask;

            var node = graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                node.MasteryLevel = Math.Clamp(masteryLevel, 0, 1);
                node.ReviewCount++;

                // 更新正确率
                if (node.ReviewCount > 0)
                {
                    node.AccuracyRate = node.AccuracyRate * (node.ReviewCount - 1) / node.ReviewCount
                        + masteryLevel / node.ReviewCount;
                }

                graph.LastUpdatedAt = DateTime.Now;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 删除节点
        /// </summary>
        public Task DeleteNodeAsync(string userId, string nodeId)
        {
            var graph = _graphs.GetValueOrDefault(userId);
            if (graph == null) return Task.CompletedTask;

            graph.Nodes.RemoveAll(n => n.Id == nodeId);
            graph.Edges.RemoveAll(e => e.Source == nodeId || e.Target == nodeId);

            _logger?.LogInformation("删除图谱节点: 用户={UserId}, 节点={NodeId}", userId, nodeId);

            return Task.CompletedTask;
        }

        /// <summary>
        /// 获取薄弱节点
        /// </summary>
        public Task<List<KGNode>> GetWeakNodesAsync(string userId, int count = 10)
        {
            var graph = _graphs.GetValueOrDefault(userId);
            if (graph == null)
                return Task.FromResult(new List<KGNode>());

            var weakNodes = graph.Nodes
                .Where(n => n.MasteryLevel < 0.6)
                .OrderBy(n => n.MasteryLevel)
                .Take(count)
                .ToList();

            return Task.FromResult(weakNodes);
        }

        /// <summary>
        /// 获取推荐学习路径（基于前置知识）
        /// </summary>
        public Task<List<KGNode>> GetLearningPathAsync(string userId, string targetNodeId)
        {
            var graph = _graphs.GetValueOrDefault(userId);
            if (graph == null)
                return Task.FromResult(new List<KGNode>());

            var path = new List<KGNode>();
            var visited = new HashSet<string>();

            // 简单的BFS查找前置知识链
            BuildLearningPathRecursive(graph, targetNodeId, path, visited);

            // 反转，使得从基础到目标
            path.Reverse();

            return Task.FromResult(path);
        }

        private void BuildLearningPathRecursive(
            Models.KnowledgeGraph.KnowledgeGraph graph, string nodeId,
            List<KGNode> path, HashSet<string> visited)
        {
            if (visited.Contains(nodeId)) return;

            var node = graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node == null) return;

            visited.Add(nodeId);

            // 查找该节点的前置知识
            var prerequisites = graph.Edges
                .Where(e => e.Target == nodeId && e.RelationType == KGRelationType.Prerequisite)
                .Select(e => graph.Nodes.FirstOrDefault(n => n.Id == e.Source))
                .Where(n => n != null)
                .ToList();

            foreach (var prereq in prerequisites)
            {
                if (prereq != null)
                    BuildLearningPathRecursive(graph, prereq.Id, path, visited);
            }

            path.Add(node);
        }

        /// <summary>
        /// 解析关系类型
        /// </summary>
        private static KGRelationType? ParseRelationType(string text)
        {
            var lower = text.ToLower();

            if (lower.Contains("pre") || lower.Contains("前置"))
                return KGRelationType.Prerequisite;
            if (lower.Contains("related") || lower.Contains("相关"))
                return KGRelationType.Related;
            if (lower.Contains("example") || lower.Contains("示例"))
                return KGRelationType.Example;
            if (lower.Contains("part") || lower.Contains("包含"))
                return KGRelationType.PartOf;
            if (lower.Contains("similar") || lower.Contains("相似"))
                return KGRelationType.Similar;
            if (lower.Contains("opposite") || lower.Contains("对立") || lower.Contains("反"))
                return KGRelationType.Opposite;

            return null;
        }
    }
}
