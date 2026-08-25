using LearningAssistant.Models.Learning;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 笔记服务接口
    /// 提供笔记的增删改查、分类管理、复习提醒等功能
    /// </summary>
    public interface INoteService
    {
        /// <summary>
        /// 添加笔记
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="note">笔记项</param>
        void AddNote(string userId, NoteItem note);

        /// <summary>
        /// 更新笔记
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="note">笔记项</param>
        void UpdateNote(string userId, NoteItem note);

        /// <summary>
        /// 删除笔记
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="noteId">笔记ID</param>
        void DeleteNote(string userId, string noteId);

        /// <summary>
        /// 获取指定笔记
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="noteId">笔记ID</param>
        /// <returns>笔记项</returns>
        NoteItem? GetNote(string userId, string noteId);

        /// <summary>
        /// 获取所有笔记
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="category">分类筛选</param>
        /// <param name="tag">标签筛选</param>
        /// <returns>笔记列表</returns>
        List<NoteItem> GetNotes(string userId, string category = "", string tag = "");

        /// <summary>
        /// 搜索笔记
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="keyword">关键词</param>
        /// <returns>匹配的笔记列表</returns>
        List<NoteItem> SearchNotes(string userId, string keyword);

        /// <summary>
        /// 获取关联的笔记
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="relatedType">关联类型</param>
        /// <param name="relatedItemId">关联内容ID</param>
        /// <returns>关联的笔记列表</returns>
        List<NoteItem> GetRelatedNotes(string userId, string relatedType, string relatedItemId);

        /// <summary>
        /// 收藏/取消收藏笔记
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="noteId">笔记ID</param>
        /// <param name="isFavorite">是否收藏</param>
        void SetFavorite(string userId, string noteId, bool isFavorite);

        /// <summary>
        /// 获取收藏的笔记
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>收藏的笔记列表</returns>
        List<NoteItem> GetFavoriteNotes(string userId);

        /// <summary>
        /// 获取所有分类
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>分类列表</returns>
        List<string> GetAllCategories(string userId);

        /// <summary>
        /// 获取所有标签
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>标签列表</returns>
        List<string> GetAllTags(string userId);

        /// <summary>
        /// 标记为已复习
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="noteId">笔记ID</param>
        void MarkAsReviewed(string userId, string noteId);

        /// <summary>
        /// 获取需要复习的笔记
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="days">多少天内未复习的</param>
        /// <returns>需要复习的笔记列表</returns>
        List<NoteItem> GetNotesForReview(string userId, int days = 7);

        /// <summary>
        /// 获取笔记总数
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>笔记总数</returns>
        int GetNoteCount(string userId);

        /// <summary>
        /// 导出笔记
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="filePath">导出文件路径</param>
        /// <param name="format">导出格式（txt、md等）</param>
        void ExportNotes(string userId, string filePath, string format = "txt");

        /// <summary>
        /// 分页获取笔记
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="page">页码（从1开始）</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="category">分类筛选</param>
        /// <param name="tag">标签筛选</param>
        /// <returns>分页后的笔记列表</returns>
        (List<NoteItem> items, int total) GetNotesPaged(string userId, int page, int pageSize, string category = "", string tag = "");

        /// <summary>
        /// 批量删除笔记
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="noteIds">要删除的笔记ID列表</param>
        void BatchDelete(string userId, List<string> noteIds);

        /// <summary>
        /// 批量移动笔记到指定分类
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="noteIds">要移动的笔记ID列表</param>
        /// <param name="targetCategory">目标分类</param>
        void BatchMove(string userId, List<string> noteIds, string targetCategory);
    }
}
