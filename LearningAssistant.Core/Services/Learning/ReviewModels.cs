namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 复习项模型 - 包含SM-2算法所需的所有参数
    /// 从 SqliteSpacedRepetitionService 拆出以消除 Core 反向依赖
    /// </summary>
    public class ReviewItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Hint { get; set; } = string.Empty;
        public int Interval { get; set; } = 0;
        public int Repetitions { get; set; } = 0;
        public double EFactor { get; set; } = 2.5;
        public DateTime NextReviewDate { get; set; } = DateTime.Now;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public int WrongCount { get; set; } = 0;
        public int CorrectCount { get; set; } = 0;
        public int CorrectStreak { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public double Stability { get; set; } = 0;
        public double Difficulty { get; set; } = 5;
        public double Retrievability { get; set; } = 1;
        public int LearningStage { get; set; } = 0;
        public DateTime? LastReviewDate { get; set; }
        public int ReviewCount { get; set; } = 0;

        public string Question
        {
            get => Content;
            set => Content = value;
        }

        public string? AlgorithmType { get; set; }

        public string? Category { get; set; }

        public string? Subject { get; set; }
    }

    /// <summary>
    /// 复习结果 - 计算后的间隔和难度因子
    /// </summary>
    public class ReviewResult
    {
        public bool ShouldReview { get; set; }
        public int NewInterval { get; set; }
        public int NewRepetitions { get; set; }
        public double NewEFactor { get; set; }
        public DateTime NextReviewDate { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Duration { get; set; }
    }

    public class ReviewLog
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public Guid ContentId { get; set; }
        public int Rating { get; set; }
        public int Interval { get; set; }
        public double? EaseFactor { get; set; }
        public double? Stability { get; set; }
        public double? Difficulty { get; set; }
        public DateTime ReviewTime { get; set; } = DateTime.Now;
        public int Duration { get; set; }
        public string? AlgorithmType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
