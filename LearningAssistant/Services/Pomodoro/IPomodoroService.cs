using LearningAssistant.Models.Pomodoro;

namespace LearningAssistant.Services.Pomodoro
{
    /// <summary>
    /// 番茄钟服务接口
    /// 提供专注计时、休息提醒、统计分析等功能
    /// </summary>
    public interface IPomodoroService
    {
        /// <summary>
        /// 当前状态
        /// </summary>
        PomodoroState CurrentState { get; }

        /// <summary>
        /// 剩余时间（秒）
        /// </summary>
        int RemainingSeconds { get; }

        /// <summary>
        /// 已用时间（秒）
        /// </summary>
        int ElapsedSeconds { get; }

        /// <summary>
        /// 总时长（秒）
        /// </summary>
        int TotalSeconds { get; }

        /// <summary>
        /// 当前轮次（已完成的番茄数）
        /// </summary>
        int CompletedCount { get; }

        /// <summary>
        /// 配置
        /// </summary>
        PomodoroConfig Config { get; }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 开始工作
        /// </summary>
        void StartWork(string? task = null);

        /// <summary>
        /// 开始短休息
        /// </summary>
        void StartShortBreak();

        /// <summary>
        /// 开始长休息
        /// </summary>
        void StartLongBreak();

        /// <summary>
        /// 暂停
        /// </summary>
        void Pause();

        /// <summary>
        /// 继续
        /// </summary>
        void Resume();

        /// <summary>
        /// 停止/重置
        /// </summary>
        void Stop();

        /// <summary>
        /// 跳过当前阶段
        /// </summary>
        void Skip();

        /// <summary>
        /// 重置今日计数
        /// </summary>
        void ResetDailyCount();

        /// <summary>
        /// 更新配置
        /// </summary>
        void UpdateConfig(Action<PomodoroConfig> updateAction);

        /// <summary>
        /// 获取统计信息
        /// </summary>
        PomodoroStatistics GetStatistics();

        /// <summary>
        /// 获取指定日期范围的记录
        /// </summary>
        List<PomodoroRecord> GetRecords(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取今日记录
        /// </summary>
        List<PomodoroRecord> GetTodayRecords();

        /// <summary>
        /// 添加手动记录
        /// </summary>
        void AddManualRecord(DateTime date, int minutes, string? task = null);

        /// <summary>
        /// 删除记录
        /// </summary>
        bool DeleteRecord(string recordId);

        /// <summary>
        /// 保存数据
        /// </summary>
        void SaveChanges();

        /// <summary>
        /// 计时器滴答事件（每秒触发）
        /// </summary>
        event EventHandler<int>? Tick;

        /// <summary>
        /// 状态变化事件
        /// </summary>
        event EventHandler<PomodoroState>? StateChanged;

        /// <summary>
        /// 番茄完成事件
        /// </summary>
        event EventHandler<int>? PomodoroCompleted;

        /// <summary>
        /// 阶段完成事件
        /// </summary>
        event EventHandler<PomodoroState>? PhaseCompleted;
    }
}
