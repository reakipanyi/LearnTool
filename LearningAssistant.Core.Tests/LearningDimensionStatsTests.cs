using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.Practice;
using LearningAssistant.Services.Learning;
using Xunit;

namespace LearningAssistant.Core.Tests;

public class LearningDimensionStatsTests
{
    private static LearningItem CreateItem(
        string id,
        string content,
        ImportanceLevel importance = ImportanceLevel.Normal,
        string knowledgePointPath = "",
        StudyContentSource sourceType = StudyContentSource.Manual)
    {
        return new LearningItem
        {
            Id = id,
            Subject = SubjectType.English,
            SubCategory = SubCategoryType.EnglishWord,
            MainContent = content,
            Importance = importance,
            KnowledgePointPath = knowledgePointPath,
            SourceType = sourceType
        };
    }

    [Fact]
    public void Calculate_GroupsByImportance_AndAggregatesKnownAndPractice()
    {
        var items = new[]
        {
            CreateItem("a", "apple", ImportanceLevel.Core),
            CreateItem("b", "banana", ImportanceLevel.Core),
            CreateItem("c", "cat", ImportanceLevel.Normal)
        };
        var practices = new[]
        {
            new PracticeRecord { LearningItemId = "a", ItemContent = "apple", Score = 90 }
        };

        var rows = LearningDimensionStatsService.Calculate(
            items, item => item.Id == "a", practices, DimensionKind.Importance);

        var core = Assert.Single(rows, row => row.Key == "核心");
        Assert.Equal(2, core.Total);
        Assert.Equal(1, core.Known);
        Assert.Equal(1, core.PracticeCount);
        Assert.Equal(90, core.AverageScore);

        var normal = Assert.Single(rows, row => row.Key == "一般");
        Assert.Equal(1, normal.Total);
        Assert.Equal(0, normal.PracticeCount);
    }

    [Fact]
    public void Calculate_EmptyKnowledgePoint_FallsBackToUnclassified()
    {
        var items = new[]
        {
            CreateItem("a", "apple", knowledgePointPath: "词法/名词"),
            CreateItem("b", "banana")
        };

        var rows = LearningDimensionStatsService.Calculate(
            items, _ => false, Array.Empty<PracticeRecord>(), DimensionKind.KnowledgePoint);

        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, row => row.Key == "词法/名词" && row.Total == 1);
        Assert.Contains(rows, row => row.Key == "未分类" && row.Total == 1);
    }

    [Fact]
    public void Calculate_UsesEffectiveSourceType()
    {
        var item = CreateItem("a", "apple", sourceType: StudyContentSource.Manual);
        // 旧数据：SourceType 为默认值但 ExtendedProperties 记录了 PDF 来源
        item.ExtendedProperties = "{\"SourceType\":\"PdfHighlight\"}";

        var rows = LearningDimensionStatsService.Calculate(
            new[] { item }, _ => false, Array.Empty<PracticeRecord>(), DimensionKind.Source);

        var row = Assert.Single(rows);
        Assert.Equal("PDF高亮", row.Key);
    }

    [Fact]
    public void Calculate_MatchesPracticeByContent_WhenItemIdMissing()
    {
        var items = new[] { CreateItem(string.Empty, "apple") };
        var practices = new[]
        {
            new PracticeRecord { LearningItemId = null, ItemContent = "  APPLE ", Score = 80 }
        };

        var rows = LearningDimensionStatsService.Calculate(
            items, _ => true, practices, DimensionKind.AgeRange);

        var row = Assert.Single(rows);
        Assert.Equal(1, row.PracticeCount);
        Assert.Equal(80, row.AverageScore);
        Assert.Equal(1, row.Known);
    }
}
