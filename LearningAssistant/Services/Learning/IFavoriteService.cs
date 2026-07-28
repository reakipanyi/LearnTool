using LearningAssistant.Common;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 收藏服务接口
    /// 负责管理用户的学习项收藏功能
    /// </summary>
    public interface IFavoriteService
    {
        /// <summary>
        /// 检查指定项是否已被收藏
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="subCategory">子类别</param>
        /// <param name="content">学习项内容</param>
        /// <returns>是否已收藏</returns>
        Task<bool> IsFavoriteAsync(string userId, SubCategoryType subCategory, string content);

        /// <summary>
        /// 添加收藏项
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="subCategory">子类别</param>
        /// <param name="content">学习项内容</param>
        Task AddFavoriteAsync(string userId, SubCategoryType subCategory, string content);

        /// <summary>
        /// 移除收藏项
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="subCategory">子类别</param>
        /// <param name="content">学习项内容</param>
        Task RemoveFavoriteAsync(string userId, SubCategoryType subCategory, string content);

        /// <summary>
        /// 获取用户的所有收藏项
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>收藏项内容集合</returns>
        Task<HashSet<string>> GetUserFavoritesAsync(string userId);

        /// <summary>
        /// 使缓存失效(当用户切换或数据变更时调用)
        /// </summary>
        /// <param name="userId">用户ID</param>
        void InvalidateCache(string userId);
    }
}