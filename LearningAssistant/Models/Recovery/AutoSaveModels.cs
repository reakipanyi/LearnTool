namespace LearningAssistant.Models.Recovery
{
    /// <summary>
    /// 自动保存配置
    /// </summary>
    public class AutoSaveConfig
    {
        /// <summary>
        /// 是否启用自动保存
        /// </summary>
        public bool AutoSaveEnabled { get; set; } = true;

        /// <summary>
        /// 自动保存间隔（秒）
        /// </summary>
        public int AutoSaveIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// 最大自动保存文件数
        /// </summary>
        public int MaxAutoSaveFiles { get; set; } = 10;

        /// <summary>
        /// 是否启用崩溃恢复
        /// </summary>
        public bool CrashRecoveryEnabled { get; set; } = true;

        /// <summary>
        /// 自动保存目录
        /// </summary>
        public string AutoSaveDirectory { get; set; } = string.Empty;
    }

    /// <summary>
    /// 自动保存快照信息
    /// </summary>
    public class AutoSaveSnapshot
    {
        /// <summary>
        /// 快照ID
        /// </summary>
        public string SnapshotId { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 快照描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 数据类型（学习数据、笔记、设置等）
        /// </summary>
        public string DataType { get; set; } = string.Empty;
    }

    /// <summary>
    /// 恢复结果
    /// </summary>
    public class RecoveryResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 是否有可恢复的数据
        /// </summary>
        public bool HasRecoverableData { get; set; }

        /// <summary>
        /// 恢复的快照数量
        /// </summary>
        public int RecoveredCount { get; set; }

        /// <summary>
        /// 恢复的文件列表
        /// </summary>
        public List<string> RecoveredFiles { get; set; } = new();

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 上次退出时间
        /// </summary>
        public DateTime? LastExitTime { get; set; }

        /// <summary>
        /// 上次是否为异常退出
        /// </summary>
        public bool WasCrashed { get; set; }
    }

    /// <summary>
    /// 可自动保存的数据提供者接口
    /// </summary>
    public interface IAutoSaveProvider
    {
        /// <summary>
        /// 数据类型标识
        /// </summary>
        string DataType { get; }

        /// <summary>
        /// 保存数据到指定路径
        /// </summary>
        void SaveTo(string filePath);

        /// <summary>
        /// 从指定路径恢复数据
        /// </summary>
        bool RestoreFrom(string filePath);

        /// <summary>
        /// 获取数据描述
        /// </summary>
        string GetDescription();
    }
}
