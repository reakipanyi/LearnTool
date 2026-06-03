using System;
using System.Collections.Generic;
using System.Linq;
using UnifiedLearningAssistant.Common;
using UnifiedLearningAssistant.Models.User;
using UnifiedLearningAssistant.Services.Learning;

namespace UnifiedLearningAssistant.Data.Database
{
    public static class DbModelConverter
    {
        // UserProfile 转换
        public static UserProfileEntity ToEntity(this UserProfile profile)
        {
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

        public static UserProfile ToModel(this UserProfileEntity entity)
        {
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
                profile.LearningProgress.CategoryProgresses[catEntity.CategoryName] = catEntity.ToModel();
            }

            // 计算总进度
            profile.LearningProgress.TotalItemsStudied = entity.CategoryProgresses?.Sum(c => c.TotalTestCount) ?? 0;
            profile.LearningProgress.TotalItemsMastered = entity.CategoryProgresses?.Sum(c => c.CorrectCount) ?? 0;

            return profile;
        }

        // CategoryProgress 转换
        public static CategoryProgressEntity ToEntity(this CategoryProgress progress, string userId)
        {
            return new CategoryProgressEntity
            {
                UserId = userId,
                CategoryName = progress.CategoryName,
                KnownItemsJson = JsonHelper.Serialize(progress.KnownItems),
                UnknownItemsJson = JsonHelper.Serialize(progress.UnknownItems),
                TotalTestCount = progress.TotalTestCount,
                CorrectCount = progress.CorrectCount,
                LastTestDate = progress.LastTestDate,
                LastResumeIndex = progress.LastResumeIndex,
                QuickTestResumeIndex = progress.QuickTestResumeIndex,
                LastStudyMode = progress.LastStudyMode
            };
        }

        public static CategoryProgress ToModel(this CategoryProgressEntity entity)
        {
            return new CategoryProgress
            {
                CategoryName = entity.CategoryName,
                KnownItems = JsonHelper.Deserialize<List<string>>(entity.KnownItemsJson) ?? new List<string>(),
                UnknownItems = JsonHelper.Deserialize<List<string>>(entity.UnknownItemsJson) ?? new List<string>(),
                TotalTestCount = entity.TotalTestCount,
                CorrectCount = entity.CorrectCount,
                LastTestDate = entity.LastTestDate,
                LastResumeIndex = entity.LastResumeIndex,
                QuickTestResumeIndex = entity.QuickTestResumeIndex,
                LastStudyMode = entity.LastStudyMode
            };
        }

        // Reminder 转换
        public static ReminderEntity ToEntity(this Reminder reminder)
        {
            return new ReminderEntity
            {
                Id = reminder.Id,
                UserId = reminder.UserId,
                Title = reminder.Title,
                Description = reminder.Description,
                Time = reminder.Time,
                RepeatType = reminder.RepeatType.ToString(),
                RepeatDaysJson = reminder.RepeatDays != null ? JsonHelper.Serialize(reminder.RepeatDays) : null,
                Enabled = reminder.Enabled,
                LastTriggered = reminder.LastTriggered,
                CreatedAt = reminder.CreatedAt
            };
        }

        public static Reminder ToModel(this ReminderEntity entity)
        {
            var repeatType = Enum.TryParse<ReminderRepeatType>(entity.RepeatType, out var rt) 
                ? rt 
                : ReminderRepeatType.None;
            
            var repeatDays = !string.IsNullOrEmpty(entity.RepeatDaysJson) 
                ? JsonHelper.Deserialize<List<DayOfWeek>>(entity.RepeatDaysJson) 
                : null;

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
