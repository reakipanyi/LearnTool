using Microsoft.Extensions.Logging;
using System.Timers;

namespace LearningAssistant.Services.Learning
{
    public interface IEnhancedReminderService
    {
        void AddSmartReminder(string userId, string title, ReminderRepeatType repeatType);
        void AddCustomReminder(Reminder reminder);
        void UpdateReminder(Guid reminderId, Action<Reminder> updateAction);
        void RemoveReminder(Guid reminderId);
        List<Reminder> GetActiveReminders(string userId);
        List<Reminder> GetRemindersDueToday(string userId);
        void EnableReminder(Guid reminderId);
        void DisableReminder(Guid reminderId);
        void ScheduleReminder(Reminder reminder);
        void ShowNotification(string title, string message);
        void CheckAndShowDueReminders();
        void SetReminderSound(string soundPath);
        List<ReminderTemplate> GetReminderTemplates();
        Reminder CreateFromTemplate(string userId, ReminderTemplate template);
    }

    public class ReminderTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public TimeSpan Time { get; set; }
        public ReminderRepeatType RepeatType { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class EnhancedReminderService : IEnhancedReminderService, IDisposable
    {
        private readonly ILearningReminderService _reminderService;
        private readonly ILearningAnalyticsService _analyticsService;
        private readonly ILogger<EnhancedReminderService>? _logger;
        private readonly System.Timers.Timer _checkTimer;
        private readonly List<Reminder> _activeReminders = new List<Reminder>();
        private string _soundPath = string.Empty;
        private bool _disposed = false;

        public event EventHandler<ReminderEventArgs>? ReminderTriggered;

        public EnhancedReminderService(
            ILearningReminderService reminderService,
            ILearningAnalyticsService analyticsService,
            ILogger<EnhancedReminderService>? logger = null)
        {
            _reminderService = reminderService;
            _analyticsService = analyticsService;
            _logger = logger;

            _checkTimer = new System.Timers.Timer(60000);
            _checkTimer.Elapsed += CheckReminders;
            _checkTimer.Start();

            _logger?.LogInformation("增强提醒服务已启动");
        }

        public void AddSmartReminder(string userId, string title, ReminderRepeatType repeatType)
        {
            var suggestedTime = GetSuggestedReminderTime(userId);

            var reminder = new Reminder
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Time = suggestedTime,
                RepeatType = repeatType,
                Enabled = true,
                CreatedAt = DateTime.Now
            };

            _reminderService.AddReminder(reminder);
            ScheduleReminder(reminder);

            _logger?.LogInformation("添加智能提醒: {Title}, 时间: {Time}", title, suggestedTime);
        }

        public void AddCustomReminder(Reminder reminder)
        {
            reminder.Id = Guid.NewGuid();
            reminder.CreatedAt = DateTime.Now;

            _reminderService.AddReminder(reminder);
            ScheduleReminder(reminder);

            _logger?.LogInformation("添加自定义提醒: {Title}, 时间: {Time}", reminder.Title, reminder.Time);
        }

        public void UpdateReminder(Guid reminderId, Action<Reminder> updateAction)
        {
            try
            {
                var allReminders = new List<Reminder>();

                foreach (var userId in new[] { "current_user", Environment.UserName })
                {
                    try
                    {
                        allReminders.AddRange(_reminderService.GetUserReminders(userId));
                    }
                    catch
                    {
                        continue;
                    }
                }

                var reminder = allReminders.FirstOrDefault(r => r.Id == reminderId);

                if (reminder != null)
                {
                    updateAction(reminder);
                    reminder.UpdatedAt = DateTime.Now;
                    _reminderService.UpdateReminder(reminder);

                    _logger?.LogInformation("更新提醒: {Id}", reminderId);
                }
                else
                {
                    _logger?.LogWarning("未找到提醒: {Id}", reminderId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "更新提醒失败: {Id}", reminderId);
            }
        }

        public void RemoveReminder(Guid reminderId)
        {
            _reminderService.DeleteReminder(reminderId);
            _activeReminders.RemoveAll(r => r.Id == reminderId);

            _logger?.LogInformation("删除提醒: {Id}", reminderId);
        }

        public List<Reminder> GetActiveReminders(string userId)
        {
            return _reminderService.GetUserReminders(userId).Where(r => r.Enabled).ToList();
        }

        public List<Reminder> GetRemindersDueToday(string userId)
        {
            var today = DateTime.Today;
            var reminders = GetActiveReminders(userId);

            return reminders.Where(r => IsDueToday(r, today)).ToList();
        }

        public void EnableReminder(Guid reminderId)
        {
            UpdateReminder(reminderId, r => r.Enabled = true);
        }

        public void DisableReminder(Guid reminderId)
        {
            UpdateReminder(reminderId, r => r.Enabled = false);
        }

        public void ScheduleReminder(Reminder reminder)
        {
            if (!reminder.Enabled)
                return;

            lock (_activeReminders)
            {
                var existing = _activeReminders.FirstOrDefault(r => r.Id == reminder.Id);
                if (existing != null)
                {
                    _activeReminders.Remove(existing);
                }
                _activeReminders.Add(reminder);
            }

            _logger?.LogDebug("调度提醒: {Title}", reminder.Title);
        }

        public void ShowNotification(string title, string message)
        {
            try
            {
                if (!string.IsNullOrEmpty(_soundPath) && File.Exists(_soundPath))
                {
                    System.Media.SoundPlayer player = new System.Media.SoundPlayer(_soundPath);
                    player.Play();
                }

                ReminderTriggered?.Invoke(this, new ReminderEventArgs(title, message));
                _logger?.LogInformation("显示通知: {Title}", title);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "显示通知失败");
            }
        }

        public void CheckAndShowDueReminders()
        {
            var now = DateTime.Now;
            var dueReminders = new List<Reminder>();

            lock (_activeReminders)
            {
                foreach (var reminder in _activeReminders.Where(r => r.Enabled))
                {
                    if (IsReminderDue(reminder, now))
                    {
                        dueReminders.Add(reminder);
                    }
                }
            }

            foreach (var reminder in dueReminders)
            {
                ShowNotification(reminder.Title, "该学习啦！");

                if (reminder.RepeatType == ReminderRepeatType.Once)
                {
                    DisableReminder(reminder.Id);
                }
            }
        }

        public void SetReminderSound(string soundPath)
        {
            _soundPath = soundPath;
            _logger?.LogInformation("提醒音效已设置: {Path}", soundPath);
        }

        public List<ReminderTemplate> GetReminderTemplates()
        {
            return new List<ReminderTemplate>
            {
                new ReminderTemplate
                {
                    Name = "晨间学习",
                    Title = "早上好！开始学习吧",
                    Time = TimeSpan.FromHours(8),
                    RepeatType = ReminderRepeatType.Workday,
                    Description = "工作日早上8点提醒"
                },
                new ReminderTemplate
                {
                    Name = "午后复习",
                    Title = "该复习啦！",
                    Time = TimeSpan.FromHours(14),
                    RepeatType = ReminderRepeatType.Daily,
                    Description = "每天下午2点复习提醒"
                },
                new ReminderTemplate
                {
                    Name = "晚间学习",
                    Title = "晚上好！别忘了学习",
                    Time = TimeSpan.FromHours(20),
                    RepeatType = ReminderRepeatType.Daily,
                    Description = "每天晚上8点学习提醒"
                },
                new ReminderTemplate
                {
                    Name = "周末复习",
                    Title = "周末愉快！别忘了复习",
                    Time = TimeSpan.FromHours(10),
                    RepeatType = ReminderRepeatType.Weekend,
                    Description = "周末上午10点复习"
                }
            };
        }

        public Reminder CreateFromTemplate(string userId, ReminderTemplate template)
        {
            return new Reminder
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = template.Title,
                Time = template.Time,
                RepeatType = template.RepeatType,
                Enabled = true,
                CreatedAt = DateTime.Now
            };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _checkTimer?.Stop();
                _checkTimer?.Dispose();
                _logger?.LogInformation("EnhancedReminderService disposed");
            }

