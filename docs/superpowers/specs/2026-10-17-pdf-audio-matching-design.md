# PDF 配套音频自动匹配与播放 — 设计文档

> 日期：2026-10-17
> 状态：待评审（v3：整合统一媒体内核 + 英语四增强）
> 范围：`LearningAssistant.Core`（模型 + 匹配引擎 + 字幕抽象），`LearningAssistant.WinApp`（播放 + UI）
> 关联代码：`Forms/Pdf/PdfReaderForm.cs`、`Managers/Pdf/PdfReaderAudioManager.cs`、`Core/Services/Pdf/IAudioRecorderService.cs`、`Platform/Media/NAudioMediaPlayer.cs`（新）
> 依赖文档：`2026-10-17-media-kernel-design.md`（`IMediaPlayer`）、`2026-10-17-video-resource-window-design.md`（spec2）

---

## 一、背景与目标

教材 PDF 常在**同目录**配有音频文件（课文朗读、听力材料等），当前 `PdfReaderForm` 已有「🎤 音频」Tab，但只支持录音，无法自动把"页"和"音频"关联起来。

**目标**：打开 PDF 后，按用户配置的规则自动把页面映射到同目录音频；翻页时自动切歌并保持播放状态；自动匹配不到的页支持手动匹配并持久化；支持借助 AI 分析目录结构给出规则建议。

**成功标准**：

- 翻到哪页听哪页，零手工点选；
- 暂停/续播即时响应；
- 手动匹配一次后，下次打开同一 PDF 自动命中；
- **不影响原有录音功能**。

---

## 二、已确认需求（决策记录）

| # | 决策 | 结论 |
|---|------|------|
| 1 | 匹配规则形式 | **仅正则**（作用于相对路径；非递归时相对路径 == 文件名） |
| 2 | 页映射粒度 | **可扩展策略**：页码直配 / 顺序映射 / 单元区间 / 自定义，由规则声明 |
| 3 | 翻页行为 | **切歌并保持播放状态**：播放中→切到新歌从头播；暂停/停止中→仅切歌不响 |
| 4 | 自动匹配不到的页 | **支持手动匹配**，存为独立覆盖项，下次自动生效 |
| 5 | AI 辅助 | 上传**目录结构**（文件名列表，不含内容）→ AI 建议正则 → 用户确认后存为规则；**绝不静默执行** |
| 6 | 练习册自动加载 | **不做**（本期移除） |
| 7 | 迷你播放条 | **不做**；播放器复用音频 Tab，Tab 内**模式切换** |
| 8 | 原录音功能 | **保持不变**，仅换挂载父面板 |
| 9 | 子目录扫描 | **默认不递归**，提供开关；递归时正则输入为「相对子目录路径 + 文件名」 |
| 10 | 架构路线 | **渐进版**：纯逻辑进 Core（阶段一即放），播放与 UI 留 WinApp |
| 11 | 统一媒体内核 | 播放能力抽为 `IMediaPlayer`（播放/暂停/续播/Seek/倍速/AB循环/字幕轨），NAudio 实现本 spec 用；视频实现（LibVLCSharp）供 spec2，见媒体内核文档 |
| 12 | 英语四增强 | **全部纳入本期内核**：① 倍速 0.6–2.0 不变调 ② AB 段落循环复读 ③ 字幕同步（`.lrc/.srt` 探测才启用、当前句高亮、点句跳转） ④ 跟读录音对比（复用 `NAudioRecorderService`，不做自动评分/ASR） |
| 13 | 视频资源 | **独立资源**（无 PDF 对照），内嵌 `LibVLCSharp VideoView`，后于本 spec 交付（spec2） |

---

## 三、总体架构

### 分层原则（审查修订）

**纯逻辑一律放 `LearningAssistant.Core`**（阶段一即放，不是阶段二再搬），因为：
- `PdfAudioMatcher` 等需要纯函数单测，而 WinApp 没有测试项目；
- 纯数据模型不依赖 WinForms，放 Core 零成本；
- 避免阶段二"搬运 + 重测"的二次成本。

