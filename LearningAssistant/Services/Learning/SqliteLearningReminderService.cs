using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LearningAssistant.Data.Database;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// SQLite 版本的学习提醒服务
    /// </summary>
    public class SqliteLearningReminderService : ILearningReminderService, IDisposable
    {
        private const int CheckIntervalMinutes = 1;
        private const int MinTriggerIntervalMinutes = 5;
        
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILearningAnalyticsService? _analyticsService;
        private readonly ILogger<SqliteLearningReminderService>? _logger;
        private System.Timers.Timer? _checkTimer;
        private bool _disposed = false;

        public event EventHandler<ReminderTriggeredEventArgs>? ReminderTriggered;

        public SqliteLearningReminderService(
            IDbContextFactory<AppDbContext> dbFactory,
            ILearningAnalyticsService? analyticsService = null,
            ILogger<SqliteLearningReminderService>? logger = null)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _analyticsService = analyticsService;
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
            ArgumentNullException.ThrowIfNull(reminder, nameof(reminder));
            
            try
            {
                _logger?.LogDebug("添加提醒: {Title}", reminder.Title);
                
                using var db = _dbFactory.CreateDbContext();
                
                // 检查是否已存在
                if (db.Reminders.Any(r => r.Id == reminder.Id))
                {
                    _logger?.LogWarning("提醒已存在: {ReminderId}", reminder.Id);
                    return;
                }
                
                var entity = reminder.ToEntity();
                db.Reminders.Add(entity);
                db.SaveChanges();
                
                // 保存 RepeatDays 到 ReminderRepeatDays 表
                SaveRepeatDays(db, reminder.Id, reminder.RepeatDays);
                
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
                    // 先删除关联的 RepeatDays
                    var repeatDays = db.ReminderRepeatDays.Where(r => r.ReminderId == reminderId);
                    db.ReminderRepeatDays.RemoveRange(repeatDays);
                    
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

        public void UpdateReminder(Reminder reminder)
        {
            ArgumentNullException.ThrowIfNull(reminder, nameof(reminder));
            
            try
            {
                _logger?.LogDebug("更新提醒: {ReminderId}", reminder.Id);
                
                using var db = _dbFactory.CreateDbContext();
                var entity = db.Reminders.FirstOrDefault(r => r.Id == reminder.Id);
                if (entity != null)
                {
                    entity.UpdateFromModel(reminder);
                    
                    // 更新 RepeatDays 表
                    var existingRepeatDays = db.ReminderRepeatDays.Where(r => r.ReminderId == reminder.Id);
                    db.ReminderRepeatDays.RemoveRange(existingRepeatDays);
                    SaveRepeatDays(db, reminder.Id, reminder.RepeatDays);
                    
                    db.SaveChanges();
                    _logger?.LogInformation("提醒更新成功: {ReminderId}", reminder.Id);
                }
                else
                {
                    _logger?.LogWarning("未找到要更新的提醒: {ReminderId}", reminder.Id);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "更新提醒失败: {ReminderId}", reminder.Id);
                throw;
            }
        }

        public List<Reminder> GetUserReminders(string userId)
        {
            try
            {
                _logger?.LogDebug("获取用户提醒列表: {UserId}", userId);
                
                using var db = _dbFactory.CreateDbContext();
                var reminderEntities = db.Reminders
                    .Where(r => r.UserId == userId)
                    .OrderBy(r => r.CreatedAt)
                    .ToList();
                
                var reminders = new List<Reminder>();
                foreach (var entity in reminderEntities)
                {
                    var reminder = entity.ToModel();
                    // 从 ReminderRepeatDays 表加载 RepeatDays
                    reminder.RepeatDays = LoadRepeatDays(db, reminder.Id);
                    reminders.Add(reminder);
                }
                
                _logger?.LogDebug("获取到 {Count} 个提醒", reminders.Count);
                return reminders;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取用户提醒列表失败: {UserId}", userId);
                throw;
            }
        }

        public List<Reminder> GetUserRemindersByType(string userId, ReminderType type)
        {
            try
            {
                _logger?.LogDebug("获取用户指定类型的提醒列表: {UserId}, {Type}", userId, type);
                
                var typeStr = type.ToString();
                using var db = _dbFactory.CreateDbContext();
                var reminderEntities = db.Reminders
                    .Where(r => r.UserId == userId && r.Type == typeStr)
                    .OrderBy(r => r.CreatedAt)
                    .ToList();
                
                var reminders = new List<Reminder>();
                foreach (var entity in reminderEntities)
                {
                    var reminder = entity.ToModel();
                    reminder.RepeatDays = LoadRepeatDays(db, reminder.Id);
                    reminders.Add(reminder);
                }
                
                _logger?.LogDebug("获取到 {Count} 个 {Type} 类型的提醒", reminders.Count, type);
                return reminders;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取用户指定类型的提醒列表失败: {UserId}, {Type}", userId, type);
                throw;
            }
        }

        public void RecordReminderResponse(Guid reminderId, ReminderResponseType responseType)
        {
            try
            {
                _logger?.LogDebug("记录提醒响应: {ReminderId}, {ResponseType}", reminderId, responseType);
                
                using var db = _dbFactory.CreateDbContext();
                var entity = db.Reminders.FirstOrDefault(r => r.Id == reminderId);
                if (entity != null)
                {
                    switch (responseType)
                    {
                        case ReminderResponseType.Opened:
                            entity.OpenCount++;
                            break;
                        case ReminderResponseType.Snoozed:
                            entity.SnoozeCount++;
                            break;
                        case ReminderResponseType.Dismissed:
                            entity.DismissCount++;
                            break;
                    }
                    entity.UpdatedAt = DateTime.Now;
                    db.SaveChanges();
                    
                    _logger?.LogInformation("提醒响应记录成功: {ReminderId}, {ResponseType}", reminderId, responseType);
                }
                else
                {
                    _logger?.LogWarning("未找到要记录响应的提醒: {ReminderId}", reminderId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "记录提醒响应失败: {ReminderId}", reminderId);
                throw;
            }
        }

        public ReminderStats GetReminderStats(string userId)
        {
            try
            {
                _logger?.LogDebug("获取提醒统计: {UserId}", userId);
                
                var today = DateTime.Today;
                using var db = _dbFactory.CreateDbContext();
                var userReminders = db.Reminders
                    .Where(r => r.UserId == userId)
                    .ToList();

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

                _logger?.LogDebug("提醒统计获取成功: {UserId}", userId);
                return stats;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取提醒统计失败: {UserId}", userId);
                throw;
            }
        }

        public void SnoozeReminder(Guid reminderId, TimeSpan snoozeTime)
        {
            try
            {
                _logger?.LogDebug("延后提醒: {ReminderId}, {SnoozeTime}", reminderId, snoozeTime);
                
                using var db = _dbFactory.CreateDbContext();
                var entity = db.Reminders.FirstOrDefault(r => r.Id == reminderId);
                if (entity != null)
                {
                    entity.SnoozeCount++;
                    entity.LastTriggered = DateTime.Now.Add(snoozeTime);
                    entity.UpdatedAt = DateTime.Now;
                    db.SaveChanges();
                    
                    _logger?.LogInformation("提醒延后成功: {ReminderId}, 延后 {SnoozeTime}", reminderId, snoozeTime);
                }
                else
                {
                    _logger?.LogWarning("未找到要延后的提醒: {ReminderId}", reminderId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "延后提醒失败: {ReminderId}", reminderId);
                throw;
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
                    reminder.RepeatDays = LoadRepeatDays(db, reminder.Id);
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
                    _logger?.LogInformation("启动提醒检查定时器（每 {Interval} 分钟检查一次）", CheckIntervalMinutes);
                    _checkTimer = new System.Timers.Timer(TimeSpan.FromMinutes(CheckIntervalMinutes).TotalMilliseconds);
                    _checkTimer.Elapsed += (sender, e) => CheckReminders(null);
                    _checkTimer.AutoReset = true;
                    _checkTimer.Start();
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

        /// <summary>
        /// 获取今天应触发的提醒
        /// </summary>
        public List<Reminder> GetRemindersDueToday(string userId)
        {
            try
            {
                var today = DateTime.Today;
                var reminders = GetUserReminders(userId).Where(r => r.Enabled).ToList();
                return reminders.Where(r => IsDueToday(r, today)).ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取今日提醒失败: {UserId}", userId);
                return new List<Reminder>();
            }
        }

        /// <summary>
        /// 获取提醒模板列表
        /// </summary>
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

        /// <summary>
        /// 从模板创建提醒
        /// </summary>
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

        /// <summary>
        /// 智能添加提醒 - 根据学习统计建议最佳提醒时间
        /// </summary>
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

            AddReminder(reminder);
            _logger?.LogInformation("智能添加提醒: {Title}, 建议时间: {Time}", title, suggestedTime);
        }

        /// <summary>
        /// 检查提醒是否在今天应触发
        /// </summary>
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

        /// <summary>
        /// 根据学习统计获取建议提醒时间
        /// </summary>
        private TimeSpan GetSuggestedReminderTime(string userId)
        {
            try
            {
                if (_analyticsService != null)
                {
                    var stats = _analyticsService.GetDailyStatistics(userId, DateTime.Today);
                    var streak = _analyticsService.GetStudyStreak(userId);

                    // 有学习习惯的用户建议晚间学习
                    if (streak > 0)
                    {
                        return TimeSpan.FromHours(20);
                    }

                    // 今日学习时间较短的用户建议下午学习
                    if (stats.TotalMinutes < 30)
                    {
                        return TimeSpan.FromHours(15);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "获取建议提醒时间失败，使用默认时间");
            }

            // 默认晚间8点
            return TimeSpan.FromHours(20);
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
                    reminder.RepeatDays = LoadRepeatDays(db, reminder.Id);
                    if (ShouldTriggerReminder(reminder, now, TimeSpan.FromMinutes(CheckIntervalMinutes)))
                    {
                        // 检查是否今天已经触发过
                        if (!reminder.LastTriggered.HasValue || 
                            reminder.LastTriggered.Value.Date < now.Date ||
                            (now - reminder.LastTriggered.Value).TotalMinutes > MinTriggerIntervalMinutes)
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

        private void SaveRepeatDays(AppDbContext db, Guid reminderId, List<DayOfWeek>? repeatDays)
        {
            if (repeatDays == null || repeatDays.Count == 0) return;

            foreach (var day in repeatDays)
            {
                db.ReminderRepeatDays.Add(new ReminderRepeatDayEntity
                {
                    ReminderId = reminderId,
                    DayOfWeek = (int)day,
                    CreatedAt = DateTime.Now
                });
            }
        }

        private List<DayOfWeek>? LoadRepeatDays(AppDbContext db, Guid reminderId)
        {
            var repeatDays = db.ReminderRepeatDays
                .Where(r => r.ReminderId == reminderId)
                .Select(r => (DayOfWeek)r.DayOfWeek)
                .ToList();

            return repeatDays.Count > 0 ? repeatDays : null;
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
