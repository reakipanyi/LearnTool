# 统一媒体内核（IMediaPlayer）— 设计文档

> 日期：2026-10-17
> 状态：待评审
> 范围：`LearningAssistant.Core`（抽象 + 字幕解析纯逻辑），`LearningAssistant.WinApp`（NAudio 音频实现；LibVLCSharp 视频实现，spec2 引入）
> 依赖关系：本内核被 spec1（PDF 配套音频）与 spec2（视频独立资源窗口）共同复用。
> 关联代码：`Core/Abstractions/Audio/IAudioPlayer.cs`、`Platform/Audio/WinFormsAudioPlayer.cs`、`Services/TTS/BaseTtsService.cs`（现有变速实现）、`Services/Pdf/NAudioRecorderService.cs`（跟读录音复用）

---

## 一、背景与目标

英语阅读场景需要「音频 + 视频」两类媒体的统一播放能力。现有播放散落两处：`IAudioPlayer`/`WinFormsAudioPlayer`（只能 `PlayAsync/Stop`，无暂停/续播/定位/倍速）、`BaseTtsService`（内含自用变速实现）。视频无任何播放能力。

**目标**：抽取一个最小但完整的媒体播放抽象 `IMediaPlayer`，覆盖英语学习四增强（倍速、AB 循环复读、字幕同步、跟读），由 NAudio（音频）与 LibVLCSharp（视频）两个实现共享；字幕解析作为纯逻辑下沉 Core 以便单测。

**成功标准**：

- 同一套播放语义（播放/暂停/续播/定位/倍速/AB 循环/字幕轨）在音频与视频上表现一致；
- 字幕解析（`.srt`/`.lrc`）可脱离播放器单测；
- 音频倍速「变速不变调」，0.6–2.0 无花栗鼠效应；
- 跟读录音复用既有 `NAudioRecorderService`，不做自动评分/ASR。

---

## 二、已确认需求（决策记录）

| # | 决策 | 结论 |
|---|------|------|
| 1 | 抽象层次 | `IMediaPlayer` 放 **Core**（无 WinForms/NAudio/LibVLC 依赖），两实现放 WinApp |
| 2 | 能力边界 | 播放 / 暂停 / 续播 / Seek / 倍速 / AB 循环 / 字幕轨；**不含** 波形可视化、自动评分 |
| 3 | 倍速 | 0.6–2.0，**变速不变调**；音频改用「时域拉伸」方案，视频用 LibVLC `SetRate` |
| 4 | AB 循环 | 接口一级能力；统一由「位置到达 B → Seek 回 A」实现，音频/视频同策略 |
| 5 | 字幕 | 解析纯逻辑放 Core（`SubtitleParser`）；`.lrc`/`.srt` **探测到 sidecar 才启用**；UI 高亮当前句 + 点句跳转 |
| 6 | 跟读对比 | 复用 `NAudioRecorderService`；「播原音 → 录跟读 → 手动回放对比」，不做 ASR/自动打分 |
| 7 | 既有 `IAudioPlayer` | 保留不动；TTS 继续用它。`IMediaPlayer` 是新抽象，不改造 TTS 路径 |
| 8 | 视频实现 | spec2 引入 `LibVLCSharp`（含 `VideoView`），spec1 不引该包 |

---

## 三、总体架构

```
Core（抽象 + 纯逻辑，可单测）
 ├─ Abstractions/Media/
 │    ├─ IMediaPlayer.cs          播放抽象
 │    ├─ MediaPlaybackState.cs    状态枚举
 │    ├─ MediaLoopMode.cs         循环模式枚举
 │    └─ SubtitleCue.cs           字幕条目
 └─ Services/Media/
      ├─ ISubtitleParser.cs
      └─ SubtitleParser.cs        .srt / .lrc 解析（纯函数）

WinApp（实现）
 ├─ Platform/Media/NAudioMediaPlayer.cs       IMediaPlayer（音频，spec1 用）
 └─ Platform/Media/VlcMediaPlayer.cs          IMediaPlayer（视频，spec2 用，引 LibVLCSharp）
```

**分层原则**：接口与字幕模型不含任何 UI/第三方库类型，便于 Core.Tests 直接单测字幕解析与 AB 循环边界逻辑（AB 循环判定抽成 `AbLoopState` 纯类，见 §五）。NAudio/LibVLC 实现只做「系统调用 + 事件回填」，不含业务判断。

---

## 四、核心接口与模型（`Core/Abstractions/Media/`）

