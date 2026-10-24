using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    public void 按书记忆播放进度与倍速_持久化()
    {
        var store = new PdfAudioProfileStore();
        var pdf = NewPdfPath();
        try
        {
            var profile = new PdfAudioProfile
            {
                PdfPath = pdf,
                PageCount = 10,
                PlaybackSpeed = 1.25,
                LastPositionSeconds = 42.5,
                LastTrackPath = "audio/01.mp3"
            };
            store.Save(profile);

            var loaded = store.Load(pdf).Profile!;
            loaded.PlaybackSpeed.Should().Be(1.25);
            loaded.LastPositionSeconds.Should().Be(42.5);
            loaded.LastTrackPath.Should().Be("audio/01.mp3");
        }
        finally
        {
            Cleanup(store, pdf);
        }
    }

    [Fact]
    public void 文件改名_按指纹找回书级配置()
    {
        var store = new PdfAudioProfileStore();
        var pdf = NewPdfPath();
        var renamed = Path.Combine(Path.GetDirectoryName(pdf)!, "renamed.pdf");
        try
        {
            // 指纹基于真实文件内容，先写入一份假 PDF
            File.WriteAllBytes(pdf, Encoding.UTF8.GetBytes("fake pdf content"));
            var profile = new PdfAudioProfile { PdfPath = pdf, PageCount = 12, FollowPage = false };
            profile.ManualOverrides[4] = "m4.mp3";
            store.Save(profile);

            File.Move(pdf, renamed);
            var loaded = store.Load(renamed, 12).Profile!;
            loaded.FollowPage.Should().BeFalse();
            loaded.ManualOverrides[4].Should().Be("m4.mp3");
        }
        finally
        {
            Cleanup(store, pdf);
            if (File.Exists(renamed)) File.Delete(renamed);
        }
    }

    [Fact]
    public void 目录移动后_按文件名重叠率重挂档案()
    {
        var store = new PdfAudioProfileStore();
        var sourceDir = NewTempDir();
        var targetDir = NewTempDir();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var firstName = $"reattach_{suffix}_a.pdf";
        var secondName = $"reattach_{suffix}_b.pdf";
        var sourcePdf = Path.Combine(sourceDir, firstName);
        var sourceSecond = Path.Combine(sourceDir, secondName);
        var targetPdf = Path.Combine(targetDir, firstName);
        var targetSecond = Path.Combine(targetDir, secondName);
        try
        {
            foreach (var path in new[] { sourcePdf, sourceSecond, targetPdf, targetSecond })
                File.WriteAllBytes(path, Encoding.UTF8.GetBytes("fake pdf content"));

            var first = new PdfAudioProfile { PdfPath = sourcePdf, PageCount = 10 };
            first.ManualOverrides[2] = "02.mp3";
            store.Save(first);
            store.Save(new PdfAudioProfile { PdfPath = sourceSecond, PageCount = 10 });

            var candidate = store.FindReattachCandidates(targetPdf)
                .Single(c => c.CurrentBookCount == 2);
            candidate.OverlapRatio.Should().Be(1.0);
            candidate.SharedBooks.Should().Be(2);

            store.ReattachDirectory(targetPdf, candidate.ProfilePath).Should().BeTrue();
            File.Exists(candidate.ProfilePath).Should().BeFalse();

            var loaded = store.Load(targetPdf, 10).Profile!;
            loaded.ManualOverrides[2].Should().Be("02.mp3");
        }
        finally
        {
            Cleanup(store, sourcePdf);
            Cleanup(store, targetPdf);
            if (Directory.Exists(sourceDir)) Directory.Delete(sourceDir, true);
            if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
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

    [Fact]
    public void 导出目录档案_保留用户字段且剔除缓存()
    {
        var store = new PdfAudioProfileStore();
        var pdf = NewPdfPath();
        try
        {
            var profile = new PdfAudioProfile { PdfPath = pdf, PageCount = 5 };
            profile.Rules.Add(new AudioMatchRule { Id = "r1", Name = "规则", FileNamePattern = @"(\d+)" });
            profile.ManualOverrides[2] = "m2.mp3";
            profile.PageIndex[0] = "u1.mp3";
            store.Save(profile);

            var export = store.ExportDirectory(pdf);
            export.Success.Should().BeTrue();
            export.Json.Should().NotBeNullOrWhiteSpace();
            export.Json.Should().NotContain("cache");
            export.Json.Should().NotContain("Cache");
            export.SuggestedFileName.Should().EndWith(".audio.json");

            using var document = JsonDocument.Parse(export.Json!);
            document.RootElement.GetProperty("kind").GetString().Should().Be("directory");
            document.RootElement.GetProperty("books").TryGetProperty("a.pdf", out var book).Should().BeTrue();
            book.GetProperty("overrides").TryGetProperty("2", out _).Should().BeTrue();
        }
        finally
        {
            Cleanup(store, pdf);
        }
    }

    [Fact]
    public void 导入单书档案_只合并同名书且保留本机缓存()
    {
        var store = new PdfAudioProfileStore();
        var sourceDir = NewTempDir();
        var targetDir = NewTempDir();
        var sourcePdf = Path.Combine(sourceDir, "a.pdf");
        var targetA = Path.Combine(targetDir, "a.pdf");
        var targetB = Path.Combine(targetDir, "b.pdf");
        try
        {
            var source = new PdfAudioProfile
            {
                PdfPath = sourcePdf,
                PageCount = 5,
                SingleAudioFile = "full.mp3",
                PlaybackScope = PlaybackScope.WholeBook
            };
            source.ManualOverrides[1] = "s1.mp3";
            store.Save(source);

            var export = store.ExportBook(sourcePdf);
            export.Success.Should().BeTrue();

            var a = new PdfAudioProfile { PdfPath = targetA, PageCount = 5 };
            a.PageIndex[0] = "local.mp3";
            store.Save(a);
            var b = new PdfAudioProfile { PdfPath = targetB, PageCount = 8, SingleAudioFile = "b-full.mp3" };
            b.ManualOverrides[3] = "b3.mp3";
            store.Save(b);

            var preview = store.PreviewImport(targetA, export.Json!);
            preview.Success.Should().BeTrue(preview.Error);
            preview.Kind.Should().Be("book");

            store.ApplyImport(targetA, preview);

            var loadedA = store.Load(targetA).Profile!;
            loadedA.SingleAudioFile.Should().Be("full.mp3");
            loadedA.PlaybackScope.Should().Be(PlaybackScope.WholeBook);
            loadedA.ManualOverrides[1].Should().Be("s1.mp3");
            loadedA.PageIndex[0].Should().Be("local.mp3", "导入保留本机缓存");

            var loadedB = store.Load(targetB).Profile!;
            loadedB.SingleAudioFile.Should().Be("b-full.mp3");
            loadedB.ManualOverrides[3].Should().Be("b3.mp3");
        }
        finally
        {
            Cleanup(store, sourcePdf);
            Cleanup(store, targetA);
            Cleanup(store, targetB);
        }
    }

    [Fact]
    public void 导入档案_版本过新拒绝()
    {
        var store = new PdfAudioProfileStore();
        var pdf = NewPdfPath();
        try
        {
            var json = """
                       { "schemaVersion": 4, "kind": "book", "books": { "a.pdf": {} } }
                       """;
            var preview = store.PreviewImport(pdf, json);
            preview.Success.Should().BeFalse();
            preview.Error.Should().Contain("高于当前支持版本");
        }
        finally
        {
            Cleanup(store, pdf);
        }
    }

    [Fact]
    public void 导入单书档案_书名不一致时改落到当前书()
    {
        var store = new PdfAudioProfileStore();
        var pdf = NewPdfPath();
        try
        {
            var json = """
                       {
                         "schemaVersion": 3,
                         "kind": "book",
                         "books": {
                           "other.pdf": {
                             "overrides": { "1": { "path": "p2.mp3", "source": "manual" } }
                           }
                         }
                       }
                       """;
            var preview = store.PreviewImport(pdf, json);
            preview.Success.Should().BeTrue(preview.Error);
            preview.Warnings.Should().Contain(w => w.Contains("原属"));

            store.ApplyImport(pdf, preview);

            var loaded = store.Load(pdf).Profile!;
            loaded.ManualOverrides[1].Should().Be("p2.mp3");
        }
        finally
        {
            Cleanup(store, pdf);
        }
    }

    [Fact]
    public void 手动编辑档案原文_校验后保存并生效()
    {
        var store = new PdfAudioProfileStore();
        var pdf = NewPdfPath();
        try
        {
            var profile = new PdfAudioProfile { PdfPath = pdf, PageCount = 10 };
            profile.Rules.Add(new AudioMatchRule { Id = "r1", Name = "原文规则", FileNamePattern = @"a(\d+)" });
            store.Save(profile);

            var read = store.ReadDirectoryJson(pdf);
            read.Success.Should().BeTrue(read.Error);

            var edited = read.Json!.Replace(@"a(\\d+)", @"b(\\d+)");
            var saved = store.SaveDirectoryJson(pdf, edited);
            saved.Success.Should().BeTrue(saved.Error);

            var loaded = store.Load(pdf).Profile!;
            loaded.Rules.Single().FileNamePattern.Should().Be(@"b(\d+)");

            store.SaveDirectoryJson(pdf, "{ bad json").Success.Should().BeFalse();
        }
        finally
        {
            Cleanup(store, pdf);
        }
    }

    [Fact]
    public void 规则补丁_持久化并按目录规则合并()
    {
        var store = new PdfAudioProfileStore();
        var pdf = NewPdfPath();
        var other = Path.Combine(Path.GetDirectoryName(pdf)!, "b.pdf");
        try
        {
            var seed = new PdfAudioProfile { PdfPath = pdf, PageCount = 10 };
            seed.Rules.Add(new AudioMatchRule { Id = "r1", Name = "保留", FileNamePattern = @"a(\d+)" });
            seed.Rules.Add(new AudioMatchRule { Id = "r2", Name = "禁用", FileNamePattern = @"b(\d+)" });
            store.Save(seed);

            var profile = store.Load(pdf).Profile!;
            profile.RulePatch = new PdfAudioRulePatch
            {
                DisabledRuleIds = new List<string> { "r2" },
                ExtraRules = new List<AudioMatchRule>
                {
                    new() { Id = "r3", Name = "本书追加", FileNamePattern = @"c(\d+)" }
                }
            };
            profile.Rules = PdfAudioRuleMerger.Merge(profile.DirectoryRules, profile.RulePatch);
            store.Save(profile);

            var loaded = store.Load(pdf).Profile!;
            loaded.RulePatch.Should().NotBeNull();
            loaded.DirectoryRules.Select(r => r.Id).Should().BeEquivalentTo(new[] { "r1", "r2" });
            loaded.Rules.Select(r => r.Id).Should().BeEquivalentTo(new[] { "r1", "r3" });

            // 同目录其它书保存时只动自己的分区，不应覆盖目录规则或本书补丁
            var otherProfile = store.Load(other).Profile!;
            otherProfile.ManualOverrides[0] = "b0.mp3";
            store.Save(otherProfile);

            var again = store.Load(pdf).Profile!;
            again.RulePatch.Should().NotBeNull();
            again.DirectoryRules.Select(r => r.Id).Should().BeEquivalentTo(new[] { "r1", "r2" });
            again.Rules.Select(r => r.Id).Should().BeEquivalentTo(new[] { "r1", "r3" });
        }
        finally
        {
            Cleanup(store, pdf);
            Cleanup(store, other);
        }
    }

    [Fact]
    public void 导入目录档案_规则按id合并且不覆盖其它书()
    {
        var store = new PdfAudioProfileStore();
        var directory = NewTempDir();
        var pdfA = Path.Combine(directory, "a.pdf");
        var pdfB = Path.Combine(directory, "b.pdf");
        try
        {
            var a = new PdfAudioProfile { PdfPath = pdfA, PageCount = 5 };
            a.Rules.Add(new AudioMatchRule { Id = "r1", Name = "旧规则一", FileNamePattern = @"old(\d+)" });
            a.Rules.Add(new AudioMatchRule { Id = "r3", Name = "本地规则三", FileNamePattern = @"local(\d+)" });
            a.ManualOverrides[0] = "a0.mp3";
            a.PageIndex[0] = "a0.mp3";
            store.Save(a);

            var b = store.Load(pdfB).Profile!;
            b.PageCount = 8;
            b.SingleAudioFile = "b-full.mp3";
            b.ManualOverrides[2] = "b2.mp3";
            store.Save(b);

            var json = """
                       {
                         "schemaVersion": 3,
                         "kind": "directory",
                         "sources": ["audio", "D:\\ext\\audio"],
                         "defaults": { "playbackScope": "FollowPage", "includeSubDirectories": true },
                         "rules": [
                           { "id": "r1", "name": "新规则一", "fileNamePattern": "new(\\d+)", "patternMode": "Regex", "mapMode": "PageIndex", "enabled": true },
                           { "id": "r2", "name": "新增规则二", "fileNamePattern": "extra(\\d+)", "patternMode": "Regex", "mapMode": "PageIndex", "enabled": true }
                         ],
                         "books": {
                           "a.pdf": {
                             "pageCount": 5,
                             "singleAudio": "full.mp3",
                             "playbackScope": "WholeBook",
                             "overrides": { "1": { "path": "s1.mp3", "source": "manual", "createdAt": "2026-10-20T10:12:00" } },
                             "rulePatch": { "disabledRuleIds": ["r3"], "extraRules": [], "replaceAll": false }
                           }
                         }
                       }
                       """;

            var preview = store.PreviewImport(pdfA, json);
            preview.Success.Should().BeTrue(preview.Error);
            preview.Kind.Should().Be("directory");

            store.ApplyImport(pdfA, preview);

            var loadedA = store.Load(pdfA).Profile!;
            loadedA.Rules.Single(r => r.Id == "r1").Name.Should().Be("新规则一");
            loadedA.DirectoryRules.Select(r => r.Id).Should().BeEquivalentTo(new[] { "r1", "r2", "r3" });
            loadedA.Rules.Select(r => r.Id).Should().BeEquivalentTo(new[] { "r1", "r2" });
            loadedA.RulePatch.Should().NotBeNull();
            loadedA.RulePatch!.DisabledRuleIds.Should().Contain("r3");
            loadedA.SingleAudioFile.Should().Be("full.mp3");
            loadedA.ManualOverrides[0].Should().Be("a0.mp3");
            loadedA.ManualOverrides[1].Should().Be("s1.mp3");
            loadedA.PageIndex[0].Should().Be("a0.mp3", "导入保留本机缓存");
            loadedA.IncludeSubDirectories.Should().BeTrue();
            loadedA.ExternalAudioDirectories.Should().Contain(@"D:\ext\audio");

            var loadedB = store.Load(pdfB).Profile!;
            loadedB.SingleAudioFile.Should().Be("b-full.mp3");
            loadedB.ManualOverrides[2].Should().Be("b2.mp3");
            loadedB.ManualOverrides.Should().NotContainKey(0);
        }
        finally
        {
            Cleanup(store, pdfA);
            Cleanup(store, pdfB);
        }
    }

    private static string NewTempDir()
    {
        var directory = Path.Combine(Path.GetTempPath(), "lt_pdfaudio_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string NewPdfPath() => Path.Combine(NewTempDir(), "a.pdf");

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