            _disposed = true;
        }

        private TimeSpan GetSuggestedReminderTime(string userId)
        {
            try
            {
                var stats = _analyticsService.GetDailyStatistics(userId, DateTime.Today);
                var streak = _analyticsService.GetStudyStreak(userId);

                if (streak > 0)
                {
                    return TimeSpan.FromHours(20);
                }

                if (stats.TotalMinutes < 30)
                {
                    return TimeSpan.FromHours(15);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "获取建议提醒时间失败，使用默认时间");
            }

            return TimeSpan.FromHours(20);
        }

        private bool IsDueToday(Reminder reminder, DateTime today)
        {
            switch (reminder.RepeatType)
            {
                case ReminderRepeatType.Daily:
                    return true;
                case ReminderRepeatType.Workday:
                    return today.DayOfWeek != DayOfWeek.Saturday && today.DayOfWeek != DayOfWeek.Sunday;
                case ReminderRepeatType.Weekend:
                    return today.DayOfWeek == DayOfWeek.Saturday || today.DayOfWeek == DayOfWeek.Sunday;
                case ReminderRepeatType.Once:
                    return reminder.CreatedAt.Date == today.Date;
                case ReminderRepeatType.Weekly:
                    return today.DayOfWeek == reminder.CreatedAt.DayOfWeek;
                default:
                    return true;
            }
        }

        private bool IsReminderDue(Reminder reminder, DateTime now)
        {
            if (!reminder.Enabled)
                return false;

            if (!IsDueToday(reminder, now.Date))
                return false;

            var reminderTime = now.Date + reminder.Time;
            var tolerance = TimeSpan.FromMinutes(5);

            return now >= reminderTime && now < reminderTime + tolerance;
        }

        private void CheckReminders(object? sender, ElapsedEventArgs e)
        {
            try
            {
                CheckAndShowDueReminders();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "检查提醒失败");
            }
        }
    }

    public class ReminderEventArgs : EventArgs
    {
        public string Title { get; }
        public string Message { get; }

        public ReminderEventArgs(string title, string message)
        {
            Title = title;
            Message = message;
        }
    }
}