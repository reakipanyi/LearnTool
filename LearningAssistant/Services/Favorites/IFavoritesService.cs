using LearningAssistant.Models.Favorites;

namespace LearningAssistant.Services.Favorites
{
    /// <summary>
    /// 收藏夹服务接口
    /// 支持文件夹、多级分类、标签、搜索、导入导出等功能
    /// </summary>
    public interface IFavoritesService
    {
        /// <summary>
        /// 获取所有文件夹
        /// </summary>
        List<FavoriteFolder> GetAllFolders();

        /// <summary>
        /// 获取子文件夹
        /// </summary>
        List<FavoriteFolder> GetSubFolders(string? parentId = null);

        /// <summary>
        /// 获取文件夹信息
        /// </summary>
        FavoriteFolder? GetFolder(string folderId);

        /// <summary>
        /// 创建文件夹
        /// </summary>
        FavoriteFolder CreateFolder(string name, string? parentId = null);

        /// <summary>
        /// 更新文件夹
        /// </summary>
        void UpdateFolder(string folderId, string name, string? icon = null);

        /// <summary>
        /// 删除文件夹
        /// </summary>
        /// <param name="folderId">文件夹ID</param>
        /// <param name="deleteItems">是否同时删除其中的收藏项</param>
        bool DeleteFolder(string folderId, bool deleteItems = false);

        /// <summary>
        /// 移动文件夹
        /// </summary>
        void MoveFolder(string folderId, string? targetParentId);

        /// <summary>
        /// 重命名文件夹
        /// </summary>
        void RenameFolder(string folderId, string newName);

        /// <summary>
        /// 添加收藏项
        /// </summary>
        FavoriteItem AddItem(FavoriteItemType type, string title, string? content = null,
            string? description = null, string? folderId = null, List<string>? tags = null);

        /// <summary>
        /// 添加收藏项（完整对象）
        /// </summary>
        FavoriteItem AddItem(FavoriteItem item);

        /// <summary>
        /// 获取收藏项
        /// </summary>
        FavoriteItem? GetItem(string itemId);

        /// <summary>
        /// 更新收藏项
        /// </summary>
        void UpdateItem(string itemId, Action<FavoriteItem> updateAction);

        /// <summary>
        /// 删除收藏项
        /// </summary>
        bool DeleteItem(string itemId);

        /// <summary>
        /// 移动收藏项到文件夹
        /// </summary>
        void MoveItem(string itemId, string targetFolderId);

        /// <summary>
        /// 批量移动收藏项
        /// </summary>
        void MoveItems(List<string> itemIds, string targetFolderId);

        /// <summary>
        /// 批量删除收藏项
        /// </summary>
        void DeleteItems(List<string> itemIds);

        /// <summary>
        /// 搜索收藏项
        /// </summary>
        PagedResult<FavoriteItem> SearchItems(FavoriteSearchParams searchParams);

        /// <summary>
        /// 获取文件夹中的收藏项
        /// </summary>
        PagedResult<FavoriteItem> GetItemsByFolder(string folderId, int page = 1, int pageSize = 50,
            FavoriteSortOrder sortOrder = FavoriteSortOrder.CreatedDesc);

        /// <summary>
        /// 获取所有标签
        /// </summary>
        List<string> GetAllTags();

        /// <summary>
        /// 获取带数量的标签列表
        /// </summary>
        Dictionary<string, int> GetTagsWithCount();

        /// <summary>
        /// 给收藏项添加标签
        /// </summary>
        void AddTag(string itemId, string tag);

        /// <summary>
        /// 批量给收藏项添加标签
        /// </summary>
        void AddTags(List<string> itemIds, List<string> tags);

        /// <summary>
        /// 移除收藏项的标签
        /// </summary>
        void RemoveTag(string itemId, string tag);

        /// <summary>
        /// 设置置顶
        /// </summary>
        void SetPinned(string itemId, bool isPinned);

        /// <summary>
        /// 标记为复习 / 取消复习
        /// </summary>
        void SetMarkedForReview(string itemId, bool isMarked);

        /// <summary>
        /// 批量标记为复习
        /// </summary>
        void SetMarkedForReviewBatch(List<string> itemIds, bool isMarked);

        /// <summary>
        /// 获取待复习的收藏项
        /// </summary>
        List<FavoriteItem> GetItemsForReview(int count = 20);

        /// <summary>
        /// 记录复习
        /// </summary>
        void RecordReview(string itemId, bool remembered);

        /// <summary>
        /// 记录访问
        /// </summary>
        void RecordVisit(string itemId);

        /// <summary>
        /// 检查是否已收藏
        /// </summary>
        bool IsFavorited(FavoriteItemType type, string content);

        /// <summary>
        /// 切换收藏状态
        /// </summary>
        bool ToggleFavorite(FavoriteItemType type, string title, string content, string? folderId = null);

        /// <summary>
        /// 导出收藏夹
        /// </summary>
        /// <param name="filePath">导出文件路径</param>
        /// <param name="folderIds">要导出的文件夹ID，null则导出全部</param>
        /// <returns>是否成功</returns>
        bool ExportFavorites(string filePath, List<string>? folderIds = null);

        /// <summary>
        /// 导出为 Markdown 格式
        /// </summary>
        bool ExportToMarkdown(string filePath, List<string>? folderIds = null);

        /// <summary>
        /// 导出为文本卡片格式
        /// </summary>
        bool ExportToTextCards(string filePath, List<string>? folderIds = null);

        /// <summary>
        /// 导入收藏夹
        /// </summary>
        /// <param name="filePath">导入文件路径</param>
        /// <param name="targetFolderId">目标文件夹ID，null则导入到根目录</param>
        /// <param name="mode">导入模式（overwrite/merge）</param>
        /// <returns>导入数量</returns>
        int ImportFavorites(string filePath, string? targetFolderId = null, string mode = "merge");

        /// <summary>
        /// 获取收藏统计
        /// </summary>
        (int TotalItems, int TotalFolders, int TotalTags) GetStatistics();

        /// <summary>
        /// 获取收藏统计（别名）
        /// </summary>
        (int TotalItems, int TotalFolders, int TotalTags) GetStats();

        /// <summary>
        /// 获取收藏统计（按用户）
        /// </summary>
        (int TotalItems, int TotalFolders, int TotalTags) GetStats(string userId);

        /// <summary>
        /// 获取所有收藏项
        /// </summary>
        List<FavoriteItem> GetItems();

        /// <summary>
        /// 获取所有收藏项（按用户）
        /// </summary>
        List<FavoriteItem> GetItems(string userId);

        /// <summary>
        /// 获取所有文件夹（别名）
        /// </summary>
        List<FavoriteFolder> GetFolders();

        /// <summary>
        /// 获取所有文件夹（按用户）
        /// </summary>
        List<FavoriteFolder> GetFolders(string userId);

        /// <summary>
        /// 添加文件夹（别名）
        /// </summary>
        FavoriteFolder AddFolder(string name, string? parentId = null);

        /// <summary>
        /// 获取最近访问的收藏项
        /// </summary>
        List<FavoriteItem> GetRecentItems(int count = 10);

        /// <summary>
        /// 保存更改
        /// </summary>
        void SaveChanges();
    }
}
