using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Xunit;

namespace LearningAssistant.Core.Tests;

public class LearningItemIdentityTests
{
    [Fact]
    public void EnsureKey_AssignsStableKeyWhenIdMissing()
    {
        var item = new LearningItem { MainContent = "apple" };

        var key1 = LearningItemIdentity.EnsureKey(item);
        var key2 = LearningItemIdentity.EnsureKey(item);

        Assert.False(string.IsNullOrWhiteSpace(key1));
        Assert.Equal(key1, key2);
        Assert.Equal(key1, item.Id);
    }

    [Fact]
    public void GetCanonicalKey_NormalizesCaseFullWidthAndWhitespace()
    {
        var a = new LearningItem
        {
            Subject = SubjectType.English,
            SubCategory = SubCategoryType.EnglishWord,
            MainContent = "  Hello   World "
        };
        var b = new LearningItem
        {
            Subject = SubjectType.English,
            SubCategory = SubCategoryType.EnglishWord,
            MainContent = "hello world"
        };

        Assert.Equal(LearningItemIdentity.GetCanonicalKey(a), LearningItemIdentity.GetCanonicalKey(b));
    }

    [Fact]
    public void GetCanonicalKey_DiffersAcrossSubCategories()
    {
        var word = new LearningItem
        {
            Subject = SubjectType.English,
            SubCategory = SubCategoryType.EnglishWord,
            MainContent = "run"
        };
        var sentence = new LearningItem
        {
            Subject = SubjectType.English,
            SubCategory = SubCategoryType.EnglishSentence,
            MainContent = "run"
        };

        Assert.NotEqual(LearningItemIdentity.GetCanonicalKey(word), LearningItemIdentity.GetCanonicalKey(sentence));
    }
}
