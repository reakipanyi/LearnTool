using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Pdf;

public class PdfTextBlockBuilderTests
{
    [Fact]
    public void Build_EmptyLines_ReturnsEmpty()
    {
        Assert.Empty(PdfTextBlockBuilder.Build(Array.Empty<PdfTextLine>()));
        Assert.Empty(PdfTextBlockBuilder.Build(null));
    }

    [Fact]
    public void Build_EnglishParagraph_MergesWrappedLines()
    {
        var lines = new[]
        {
            Line("This is the first line of a paragraph", 0.1f, 0.10f, 0.6f, 0.02f),
            Line("that continues on the second line and", 0.1f, 0.125f, 0.62f, 0.02f),
            Line("finishes here.", 0.1f, 0.15f, 0.4f, 0.02f)
        };

        var blocks = PdfTextBlockBuilder.Build(lines);

        var block = Assert.Single(blocks);
        Assert.Contains("first line", block.Text);
        Assert.Contains("finishes here.", block.Text);
        Assert.Equal(3, block.LineCount);
    }

    [Fact]
    public void Build_ChineseSentences_SplitAtSentenceEnd()
    {
        var lines = new[]
        {
            Line("第一句话。", 0.1f, 0.10f, 0.5f, 0.02f),
            Line("第二句话。", 0.1f, 0.125f, 0.5f, 0.02f)
        };

        var blocks = PdfTextBlockBuilder.Build(lines);

        Assert.Equal(2, blocks.Count);
        Assert.Equal("第一句话。", blocks[0].Text);
        Assert.Equal("第二句话。", blocks[1].Text);
    }

    [Fact]
    public void Build_MergeMultiLineDisabled_KeepsOneBlockPerLine()
    {
        var lines = new[]
        {
            Line("This is the first line of a paragraph", 0.1f, 0.10f, 0.6f, 0.02f),
            Line("that continues on the second line and", 0.1f, 0.125f, 0.62f, 0.02f),
            Line("finishes here.", 0.1f, 0.15f, 0.4f, 0.02f)
        };

        var blocks = PdfTextBlockBuilder.Build(lines, new PdfTextBlockBuildOptions { MergeMultiLine = false });

        Assert.Equal(3, blocks.Count);
        Assert.Equal("This is the first line of a paragraph", blocks[0].Text);
        Assert.Equal("finishes here.", blocks[2].Text);
        Assert.All(blocks, block => Assert.Equal(1, block.LineCount));
    }

    [Fact]
    public void Build_MergeMultiLineDisabled_StillJoinsSameVisualLine()
    {
        var lines = new[]
        {
            Line("Hello", 0.10f, 0.10f, 0.10f, 0.02f),
            Line("world", 0.22f, 0.10f, 0.10f, 0.02f)
        };

        var blocks = PdfTextBlockBuilder.Build(lines, new PdfTextBlockBuildOptions { MergeMultiLine = false });

        var block = Assert.Single(blocks);
        Assert.Equal("Hello world", block.Text);
    }

    [Fact]
    public void Build_PageNumbersInEdgeBands_AreRemoved()
    {
        var lines = new[]
        {
            Line("第 3 页", 0.45f, 0.02f, 0.1f, 0.015f),
            Line("正文第一段内容", 0.1f, 0.40f, 0.5f, 0.02f),
            Line("12", 0.48f, 0.96f, 0.04f, 0.015f)
        };

        var blocks = PdfTextBlockBuilder.Build(lines);

        var block = Assert.Single(blocks);
        Assert.Equal("正文第一段内容", block.Text);
    }

    [Fact]
    public void Build_NumberInsideBody_IsKept()
    {
        var lines = new[]
        {
            Line("2024", 0.1f, 0.40f, 0.1f, 0.02f),
            Line("年度总结", 0.12f, 0.425f, 0.3f, 0.02f)
        };

        var blocks = PdfTextBlockBuilder.Build(lines);

        var block = Assert.Single(blocks);
        Assert.Contains("2024", block.Text);
        Assert.Contains("年度总结", block.Text);
    }

    [Fact]
    public void Build_SmallHeaderInTopBand_IsRemoved()
    {
        var lines = new[]
        {
            Line("Chapter 1 Introduction", 0.1f, 0.015f, 0.5f, 0.008f),
            Line("This is the body text", 0.1f, 0.30f, 0.6f, 0.02f),
            Line("and it continues here.", 0.1f, 0.33f, 0.6f, 0.02f)
        };

        var blocks = PdfTextBlockBuilder.Build(lines);

        var block = Assert.Single(blocks);
        Assert.DoesNotContain("Chapter 1", block.Text);
        Assert.Contains("body text", block.Text);
    }

    [Fact]
    public void Build_TwoColumns_DoNotMergeAcrossColumns()
    {
        var lines = new[]
        {
            Line("Left column line one", 0.08f, 0.10f, 0.35f, 0.02f),
            Line("left column line two", 0.08f, 0.125f, 0.35f, 0.02f),
            Line("Right column line one", 0.55f, 0.10f, 0.36f, 0.02f),
            Line("right column line two", 0.55f, 0.125f, 0.36f, 0.02f)
        };

        var blocks = PdfTextBlockBuilder.Build(lines);

        Assert.Equal(2, blocks.Count);
        Assert.Contains("Left column", blocks[0].Text);
        Assert.Contains("Right column", blocks[1].Text);
        Assert.DoesNotContain("Right", blocks[0].Text);
    }

    [Fact]
    public void Build_ListMarker_StartsNewBlock()
    {
        var lines = new[]
        {
            Line("段落正文没有结束符号", 0.1f, 0.10f, 0.6f, 0.02f),
            Line("1. 列表项内容", 0.1f, 0.125f, 0.5f, 0.02f)
        };

        var blocks = PdfTextBlockBuilder.Build(lines);

        Assert.Equal(2, blocks.Count);
        Assert.StartsWith("1.", blocks[1].Text);
    }

    [Fact]
    public void Build_HeadingFontJump_StartsNewBlock()
    {
        var lines = new[]
        {
            Line("Body text of the paragraph", 0.1f, 0.10f, 0.6f, 0.02f),
            Line("Section Heading", 0.1f, 0.125f, 0.4f, 0.035f)
        };

        var blocks = PdfTextBlockBuilder.Build(lines);

        Assert.Equal(2, blocks.Count);
        Assert.Equal("Section Heading", blocks[1].Text);
    }

    [Fact]
    public void Build_HyphenatedLine_IsRepaired()
    {
        var lines = new[]
        {
            Line("This is an inter-", 0.1f, 0.10f, 0.5f, 0.02f),
            Line("national paragraph.", 0.1f, 0.125f, 0.5f, 0.02f)
        };

        var blocks = PdfTextBlockBuilder.Build(lines);

        var block = Assert.Single(blocks);
        Assert.Contains("international paragraph.", block.Text);
    }

    [Fact]
    public void Build_BlockBounds_StayInsidePage()
    {
        var lines = new[]
        {
            Line("A paragraph near the edge", 0.9f, 0.9f, 0.2f, 0.05f)
        };

        var blocks = PdfTextBlockBuilder.Build(lines);

        var block = Assert.Single(blocks);
        Assert.InRange(block.X, 0f, 1f);
        Assert.InRange(block.Y, 0f, 1f);
        Assert.InRange(block.X + block.Width, 0f, 1f);
        Assert.InRange(block.Y + block.Height, 0f, 1f);
    }

    private static PdfTextLine Line(string text, float x, float y, float width, float height)
    {
        return new PdfTextLine
        {
            Text = text,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            FontSize = height * 800f
        };
    }
}
