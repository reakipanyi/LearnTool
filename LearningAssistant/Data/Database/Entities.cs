using LearningAssistant.Models.User;
using System.ComponentModel.DataAnnotations;

namespace LearningAssistant.Data.Database
{
    /// <summary>
    /// 用户配置实体
    /// </summary>
    public class UserProfileEntity : UserEntityBase
    {
        [Required(ErrorMessage = "用户名不能为空")]
        [MaxLength(50, ErrorMessage = "用户名长度不能超过 50 个字符")]
        public string UserName { get; set; } = string.Empty;

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

        public int XP { get; set; }

        public int TotalXP { get; set; }

        public int Level { get; set; } = 1;

        public int Coins { get; set; }

        public int TotalItemsStudied { get; set; }

        public int StudyDays { get; set; }

        public int LongestStreak { get; set; }

        // 导航属性
        public List<CategoryProgressEntity> CategoryProgresses { get; set; } = new List<CategoryProgressEntity>();
        public List<LearningRecordEntity> LearningRecords { get; set; } = new List<LearningRecordEntity>();
        public List<ReminderEntity> Reminders { get; set; } = new List<ReminderEntity>();
    }

    /// <summary>
    /// 分类进度实体
    /// </summary>
    public class CategoryProgressEntity : UserEntityBase
    {
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
    public class LearningRecordEntity : UserEntityBase
    {
        [Key]
        public int Id { get; set; }

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
    public class ReminderEntity : UserEntityBase
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "提醒类型不能为空")]
        [MaxLength(50, ErrorMessage = "提醒类型长度不能超过 50 个字符")]
        public string Type { get; set; } = "Study";

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

        public int TriggerCount { get; set; }

        public int OpenCount { get; set; }

        public int SnoozeCount { get; set; }

        public int DismissCount { get; set; }