```csharp
public enum MediaPlaybackState { Idle, Playing, Paused, Stopped }

public enum MediaLoopMode { None, RepeatOne, RepeatAll, AbLoop }

public sealed class SubtitleCue
{
    public int Index { get; set; }          // .srt 序号 / .lrc 行号
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }   // .lrc 无结束时间时 = 下一条 StartTime
    public string Text { get; set; } = "";
}

public interface IMediaPlayer : IDisposable
{
    MediaPlaybackState State { get; }

    /// <summary>当前播放位置；实现不支持定位时返回 TimeSpan.Zero。</summary>
    TimeSpan Position { get; }

    /// <summary>总时长；未加载或未知时为 TimeSpan.Zero。</summary>
    TimeSpan Duration { get; }

    /// <summary>0.6–2.0，变速不变调；设置即生效（播放中热切换）。</summary>
    double Speed { get; set; }

    /// <summary>循环模式；RepeatOne/RepeatAll 媒体播完自动重播。</summary>
    MediaLoopMode LoopMode { get; set; }

    /// <summary>AB 段落循环边界；A/B 任一为 null 表示未启用。</summary>
    (TimeSpan? A, TimeSpan? B) AbLoop { get; set; }

    void Load(string mediaPath);

    /// <summary>从当前位置播放（若无加载内容则从头）。</summary>
    Task PlayAsync(CancellationToken ct = default);

    void Pause();     // 保持当前位置
    void Resume();    // 从暂停点续播
    void Stop();      // 停止并回到起点
    void Seek(TimeSpan position);

    /// <summary>加载字幕；传 null 清除。仅在探测到 sidecar 时调用。</summary>
    void LoadSubtitle(string? subtitlePath);

    /// <summary>已加载字幕条目；无字幕为 null。</summary>
    IReadOnlyList<SubtitleCue>? SubtitleCues { get; }

    event EventHandler<TimeSpan>? PositionChanged;
    event EventHandler<MediaPlaybackState>? StateChanged;
    event EventHandler? PlaybackFinished;
}
```

**契约说明**：

- `PlayAsync` 语义统一为「从当前位置播放」；首次 `Load` 后为 0。**仅启动播放并返回，完成由 `PlaybackFinished` 事件通知**（与既有 `IAudioPlayer.PlayAsync` 的「await 至播完」语义不同）。调用方对返回值用 `FireAndForgetWithLogging` 包装、不阻塞 UI。非事件方法一律 `Task` + `Async` 后缀（项目约束）。
- `Speed` 热切换：播放中修改即时生效；实现端不得在改速时重建导致位置丢失。
- `PositionChanged` 的触发频率与实现相关（音频用内部定时器主动上报，视频用 LibVLC `TimeChanged`）；UI 用它驱动字幕高亮与进度条，**跨线程回调前必须 `IsHandleCreated` 检查**。
- `PlaybackFinished` 在「自然播完」与「RepeatOne/RepeatAll 不成立时」触发；AB 循环内回到 A 不算 finished。
- `IMediaPlayer` 与既有 `IAudioPlayer` 互不影响；TTS 继续走 `IAudioPlayer`。

---

## 五、字幕解析 `SubtitleParser`（Core，纯函数）

**输入**：字幕文件路径（`.srt` / `.lrc`）。
**输出**：`IReadOnlyList<SubtitleCue>`（按时间升序）。

**规则**：

| 格式 | 解析要点 |
|------|----------|
| `.srt` | 序号行 → `HH:MM:SS,mmm --> HH:MM:SS,mmm` → 单行或多行文本，空行分隔；容错：序号可缺，时间戳支持 `HH:MM:SS.mmm` |
| `.lrc` | `[mm:ss.xx]` 单/多时间标签行；多标签一行文本 → 拆成多条 Cue；跳过 `[ti:]`/`[ar:]` 等元数据标签；无结束时间 → `EndTime = 下一条 StartTime`，末条 = `Duration` 或空白 |

**错误处理**：编码非 UTF-8 时按 `GB18030` 回退（中文 lrc 常见）；非法时间戳行跳过并记录诊断，不抛异常；空文件 → 空列表。

**AB 循环边界纯类 `AbLoopState`（Core，可单测）**：

```csharp
public sealed class AbLoopState
{
    public TimeSpan? A { get; set; }
    public TimeSpan? B { get; set; }

    /// <summary>给定当前位置，返回是否应回跳及目标（true, A）。</summary>
    public bool ShouldLoopBack(TimeSpan position, out TimeSpan target);
}
```

两实现的 AB 循环共用该类：`position >= B` → 回 `A`。避免两侧各自实现一半导致行为漂移。

---

## 六、英语四增强的归属

| 增强 | 归属 | 说明 |
|------|------|------|
| ① 倍速 | `IMediaPlayer.Speed` | 音频时域拉伸；视频 `SetRate`。**不塞占位，不可用则该项砍掉并明确提示** |
| ② AB 循环复读 | `IMediaPlayer.AbLoop` + `AbLoopState` | 位置到 B 回 A；UI 提供「设 A / 设 B / 清除」 |
| ③ 字幕同步 | `SubtitleParser` + `IMediaPlayer.SubtitleCues` | 探测 sidecar 才启用；UI 高亮当前句、点句 `Seek(Cue.StartTime)` |
| ④ 跟读对比 | **非内核能力**，复用 `NAudioRecorderService` | 由播放协调器编排：先播原音，再录音，再手动回放；不做评分 |

**跟读对比交互（spec1 的配套音频面板内）**：按钮「跟读练习」→ 播放原音 → 用户点名「开始录音」→ 复用 `NAudioRecorderService` 录制 → 停后列表出现「我的跟读」条目 → 可逐条回放对比。录音与播放互斥沿用既有协调。

