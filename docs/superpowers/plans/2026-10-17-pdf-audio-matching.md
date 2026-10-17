# PDF 配套音频自动匹配与播放 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 打开 PDF 后按规则把「页」自动映射到同目录音频，翻页自动切歌并保持播放状态，支持手动覆盖持久化，并提供英语四增强（倍速/AB 循环/字幕/跟读）。

**Architecture:** 纯逻辑（模型、媒体内核、字幕解析、目录扫描、匹配引擎、profile 持久化）全部落 `LearningAssistant.Core`，用 `xunit` 单测；WinApp 只做 NAudio 播放实现、编排管理器与 UI。

**Tech Stack:** .NET 10 / WinForms / NAudio（`WaveOutEvent` + `AudioFileReader`）/ System.Text.Json（复用 `JsonHelper`）/ xunit + FluentAssertions。

**Spec:** `docs/superpowers/specs/2026-10-17-pdf-audio-matching-design.md`、`docs/superpowers/specs/2026-10-17-media-kernel-design.md`

## Global Constraints

- 命名空间：模型 `LearningAssistant.Models.Pdf`；媒体抽象 `LearningAssistant.Abstractions.Media`；媒体纯逻辑 `LearningAssistant.Services.Media`；PDF 音频纯逻辑 `LearningAssistant.Services.Pdf`；WinApp 播放实现 `LearningAssistant.Platform.Media`；WinApp 播放服务 `LearningAssistant.Services.Pdf.Audio`；管理器 `LearningAssistant.Managers`；窗体 `LearningAssistant.Forms.Pdf`。
- 页码一律 **0-based**（与 `PdfReaderForm.CurrentPageIndex` 一致）；文件名捕获值是**1-based 人类页码**，转换规则：`pageIndex = value - 1 + PageOffset`。
- 非事件异步方法带 `Async` 后缀并返回 `Task`；即发即弃用 `FireAndForgetWithLogging` 包装；跨线程 UI 回调前 `IsHandleCreated` 检查；UI getter 空检查（`?.` / `?? `）。
- 持久化复用 `JsonHelper.SaveToFile/LoadFromFile`（已内置原子写 temp→Replace/Move），不另写原子写。
- 倍速不得使用 NAudio 内置 `VarispeedWaveStream`（音调失真）；音频变速阶段一用「时域拉伸」实现前先 spike，失败则 UI 禁用倍速不塞占位。
- 录音（`IAudioRecorderService`）与播放互斥由 `PdfReaderForm` 层协调；播放服务不反向依赖录音。
- 每 PDF 一份 sidecar：`AppPaths.AudioProfilesDir/{pdfHash}.json`，`pdfHash` = PDF 绝对路径 SHA256 前 16 位。

## Review Focus

1. 文件名捕获值非数字（如 `P0X.mp3`）→ 跳过该命中，不得崩溃。
2. 页码越界（捕获 page > pageCount）→ 不写入映射，计入未匹配页。
3. 正则非法 → 跳过该规则并记录诊断，不抛异常。
4. 手动覆盖的音频文件不存在 → 下一轮扫描清理，加载时标灰。
5. 同目录无音频 → 空列表，不报错。

---

## Task 1: Core 数据模型 + `AppPaths.AudioProfilesDir`

**Files:**
- Create: `LearningAssistant.Core/Models/Pdf/AudioPageMapMode.cs`
- Create: `LearningAssistant.Core/Models/Pdf/AudioMatchRule.cs`
- Create: `LearningAssistant.Core/Models/Pdf/AudioTrack.cs`
- Create: `LearningAssistant.Core/Models/Pdf/PdfAudioProfile.cs`
- Create: `LearningAssistant.Core/Models/Pdf/AudioMatchResult.cs`
- Modify: `LearningAssistant.Core/Common/AppPaths.cs` （新增 `AudioProfilesDir` + 目录创建）
- Test: `LearningAssistant.Core.Tests/Models/Pdf/PdfAudioModelsTests.cs`

**Interfaces:**
- Produces: `AudioPageMapMode`, `AudioMatchRule`, `AudioTrack`, `PdfAudioProfile`, `AudioMatchResult`（后续所有任务依赖）。

- [ ] **Step 1: 写失败测试**

```csharp
using FluentAssertions;
using LearningAssistant.Common;
using LearningAssistant.Models.Pdf;

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
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test LearningAssistant.Core.Tests`
Expected: FAIL（类型不存在）

- [ ] **Step 3: 实现模型**

`AudioPageMapMode.cs`:
```csharp
namespace LearningAssistant.Models.Pdf;

public enum AudioPageMapMode { PageIndex, Sequence, Range, Custom }
```

`AudioMatchRule.cs`:
```csharp
namespace LearningAssistant.Models.Pdf;

public class AudioMatchRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string FileNamePattern { get; set; } = "";
    public int? CaptureGroupIndex { get; set; }
    public string? CaptureGroupName { get; set; }
    public AudioPageMapMode MapMode { get; set; } = AudioPageMapMode.PageIndex;
    public int PageOffset { get; set; }
    public int PagesPerUnit { get; set; } = 1;
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; }
}
```

`AudioTrack.cs`:
```csharp
namespace LearningAssistant.Models.Pdf;

public class AudioTrack
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public int MappedPageStart { get; set; } = -1;
    public int MappedPageEnd { get; set; } = -1;
    public int DurationMs { get; set; }
    public bool IsManual { get; set; }
    public string? SubtitlePath { get; set; }
}
```

`PdfAudioProfile.cs`:
```csharp
namespace LearningAssistant.Models.Pdf;

public class PdfAudioProfile
{
    public string PdfPath { get; set; } = "";
    public List<AudioMatchRule> Rules { get; set; } = new();
    public Dictionary<int, string> ManualOverrides { get; set; } = new();
    public List<AudioTrack> Tracks { get; set; } = new();
    public bool IncludeSubDirectories { get; set; }
    public string ScanFingerprint { get; set; } = "";
}
```

`AudioMatchResult.cs`:
```csharp
namespace LearningAssistant.Models.Pdf;

public class AudioMatchResult
{
    public Dictionary<int, AudioTrack> PageIndex { get; set; } = new();
    public HashSet<int> UnmatchedPages { get; set; } = new();
    public List<AudioTrack> Tracks { get; set; } = new();
}
```

