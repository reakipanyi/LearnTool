using Xunit;
using FluentAssertions;
using LearningAssistant.Services.Learning;

namespace LearningAssistant.Tests
{
    public class FSRSAlgorithmTests
    {
        private readonly FSRSAlgorithm _algorithm = new FSRSAlgorithm();

        private ReviewItem CreateReviewItem(int repetitions = 0, double stability = 4.0, double difficulty = 5.0)
        {
            return new ReviewItem
            {
                Repetitions = repetitions,
                Stability = stability,
                Difficulty = difficulty,
                LastReviewDate = DateTime.Now
            };
        }

        [Fact]
        public void Calculate_WithInvalidRating0_ShouldReturnShouldReviewTrue()
        {
            var item = CreateReviewItem();

            var result = _algorithm.Calculate(item, 0);

            result.ShouldReview.Should().BeTrue();
            result.Message.Should().Contain("评分范围应为 1-4");
        }

        [Fact]
        public void Calculate_WithInvalidRating5_ShouldReturnShouldReviewTrue()
        {
            var item = CreateReviewItem();

            var result = _algorithm.Calculate(item, 5);

            result.ShouldReview.Should().BeTrue();
            result.Message.Should().Contain("评分范围应为 1-4");
        }

        [Fact]
        public void Calculate_WithRating1_ShouldDecreaseStabilityAndIncreaseDifficulty()
        {
            var item = CreateReviewItem(stability: 10.0, difficulty: 5.0);

            var result = _algorithm.Calculate(item, 1);

            result.ShouldReview.Should().BeTrue();
            result.NewStability.Should().BeLessThan(10.0);
            result.NewDifficulty.Should().BeGreaterThan(5.0);
            result.Message.Should().Contain("忘记了");
        }

        [Fact]
        public void Calculate_WithRating4_ShouldIncreaseStability()
        {
            var item = CreateReviewItem(stability: 10.0, difficulty: 5.0);

            var result = _algorithm.Calculate(item, 4);

            result.ShouldReview.Should().BeFalse();
            result.NewStability.Should().BeGreaterThan(10.0);
            result.Message.Should().Contain("太轻松了");
        }

        [Fact]
        public void Calculate_WithRating3_ShouldIncreaseStability()
        {
            var item = CreateReviewItem(stability: 10.0, difficulty: 5.0);

            var result = _algorithm.Calculate(item, 3);

            result.ShouldReview.Should().BeFalse();
            result.NewStability.Should().BeGreaterThan(10.0);
            result.Message.Should().Contain("掌握良好");
        }

        [Fact]
        public void Calculate_WithRating2_ShouldApplyHardPenalty()
        {
            var item = CreateReviewItem(stability: 10.0, difficulty: 5.0);

            var result = _algorithm.Calculate(item, 2);

            result.ShouldReview.Should().BeFalse();
            result.Message.Should().Contain("有些困难");
        }

        [Fact]
        public void Calculate_StabilityShouldNotGoBelowMin()
        {
            var item = CreateReviewItem(stability: 0.5, difficulty: 5.0);

            var result = _algorithm.Calculate(item, 1);

            result.NewStability.Should().BeGreaterThanOrEqualTo(0.5);
        }

        [Fact]
        public void Calculate_DifficultyShouldBeClamped()
        {
            var item = CreateReviewItem(stability: 10.0, difficulty: 9.5);

            var result = _algorithm.Calculate(item, 1);

            result.NewDifficulty.Should().BeLessThanOrEqualTo(10.0);
            result.NewDifficulty.Should().BeGreaterThanOrEqualTo(1.0);
        }

        [Fact]
        public void Calculate_DifficultyShouldRevertToMean()
        {
            var item = CreateReviewItem(stability: 10.0, difficulty: 8.0);

            var result = _algorithm.Calculate(item, 4);

            result.NewDifficulty.Should().BeLessThan(8.0);
        }

        [Fact]
        public void Calculate_ShouldIncreaseRepetitions()
        {
            var item = CreateReviewItem(repetitions: 5);

            var result = _algorithm.Calculate(item, 3);

            result.NewRepetitions.Should().Be(6);
        }

        [Fact]
        public void Calculate_IntervalShouldBeAtLeast1()
        {
            var item = CreateReviewItem(stability: 0.1, difficulty: 5.0);

            var result = _algorithm.Calculate(item, 3);

            result.NewInterval.Should().BeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public void PredictRetention_WithZeroStability_ShouldReturnZero()
        {
            var result = _algorithm.PredictRetention(0, 5, 1);

            result.Should().Be(0);
        }

        [Fact]
        public void PredictRetention_WithNegativeStability_ShouldReturnZero()
        {
            var result = _algorithm.PredictRetention(-1, 5, 1);

            result.Should().Be(0);
        }

        [Fact]
        public void PredictRetention_WithPositiveStability_ShouldReturnValueBetweenZeroAndOne()
        {
            var result = _algorithm.PredictRetention(10, 5, 5);

            result.Should().BeGreaterThan(0);
            result.Should().BeLessThan(1);
        }

        [Fact]
        public void PredictRetention_HigherDifficulty_ShouldLowerRetention()
        {
            var retentionEasy = _algorithm.PredictRetention(10, 2, 5);
            var retentionHard = _algorithm.PredictRetention(10, 8, 5);

            retentionHard.Should().BeLessThan(retentionEasy);
        }

        [Fact]
        public void GetOptimalInterval_WithZeroStability_ShouldReturn1()
        {
            var result = _algorithm.GetOptimalInterval(0);

            result.Should().Be(1);
        }

        [Fact]
        public void GetOptimalInterval_WithPositiveStability_ShouldReturnValidInterval()
        {
            var result = _algorithm.GetOptimalInterval(10);

            result.Should().BeGreaterThan(0);
            result.Should().BeLessThanOrEqualTo(365);
        }

        [Fact]
        public void AlgorithmProperties_ShouldBeSetCorrectly()
        {
            _algorithm.Name.Should().Be("Free Spaced Repetition Scheduler");
            _algorithm.AlgorithmType.Should().Be("FSRS");
            _algorithm.RecommendedRetention.Should().Be(0.9);
            _algorithm.StabilityWeight.Should().Be(1.5);
        }

        [Fact]
        public void Calculate_NewItem_ShouldUseInitialValues()
        {
            var item = new ReviewItem
            {
                Repetitions = 0,
                Stability = 0,
                Difficulty = 0,
                LastReviewDate = DateTime.Now
            };

            var result = _algorithm.Calculate(item, 3);

            result.NewStability.Should().BeGreaterThan(0);
            result.NewDifficulty.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Calculate_WithRating4_ShouldApplyEasyBonus()
        {
            var item = CreateReviewItem(stability: 10.0);

            var resultRating3 = _algorithm.Calculate(item, 3);
            var resultRating4 = _algorithm.Calculate(item, 4);

            resultRating4.NewStability.Should().BeGreaterThan(resultRating3.NewStability);
        }
    }
}
