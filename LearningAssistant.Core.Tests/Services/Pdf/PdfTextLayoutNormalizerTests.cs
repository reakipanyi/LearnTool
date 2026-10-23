using FluentAssertions;
using LearningAssistant.Services.Pdf;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Pdf;

public class PdfTextLayoutNormalizerTests
{
    private readonly PdfTextLayoutNormalizer _normalizer = new();

    [Fact]
    public void 英文硬换行_合并为同一段落()
    {
        var text = "This is a long sentence that was\nwrapped by the renderer into\nseveral lines.\n\nSecond paragraph.";

        var result = _normalizer.NormalizePage(text);

        result.Should().Be("This is a long sentence that was wrapped by the renderer into several lines.\n\nSecond paragraph.");
    }

    [Fact]
    public void 中文硬换行_直接拼接且不补空格()
    {
        var text = "这是一个被硬换行\n拆断的中文句子。\n\n第二段。";

        var result = _normalizer.NormalizePage(text);

        result.Should().Be("这是一个被硬换行拆断的中文句子。\n\n第二段。");
    }

    [Fact]
    public void 行尾连字符_修复断词()
    {
        var text = "The word exam-\nple is split.\nNext line.";

        var result = _normalizer.NormalizePage(text);

        result.Should().Be("The word example is split.\n\nNext line.");
    }

    [Fact]
    public void 列表项_不并入上一段()
    {
        var text = "准备材料：\n• 铅笔\n• 纸张\n- 橡皮";

        var result = _normalizer.NormalizePage(text);

        result.Should().Be("准备材料：\n\n• 铅笔\n\n• 纸张\n\n- 橡皮");
    }

    [Fact]
    public void 关闭合并选项_保留原始换行()
    {
        var text = "第一行\n第二行";
        var options = new PdfTextLayoutOptions { MergeWrappedLines = false };

        var result = _normalizer.NormalizePage(text, options);

        result.Should().Be("第一行\n\n第二行");
    }

    [Fact]
    public void 检测页眉模板_需要覆盖足够多页面()
    {
        var pages = new[]
        {
            "读书笔记\n正文一。\n第 1 页",
            "读书笔记\n正文二。\n第 2 页",
            "读书笔记\n正文三。\n第 3 页"
        };

        var patterns = _normalizer.DetectHeaderFooterPatterns(pages);

        patterns.Should().ContainSingle().Which.Should().Be("读书笔记");
    }

    [Fact]
    public void 整篇规范化_移除页眉与页码()
    {
        var pages = new[]
        {
            "读书笔记\n正文一。\n第 1 页",
            "读书笔记\n正文二。\n第 2 页",
            "读书笔记\n正文三。\n第 3 页"
        };

        var result = _normalizer.NormalizeDocument(pages);

        result.Should().Equal("正文一。", "正文二。", "正文三。");
    }

    [Fact]
    public void 页数不足三页_不推断页眉页脚()
    {
        var pages = new[] { "读书笔记\n正文一。\n第 1 页", "读书笔记\n正文二。\n第 2 页" };

        var patterns = _normalizer.DetectHeaderFooterPatterns(pages);

        patterns.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n  \n")]
    [InlineData(null)]
    public void 空文本_返回空字符串(string? text)
    {
        var result = _normalizer.NormalizePage(text);

        result.Should().BeEmpty();
    }

    [Fact]
    public void 英文句号后接小写_视为句中换行并合并()
    {
        var text = "Refer to Fig.\n3 for details.";

        var result = _normalizer.NormalizePage(text);

        result.Should().Be("Refer to Fig. 3 for details.");
    }
}