`AppPaths.cs` — 在 `AudioDir` 附近新增：
```csharp
/// <summary>PDF 配套音频 profile 目录（按 PDF 哈希存 sidecar）</summary>
public static string AudioProfilesDir => Path.Combine(AudioDir, "Profiles");
```
并在 `EnsureDirectoriesExist()` 内 `EnsureDirectoryExists(AudioDir);` 之后加 `EnsureDirectoryExists(AudioProfilesDir);`。

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test LearningAssistant.Core.Tests`
Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add LearningAssistant.Core/Models/Pdf/AudioPageMapMode.cs LearningAssistant.Core/Models/Pdf/AudioMatchRule.cs LearningAssistant.Core/Models/Pdf/AudioTrack.cs LearningAssistant.Core/Models/Pdf/PdfAudioProfile.cs LearningAssistant.Core/Models/Pdf/AudioMatchResult.cs LearningAssistant.Core/Common/AppPaths.cs LearningAssistant.Core.Tests/Models/Pdf/PdfAudioModelsTests.cs
git commit -m "feat(pdf-audio): 新增配套音频模型与 profile 目录"
```

---

## Task 2: Core 媒体内核抽象 + `AbLoopState`

**Files:**
- Create: `LearningAssistant.Core/Abstractions/Media/MediaPlaybackState.cs`
- Create: `LearningAssistant.Core/Abstractions/Media/MediaLoopMode.cs`
- Create: `LearningAssistant.Core/Abstractions/Media/SubtitleCue.cs`
- Create: `LearningAssistant.Core/Abstractions/Media/IMediaPlayer.cs`
- Create: `LearningAssistant.Core/Services/Media/AbLoopState.cs`
- Test: `LearningAssistant.Core.Tests/Services/Media/AbLoopStateTests.cs`

**Interfaces:**
- Produces: `IMediaPlayer`（Task 7 实现）、`MediaPlaybackState`、`MediaLoopMode`、`SubtitleCue`、`AbLoopState`（`ShouldLoopBack(TimeSpan, out TimeSpan)`）。

- [ ] **Step 1: 写失败测试**

```csharp
using FluentAssertions;
using LearningAssistant.Services.Media;

namespace LearningAssistant.Core.Tests.Services.Media;

public class AbLoopStateTests
{
    [Fact]
    public void 未设边界_不回跳()
    {
        var state = new AbLoopState();
        state.ShouldLoopBack(TimeSpan.FromSeconds(5), out _).Should().BeFalse();
    }

    [Fact]
    public void 位置小于B_不回跳()
    {
        var state = new AbLoopState { A = TimeSpan.FromSeconds(2), B = TimeSpan.FromSeconds(4) };
        state.ShouldLoopBack(TimeSpan.FromSeconds(3), out _).Should().BeFalse();
    }

    [Fact]
    public void 位置大于等于B_回跳到A()
    {
        var state = new AbLoopState { A = TimeSpan.FromSeconds(2), B = TimeSpan.FromSeconds(4) };
        state.ShouldLoopBack(TimeSpan.FromSeconds(4), out var target).Should().BeTrue();
        target.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void B小于等于A_视为未启用()
    {
        var state = new AbLoopState { A = TimeSpan.FromSeconds(4), B = TimeSpan.FromSeconds(2) };
        state.ShouldLoopBack(TimeSpan.FromSeconds(5), out _).Should().BeFalse();
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test LearningAssistant.Core.Tests`
Expected: FAIL

- [ ] **Step 3: 实现**

`MediaPlaybackState.cs`:
```csharp
namespace LearningAssistant.Abstractions.Media;
public enum MediaPlaybackState { Idle, Playing, Paused, Stopped }
```

`MediaLoopMode.cs`:
```csharp
namespace LearningAssistant.Abstractions.Media;
public enum MediaLoopMode { None, RepeatOne, RepeatAll, AbLoop }
```

`SubtitleCue.cs`:
```csharp
namespace LearningAssistant.Abstractions.Media;
public sealed class SubtitleCue
{
    public int Index { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Text { get; set; } = "";
}
```

`IMediaPlayer.cs`:
```csharp
namespace LearningAssistant.Abstractions.Media;

public interface IMediaPlayer : IDisposable
{
    MediaPlaybackState State { get; }
    TimeSpan Position { get; }
    TimeSpan Duration { get; }
    double Speed { get; set; }
    MediaLoopMode LoopMode { get; set; }
    (TimeSpan? A, TimeSpan? B) AbLoop { get; set; }

    void Load(string mediaPath);
    Task PlayAsync(CancellationToken ct = default);
    void Pause();
    void Resume();
    void Stop();
    void Seek(TimeSpan position);
    void LoadSubtitle(string? subtitlePath);
    IReadOnlyList<SubtitleCue>? SubtitleCues { get; }

    event EventHandler<TimeSpan>? PositionChanged;
    event EventHandler<MediaPlaybackState>? StateChanged;
    event EventHandler? PlaybackFinished;
}
```

`AbLoopState.cs`:
```csharp
namespace LearningAssistant.Services.Media;

public sealed class AbLoopState
{
    public TimeSpan? A { get; set; }
    public TimeSpan? B { get; set; }

    public bool ShouldLoopBack(TimeSpan position, out TimeSpan target)
    {
        target = A ?? TimeSpan.Zero;
        if (A is null || B is null) return false;
        if (B <= A) return false;
        return position >= B;
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test LearningAssistant.Core.Tests`
Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add LearningAssistant.Core/Abstractions/Media LearningAssistant.Core/Services/Media/AbLoopState.cs LearningAssistant.Core.Tests/Services/Media/AbLoopStateTests.cs
git commit -m "feat(media): 新增 IMediaPlayer 抽象与 AB 循环纯逻辑"
```

---

## Task 3: Core 字幕解析 `SubtitleParser`

**Files:**
- Create: `LearningAssistant.Core/Services/Media/ISubtitleParser.cs`
- Create: `LearningAssistant.Core/Services/Media/SubtitleParser.cs`
- Test: `LearningAssistant.Core.Tests/Services/Media/SubtitleParserTests.cs`

**Interfaces:**
- Produces: `ISubtitleParser.Parse(string filePath) -> IReadOnlyList<SubtitleCue>`。

- [ ] **Step 1: 写失败测试**

```csharp
using FluentAssertions;
using LearningAssistant.Abstractions.Media;
using LearningAssistant.Services.Media;

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
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test LearningAssistant.Core.Tests`
Expected: FAIL

- [ ] **Step 3: 实现**

`ISubtitleParser.cs`:
```csharp
using LearningAssistant.Abstractions.Media;
namespace LearningAssistant.Services.Media;