        // 导航属性
        public UserProfileEntity? UserProfile { get; set; }
    }

    /// <summary>
    /// 间隔重复复习项实体
    /// </summary>
    public class SpacedRepetitionItemEntity : UserEntityBase
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public string Answer { get; set; } = string.Empty;

        public int Interval { get; set; } = 0;

        public int Repetitions { get; set; } = 0;

        public double EFactor { get; set; } = 2.5;

        public DateTime NextReviewDate { get; set; } = DateTime.Now;

        public int WrongCount { get; set; } = 0;

        public int CorrectCount { get; set; } = 0;

        public double Stability { get; set; } = 0;

        public double Difficulty { get; set; } = 5;

        public double Retrievability { get; set; } = 1;

        public int LearningStage { get; set; } = 0;

        public DateTime? LastReviewDate { get; set; }

        public int ReviewCount { get; set; } = 0;

        public int CorrectStreak { get; set; } = 0;

        [MaxLength(20)]
        public string? AlgorithmType { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(50)]
        public string? Subject { get; set; }

        public List<ReviewLogEntity> ReviewLogs { get; set; } = new List<ReviewLogEntity>();
    }

    /// <summary>
    /// 复习日志实体 - 记录每次复习详情，用于 FSRS 机器学习
    /// </summary>
    public class ReviewLogEntity : UserEntityBase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid ContentId { get; set; }

        [Required]
        public int Rating { get; set; }

        public int Interval { get; set; }

        public double? EaseFactor { get; set; }

        public double? Stability { get; set; }

        public double? Difficulty { get; set; }

        [Required]
        public DateTime ReviewTime { get; set; } = DateTime.Now;

        public int Duration { get; set; }

        [MaxLength(20)]
        public string? AlgorithmType { get; set; }

        public SpacedRepetitionItemEntity? SpacedRepetitionItem { get; set; }
    }

    /// <summary>
    /// 学习项状态实体 - 替代 CategoryProgress 中的 JSON 存储
    /// </summary>
    public class LearningItemStateEntity : UserEntityBase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public bool IsKnown { get; set; } = false;
    }

    /// <summary>
    /// 学习项实体 - 持久化 LearningItem 领域模型
    /// </summary>
    public class LearningItemEntity : AuditableEntityBase
    {
        [Key]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string SubCategory { get; set; } = string.Empty;

        [Required]
        public string MainContent { get; set; } = string.Empty;

        public string? MeaningJson { get; set; }

        public string? ExampleJson { get; set; }

        public string? PronunciationJson { get; set; }

        public string? CharacterFeaturesJson { get; set; }

        public string? WordFeaturesJson { get; set; }

        public string ExtendedProperties { get; set; } = "{}";

        [MaxLength(20)]
        public string Status { get; set; } = "New";

        public int ReviewCount { get; set; }

        public DateTime? LastReviewedAt { get; set; }
    }

    /// <summary>
    /// 提醒重复日期实体 - 替代 Reminder 中的 RepeatDaysJson
    /// </summary>
    public class ReminderRepeatDayEntity : AuditableEntityBase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid ReminderId { get; set; }

        public int DayOfWeek { get; set; }

        public ReminderEntity? Reminder { get; set; }
    }

    /// <summary>
    /// 应用会话状态实体
    /// </summary>
    public class AppSessionEntity : AuditableEntityBase
    {
        [Key]
        [MaxLength(100)]
        public string SessionKey { get; set; } = string.Empty;

        [Required]
        public string SessionDataJson { get; set; } = string.Empty;

        public DateTime LastAccessTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 徽章解锁记录实体
    /// </summary>
    public class BadgeUnlockEntity : UserEntityBase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string BadgeId { get; set; } = string.Empty;

        public DateTime UnlockedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 学习统计实体
    /// </summary>
    public class StudyStatsEntity : UserEntityBase
    {
        [Key]
        public int Id { get; set; }

        public int TodayLearnedCount { get; set; }

        public int StreakDays { get; set; }

        public int TotalScore { get; set; }

        public int TotalLearnedCount { get; set; }

        public int XP { get; set; }

        public DateTime LastStudyDate { get; set; } = DateTime.MinValue;
    }

    /// <summary>
    /// 每日挑战实体
    /// </summary>
    public class DailyChallengeEntity : UserEntityBase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Date { get; set; } = string.Empty;

        [Required]
        public string ChallengesJson { get; set; } = "[]";
    }

    /// <summary>
    /// 挑战历史记录实体
    /// </summary>
    public class ChallengeHistoryEntity : UserEntityBase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Date { get; set; } = string.Empty;

        public int CompletedCount { get; set; }

        public int TotalCount { get; set; }

        public int ClaimedCount { get; set; }

        public int TotalXP { get; set; }

        public string ChallengesJson { get; set; } = "[]";
    }

    /// <summary>
    /// 学习目标设置实体
    /// </summary>
    public class LearningGoalEntity : UserEntityBase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string GoalType { get; set; } = string.Empty;

        public int TargetValue { get; set; }

        [MaxLength(20)]
        public string? Unit { get; set; }

        public bool Enabled { get; set; }
    }

    /// <summary>
    /// 每日目标记录实体
    /// </summary>
    public class DailyGoalRecordEntity : UserEntityBase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public string ProgressJson { get; set; } = "{}";

        public string CompletedJson { get; set; } = "{}";

        public bool AllCompleted { get; set; }
    }

    /// <summary>
    /// 收藏夹文件夹实体
    /// </summary>
    public class FavoriteFolderEntity : UserEntityBase
    {
        [Key]
        [MaxLength(100)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ParentId { get; set; }

        public int OrderIndex { get; set; }

        [MaxLength(50)]
        public string? Icon { get; set; }
    }

    /// <summary>
    /// 收藏项实体
    /// </summary>
    public class FavoriteItemEntity : UserEntityBase
    {
        [Key]
        [MaxLength(100)]
        public string Id { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? FolderId { get; set; }

        [MaxLength(50)]
        public string ItemType { get; set; } = "Text";

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public string Content { get; set; } = string.Empty;

        public string? Answer { get; set; }

        [MaxLength(200)]
        public string? Subject { get; set; }

        [MaxLength(200)]
        public string? SubCategory { get; set; }

        public string? ExtraData { get; set; }

        [MaxLength(200)]
        public string? Tags { get; set; }

        public bool IsPinned { get; set; }

        public int OrderIndex { get; set; }
    }

    /// <summary>
    /// 番茄钟设置实体
    /// </summary>
    public class PomodoroSettingsEntity : UserEntityBase
    {
        [Key]
        public int Id { get; set; }

        public int StudyMinutes { get; set; } = 25;

        public int ShortBreakMinutes { get; set; } = 5;

        public int LongBreakMinutes { get; set; } = 15;

        public int LongBreakInterval { get; set; } = 4;

        public bool AutoStartBreak { get; set; }

        public bool AutoStartStudy { get; set; }

        public bool PlaySound { get; set; } = true;

        public bool ShowNotification { get; set; } = true;
    }

    /// <summary>
    /// 番茄钟记录实体
    /// </summary>
    public class PomodoroRecordEntity : UserEntityBase
    {
        [Key]
        [MaxLength(100)]
        public string Id { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        public int DurationSeconds { get; set; }

        public int PlannedDurationSeconds { get; set; }

        public bool Completed { get; set; }

        [MaxLength(500)]
        public string? Task { get; set; }

        public int InterruptionCount { get; set; }
    }

    /// <summary>
    /// 错题本实体
    /// </summary>
    public class WrongAnswerEntity : UserEntityBase
    {
        [Key]
        [MaxLength(100)]
        public string Id { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Question { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string CorrectAnswer { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string UserAnswer { get; set; } = string.Empty;

        public string Explanation { get; set; } = string.Empty;

        public DateTime AddedAt { get; set; } = DateTime.Now;

        public DateTime? LastReviewAt { get; set; }

        public int ReviewCount { get; set; }

        public int WrongCount { get; set; } = 1;

        public int CorrectCount { get; set; }

        public double Difficulty { get; set; } = 0.5;

        public int MasteryLevel { get; set; }

        [MaxLength(200)]
        public string? Tags { get; set; }

        public DateTime? NextReviewAt { get; set; }

        public DateTime FirstWrongAt { get; set; } = DateTime.Now;

        public DateTime LastWrongAt { get; set; } = DateTime.Now;

        public string Notes { get; set; } = string.Empty;
    }
}