**WinApp 只保留**：播放服务（NAudio）、`PdfAudioTrackManager`、UI 面板/对话框、依赖注入接线。

```
Core（模型 + 引擎 + 媒体抽象，可单测）
 ├─ Models/Pdf/AudioMatchRule, AudioTrack, PdfAudioProfile, AudioPageMapMode
 ├─ Abstractions/Media/IMediaPlayer, MediaPlaybackState, MediaLoopMode, SubtitleCue
 ├─ Services/Media/ISubtitleParser, SubtitleParser, AbLoopState        ← 媒体内核
 └─ Services/Pdf/Audio/
      ├─ PdfAudioDirectoryScanner   扫描同目录（可选递归）音频 + 探测 sidecar 字幕
      ├─ PdfAudioMatcher            正则规则 + 手动覆盖 → 页→音频索引 + 未匹配页
      └─ PdfAudioProfileStore       sidecar JSON 持久化（原子写）

WinApp（播放 + UI）
 └─ PdfReaderForm (IPdfReaderFormAccess)
      ├─ PdfReaderAudioManager  [改造为协调器]
      │   ├─ 子面板A「配套音频」 ← PdfAudioPanel（新增）
      │   └─ 子面板B「我的录音」 ← 原录音 UI 原样平移
      └─ PdfAudioTrackManager (新增)
           ├─ 依赖 Core 的 Matcher/Scanner/Store
           └─ PdfAudioPlaybackService（依赖 Core 的 IMediaPlayer + NAudioMediaPlayer 实现）

Platform/Media/NAudioMediaPlayer   IMediaPlayer 音频实现（媒体内核）
PdfAudioRuleDialog      规则配置 + 手动指定 + 试算预览
PdfAudioAiSuggestionService   （可选）调用 IAIService 产出建议正则
```

**播放服务依赖反转**：`PdfAudioPlaybackService` 依赖 Core 的 `IMediaPlayer`（而非具体 NAudio 类型），从而四增强（倍速/AB循环/字幕/跟读编排）可对假实现单测；NAudio 真实实现仅作手动集成验证。

---

## 四、领域模型（置于 `LearningAssistant.Core/Models/Pdf/`）

```csharp
public enum AudioPageMapMode
{
    PageIndex,  // 捕获值即页码（+ PageOffset）
    Sequence,   // 命中音频按捕获值/自然序排列，序号→页
    Range,      // 捕获值 = 单元号，配合 PagesPerUnit 展开页区间
    Custom      // 预留：显式页码映射表
}

public class AudioMatchRule   // 仅正则规则；手动覆盖 = 记录，不在规则列表
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string FileNamePattern { get; set; } = "";  // 正则，作用于相对路径
    public int? CaptureGroupIndex { get; set; }        // 序号捕获组（默认 1）
    public string? CaptureGroupName { get; set; }      // 命名捕获组（优先于 Index）
    public AudioPageMapMode MapMode { get; set; } = AudioPageMapMode.PageIndex;
    public int PageOffset { get; set; }                // PageIndex/Sequence 模式偏移
    public int PagesPerUnit { get; set; } = 1;         // Range 模式每单元页数
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; }                  // 越大越优先
}

public class AudioTrack
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string RelativePath { get; set; } = "";     // 相对 PDF 目录；非递归时 == 文件名
    public int MappedPageStart { get; set; } = -1;     // -1 表示未匹配
    public int MappedPageEnd { get; set; } = -1;
    public int DurationMs { get; set; }                // 懒加载
    public bool IsManual { get; set; }                 // 来自手动覆盖
    public string? SubtitlePath { get; set; }          // 探测到的同名 .srt/.lrc sidecar，无则 null
}

public class PdfAudioProfile
{
    public string PdfPath { get; set; } = "";
    public List<AudioMatchRule> Rules { get; set; } = new();              // 正则规则
    public Dictionary<int, string> ManualOverrides { get; set; } = new(); // 页→音频文件（最高优先级，不参与排序）
    public List<AudioTrack> Tracks { get; set; } = new();                 // 匹配结果缓存
    public bool IncludeSubDirectories { get; set; }
    public string ScanFingerprint { get; set; } = "";                     // 目录+mtime+size 指纹
}

// Matcher 输出结构（内存态，不必全部落盘）
public class AudioMatchResult
{
    public Dictionary<int, AudioTrack> PageIndex { get; set; } = new();   // 页→音频
    public IReadOnlySet<int> UnmatchedPages { get; set; } = new HashSet<int>();
    public List<AudioTrack> Tracks { get; set; } = new();
}
```

