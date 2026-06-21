using LearningAssistant.Models.Learning;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习路径推荐服务接口
    /// 提供智能学习路径规划、个性化推荐等功能
    /// </summary>
    public interface ILearningPathService
    {
        /// <summary>
        /// 创建学习路径
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="path">学习路径</param>
        void CreatePath(string userId, LearningPath path);

        /// <summary>
        /// 更新学习路径
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="path">学习路径</param>
        void UpdatePath(string userId, LearningPath path);

        /// <summary>
        /// 删除学习路径
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="pathId">路径ID</param>
        void DeletePath(string userId, string pathId);

        /// <summary>
        /// 获取学习路径
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="pathId">路径ID</param>
        /// <returns>学习路径</returns>
        LearningPath? GetPath(string userId, string pathId);

        /// <summary>
        /// 获取用户所有学习路径
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>学习路径列表</returns>
        List<LearningPath> GetAllPaths(string userId);

        /// <summary>
        /// 获取激活的学习路径
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>激活的学习路径</returns>
        LearningPath? GetActivePath(string userId);

        /// <summary>
        /// 激活学习路径
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="pathId">路径ID</param>
        void ActivatePath(string userId, string pathId);

        /// <summary>
        /// 更新学习路径项进度
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="pathId">路径ID</param>
        /// <param name="itemId">路径项ID</param>
        /// <param name="progress">进度（0-100）</param>
        void UpdateItemProgress(string userId, string pathId, string itemId, int progress);

        /// <summary>
        /// 标记路径项为完成
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="pathId">路径ID</param>
        /// <param name="itemId">路径项ID</param>
        void MarkItemCompleted(string userId, string pathId, string itemId);

        /// <summary>
        /// 获取今日推荐学习内容
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="count">推荐数量</param>
        /// <returns>推荐列表</returns>
        List<LearningRecommendation> GetTodayRecommendations(string userId, int count = 5);

        /// <summary>
        /// 基于用户水平生成推荐学习路径
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="domain">学习领域</param>
        /// <param name="targetLevel">目标水平</param>
        /// <param name="days">预计天数</param>
        /// <returns>生成的学习路径</returns>
        LearningPath GenerateRecommendedPath(string userId, string domain, string targetLevel, int days = 30);

        /// <summary>
        /// 获取下一阶段学习建议
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>建议内容</returns>
        string GetNextStageSuggestion(string userId);

        /// <summary>
        /// 获取学习薄弱点分析
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>薄弱点列表</returns>
        List<LearningRecommendation> GetWeakPoints(string userId);

        /// <summary>
        /// 获取下一个待学习的路径项
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>下一个学习项</returns>
        LearningPathItem? GetNextItem(string userId);
    }
}
