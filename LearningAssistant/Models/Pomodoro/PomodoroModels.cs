using System.Text.Json.Serialization;

namespace LearningAssistant.Models.Pomodoro
{
    /// <summary>
    /// 番茄钟状态
    /// </summary>
    public enum PomodoroState
    {
        /// <summary>
        /// 空闲/未开始
        /// </summary>
        Idle,

        /// <summary>
        /// 学习中/工作中
        /// </summary>
        Studying,

        /// <summary>
        /// 短休息
        /// </summary>
        ShortBreak,

        /// <summary>
        /// 长休息
        /// </summary>
        LongBreak,

        /// <summary>
        /// 暂停
        /// </summary>
        Paused
    }

    /// <summary>
    /// 番茄钟模式
    /// </summary>
    public enum PomodoroMode
    {
        /// <summary>
        /// 标准模式
        /// </summary>
        Standard,

        /// <summary>
        /// 自定义模式
        /// </summary>
        Custom,

        /// <summary>
        /// 深度工作模式
        /// </summary>
        DeepWork
    }

    /// <summary>
    /// 番茄钟配置
    /// </summary>
    public class PomodoroConfig
    {
        /// <summary>
        /// 工作时长（分钟）
        /// </summary>
        public int WorkDuration { get; set; } = 25;

        /// <summary>
        /// 短休息时长（分钟）
        /// </summary>
        public int ShortBreakDuration { get; set; } = 5;

        /// <summary>
        /// 长休息时长（分钟）
        /// </summary>
        public int LongBreakDuration { get; set; } = 15;

        /// <summary>
        /// 长休息间隔（完成几个番茄后长休息）
        /// </summary>
        public int LongBreakInterval { get; set; } = 4;

        /// <summary>
        /// 是否自动开始下一个
        /// </summary>
        public bool AutoStartNext { get; set; } = false;

        /// <summary>
        /// 是否启用通知
        /// </summary>
        public bool NotificationsEnabled { get; set; } = true;

        /// <summary>
        /// 是否启用声音提醒
        /// </summary>
        public bool SoundEnabled { get; set; } = true;

        /// <summary>
        /// 提醒声音路径
        /// </summary>
        public string? SoundPath { get; set; }

        /// <summary>
        /// 每日目标番茄数
        /// </summary>
        public int DailyTarget { get; set; } = 8;

        /// <summary>
        /// 模式
        /// </summary>
        public PomodoroMode Mode { get; set; } = PomodoroMode.Standard;

        /// <summary>
        /// 白噪音是否开启
        /// </summary>
        public bool WhiteNoiseEnabled { get; set; } = false;

        /// <summary>
        /// 白噪音类型
        /// </summary>
        public string WhiteNoiseType { get; set; } = "rain";

        /// <summary>
        /// 白噪音音量 (0-100)
        /// </summary>
        public int WhiteNoiseVolume { get; set; } = 50;
    }

    /// <summary>
    /// 番茄钟记录
    /// </summary>
    public class PomodoroRecord
    {
        /// <summary>
        /// 记录ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 类型（工作/短休息/长休息）
        /// </summary>
        public PomodoroState Type { get; set; }

        /// <summary>
        /// 实际持续时间（秒）
        /// </summary>
        public int DurationSeconds { get; set; }

        /// <summary>
        /// 计划持续时间（秒）
        /// </summary>
        public int PlannedDurationSeconds { get; set; }

        /// <summary>
        /// 是否完成
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        /// 相关任务/标签
        /// </summary>
        public string? Task { get; set; }

        /// <summary>
        /// 标签
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// 备注
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// 中断次数
        /// </summary>
        public int InterruptionCount { get; set; }
    }

    /// <summary>
    /// 番茄钟统计
    /// </summary>
    public class PomodoroStatistics
    {
        /// <summary>
        /// 今日完成番茄数
        /// </summary>
        public int TodayCount { get; set; }

        /// <summary>
        /// 今日专注时长（分钟）
        /// </summary>
        public int TodayFocusMinutes { get; set; }

        /// <summary>
        /// 本周完成番茄数
        /// </summary>
        public int WeekCount { get; set; }

        /// <summary>
        /// 本周专注时长（分钟）
        /// </summary>
        public int WeekFocusMinutes { get; set; }

        /// <summary>
        /// 本月完成番茄数
        /// </summary>
        public int MonthCount { get; set; }

        /// <summary>
        /// 本月专注时长（分钟）
        /// </summary>
        public int MonthFocusMinutes { get; set; }

        /// <summary>
        /// 总完成番茄数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 总专注时长（分钟）
        /// </summary>
        public int TotalFocusMinutes { get; set; }

        /// <summary>
        /// 连续天数
        /// </summary>
        public int StreakDays { get; set; }

        /// <summary>
        /// 每日数据（最近7天）
        /// </summary>
        public List<DailyPomodoroData> DailyData { get; set; } = new();

        /// <summary>
        /// 今日完成率
        /// </summary>
        public double TodayCompletionRate { get; set; }
    }

    /// <summary>
    /// 每日番茄数据
    /// </summary>
    public class DailyPomodoroData
    {
        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 完成数量
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 专注时长（分钟）
        /// </summary>
        public int FocusMinutes { get; set; }

        /// <summary>
        /// 日期显示字符串
        /// </summary>
        [JsonIgnore]
        public string DateDisplay => Date.ToString("MM/dd");
    }

    /// <summary>
    /// 番茄钟设置
    /// </summary>
    public class PomodoroSettings
    {
        /// <summary>
        /// 学习时长（分钟）
        /// </summary>
        public int StudyMinutes { get; set; } = 25;

        /// <summary>
        /// 短休息时长（分钟）
        /// </summary>
        public int ShortBreakMinutes { get; set; } = 5;

        /// <summary>
        /// 长休息时长（分钟）
        /// </summary>
        public int LongBreakMinutes { get; set; } = 15;

        /// <summary>
        /// 长休息间隔（完成几个番茄后长休息）
        /// </summary>
        public int LongBreakInterval { get; set; } = 4;

        /// <summary>
        /// 是否自动开始休息
        /// </summary>
        public bool AutoStartBreak { get; set; } = true;

        /// <summary>
        /// 是否自动开始学习
        /// </summary>
        public bool AutoStartStudy { get; set; } = false;

        /// <summary>
        /// 是否播放声音
        /// </summary>
        public bool PlaySound { get; set; } = true;

        /// <summary>
        /// 是否显示通知
        /// </summary>
        public bool ShowNotification { get; set; } = true;
    }

    /// <summary>
    /// 番茄钟状态变更事件参数
    /// </summary>
    public class PomodoroStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 旧状态
        /// </summary>
        public PomodoroState OldState { get; set; }

        /// <summary>
        /// 新状态
        /// </summary>
        public PomodoroState NewState { get; set; }

        /// <summary>
        /// 持续时间
        /// </summary>
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// 番茄钟每日统计
    /// </summary>
    public class PomodoroDailyStats
    {
        /// <summary>
        /// 完成番茄数
        /// </summary>
        public int CompletedPomodoros { get; set; }

        /// <summary>
        /// 总学习分钟数
        /// </summary>
        public int TotalStudyMinutes { get; set; }

        /// <summary>
        /// 总休息分钟数
        /// </summary>
        public int TotalBreakMinutes { get; set; }

        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }
    }
}