**字段说明**：`ScanFingerprint` 由「音频相对路径 + 修改时间 + 大小」排序后哈希，用于判断是否需重新匹配。手动覆盖放 `PdfAudioProfile.ManualOverrides`，独立于规则，**永远最高优先级**。

---

## 五、匹配引擎 `PdfAudioMatcher`（Core）

**输入**：PDF 路径、音频文件集（来自 Scanner）、规则集、`ManualOverrides`、**`pageCount`**、`IncludeSubDirectories`。
**输出**：`AudioMatchResult`（含 `PageIndex`、`UnmatchedPages`、`Tracks`）。

**流程**：

1. `PdfAudioDirectoryScanner` 枚举音频扩展名：`.mp3 .wav .m4a .aac .flac .ogg .wma`；
   - 默认只扫 PDF 同目录；`IncludeSubDirectories = true` 时递归子目录；
   - **正则输入统一为 `RelativePath`**（非递归时 `RelativePath == 文件名`），消除开关漂移；
   - 同时探测每个音频的同名 sidecar 字幕（`.srt`/`.lrc`），写入 `AudioTrack.SubtitlePath`，供字幕同步启用。
2. 按 `Priority` 降序，对每条 `Enabled` 的规则：
   - 用 `FileNamePattern` 匹配 `RelativePath`；
   - 取捕获值：`CaptureGroupName` 优先，否则用 `CaptureGroupIndex ?? 1`；
   - 按 `MapMode` 生成映射：
     - `PageIndex`：`page = value + PageOffset`；
     - `Sequence`：本规则所有命中项按捕获值升序（无捕获值按自然序）排列，第 i 项 → 第 `i + PageOffset` 页；
     - `Range`：`pageStart = (value-1)*PagesPerUnit + PageOffset + 1`，`pageEnd = pageStart + PagesPerUnit - 1`；
     - `Custom`：查规则内显式映射表（预留，本期可不实现 UI）。
3. **叠加手动覆盖**：对 `ManualOverrides` 中每个 `page→file` 直接写入 `PageIndex`，覆盖同页正则结果，`AudioTrack.IsManual = true`。
4. **未匹配页**：`[0, pageCount)` 范围内不在 `PageIndex` 的页，写入 `UnmatchedPages`，供 UI 提示手动指定。
5. 冲突与容错：多正则规则命中同一页取 `Priority` 高者；正则非法/捕获组缺失 → 跳过该规则并记录诊断（不抛异常）。
6. 结果与手动覆盖写回 `PdfAudioProfile`，落盘（原子写）。

---

## 六、播放服务（WinApp）

`PdfAudioPlaybackService` 基于 **Core `IMediaPlayer`**（实现为 `Platform/Media/NAudioMediaPlayer`，`WaveOutEvent` + `AudioFileReader`）。播放服务自身不再是另一套播放器，而是**编排层**：持有 `IMediaPlayer`，负责「页 → track → 字幕 → 四增强」联动。

