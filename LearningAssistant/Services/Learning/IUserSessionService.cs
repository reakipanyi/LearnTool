using LearningAssistant.Models.User;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 用户会话服务接口 - 管理用户会话、配置和学习数据
    /// </summary>
    public interface IUserSessionService
    {
        /// <summary>
        /// 当前登录用户的ID
        /// </summary>
        string CurrentUserId { get; }

        /// <summary>
        /// 加载用户会话
        /// </summary>
        /// <returns>用户ID，若无会话则返回空字符串</returns>
        string LoadSession();

        /// <summary>
        /// 保存用户会话
        /// </summary>
        /// <param name="userId">用户ID</param>
        void SaveSession(string userId);

        /// <summary>
        /// 获取用户列表
        /// </summary>
        /// <returns>所有已注册用户ID列表</returns>
        List<string> GetUserList();

        /// <summary>
        /// 加载用户Profile数据
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户Profile对象</returns>
        UserProfile LoadUserProfile(string userId);

        /// <summary>
        /// 保存学习配置
        /// </summary>
        /// <param name="language">学习语言</param>
        /// <param name="subCategory">子类别</param>
        /// <param name="mode">学习模式</param>
        /// <param name="wordBankFile">词库文件</param>
        /// <param name="sortOrder">排序方式</param>
        void SaveLearningConfig(string language, string subCategory, string mode, string wordBankFile, string sortOrder);

        /// <summary>
        /// 加载学习配置
        /// </summary>
        /// <returns>元组(语言, 子类别, 模式, 词库文件, 排序方式)</returns>
        (string Language, string SubCategory, string Mode, string WordBankFile, string SortOrder) LoadLearningConfig();
    }
}
