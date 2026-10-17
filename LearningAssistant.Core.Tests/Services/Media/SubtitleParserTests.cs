using FluentAssertions;
using LearningAssistant.Abstractions.Media;
using LearningAssistant.Services.Media;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Media;

public class SubtitleParserTests
{
    private static string WriteTemp(string content, string ext)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ext);
        File.WriteAllText(path, content, System.Text.Encoding.UTF8);
        return path;
    }

    [Fact]
    public void Srt_标准两幕_解析正确()
    {
        var path = WriteTemp(
            "1\r\n00:00:01,000 --> 00:00:02,500\r\nHello\r\n\r\n2\r\n00:00:03,000 --> 00:00:04,000\r\nWorld\r\n",
            ".srt");
        var cues = new SubtitleParser().Parse(path);
        cues.Count.Should().Be(2);
        cues[0].Text.Should().Be("Hello");
        cues[0].StartTime.Should().Be(TimeSpan.FromSeconds(1));
        cues[1].Text.Should().Be("World");
    }

    [Fact]
    public void Lrc_多标签一行_拆成多条()
    {
        var path = WriteTemp("[00:01.00][00:02.00]你好\n[00:03.00]世界\n", ".lrc");
        var cues = new SubtitleParser().Parse(path);
        cues.Count.Should().Be(3);
        cues[0].Text.Should().Be("你好");
        cues[1].Text.Should().Be("你好");
        cues[2].Text.Should().Be("世界");
    }

    [Fact]
    public void Lrc_无结束时间_取下一幕开始()
    {
        var path = WriteTemp("[00:01.00]A\n[00:02.00]B\n", ".lrc");
        var cues = new SubtitleParser().Parse(path);
        cues[0].EndTime.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void 非法行_跳过不抛异常()
    {
        var path = WriteTemp("[ti:title]\nnot a valid line\n[00:01.00]OK\n", ".lrc");
        var cues = new SubtitleParser().Parse(path);
        cues.Count.Should().Be(1);
        cues[0].Text.Should().Be("OK");
    }
}