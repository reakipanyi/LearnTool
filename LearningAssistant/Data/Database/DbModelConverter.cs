using System;
using System.Collections.Generic;
using System.Linq;
using LearningAssistant.Common;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Learning;

namespace LearningAssistant.Data.Database
{
    /// <summary>
    /// 数据库模型转换器，用于在实体模型和领域模型之间进行转换
    /// </summary>
    public static class DbModelConverter
    {
        /// <summary>
        /// 将 UserProfile 转换为 UserProfileEntity
        /// </summary>
        public static UserProfileEntity ToEntity(this UserProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            
            return new UserProfileEntity
            {
                UserId = profile.UserId,
                UserName = profile.UserName,
                CreatedAt = profile.CreatedAt,
                LastLoginTime = profile.LastLoginTime,
                AvatarPath = profile.AvatarPath,
                ConsecutiveStudyDays = profile.ConsecutiveStudyDays,
                LastStudyDate = profile.LastStudyDate,
                TotalStudyTimeMinutes = profile.TotalStudyTimeMinutes,
                TodayStudyTimeMinutes = profile.TodayStudyTimeMinutes,
                TodayItemsStudied = profile.TodayItemsStudied,
                XP = profile.XP,
                TotalXP = profile.TotalXP,
                Level = profile.Level,
                Coins = profile.Coins,
                TotalItemsStudied = profile.TotalItemsStudied,
                StudyDays = profile.StudyDays,
                LongestStreak = profile.LongestStreak
            };
        }

        /// <summary>
        /// 更新已存在的 UserProfileEntity
        /// </summary>
        public static void UpdateEntity(this UserProfileEntity entity, UserProfile profile)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            entity.UserName = profile.UserName;
            entity.LastLoginTime = profile.LastLoginTime;
            entity.AvatarPath = profile.AvatarPath;
            entity.ConsecutiveStudyDays = profile.ConsecutiveStudyDays;
            entity.LastStudyDate = profile.LastStudyDate;
            entity.TotalStudyTimeMinutes = profile.TotalStudyTimeMinutes;
            entity.TodayStudyTimeMinutes = profile.TodayStudyTimeMinutes;
            entity.TodayItemsStudied = profile.TodayItemsStudied;
            entity.XP = profile.XP;
            entity.TotalXP = profile.TotalXP;
            entity.Level = profile.Level;
            entity.Coins = profile.Coins;
            entity.TotalItemsStudied = profile.TotalItemsStudied;
            entity.StudyDays = profile.StudyDays;
            entity.LongestStreak = profile.LongestStreak;
        }

        /// <summary>
        /// 将 UserProfileEntity 转换为 UserProfile
        /// </summary>
        public static UserProfile ToModel(this UserProfileEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            var profile = new UserProfile
            {
                UserId = entity.UserId,
                UserName = entity.UserName,
                CreatedAt = entity.CreatedAt,
                LastLoginTime = entity.LastLoginTime,
                AvatarPath = entity.AvatarPath,
                ConsecutiveStudyDays = entity.ConsecutiveStudyDays,
                LastStudyDate = entity.LastStudyDate,
                TotalStudyTimeMinutes = entity.TotalStudyTimeMinutes,
                TodayStudyTimeMinutes = entity.TodayStudyTimeMinutes,
                TodayItemsStudied = entity.TodayItemsStudied,
                XP = entity.XP,
                TotalXP = entity.TotalXP,
                Level = entity.Level,
                Coins = entity.Coins,
                TotalItemsStudied = entity.TotalItemsStudied,
                StudyDays = entity.StudyDays,
                LongestStreak = entity.LongestStreak
            };

            // 加载分类进度
            foreach (var catEntity in entity.CategoryProgresses ?? Enumerable.Empty<CategoryProgressEntity>())
            {
                try
                {
                    profile.LearningProgress.CategoryProgresses[catEntity.CategoryName] = catEntity.ToModel();
                }
                catch
                {
                    // 忽略单个分类的转换错误
                }
            }

            // 计算总进度
            // TotalItemsStudied / TotalItemsMastered 已改为计算属性，自动从 CategoryProgresses 聚合
            // 无需手动赋值

            return profile;
        }

        /// <summary>
        /// 将 CategoryProgress 转换为 CategoryProgressEntity
        /// </summary>
        public static CategoryProgressEntity ToEntity(this CategoryProgress progress, string userId)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (string.IsNullOrEmpty(userId)) throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            
            string knownItemsJson;
            string unknownItemsJson;
            
            try
            {
                knownItemsJson = JsonHelper.Serialize(progress.KnownItems);
                unknownItemsJson = JsonHelper.Serialize(progress.UnknownItems);
            }
            catch
            {
                knownItemsJson = "[]";
                unknownItemsJson = "[]";
            }
            
