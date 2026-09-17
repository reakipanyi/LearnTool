using LearningAssistant.Models.Recovery;

namespace LearningAssistant.Services.Recovery
{
    /// <summary>
    /// 崩溃恢复与自动保存服务接口
    /// 提供数据自动保存、崩溃检测、数据恢复等功能
    /// </summary>
    public interface ICrashRecoveryService
    {
        /// <summary>
        /// 是否启用自动保存
        /// </summary>
        bool AutoSaveEnabled { get; set; }

        /// <summary>
        /// 自动保存间隔（秒）
        /// </summary>
        int AutoSaveInterval { get; set; }

        /// <summary>
        /// 启动自动保存
        /// </summary>
        void StartAutoSave();

        /// <summary>
        /// 停止自动保存
        /// </summary>
        void StopAutoSave();

        /// <summary>
        /// 立即执行一次自动保存
        /// </summary>
        void SaveNow();

        /// <summary>
        /// 注册自动保存数据提供者
        /// </summary>
        void RegisterProvider(IAutoSaveProvider provider);

        /// <summary>
        /// 注销自动保存数据提供者
        /// </summary>
        void UnregisterProvider(string dataType);

        /// <summary>
        /// 检查上次是否异常退出
        /// </summary>
        bool CheckLastExitWasCrash();

        /// <summary>
        /// 获取可用的恢复快照列表
        /// </summary>
        List<AutoSaveSnapshot> GetRecoverableSnapshots();

        /// <summary>
        /// 从指定快照恢复数据
        /// </summary>
        RecoveryResult RestoreFromSnapshot(string snapshotId);

        /// <summary>
        /// 恢复最近的自动保存数据
        /// </summary>
        RecoveryResult RestoreLatest();

        /// <summary>
        /// 获取自动保存配置
        /// </summary>
        AutoSaveConfig GetConfig();

        /// <summary>
        /// 更新自动保存配置
        /// </summary>
        void UpdateConfig(AutoSaveConfig config);

        /// <summary>
        /// 标记应用正常退出
        /// </summary>
        void MarkNormalExit();

        /// <summary>
        /// 标记应用启动
        /// </summary>
        void MarkAppStarted();

        /// <summary>
        /// 清理旧的自动保存文件
        /// </summary>
        void CleanOldSnapshots(int? maxFiles = null);

        /// <summary>
        /// 上次自动保存时间
        /// </summary>
        DateTime? LastAutoSaveTime { get; }

        /// <summary>
        /// 自动保存状态变更事件
        /// </summary>
        event EventHandler? AutoSaveStateChanged;
    }
}
