using FluentAssertions;
using LearningAssistant.Common;
using LearningAssistant.Models.Pdf;
using Xunit;

namespace LearningAssistant.Core.Tests.Models.Pdf;

public class PdfAudioModelsTests
{
    [Fact]
    public void AudioTrack_默认未匹配()
    {
        var t = new AudioTrack { FileName = "a.mp3" };
        t.MappedPageStart.Should().Be(-1);
        t.IsManual.Should().BeFalse();
    }

    [Fact]
    public void PdfAudioProfile_Json往返_保留手动覆盖()
    {
        var p = new PdfAudioProfile { PdfPath = @"C:\a.pdf" };
        p.ManualOverrides[3] = "unit3.mp3";
        var json = JsonHelper.Serialize(p);
        var back = JsonHelper.Deserialize<PdfAudioProfile>(json)!;
        back.ManualOverrides[3].Should().Be("unit3.mp3");
    }
}