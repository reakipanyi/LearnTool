using System.Text.Json.Serialization;

namespace LearningAssistant.Models.Learning
{
    /// <summary>
    /// 掌握程度
    /// </summary>
    public enum MasteryLevel
    {
        /// <summary>
        /// 未掌握
        /// </summary>
        NotMastered = 0,

        /// <summary>
        /// 模糊/不确定
        /// </summary>
        Fuzzy = 1,

        /// <summary>
        /// 已掌握
        /// </summary>
        Mastered = 2
    }

    /// <summary>
    /// 错题筛选条件
    /// </summary>
    public class WrongAnswerFilter
    {
        /// <summary>
        /// 学科
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// 子类别
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// 掌握程度
        /// </summary>
        public MasteryLevel? Mastery { get; set; }

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 最少错误次数
        /// </summary>
        public int? MinWrongCount { get; set; }

        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// 标签
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>
        /// 跳过的记录数（分页用）
        /// </summary>
        public int? Skip { get; set; }

        /// <summary>
        /// 取的记录数（分页用）
        /// </summary>
        public int? Take { get; set; }
    }

    /// <summary>
    /// 错题统计信息
    /// </summary>
    public class WrongAnswerStats
    {
        /// <summary>
        /// 总错题数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 未掌握数
        /// </summary>
        public int NotMasteredCount { get; set; }

        /// <summary>
        /// 模糊数
        /// </summary>
        public int FuzzyCount { get; set; }

        /// <summary>
        /// 已掌握数
        /// </summary>
        public int MasteredCount { get; set; }

        /// <summary>
        /// 总错误次数
        /// </summary>
        public int TotalWrongCount { get; set; }

        /// <summary>
        /// 学科统计
        /// </summary>
        public Dictionary<string, int> SubjectStats { get; set; } = new();

        /// <summary>
        /// 错误次数 Top 10
        /// </summary>
        public List<WrongAnswerItem> TopWrongItems { get; set; } = new();

        /// <summary>
        /// 标签统计
        /// </summary>
        public Dictionary<string, int> TagStats { get; set; } = new();

        /// <summary>
        /// 掌握率
        /// </summary>
        [JsonIgnore]
        public double MasteryRate => TotalCount > 0
            ? (double)MasteredCount / TotalCount * 100
            : 0;
    }

    /// <summary>
    /// 错题记录
    /// </summary>
    public class WrongAnswerItem
    {
        /// <summary>
        /// ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 学科
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// 分类/子类别
        /// </summary>
        public string Category { get; set; } = string.Empty;

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
