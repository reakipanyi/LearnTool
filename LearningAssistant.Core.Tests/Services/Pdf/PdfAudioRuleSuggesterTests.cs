using FluentAssertions;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using System.Text.RegularExpressions;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Pdf;

public class PdfAudioRuleSuggesterTests
{
    private static AudioTrack Track(string relativePath) => new()
    {
        RelativePath = relativePath,
        FileName = Path.GetFileName(relativePath),
        FilePath = @"C:\pdf\" + relativePath
    };

    [Fact]
    public void 同前缀数字_归纳为页码直配()
    {
        var tracks = new[] { Track("P12.mp3"), Track("P13.mp3") };
        var overrides = new Dictionary<int, string> { [11] = "P12.mp3", [12] = "P13.mp3" };

        var suggestion = PdfAudioRuleSuggester.Suggest(overrides, tracks)!;

        suggestion.Rule.MapMode.Should().Be(AudioPageMapMode.PageIndex);
        suggestion.Rule.PageOffset.Should().Be(-1);
        new Regex(suggestion.Rule.FileNamePattern, RegexOptions.IgnoreCase)
            .Match("P12.mp3").Groups[1].Value.Should().Be("12");
    }

    [Fact]
    public void 补零命名_正则捕获完整数字()
    {
        var tracks = new[] { Track("Unit01.mp3"), Track("Unit02.mp3") };
        var overrides = new Dictionary<int, string> { [20] = "Unit01.mp3", [21] = "Unit02.mp3" };

        var suggestion = PdfAudioRuleSuggester.Suggest(overrides, tracks)!;

        suggestion.Rule.MapMode.Should().Be(AudioPageMapMode.PageIndex);
        suggestion.Rule.PageOffset.Should().Be(19);
        var regex = new Regex(suggestion.Rule.FileNamePattern, RegexOptions.IgnoreCase);
        regex.Match("Unit02.mp3").Groups[1].Value.Should().Be("02");
    }

    [Fact]
    public void 页码与数字不同步_归纳为顺序映射()
    {
        var tracks = new[] { Track("A5.mp3"), Track("A9.mp3"), Track("A12.mp3") };
        var overrides = new Dictionary<int, string>
        { [0] = "A5.mp3", [1] = "A9.mp3", [2] = "A12.mp3" };

        var suggestion = PdfAudioRuleSuggester.Suggest(overrides, tracks)!;

        suggestion.Rule.MapMode.Should().Be(AudioPageMapMode.Sequence);
        suggestion.Rule.PageOffset.Should().Be(0);
    }

    [Fact]
    public void 前缀不同_仍能生成可用正则()
    {
        var tracks = new[] { Track("Lesson1.mp3"), Track("Unit2.mp3") };
        var overrides = new Dictionary<int, string> { [0] = "Lesson1.mp3", [1] = "Unit2.mp3" };

        var suggestion = PdfAudioRuleSuggester.Suggest(overrides, tracks)!;

        suggestion.Rule.MapMode.Should().Be(AudioPageMapMode.PageIndex);
        suggestion.Rule.PageOffset.Should().Be(-1);
        var regex = new Regex(suggestion.Rule.FileNamePattern, RegexOptions.IgnoreCase);
        regex.Match("Lesson1.mp3").Groups[1].Value.Should().Be("1");
        regex.Match("Unit2.mp3").Groups[1].Value.Should().Be("2");
    }

    [Fact]
    public void 文件名无数字_返回空()
    {
        var tracks = new[] { Track("intro.mp3") };
        var overrides = new Dictionary<int, string> { [0] = "intro.mp3" };

        PdfAudioRuleSuggester.Suggest(overrides, tracks).Should().BeNull();
    }

    [Fact]
    public void 无手动匹配_返回空()
    {
        PdfAudioRuleSuggester.Suggest(new Dictionary<int, string>(), new[] { Track("P1.mp3") })
            .Should().BeNull();
    }
}