| 能力 | 说明 |
|------|------|
| `Load(track)` / `PlayAsync` / `Pause` / `Resume` / `Stop` | 直接委托 `IMediaPlayer`；`Pause` 保持位置，`Resume` 续播 |
| `Seek(pos)` | 委托 `IMediaPlayer.Seek` |
| 倍速 | 委托 `IMediaPlayer.Speed`（0.6–2.0 不变调）；引擎见媒体内核文档，实现前 spike，失败则阶段一砍倍速（UI 禁用，不塞占位） |
| AB 循环复读 | 委托 `IMediaPlayer.AbLoop`（`AbLoopState` 纯类）；播放条提供「设 A / 设 B / 清除」 |
| 字幕同步 | `PdfAudioDirectoryScanner` 探测 track 同名 `.srt`/`.lrc` sidecar → `IMediaPlayer.LoadSubtitle`；UI 高亮当前句 + 点句 `Seek(Cue.StartTime)` |
| 跟读录音对比 | 编排：播原音 → 复用 `NAudioRecorderService` 录音 → 列表回放手动对比；不做评分 |
| `Next()` / `Prev()` | 依据当前页索引在已匹配 track 列表中切换 |
| 事件 | `StateChanged`、`PositionChanged`、`PlaybackFinished`（媒体内核定义） |

**接口衔接（修订）**：不再新建 `ITrackPlayer`。播放统一走 Core 的 `IMediaPlayer`（见 `2026-10-17-media-kernel-design.md`）；既有 `IAudioPlayer` 保留给 TTS，不动。`PdfAudioPlaybackService` 依赖 `IMediaPlayer`（注入 `NAudioMediaPlayer`），四增强对假实现可单测。

**互斥（归属明确）**：录音（`NAudioRecorderService`）与播放（`IMediaPlayer`）互斥由 **`PdfReaderForm` 层协调**——它同时持有 `PdfReaderAudioManager`（录音/跟读）与 `PdfAudioTrackManager`（播放）引用；任一侧启动前判断对侧状态并提示。播放服务不反向依赖录音。

---

## 七、与 PDF 阅读器集成（翻页联动）

**挂钩点**：`PdfReaderForm.SetCurrentPageIndex(int)`（`Forms/Pdf/PdfReaderForm.cs:1563`）——现行唯一页码变更入口。

**触发路径（单一，防双触发）**：仅在 `SetCurrentPageIndex` 内直接调用 `_audioTrackManager.OnPageChanged(page)`。**不启用 `PageChanged` 事件**（保持闲置），避免 manager 同时直接调用又订阅事件造成重复。

**行为**（`PdfAudioTrackManager.OnPageChanged(page)`）：

| 场景 | 行为 |
|------|------|
| 本页有音频 + 当前**播放中** | 切到新歌，从 0 开始播放（保持播放状态） |
| 本页有音频 + 当前**暂停/停止** | 仅切歌并加载，保持不响 |
| 本页无音频 | 停止播放并轻提示「本页无配套音频」 |

- 双页模式取当前页（左页）索引；
- 提供「上一首 / 下一首」按钮覆盖自动逻辑；
- 打开新 PDF（`SetCurrentPdfPath`，`:1393`）时重新加载该 PDF 的 profile 并刷新配套音频列表。

---

## 八、UI 设计

### 8.1 音频 Tab 改造（不破坏原功能）

「🎤 音频」Tab 顶部新增**子切换**（RadioButton 或子 TabControl），两个面板：

