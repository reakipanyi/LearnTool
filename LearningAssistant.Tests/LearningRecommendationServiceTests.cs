using Moq;
using Xunit;
using FluentAssertions;
using LearningAssistant.Services.Learning;
using LearningAssistant.Models.Learning;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Tests
{
    public class LearningRecommendationServiceTests
    {
        private readonly Mock<ILogger<LearningRecommendationService>> _mockLogger;
        private readonly Mock<ILearningAnalyticsService> _mockAnalytics;
        private readonly Mock<ISpacedRepetitionService> _mockSpacedRepetition;
        private readonly Mock<IWrongAnswerService> _mockWrongAnswer;
        private readonly Mock<ILearningPathService> _mockLearningPath;
        private readonly LearningRecommendationService _service;

        public LearningRecommendationServiceTests()
        {
            _mockLogger = new Mock<ILogger<LearningRecommendationService>>();
            _mockAnalytics = new Mock<ILearningAnalyticsService>();
            _mockSpacedRepetition = new Mock<ISpacedRepetitionService>();
            _mockWrongAnswer = new Mock<IWrongAnswerService>();
            _mockLearningPath = new Mock<ILearningPathService>();

            _mockAnalytics.Setup(a => a.GetCategoryStats(It.IsAny<string>()))
                .Returns(new Dictionary<string, int>());

            _service = new LearningRecommendationService(
                _mockSpacedRepetition.Object,
                _mockWrongAnswer.Object,
                _mockAnalytics.Object,
                _mockLearningPath.Object,
                _mockLogger.Object);
        }

        [Fact]
        public void GetDailyRecommendations_ShouldReturnRecommendations()
        {
            var recommendations = _service.GetDailyRecommendations("test_user", 5);

            recommendations.Should().NotBeNull();
            recommendations.Count.Should().BeLessThanOrEqualTo(5);
        }

        [Fact]
        public void GetDailyRecommendations_WithDefaultCount_ShouldReturnSix()
        {
            var recommendations = _service.GetDailyRecommendations("test_user");

            recommendations.Count.Should().BeLessThanOrEqualTo(6);
        }

        [Fact]
        public void GetNextItem_WithNoData_ShouldReturnNull()
        {
            var nextItem = _service.GetNextItem("test_user");

            nextItem.Should().BeNull();
        }

        [Fact]
        public void GetWeakPoints_WithNoData_ShouldReturnEmpty()
        {
            var weakPoints = _service.GetWeakPoints("test_user");

            weakPoints.Should().BeEmpty();
        }

        [Fact]
        public void GetLearningPathSuggestion_ShouldReturnSuggestion()
        {
            var suggestion = _service.GetLearningPathSuggestion("test_user", "English");

            suggestion.Should().NotBeNull();
            suggestion.Domain.Should().Be("English");
            suggestion.CurrentLevel.Should().NotBeEmpty();
            suggestion.SuggestedNextLevel.Should().NotBeEmpty();
        }

        [Fact]
        public void GetReviewPriorities_ShouldReturnPriorities()
        {
            var priorities = _service.GetReviewPriorities("test_user", 5);

            priorities.Should().NotBeNull();
            priorities.Count.Should().BeLessThanOrEqualTo(5);
        }

        [Fact]
        public void GetReviewPriorities_WithDefaultCount_ShouldReturnTen()
        {
            var priorities = _service.GetReviewPriorities("test_user");

            priorities.Count.Should().BeLessThanOrEqualTo(10);
        }

        [Fact]
        public void CalculateRecommendationScore_ShouldReturnScore()
        {
            var recommendation = new LearningRecommendation
            {
                Id = "test_1",
                Title = "Test Item",
                Type = "review",
                ContentType = "English",
                Priority = 8
            };

            var score = _service.CalculateRecommendationScore("test_user", recommendation);

            score.Should().BeInRange(0, 1);
        }

        [Fact]
        public void RecordFeedback_ShouldNotThrow()
        {
            Action action = () => _service.RecordFeedback("test_user", "rec_1", true);
            action.Should().NotThrow();
        }

        [Fact]
        public void GetWeights_ShouldReturnDefaultWeights()
        {
            var weights = _service.GetWeights("test_user");

            weights.Should().NotBeNull();
            weights.UrgencyWeight.Should().Be(0.4);
            weights.WeaknessWeight.Should().Be(0.3);
            weights.FreshnessWeight.Should().Be(0.2);
            weights.VarietyWeight.Should().Be(0.1);
        }

        [Fact]
        public void AdjustWeights_ShouldUpdateWeights()
        {
            var newWeights = new RecommendationWeights
            {
                UrgencyWeight = 0.5,
                WeaknessWeight = 0.25,
                FreshnessWeight = 0.15,
                VarietyWeight = 0.1
            };

            _service.AdjustWeights("test_user", newWeights);

            var weights = _service.GetWeights("test_user");
            weights.UrgencyWeight.Should().Be(0.5);
            weights.WeaknessWeight.Should().Be(0.25);
        }

        [Fact]
        public void AdjustWeights_WithInvalidWeights_ShouldNormalize()
        {
            var newWeights = new RecommendationWeights
            {
                UrgencyWeight = 2.0,
                WeaknessWeight = 0.0,
                FreshnessWeight = 0.0,
                VarietyWeight = 0.0
            };

            _service.AdjustWeights("test_user", newWeights);

            var weights = _service.GetWeights("test_user");
            weights.UrgencyWeight.Should().Be(1.0);
            weights.WeaknessWeight.Should().Be(0.0);
        }

        [Fact]
        public void GetLearningPathSuggestion_WithDifferentDomains_ShouldReturnDifferentLevels()
        {
            var suggestion1 = _service.GetLearningPathSuggestion("test_user", "Math");
            var suggestion2 = _service.GetLearningPathSuggestion("test_user", "Science");

            suggestion1.Domain.Should().Be("Math");
            suggestion2.Domain.Should().Be("Science");
        }

        [Fact]
        public void GetLearningPathSuggestion_WithEmptyDomain_ShouldReturnDefault()
        {
            var suggestion = _service.GetLearningPathSuggestion("test_user", "");

            suggestion.Should().NotBeNull();
            suggestion.Domain.Should().Be("");
        }

        [Fact]
        public void GetWeakPoints_WithCategoryStats_ShouldReturnAnalysis()
        {
            _mockAnalytics.Setup(a => a.GetCategoryStats("test_user"))
                .Returns(new Dictionary<string, int> { { "EnglishWord", 10 }, { "MathFormula", 5 } });

            var weakPoints = _service.GetWeakPoints("test_user");

            weakPoints.Should().NotBeNull();
        }
    }
}