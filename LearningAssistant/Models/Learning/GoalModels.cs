using System.Text.Json.Serialization;

namespace LearningAssistant.Models.Learning
{
    /// <summary>
    /// 目标类型
    /// </summary>
    public enum GoalType
    {
        /// <summary>
        /// 每日学习时长（分钟）
        /// </summary>
        DailyStudyMinutes,

        /// <summary>
        /// 每日学习数量（个）
        /// </summary>
        DailyStudyItems,

        /// <summary>
        /// 每日复习数量（个）
        /// </summary>
        DailyReviewItems,

        /// <summary>
        /// 每周学习天数
        /// </summary>
        WeeklyStudyDays
    }

    /// <summary>
    /// 学习目标设置
    /// </summary>
    public class LearningGoal
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 目标类型
        /// </summary>
        public GoalType Type { get; set; }

        /// <summary>
        /// 目标值
        /// </summary>
        public int TargetValue { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        [JsonIgnore]
        public string DisplayName => GetDisplayName(Type);

        /// <summary>
        /// 目标描述
        /// </summary>
        [JsonIgnore]
        public string Description => GetDescription(Type, TargetValue);

        private static string GetDisplayName(GoalType type) => type switch
        {
            GoalType.DailyStudyMinutes => "每日学习时长",
            GoalType.DailyStudyItems => "每日学习数量",
            GoalType.DailyReviewItems => "每日复习数量",
            GoalType.WeeklyStudyDays => "每周学习天数",
            _ => "学习目标"
        };

        private static string GetDescription(GoalType type, int value) => type switch
        {
            GoalType.DailyStudyMinutes => $"每天学习 {value} 分钟",
            GoalType.DailyStudyItems => $"每天学习 {value} 个内容",
            GoalType.DailyReviewItems => $"每天复习 {value} 个内容",
            GoalType.WeeklyStudyDays => $"每周学习 {value} 天",
            _ => $"目标: {value}"
        };
    }

    /// <summary>
    /// 每日目标完成记录
    /// </summary>
    public class DailyGoalRecord
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 各目标进度
        /// </summary>
        public Dictionary<GoalType, int> Progress { get; set; } = new();

        /// <summary>
        /// 各目标完成状态
        /// </summary>
        public Dictionary<GoalType, bool> Completed { get; set; } = new();

        /// <summary>
        /// 是否全部完成
        /// </summary>
        public bool AllCompleted { get; set; }

        /// <summary>
        /// 完成的目标数
        /// </summary>
        [JsonIgnore]
        public int CompletedCount => Completed.Count(c => c.Value);

        /// <summary>
        /// 总目标数
        /// </summary>
        [JsonIgnore]
        public int TotalCount => Completed.Count;
    }

    /// <summary>
    /// 目标进度信息
    /// </summary>
    public class GoalProgress
    {
        /// <summary>
        /// 目标类型
        /// </summary>
        public GoalType Type { get; set; }

        /// <summary>
        /// 目标设置
        /// </summary>
        public LearningGoal Goal { get; set; } = new();

        /// <summary>
        /// 当前进度值
        /// </summary>
        public int CurrentValue { get; set; }

        /// <summary>
        /// 目标值
        /// </summary>
        public int TargetValue => Goal.TargetValue;

        /// <summary>
        /// 进度百分比
        /// </summary>
        [JsonIgnore]
        public double Percentage => TargetValue > 0
            ? Math.Min(100.0, CurrentValue * 100.0 / TargetValue)
            : 0;

        /// <summary>
        /// 是否完成
        /// </summary>
        [JsonIgnore]
        public bool IsCompleted => CurrentValue >= TargetValue;

        /// <summary>
        /// 剩余数量
        /// </summary>
        [JsonIgnore]
        public int Remaining => Math.Max(0, TargetValue - CurrentValue);
    }

    /// <summary>
    /// 连续达成统计
    /// </summary>
    public class StreakInfo
    {
        /// <summary>
        /// 当前连续达成天数
        /// </summary>
        public int CurrentStreak { get; set; }

        /// <summary>
        /// 最长连续达成天数
        /// </summary>
        public int LongestStreak { get; set; }

        /// <summary>
        /// 总达成天数
        /// </summary>
        public int TotalCompletedDays { get; set; }

        /// <summary>
        /// 总记录天数
        /// </summary>
        public int TotalRecordedDays { get; set; }

        /// <summary>
        /// 达成率
        /// </summary>
        [JsonIgnore]
        public double CompletionRate => TotalRecordedDays > 0
            ? (double)TotalCompletedDays / TotalRecordedDays * 100
            : 0;
    }
}
