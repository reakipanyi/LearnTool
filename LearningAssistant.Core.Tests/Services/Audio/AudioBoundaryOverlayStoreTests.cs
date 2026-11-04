using System.Text.Json;
using LearningAssistant.Models.Audio;
using LearningAssistant.Services.Audio;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Audio;

public class AudioBoundaryOverlayStoreTests : IDisposable
{
    private readonly string _directory;
    private readonly AudioBoundaryOverlayStore _store;
    private const string PdfPath = @"C:\Books\demo.pdf";
    private const string TrackPath = @"C:\Audio\demo.mp3";

    public AudioBoundaryOverlayStoreTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "LearningAssistant_AudioBoundaryTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _store = new AudioBoundaryOverlayStore(_directory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }
        catch (Exception)
        {
            // 清理失败不影响测试结论
        }
    }

    [Fact]
    public void Save_Load_往返保留修正与页码锚点()
    {
        var overlay = new AudioBoundaryOverlay
        {
            TrackKey = "demo.mp3",
            Fingerprint = "fp",
            Edits = { new AudioSentenceEdit { Id = "s1", StartMs = 100, EndMs = 900, Kind = "adjust" } },
            PageAnchors = { new AudioPageAnchor { PageIndex = 3, StartMs = 5000, Source = "manual" } }
        };

        _store.Save(PdfPath, TrackPath, overlay);

        var loaded = _store.Load(PdfPath, TrackPath);
        Assert.NotNull(loaded);
        Assert.Equal(1, loaded!.SchemaVersion);
        Assert.Single(loaded.Edits);
        Assert.Equal("s1", loaded.Edits[0].Id);
        Assert.Single(loaded.PageAnchors);
        Assert.Equal(3, loaded.PageAnchors[0].PageIndex);
    }

    [Fact]
    public void Load_版本不符时视为无覆盖层()
    {
        string path = _store.GetOverlayPath(PdfPath, TrackPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new { SchemaVersion = 99 }));

        var loaded = _store.Load(PdfPath, TrackPath);

        Assert.Null(loaded);
    }

    [Fact]
    public void Load_文件不存在时返回空()
    {
        Assert.Null(_store.Load(PdfPath, TrackPath));
    }

    [Fact]
    public void Delete_只删除当前曲目覆盖层()
    {
        _store.Save(PdfPath, TrackPath, new AudioBoundaryOverlay { Fingerprint = "fp" });

        _store.Delete(PdfPath, TrackPath);

        Assert.Null(_store.Load(PdfPath, TrackPath));
    }

    [Fact]
    public void IsFingerprintStale_指纹变化时提示可能偏移()
    {
        var overlay = new AudioBoundaryOverlay { Fingerprint = "old" };

        Assert.True(AudioBoundaryOverlayStore.IsFingerprintStale(overlay, "new"));
        Assert.False(AudioBoundaryOverlayStore.IsFingerprintStale(overlay, "old"));
        Assert.False(AudioBoundaryOverlayStore.IsFingerprintStale(overlay, ""));
        Assert.False(AudioBoundaryOverlayStore.IsFingerprintStale(null, "new"));
    }

    [Fact]
    public void GetOverlayPath_按书与曲目分桶()
    {
        string otherTrack = @"C:\Audio\other.mp3";
        Assert.NotEqual(
            _store.GetOverlayPath(PdfPath, TrackPath),
            _store.GetOverlayPath(PdfPath, otherTrack));
    }
}
