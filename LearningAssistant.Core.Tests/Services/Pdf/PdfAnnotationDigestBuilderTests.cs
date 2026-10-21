using FluentAssertions;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Pdf;

public class PdfAnnotationDigestBuilderTests
{
    [Fact]
    public void 采集标注_按时间戳排序并保留页码()
    {
        var annotation = new PdfAnnotation
        {
            Texts =
            {
                new AnnotationText { Content = "复习这一节", AudioTimestampMs = 65_000 },
                new AnnotationText { Content = "无时间戳备注" }
            },
            Strokes =
            {
                new AnnotationStroke { ShapeType = "Rectangle", AudioTimestampMs = 125_000 }
            }
        };

        var items = PdfAnnotationDigestBuilder.Collect(annotation, 2);

        items.Should().HaveCount(3);
        items[0].PageIndex.Should().Be(2);
        items[0].AudioTimestampMs.Should().Be(65_000);
        items[1].Kind.Should().Be("形状-矩形");
        items[2].AudioTimestampMs.Should().BeNull();

        var digest = PdfAnnotationDigestBuilder.Build(items);
        digest.Should().Contain("[[p3]] [01:05] 文字: 复习这一节");
        digest.Should().Contain("[[p3]] [02:05] 形状-矩形");
        digest.Should().Contain("[[p3]] 文字: 无时间戳备注");
    }

    [Fact]
    public void 超过一万字的时间戳_使用时分秒格式()
    {
        PdfAnnotationDigestBuilder.FormatTimestamp(3_725_000).Should().Be("1:02:05");
        PdfAnnotationDigestBuilder.FormatTimestamp(0).Should().Be("00:00");
    }

    [Fact]
    public void 长文本被截断_避免提示词过长()
    {
        var annotation = new PdfAnnotation
        {
            Texts = { new AnnotationText { Content = new string('字', 500) } }
        };

        var items = PdfAnnotationDigestBuilder.Collect(annotation, 0);

        items[0].Text.Length.Should().Be(PdfAnnotationDigestBuilder.MaxTextLength + 1); // 含省略号
        items[0].Text.Should().EndWith("…");
    }

    [Fact]
    public void 空标注_返回空集合与空文本()
    {
        PdfAnnotationDigestBuilder.Collect(null, 0).Should().BeEmpty();
        PdfAnnotationDigestBuilder.Build(Array.Empty<PdfAnnotationDigestItem>()).Should().BeEmpty();
    }

    [Fact]
    public void 条目数量超过上限_只保留前N条()
    {
        var items = Enumerable.Range(0, PdfAnnotationDigestBuilder.MaxItems + 20)
            .Select(i => new PdfAnnotationDigestItem { PageIndex = i, Kind = "文字", Text = "x" });

        var digest = PdfAnnotationDigestBuilder.Build(items);

        digest.Split('\n').Length.Should().Be(PdfAnnotationDigestBuilder.MaxItems);
    }
}
