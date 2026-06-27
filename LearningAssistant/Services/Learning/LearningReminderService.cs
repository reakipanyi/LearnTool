using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using LearningAssistant.Common;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习提醒服务实现
    /// </summary>
    public class LearningReminderService : ILearningReminderService, IDisposable
    {
        private List<Reminder> _reminders = new List<Reminder>();
        private readonly string _remindersFilePath;
        private System.Timers.Timer? _checkTimer;
        private readonly object _lock = new object();
        private bool _disposed = false;

        public event EventHandler<ReminderTriggeredEventArgs>? ReminderTriggered;

        public LearningReminderService()
        {
            _remindersFilePath = AppPaths.RemindersPath;
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

        public void UpdateReminder(Reminder reminder)
        {
            lock (_lock)
            {
                var index = _reminders.FindIndex(r => r.Id == reminder.Id);
                if (index >= 0)
                {
                    reminder.UpdatedAt = DateTime.Now;
                    _reminders[index] = reminder;
                    SaveReminders();
                }
            }
        }

        public List<Reminder> GetUserReminders(string userId)
        {
            lock (_lock)
            {
                return _reminders.Where(r => r.UserId == userId).ToList();
            }
        }

        public List<Reminder> GetUserRemindersByType(string userId, ReminderType type)
        {
            lock (_lock)
            {
                return _reminders.Where(r => r.UserId == userId && r.Type == type).ToList();
            }
        }

        public void RecordReminderResponse(Guid reminderId, ReminderResponseType responseType)
        {
            lock (_lock)
            {
                var reminder = _reminders.FirstOrDefault(r => r.Id == reminderId);
                if (reminder != null)
                {
                    switch (responseType)
                    {
                        case ReminderResponseType.Opened:
                            reminder.OpenCount++;
                            break;
                        case ReminderResponseType.Snoozed:
                            reminder.SnoozeCount++;
                            break;
                        case ReminderResponseType.Dismissed:
                            reminder.DismissCount++;
                            break;
                    }
                    reminder.UpdatedAt = DateTime.Now;
                    SaveReminders();
                }
            }
        }

        public ReminderStats GetReminderStats(string userId)
        {
            lock (_lock)
            {
                var userReminders = _reminders.Where(r => r.UserId == userId).ToList();
                var today = DateTime.Today;

                var stats = new ReminderStats
                {
                    TotalReminders = userReminders.Count,
                    EnabledReminders = userReminders.Count(r => r.Enabled),
                    TriggeredToday = userReminders.Count(r => r.LastTriggered.HasValue && r.LastTriggered.Value.Date == today),
                    OpenedToday = userReminders.Sum(r => r.OpenCount),
                    SnoozedToday = userReminders.Sum(r => r.SnoozeCount),
                    DismissedToday = userReminders.Sum(r => r.DismissCount)
                };

                var totalResponses = stats.OpenedToday + stats.SnoozedToday + stats.DismissedToday;
                stats.ResponseRate = stats.TriggeredToday > 0
                    ? (double)totalResponses / stats.TriggeredToday * 100
                    : 0;

                stats.AverageSnoozeCount = stats.TriggeredToday > 0
                    ? (double)stats.SnoozedToday / stats.TriggeredToday
                    : 0;

                return stats;
            }
        }

        public void SnoozeReminder(Guid reminderId, TimeSpan snoozeTime)
        {
            lock (_lock)
            {
                var reminder = _reminders.FirstOrDefault(r => r.Id == reminderId);
                if (reminder != null)
                {
                    reminder.SnoozeCount++;
                    reminder.NextTriggerTime = DateTime.Now.Add(snoozeTime);
                    reminder.UpdatedAt = DateTime.Now;
                    SaveReminders();
                }
            }
        }

        public List<string> GetLearningRecommendations(string userId)
        {
            var recommendations = new List<string>
            {
                "继续保持学习习惯，每天进步一点点",
                "定期复习错题，巩固薄弱知识点",
                "合理安排学习时间，注意劳逸结合",
                "尝试不同的学习方法，找到最适合自己的",
                "设定明确的学习目标，保持学习动力",
                "多做练习，在实践中加深理解",
                "逐步拓展学习范围，开阔知识面"
            };
            return recommendations;
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
                _checkTimer = new System.Timers.Timer(TimeSpan.FromMinutes(1).TotalMilliseconds);
                _checkTimer.Elapsed += CheckReminders;
            }
            
            if (!_checkTimer.Enabled)
            {
                _checkTimer.Start();
            }
        }

        public void Stop()
        {
            _checkTimer?.Dispose();
            _checkTimer = null;
        }

        private void CheckReminders(object? sender, System.Timers.ElapsedEventArgs e)
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
