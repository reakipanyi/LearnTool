using LearningAssistant.Common;
using System.Text.Json.Serialization;

namespace LearningAssistant.Models.Learning
{
    public enum MasteryLevel
    {
        NotMastered = 0,
        Fuzzy = 1,
        Mastered = 2
    }

    public class WrongAnswerFilter
    {
        public SubjectType? Subject { get; set; }
        public SubCategoryType? Category { get; set; }
        public MasteryLevel? Mastery { get; set; }
        public string? Keyword { get; set; }
        public int? MinWrongCount { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<string>? Tags { get; set; }
        public int? Skip { get; set; }
        public int? Take { get; set; }
    }

    public class WrongAnswerStats
    {
        public int TotalCount { get; set; }
        public int NotMasteredCount { get; set; }
        public int FuzzyCount { get; set; }
        public int MasteredCount { get; set; }
        public int TotalWrongCount { get; set; }
        public Dictionary<SubjectType, int> SubjectStats { get; set; } = new();
        public List<WrongAnswerItem> TopWrongItems { get; set; } = new();
        public Dictionary<string, int> TagStats { get; set; } = new();

        [JsonIgnore]
        public double MasteryRate => TotalCount > 0
            ? (double)MasteredCount / TotalCount * 100
            : 0;
    }

    public class WrongAnswerItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SubjectType Subject { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SubCategoryType Category { get; set; }

        /// <summary>
        /// 题目内容
        /// </summary>
        public string Question { get; set; } = string.Empty;

        /// <summary>
        /// 正确答案
        /// </summary>
        public string CorrectAnswer { get; set; } = string.Empty;

        /// <summary>
        /// 用户答案
        /// </summary>
        public string UserAnswer { get; set; } = string.Empty;

        /// <summary>
        /// 解析/解释
        /// </summary>
        public string Explanation { get; set; } = string.Empty;

        /// <summary>
        /// 添加时间（首次错误时间）
        /// </summary>
        public DateTime AddedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后复习时间
        /// </summary>
        public DateTime? LastReviewAt { get; set; }

        /// <summary>
        /// 复习次数
        /// </summary>
        public int ReviewCount { get; set; } = 0;

        /// <summary>
        /// 错误次数
        /// </summary>
        public int WrongCount { get; set; } = 1;

        /// <summary>
        /// 正确次数
        /// </summary>
        public int CorrectCount { get; set; } = 0;

        /// <summary>
        /// 难度系数（0-1，越高越难）
        /// </summary>
        public double Difficulty { get; set; } = 0.5;

        /// <summary>
        /// 掌握程度
        /// </summary>
        public MasteryLevel Mastery { get; set; } = MasteryLevel.NotMastered;

        /// <summary>
        /// 标签列表（知识点标签）
        /// </summary>
        public List<string> TagsList { get; set; } = new();

        /// <summary>
        /// 下次复习时间
        /// </summary>
        public DateTime? NextReviewAt { get; set; }

        /// <summary>
        /// 首次错误时间
        /// </summary>
        public DateTime FirstWrongAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后错误时间
        /// </summary>
        public DateTime LastWrongAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否启用（软删除）
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 备注信息
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// 标签字符串（兼容旧版本）
        /// </summary>
        [JsonIgnore]
        public string Tags
        {
            get => string.Join(",", TagsList);
            set
            {
                TagsList = string.IsNullOrWhiteSpace(value)
                    ? new List<string>()
                    : value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            }
        }

        /// <summary>
        /// 是否已掌握（兼容旧版本）
        /// </summary>
        [JsonIgnore]
        public bool IsMastered
        {
            get => Mastery == MasteryLevel.Mastered;
            set => Mastery = value ? MasteryLevel.Mastered : MasteryLevel.NotMastered;
        }

        /// <summary>
        /// 显示标题（题目前50字）
        /// </summary>
        [JsonIgnore]
        public string DisplayTitle => Question.Length > 50
            ? Question.Substring(0, 50) + "..."
            : Question;

        /// <summary>
        /// 掌握程度显示文本
        /// </summary>
        [JsonIgnore]
        public string MasteryText => Mastery switch
        {
            MasteryLevel.NotMastered => "未掌握",
            MasteryLevel.Fuzzy => "模糊",
            MasteryLevel.Mastered => "已掌握",
            _ => "未知"
        };

        /// <summary>
        /// 掌握程度图标
        /// </summary>
        [JsonIgnore]
        public string MasteryIcon => Mastery switch
        {
            MasteryLevel.NotMastered => "❌",
            MasteryLevel.Fuzzy => "🟡",
            MasteryLevel.Mastered => "✅",
            _ => "❓"
        };
    }
}
