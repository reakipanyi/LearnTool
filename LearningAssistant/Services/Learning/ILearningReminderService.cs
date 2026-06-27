namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习提醒服务接口 - 管理学习提醒的增删改查和触发
    /// 支持定时提醒、重复提醒、多种提醒类型等功能
    /// </summary>
    public interface ILearningReminderService
    {
        /// <summary>
        /// 添加提醒
        /// </summary>
        /// <param name="reminder">提醒对象</param>
        void AddReminder(Reminder reminder);

        /// <summary>
        /// 移除提醒
        /// </summary>
        /// <param name="reminderId">提醒唯一ID</param>
        void RemoveReminder(Guid reminderId);

        /// <summary>
        /// 更新提醒
        /// </summary>
        /// <param name="reminder">更新后的提醒对象</param>
        void UpdateReminder(Reminder reminder);

        /// <summary>
        /// 获取用户的所有提醒
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>提醒列表</returns>
        List<Reminder> GetUserReminders(string userId);

        /// <summary>
        /// 获取指定类型的提醒
        /// </summary>
        List<Reminder> GetUserRemindersByType(string userId, ReminderType type);

        /// <summary>
        /// 获取即将触发的提醒
        /// </summary>
        /// <param name="within">时间范围</param>
        /// <returns>即将触发的提醒列表</returns>
        List<Reminder> GetUpcomingReminders(TimeSpan within);

        /// <summary>
        /// 启用/禁用提醒
        /// </summary>
        /// <param name="reminderId">提醒唯一ID</param>
        /// <param name="enabled">是否启用</param>
        void ToggleReminder(Guid reminderId, bool enabled);

        /// <summary>
        /// 保存提醒数据到持久化存储
        /// </summary>
        void SaveReminders();

        /// <summary>
        /// 从持久化存储加载提醒数据
        /// </summary>
        void LoadReminders();

        /// <summary>
        /// 记录提醒响应
        /// </summary>
        void RecordReminderResponse(Guid reminderId, ReminderResponseType responseType);

        /// <summary>
        /// 获取提醒统计
        /// </summary>
        ReminderStats GetReminderStats(string userId);

        /// <summary>
        /// 延后提醒
        /// </summary>
        void SnoozeReminder(Guid reminderId, TimeSpan snoozeTime);

        /// <summary>
        /// 获取学习建议列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>学习建议列表</returns>
        List<string> GetLearningRecommendations(string userId);

        /// <summary>
        /// 提醒触发事件 - 当提醒时间到达时触发
        /// </summary>
        event EventHandler<ReminderTriggeredEventArgs>? ReminderTriggered;

        /// <summary>
        /// 启动提醒检查（后台定时检查提醒是否到达）
        /// </summary>
        void Start();

        /// <summary>
        /// 停止提醒检查
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// 提醒触发事件参数
    /// </summary>
    public class ReminderTriggeredEventArgs : EventArgs
    {
        /// <summary>
        /// 被触发的提醒对象
        /// </summary>
        public Reminder Reminder { get; set; } = null!;
    }

    /// <summary>
    /// 提醒类型
    /// </summary>
    public enum ReminderType
    {
        Study,
        Review,
        Rest,
        Water,
        Custom
    }

    /// <summary>
    /// 提醒触发方式
    /// </summary>
    public enum ReminderTrigger
    {
        FixedTime,
        Interval,
        StudyTime,
        AfterSession
    }

    /// <summary>
    /// 提醒响应类型
    /// </summary>
    public enum ReminderResponseType
    {
        Opened,
        Snoozed,
        Dismissed
    }

    /// <summary>
    /// 提醒重复类型枚举
    /// </summary>
    public enum ReminderRepeatType
    {
        None,
        Daily,
        Weekly,
        Weekdays,
        Custom,
        Workday,
        Weekend,
        Once
    }

    /// <summary>
    /// 提醒统计
    /// </summary>
    public class ReminderStats
    {
        public int TotalReminders { get; set; }
        public int EnabledReminders { get; set; }
        public int TriggeredToday { get; set; }
        public int OpenedToday { get; set; }
        public int SnoozedToday { get; set; }
        public int DismissedToday { get; set; }
        public double ResponseRate { get; set; }
        public double AverageSnoozeCount { get; set; }
    }

    /// <summary>
    /// 提醒数据模型 - 包含提醒的所有属性
    /// </summary>
    public class Reminder
    {
        /// <summary>
        /// 提醒唯一ID
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 所属用户ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 提醒类型
        /// </summary>
        public ReminderType Type { get; set; } = ReminderType.Study;

        /// <summary>
        /// 提醒标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 提醒描述（可选）
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 提醒时间（每天的触发时间点）
        /// </summary>
        public TimeSpan Time { get; set; }

        /// <summary>
        /// 重复类型
        /// </summary>
        public ReminderRepeatType RepeatType { get; set; }

        /// <summary>
        /// 自定义重复的星期列表（当RepeatType为Custom时使用）
        /// </summary>
        public List<DayOfWeek>? RepeatDays { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 最后触发时间
        /// </summary>
        public DateTime? LastTriggered { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 触发方式
        /// </summary>
        public ReminderTrigger Trigger { get; set; } = ReminderTrigger.FixedTime;

        /// <summary>
        /// 固定时间（FixedTime 模式）
        /// </summary>
        public TimeSpan? FixedTime { get; set; }

        /// <summary>
        /// 间隔分钟数（Interval 模式）
        /// </summary>
        public int? IntervalMinutes { get; set; }

        /// <summary>
        /// 学习时长阈值（StudyTime 模式，分钟）
        /// </summary>
        public int? StudyMinutesThreshold { get; set; }

        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// 是否显示弹窗
        /// </summary>
        public bool ShowPopup { get; set; } = true;

        /// <summary>
        /// 是否播放音效
        /// </summary>
        public bool PlaySound { get; set; } = true;

        /// <summary>
        /// 是否语音播报
        /// </summary>
        public bool SpeakText { get; set; } = false;

        /// <summary>
        /// 提醒图标 emoji
        /// </summary>
        public string Icon { get; set; } = "📚";

        /// <summary>
        /// 累计触发次数
        /// </summary>
        public int TriggerCount { get; set; }

        /// <summary>
        /// 累计打开次数
        /// </summary>
        public int OpenCount { get; set; }

        /// <summary>
        /// 累计延后次数
        /// </summary>
        public int SnoozeCount { get; set; }

        /// <summary>
        /// 累计忽略次数
        /// </summary>
        public int DismissCount { get; set; }

        /// <summary>
        /// 下次触发时间（内部计算用）
        /// </summary>
        public DateTime? NextTriggerTime { get; set; }
    }
}