            return new CategoryProgressEntity
            {
                UserId = userId,
                CategoryName = progress.CategoryName,
                KnownItemsJson = knownItemsJson,
                UnknownItemsJson = unknownItemsJson,
                TotalTestCount = progress.TotalTestCount,
                CorrectCount = progress.CorrectCount,
                LastTestDate = progress.LastTestDate,
                LastResumeIndex = progress.LastResumeIndex,
                QuickTestResumeIndex = progress.QuickTestResumeIndex,
                LastStudyMode = progress.LastStudyMode
            };
        }

        /// <summary>
        /// 将 CategoryProgressEntity 转换为 CategoryProgress
        /// </summary>
        public static CategoryProgress ToModel(this CategoryProgressEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            List<string> knownItems;
            List<string> unknownItems;
            
            try
            {
                knownItems = JsonHelper.Deserialize<List<string>>(entity.KnownItemsJson) ?? new List<string>();
                unknownItems = JsonHelper.Deserialize<List<string>>(entity.UnknownItemsJson) ?? new List<string>();
            }
            catch
            {
                knownItems = new List<string>();
                unknownItems = new List<string>();
            }
            
            return new CategoryProgress
            {
                CategoryName = entity.CategoryName,
                KnownItems = knownItems,
                UnknownItems = unknownItems,
                TotalTestCount = entity.TotalTestCount,
                CorrectCount = entity.CorrectCount,
                LastTestDate = entity.LastTestDate,
                LastResumeIndex = entity.LastResumeIndex,
                QuickTestResumeIndex = entity.QuickTestResumeIndex,
                LastStudyMode = entity.LastStudyMode
            };
        }

        /// <summary>
        /// 将 Reminder 转换为 ReminderEntity
        /// </summary>
        public static ReminderEntity ToEntity(this Reminder reminder)
        {
            if (reminder == null) throw new ArgumentNullException(nameof(reminder));
            
            string? repeatDaysJson = null;
            try
            {
                repeatDaysJson = reminder.RepeatDays != null ? JsonHelper.Serialize(reminder.RepeatDays) : null;
            }
            catch
            {
                // 忽略序列化错误
            }
            
            return new ReminderEntity
            {
                Id = reminder.Id,
                UserId = reminder.UserId,
                Type = reminder.Type.ToString(),
                Title = reminder.Title,
                Description = reminder.Description,
                Time = reminder.Time,
                RepeatType = reminder.RepeatType.ToString(),
                RepeatDaysJson = repeatDaysJson,
                Enabled = reminder.Enabled,
                LastTriggered = reminder.LastTriggered,
                CreatedAt = reminder.CreatedAt,
                UpdatedAt = reminder.UpdatedAt,
                TriggerCount = reminder.TriggerCount,
                OpenCount = reminder.OpenCount,
                SnoozeCount = reminder.SnoozeCount,
                DismissCount = reminder.DismissCount
            };
        }

        /// <summary>
        /// 将 ReminderEntity 转换为 Reminder
        /// </summary>
        public static Reminder ToModel(this ReminderEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            var repeatType = Enum.TryParse<ReminderRepeatType>(entity.RepeatType, out var rt) 
                ? rt 
                : ReminderRepeatType.None;

            var reminderType = Enum.TryParse<ReminderType>(entity.Type, out var rType)
                ? rType
                : ReminderType.Study;
            
            List<DayOfWeek>? repeatDays = null;
            try
            {
                repeatDays = !string.IsNullOrEmpty(entity.RepeatDaysJson) 
                    ? JsonHelper.Deserialize<List<DayOfWeek>>(entity.RepeatDaysJson) 
                    : null;
            }
            catch
            {
                // 忽略反序列化错误
            }

            return new Reminder
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Type = reminderType,
                Title = entity.Title,
                Description = entity.Description,
                Time = entity.Time,
                RepeatType = repeatType,
                RepeatDays = repeatDays,
                Enabled = entity.Enabled,
                LastTriggered = entity.LastTriggered,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                TriggerCount = entity.TriggerCount,
                OpenCount = entity.OpenCount,
                SnoozeCount = entity.SnoozeCount,
                DismissCount = entity.DismissCount
            };
        }

        /// <summary>
        /// 更新 ReminderEntity 从 Reminder
        /// </summary>
        public static void UpdateFromModel(this ReminderEntity entity, Reminder reminder)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (reminder == null) throw new ArgumentNullException(nameof(reminder));

            string? repeatDaysJson = null;
            try
            {
                repeatDaysJson = reminder.RepeatDays != null ? JsonHelper.Serialize(reminder.RepeatDays) : null;
            }
            catch
            {
                // 忽略序列化错误
            }

            entity.Type = reminder.Type.ToString();
            entity.Title = reminder.Title;
            entity.Description = reminder.Description;
            entity.Time = reminder.Time;
            entity.RepeatType = reminder.RepeatType.ToString();
            entity.RepeatDaysJson = repeatDaysJson;
            entity.Enabled = reminder.Enabled;
            entity.LastTriggered = reminder.LastTriggered;
            entity.UpdatedAt = DateTime.Now;
            entity.TriggerCount = reminder.TriggerCount;
            entity.OpenCount = reminder.OpenCount;
            entity.SnoozeCount = reminder.SnoozeCount;
            entity.DismissCount = reminder.DismissCount;
        }

        public static ReviewLogEntity ToEntity(this ReviewLog log)
        {
            return new ReviewLogEntity
            {
                Id = log.Id,
                UserId = log.UserId,
                ContentId = log.ContentId,
                Rating = log.Rating,
                Interval = log.Interval,
                EaseFactor = log.EaseFactor,
                Stability = log.Stability,
                Difficulty = log.Difficulty,
                ReviewTime = log.ReviewTime,
                Duration = log.Duration,
                AlgorithmType = log.AlgorithmType,
                CreatedAt = log.CreatedAt
            };
        }

        public static ReviewLog ToModel(this ReviewLogEntity entity)
        {
            return new ReviewLog
            {
                Id = entity.Id,
                UserId = entity.UserId,
                ContentId = entity.ContentId,
                Rating = entity.Rating,
                Interval = entity.Interval,
                EaseFactor = entity.EaseFactor,
                Stability = entity.Stability,
                Difficulty = entity.Difficulty,
                ReviewTime = entity.ReviewTime,
                Duration = entity.Duration,
                AlgorithmType = entity.AlgorithmType,
                CreatedAt = entity.CreatedAt
            };
        }

        public static SpacedRepetitionItemEntity ToEntity(this ReviewItem item)
        {
            return new SpacedRepetitionItemEntity
            {
                Id = item.Id,
                UserId = item.UserId,
                Content = item.Content,
                Answer = item.Answer,
                Interval = item.Interval,
                Repetitions = item.Repetitions,
                EFactor = item.EFactor,
                NextReviewDate = item.NextReviewDate,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                WrongCount = item.WrongCount,
                CorrectCount = item.CorrectCount,
                IsActive = item.IsActive,
                Stability = item.Stability,
                Difficulty = item.Difficulty,
                Retrievability = item.Retrievability,
                LearningStage = item.LearningStage,
                LastReviewDate = item.LastReviewDate,
                ReviewCount = item.ReviewCount,
                CorrectStreak = item.CorrectStreak,
                AlgorithmType = item.AlgorithmType,
                Category = item.Category,
                Subject = item.Subject
            };
        }

        public static ReviewItem ToModel(this SpacedRepetitionItemEntity entity)
        {
            return new ReviewItem
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Content = entity.Content,
                Answer = entity.Answer,
                Interval = entity.Interval,
                Repetitions = entity.Repetitions,
                EFactor = entity.EFactor,
                NextReviewDate = entity.NextReviewDate,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                WrongCount = entity.WrongCount,
                CorrectCount = entity.CorrectCount,
                IsActive = entity.IsActive,
                Stability = entity.Stability,
                Difficulty = entity.Difficulty,
                Retrievability = entity.Retrievability,
                LearningStage = entity.LearningStage,
                LastReviewDate = entity.LastReviewDate,
                ReviewCount = entity.ReviewCount,
                CorrectStreak = entity.CorrectStreak,
                AlgorithmType = entity.AlgorithmType,
                Category = entity.Category,
                Subject = entity.Subject
            };
        }

        public static void UpdateFromModel(this SpacedRepetitionItemEntity entity, ReviewItem item)
        {
            entity.Content = item.Content;
            entity.Answer = item.Answer;
            entity.Interval = item.Interval;
            entity.Repetitions = item.Repetitions;
            entity.EFactor = item.EFactor;
            entity.NextReviewDate = item.NextReviewDate;
            entity.UpdatedAt = item.UpdatedAt;
            entity.WrongCount = item.WrongCount;
            entity.CorrectCount = item.CorrectCount;
            entity.IsActive = item.IsActive;
            entity.Stability = item.Stability;
            entity.Difficulty = item.Difficulty;
            entity.Retrievability = item.Retrievability;
            entity.LearningStage = item.LearningStage;
            entity.LastReviewDate = item.LastReviewDate;
            entity.ReviewCount = item.ReviewCount;
            entity.CorrectStreak = item.CorrectStreak;
            entity.AlgorithmType = item.AlgorithmType;
            entity.Category = item.Category;
            entity.Subject = item.Subject;
        }
    }
}
