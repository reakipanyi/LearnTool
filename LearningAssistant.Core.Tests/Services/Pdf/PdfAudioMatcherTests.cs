using FluentAssertions;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Pdf;

public class PdfAudioMatcherTests
{
    private static AudioTrack T(string fileName) => new() { FileName = fileName, FilePath = @"C:\pdf\" + fileName, RelativePath = fileName };
    private static readonly PdfAudioMatcher Matcher = new();

    [Fact]
    public void PageIndex_P012_映射到页11()
    {
        var rule = new AudioMatchRule { FileNamePattern = @"P(\d+)", MapMode = AudioPageMapMode.PageIndex, CaptureGroupIndex = 1 };
        var r = Matcher.Match(@"C:\pdf\a.pdf", 20, new[] { T("P12.mp3") }, new[] { rule }, null);
        r.PageIndex[11].FileName.Should().Be("P12.mp3");
    }

    [Fact]
    public void Sequence_按捕获值排序_映射前三页()
    {
        var rule = new AudioMatchRule { FileNamePattern = @"(\d+)\.mp3", MapMode = AudioPageMapMode.Sequence };
        var r = Matcher.Match(@"C:\pdf\a.pdf", 10,
            new[] { T("02.mp3"), T("01.mp3"), T("03.mp3") }, new[] { rule }, null);
        r.PageIndex[0].FileName.Should().Be("01.mp3");
        r.PageIndex[2].FileName.Should().Be("03.mp3");
    }

    [Fact]
    public void Custom模式_跳过并记录诊断()
    {
        var rule = new AudioMatchRule { Name = "自定义", FileNamePattern = @"(\d+)", MapMode = AudioPageMapMode.Custom };
        var r = Matcher.Match(@"C:\pdf\a.pdf", 10, new[] { T("01.mp3") }, new[] { rule }, null);
        r.PageIndex.Should().BeEmpty();
        Matcher.Diagnostics.Should().Contain(d => d.Contains("Custom"));
    }

    [Fact]
    public void Range_Unit3每单元10页_映射21到30()
    {
        var rule = new AudioMatchRule { FileNamePattern = @"Unit(\d+)", MapMode = AudioPageMapMode.Range, PagesPerUnit = 10 };
        var r = Matcher.Match(@"C:\pdf\a.pdf", 50, new[] { T("Unit3.mp3") }, new[] { rule }, null);
        r.PageIndex[20].FileName.Should().Be("Unit3.mp3");   // 显示 21
        r.PageIndex[29].FileName.Should().Be("Unit3.mp3");   // 显示 30
    }

    [Fact]
    public void 手动覆盖_无条件覆盖正则结果()
    {
        var rule = new AudioMatchRule { FileNamePattern = @"P(\d+)", MapMode = AudioPageMapMode.PageIndex };
        var overrides = new Dictionary<int, string> { [11] = "manual.mp3" };
        var r = Matcher.Match(@"C:\pdf\a.pdf", 20, new[] { T("P12.mp3"), T("manual.mp3") }, new[] { rule }, overrides);
        r.PageIndex[11].FileName.Should().Be("manual.mp3");
        r.PageIndex[11].IsManual.Should().BeTrue();
    }

    [Fact]
    public void 手动覆盖_目录外绝对路径不被同名曲目顶替()
    {
        var overrides = new Dictionary<int, string> { [0] = @"D:\other\manual.mp3" };
        var r = Matcher.Match(@"C:\pdf\a.pdf", 5, new[] { T("manual.mp3") },
            Array.Empty<AudioMatchRule>(), overrides);
        r.PageIndex[0].FilePath.Should().Be(@"D:\other\manual.mp3");
    }

    [Fact]
    public void 非法正则_跳过不抛异常()
    {
        var rule = new AudioMatchRule { FileNamePattern = "(" };
        Action act = () => Matcher.Match(@"C:\pdf\a.pdf", 5, new[] { T("P1.mp3") }, new[] { rule }, null);
        act.Should().NotThrow();
    }

    [Fact]
    public void 未匹配页集合正确()
    {
        var rule = new AudioMatchRule { FileNamePattern = @"P(\d+)", MapMode = AudioPageMapMode.PageIndex };
        var r = Matcher.Match(@"C:\pdf\a.pdf", 3, new[] { T("P1.mp3") }, new[] { rule }, null);
        r.UnmatchedPages.Should().Contain(1).And.Contain(2);
    }

    [Fact]
    public void 文件名优先_目录中的数字不干扰匹配()
    {
        var track = new AudioTrack
        {
            FileName = "P5.mp3",
            RelativePath = @"第2单元\P5.mp3",
            FilePath = @"C:\pdf\第2单元\P5.mp3"
        };
        var rule = new AudioMatchRule { FileNamePattern = @"(\d+)", MapMode = AudioPageMapMode.PageIndex };

        var r = Matcher.Match(@"C:\pdf\a.pdf", 20, new[] { track }, new[] { rule }, null);

        r.PageIndex[4].FileName.Should().Be("P5.mp3");
    }

    [Fact]
    public void 通配符模式_无捕获组时按文件名数字映射()
    {
        var rule = new AudioMatchRule
        {
            PatternMode = AudioPatternMode.Wildcard,
            FileNamePattern = "*.mp3",
            MapMode = AudioPageMapMode.PageIndex
        };

        var r = Matcher.Match(@"C:\pdf\a.pdf", 20, new[] { T("P12.mp3") }, new[] { rule }, null);

        r.PageIndex[11].FileName.Should().Be("P12.mp3");
    }

    [Fact]
    public void 正则未写捕获组_自动取文件名第一个数字()
    {
        var rule = new AudioMatchRule { FileNamePattern = @"P\d+", MapMode = AudioPageMapMode.PageIndex };

        var r = Matcher.Match(@"C:\pdf\a.pdf", 20, new[] { T("P12.mp3") }, new[] { rule }, null);

        r.PageIndex[11].FileName.Should().Be("P12.mp3");
    }

    [Fact]
    public void 非法正则含星号_自动按通配符解释()
    {
        var rule = new AudioMatchRule { Name = "星号误写", FileNamePattern = "*.mp3", MapMode = AudioPageMapMode.PageIndex };

        var r = Matcher.Match(@"C:\pdf\a.pdf", 20, new[] { T("P12.mp3") }, new[] { rule }, null);

        r.PageIndex[11].FileName.Should().Be("P12.mp3");
        Matcher.Diagnostics.Should().Contain(d => d.Contains("通配符"));
    }

    [Fact]
    public void 规则未命中任何音频_输出诊断()
    {
        var rule = new AudioMatchRule { Name = "WAV规则", FileNamePattern = "*.wav" };

        Matcher.Match(@"C:\pdf\a.pdf", 5, new[] { T("P1.mp3") }, new[] { rule }, null);

        Matcher.Diagnostics.Should().Contain(d => d.Contains("未匹配到任何音频") && d.Contains("P1.mp3"));
    }

    [Fact]
    public void 无规则时_按文件名第一个数字自动匹配()
    {
        var r = Matcher.Match(@"C:\pdf\a.pdf", 20,
            new[] { T("第3课.mp3"), T("P12.mp3") }, Array.Empty<AudioMatchRule>(), null);

        r.PageIndex[2].FileName.Should().Be("第3课.mp3");
        r.PageIndex[11].FileName.Should().Be("P12.mp3");
        Matcher.Diagnostics.Should().Contain(d => d.Contains("自动匹配"));
    }

    [Fact]
    public void 无规则时_文件名优先_目录数字不干扰()
    {
        var track = new AudioTrack
        {
            FileName = "P5.mp3",
            RelativePath = @"第2单元\P5.mp3",
            FilePath = @"C:\pdf\第2单元\P5.mp3"
        };

        var r = Matcher.Match(@"C:\pdf\a.pdf", 20, new[] { track },
            Array.Empty<AudioMatchRule>(), null);

        r.PageIndex[4].FileName.Should().Be("P5.mp3");
        r.PageIndex.Should().NotContainKey(1);
    }

    [Fact]
    public void 全部规则禁用时_仍走默认自动匹配()
    {
        var rule = new AudioMatchRule { FileNamePattern = @"P(\d+)", Enabled = false };

        var r = Matcher.Match(@"C:\pdf\a.pdf", 20, new[] { T("P8.mp3") }, new[] { rule }, null);

        r.PageIndex[7].FileName.Should().Be("P8.mp3");
    }

    [Fact]
    public void 启用规则优先于默认自动匹配()
    {
        var rule = new AudioMatchRule
        {
            FileNamePattern = @"P(\d+)",
            MapMode = AudioPageMapMode.PageIndex,
            PageOffset = 1
        };

        var r = Matcher.Match(@"C:\pdf\a.pdf", 20, new[] { T("P5.mp3") }, new[] { rule }, null);

        r.PageIndex[5].FileName.Should().Be("P5.mp3");
        r.PageIndex.Should().NotContainKey(4);
    }

    [Fact]
    public void 手动覆盖优先于默认自动匹配()
    {
        var overrides = new Dictionary<int, string> { [3] = "P9.mp3" };

        var r = Matcher.Match(@"C:\pdf\a.pdf", 20, new[] { T("P4.mp3"), T("P9.mp3") },
            Array.Empty<AudioMatchRule>(), overrides);

        r.PageIndex[3].FileName.Should().Be("P9.mp3");
        r.PageIndex[3].IsManual.Should().BeTrue();
    }

    [Fact]
    public void 无规则时_超出页数范围的文件不映射()
    {
        var r = Matcher.Match(@"C:\pdf\a.pdf", 5, new[] { T("P99.mp3") },
            Array.Empty<AudioMatchRule>(), null);

        r.PageIndex.Should().BeEmpty();
        r.UnmatchedPages.Should().HaveCount(5);
    }
}