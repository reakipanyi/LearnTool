using LearningAssistant.Models.User;
using System.ComponentModel.DataAnnotations;

namespace LearningAssistant.Data.Database
{
    /// <summary>
    /// 用户配置实体
    /// </summary>
    public class UserProfileEntity
    {
        [Key]
        [Required(ErrorMessage = "用户 ID 不能为空")]
        [MaxLength(100, ErrorMessage = "用户 ID 长度不能超过 100 个字符")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "用户名不能为空")]
        [MaxLength(50, ErrorMessage = "用户名长度不能超过 50 个字符")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public DateTime LastLoginTime { get; set; } = DateTime.Now;

        [MaxLength(500, ErrorMessage = "头像路径长度不能超过 500 个字符")]
        public string AvatarPath { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "连续学习天数不能为负数")]
        public int ConsecutiveStudyDays { get; set; }

        public DateTime? LastStudyDate { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "总学习时间不能为负数")]
        public int TotalStudyTimeMinutes { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "今日学习时间不能为负数")]
        public int TodayStudyTimeMinutes { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "今日学习项目数不能为负数")]
        public int TodayItemsStudied { get; set; }

        // 导航属性
        public List<CategoryProgressEntity> CategoryProgresses { get; set; } = new List<CategoryProgressEntity>();
        public List<LearningRecordEntity> LearningRecords { get; set; } = new List<LearningRecordEntity>();
        public List<ReminderEntity> Reminders { get; set; } = new List<ReminderEntity>();
    }

    /// <summary>
    /// 分类进度实体
    /// </summary>
    public class CategoryProgressEntity
    {
        [Key]
        [Required(ErrorMessage = "用户 ID 不能为空")]
        [MaxLength(100, ErrorMessage = "用户 ID 长度不能超过 100 个字符")]
        public string UserId { get; set; } = string.Empty;

        [Key]
        [Required(ErrorMessage = "分类名称不能为空")]
        [MaxLength(100, ErrorMessage = "分类名称长度不能超过 100 个字符")]
        public string CategoryName { get; set; } = string.Empty;

        [Required]
        public string KnownItemsJson { get; set; } = "[]";

        [Required]
        public string UnknownItemsJson { get; set; } = "[]";

        [Range(0, int.MaxValue, ErrorMessage = "总测试次数不能为负数")]
        public int TotalTestCount { get; set; } = 0;

        [Range(0, int.MaxValue, ErrorMessage = "正确次数不能为负数")]
        public int CorrectCount { get; set; } = 0;

        public DateTime LastTestDate { get; set; } = DateTime.MinValue;

        [Range(0, int.MaxValue, ErrorMessage = "上次继续索引不能为负数")]
        public int LastResumeIndex { get; set; } = 0;

        [Range(0, int.MaxValue, ErrorMessage = "快速测试继续索引不能为负数")]
        public int QuickTestResumeIndex { get; set; } = 0;

        [MaxLength(50, ErrorMessage = "上次学习模式长度不能超过 50 个字符")]
        public string LastStudyMode { get; set; } = string.Empty;

        public UserProfileEntity? UserProfile { get; set; }

        public void UpdateEntity(CategoryProgress progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            var updatedEntity = progress.ToEntity(this.UserId);
            this.KnownItemsJson = updatedEntity.KnownItemsJson;
            this.UnknownItemsJson = updatedEntity.UnknownItemsJson;
            this.TotalTestCount = updatedEntity.TotalTestCount;
            this.CorrectCount = updatedEntity.CorrectCount;
            this.LastTestDate = updatedEntity.LastTestDate;
            this.LastResumeIndex = updatedEntity.LastResumeIndex;
            this.QuickTestResumeIndex = updatedEntity.QuickTestResumeIndex;
            this.LastStudyMode = updatedEntity.LastStudyMode;
        }
    }

    /// <summary>
    /// 学习记录实体
    /// </summary>
    public class LearningRecordEntity
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "用户 ID 不能为空")]
        [MaxLength(100, ErrorMessage = "用户 ID 长度不能超过 100 个字符")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "活动类型不能为空")]
        [MaxLength(50, ErrorMessage = "活动类型长度不能超过 50 个字符")]
        public string ActivityType { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "子分类长度不能超过 100 个字符")]
        public string SubCategory { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "计数必须大于 0")]
        public int Count { get; set; } = 1;

        [Required]
        public DateTime RecordDate { get; set; } = DateTime.Now;

        // 导航属性
        public UserProfileEntity? UserProfile { get; set; }
    }

    /// <summary>
    /// 提醒实体
    /// </summary>
    public class ReminderEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "用户 ID 不能为空")]
        [MaxLength(100, ErrorMessage = "用户 ID 长度不能超过 100 个字符")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "提醒标题不能为空")]
        [MaxLength(200, ErrorMessage = "提醒标题长度不能超过 200 个字符")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "描述长度不能超过 1000 个字符")]
        public string? Description { get; set; }

        [Required]
        public TimeSpan Time { get; set; }

        [Required(ErrorMessage = "重复类型不能为空")]
        [MaxLength(50, ErrorMessage = "重复类型长度不能超过 50 个字符")]
        public string RepeatType { get; set; } = string.Empty;

        public string? RepeatDaysJson { get; set; }

        public bool Enabled { get; set; } = true;

        public DateTime? LastTriggered { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // 导航属性
        public UserProfileEntity? UserProfile { get; set; }
    }

    /// <summary>
    /// 间隔重复复习项实体
    /// </summary>
    public class SpacedRepetitionItemEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public string Answer { get; set; } = string.Empty;

        public int Interval { get; set; } = 0;

        public int Repetitions { get; set; } = 0;

        public double EFactor { get; set; } = 2.5;

        public DateTime NextReviewDate { get; set; } = DateTime.Now;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public int WrongCount { get; set; } = 0;

        public int CorrectCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// 应用会话状态实体
    /// </summary>
    public class AppSessionEntity
    {
        [Key]
        [MaxLength(100)]
        public string SessionKey { get; set; } = string.Empty;

        [Required]
        public string SessionDataJson { get; set; } = string.Empty;

        public DateTime LastAccessTime { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