public interface ISubtitleParser
{
    IReadOnlyList<SubtitleCue> Parse(string filePath);
}
```

`SubtitleParser.cs`（含 GB18030 回退 + srt/lrc）：
```csharp
using LearningAssistant.Abstractions.Media;
using System.Text;
using System.Text.RegularExpressions;

namespace LearningAssistant.Services.Media;

public class SubtitleParser : ISubtitleParser
{
    private static readonly Regex SrtTime = new(@"(\d{1,2}):(\d{2}):(\d{2})[,.](\d{1,3})\s*-->\s*(\d{1,2}):(\d{2}):(\d{2})[,.](\d{1,3})", RegexOptions.Compiled);
    private static readonly Regex LrcTag = new(@"\[(\d{1,2}):(\d{2})(?:[.:](\d{1,3}))?\]", RegexOptions.Compiled);

    public IReadOnlyList<SubtitleCue> Parse(string filePath)
    {
        var text = ReadAllText(filePath);
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var cues = ext == ".srt" ? ParseSrt(text) : ParseLrc(text);
        for (int i = 0; i < cues.Count - 1; i++)
            if (cues[i].EndTime <= TimeSpan.Zero)
                cues[i].EndTime = cues[i + 1].StartTime;
        return cues;
    }

    private static string ReadAllText(string path)
    {
        try { return File.ReadAllText(path, Encoding.UTF8); }
        catch (DecoderFallbackException) { return File.ReadAllText(path, Encoding.GetEncoding("GB18030")); }
    }

    private static List<SubtitleCue> ParseSrt(string text)
    {
        var list = new List<SubtitleCue>();
        var blocks = text.Replace("\r\n", "\n").Split("\n\n");
        int index = 0;
        foreach (var block in blocks)
        {
            var m = SrtTime.Match(block);
            if (!m.Success) continue;
            var start = ToTimeSpan(m, 1);
            var end = ToTimeSpan(m, 5);
            var body = string.Join(" ", block.Split('\n').Skip(2).Where(l => !string.IsNullOrWhiteSpace(l))).Trim();
            if (string.IsNullOrWhiteSpace(body)) continue;
            list.Add(new SubtitleCue { Index = ++index, StartTime = start, EndTime = end, Text = body });
        }
        return list;
    }

