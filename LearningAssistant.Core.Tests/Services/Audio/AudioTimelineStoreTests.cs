using FluentAssertions;
using LearningAssistant.Models.Audio;
using LearningAssistant.Services.Audio;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Audio;

public class AudioTimelineStoreTests : IDisposable
{
    private readonly string _directory;
    private readonly AudioTimelineStore _store;

    public AudioTimelineStoreTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "LearningAssistant_AudioTimelineTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _store = new AudioTimelineStore(_directory);
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

    private static AudioSentenceTimeline CreateTimeline(string fingerprint, string trackKey, int durationMs = 60000)
    {
        var options = new AudioTimelineOptions();
        return new AudioSentenceTimeline
        {
            Fingerprint = fingerprint,
            TrackKey = trackKey,
            DurationMs = durationMs,
            Analysis = new AudioTimelineAnalysis
            {
                Engine = "energy-vad",
                Options = options,
                Confidence = 1.0,
                SourceCounts = new Dictionary<string, int> { ["vad"] = 3 }
            },
            Sentences = new List<AudioSentenceSpan>
            {
                new() { StartMs = 0, EndMs = 2000, BoundarySource = "vad" },
                new() { StartMs = 2350, EndMs = 4000, BoundarySource = "vad" },
                new() { StartMs = 4350, EndMs = 6000, BoundarySource = "vad" }
            }
        };
    }

    [Fact]
    public void 写入后读取_内容一致()
    {
        var timeline = CreateTimeline("100|200|60000", "track.mp3");

        _store.Save(timeline);
        var loaded = _store.Load("100|200|60000", new AudioTimelineOptions());

        loaded.Should().NotBeNull();
        loaded!.Fingerprint.Should().Be("100|200|60000");
        loaded.TrackKey.Should().Be("track.mp3");
        loaded.Sentences.Should().HaveCount(3);
        loaded.Sentences.Select(s => s.Index).Should().Equal(1, 2, 3);
        loaded.Analysis.SourceCounts["vad"].Should().Be(3);
    }

    [Fact]
    public void 同一指纹_不同TrackKey_命中同一缓存文件()
    {
        _store.Save(CreateTimeline("same|1|60000", "a.mp3"));
        var pathA = _store.GetFilePath("same|1|60000");

        _store.Save(CreateTimeline("same|1|60000", "b.mp3"));
        var pathB = _store.GetFilePath("same|1|60000");

        pathA.Should().Be(pathB);
        _store.Load("same|1|60000", new AudioTimelineOptions())!.TrackKey.Should().Be("b.mp3");
    }

    [Fact]
    public void 指纹变化_缓存未命中()
    {
        _store.Save(CreateTimeline("100|200|60000", "track.mp3"));

        _store.Load("101|200|60000", new AudioTimelineOptions()).Should().BeNull();
    }

    [Fact]
    public void 参数签名变化_缓存未命中()
    {
        _store.Save(CreateTimeline("100|200|60000", "track.mp3"));

        var changed = new AudioTimelineOptions { SentenceGapMs = 500 };
        _store.Load("100|200|60000", changed).Should().BeNull();
        File.Exists(_store.GetFilePath("100|200|60000")).Should().BeFalse();
    }

    [Fact]
    public void Schema版本不符_缓存未命中并删除()
    {
        var timeline = CreateTimeline("100|200|60000", "track.mp3");
        timeline.SchemaVersion = 2;

        var path = _store.GetFilePath("100|200|60000");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(timeline));

        _store.Load("100|200|60000", new AudioTimelineOptions()).Should().BeNull();
        File.Exists(path).Should().BeFalse();
    }

    [Fact]
    public void 损坏JSON_自愈返回null()
    {
        var path = _store.GetFilePath("broken|1|1000");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{ not json");

        _store.Load("broken|1|1000", new AudioTimelineOptions()).Should().BeNull();
        File.Exists(path).Should().BeFalse();
    }

    [Fact]
    public void 并发写入_文件不损坏()
    {
        var fingerprint = "100|200|60000";
        var tasks = Enumerable.Range(0, 20)
            .Select(i => Task.Run(() => _store.Save(CreateTimeline(fingerprint, $"track-{i}.mp3"))))
            .ToArray();
        Task.WaitAll(tasks);

        var loaded = _store.Load(fingerprint, new AudioTimelineOptions());
        loaded.Should().NotBeNull();
        loaded!.Sentences.Should().HaveCount(3);
    }

    [Fact]
    public void 失败负缓存_冷却期内命中_过期后失效()
    {
        _store.SaveFailure("fail|1|1000", TimeSpan.FromHours(1), "decode failed");

        _store.IsInFailureCooldown("fail|1|1000", out var retryAfter).Should().BeTrue();
        retryAfter.Should().BeAfter(DateTime.UtcNow);

        _store.SaveFailure("fail|1|1000", TimeSpan.Zero);
        _store.IsInFailureCooldown("fail|1|1000", out _).Should().BeFalse();
    }

    [Fact]
    public void 保存成功会清除失败标记()
    {
        _store.SaveFailure("100|200|60000", TimeSpan.FromHours(1));
        _store.Save(CreateTimeline("100|200|60000", "track.mp3"));

        _store.IsInFailureCooldown("100|200|60000", out _).Should().BeFalse();
    }

    [Fact]
    public void 指纹计算_包含大小修改时间与时长()
    {
        var file = Path.Combine(_directory, "a.mp3");
        File.WriteAllBytes(file, new byte[10]);

        var fingerprint = AudioTimelineStore.ComputeFingerprint(file, 5000);

        fingerprint.Should().StartWith("10|");
        fingerprint.Should().EndWith("|5000");
    }
}