---

## 七、实现矩阵

| 能力 | NAudioMediaPlayer（spec1） | VlcMediaPlayer（spec2） |
|------|---------------------------|------------------------|
| 引擎 | `WaveOutEvent` + `AudioFileReader` | `LibVLCSharp` `VlcMediaPlayer` + `VideoView` |
| 播放/暂停/续播 | `WaveOutEvent.Pause()` / `Play()` | `Pause()` / `Play()` |
| Seek/Position | `AudioFileReader.CurrentTime` + 定时器上报 | `Time` / `TimeChanged` |
| 倍速 | 时域拉伸（WSOLA，复用 `BaseTtsService` 已验证实现）；**禁止** NAudio 内置 `VarispeedWaveStream` | `SetRate(double)` |
| AB 循环 | `AbLoopState.ShouldLoopBack` + `Seek(A)` | 同左，订阅 `TimeChanged` |
| 字幕 | Core `SubtitleParser` + 自行上报位置 | 可借 LibVLC 原生字幕渲染，但仍经 `SubtitleParser` 暴露 Cues 供「点句跳转」 |
| RepeatOne/All | `PlaybackStopped` 后判断 `LoopMode` 重播 | `EndReached` + `SetMedia` 重播 |

> **倍速技术风险（须先 spike）**：`MediaFoundationResampler` 只做采样率转换、不直接做变速不变调。项目既有 `BaseTtsService` 内的 WSOLA 时域拉伸已验证可用，但与「定位/续播」交互（拉伸缓存 vs 实时）需评估。**实施前先做一个 spike**：确认「AudioFileReader → 时域拉伸 provider → WaveOutEvent」链路下 `Seek` 与热切速可用、无花栗鼠。若 spike 失败，spec1 阶段一砍掉倍速（保留 `Speed` 属性但默认 1.0 且 UI 禁用），不塞占位逻辑。

---

## 八、文件清单

**新增文件 — Core**

- `LearningAssistant.Core/Abstractions/Media/IMediaPlayer.cs`
- `LearningAssistant.Core/Abstractions/Media/MediaPlaybackState.cs`
- `LearningAssistant.Core/Abstractions/Media/MediaLoopMode.cs`
- `LearningAssistant.Core/Abstractions/Media/SubtitleCue.cs`
- `LearningAssistant.Core/Services/Media/ISubtitleParser.cs`
- `LearningAssistant.Core/Services/Media/SubtitleParser.cs`
- `LearningAssistant.Core/Services/Media/AbLoopState.cs`

**新增文件 — WinApp**

- `LearningAssistant.WinApp/Platform/Media/NAudioMediaPlayer.cs`（spec1）
- `LearningAssistant.WinApp/Platform/Media/VlcMediaPlayer.cs`（spec2，随 LibVLCSharp 引入）

**修改文件**

- `LearningAssistant.WinApp/Common/ServiceCollectionExtensions.cs` → 注册 `SubtitleParser` / `AbLoopState`（单例）；`IMediaPlayer` 注册为 Transient（每次播放会话新建，避免跨文档状态串扰）

---

## 九、测试策略（`LearningAssistant.Core.Tests`）

纯逻辑部分全部单测（不依赖声卡/窗体）：

- `SubtitleParser`：srt 标准/多行/无序号/`.sss` 变体；lrc 单/多标签/元数据跳过/编码回退；非法行跳过不抛异常；
- `AbLoopState`：未设 A/B 不回跳；`position < B` 不回跳；`position >= B` 回 A；B < A 的无效输入按未启用处理；
- `IMediaPlayer` 用假实现（内存位置源）验证 状态机：Load→Play→Pause→Resume→Seek→Stop→PlaybackFinished 顺序与事件；
- NAudio/LibVLC 真实实现仅做手动集成验证（依赖真实声卡/显卡）。

---

## 十、风险与取舍

| 风险 | 取舍 / 缓解 |
|------|-------------|
| 音频倍速引擎选择不确定 | 实施前 spike（见 §七）；失败则阶段一砍倍速，不塞占位 |
| AB 循环依赖位置上报精度 | 音频定时器 100ms 级；到达判定含容差（`B - position <= 容差` 即触发） |
| 跟读录音复用 `WaveInEvent` 与播放互斥 | 由协调器（spec1 `PdfAudioTrackManager`/`PdfReaderForm`）统一保证 |
| LibVLCSharp 体积/依赖 | 仅 spec2 引入；spec1 完全不引，避免为音频功能拖入视频运行时 |
| `PositionChanged` 高频回调压 UI | UI 侧节流（如 200ms）或仅更新进度/字幕，不整页重绘 |

## 十一、明确不做（Out of Scope）

- 波形可视化、音频频谱、人声/伴奏分离；
- 逐句逐词自动对齐（.mp3 无现成句级时间戳时不自动切分）；
- 跟读自动评分 / ASR；
- 替换既有 TTS 的 `IAudioPlayer` 播放路径。