- **「配套音频」（新增）**：`PdfAudioPanel`，内含两个视图：
  - **音频列表**（按 `AudioTrack`）：文件名 / 映射页范围 / 时长 / 匹配状态（已匹配 / 手动）；
  - **未匹配页列表**（按 `UnmatchedPages` 的页码）：在此选中页码发起"手动指定"——它和音频列表是**两种视图**，不能混用。
  - 播放控制条：▶/⏸、⏮/⏭、进度条（可拖拽 Seek）、倍速控件（复用 `SpeedSelectorControl` 交互风格）、当前曲名；
  - 英语四增强控件（见媒体内核文档）：
    - **AB 循环**：`设 A`、`设 B`、`清除`（状态显示「A 03:12 — B 04:05 循环中」）；
    - **字幕**：开关 + 「当前句」高亮显示行；点击历史句 `Seek(Cue.StartTime)` 跳转；
    - **跟读**：`跟读练习` 按钮 → 播放原音 → 录音 → 「我的跟读」列表回放手动对比（不做评分）；
  - 按钮：`规则配置`、`重新扫描`、`手动指定`（对选中的"未匹配页"）；
  - `IncludeSubDirectories` 开关。
- **「我的录音」（原有）**：原 `PdfReaderAudioManager` 的录音 UI **整体平移**，行为与外观不变。

`PdfReaderAudioManager` 改为**协调器**：持有两个子面板，录音逻辑照旧（仅父容器由 Tab 换成子面板），新增配套音频面板的托管。**这是本次改动唯一的既有文件结构性改动，需重点回归测试录音功能。**

### 8.2 `PdfAudioRuleDialog`（规则配置）

- 规则列表：增 / 删 / 启停 / 排序（Priority）；
- 规则编辑：正则输入 + 映射模式 + 参数（PageOffset / PagesPerUnit）+ 捕获组（命名或序号）；
- **实时试算预览**：列出「相对路径 → 命中页」，规则编写者看到的就是匹配引擎最终输入；
- 非法正则**即时标红**并阻止保存；
- 提供「由当前手动匹配归纳为规则」入口（把已手动指定的页/文件反推建议正则，用户确认后保存）。

### 8.3 手动匹配

- 在「未匹配页」列表中选中页码 →「手动指定音频」→ 选择目录内音频文件 → 写入 `PdfAudioProfile.ManualOverrides[page] = file` → 立即生效并持久化；
- 下次打开同一 PDF 时 `PdfAudioMatcher` 直接命中，无需重复操作。

### 8.4 AI 辅助匹配（可选）

- `PdfAudioAiSuggestionService`：收集**目录结构**（音频文件名 + 大小列表，**不含文件内容**）→ 构造 prompt → 调用 `IAIService` → 解析返回的**建议正则与映射模式**；
- 结果**不自动写入**：在 `PdfAudioRuleDialog` 中作为"建议"展示并试算预览，**用户确认后**才存为规则；
- AI 不可用/返回不可解析时，静默降级为纯手动配置（不报错阻断）。

---

## 九、持久化与配置

- **每 PDF 一份 sidecar**：`AppPaths.AudioDir/Profiles/{pdfHash}.json`（`pdfHash` = PDF 绝对路径的稳定哈希；新增 `AppPaths.AudioProfilesDir`）；
- **原子写**：写临时文件 → `File.Move(temp, target, overwrite: true)`；
- **缓存判定**：仅当 `ScanFingerprint` 变化时重算匹配；`ManualOverrides` 变更时增量更新（不触发全量重算）；
- **失效清理**：加载时校验 `File.Exists`，失效项标灰并在下次扫描时移除；
- `IncludeSubDirectories` 存于 profile 级别。

---

## 十、错误处理与边界

