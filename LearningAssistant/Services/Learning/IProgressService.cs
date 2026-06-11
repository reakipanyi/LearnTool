namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习进度服务接口 - 提供学习进度查询和统计功能
    /// </summary>
    public interface IProgressService
    {
        /// <summary>
        /// 获取学习进度摘要
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="language">学习语言</param>
        /// <param name="subCategory">学习子类别（如CET4, CET6）</param>
        /// <returns>进度摘要文本（如 "已学习 50/100，已掌握 30"）</returns>
        string GetProgressSummary(string userId, string language, string subCategory);

        /// <summary>
        /// 获取已掌握的项数量
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="subCategory">学习子类别</param>
        /// <returns>已掌握的项数量</returns>
        int GetKnownCount(string userId, string subCategory);

        /// <summary>
        /// 获取未掌握的项数量
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="subCategory">学习子类别</param>
        /// <returns>未掌握的项数量</returns>
        int GetUnknownCount(string userId, string subCategory);

        /// <summary>
        /// 获取学习准确率
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="subCategory">学习子类别</param>
        /// <returns>准确率百分比（0-100）</returns>
        double GetAccuracy(string userId, string subCategory);

        /// <summary>
        /// 获取用户的未掌握项列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>所有未掌握项的内容列表</returns>
        List<string> GetUnknownItems(string userId);
    }
}
