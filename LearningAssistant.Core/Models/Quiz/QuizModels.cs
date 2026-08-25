namespace LearningAssistant.Models.Quiz
{
    /// <summary>
    /// 题目类型
    /// </summary>
    public enum QuestionType
    {
        /// <summary>
        /// 单选题
        /// </summary>
        SingleChoice,

        /// <summary>
        /// 多选题
        /// </summary>
        MultipleChoice,

        /// <summary>
        /// 判断题
        /// </summary>
        TrueFalse,

        /// <summary>
        /// 填空题
        /// </summary>
        FillInBlank,

        /// <summary>
        /// 简答题
        /// </summary>
        ShortAnswer
    }

    /// <summary>
    /// 题目难度
    /// </summary>
    public enum QuestionDifficulty
    {
        Easy = 1,
        Medium = 2,
        Hard = 3
    }

    /// <summary>
    /// 测验题目
    /// </summary>
    public class QuizQuestion
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 题目内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 题目类型
        /// </summary>
        public QuestionType Type { get; set; }

        /// <summary>
        /// 难度级别
        /// </summary>
        public QuestionDifficulty Difficulty { get; set; }

        /// <summary>
        /// 选项列表（用于选择/判断题）
        /// </summary>
        public List<string> Options { get; set; } = new();

        /// <summary>
        /// 正确答案索引（用于选择/判断题）
        /// </summary>
        public List<int> CorrectOptionIndices { get; set; } = new();

        /// <summary>
        /// 正确答案文本（用于填空/简答题）
        /// </summary>
        public string CorrectAnswer { get; set; } = string.Empty;

        /// <summary>
        /// 解析说明
        /// </summary>
        public string Explanation { get; set; } = string.Empty;

        /// <summary>
        /// 相关知识点
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// 所属学习内容
        /// </summary>
        public string SourceContent { get; set; } = string.Empty;

        /// <summary>
        /// 是否已作答
        /// </summary>
        public bool IsAnswered { get; set; }

        /// <summary>
        /// 用户选择的答案
        /// </summary>
        public List<int> UserSelectedIndices { get; set; } = new();

        /// <summary>
        /// 用户的文本回答
        /// </summary>
        public string UserTextAnswer { get; set; } = string.Empty;

        /// <summary>
        /// 是否正确
        /// </summary>
        public bool IsCorrect
        {
            get
            {
                if (Type == QuestionType.ShortAnswer || Type == QuestionType.FillInBlank)
                {
                    return IsTextAnswerCorrect();
                }

                if (UserSelectedIndices.Count != CorrectOptionIndices.Count)
                    return false;

                return UserSelectedIndices.All(CorrectOptionIndices.Contains);
            }
        }

        /// <summary>
        /// 检查文本答案是否正确
        /// </summary>
        private bool IsTextAnswerCorrect()
        {
            if (string.IsNullOrWhiteSpace(UserTextAnswer) || string.IsNullOrWhiteSpace(CorrectAnswer))
                return false;

            return UserTextAnswer.Trim().Equals(CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 获取正确选项文本
        /// </summary>
        public string GetCorrectOptionsText()
        {
            if (Type == QuestionType.ShortAnswer || Type == QuestionType.FillInBlank)
                return CorrectAnswer;

            return string.Join(", ", CorrectOptionIndices.Select(i =>
                i < Options.Count ? Options[i] : ""));
        }
    }

    /// <summary>
    /// 测验会话
    /// </summary>
    public class QuizSession
    {
        public Guid SessionId { get; set; } = Guid.NewGuid();

        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 测验标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 题目列表
        /// </summary>
        public List<QuizQuestion> Questions { get; set; } = new();

        /// <summary>
        /// 当前题目索引
        /// </summary>
        public int CurrentIndex { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 是否完成
        /// </summary>
        public bool IsCompleted => EndTime.HasValue;

        /// <summary>
        /// 答题限时（分钟）
        /// </summary>
        public int TimeLimitMinutes { get; set; }

        /// <summary>
        /// 总题数
        /// </summary>
        public int TotalCount => Questions.Count;

        /// <summary>
        /// 已答题数
        /// </summary>
        public int AnsweredCount => Questions.Count(q => q.IsAnswered);

        /// <summary>
        /// 正确题数
        /// </summary>
        public int CorrectCount => Questions.Count(q => q.IsAnswered && q.IsCorrect);

        /// <summary>
        /// 正确率
        /// </summary>
        public double AccuracyRate => AnsweredCount > 0 ? (double)CorrectCount / AnsweredCount * 100 : 0;

        /// <summary>
        /// 用时（秒）
        /// </summary>
        public int ElapsedSeconds => (int)((EndTime ?? DateTime.Now) - StartTime).TotalSeconds;

        /// <summary>
        /// 获取当前题目
        /// </summary>
        public QuizQuestion? CurrentQuestion =>
            CurrentIndex < Questions.Count ? Questions[CurrentIndex] : null;

        /// <summary>
        /// 移动到下一题
        /// </summary>
        public bool MoveNext()
        {
            if (CurrentIndex < Questions.Count - 1)
            {
                CurrentIndex++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 移动到上一题
        /// </summary>
        public bool MovePrevious()
        {
            if (CurrentIndex > 0)
            {
                CurrentIndex--;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 完成测验
        /// </summary>
        public void Complete()
        {
            EndTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 测验结果
    /// </summary>
    public class QuizResult
    {
        public Guid SessionId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public double AccuracyRate { get; set; }
        public int TimeSpentSeconds { get; set; }
        public DateTime CompletedAt { get; set; }

        /// <summary>
        /// 错题列表
        /// </summary>
        public List<QuizQuestion> WrongQuestions { get; set; } = new();

        /// <summary>
        /// 薄弱知识点
        /// </summary>
        public List<string> WeakTags { get; set; } = new();

        /// <summary>
        /// 成绩等级
        /// </summary>
        public string Grade
        {
            get
            {
                if (AccuracyRate >= 90) return "A+";
                if (AccuracyRate >= 85) return "A";
                if (AccuracyRate >= 80) return "B+";
                if (AccuracyRate >= 75) return "B";
                if (AccuracyRate >= 70) return "C+";
                if (AccuracyRate >= 60) return "C";
                return "D";
            }
        }
    }
}
