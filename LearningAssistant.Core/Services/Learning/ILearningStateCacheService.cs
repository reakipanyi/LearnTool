using LearningAssistant.Common;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习状态缓存服务接口
    /// 负责管理学习项的已知/未知状态缓存
    /// </summary>
    public interface ILearningStateCacheService
    {
        /// <summary>
        /// 加载学习状态缓存
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="subCategory">子类别</param>
        /// <returns>包含已知和未知项的元组</returns>
        Task<(HashSet<string> KnownItems, HashSet<string> UnknownItems)> LoadCacheAsync(string userId, SubCategoryType subCategory);

        /// <summary>
        /// 检查项是否为已知
        /// </summary>
        /// <param name="itemText">项内容</param>
        /// <returns>是否已知</returns>
        bool IsItemKnown(string itemText);

        /// <summary>
        /// 检查项是否为未知
        /// </summary>
        /// <param name="itemText">项内容</param>
        /// <returns>是否未知</returns>
        bool IsItemUnknown(string itemText);

        /// <summary>
        /// 立即更新缓存中的项状态
        /// </summary>
        /// <param name="itemText">项内容</param>
        /// <param name="isKnown">是否已知</param>
        void UpdateItemStateImmediately(string itemText, bool isKnown);

        /// <summary>
        /// 使缓存失效
        /// </summary>
        void InvalidateCache();

        /// <summary>
        /// 获取当前缓存的已知项
        /// </summary>
        HashSet<string>? GetCachedKnownItems();

        /// <summary>
        /// 获取当前缓存的未知项
        /// </summary>
        HashSet<string>? GetCachedUnknownItems();
    }
}