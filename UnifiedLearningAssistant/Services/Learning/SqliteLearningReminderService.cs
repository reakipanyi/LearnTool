using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using UnifiedLearningAssistant.Data.Database;

namespace UnifiedLearningAssistant.Services.Learning
{
    public class SqliteLearningReminderService : ILearningReminderService, IDisposable
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private Timer? _checkTimer;
        private bool _disposed = false;

        public event EventHandler<ReminderTriggeredEventArgs>? ReminderTriggered;

        public SqliteLearningReminderService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
            
            // 确保数据库已创建
            using var db = _dbFactory.CreateDbContext();
            db.EnsureDatabaseCreated();
        }

        public void AddReminder(Reminder reminder)
        {
            using var db = _dbFactory.CreateDbContext();
            var entity = reminder.ToEntity();
            db.Reminders.Add(entity);
            db.SaveChanges();
        }

        public void RemoveReminder(Guid reminderId)
        {
            using var db = _dbFactory.CreateDbContext();
            var entity = db.Reminders.FirstOrDefault(r => r.Id == reminderId);
            if (entity != null)
            {
                db.Reminders.Remove(entity);
                db.SaveChanges();
            }
        }

        public List<Reminder> GetUserReminders(string userId)
        {
            using var db = _dbFactory.CreateDbContext();
            return db.Reminders
                .Where(r => r.UserId == userId)
                .OrderBy(r => r.CreatedAt)
                .Select(r => r.ToModel())
                .ToList();
        }

        public List<Reminder> GetUpcomingReminders(TimeSpan within)
        {
            var now = DateTime.Now;
            using var db = _dbFactory.CreateDbContext();
            var upcoming = new List<Reminder>();
            
            var enabledReminders = db.Reminders
                .Where(r => r.Enabled)
                .ToList();

            foreach (var entity in enabledReminders)
            {
                var reminder = entity.ToModel();
                if (ShouldTriggerReminder(reminder, now, within))
                {
                    upcoming.Add(reminder);
                }
            }

            return upcoming;
        }

        public void ToggleReminder(Guid reminderId, bool enabled)
        {
            using var db = _dbFactory.CreateDbContext();
            var entity = db.Reminders.FirstOrDefault(r => r.Id == reminderId);
            if (entity != null)
            {
                entity.Enabled = enabled;
                db.SaveChanges();
            }
        }

        public void SaveReminders()
        {
            // 这里不需要，因为每次操作都会立即保存
        }

        public void LoadReminders()
        {
            // 这里不需要，因为每次操作都会从数据库加载
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

            using var db = _dbFactory.CreateDbContext();
            var enabledReminders = db.Reminders.Where(r => r.Enabled).ToList();

            foreach (var entity in enabledReminders)
            {
                var reminder = entity.ToModel();
                if (ShouldTriggerReminder(reminder, now, TimeSpan.FromMinutes(1)))
                {
                    // 检查是否今天已经触发过
                    if (!reminder.LastTriggered.HasValue || 
                        reminder.LastTriggered.Value.Date < now.Date ||
                        (now - reminder.LastTriggered.Value).TotalMinutes > 5)
                    {
                        // 更新最后触发时间
                        entity.LastTriggered = now;
                        toTrigger.Add(reminder);
                    }
                }
            }

            if (toTrigger.Any())
            {
                db.SaveChanges();
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
