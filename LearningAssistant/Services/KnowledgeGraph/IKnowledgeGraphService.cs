using LearningAssistant.Models.KnowledgeGraph;

namespace LearningAssistant.Services.KnowledgeGraph
{
    public interface IKnowledgeGraphService
    {
        Task<Models.KnowledgeGraph.KnowledgeGraph> GetGraphAsync(string userId);

        Task<Models.KnowledgeGraph.KnowledgeGraph> BuildFromContentAsync(string userId, List<string> contents, string category);

        /// <summary>
        /// 添加节点
        /// </summary>
        Task<KGNode> AddNodeAsync(string userId, string label, string category);

        /// <summary>
        /// 添加关系
        /// </summary>
        Task AddRelationAsync(string userId, string sourceLabel, string targetLabel, KGRelationType relationType);

        /// <summary>
        /// 更新节点掌握程度
        /// </summary>
        Task UpdateMasteryAsync(string userId, string nodeId, double masteryLevel);

        /// <summary>
        /// 删除节点
        /// </summary>
        Task DeleteNodeAsync(string userId, string nodeId);

        /// <summary>
        /// 获取薄弱节点
        /// </summary>
        Task<List<KGNode>> GetWeakNodesAsync(string userId, int count = 10);

        /// <summary>
        /// 获取推荐学习路径
        /// </summary>
        Task<List<KGNode>> GetLearningPathAsync(string userId, string targetNodeId);
    }
}
