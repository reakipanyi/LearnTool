namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习提醒服务接口
    /// </summary>
    public interface ILearningReminderService
    {
        /// <summary>
        /// 添加提醒
        /// </summary>
        void AddReminder(Reminder reminder);

        /// <summary>
        /// 移除提醒
        /// </summary>
        void RemoveReminder(Guid reminderId);

        /// <summary>
        /// 删除提醒（别名）
        /// </summary>
        void DeleteReminder(Guid reminderId);

        /// <summary>
        /// 更新提醒
        /// </summary>
        void UpdateReminder(Reminder reminder);

        /// <summary>
        /// 获取用户的所有提醒
        /// </summary>
        List<Reminder> GetUserReminders(string userId);

        /// <summary>
        /// 获取即将触发的提醒
        /// </summary>
        List<Reminder> GetUpcomingReminders(TimeSpan within);

        /// <summary>
        /// 启用/禁用提醒
        /// </summary>
        void ToggleReminder(Guid reminderId, bool enabled);

        /// <summary>
        /// 保存提醒数据
        /// </summary>
        void SaveReminders();

        /// <summary>
        /// 加载提醒数据
        /// </summary>
        void LoadReminders();

        /// <summary>
        /// 提醒触发事件
        /// </summary>
        event EventHandler<ReminderTriggeredEventArgs>? ReminderTriggered;

        /// <summary>
        /// 启动提醒检查
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
        public Reminder Reminder { get; set; } = null!;
    }

    /// <summary>
    /// 提醒重复类型
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
    /// 提醒模型
    /// </summary>
    public class Reminder
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TimeSpan Time { get; set; }
        public ReminderRepeatType RepeatType { get; set; }
        public List<DayOfWeek>? RepeatDays { get; set; }
        public bool Enabled { get; set; } = true;
        public DateTime? LastTriggered { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
