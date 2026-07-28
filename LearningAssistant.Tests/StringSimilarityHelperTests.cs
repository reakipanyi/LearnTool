using Xunit;
using FluentAssertions;
using LearningAssistant.Common;

namespace LearningAssistant.Tests
{
    public class StringSimilarityHelperTests
    {
        [Fact]
        public void CalculateSimilarity_WithIdenticalStrings_ShouldReturnOne()
        {
            var result = StringSimilarityHelper.CalculateSimilarity("hello", "hello");

            result.Should().Be(1.0);
        }

        [Fact]
        public void CalculateSimilarity_WithDifferentStrings_ShouldReturnZero()
        {
            var result = StringSimilarityHelper.CalculateSimilarity("hello", "world");

            result.Should().BeLessThan(1.0);
        }

        [Fact]
        public void CalculateSimilarity_WithEmptySource_ShouldReturnZero()
        {
            var result = StringSimilarityHelper.CalculateSimilarity("", "hello");

            result.Should().Be(0);
        }

        [Fact]
        public void CalculateSimilarity_WithEmptyTarget_ShouldReturnZero()
        {
            var result = StringSimilarityHelper.CalculateSimilarity("hello", "");

            result.Should().Be(0);
        }

        [Fact]
        public void CalculateSimilarity_WithNullSource_ShouldReturnZero()
        {
            var result = StringSimilarityHelper.CalculateSimilarity(null!, "hello");

            result.Should().Be(0);
        }

        [Fact]
        public void CalculateSimilarity_WithNullTarget_ShouldReturnZero()
        {
            var result = StringSimilarityHelper.CalculateSimilarity("hello", null!);

            result.Should().Be(0);
        }

        [Fact]
        public void CalculateSimilarity_WithCaseDifferences_ShouldIgnoreCase()
        {
            var result = StringSimilarityHelper.CalculateSimilarity("Hello", "hello");

            result.Should().Be(1.0);
        }

        [Fact]
        public void CalculateSimilarity_WithPartialMatch_ShouldReturnHighSimilarity()
        {
            var result = StringSimilarityHelper.CalculateSimilarity("apple", "app");

            result.Should().BeGreaterThan(0.5);
        }

        [Fact]
        public void CalculateSimilarity_WithChineseCharacters_ShouldCalculateCorrectly()
        {
            var result = StringSimilarityHelper.CalculateSimilarity("学习", "学习");

            result.Should().Be(1.0);
        }

        [Fact]
        public void CalculateSimilarity_WithSimilarChineseCharacters_ShouldReturnMediumSimilarity()
        {
            var result = StringSimilarityHelper.CalculateSimilarity("学习", "学");

            result.Should().Be(0.5);
        }

        [Fact]
        public void LevenshteinDistance_WithIdenticalStrings_ShouldReturnZero()
        {
            var result = StringSimilarityHelper.LevenshteinDistance("hello", "hello");

            result.Should().Be(0);
        }

        [Fact]
        public void LevenshteinDistance_WithCompletelyDifferentStrings_ShouldReturnMaxLength()
        {
            var result = StringSimilarityHelper.LevenshteinDistance("abc", "def");

            result.Should().Be(3);
        }

        [Fact]
        public void LevenshteinDistance_WithEmptySource_ShouldReturnTargetLength()
        {
            var result = StringSimilarityHelper.LevenshteinDistance("", "hello");

            result.Should().Be(5);
        }

        [Fact]
        public void LevenshteinDistance_WithEmptyTarget_ShouldReturnSourceLength()
        {
            var result = StringSimilarityHelper.LevenshteinDistance("hello", "");

            result.Should().Be(5);
        }

        [Fact]
        public void LevenshteinDistance_WithNullSource_ShouldReturnTargetLength()
        {
            var result = StringSimilarityHelper.LevenshteinDistance(null!, "hello");

            result.Should().Be(5);
        }

        [Fact]
        public void LevenshteinDistance_WithNullTarget_ShouldReturnSourceLength()
        {
            var result = StringSimilarityHelper.LevenshteinDistance("hello", null!);

            result.Should().Be(5);
        }

        [Fact]
        public void LevenshteinDistance_WithOneCharacterDifference_ShouldReturnOne()
        {
            var result = StringSimilarityHelper.LevenshteinDistance("hello", "hallo");

            result.Should().Be(1);
        }

        [Fact]
        public void CheckAnswer_WithExactMatch_ShouldReturnTrue()
        {
            var result = StringSimilarityHelper.CheckAnswer("hello", "hello");

            result.Should().BeTrue();
        }

        [Fact]
        public void CheckAnswer_WithCaseDifference_ShouldReturnTrue()
        {
            var result = StringSimilarityHelper.CheckAnswer("Hello", "hello");

            result.Should().BeTrue();
        }

        [Fact]
        public void CheckAnswer_WithUserAnswerSubset_ShouldReturnTrue()
        {
            var result = StringSimilarityHelper.CheckAnswer("学", "学习");

            result.Should().BeTrue();
        }

        [Fact]
        public void CheckAnswer_WithCorrectAnswerSubset_ShouldReturnTrue()
        {
            var result = StringSimilarityHelper.CheckAnswer("学习中", "学习");

            result.Should().BeTrue();
        }

        [Fact]
        public void CheckAnswer_WithMediumSimilarity_ShouldReturnFalse()
        {
            var result = StringSimilarityHelper.CheckAnswer("学西", "学习");

            result.Should().BeFalse();
        }

        [Fact]
        public void CheckAnswer_WithLowSimilarity_ShouldReturnFalse()
        {
            var result = StringSimilarityHelper.CheckAnswer("abc", "xyz");

            result.Should().BeFalse();
        }

        [Fact]
        public void CheckAnswer_WithEmptyUserAnswer_ShouldReturnFalse()
        {
            var result = StringSimilarityHelper.CheckAnswer("", "hello");

            result.Should().BeFalse();
        }

        [Fact]
        public void CheckAnswer_WithNullUserAnswer_ShouldReturnFalse()
        {
            var result = StringSimilarityHelper.CheckAnswer(null!, "hello");

            result.Should().BeFalse();
        }

        [Fact]
        public void CheckAnswer_WithTrimmedSpaces_ShouldReturnTrue()
        {
            var result = StringSimilarityHelper.CheckAnswer("  hello  ", "hello");

            result.Should().BeTrue();
        }

        [Fact]
        public void CheckAnswer_WithCustomThreshold_ShouldReturnBasedOnThreshold()
        {
            var resultHighThreshold = StringSimilarityHelper.CheckAnswer("学西", "学习", 0.9);
            var resultLowThreshold = StringSimilarityHelper.CheckAnswer("学西", "学习", 0.5);
            var resultVeryLowThreshold = StringSimilarityHelper.CheckAnswer("学西", "学习", 0.4);

            resultHighThreshold.Should().BeFalse();
            resultLowThreshold.Should().BeFalse();
            resultVeryLowThreshold.Should().BeTrue();
        }

        [Fact]
        public void CheckAnswer_WithChineseExactMatch_ShouldReturnTrue()
        {
            var result = StringSimilarityHelper.CheckAnswer("苹果", "苹果");

            result.Should().BeTrue();
        }

        [Fact]
        public void CheckAnswer_WithChinesePartialMatch_ShouldReturnTrue()
        {
            var result = StringSimilarityHelper.CheckAnswer("苹", "苹果");

            result.Should().BeTrue();
        }
    }
}