| 场景 | 处理 |
|------|------|
| 同目录无音频 | 列表显示空态提示，不报错 |
| 匹配结果 0 条 | 提示「未匹配到音频，可手动指定」 |
| 正则非法 / 捕获组缺失 | 规则对话框即时标红；引擎侧跳过该规则并记录日志 |
| 音频被移动/删除 | 加载时 `File.Exists` 校验，标灰 + 下次扫描清理 |
| 大目录（数百音频） | 后台线程扫描，UI 不卡；提供取消 |
| 录音与播放冲突 | 由 `PdfReaderForm` 协调互斥并提示 |
| 多实例并发写 profile | 原子写 + 基于路径的互斥（沿用项目既有 `Global\` 互斥锁风格） |

---

## 十一、测试策略

**纯函数单测**（`LearningAssistant.Core.Tests`，`PdfAudioMatcher`，因 matcher 在 Core）：

- 页码直配：`P012.mp3` → 第 12 页（含 `PageOffset`）；
- 顺序映射：`01/02/03.mp3` → 第 1/2/3 页；
- 单元区间：`Unit3.mp3` + `PagesPerUnit=10` → 第 21–30 页；
- 多规则优先级冲突取高者；
- 手动覆盖覆盖正则结果；
- 非法正则 → 跳过且不抛异常；
- 递归扫描时正则匹配相对路径；
- 未匹配页集合正确（含 `pageCount` 影响）。

**媒体内核单测**（`LearningAssistant.Core.Tests`）：

- `SubtitleParser`：srt 标准/多行/无序号；lrc 单/多标签/元数据跳过；编码回退；非法行跳过不抛异常（详见媒体内核文档 §九）；
- `AbLoopState`：未设 A/B 不回跳；`position < B` 不回跳；`position >= B` 回 A；B < A 无效输入按未启用处理。

**播放服务**：用 `IMediaPlayer` 假实现（内存位置源）验证 播放→暂停→续播→定位→切歌 状态机与四增强编排（AB 回跳、字幕点句跳转）；NAudio 真实实现仅做手动集成验证（避免依赖真实声卡）。

**手动验证清单**：翻页续播、暂停后翻页不响、无音频页提示、手动匹配持久化后重开 PDF 自动命中、倍速不变调、AB 循环、字幕高亮/点句跳转、跟读录音回放、录音功能回归。

---

## 十二、落地阶段与文件清单

### 阶段一（MVP）

| 步骤 | 产出 |
|------|------|
| 1 | Core 模型（`Models/Pdf/`）+ `PdfAudioProfileStore`（原子写）+ `AppPaths.AudioProfilesDir` |
| 2 | Core `PdfAudioDirectoryScanner`（含 sidecar 字幕探测）+ `PdfAudioMatcher`（正则 + 手动覆盖 + 单测） |
| 3 | Core 媒体内核：`SubtitleParser` + `AbLoopState` + `IMediaPlayer` 抽象（见媒体内核文档） |
| 4 | WinApp `NAudioMediaPlayer`（`IMediaPlayer` 音频实现）+ `PdfAudioPlaybackService`（四增强编排） |
| 5 | WinApp `PdfAudioTrackManager`（串联 + 翻页联动） |
| 6 | WinApp UI：音频 Tab 双子面板切换 + `PdfAudioPanel`（含倍速/AB/字幕/跟读）+ `PdfAudioRuleDialog`（含手动指定/试算） |
| 7 | （可选）WinApp `PdfAudioAiSuggestionService`（AI 建议正则） |

**新增文件 — Core**

- `LearningAssistant.Core/Models/Pdf/AudioPageMapMode.cs`
- `LearningAssistant.Core/Models/Pdf/AudioMatchRule.cs`
- `LearningAssistant.Core/Models/Pdf/AudioTrack.cs`
- `LearningAssistant.Core/Models/Pdf/PdfAudioProfile.cs`
- `LearningAssistant.Core/Models/Pdf/AudioMatchResult.cs`
- `LearningAssistant.Core/Services/Pdf/Audio/IPdfAudioDirectoryScanner.cs`
- `LearningAssistant.Core/Services/Pdf/Audio/PdfAudioDirectoryScanner.cs`
- `LearningAssistant.Core/Services/Pdf/Audio/IPdfAudioMatcher.cs`
- `LearningAssistant.Core/Services/Pdf/Audio/PdfAudioMatcher.cs`
- `LearningAssistant.Core/Services/Pdf/Audio/IPdfAudioProfileStore.cs`
- `LearningAssistant.Core/Services/Pdf/Audio/PdfAudioProfileStore.cs`

**新增文件 — Core（媒体内核）**

- `LearningAssistant.Core/Abstractions/Media/IMediaPlayer.cs`
- `LearningAssistant.Core/Abstractions/Media/MediaPlaybackState.cs`
- `LearningAssistant.Core/Abstractions/Media/MediaLoopMode.cs`
- `LearningAssistant.Core/Abstractions/Media/SubtitleCue.cs`
- `LearningAssistant.Core/Services/Media/ISubtitleParser.cs`
- `LearningAssistant.Core/Services/Media/SubtitleParser.cs`
- `LearningAssistant.Core/Services/Media/AbLoopState.cs`

**新增文件 — WinApp**

- `LearningAssistant.WinApp/Platform/Media/NAudioMediaPlayer.cs`（`IMediaPlayer` 音频实现）
- `LearningAssistant.WinApp/Services/Pdf/Audio/PdfAudioPlaybackService.cs`
- `LearningAssistant.WinApp/Managers/Pdf/PdfAudioTrackManager.cs`
- `LearningAssistant.WinApp/Forms/Pdf/PdfAudioPanel.cs`
- `LearningAssistant.WinApp/Forms/Pdf/PdfAudioRuleDialog.cs`
- `LearningAssistant.WinApp/Services/Pdf/Audio/PdfAudioAiSuggestionService.cs`（步骤 7，可选）

**修改文件**

- `LearningAssistant.Core/Common/AppPaths.cs` → 新增 `AudioProfilesDir`
- `LearningAssistant.WinApp/Managers/Pdf/PdfReaderAudioManager.cs` → 改造为协调器（原录音 UI 平移）
- `LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs` → Tab 子切换、`SetCurrentPageIndex` 联动
- `LearningAssistant.WinApp/Managers/Pdf/IPdfReaderFormAccess.cs` → 新增配套音频访问点
- `LearningAssistant.WinApp/Common/ServiceCollectionExtensions.cs` → 注册 Core matcher/scanner/store + 媒体内核（`SubtitleParser`/`AbLoopState`）+ WinApp 播放服务

### 阶段二（视频独立资源窗口）

- 见 `2026-10-17-video-resource-window-design.md`（spec2）：引入 LibVLCSharp，`VlcMediaPlayer` 实现 `IMediaPlayer`，新建 `VideoResourceForm`；
- AI 建议正式启用（若阶段一未做）；spec1 的 UI 不变。

---

## 十三、风险与取舍

| 风险 | 取舍 / 缓解 |
|------|-------------|
| 正则门槛高，易写错 | 强制「试算预览 + 非法标红」；提供 AI 建议与"由手动匹配归纳规则" |
| 倍速播放音频侧引擎 | 实施前 spike：时域拉伸（复用 `BaseTtsService` WSOLA）+ Seek/热切速可用性；失败则阶段一砍倍速（UI 禁用，不塞占位），见媒体内核文档 §七 |
| AB 循环衔接字幕/翻页 | `AbLoopState` 纯类 + 位置容差；翻页切歌时重置 AB 状态 |
| 递归扫描性能 | 后台线程 + 指纹缓存 + 可取消 |
| 改动 `PdfReaderAudioManager` 可能影响录音 | 录音逻辑不重写、只换父面板；录音功能需回归测试 |
| 手动覆盖与正则并存 | 独立字典 + 无条件覆盖，语义清晰、不参与优先级 |
| 字幕 sidecar 探测编码/对齐 | 仅解析现成 `.srt`/`.lrc`，GB18030 编码回退；不做自动对齐 |

---

## 十四、明确不做（Out of Scope）

- 练习册检测与自动加载；
- 底部迷你播放条；
- 音频内容上传给 AI（只上传目录结构，不上传音频）；
- 波形可视化、**逐句自动对齐**（无现成 `.srt`/`.lrc` 时自动切分）、音频时间戳标注（本期不做；现成字幕文件的同步**做**）；
- 跟读自动评分 / ASR（跟读对比仅手动回放）。