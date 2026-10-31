using LearningAssistant.Models.Home;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 首页快照聚合服务。
    /// 将统计底座、游戏化、笔记、间隔重复、学习目标等分散数据源收敛为一次快照。
    /// </summary>
    public interface IHomeSnapshotService
    {
        /// <summary>
        /// 获取指定用户的首页快照。
        /// </summary>
        /// <param name="userId">用户ID</param>
        HomeSnapshot GetSnapshot(string userId);
    }
}
