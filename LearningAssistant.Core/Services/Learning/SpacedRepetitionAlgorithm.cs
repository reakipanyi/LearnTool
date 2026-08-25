namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 间隔重复算法接口 - 支持多种算法实现（SM-2、FSRS等）
    /// </summary>
    public interface ISpacedRepetitionAlgorithm
    {
        /// <summary>
        /// 算法名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 算法类型标识
        /// </summary>
        string AlgorithmType { get; }

        /// <summary>
        /// 计算下次复习间隔
        /// </summary>
        /// <param name="item">复习项</param>
        /// <param name="rating">评分 (0-5)</param>
        /// <returns>复习结果</returns>
        AlgorithmResult Calculate(ReviewItem item, int rating);

        /// <summary>
        /// 预测指定天数后的保留率
        /// </summary>
        /// <param name="stability">记忆稳定性</param>
        /// <param name="difficulty">难度</param>
        /// <param name="days">天数</param>
        /// <returns>保留率 (0-1)</returns>
        double PredictRetention(double stability, double difficulty, int days);

        /// <summary>
        /// 根据目标保留率计算复习间隔
        /// </summary>
        /// <param name="stability">记忆稳定性</param>
        /// <param name="targetRetention">目标保留率 (0-1)</param>
        /// <returns>建议的复习间隔（天数）</returns>
        int GetOptimalInterval(double stability, double targetRetention = 0.9);

        /// <summary>
        /// 获取算法的推荐保留率
        /// </summary>
        double RecommendedRetention { get; }

        /// <summary>
        /// 获取算法的稳定性权重（用于自适应切换）
        /// </summary>
        double StabilityWeight { get; }

        /// <summary>
        /// 获取算法的精度评分（基于历史数据）
        /// </summary>
        double AccuracyScore { get; set; }
    }

    /// <summary>
    /// 算法计算结果
    /// </summary>
    public class AlgorithmResult
    {
        /// <summary>
        /// 新的间隔（天数）
        /// </summary>
        public int NewInterval { get; set; }

        /// <summary>
        /// 新的重复次数
        /// </summary>
        public int NewRepetitions { get; set; }

        /// <summary>
        /// 新的易度因子
        /// </summary>
        public double NewEFactor { get; set; }

        /// <summary>
        /// 新的稳定性
        /// </summary>
        public double NewStability { get; set; }

        /// <summary>
        /// 新的难度
        /// </summary>
        public double NewDifficulty { get; set; }

        /// <summary>
        /// 预测的保留率
        /// </summary>
        public double PredictedRetention { get; set; }

        /// <summary>
        /// 是否需要立即复习
        /// </summary>
        public bool ShouldReview { get; set; }

        /// <summary>
        /// 学习阶段（用于FSRS学习阶段管理）
        /// </summary>
        public int LearningStage { get; set; } = -1;

        /// <summary>
        /// 消息提示
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 算法类型
        /// </summary>
        public string AlgorithmType { get; set; } = string.Empty;
    }

    /// <summary>
    /// 算法对比结果
    /// </summary>
    public class AlgorithmComparisonResult
    {
        public string RecommendedAlgorithm { get; set; } = "SM-2";
        public double RecommendedScore { get; set; }
        public Dictionary<string, AlgorithmStats> AlgorithmStats { get; set; } = new Dictionary<string, AlgorithmStats>();
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 算法统计数据
    /// </summary>
    public class AlgorithmStats
    {
        public string AlgorithmType { get; set; } = string.Empty;
        public int TotalReviews { get; set; }
        public int CorrectReviews { get; set; }
        public double AccuracyRate { get; set; }
        public double AverageInterval { get; set; }
        public double RetentionRate { get; set; }
        public double ConsistencyScore { get; set; }
    }
}
