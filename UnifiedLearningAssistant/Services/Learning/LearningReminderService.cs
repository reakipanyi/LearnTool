using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnifiedLearningAssistant.Common;

namespace UnifiedLearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习提醒服务实现
    /// </summary>
    public class LearningReminderService : ILearningReminderService, IDisposable
    {
        private List<Reminder> _reminders = new List<Reminder>();
        private readonly string _remindersFilePath;
        private Timer? _checkTimer;
        private readonly object _lock = new object();
        private bool _disposed = false;

        public event EventHandler<ReminderTriggeredEventArgs>? ReminderTriggered;

        public LearningReminderService()
        {
            _remindersFilePath = Path.Combine(FileHelper.GetAppDirectory(), "learning_reminders.json");
            LoadReminders();
        }

        public void AddReminder(Reminder reminder)
        {
            lock (_lock)
            {
                _reminders.Add(reminder);
                SaveReminders();
            }
        }

        public void RemoveReminder(Guid reminderId)
        {
            lock (_lock)
            {
                _reminders.RemoveAll(r => r.Id == reminderId);
                SaveReminders();
            }
        }

        public List<Reminder> GetUserReminders(string userId)
        {
            lock (_lock)
            {
                return _reminders.Where(r => r.UserId == userId).ToList();
            }
        }

        public List<Reminder> GetUpcomingReminders(TimeSpan within)
        {
            var now = DateTime.Now;
            var upcoming = new List<Reminder>();

            lock (_lock)
            {
                foreach (var reminder in _reminders.Where(r => r.Enabled))
                {
                    if (ShouldTriggerReminder(reminder, now, within))
                    {
                        upcoming.Add(reminder);
                    }
                }
            }

            return upcoming;
        }

        public void ToggleReminder(Guid reminderId, bool enabled)
        {
            lock (_lock)
            {
                var reminder = _reminders.FirstOrDefault(r => r.Id == reminderId);
                if (reminder != null)
                {
                    reminder.Enabled = enabled;
                    SaveReminders();
                }
            }
        }

        public void SaveReminders()
        {
            try
            {
                JsonHelper.SaveToFile(_remindersFilePath, _reminders);
            }
            catch
            {
                // 静默处理保存错误
            }
        }

        public void LoadReminders()
        {
            try
            {
                if (File.Exists(_remindersFilePath))
                {
                    var loaded = JsonHelper.LoadFromFile<List<Reminder>>(_remindersFilePath);
                    if (loaded != null)
                    {
                        _reminders = loaded;
                    }
                }
            }
            catch
            {
                // 静默处理加载错误
            }
        }

        public void Start()
        {
            if (_checkTimer == null)
            {
                // 每分钟检查一次
                _checkTimer = new Timer(CheckReminders, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            }
        }

        public void Stop()
        {
            _checkTimer?.Dispose();
            _checkTimer = null;
        }

        private void CheckReminders(object? state)
        {
            if (_disposed) return;

            var now = DateTime.Now;
            var toTrigger = new List<Reminder>();

            lock (_lock)
            {
                foreach (var reminder in _reminders.Where(r => r.Enabled))
                {
                    if (ShouldTriggerReminder(reminder, now, TimeSpan.FromMinutes(1)))
                    {
                        // 检查是否今天已经触发过
                        if (!reminder.LastTriggered.HasValue || 
                            reminder.LastTriggered.Value.Date < now.Date ||
                            (now - reminder.LastTriggered.Value).TotalMinutes > 5)
                        {
                            reminder.LastTriggered = now;
                            toTrigger.Add(reminder);
                        }
                    }
                }

                if (toTrigger.Any())
                {
                    SaveReminders();
                }
            }

            foreach (var reminder in toTrigger)
            {
                OnReminderTriggered(reminder);
            }
        }

        private bool ShouldTriggerReminder(Reminder reminder, DateTime now, TimeSpan within)
        {
            var reminderTimeToday = now.Date.Add(reminder.Time);
            var timeDiff = reminderTimeToday - now;

            // 检查时间是否在范围内
            if (timeDiff < TimeSpan.Zero || timeDiff > within)
                return false;

            // 检查重复规则
            switch (reminder.RepeatType)
            {
                case ReminderRepeatType.None:
                    // 不重复，只检查是否已经触发过
                    return !reminder.LastTriggered.HasValue;

                case ReminderRepeatType.Daily:
                    return true;

                case ReminderRepeatType.Weekdays:
                    return now.DayOfWeek >= DayOfWeek.Monday && 
                           now.DayOfWeek <= DayOfWeek.Friday;

                case ReminderRepeatType.Weekly:
                    return reminder.RepeatDays != null && 
                           reminder.RepeatDays.Contains(now.DayOfWeek);

                case ReminderRepeatType.Custom:
                    return reminder.RepeatDays != null && 
                           reminder.RepeatDays.Contains(now.DayOfWeek);

                default:
                    return false;
            }
        }

        protected virtual void OnReminderTriggered(Reminder reminder)
        {
            ReminderTriggered?.Invoke(this, new ReminderTriggeredEventArgs { Reminder = reminder });
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Stop();
            }
        }
    }
}
