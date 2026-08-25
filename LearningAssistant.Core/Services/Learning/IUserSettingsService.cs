using LearningAssistant.Models.User;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 用户设置服务接口
    /// 负责管理用户的学习设置
    /// </summary>
    public interface IUserSettingsService
    {
        /// <summary>
        /// 加载用户设置
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户设置对象</returns>
        Task<Settings> LoadSettingsAsync(string userId);

        /// <summary>
        /// 保存用户设置
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="settings">设置对象</param>
        Task SaveSettingsAsync(string userId, Settings settings);

        /// <summary>
        /// 获取设置文件路径
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>设置文件完整路径</returns>
        string GetSettingsPath(string userId);

        /// <summary>
        /// 设置变更事件
        /// </summary>
        event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
    }

    /// <summary>
    /// 设置变更事件参数
    /// </summary>
    public class SettingsChangedEventArgs : EventArgs
    {
        public string UserId { get; set; } = string.Empty;
        public Settings OldSettings { get; set; } = new Settings();
        public Settings NewSettings { get; set; } = new Settings();
    }
}