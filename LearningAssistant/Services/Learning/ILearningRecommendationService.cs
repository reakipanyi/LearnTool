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
}
