using FluentAssertions;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Pdf;

public class PdfKeywordRetrieverTests
{
    [Fact]
    public void 命中页按得分召回_并扩展邻近页()
    {
        var document = BuildDocument(10);
        document.GetPage(3)!.Text = "苹果 苹果 香蕉 香蕉 香蕉";
        document.GetPage(8)!.Text = "苹果 一次";

        var result = PdfKeywordRetriever.Retrieve(document, "苹果和香蕉有什么区别");

        result.HasMatch.Should().BeTrue();
        result.PageIndexes.Should().Contain(3).And.Contain(2).And.Contain(4);
        result.PageIndexes.Should().BeInAscendingOrder();
        // 第 3 页得分最高，应优先召回
        result.PageIndexes.First().Should().Be(2);
    }

    [Fact]
    public void 无关键词命中_返回空结果()
    {
        var document = BuildDocument(3);
        document.GetPage(0)!.Text = "苹果";
        document.GetPage(1)!.Text = "香蕉";

        var result = PdfKeywordRetriever.Retrieve(document, "量子纠缠");

        result.HasMatch.Should().BeFalse();
        result.PageIndexes.Should().BeEmpty();
        result.Keywords.Should().NotBeEmpty();
    }

    [Fact]
    public void 空页不参与召回()
    {
        var document = BuildDocument(4);
        document.GetPage(1)!.Text = "关键概念说明";
        document.GetPage(2)!.Text = string.Empty;

        var result = PdfKeywordRetriever.Retrieve(document, "关键概念");

        result.PageIndexes.Should().Contain(1);
        result.PageIndexes.Should().NotContain(2);
    }

    [Fact]
    public void 关键词提取_过滤疑问词并保留中英文()
    {
        var keywords = PdfKeywordRetriever.ExtractKeywords("什么是 Machine Learning 的 训练过程？");

        keywords.Should().Contain("Machine");
        keywords.Should().Contain("Learning");
        keywords.Should().Contain("训练");
        keywords.Should().NotContain("什么");
    }

    [Fact]
    public void 空问题_返回空结果()
    {
        var document = BuildDocument(2);
        document.GetPage(0)!.Text = "内容";

        PdfKeywordRetriever.Retrieve(document, " ").HasMatch.Should().BeFalse();
    }

    private static PdfTextIndexDocument BuildDocument(int pageCount)
    {
        return new PdfTextIndexDocument
        {
            DocumentPath = "book.pdf",
            Fingerprint = "fp",
            PageCount = pageCount,
            Pages = Enumerable.Range(0, pageCount)
                .Select(i => new PdfTextIndexPage { PageIndex = i, Text = "占位内容" })
                .ToList()
        };
    }
}