    private static List<SubtitleCue> ParseLrc(string text)
    {
        var list = new List<SubtitleCue>();
        int index = 0;
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var matches = LrcTag.Matches(raw);
            if (matches.Count == 0) continue;
            var body = LrcTag.Replace(raw, "").Trim();
            if (string.IsNullOrWhiteSpace(body)) continue;
            foreach (Match m in matches)
            {
                var min = int.Parse(m.Groups[1].Value);
                var sec = int.Parse(m.Groups[2].Value);
                var ms = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0;
                list.Add(new SubtitleCue { Index = ++index, StartTime = new TimeSpan(0, 0, min, sec, ms), Text = body });
            }
        }
        return list.OrderBy(c => c.StartTime).ToList();
    }

    private static TimeSpan ToTimeSpan(Match m, int group)
    {
        int h = int.Parse(m.Groups[group].Value);
        int mm = int.Parse(m.Groups[group + 1].Value);
        int ss = int.Parse(m.Groups[group + 2].Value);
        int ms = int.Parse(m.Groups[group + 3].Value);
        return new TimeSpan(0, h, mm, ss, ms);
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test LearningAssistant.Core.Tests`
Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add LearningAssistant.Core/Services/Media/ISubtitleParser.cs LearningAssistant.Core/Services/Media/SubtitleParser.cs LearningAssistant.Core.Tests/Services/Media/SubtitleParserTests.cs
git commit -m "feat(media): 新增 srt/lrc 字幕解析"
```

---

## Task 4: Core 目录扫描 `PdfAudioDirectoryScanner`

**Files:**
- Create: `LearningAssistant.Core/Services/Pdf/Audio/IPdfAudioDirectoryScanner.cs`
- Create: `LearningAssistant.Core/Services/Pdf/Audio/PdfAudioDirectoryScanner.cs`
- Test: `LearningAssistant.Core.Tests/Services/Pdf/PdfAudioDirectoryScannerTests.cs`

**Interfaces:**
- Consumes: `AudioTrack`（Task 1）
- Produces: `IPdfAudioDirectoryScanner.Scan(string pdfDirectoryPath, bool includeSubDirectories) -> List<AudioTrack>`；`ComputeScanFingerprint(List<AudioTrack>) -> string`。

- [ ] **Step 1: 写失败测试**（临时目录创建真实文件）

```csharp
using FluentAssertions;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;

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
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test LearningAssistant.Core.Tests`
Expected: FAIL

- [ ] **Step 3: 实现**

`IPdfAudioDirectoryScanner.cs`:
```csharp
using LearningAssistant.Models.Pdf;
namespace LearningAssistant.Services.Pdf;

public interface IPdfAudioDirectoryScanner
{
    List<AudioTrack> Scan(string pdfDirectoryPath, bool includeSubDirectories);
}
```

`PdfAudioDirectoryScanner.cs`:
```csharp
using LearningAssistant.Models.Pdf;
using System.Security.Cryptography;
using System.Text;

namespace LearningAssistant.Services.Pdf;

public class PdfAudioDirectoryScanner : IPdfAudioDirectoryScanner
{
    private static readonly string[] AudioExts =
        { ".mp3", ".wav", ".m4a", ".aac", ".flac", ".ogg", ".wma" };
    private static readonly string[] SubtitleExts = { ".srt", ".lrc" };

    public List<AudioTrack> Scan(string pdfDirectoryPath, bool includeSubDirectories)
    {
        var tracks = new List<AudioTrack>();
        if (!Directory.Exists(pdfDirectoryPath)) return tracks;

        var option = includeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        foreach (var ext in AudioExts)
        {
            foreach (var file in Directory.EnumerateFiles(pdfDirectoryPath, "*" + ext, option))
            {
                var relative = Path.GetRelativePath(pdfDirectoryPath, file);
                tracks.Add(new AudioTrack
                {
                    FilePath = file,
                    FileName = Path.GetFileName(file),
                    RelativePath = relative,
                    SubtitlePath = FindSubtitle(file)
                });
            }
        }
        return tracks.OrderBy(t => t.RelativePath, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string? FindSubtitle(string audioPath)
    {
        var baseName = Path.Combine(Path.GetDirectoryName(audioPath)!, Path.GetFileNameWithoutExtension(audioPath));
        foreach (var ext in SubtitleExts)
            if (File.Exists(baseName + ext)) return baseName + ext;
        return null;
    }

    public static string ComputeScanFingerprint(List<AudioTrack> tracks)
    {
        var sb = new StringBuilder();
        foreach (var t in tracks.OrderBy(t => t.RelativePath, StringComparer.OrdinalIgnoreCase))
        {
            long size = 0; DateTime m; try { var fi = new FileInfo(t.FilePath); size = fi.Length; m = fi.LastWriteTimeUtc; }
            catch { m = default; }
            sb.Append(t.RelativePath).Append('|').Append(size).Append('|').Append(m.Ticks).Append('\n');
        }
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes)[..16];
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test LearningAssistant.Core.Tests`
Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add LearningAssistant.Core/Services/Pdf/Audio LearningAssistant.Core.Tests/Services/Pdf/PdfAudioDirectoryScannerTests.cs
git commit -m "feat(pdf-audio): 新增音频目录扫描与 sidecar 字幕探测"
```

---

## Task 5: Core 匹配引擎 `PdfAudioMatcher`

**Files:**
- Create: `LearningAssistant.Core/Services/Pdf/Audio/IPdfAudioMatcher.cs`
- Create: `LearningAssistant.Core/Services/Pdf/Audio/PdfAudioMatcher.cs`
- Test: `LearningAssistant.Core.Tests/Services/Pdf/PdfAudioMatcherTests.cs`

**Interfaces:**
- Consumes: `AudioTrack`/`AudioMatchRule`/`AudioMatchResult`/`AudioPageMapMode`（Task 1）
- Produces: `IPdfAudioMatcher.Match(string pdfPath, int pageCount, IReadOnlyList<AudioTrack> tracks, IReadOnlyList<AudioMatchRule> rules, IReadOnlyDictionary<int,string>? manualOverrides) -> AudioMatchResult`。

- [ ] **Step 1: 写失败测试**

```csharp
using FluentAssertions;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;

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
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test LearningAssistant.Core.Tests`
Expected: FAIL

- [ ] **Step 3: 实现**

`IPdfAudioMatcher.cs`:
```csharp
using LearningAssistant.Models.Pdf;
namespace LearningAssistant.Services.Pdf;

public interface IPdfAudioMatcher
{
    AudioMatchResult Match(string pdfPath, int pageCount, IReadOnlyList<AudioTrack> tracks,
        IReadOnlyList<AudioMatchRule> rules, IReadOnlyDictionary<int, string>? manualOverrides);
}
```

`PdfAudioMatcher.cs`（核心逻辑见下，含 TryAssign 优先级与手动覆盖）:
```csharp
using LearningAssistant.Models.Pdf;
using System.Text.RegularExpressions;

namespace LearningAssistant.Services.Pdf;

public class PdfAudioMatcher : IPdfAudioMatcher
{
    private readonly List<string> _diagnostics = new();
    public IReadOnlyList<string> Diagnostics => _diagnostics;

    public AudioMatchResult Match(string pdfPath, int pageCount, IReadOnlyList<AudioTrack> tracks,
        IReadOnlyList<AudioMatchRule> rules, IReadOnlyDictionary<int, string>? manualOverrides)
    {
        _diagnostics.Clear();
        var result = new AudioMatchResult { Tracks = tracks.ToList() };
        var pageIndex = result.PageIndex;
        var priorityMap = new Dictionary<int, int>(); // page -> assigned priority

        foreach (var rule in rules.Where(r => r.Enabled).OrderByDescending(r => r.Priority))
        {
            Regex regex;
            try { regex = new Regex(rule.FileNamePattern, RegexOptions.IgnoreCase); }
            catch (ArgumentException) { _diagnostics.Add($"规则 '{rule.Name}' 正则非法，已跳过"); continue; }

            var hits = new List<(AudioTrack Track, int? Value)>();
            foreach (var track in tracks)
            {
                var m = regex.Match(track.RelativePath);
                if (!m.Success) continue;
                hits.Add((track, ExtractValue(m, rule)));
            }
            if (hits.Count == 0) continue;

            if (rule.MapMode == AudioPageMapMode.Sequence)
            {
                var ordered = hits.OrderBy(h => h.Value ?? 0).ThenBy(h => h.Track.RelativePath, StringComparer.OrdinalIgnoreCase).ToList();
                for (int i = 0; i < ordered.Count; i++)
                    Assign(pageIndex, priorityMap, i + rule.PageOffset, pageCount, ordered[i].Track, rule.Priority);
            }
            else foreach (var h in hits)
            {
                if (h.Value is not int v) continue;
                var pages = rule.MapMode switch
                {
                    AudioPageMapMode.PageIndex => new[] { v - 1 + rule.PageOffset },
                    AudioPageMapMode.Range => Enumerable.Range((v - 1) * rule.PagesPerUnit + rule.PageOffset, rule.PagesPerUnit),
                    _ => Array.Empty<int>()
                };
                foreach (var p in pages) Assign(pageIndex, priorityMap, p, pageCount, h.Track, rule.Priority);
            }
        }

        if (manualOverrides != null)
            foreach (var kv in manualOverrides)
            {
                if (kv.Key < 0 || kv.Key >= pageCount) continue;
                var track = tracks.FirstOrDefault(t => string.Equals(t.FileName, Path.GetFileName(kv.Value), StringComparison.OrdinalIgnoreCase)
                        || string.Equals(t.RelativePath, kv.Value, StringComparison.OrdinalIgnoreCase))
                    ?? new AudioTrack { FilePath = Path.Combine(Path.GetDirectoryName(pdfPath) ?? "", kv.Value), FileName = Path.GetFileName(kv.Value), RelativePath = kv.Value };
                track.IsManual = true;
                pageIndex[kv.Key] = track;
            }

        for (int p = 0; p < pageCount; p++)
            if (!pageIndex.ContainsKey(p)) result.UnmatchedPages.Add(p);

        return result;
    }

    private static int? ExtractValue(Match m, AudioMatchRule rule)
    {
        if (!string.IsNullOrEmpty(rule.CaptureGroupName) && m.Groups[rule.CaptureGroupName].Success)
            return int.TryParse(m.Groups[rule.CaptureGroupName].Value, out var n) ? n : null;
        var idx = rule.CaptureGroupIndex ?? 1;
        if (idx >= 1 && idx < m.Groups.Count && m.Groups[idx].Success)
            return int.TryParse(m.Groups[idx].Value, out var c) ? c : null;
        return null;
    }

    private static void Assign(Dictionary<int, AudioTrack> pageIndex, Dictionary<int, int> priorityMap,
        int page, int pageCount, AudioTrack track, int priority)
    {
        if (page < 0 || page >= pageCount) return;
        if (priorityMap.TryGetValue(page, out var existing) && existing >= priority) return;
        priorityMap[page] = priority;
        pageIndex[page] = track;
        track.MappedPageStart = page; track.MappedPageEnd = page;
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test LearningAssistant.Core.Tests`
Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add LearningAssistant.Core/Services/Pdf/Audio/IPdfAudioMatcher.cs LearningAssistant.Core/Services/Pdf/Audio/PdfAudioMatcher.cs LearningAssistant.Core.Tests/Services/Pdf/PdfAudioMatcherTests.cs
git commit -m "feat(pdf-audio): 新增正则匹配引擎与手动覆盖"
```

---

## Task 6: Core profile 存储 `PdfAudioProfileStore`

**Files:**
- Create: `LearningAssistant.Core/Services/Pdf/Audio/IPdfAudioProfileStore.cs`
- Create: `LearningAssistant.Core/Services/Pdf/Audio/PdfAudioProfileStore.cs`
- Test: `LearningAssistant.Core.Tests/Services/Pdf/PdfAudioProfileStoreTests.cs`

**Interfaces:**
- Consumes: `PdfAudioProfile`（Task 1）、`JsonHelper`
- Produces: `IPdfAudioProfileStore.Load(string pdfPath) -> PdfAudioProfile?`、`Save(PdfAudioProfile)`、`GetProfilePath(string pdfPath) -> string`。

- [ ] **Step 1: 写失败测试**

```csharp
using FluentAssertions;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;

namespace LearningAssistant.Core.Tests.Services.Pdf;

public class PdfAudioProfileStoreTests
{
    [Fact]
    public void 保存再加载_字段一致()
    {
        var store = new PdfAudioProfileStore();
        var profile = new PdfAudioProfile { PdfPath = @"C:\x\a.pdf", IncludeSubDirectories = true };
        profile.ManualOverrides[2] = "u2.mp3";
        store.Save(profile);
        var loaded = store.Load(@"C:\x\a.pdf")!;
        loaded.PdfPath.Should().Be(@"C:\x\a.pdf");
        loaded.IncludeSubDirectories.Should().BeTrue();
        loaded.ManualOverrides[2].Should().Be("u2.mp3");
    }

    [Fact]
    public void 加载不存在_返回null()
    {
        var store = new PdfAudioProfileStore();
        store.Load(@"C:\x\not_exist.pdf").Should().BeNull();
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test LearningAssistant.Core.Tests`
Expected: FAIL

- [ ] **Step 3: 实现**

`IPdfAudioProfileStore.cs`:
```csharp
using LearningAssistant.Models.Pdf;
namespace LearningAssistant.Services.Pdf;

public interface IPdfAudioProfileStore
{
    PdfAudioProfile? Load(string pdfPath);
    void Save(PdfAudioProfile profile);
    string GetProfilePath(string pdfPath);
}
```

`PdfAudioProfileStore.cs`:
```csharp
using LearningAssistant.Common;
using LearningAssistant.Models.Pdf;
using System.Security.Cryptography;
using System.Text;

namespace LearningAssistant.Services.Pdf;

public class PdfAudioProfileStore : IPdfAudioProfileStore
{
    public PdfAudioProfile? Load(string pdfPath)
    {
        var path = GetProfilePath(pdfPath);
        return JsonHelper.LoadFromFile<PdfAudioProfile>(path);
    }

    public void Save(PdfAudioProfile profile)
    {
        profile.PdfPath = Path.GetFullPath(profile.PdfPath);
        JsonHelper.SaveToFile(GetProfilePath(profile.PdfPath), profile);
    }

    public string GetProfilePath(string pdfPath)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(pdfPath))))[..16];
        return Path.Combine(AppPaths.AudioProfilesDir, hash + ".json");
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test LearningAssistant.Core.Tests`
Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add LearningAssistant.Core/Services/Pdf/Audio/IPdfAudioProfileStore.cs LearningAssistant.Core/Services/Pdf/Audio/PdfAudioProfileStore.cs LearningAssistant.Core.Tests/Services/Pdf/PdfAudioProfileStoreTests.cs
git commit -m "feat(pdf-audio): 新增 profile sidecar 持久化"
```

---

## Task 7: WinApp `NAudioMediaPlayer`（IMediaPlayer 实现）

**Files:**
- Create: `LearningAssistant.WinApp/Platform/Media/NAudioMediaPlayer.cs`

**Interfaces:**
- Consumes: `IMediaPlayer`/`MediaPlaybackState`/`MediaLoopMode`/`SubtitleCue`（Task 2）、`ISubtitleParser`（Task 3）、`AbLoopState`（Task 2）

**Notes（验收点）**：
- `WaveOutEvent` + `AudioFileReader`；`Pause()` 用 `WaveOutEvent.Pause()`，`Resume()` 用 `Play()`；`Seek` 设 `AudioFileReader.CurrentTime`。
- `Speed` 阶段一：因倍速引擎需 spike，**先实现为「仅接受 1.0，其它值记日志并忽略」**，UI 禁用倍速；spike 通过后替换为时域拉伸，**不用 NAudio `VarispeedWaveStream`**。
- `PositionChanged` 用 `System.Windows.Forms.Timer`（间隔 100ms）上报；`PlaybackFinished` 在 `PlaybackStopped` 且非用户 Stop 时触发；AB 循环用 `AbLoopState.ShouldLoopBack` + `Seek(A)`。
- `LoadSubtitle` 用 `ISubtitleParser.Parse` 填充 `SubtitleCues`；无字幕传 null 清空。

- [ ] **Step 1: 编写实现**（骨架 + 关键逻辑，播放控制完整）

```csharp
using LearningAssistant.Abstractions.Media;
using LearningAssistant.Services.Media;
using NAudio.Wave;

namespace LearningAssistant.Platform.Media;

public class NAudioMediaPlayer : IMediaPlayer
{
    private readonly ISubtitleParser _subtitleParser;
    private WaveOutEvent? _waveOut;
    private AudioFileReader? _reader;
    private System.Windows.Forms.Timer? _timer;
    private readonly AbLoopState _abLoop = new();
    private bool _manualStop;

    public event EventHandler<TimeSpan>? PositionChanged;
    public event EventHandler<MediaPlaybackState>? StateChanged;
    public event EventHandler? PlaybackFinished;

    public MediaPlaybackState State { get; private set; } = MediaPlaybackState.Idle;
    public TimeSpan Position => _reader?.CurrentTime ?? TimeSpan.Zero;
    public TimeSpan Duration => _reader?.TotalTime ?? TimeSpan.Zero;
    public double Speed { get; set; } = 1.0;
    public MediaLoopMode LoopMode { get; set; } = MediaLoopMode.None;
    public (TimeSpan? A, TimeSpan? B) AbLoop { get => (_abLoop.A, _abLoop.B); set => (_abLoop.A, _abLoop.B) = value; }
    public IReadOnlyList<SubtitleCue>? SubtitleCues { get; private set; }

    public NAudioMediaPlayer(ISubtitleParser subtitleParser) => _subtitleParser = subtitleParser;

    public void Load(string mediaPath)
    {
        Stop();
        _reader = new AudioFileReader(mediaPath);
        _waveOut = new WaveOutEvent();
        _waveOut.PlaybackStopped += OnPlaybackStopped;
        _waveOut.Init(_reader);
        _timer = new System.Windows.Forms.Timer { Interval = 100 };
        _timer.Tick += (_, _) => OnTick();
        _timer.Start();
        SetState(MediaPlaybackState.Idle);
    }

    public Task PlayAsync(CancellationToken ct = default)
    {
        if (_waveOut == null) return Task.CompletedTask;
        _manualStop = false;
        if (_waveOut.PlaybackState != PlaybackState.Paused) _reader!.CurrentTime = _reader.CurrentTime;
        _waveOut.Play();
        SetState(MediaPlaybackState.Playing);
        return Task.CompletedTask;
    }

    public void Pause() { _waveOut?.Pause(); SetState(MediaPlaybackState.Paused); }
    public void Resume() { _ = PlayAsync(); }
    public void Stop() { _manualStop = true; _waveOut?.Stop(); _reader?.Seek(0, System.IO.SeekOrigin.Begin); SetState(MediaPlaybackState.Stopped); }
    public void Seek(TimeSpan position) { if (_reader != null && position >= TimeSpan.Zero && position <= _reader.TotalTime) _reader.CurrentTime = position; }

    public void LoadSubtitle(string? subtitlePath)
    {
        SubtitleCues = string.IsNullOrWhiteSpace(subtitlePath) || !File.Exists(subtitlePath)
            ? null : _subtitleParser.Parse(subtitlePath);
    }

    private void OnTick()
    {
        if (_abLoop.ShouldLoopBack(Position, out var target)) Seek(target);
        PositionChanged?.Invoke(this, Position);
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (_manualStop) return;
        if (LoopMode == MediaLoopMode.RepeatOne || LoopMode == MediaLoopMode.RepeatAll)
        { _reader?.Seek(0, System.IO.SeekOrigin.Begin); _waveOut?.Play(); SetState(MediaPlaybackState.Playing); return; }
        SetState(MediaPlaybackState.Stopped);
        PlaybackFinished?.Invoke(this, EventArgs.Empty);
    }

    private void SetState(MediaPlaybackState s)
    {
        if (State == s) return;
        State = s;
        StateChanged?.Invoke(this, s);
    }

    public void Dispose() { _timer?.Stop(); _timer?.Dispose(); Stop(); _waveOut?.Dispose(); _reader?.Dispose(); }
}
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build LearningAssistant.WinApp`
Expected: 编译通过

- [ ] **Step 3: 提交**

```bash
git add LearningAssistant.WinApp/Platform/Media/NAudioMediaPlayer.cs
git commit -m "feat(media): NAudio 版 IMediaPlayer 实现"
```
> 验收：手动在 PDF 音频面板（Task 9 完成后）验证播放/暂停/续播/定位/AB 循环。

---

## Task 8: WinApp 播放编排 + `PdfAudioTrackManager`

**Files:**
- Create: `LearningAssistant.WinApp/Services/Pdf/Audio/PdfAudioPlaybackService.cs`
- Create: `LearningAssistant.WinApp/Managers/Pdf/PdfAudioTrackManager.cs`

**Interfaces:**
- Consumes: `IMediaPlayer`（Task 2）、`IPdfAudioMatcher`/`IPdfAudioDirectoryScanner`/`IPdfAudioProfileStore`（Task 4/5/6）、`AudioMatchResult`
- Produces: `PdfAudioTrackManager.OnPageChanged(int pageIndex)`、`LoadProfile(string pdfPath, int pageCount)`、`Next()`/`Prev()`、`PlayOrPause()`、`SetManualOverride(int page, string file)`、`RunFollowRead()`

**Notes（验收点）**：
- `LoadProfile`：扫目录 → 指纹比对 → 命中则用缓存 `Tracks`，否则 `Matcher.Match` 重算 → 回写 `ScanFingerprint` + `Tracks` + `Save`。
- `OnPageChanged(page)`：本页有音频 && 播放中 → 切新歌从头播；本页有音频 && 暂停/停止 → 仅 Load；本页无音频 → `Stop` + 轻提示。
- `PdfAudioPlaybackService` 持有 `IMediaPlayer`，封装四增强；构造时注入 `IMediaPlayer`。

- [ ] **Step 1: 编写 `PdfAudioPlaybackService`**

```csharp
using LearningAssistant.Abstractions.Media;

namespace LearningAssistant.Services.Pdf.Audio;

public class PdfAudioPlaybackService
{
    private readonly IMediaPlayer _player;
    public IMediaPlayer Player => _player;
    public PdfAudioPlaybackService(IMediaPlayer player) => _player = player;

    public void Load(AudioTrack track)
    {
        _player.Stop();
        _player.Load(track.FilePath);
        _player.LoadSubtitle(track.SubtitlePath);
    }
    public Task PlayAsync() => _player.PlayAsync();
    public void Pause() => _player.Pause();
    public void Resume() => _player.Resume();
    public void Stop() => _player.Stop();
    public void Seek(TimeSpan p) => _player.Seek(p);
    public void SetSpeed(double s) { if (s >= 0.6 && s <= 2.0) _player.Speed = s; }
    public void SetAbLoop(TimeSpan? a, TimeSpan? b) => _player.AbLoop = (a, b);
}
```
> `using LearningAssistant.Models.Pdf;` 需要补在文件顶部（`AudioTrack` 来自 Task 1）。

- [ ] **Step 2: 编写 `PdfAudioTrackManager`**（关键逻辑完整）

```csharp
using LearningAssistant.Abstractions.Media;
using LearningAssistant.Common;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Platform.Media;
using LearningAssistant.Services.Media;
using LearningAssistant.Services.Pdf;
using LearningAssistant.Services.Pdf.Audio;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Managers;

public class PdfAudioTrackManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly IPdfReaderFormAccess _form;
    private readonly IPdfAudioMatcher _matcher;
    private readonly IPdfAudioDirectoryScanner _scanner;
    private readonly IPdfAudioProfileStore _store;
    private readonly PdfAudioPlaybackService _playback;
    private AudioMatchResult? _result;
    private PdfAudioProfile? _profile;
    private string? _currentPdfPath;
    private int _currentPageIndex = -1;
    private bool _disposed;

    public PdfAudioTrackManager(ILogger logger, IPdfReaderFormAccess form,
        IPdfAudioMatcher matcher, IPdfAudioDirectoryScanner scanner,
        IPdfAudioProfileStore store, ISubtitleParser subtitleParser)
    {
        _logger = logger; _form = form; _matcher = matcher; _scanner = scanner; _store = store;
        _playback = new PdfAudioPlaybackService(new NAudioMediaPlayer(subtitleParser));
    }

    public void LoadProfile(string pdfPath, int pageCount)
    {
        _currentPdfPath = pdfPath;
        _profile = _store.Load(pdfPath) ?? new PdfAudioProfile { PdfPath = pdfPath };
        var dir = Path.GetDirectoryName(pdfPath) ?? "";
        var tracks = _scanner.Scan(dir, _profile.IncludeSubDirectories);
        var fingerprint = PdfAudioDirectoryScanner.ComputeScanFingerprint(tracks);

        if (fingerprint != _profile.ScanFingerprint || _profile.Tracks.Count == 0)
        {
            _result = _matcher.Match(pdfPath, pageCount, tracks, _profile.Rules, _profile.ManualOverrides);
            _profile.Tracks = tracks;
            _profile.ScanFingerprint = fingerprint;
            _store.Save(_profile);
        }
        else
        {
            _result = new AudioMatchResult { Tracks = _profile.Tracks };
            foreach (var kv in _profile.ManualOverrides) _result.PageIndex[kv.Key] = FindOrCreate(kv.Value);
        }
    }

    public void OnPageChanged(int pageIndex)
    {
        if (_result == null || pageIndex == _currentPageIndex) { _currentPageIndex = pageIndex; return; }
        _currentPageIndex = pageIndex;
        if (_result.PageIndex.TryGetValue(pageIndex, out var track))
        {
            if (_playback.Player.State == MediaPlaybackState.Playing)
            { _playback.Load(track); FireAndForgetWithLogging(_playback.PlayAsync(), _logger); }
            else
            { _playback.Load(track); }
        }
        else
        {
            _playback.Stop();
        }
    }

    public void Next() => Move(1);
    public void Prev() => Move(-1);
    private void Move(int delta)
    {
        if (_result == null || _result.Tracks.Count == 0) return;
        var cur = _result.Tracks.FindIndex(t => _result.PageIndex.TryGetValue(_currentPageIndex, out var tt) && tt.FilePath == t.FilePath);
        var next = (_result.Tracks.Count + (cur < 0 ? 0 : cur) + delta) % _result.Tracks.Count;
        var track = _result.Tracks[next];
        _playback.Load(track);
        FireAndForgetWithLogging(_playback.PlayAsync(), _logger);
    }

    public void SetManualOverride(int page, string file)
    {
        if (_profile == null) return;
        _profile.ManualOverrides[page] = file;
        _store.Save(_profile);
        if (_result != null) _result.PageIndex[page] = FindOrCreate(file);
    }

    private AudioTrack FindOrCreate(string fileName)
    {
        var track = _profile?.Tracks.FirstOrDefault(t =>
            string.Equals(t.FileName, Path.GetFileName(fileName), StringComparison.OrdinalIgnoreCase));
        if (track == null)
        {
            track = new AudioTrack { FilePath = Path.Combine(Path.GetDirectoryName(_currentPdfPath) ?? "", fileName), FileName = Path.GetFileName(fileName), RelativePath = fileName };
            _profile?.Tracks.Add(track);
        }
        track!.IsManual = true;
        return track;
    }

    public void Dispose() { if (_disposed) return; _disposed = true; _playback.Stop(); _playback.Player.Dispose(); }
}
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build LearningAssistant.WinApp`
Expected: 编译通过（若 `FireAndForgetWithLogging` 命名空间不符，定位到 `LearningAssistant.Common` 的既有方法并调整 using）

- [ ] **Step 4: 提交**

```bash
git add LearningAssistant.WinApp/Services/Pdf/Audio/PdfAudioPlaybackService.cs LearningAssistant.WinApp/Managers/Pdf/PdfAudioTrackManager.cs
git commit -m "feat(pdf-audio): 播放编排与翻页联动管理器"
```

---

## Task 9: WinApp UI（`PdfAudioPanel` + `PdfAudioRuleDialog`）

**Files:**
- Create: `LearningAssistant.WinApp/Forms/Pdf/PdfAudioPanel.cs`
- Create: `LearningAssistant.WinApp/Forms/Pdf/PdfAudioRuleDialog.cs`

**Interfaces:**
- Consumes: `PdfAudioTrackManager`（Task 8）、`PdfAudioProfile`/`AudioTrack`/`AudioMatchRule`（Task 1）
- Produces: 挂在「🎤 音频」Tab 内的配套音频面板。

**Notes（验收点，WinForms 布局只列结构不逐像素）**：
- `PdfAudioPanel`（UserControl）成员：音频 ListBox、未匹配页 ListBox、播放条（▶/⏸/⏮/⏭/进度 TrackBar/倍速 ComboBox 或 `SpeedSelectorControl`）、AB(设A/设B/清除)、字幕开关+当前句 Label、跟读按钮、`规则配置`/`重新扫描`/`手动指定` 按钮、`IncludeSubDirectories` CheckBox。
- 事件委托回 `PdfAudioTrackManager`（如 `_manager.PlayAsync()`、`PlayPause()`、`Next()`、`Prev()`、`SetManualOverride(page, file)`、`RunFollowRead()`）。
- `PdfAudioRuleDialog`（Form）：规则 ListView（增/删/启停/上移下移=Priority）、正则 TextBox + 映射模式 ComboBox + PageOffset/PagesPerUnit NumericUpDown + 捕获组 TextBox、试算预览 ListBox、非法正则标红并阻止保存。保存后写回 `profile.Rules` 并 `store.Save`。

- [ ] **Step 1: 实现两窗体骨架与交互逻辑**（成员 + 事件处理，绘制细节按现有 Tab 风格）

- [ ] **Step 2: 编译验证**

Run: `dotnet build LearningAssistant.WinApp`
Expected: 编译通过

- [ ] **Step 3: 提交**

```bash
git add LearningAssistant.WinApp/Forms/Pdf/PdfAudioPanel.cs LearningAssistant.WinApp/Forms/Pdf/PdfAudioRuleDialog.cs
git commit -m "feat(pdf-audio): 配套音频面板与规则对话框"
```

---

## Task 10: DI 注册 + `PdfReaderForm` 集成

**Files:**
- Modify: `LearningAssistant.WinApp/Common/ServiceCollectionExtensions.cs`（`AddPdfServices` 内新增注册）
- Modify: `LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs`
- Modify: `LearningAssistant.WinApp/Managers/Pdf/IPdfReaderFormAccess.cs`（可选：新增 `PdfAudioTrackManager` 访问点）

**Interfaces:**
- Consumes: Task 3/4/5/6/7/8 全部产物

- [ ] **Step 1: DI 注册**（`AddPdfServices` 内追加）

```csharp
services.AddSingleton<ISubtitleParser, SubtitleParser>();
services.AddSingleton<IPdfAudioDirectoryScanner, PdfAudioDirectoryScanner>();
services.AddSingleton<IPdfAudioMatcher, PdfAudioMatcher>();
services.AddSingleton<IPdfAudioProfileStore, PdfAudioProfileStore>();
```
> 顶部补 `using LearningAssistant.Services.Media;`（若未引入）。

- [ ] **Step 2: `PdfReaderForm` 集成**

在字段区（`_audioManager` 附近）新增：
```csharp
private PdfAudioTrackManager? _audioTrackManager;
```

在 `InitializeManagers()` 的音频初始化块之后追加（沿用 `Program.ServiceProvider` 解析风格）：
```csharp
try
{
    var matcher = Program.ServiceProvider?.GetService(typeof(IPdfAudioMatcher)) as IPdfAudioMatcher;
    var scanner = Program.ServiceProvider?.GetService(typeof(IPdfAudioDirectoryScanner)) as IPdfAudioDirectoryScanner;
    var store = Program.ServiceProvider?.GetService(typeof(IPdfAudioProfileStore)) as IPdfAudioProfileStore;
    var subParser = Program.ServiceProvider?.GetService(typeof(ISubtitleParser)) as ISubtitleParser;
    if (matcher != null && scanner != null && store != null && subParser != null)
        _audioTrackManager = new PdfAudioTrackManager(_logger, this, matcher, scanner, store, subParser);
}
catch { /* 配套音频管理器初始化失败，不阻断 PDF 阅读 */ }
```
> 补 using：`LearningAssistant.Services.Media;`

在 `SetCurrentPdfPath(string pdfPath)`（约 L1398 `_currentPdfPath = pdfPath;` 之后）追加：
```csharp
_audioTrackManager?.LoadProfile(pdfPath, _presenter?.PageCount ?? 0);
```

在 `SetCurrentPageIndex(int pageIndex)`（约 L1570 `_currentPageIndex = pageIndex;` 之后）追加：
```csharp
_audioTrackManager?.OnPageChanged(pageIndex);
```
> 不启用 `PageChanged` 事件，防双触发（见 spec §七）。

在 `Dispose` 区（`_audioManager?.Dispose();` 之后）追加：
```csharp
_audioTrackManager?.Dispose();
```

- [ ] **Step 3: 编译验证 + 全量测试**

Run: `dotnet build` 然后 `dotnet test LearningAssistant.Core.Tests`
Expected: 全部通过

- [ ] **Step 4: 提交**

```bash
git add LearningAssistant.WinApp/Common/ServiceCollectionExtensions.cs LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs
git commit -m "feat(pdf-audio): DI 注册与阅读器翻页联动接入"
```

---

## Task 11（可选）: AI 规则建议 `PdfAudioAiSuggestionService`

**Files:**
- Create: `LearningAssistant.WinApp/Services/Pdf/Audio/PdfAudioAiSuggestionService.cs`

**Notes（验收点）**：收集目录结构（文件名+大小列表，不含内容）→ 调 `IAIService` → 解析建议正则与映射模式 → `PdfAudioRuleDialog` 展示为「建议」并试算，**用户确认后才保存**。AI 不可用/不可解析时静默降级，不阻断。此任务可延后，不影响 MVP。

---

## 执行顺序与验收总览

完成 Task 1–6 后 `dotnet test LearningAssistant.Core.Tests` 全绿；Task 7–10 后启动应用手动回归。手动验收清单（spec §十一）：翻页续播、暂停后翻页不响、无音频页提示、手动匹配持久化后重开命中、倍速不变调（spike 后）、AB 循环、字幕高亮/点句跳转、跟读对比、录音功能回归。