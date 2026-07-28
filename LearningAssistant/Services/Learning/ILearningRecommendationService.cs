using LearningAssistant.Models.Learning;

namespace LearningAssistant.Services.Learning
{
    public interface ILearningRecommendationService
    {
        List<LearningRecommendation> GetDailyRecommendations(string userId, int count = 6);

        LearningRecommendation? GetNextItem(string userId);

        List<WeakPointAnalysis> GetWeakPoints(string userId);

        LearningPathSuggestion GetLearningPathSuggestion(string userId, string domain);

        List<LearningRecommendation> GetReviewPriorities(string userId, int count = 10);

        double CalculateRecommendationScore(string userId, LearningRecommendation item);

        void RecordFeedback(string userId, string recommendationId, bool isInterested);

        RecommendationWeights GetWeights(string userId);

        void AdjustWeights(string userId, RecommendationWeights weights);

        /// <summary>
        /// 深度薄弱点分析（P-004）
        /// 基于错题频率、复习间隔、正确率等多维度分析
        /// </summary>
        List<DeepWeakPointAnalysis> GetDeepWeakPoints(string userId);

        /// <summary>
        /// 生成个性化学习路径建议（P-004）
        /// </summary>
        PersonalizedPathSuggestion GetPersonalizedPath(string userId);
    }

    public class WeakPointAnalysis
    {
        public string Category { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public double ErrorRate { get; set; }
        public int TotalCount { get; set; }
        public int WrongCount { get; set; }
        public double Severity { get; set; }
        public string Suggestion { get; set; } = string.Empty;
        public string Icon { get; set; } = "📚";
    }

    public class LearningPathSuggestion
    {
        public string Domain { get; set; } = string.Empty;
        public string CurrentLevel { get; set; } = string.Empty;
        public string SuggestedNextLevel { get; set; } = string.Empty;
        public List<string> NextTopics { get; set; } = new List<string>();
        public double ProgressPercent { get; set; }
        public string Suggestion { get; set; } = string.Empty;
        public int EstimatedDaysToNextLevel { get; set; }
    }

    public class RecommendationWeights
    {
        public double UrgencyWeight { get; set; } = 0.4;
        public double WeaknessWeight { get; set; } = 0.3;
        public double FreshnessWeight { get; set; } = 0.2;
        public double VarietyWeight { get; set; } = 0.1;
    }

    /// <summary>
    /// 深度薄弱点分析结果
    /// </summary>
    public class DeepWeakPointAnalysis
    {
        public string Category { get; set; } = string.Empty;
        public double WeaknessScore { get; set; }
        public int WrongCount { get; set; }
        public int ReviewCount { get; set; }
        public double AccuracyRate { get; set; }
        public int DaysSinceLastReview { get; set; }
        public List<string> RecommendedActions { get; set; } = new();
    }

    /// <summary>
    /// 个性化路径建议
    /// </summary>
    public class PersonalizedPathSuggestion
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Steps { get; set; } = new();
        public int EstimatedDays { get; set; }
        public double MatchScore { get; set; }
    }
}
