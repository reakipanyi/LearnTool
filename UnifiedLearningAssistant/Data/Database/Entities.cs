using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UnifiedLearningAssistant.Data.Database
{
    public class UserProfileEntity
    {
        [Key]
        public string UserId { get; set; } = string.Empty;
        
        public string UserName { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime LastLoginTime { get; set; } = DateTime.Now;
        
        public string AvatarPath { get; set; } = string.Empty;
        
        public int ConsecutiveStudyDays { get; set; }
        
        public DateTime? LastStudyDate { get; set; }
        
        public int TotalStudyTimeMinutes { get; set; }
        
        public int TodayStudyTimeMinutes { get; set; }
        
        public int TodayItemsStudied { get; set; }
        
        // 导航属性
        public List<CategoryProgressEntity> CategoryProgresses { get; set; } = new List<CategoryProgressEntity>();
        public List<LearningRecordEntity> LearningRecords { get; set; } = new List<LearningRecordEntity>();
        public List<ReminderEntity> Reminders { get; set; } = new List<ReminderEntity>();
    }

    public class CategoryProgressEntity
    {
        [Key]
        public string UserId { get; set; } = string.Empty;
        
        [Key]
        public string CategoryName { get; set; } = string.Empty;
        
        // 存储为 JSON 字符串
        public string KnownItemsJson { get; set; } = "[]";
        
        public string UnknownItemsJson { get; set; } = "[]";
        
        public int TotalTestCount { get; set; } = 0;
        
        public int CorrectCount { get; set; } = 0;
        
        public DateTime LastTestDate { get; set; } = DateTime.MinValue;
        
        public int LastResumeIndex { get; set; } = 0;
        
        public int QuickTestResumeIndex { get; set; } = 0;
        
        public string LastStudyMode { get; set; } = string.Empty;
        
        // 导航属性
        public UserProfileEntity? UserProfile { get; set; }
    }

    public class LearningRecordEntity
    {
        [Key]
        public int Id { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        
        public string ActivityType { get; set; } = string.Empty;
        
        public string SubCategory { get; set; } = string.Empty;
        
        public int Count { get; set; } = 1;
        
        public DateTime RecordDate { get; set; } = DateTime.Now;
        
        // 导航属性
        public UserProfileEntity? UserProfile { get; set; }
    }

    public class ReminderEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public string UserId { get; set; } = string.Empty;
        
        public string Title { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public TimeSpan Time { get; set; }
        
        public string RepeatType { get; set; } = string.Empty;
        
        public string? RepeatDaysJson { get; set; }
        
        public bool Enabled { get; set; } = true;
        
        public DateTime? LastTriggered { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // 导航属性
        public UserProfileEntity? UserProfile { get; set; }
    }
}
