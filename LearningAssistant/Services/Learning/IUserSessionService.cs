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
        /// <param name="config">学习配置</param>
        void SaveLearningConfig(LearningConfig config);

        /// <summary>
        /// 加载学习配置
        /// </summary>
        /// <returns>学习配置对象</returns>
        LearningConfig LoadLearningConfig();
    }

    /// <summary>
    /// 学习配置 - 包含语言、子类别、模式等学习设置
    /// </summary>
    public class LearningConfig
    {
        /// <summary>
        /// 学习语言
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// 子类别（如 CET4, CET6）
        /// </summary>
        public string SubCategory { get; set; } = string.Empty;

        /// <summary>
        /// 学习模式（Study/Test）
        /// </summary>
        public string Mode { get; set; } = string.Empty;

        /// <summary>
        /// 词库文件路径
        /// </summary>
        public string WordBankFile { get; set; } = string.Empty;

        /// <summary>
        /// 排序方式（Sequential/Random）
        /// </summary>
        public string SortOrder { get; set; } = string.Empty;
    }
}
