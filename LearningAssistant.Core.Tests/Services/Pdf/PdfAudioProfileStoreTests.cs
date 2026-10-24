using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using LearningAssistant.Common;
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
        var pdf = NewPdfPath();
        try
        {
            var profile = new PdfAudioProfile
            {
                PdfPath = pdf,
                IncludeSubDirectories = true,
                PageCount = 42,
                PlaybackScope = PlaybackScope.WholeBook,
                SingleAudioFile = "full.mp3",
                FollowPage = false,
                SyncPageOnTrackChange = false
            };
            profile.Rules.Add(new AudioMatchRule { Id = "r1", Name = "页码", FileNamePattern = @"(\d+)\.mp3" });
            profile.ManualOverrides[2] = "u2.mp3";
            profile.PageIndex[0] = "u1.mp3";
            profile.PageIndex[1] = "u2.mp3";
            store.Save(profile);

            var loaded = store.Load(pdf).Profile!;
            loaded.PdfPath.Should().Be(pdf);
            loaded.IncludeSubDirectories.Should().BeTrue();
            loaded.ManualOverrides[2].Should().Be("u2.mp3");
            loaded.PageCount.Should().Be(42);
            loaded.PageIndex[0].Should().Be("u1.mp3");
            loaded.PageIndex[1].Should().Be("u2.mp3");
            loaded.Rules.Should().ContainSingle(r => r.Id == "r1");
            loaded.PlaybackScope.Should().Be(PlaybackScope.WholeBook);
            loaded.SingleAudioFile.Should().Be("full.mp3");
            loaded.MatchMode.Should().Be(PdfAudioMatchMode.SingleAudio);
            loaded.FollowPage.Should().BeFalse();
            loaded.SyncPageOnTrackChange.Should().BeFalse();
        }
        finally
        {
            Cleanup(store, pdf);
        }
    }

    [Fact]
    public void 加载不存在_返回空结果()
    {
        var store = new PdfAudioProfileStore();
        var pdf = NewPdfPath();
        var result = store.Load(pdf);
        result.Profile.Should().BeNull();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void 同目录多书_共享目录规则且书级覆盖不互相污染()
    {
        var store = new PdfAudioProfileStore();
        var pdfA = NewPdfPath();
        var pdfB = Path.Combine(Path.GetDirectoryName(pdfA)!, "b.pdf");
        try
        {
            var a = new PdfAudioProfile { PdfPath = pdfA, PageCount = 10 };
            a.Rules.Add(new AudioMatchRule { Id = "shared", Name = "共享规则", FileNamePattern = @"p(\d+)" });
            a.ManualOverrides[0] = "a0.mp3";
            store.Save(a);

            var b = store.Load(pdfB).Profile!;
            b.Rules.Should().ContainSingle(r => r.Id == "shared");
            b.ManualOverrides.Should().BeEmpty();

            b.ManualOverrides[5] = "b5.mp3";
            store.Save(b);

            var aAgain = store.Load(pdfA).Profile!;
            aAgain.Rules.Should().ContainSingle(r => r.Id == "shared");
            aAgain.ManualOverrides.Should().ContainKey(0);
            aAgain.ManualOverrides.Should().NotContainKey(5);

            var bAgain = store.Load(pdfB).Profile!;
            bAgain.ManualOverrides.Should().ContainKey(5);
            bAgain.ManualOverrides.Should().NotContainKey(0);
        }
        finally
        {
            Cleanup(store, pdfA);
        }
    }

    [Fact]
    public void 损坏档案_返回可读错误()
    {
        var store = new PdfAudioProfileStore();
        var pdf = NewPdfPath();
        try
        {
            var path = store.GetProfilePath(pdf);
            AppPaths.EnsureDirectoryExists(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "{ 这不是合法 JSON");

            var result = store.Load(pdf);
            result.Profile.Should().BeNull();
            result.Error.Should().NotBeNullOrWhiteSpace();
            result.Error.Should().Contain("解析失败");
        }
        finally
        {
            Cleanup(store, pdf);
        }
    }

    [Fact]
    public void 旧版一书一档_首次打开迁移为目录档案()
    {
        var store = new PdfAudioProfileStore();
        var pdf = NewPdfPath();
        var legacyPath = LegacyPath(pdf);
        try
        {
            AppPaths.EnsureDirectoryExists(Path.GetDirectoryName(legacyPath)!);
            var legacy = new PdfAudioProfile { PdfPath = pdf, PageCount = 7 };
            legacy.Rules.Add(new AudioMatchRule { Id = "legacy", Name = "旧规则", FileNamePattern = @"(\d+)" });
            legacy.ManualOverrides[3] = "m3.mp3";
            JsonHelper.SaveToFile(legacyPath, legacy);

            var result = store.Load(pdf);
            result.Error.Should().BeNull();
            result.Migrated.Should().BeTrue();
            result.Profile.Should().NotBeNull();
            result.Profile!.Rules.Should().ContainSingle(r => r.Id == "legacy");
            result.Profile.ManualOverrides[3].Should().Be("m3.mp3");
            result.Profile.PageCount.Should().Be(7);
            File.Exists(store.GetProfilePath(pdf)).Should().BeTrue();
            File.Exists(legacyPath).Should().BeFalse("旧档案应移动到 _legacy");
        }
        finally
        {
            Cleanup(store, pdf);
            if (File.Exists(legacyPath)) File.Delete(legacyPath);
        }
    }

    private static string NewPdfPath()
        => Path.Combine(Path.GetTempPath(), "lt_pdfaudio_" + Guid.NewGuid().ToString("N"), "a.pdf");

    private static string LegacyPath(string pdfPath)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(pdfPath))))[..16];
        return Path.Combine(AppPaths.AudioProfilesDir, hash + ".json");
    }

    private static void Cleanup(PdfAudioProfileStore store, string pdfPath)
    {
        var directoryProfile = store.GetProfilePath(pdfPath);
        if (File.Exists(directoryProfile)) File.Delete(directoryProfile);
        var legacyArchive = Path.Combine(AppPaths.AudioProfilesDir, "_legacy",
            Path.GetFileName(LegacyPath(pdfPath)));
        if (File.Exists(legacyArchive)) File.Delete(legacyArchive);
    }
}
