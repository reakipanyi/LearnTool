using FluentAssertions;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Pdf;

public class PdfAudioDirectoryScannerTests
{
    [Fact]
    public void 扫描同目录_命中间隔并在返回相对路径与字幕sidecar()
    {
        var dir = Path.Combine(Path.GetTempPath(), "scan_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "Unit1.mp3"), "x");
        File.WriteAllText(Path.Combine(dir, "Unit1.lrc"), "[00:01.00]hi");
        File.WriteAllText(Path.Combine(dir, "note.txt"), "x");

        var tracks = new PdfAudioDirectoryScanner().Scan(dir, includeSubDirectories: false);

        tracks.Count.Should().Be(1);
        tracks[0].RelativePath.Should().Be("Unit1.mp3");
        tracks[0].SubtitlePath.Should().EndWith("Unit1.lrc");
    }

    [Fact]
    public void 递归扫描_相对路径含子目录()
    {
        var dir = Path.Combine(Path.GetTempPath(), "scan_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "sub"));
        File.WriteAllText(Path.Combine(dir, "sub", "a.mp3"), "x");

        var tracks = new PdfAudioDirectoryScanner().Scan(dir, includeSubDirectories: true);

        tracks.Count.Should().Be(1);
        tracks[0].RelativePath.Should().Be(Path.Combine("sub", "a.mp3"));
    }
}