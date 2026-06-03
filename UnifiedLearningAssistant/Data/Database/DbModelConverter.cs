using System;
using System.Collections.Generic;
using System.Linq;
using UnifiedLearningAssistant.Common;
using UnifiedLearningAssistant.Models.User;
using UnifiedLearningAssistant.Services.Learning;

namespace UnifiedLearningAssistant.Data.Database
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
                TodayItemsStudied = profile.TodayItemsStudied
            };
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
                TodayItemsStudied = entity.TodayItemsStudied
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
            profile.LearningProgress.TotalItemsStudied = entity.CategoryProgresses?.Sum(c => c.TotalTestCount) ?? 0;
            profile.LearningProgress.TotalItemsMastered = entity.CategoryProgresses?.Sum(c => c.CorrectCount) ?? 0;

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
                Title = reminder.Title,
                Description = reminder.Description,
                Time = reminder.Time,
                RepeatType = reminder.RepeatType.ToString(),
                RepeatDaysJson = repeatDaysJson,
                Enabled = reminder.Enabled,
                LastTriggered = reminder.LastTriggered,
                CreatedAt = reminder.CreatedAt
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
                Title = entity.Title,
                Description = entity.Description,
                Time = entity.Time,
                RepeatType = repeatType,
                RepeatDays = repeatDays,
                Enabled = entity.Enabled,
                LastTriggered = entity.LastTriggered,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
