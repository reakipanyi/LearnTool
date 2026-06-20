namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习提醒服务接口 - 管理学习提醒的增删改查和触发
    /// 支持定时提醒、重复提醒等功能
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
    }
}
