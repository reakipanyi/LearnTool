using FluentAssertions;
using LearningAssistant.Models.Audio;
using LearningAssistant.Services.Audio;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Audio;

public class SentenceSrtExporterTests
{
    private static AudioSentenceSpan Span(int start, int end, string? text = null)
        => new() { StartMs = start, EndMs = end, Text = text, TextSource = text == null ? null : "manual" };

    [Fact]
    public void 空列表_导出空字符串()
    {
        SentenceSrtExporter.Format(null).Should().BeEmpty();
        SentenceSrtExporter.Format(Array.Empty<AudioSentenceSpan>()).Should().BeEmpty();
    }

    [Fact]
    public void 按时间排序_并生成连续序号()
    {
        var spans = new[]
        {
            Span(5000, 7000, "第二句"),
            Span(0, 4000, "第一句")
        };

        string srt = SentenceSrtExporter.Format(spans);

        srt.Should().StartWith("1\n00:00:00,000 --> 00:00:04,000\n第一句");
        srt.Should().Contain("2\n00:00:05,000 --> 00:00:07,000\n第二句");
    }

    [Fact]
    public void 无文本句子_使用序号占位()
    {
        string srt = SentenceSrtExporter.Format(new[] { Span(0, 1000) });

        srt.Should().Contain("第 1 句");
    }

    [Fact]
    public void 毫秒时间格式_跨越小时()
    {
        SentenceSrtExporter.FormatTime(3723456).Should().Be("01:02:03,456");
        SentenceSrtExporter.FormatTime(-100).Should().Be("00:00:00,000");
    }

    [Fact]
    public void 无效区间被跳过()
    {
        var spans = new[] { Span(0, 0, "空"), Span(1000, 2000, "有效") };

        string srt = SentenceSrtExporter.Format(spans);

        srt.Should().NotContain("空");
        srt.Should().StartWith("1\n00:00:01,000 --> 00:00:02,000\n有效");
    }
}
