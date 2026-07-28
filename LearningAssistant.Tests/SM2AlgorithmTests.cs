using Xunit;
using FluentAssertions;
using LearningAssistant.Services.Learning;

namespace LearningAssistant.Tests
{
    public class SM2AlgorithmTests
    {
        private readonly SM2Algorithm _algorithm = new SM2Algorithm();

        private ReviewItem CreateReviewItem(int repetitions = 0, int interval = 0, double eFactor = 2.5)
        {
            return new ReviewItem
            {
                Repetitions = repetitions,
                Interval = interval,
                EFactor = eFactor
            };
        }

        [Fact]
        public void Calculate_WithInvalidRating_ShouldReturnShouldReviewTrue()
        {
            var item = CreateReviewItem();

            var result = _algorithm.Calculate(item, -1);

            result.ShouldReview.Should().BeTrue();
            result.Message.Should().Contain("质量评分无效");
        }

        [Fact]
        public void Calculate_WithRating6_ShouldReturnShouldReviewTrue()
        {
            var item = CreateReviewItem();

            var result = _algorithm.Calculate(item, 6);

            result.ShouldReview.Should().BeTrue();
            result.Message.Should().Contain("质量评分无效");
        }

        [Fact]
        public void Calculate_WithRating0_ShouldReturnShouldReviewTrue()
        {
            var item = CreateReviewItem();

            var result = _algorithm.Calculate(item, 0);

            result.ShouldReview.Should().BeTrue();
            result.Message.Should().Contain("需要重新学习");
        }

        [Fact]
        public void Calculate_WithRating2_ShouldResetRepetitionsAndSetIntervalTo1()
        {
            var item = CreateReviewItem(repetitions: 3, interval: 14, eFactor: 2.5);

            var result = _algorithm.Calculate(item, 2);

            result.ShouldReview.Should().BeTrue();
            result.NewRepetitions.Should().Be(0);
            result.NewInterval.Should().Be(1);
            result.Message.Should().Contain("需要重新学习");
        }

        [Fact]
        public void Calculate_WithRating3_ShouldIncreaseRepetitions()
        {
            var item = CreateReviewItem(repetitions: 0, interval: 0, eFactor: 2.5);

            var result = _algorithm.Calculate(item, 3);

            result.ShouldReview.Should().BeFalse();
            result.NewRepetitions.Should().Be(1);
        }

        [Fact]
        public void Calculate_FirstReviewWithRating3_ShouldSetIntervalTo1()
        {
            var item = CreateReviewItem(repetitions: 0, interval: 0, eFactor: 2.5);

            var result = _algorithm.Calculate(item, 3);

            result.NewInterval.Should().Be(1);
        }

        [Fact]
        public void Calculate_SecondReviewWithRating3_ShouldSetIntervalTo6()
        {
            var item = CreateReviewItem(repetitions: 1, interval: 1, eFactor: 2.5);

            var result = _algorithm.Calculate(item, 3);

            result.NewInterval.Should().Be(6);
        }

        [Fact]
        public void Calculate_ThirdReviewWithRating5_ShouldIncreaseIntervalByEFactor()
        {
            var item = CreateReviewItem(repetitions: 2, interval: 6, eFactor: 2.5);

            var result = _algorithm.Calculate(item, 5);

            result.NewInterval.Should().Be(16);
        }

        [Fact]
        public void Calculate_WithRating5_ShouldIncreaseEFactor()
        {
            var item = CreateReviewItem(eFactor: 2.5);

            var result = _algorithm.Calculate(item, 5);

            result.NewEFactor.Should().BeGreaterThan(2.5);
        }

        [Fact]
        public void Calculate_WithRating4_ShouldKeepEFactorSame()
        {
            var item = CreateReviewItem(eFactor: 2.5);

            var result = _algorithm.Calculate(item, 4);

            result.NewEFactor.Should().Be(2.5);
        }

        [Fact]
        public void Calculate_WithRating3_ShouldSlightlyDecreaseEFactor()
        {
            var item = CreateReviewItem(eFactor: 2.5);

            var result = _algorithm.Calculate(item, 3);

            result.NewEFactor.Should().BeLessThan(2.5);
        }

        [Fact]
        public void Calculate_WithRating1_ShouldDecreaseEFactorSignificantly()
        {
            var item = CreateReviewItem(eFactor: 2.5);

            var result = _algorithm.Calculate(item, 1);

            result.NewEFactor.Should().BeLessThan(2.5);
        }

        [Fact]
        public void Calculate_EFactorShouldNotGoBelowMin()
        {
            var item = CreateReviewItem(eFactor: 1.3);

            var result = _algorithm.Calculate(item, 0);

            result.NewEFactor.Should().Be(1.3);
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
        public void GetOptimalInterval_ShouldReturnAtLeast1()
        {
            var result = _algorithm.GetOptimalInterval(0.1);

            result.Should().Be(1);
        }

        [Fact]
        public void AlgorithmProperties_ShouldBeSetCorrectly()
        {
            _algorithm.Name.Should().Be("SuperMemo 2");
            _algorithm.AlgorithmType.Should().Be("SM-2");
            _algorithm.RecommendedRetention.Should().Be(0.9);
            _algorithm.StabilityWeight.Should().Be(1.0);
        }
    }
}
