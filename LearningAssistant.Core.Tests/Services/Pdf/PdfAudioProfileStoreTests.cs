using FluentAssertions;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Pdf;

public class PdfAudioProfileStoreTests
{
    [Fact]
    public void 保存再加载_字段一致()
    {
        var store = new PdfAudioProfileStore();
        var profile = new PdfAudioProfile { PdfPath = @"C:\x\a.pdf", IncludeSubDirectories = true, PageCount = 42 };
        profile.ManualOverrides[2] = "u2.mp3";
        profile.PageIndex[0] = "u1.mp3";
        profile.PageIndex[1] = "u2.mp3";
        store.Save(profile);
        var loaded = store.Load(@"C:\x\a.pdf")!;
        loaded.PdfPath.Should().Be(@"C:\x\a.pdf");
        loaded.IncludeSubDirectories.Should().BeTrue();
        loaded.ManualOverrides[2].Should().Be("u2.mp3");
        loaded.PageCount.Should().Be(42);
        loaded.PageIndex[0].Should().Be("u1.mp3");
        loaded.PageIndex[1].Should().Be("u2.mp3");
    }

    [Fact]
    public void 加载不存在_返回null()
    {
        var store = new PdfAudioProfileStore();
        store.Load(@"C:\x\not_exist.pdf").Should().BeNull();
    }
}