using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UnifiedLearningAssistant.Data.Database;

namespace UnifiedLearningAssistant.Services.Learning
{
    /// <summary>
    /// SQLite 版本的学习提醒服务
    /// </summary>
    public class SqliteLearningReminderService : ILearningReminderService, IDisposable
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<SqliteLearningReminderService>? _logger;
        private Timer? _checkTimer;
        private bool _disposed = false;

        public event EventHandler<ReminderTriggeredEventArgs>? ReminderTriggered;

        public SqliteLearningReminderService(
            IDbContextFactory<AppDbContext> dbFactory,
            ILogger<SqliteLearningReminderService>? logger = null)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _logger = logger;
            
            try
            {
                // 确保数据库已创建
                using var db = _dbFactory.CreateDbContext();
                db.EnsureDatabaseCreated();
                _logger?.LogInformation("SqliteLearningReminderService 初始化成功");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "SqliteLearningReminderService 初始化失败");
                throw;
            }
        }

        public void AddReminder(Reminder reminder)
        {
            try
            {
                _logger?.LogDebug("添加提醒: {Title}", reminder.Title);
                
                using var db = _dbFactory.CreateDbContext();
                var entity = reminder.ToEntity();
                db.Reminders.Add(entity);
                db.SaveChanges();
                
                _logger?.LogInformation("提醒添加成功: {ReminderId}", reminder.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "添加提醒失败: {Title}", reminder.Title);
                throw;
            }
        }

        public void RemoveReminder(Guid reminderId)
        {
            try
            {
                _logger?.LogDebug("删除提醒: {ReminderId}", reminderId);
                
                using var db = _dbFactory.CreateDbContext();
                var entity = db.Reminders.FirstOrDefault(r => r.Id == reminderId);
                if (entity != null)
                {
                    db.Reminders.Remove(entity);
                    db.SaveChanges();
                    _logger?.LogInformation("提醒删除成功: {ReminderId}", reminderId);
                }
                else
                {
                    _logger?.LogWarning("未找到要删除的提醒: {ReminderId}", reminderId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "删除提醒失败: {ReminderId}", reminderId);
                throw;
            }
        }

        public List<Reminder> GetUserReminders(string userId)
        {
            try
            {
                _logger?.LogDebug("获取用户提醒列表: {UserId}", userId);
                
                using var db = _dbFactory.CreateDbContext();
                var reminders = db.Reminders
                    .Where(r => r.UserId == userId)
                    .OrderBy(r => r.CreatedAt)
                    .Select(r => r.ToModel())
                    .ToList();
                
                _logger?.LogDebug("获取到 {Count} 个提醒", reminders.Count);
                return reminders;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取用户提醒列表失败: {UserId}", userId);
                throw;
            }
        }

        public List<Reminder> GetUpcomingReminders(TimeSpan within)
        {
            try
            {
                _logger?.LogDebug("获取即将触发的提醒，时间范围: {Within}", within);
                
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

                _logger?.LogDebug("找到 {Count} 个即将触发的提醒", upcoming.Count);
                return upcoming;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取即将触发的提醒失败");
                throw;
            }
        }

        public void ToggleReminder(Guid reminderId, bool enabled)
        {
            try
            {
                _logger?.LogDebug("切换提醒状态: {ReminderId} -> {Enabled}", reminderId, enabled);
                
                using var db = _dbFactory.CreateDbContext();
                var entity = db.Reminders.FirstOrDefault(r => r.Id == reminderId);
                if (entity != null)
                {
                    entity.Enabled = enabled;
                    db.SaveChanges();
                    _logger?.LogInformation("提醒状态更新成功: {ReminderId} -> {Enabled}", reminderId, enabled);
                }
                else
                {
                    _logger?.LogWarning("未找到要更新的提醒: {ReminderId}", reminderId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "切换提醒状态失败: {ReminderId}", reminderId);
                throw;
            }
        }

        public void SaveReminders()
        {
            _logger?.LogDebug("SaveReminders（SQLite 版本无需此操作）");
            // 这里不需要，因为每次操作都会立即保存
        }

        public void LoadReminders()
        {
            _logger?.LogDebug("LoadReminders（SQLite 版本无需此操作）");
            // 这里不需要，因为每次操作都会从数据库加载
        }

        public void Start()
        {
            try
            {
                if (_checkTimer == null)
                {
                    _logger?.LogInformation("启动提醒检查定时器（每分钟检查一次）");
                    // 每分钟检查一次
                    _checkTimer = new Timer(CheckReminders, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
                }
                else
                {
                    _logger?.LogDebug("提醒检查定时器已在运行");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "启动提醒检查定时器失败");
                throw;
            }
        }

        public void Stop()
        {
            try
            {
                _logger?.LogInformation("停止提醒检查定时器");
                _checkTimer?.Dispose();
                _checkTimer = null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "停止提醒检查定时器失败");
                throw;
            }
        }

        private void CheckReminders(object? state)
        {
            if (_disposed) return;

            try
            {
                var now = DateTime.Now;
                var toTrigger = new List<Reminder>();

                using var db = _dbFactory.CreateDbContext();
                var enabledReminders = db.Reminders.Where(r => r.Enabled).ToList();

                _logger?.LogDebug("检查提醒，共有 {Count} 个启用的提醒", enabledReminders.Count);

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
                    _logger?.LogInformation("保存了 {Count} 个提醒的触发时间", toTrigger.Count);
                }

                foreach (var reminder in toTrigger)
                {
                    OnReminderTriggered(reminder);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "检查提醒过程中发生错误");
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
