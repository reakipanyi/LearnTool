using LearningAssistant.Models.Learning;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 错题本服务接口
    /// 提供错题记录、分类、复习、统计等功能
    /// </summary>
    public interface IWrongAnswerService
    {
        /// <summary>
        /// 添加错题
        /// </summary>
        void AddWrongAnswer(string userId, WrongAnswerItem item);

        /// <summary>
        /// 删除错题
        /// </summary>
        void RemoveWrongAnswer(string userId, string itemId);

        /// <summary>
        /// 获取错题列表（兼容旧接口）
        /// </summary>
        List<WrongAnswerItem> GetWrongAnswers(string userId, string subject = "", string category = "");

        /// <summary>
        /// 按学科和分类获取错题
        /// </summary>
        List<WrongAnswerItem> GetBySubjectCategory(string userId, string subject, string category);

        /// <summary>
        /// 获取复习用错题
        /// </summary>
        List<WrongAnswerItem> GetWrongAnswersForReview(string userId, int count = 10);

        /// <summary>
        /// 标记已复习
        /// </summary>
        void MarkAsReviewed(string userId, string itemId, bool remembered);

        /// <summary>
        /// 标记为已掌握（兼容旧接口）
        /// </summary>
        void MarkAsMastered(string userId, string itemId);

        /// <summary>
        /// 获取错题总数（未掌握）
        /// </summary>
        int GetWrongAnswerCount(string userId);

        /// <summary>
        /// 获取已掌握题数（兼容旧接口）
        /// </summary>
        int GetMasteredCount(string userId);

        /// <summary>
        /// 导出错题（兼容旧接口）
        /// </summary>
        void ExportWrongAnswers(string userId, string filePath);

        /// <summary>
        /// 按筛选条件获取错题
        /// </summary>
        List<WrongAnswerItem> GetWrongAnswers(string userId, WrongAnswerFilter filter);

        /// <summary>
        /// 分页获取错题
        /// </summary>
        List<WrongAnswerItem> GetWrongAnswers(string userId, int skip, int take);

        /// <summary>
        /// 更新掌握程度
        /// </summary>
        void UpdateMastery(string userId, string itemId, MasteryLevel mastery);

        /// <summary>
        /// 搜索错题
        /// </summary>
        List<WrongAnswerItem> SearchWrongAnswers(string userId, string keyword);

        /// <summary>
        /// 获取错题统计信息
        /// </summary>
        WrongAnswerStats GetStatistics(string userId);

        /// <summary>
        /// 获取所有学科列表
        /// </summary>
        List<string> GetSubjects(string userId);

        /// <summary>
        /// 获取指定学科下的分类列表
        /// </summary>
        List<string> GetCategories(string userId, string subject);

        /// <summary>
        /// 获取所有标签
        /// </summary>
        Dictionary<string, int> GetAllTags(string userId);

        /// <summary>
        /// 给错题添加标签
        /// </summary>
        void AddTag(string userId, string itemId, string tag);

        /// <summary>
        /// 给错题移除标签
        /// </summary>
        void RemoveTag(string userId, string itemId, string tag);

        /// <summary>
        /// 批量更新掌握程度
        /// </summary>
        void BatchUpdateMastery(string userId, List<string> itemIds, MasteryLevel mastery);

        /// <summary>
        /// 批量删除错题
        /// </summary>
        void BatchRemove(string userId, List<string> itemIds);

        /// <summary>
        /// 导出为 Markdown 格式
        /// </summary>
        bool ExportToMarkdown(string userId, string filePath, WrongAnswerFilter? filter = null);

        /// <summary>
        /// 导出为文本卡片格式
        /// </summary>
        bool ExportToTextCards(string userId, string filePath, WrongAnswerFilter? filter = null);

        /// <summary>
        /// 获取错题分页
        /// </summary>
        (List<WrongAnswerItem> items, int total) GetWrongAnswersPaged(
            string userId, WrongAnswerFilter filter, int page = 1, int pageSize = 20);
    }
}
