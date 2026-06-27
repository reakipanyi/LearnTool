using LearningAssistant.Models.Config;
using LearningAssistant.Models.User;

namespace LearningAssistant.Services.Persistence
{
    /// <summary>
    /// 数据持久化服务接口 - 提供配置、用户数据、会话数据的存取功能
    /// 支持 JSON 文件格式的读写操作
    /// </summary>
    public interface IDataPersistenceService
    {
        /// <summary>
        /// 初始化存储层（确保数据库/文件目录就绪）
        /// </summary>
        void Initialize();

        /// <summary>
        /// 加载应用配置
        /// </summary>
        /// <returns>应用配置对象</returns>
        AppConfig LoadConfig();

        /// <summary>
        /// 保存应用配置
        /// </summary>
        /// <param name="config">要保存的配置对象</param>
        void SaveConfig(AppConfig config);

        /// <summary>
        /// 加载指定用户的数据
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户 Profile 对象</returns>
        UserProfile LoadUserProfile(string userId);

        /// <summary>
        /// 保存用户 Profile 数据
        /// </summary>
        /// <param name="profile">用户 Profile 对象</param>
        void SaveUserProfile(UserProfile profile);

        /// <summary>
        /// 获取所有已存在的用户ID列表
        /// </summary>
        /// <returns>用户ID列表</returns>
        List<string> GetUserIds();

        /// <summary>
        /// 创建新的用户 Profile
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="userName">用户名</param>
        void CreateUserProfile(string userId, string userName);

        /// <summary>
        /// 保存会话数据
        /// </summary>
        /// <param name="session">会话数据对象</param>
        void SaveSession(SessionData session);

        /// <summary>
        /// 加载最近的会话数据
        /// </summary>
        /// <returns>会话数据对象</returns>
        SessionData LoadSession();

        /// <summary>
        /// 泛型方法：从 JSON 文件加载数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="filePath">文件路径</param>
        /// <returns>反序列化后的对象，文件不存在返回 default</returns>
        T? LoadJsonFile<T>(string filePath);

        /// <summary>
        /// 泛型方法：保存数据到 JSON 文件
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="filePath">文件路径</param>
        /// <param name="data">要保存的对象</param>
        void SaveJsonFile<T>(string filePath, T data);

        /// <summary>
        /// 将内存缓存数据持久化到磁盘
        /// </summary>
        void PersistCache();

        // ========== LearningItemStates 表操作方法 ==========

        /// <summary>
        /// 获取用户指定分类的已知项列表
        /// </summary>
        List<string> GetKnownItems(string userId, string categoryName);

        /// <summary>
        /// 获取用户指定分类的未知项列表
        /// </summary>
        List<string> GetUnknownItems(string userId, string categoryName);

        /// <summary>
        /// 添加或更新学习项状态
        /// </summary>
        void UpsertLearningItemState(string userId, string categoryName, string content, bool isKnown);

        /// <summary>
        /// 批量添加或更新学习项状态
        /// </summary>
        void UpsertLearningItemStates(string userId, string categoryName, IEnumerable<string> contents, bool isKnown);

        /// <summary>
        /// 删除学习项状态
        /// </summary>
        void DeleteLearningItemState(string userId, string categoryName, string content);

        /// <summary>
        /// 同步 CategoryProgress 中的 KnownItems/UnknownItems 到 LearningItemStates 表
        /// </summary>
        void SyncCategoryProgressToLearningItemStates(string userId, string categoryName, List<string> knownItems, List<string> unknownItems);
    }

    /// <summary>
    /// 会话数据类 - 存储用户最后一次使用应用时的状态
    /// 用于实现"继续上次学习"功能
    /// </summary>
    public class SessionData
    {
        /// <summary>
        /// 当前用户ID
        /// </summary>
        public string CurrentUserId { get; set; } = string.Empty;

        /// <summary>
        /// 最后使用的标签页
        /// </summary>
        public string LastTab { get; set; } = string.Empty;

        /// <summary>
        /// 最后打开的PDF文件路径
        /// </summary>
        public string LastPdfPath { get; set; } = string.Empty;

        /// <summary>
        /// 最后阅读的页面索引
        /// </summary>
        public int LastPageIndex { get; set; } = 0;

        /// <summary>
        /// 最后使用的文件夹路径
        /// </summary>
        public string LastFolderPath { get; set; } = string.Empty;

        /// <summary>
        /// 最后访问时间
        /// </summary>
        public DateTime LastAccessTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后学习的语言
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// 最后学习的子类别
        /// </summary>
        public string SubCategory { get; set; } = string.Empty;

        /// <summary>
        /// 最后使用的学习模式
        /// </summary>
        public string Mode { get; set; } = string.Empty;

        /// <summary>
        /// 最后使用的词库文件
        /// </summary>
        public string WordBankFile { get; set; } = string.Empty;

        /// <summary>
        /// 最后使用的排序方式
        /// </summary>
        public string SortOrder { get; set; } = string.Empty;
    }
}
