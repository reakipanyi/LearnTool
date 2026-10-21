using FluentAssertions;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Pdf;

public class PdfAiContextBuilderTests
{
    [Fact]
    public void 选中作用域_仅包含当前页与选中文本()
    {
        var document = CreateDocument(5);

        var result = PdfAiContextBuilder.Build(document, PdfAiScope.Selection, 2, 1, 5, "选中的句子");

        result.IncludedPages.Should().BeEquivalentTo(new[] { 3 });
        result.Context.Should().Contain("[[p3]]").And.Contain("选中的句子");
        result.Context.Should().NotContain("[[p1]]");
    }

    [Fact]
    public void 本页作用域_包含当前页及前后页()
    {
        var document = CreateDocument(5);

        var result = PdfAiContextBuilder.Build(document, PdfAiScope.CurrentPage, 2, 1, 5, null);

        result.IncludedPages.Should().BeEquivalentTo(new[] { 2, 3, 4 });
    }

    [Fact]
    public void 指定页作用域_按范围输出连续页码()
    {
        var document = CreateDocument(10);

        var result = PdfAiContextBuilder.Build(document, PdfAiScope.SpecifiedPages, 0, 1, 3, null);

        result.IncludedPages.Should().BeEquivalentTo(new[] { 2, 3, 4 });
    }

    [Fact]
    public void 全文作用域_受总字符预算限制()
    {
        var document = CreateDocument(10, 1000);

        var result = PdfAiContextBuilder.Build(document, PdfAiScope.WholeDocument, 0, 0, 9, null,
            maxCharsPerPage: 500, maxTotalChars: 1200);

        result.IncludedPages.Count.Should().BeLessThan(10);
        result.IncludedPages.Count.Should().BeGreaterThan(0);
        result.Context.Length.Should().BeLessOrEqualTo(1300);
    }

    [Fact]
    public void 空页面被跳过_不产生空页码()
    {
        var document = CreateDocument(3);
        document.Pages[1].Text = string.Empty;

        var result = PdfAiContextBuilder.Build(document, PdfAiScope.WholeDocument, 0, 0, 2, null);

        result.IncludedPages.Should().BeEquivalentTo(new[] { 1, 3 });
    }

    private static PdfTextIndexDocument CreateDocument(int pageCount, int charsPerPage = 20)
    {
        return new PdfTextIndexDocument
        {
            PageCount = pageCount,
            Fingerprint = "fp",
            Pages = Enumerable.Range(0, pageCount).Select(i => new PdfTextIndexPage
            {
                PageIndex = i,
                Text = new string('文', charsPerPage),
                CharacterCount = charsPerPage
            }).ToList()
        };
    }
}
