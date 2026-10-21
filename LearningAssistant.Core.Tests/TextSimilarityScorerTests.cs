using LearningAssistant.Services.Shadowing;
using Xunit;

namespace LearningAssistant.Core.Tests;

public class TextSimilarityScorerTests
{
    [Fact]
    public void Score_ExactMatchReturnsFullScore()
    {
        var result = TextSimilarityScorer.Score("The cat sat down.", "The cat sat down.");

        Assert.True(result.IsValid);
        Assert.Equal(100, result.Score);
        Assert.Empty(result.MissingWords);
    }

    [Fact]
    public void Score_IgnoresCaseAndPunctuation()
    {
        var result = TextSimilarityScorer.Score("The cat sat down.", "the CAT sat, down!");

        Assert.Equal(100, result.Score);
    }

    [Fact]
    public void Score_MissingWordsLowersScoreAndReportsThem()
    {
        var result = TextSimilarityScorer.Score("The cat sat down.", "The cat");

        Assert.True(result.Score < 100);
        Assert.Contains("sat", result.MissingWords);
        Assert.Contains("down", result.MissingWords);
    }

    [Fact]
    public void Score_EmptyRecognitionReturnsZero()
    {
        var result = TextSimilarityScorer.Score("The cat sat down.", "");

        Assert.True(result.IsValid);
        Assert.Equal(0, result.Score);
        Assert.Equal(4, result.MissingWords.Count);
    }

    [Fact]
    public void Score_EmptyExpectedTextIsInvalid()
    {
        var result = TextSimilarityScorer.Score("", "anything");

        Assert.False(result.IsValid);
    }
}
