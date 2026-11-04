# 音频进度条「静音断句」打点与点击预览 — 技术方案

- 状态：设计待评审（本文只定方案，不含实现）
- 日期：2026-11-04
- 范围：`PdfAudioPlayerBar` 的进度条升级为「句子刻度时间轴」，支持点句/点刻度预览单句；整本音频增加页码刻度层
- 关联代码：
  - `LearningAssistant.WinApp/Forms/Pdf/PdfAudioPlayerBar.cs`
  - `LearningAssistant.WinApp/Managers/Pdf/PdfAudioTrackManager.cs`
  - `LearningAssistant.WinApp/Platform/Media/NAudioMediaPlayer.cs`
  - `LearningAssistant.WinApp/Services/Shadowing/VadSegmentService.cs`
  - `LearningAssistant.Core/Services/Shadowing/SilenceVadSegmenter.cs`
  - `LearningAssistant.Core/Services/Shadowing/ShadowingSegmentStore.cs`
  - `LearningAssistant.Core/Abstractions/Media/IMediaPlayer.cs`
  - `LearningAssistant.WinApp/Common/UI/PdfToolbarSlider.cs`（自绘滑杆范式）

### 1.0 已确认的产品决策（2026-11-04）

| # | 决策 | 对方案的影响 |
| --- | --- | --- |
| 1 | 点句默认「播完暂停」 | 单句预览是主行为；`Shift+单击` 才连续播放 |
| 2 | 无字幕时只显示「第 N 句 + 时间」 | 不强制做 OCR/文本对齐；文本为可选增强 |
| 3 | 粒度按精听短句 3–8 秒 | `MaxSentenceMs = 8000`，长段强制切分；默认预设改为「精听」 |
| 4 | P0 需要最小手动修正（建议） | P0 提供「拖一个边界 / 合并 / 拆分 / 撤销」，不提供完整编辑器；手动数据存用户目录 |
| 5 | 与现有跟读切片共用边界 | 跟读切片作为**高优先级边界来源**；P0 只读复用，写回跟读切片放 P1（见 §7.3 风险） |
| 6 | 整本书音频叠一层「页码起点」 | 新增页码刻度层；页码时间点必须有来源（切片/手动/插值），不能凭空按页数均分 |

---

## 1. 结论先行

### 1.1 一句话方案

以**跟读切片边界优先、静音 VAD 兜底**的方式得到「句子区间」（每段有明确的 `StartMs/EndMs`），把每句起点画成进度条刻度；短句密度高时默认启用「点句模式」——单击轨道任意位置即预览**最近一句**，拖动才做普通 seek。悬停显示「第 N 句 + 起止时间」，有字幕时叠加文本；整本书单音频额外叠加一层页码起点。断句与手动修正落盘（分析缓存 + 用户修正分离），按音频指纹失效，分析全程后台线程 + 可取消。

> 与原始设想的一个关键差异：3–8 秒粒度下，10 分钟音频就有 100–200 句，300–500px 的播放条上相邻刻度只有 1–3px。**靠“精准点中刻度”不可行**，所以交互必须是“点位置 → 找最近句”，而不是“点小圆点”。这和学习软件「点句」的做法一致。

### 1.2 默认交互（可配置）

| 操作 | 行为 |
| --- | --- |
| 悬停轨道 | 高亮**离鼠标最近**的句子区间；显示「第 N 句 · 00:12.4 – 00:18.0 (5.6s)」+ 文本（有字幕时） |
| 单击轨道（点句模式，默认开） | 预览离单击位置最近的一句：跳到句首播放，到句尾自动暂停，播放头停在句尾 |
| Shift + 单击 | 跳到最近句首并继续正常播放（像视频章节跳转） |
| Alt + 单击 / 按住拖动 | 普通 seek：按住拖动连续定位，松手提交；点句模式下 Alt 单击用于精确跳转 |
| 单击页码刻度（整本模式） | 跳到该页起点，同步 PDF 页码；不自动预览句子 |
| 双击轨道 | 从最近句首开始预览（与单击等价，保留旧习惯） |
| `,` / `.` 或 `[` / `]` | 上一句 / 下一句（预览） |
| `Esc` | 仅当播放条获得焦点或正在预览时取消预览；不抢占已有的 Esc（翻译条/全屏）关闭逻辑 |
| `⋯` 菜单（不设全局快捷键） | 点句模式开 / 关；关闭后恢复「点刻度预览、点空白 seek」。**不能用 `P` 切换**：`PdfReaderForm` 的 `P` 已被「钢笔标注」占用（`PdfReaderForm.cs:3567`），新增单字母键必须先查键盘表 |

文本显示规则：有字幕/跟读文本 → 显示原文（最多 2 行）；无字幕 → 只显示「第 N 句 · 起止时间 · 时长」，不显示空文本占位。

### 1.3 成功标准（验收口径）

1. 冷启动分析 10 分钟 MP3 在普通机器上 ≤ 5s，且 UI 不卡顿；命中缓存 ≤ 300ms 出刻度。
2. 点句起播位置与目标句 `StartMs` 误差 ≤ 30ms；句尾自动暂停不越过下一句 100ms 以上。
3. 3–8 秒粒度下，**任意位置单击都能预览到正确的一句**（点句模式），不要求点中 1–3px 的刻度。
4. 原有拖拽 seek、A-B 复读、0.5–2.0 倍速、翻页切歌、字幕显示全部不回归。
5. 刻度绘制在 100% / 125% / 150% DPI 下不糊、不越界；深色/浅色主题都成立。
6. 断句缓存跨重启有效；源音频变更后自动失效并重分析；手动修正不因清缓存丢失。
7. 整本模式页码层只画**有可信来源**的页码；无来源的页不画、不伪造，页码层覆盖率在菜单/图例中可见（如「12/300 页已校准」）。

---

## 2. 现状盘点

### 2.1 已有可复用资产

| 能力 | 现有实现 | 复用方式 |
| --- | --- | --- |
| 流式能量 VAD | `SilenceVadAnalyzer`（Core，逐帧 RMS，两档阈值 + 迟滞 + 留白） | 直接复用；新增参数实例做「粗/细」两遍 |
| MP3 解码 + 混单声道 | `VadSegmentService.AnalyzeAsync`（NAudio `AudioFileReader`，边读边喂，整轨不进内存） | 抽成通用 `AudioAnalysisService`，一次解码喂两个分析器 |
| 播放 / 定位 / 倍速 / AB 循环 | `IMediaPlayer` + `NAudioMediaPlayer` | 扩展「播到指定位置暂停」 |
| 进度条刷新（10Hz 限频） | `PdfAudioPlayerBar.OnPositionChanged` / `UpdateProgress` | 保留节流，改喂新控件 |
| 自绘控件范式 | `PdfToolbarSlider`（双缓冲、圆角轨道、DPI 边距、键盘） | 作为新控件 `AudioTimelineBar` 的模板 |
| 分句结果持久化范式 | `ShadowingSegmentStore`（用户 + PDF 哈希目录、进程内锁、指纹、原子写） | 复制目录/锁/指纹/原子写做法 |
| 字幕文本 | `SubtitleParser`（`.srt`/`.lrc`）+ `AudioTrack.SubtitlePath` | 作为句子文本来源与「字幕优先断句」来源 |
| 功能隐藏开关 | `PdfHiddenFeatures`（隐藏入口、保留实现） | 新增 `EnableSentenceTimeline`，便于灰度 |
| 已有手动/自动切片 | `ShadowingSegmentStore` 的 `p000.segments.json`（含 `StartMs/EndMs`、`BoundarySource`、`Text`、`HighlightAnchor`、takes） | **P0 只读复用为句子边界**；写回放 P1（见 §7.3 风险） |
| 整本音频的页范围 | `AudioTrack.MappedPageStart/MappedPageEnd`（整本时为 0..pageCount-1） | 只描述页范围，**没有页→时间**；页码层需新增锚点来源 |
| 音频侧栏 | 实际使用 `PdfAudioPanel`（`PdfAudioSidebarPanel` 未接入窗体） | 句子列表应挂到 `PdfAudioPanel`/抽屉，不挂旧面板 |
| 跟读窗口 | `SentenceShadowingForm` 是独立窗口，不是侧栏子页 | 句子列表需新做或复用其列表样式，不能假设已有子页 |

### 2.2 缺口

1. 没有「音频时间轴」模型/存储：现有 `ShadowingSegment` 绑定页面、跟读 take、切片，语义过重，不能直接当通用刻度；但它可以作为**边界来源**复用。
2. `TrackBar`（`PdfAudioPlayerBar.cs:219`，0–1000）无法画刻度点，也无法做刻度命中测试；必须换自绘控件。
3. `IMediaPlayer` 只有 A-B **循环**，没有「到 B 停一次」，无法直接实现单句预览。
4. 分析没有和曲目生命周期绑定：`VadSegmentService` 只在弹出的审查窗里调用一次，无缓存、无取消、无代际校验。
5. 进度条宽度有限（播放条中栏约占窗口 40%，常见 300–500px）；精听短句 3–8 秒下刻度间距 1–3px，必须用「点句模式」而非精准点刻度。
6. 整本音频没有「页 → 时间」映射：`MappedPageStart/End` 只是页范围；页码刻度必须从跟读切片、手动锚点或字幕对齐中获得，不能按页数均分。
7. 跟读切片与时间轴共用边界时，切片 `takes` 与切片文件绑定在段上；**直接改切片时间会使命中 take 偏移**，需要保护策略（P0 只读、P1 写回前校验）。

---

## 3. 成熟软件参照

| 软件 / 场景 | 打点来源 | 交互 | 可借鉴点 |
| --- | --- | --- | --- |
| YouTube / B 站 视频章节 | 作者标注或自动章节 | 进度条分段刻度、悬停显示章节标题、点击跳转并继续播放 | **刻度 + 悬停卡片 + 点击跳转**是用户最熟悉的范式 |
| Audacity 标签轨 | 手动 / 静音分析插件 | 标签独立轨道、可拖动边界、点击定位、导入导出 | 边界应可编辑；标签与音频同轴 |
| Aegisub / Subtitle Edit | 音频波形 + `silencedetect` | 波形上显示静音区、点波形定位、自动生成字幕行 | 静音检测结果要可视化；可导出 SRT |
| Apple Podcasts / Overcast / Snipd | 章节 + 静音裁剪 | 章节列表 + 进度条刻度，点章节播放 | 静音检测用于「去空白」；章节可命名 |
| 每日英语听力 / Aboboo 等学习软件 | 人工/自动逐句 | 句子列表与音频同步，点句播放、单句循环、跟读 | 学习场景真正需要的是**句子列表 + 单句循环**，刻度只是入口 |
| foobar2000 / VLC（CUE） | CUE 分轨 | seekbar 上显示分轨点，点选切轨 | 分轨点多时按像素合并 |

**取捨结论**：

- 视频章节的「悬停提示 + 点击跳转」借鉴，但学习场景默认改为**单句预览后暂停**，避免一直播下去。
- YouTube 章节之所以能“点小刻度”，是因为章节数很少；精听短句有 100–1200 句，必须借鉴学习软件的**点句模式**（点位置选最近句），刻度只做视觉提示。
- 借鉴 Audacity/Aegisub 的「边界可编辑」，但 P0 只做**最小修正**（拖边界/合并/拆分）；完整编辑器放 P2。
- 借鉴学习软件的「句子列表」，放 P1；进度条太窄时它是逐句浏览的最终解法。
- 整本书用**双层刻度**（页码层 + 句子层），类似视频章节 + 分镜；句子层在整本模式下只渲染当前页附近，避免上千刻度糊成一片。

---

## 4. 术语与交互模型

### 4.1 术语

| 术语 | 定义 |
| --- | --- |
| 句子区间（`AudioSentenceSpan`） | 源音频时间轴上的 `[StartMs, EndMs)`，一句一条 |
| 刻度（marker） | 画在进度条上的句子**起点**；不是静音中点 |
| 时间轴（`AudioSentenceTimeline`） | 一条曲目的全部句子区间 + 分析元信息 |
| 点句模式（point-sentence） | 单击轨道任意位置 → 取最近句子并预览；拖动/Alt 单击才是普通 seek |
| 页码锚点（`AudioPageAnchor`） | 整本音频中「第 N 页从哪个时间开始」；来源必须是切片/手动/字幕，不能均分 |
| 预览（preview） | 从句子起点播放，到终点自动暂停一次 |
| 边界来源 | `manual`（用户修正）> `shadowing`（跟读切片）> `subtitle`（字幕）> `vad`（静音检测） |
| 指纹（fingerprint） | 源文件大小 + 修改时间 + 时长，用于缓存失效（沿用 `ShadowingSegmentStore.BuildFingerprint`） |

> 为什么刻度取「句首」而不是「静音中点」：点句应当**从这句话开始听**。静音中点会让人先听到半段前一句的尾巴；句首刻度与用户的直觉一致，也和 YouTube 章节一致。

### 4.2 预览状态机

```
Idle ──点句/点刻度──▶ Previewing(span)
Previewing ──到达 EndMs──▶ PausedAtEnd（3s 后回 Idle）
Previewing ──用户拖动/Alt seek/切歌/取消──▶ Cancelled(Idle)
```

- `Previewing` 期间：进度条高亮当前句区间、播放按钮仍显示 ⏸、允许随时暂停。
- `PausedAtEnd`：不改变播放位置（停在句尾），只把状态置为暂停；3 秒内再点同一句可重播。
- **单句预览不写入 `LastPositionSeconds`（强制，不是可选）**：预览结束时走 `PdfAudioPlaybackService.Pause()` 或带 `_suppressProgressSave` 的路径，不能走 `PdfAudioTrackManager.Pause()`（后者会立即 `SavePlaybackState()`，把“听一句”写成阅读进度）。
- 预览期间若用户主动按暂停：允许，但同样不写进度；只有用户退出预览后正常暂停才写。

### 4.3 刻度密度策略

设轨道可用像素宽 `W`、句数 `N`、相邻句首像素间距 `gapPx`。精听 3–8 秒下，10 分钟音频约 100–200 句，30 分钟约 300–600 句，1 小时约 450–1200 句。

| 条件 | 画法 | 命中 |
| --- | --- | --- |
| `gapPx >= 6` | 全部刻度 + 圆点 | 最近句（点句模式）；关闭点句模式时按 ±5px 命中刻度 |
| `3 <= gapPx < 6` | 全部刻度，只画 1px 细线，不画圆点 | 同上；点句模式仍是最近句 |
| `gapPx < 3` | 抽稀：按像素桶只保留每桶第一个 + 当前句 + 首末句 | **不依赖命中刻度**；点句模式取最近句 |
| `N > 2000`（整本） | 句子层只渲染当前页时间范围；页码层全量 | 句子层只画当前页窗口，但**命中仍取全局最近句**（点句模式）；页码层 ±6px 可点 |
| 关闭点句模式 | 按上面的绘制规则；空白处单击 seek | 只有 ±5px 命中刻度才预览，否则 seek |

- 抽稀只影响**绘制**，不影响“最近句”计算（按 `StartMs` 有序二分/线性扫描）。
- 页码刻度是**独立上层**，命中优先于句子刻度；页码层不参与抽稀（整本书通常几十到几百页，可全画）。
- 点句模式下，悬停任意位置都显示最近句的高亮与 tooltip，用户不需要“瞄准”。

> 3–8 秒 + 300px 轨道是最坏组合：`gapPx ≈ 1.5–3px`。所以**点句模式是 P0 必做项**，不是可选优化；否则“点击预览”在真实宽度下不可用。

---

## 5. 总体架构

```
┌──────────────────────── Core（纯逻辑，可单测） ────────────────────────┐
│ Models/Audio/AudioSentenceTimeline.cs   实体 + JSON Schema               │
│ Services/Audio/SentenceBoundaryBuilder.cs  区间合并/拆分/切短（纯函数） │
│ Services/Audio/AudioTimelineStore.cs    缓存读写 + 指纹校验 + 进程锁     │
│ Services/Audio/AudioBoundaryOverlayStore.cs  手动修正（用户数据）        │
│ Services/Audio/AudioPageAnchorBuilder.cs  页→时间锚点（切片/字幕/手动）  │
│ Services/Audio/SentenceBoundaryResolver.cs  来源优先级与混合填充         │
│ Services/Audio/PlaybackRangeState.cs    「播到 B 停一次」的纯判定        │
│ Abstractions/Audio/IAudioSegmentAnalyzer.cs  分析器可插拔接口（P1）      │
└──────────────────────────────────────────────────────────────────────────┘
                ▲                                   ▲
                │                                   │
┌───────────────┴──────────── WinApp（编排 + UI） ───┴─────────────────────┐
│ Services/Audio/SentenceTimelineService.cs  缓存→分析→边界合成→发布       │
│ Services/Audio/AudioDecodeAnalysisService.cs  解码一次，喂粗/细两分析器  │
│ Managers/Pdf/PdfAudioTrackManager.cs   曲目生命周期、取消、事件          │
│ Common/UI/AudioTimelineBar.cs          双层刻度 + 点句命中 + 悬停         │
│ Forms/Pdf/PdfAudioPlayerBar.cs         装配、tooltip、菜单、点句开关      │
│ Forms/Pdf/AudioSentenceListPanel.cs    P1 句子列表                       │
│ Platform/Media/NAudioMediaPlayer.cs     StopAt 实现                      │
└──────────────────────────────────────────────────────────────────────────┘
```

### 5.1 分层原则

- Core 只放「不依赖 UI / NAudio」的实体、后处理、存储、纯状态机；`SentenceBoundaryBuilder`、`SentenceBoundaryResolver`、`AudioPageAnchorBuilder` 必须可脱离音频单测。
- 解码与 VAD 仍留在 WinApp（依赖 NAudio），但通过 `IAudioSegmentAnalyzer` 抽象，后续可换 Silero/WebRTC；**接口必须是流式 `Push/Complete`，不能收整轨 `ReadOnlySpan<float>`**，否则与“不把整轨 PCM 读进内存”冲突。
- `PdfAudioTrackManager` 仍是播放状态的唯一真源；UI 不自行推断当前曲目/位置。
- 时间轴、跟读切片、手动修正分三层存储：**切片是既有用户数据，P0 只读；手动修正是新用户数据；分析缓存可重建**。
- 所有跨线程事件（分析完成、缓存命中）必须回到 UI 线程再触发 UI 更新；`PdfAudioTrackManager` 用 `SynchronizationContext`/`BeginInvoke` 保证。

---

## 6. 数据模型与持久化

### 6.1 实体（新建 `LearningAssistant.Core/Models/Audio/AudioSentenceTimeline.cs`）

```csharp
public sealed class AudioSentenceTimeline
{
    public int SchemaVersion { get; set; } = 1;
    public string TrackKey { get; set; } = "";        // 相对路径优先，外部目录用绝对路径
    public string Fingerprint { get; set; } = "";      // 大小|mtime|时长
    public int DurationMs { get; set; }
    public AudioTimelineAnalysis Analysis { get; set; } = new();
    public List<AudioSentenceSpan> Sentences { get; set; } = new();
}

public sealed class AudioTimelineAnalysis
{
    public string Engine { get; set; } = "energy-vad"; // energy-vad / silero / subtitle / shadowing / manual
    public string EngineVersion { get; set; } = "1";
    public DateTime AnalyzedAt { get; set; } = DateTime.Now;
    public AudioTimelineOptions Options { get; set; } = new();
    public double Confidence { get; set; } = 1.0;      // 结果自评，低分提示重新断句
    // 各来源贡献：shadowing 123 / vad 45 / subtitle 0，便于解释“为什么这句在这里”
    public Dictionary<string, int> SourceCounts { get; set; } = new();
}

public sealed class AudioTimelineOptions
{
    public int SentenceGapMs { get; set; } = 350;       // 句间停顿阈值（粗遍唯一阈值）
    public int FineGapMs { get; set; } = 150;           // 长句内部切分（细遍）
    public int MinSpeechMs { get; set; } = 150;         // 保留短应答；对应 SilenceVadOptions.MinSegmentMilliseconds
    public int MinSentenceMs { get; set; } = 700;       // 过短并入相邻句；不再丢弃
    public int MaxSentenceMs { get; set; } = 8000;      // 精听短句上限，超长强制切
    public int PaddingMs { get; set; } = 100;           // 句首尾留白
    public double NoiseMultiplier { get; set; } = 2.5;
    public double MinAbsoluteRms { get; set; } = 0.0005;
}

public sealed class AudioSentenceSpan
{
    public int Index { get; set; }                 // 1-based，显示用
    public int StartMs { get; set; }
    public int EndMs { get; set; }
    public int? PageIndex { get; set; }            // 来自跟读切片/单页音频时，便于页码层与页面同步
    public string? BoundarySource { get; set; }    // manual / shadowing / subtitle / vad
    public double Confidence { get; set; } = 1.0;
    public string? SegmentId { get; set; }         // 跟读切片的 Segment.Id；P0 只读关联
    public string? HighlightId { get; set; }

    // 运行期从字幕/切片叠加，缓存不写；避免字幕更新后读到旧文本
    [JsonIgnore] public string? Text { get; set; }
    [JsonIgnore] public string? TextSource { get; set; }
}

// 页码层：只存“有来源”的锚点；runtime 计算，不写缓存
public sealed class AudioPageAnchor
{
    public int PageIndex { get; set; }
    public int StartMs { get; set; }
    public string Source { get; set; } = "shadowing"; // shadowing / manual / subtitle
    public double Confidence { get; set; } = 1.0;     // 插值/推测必须 < 0.5，UI 不画或不强调
}
```

### 6.2 存储位置

**三层分离**：

| 数据 | 位置 | 是否可重建 | 说明 |
| --- | --- | --- | --- |
| 自动断句缓存 | `AppPaths.CurrentUserCacheDir/audio_timelines/{pdfHash}/{trackKeyHash}.timeline.json` | 是 | 清缓存只丢自动结果 |
| 手动修正 | `AppPaths.CurrentUserDir/audio_boundaries/{pdfHash}/{trackKeyHash}.manual.json` | 否 | 用户数据；拖边界/合并/拆分/页码锚点 |
| 跟读切片 | 既有 `Shadowings/{user}/{pdfHash}/p{page}.segments.json` | 否 | **P0 只读**；P1 才允许“应用边界到切片” |

```csharp
// 手动覆盖层：按稳定句 ID（或 PageIndex + 起始时间）定位
public sealed class AudioBoundaryOverlay
{
    public int SchemaVersion { get; set; } = 1;
    public string TrackKey { get; set; } = "";
    public string Fingerprint { get; set; } = "";
    public List<AudioSentenceEdit> Edits { get; set; } = new();
    public List<AudioPageAnchor> PageAnchors { get; set; } = new();
}

public sealed class AudioSentenceEdit
{
    public string Id { get; set; } = "";        // 稳定 ID，优先用 SegmentId / anchorKey
    public int StartMs { get; set; }
    public int EndMs { get; set; }
    public bool IsDeleted { get; set; }
    public string Kind { get; set; } = "adjust"; // adjust / merge / split / pageAnchor
}
```

- 读取时校验 `SchemaVersion`、`Fingerprint`、`Options` 签名；任一不一致 → 自动缓存视为未命中并后台重分析，**但手动覆盖层保留**（若指纹变化，提示“源音频已变化，手动边界可能偏移”，仍允许用户选择复用或清空）。
- JSON 采用 `.part` + 原子改名；进程内按 `user + pdf + trackKey` 加锁串行写。
- `Text`/`TextSource` 标记 `[JsonIgnore]`，运行期从 `SubtitleParser` 或跟读切片叠加；新增字幕不需要重跑分析。
- 分析失败写一个 `status=failed` 的短 TTL 负缓存，避免每次播放都重试；网络/格式问题提示一次即可。
- `trackKeyHash` 用稳定曲目键（相对路径）的 SHA-256 前 12 位；同一条音频被多本书引用时会重复分析，P1 可改内容指纹级缓存（非 P0 必需）。

### 6.3 缓存容量

精听 3–8 秒下：

| 音频长度 | 句数（约 4s/句） | JSON 大小 |
| --- | --- | --- |
| 10 分钟 | 150 | ~12 KB |
| 1 小时 | 900 | ~75 KB |
| 10 小时 | 9000 | ~750 KB |

可忽略。P1 增加「按最后访问时间清理 30 天未用」即可；手动覆盖层不清理。

---

## 7. 断句算法设计

### 7.1 输入 / 输出契约

```csharp
// 纯函数，便于单测
IReadOnlyList<AudioSentenceSpan> SentenceBoundaryBuilder.Build(
    IReadOnlyList<VadSegment> coarse,   // MinSilence = SentenceGapMs，Padding = 0
    IReadOnlyList<VadSegment> fine,     // MinSilence = FineGapMs，Padding = 0
    int durationMs,
    AudioTimelineOptions options);
```

- 两个分析器共用同一次解码流（解码一次，`Push` 两次），避免读两遍 MP3。
- 分析器 `PaddingMilliseconds = 0`，留白统一由后处理加，避免重叠后无法区分「真间隔」与「padding 造成的假重叠」。
- **两个分析器都要把 `MinSegmentMilliseconds` 降到 `MinSpeechMs = 150`**；否则 `SilenceVadAnalyzer` 默认会先丢掉 250ms 以下的短应答，后处理再“不丢弃”也救不回来。
- 后处理只接受**粗遍边界**作为候选句界；细遍只用于切超长段，不直接生成句子，避免 150ms 换气把一句切碎。

### 7.2 后处理步骤（顺序固定）

1. **规范化**：裁剪到 `[0, durationMs]`，丢弃 `End <= Start`；按 `Start` 排序；合并完全重叠段。
2. **粗遍定句**：粗遍输出直接作为句子候选（`SentenceGapMs` 是唯一句间阈值）；**不再有独立的 `MergeGapMs`**，避免“阈值 A 检测、阈值 B 合并”导致 B 永远不生效。
3. **超长切分**：粗段时长 `> MaxSentenceMs` 时，用落在该段内部的细遍间隔反复切分：
   - 优先选**最长**的内部间隔，而不是最靠近中点（中点可能切在词中）；
   - 要求切分后两侧都 `>= MinSentenceMs`；
   - 没有合法细边界时，在 `MaxSentenceMs` 处硬切，`Confidence` 降 0.1，并标记 `BoundarySource = "vad-hard"`。
   - 精听默认 `MaxSentenceMs = 8000`；3–8 秒目标下，超过 8 秒的连续朗读必须切。
4. **短句归并**：`Duration < MinSentenceMs` 的段并入「间隔更小」的相邻句（首段并入后一句、末段并入前一句），**不直接丢弃**；若前后间隔都很大且时长 `>= MinSpeechMs`，也允许保留为独立短句（例如 "Yes."），避免误并到下一句。
5. **加留白**：每段前后各加 `PaddingMs`，裁剪到 `[0,durationMs]`。
6. **消重叠**：相邻段若重叠，按间隔中点回退，保证 `prev.End <= next.Start`；回退后若某段 < 300ms，则与前段合并。
7. **兜底**：结果为空，或整轨只有 0–1 句时，返回 `[0, durationMs]` 单句并标记 `Confidence < 0.5`（UI 不画刻度，但可「点任意位置」）。
8. **自评**：统计句长分布；精听目标 3–8 秒，若 `> 40%` 句子 < 1s、或 `> 30%` 句子 > 8s、或中位数 > 10s，`Confidence` 降级，UI 提示「断句效果可能不佳，试试灵敏度 / 手动修正」。
9. **标记来源**：`SentenceBoundaryBuilder` 只产出 VAD 段并写 `BoundarySource = "vad"` / `vad-hard`；切片、字幕、手动来源由 `SentenceBoundaryResolver` 在合并阶段写入对应值，并保留 `Confidence`。

### 7.3 边界来源优先级（与跟读切片共用）

| 优先级 | 来源 | 说明 |
| --- | --- | --- |
| 1 | 手动修正覆盖层 | 用户在进度条上拖过/合并/拆分的边界，永远最高；存用户目录 |
| 2 | 跟读切片 `p{page}.segments.json` | 同一 `Source.TrackPath` + `Fingerprint` 匹配、且覆盖当前页有效范围时采用；`SegmentId`/`HighlightAnchor` 保留 |
| 3 | 字幕文件（`.srt`/`.lrc`） | 作者已按句切好，通常比 VAD 准；同时天然带文本；仅在切片缺失的区间使用 |
| 4 | 能量 VAD | 兜底；生成缺失区间的句子 |

**混合填充规则（关键）**：不要“有切片就整轨只用切片、没有就整轨重跑”。按时间轴合并：

```
切片段（可信） ──┐
字幕段（可信） ──┼─▶ 按时间排序 → 覆盖区间
VAD 段（兜底） ──┘    缺失区间用 VAD 补 → 重叠处按优先级取高 → 统一 padding/消重叠
```

- 这样 P0 既能复用已对齐页，又不会因为“只对齐了半页”导致整轨刻度消失。
- 切片覆盖率低于阈值（例如有效段覆盖 < 30% 且中间有大空洞）时，只采用其有效区间，空洞用 VAD 补，不整页丢弃。
- `Source.TrackPath` 或 `Fingerprint` 不匹配 → 不使用该页切片，退回 VAD，并提示“跟读切片与当前音频不匹配”。
- **P0 只读**：不改 `segments.json`。**P1 写回**：新增“应用断句到跟读切片”，保留 `Segment.Id` 与 takes；边界移动超过 300ms 时提示“已有录音可能偏移”，允许取消；切片由后续流程重新生成，不删除 takes。完整双向编辑放 P2。

> 风险：`ShadowingSegment` 上挂有 `Takes`/`SlicePath`，直接改时间会使命中 take 与音频错位。因此“共用边界”在 P0 只能保证**读取一致**，写回必须有人工确认和失效提示，不能静默双向同步。

### 7.4 灵敏度预设（默认改为精听）

| 预设 | SentenceGap | FineGap | MinSentence | MaxSentence | 适用 |
| --- | --- | --- | --- | --- | --- |
| 精听短句（默认） | 350ms | 150ms | 700ms | 8000ms | 逐句精听、3–8 秒 |
| 标准 | 500ms | 180ms | 800ms | 10000ms | 普通课文 |
| 宽松 | 700ms | 250ms | 1000ms | 15000ms | 有背景音乐、长句 |

- `MaxSentenceMs` 只影响超过上限的段；自然停顿优先。
- 参数存在档案（`PdfAudioProfile`）与时间轴 JSON 中；切换预设触发重分析，**手动覆盖层保留**，由用户选择“重新断句并清空手动修正 / 仅重分析未修改段”。

### 7.5 质量升级路线

| 阶段 | 引擎 | 理由 |
| --- | --- | --- |
| P0 | 现有 `SilenceVadAnalyzer` | 零新依赖，已验证；清晰人声效果足够 |
| P1 | 可插拔 `IAudioSegmentAnalyzer`（流式 `Push/Complete`）+ WebRTC VAD | 抗噪更好，包体小 |
| P2 | Silero VAD（ONNX，16k 单声道） | 对音乐/噪声/低音量明显更稳；需打包 ~2MB 模型 |
| P2 | Whisper / 强制对齐 | 同时得到文本与精确句界，但成本高，按需下载 |

不建议引入 ffmpeg：当前仓库没有 ffmpeg 依赖，仅为一个静音检测引入外部二进制不划算；NAudio 已能解码。

---

## 8. 播放与单句预览

### 8.1 播放器能力扩展

`IMediaPlayer` 增加一个**一次性的停止点**（只有 `NAudioMediaPlayer` 一个实现，改动面小）：

```csharp
/// <summary>播放到该源时间后自动暂停并清除；null 表示不限制。用于单句预览。</summary>
TimeSpan? StopAt { get; set; }

/// <summary>到达 StopAt 并已暂停后触发（UI 线程）。</summary>
event EventHandler? PlaybackRangeCompleted;
```

- 在 `NAudioMediaPlayer.OnTick`（当前 100ms）里，先判 `StopAt` 再触发 `PositionChanged`；命中时先**取出并清空** `StopAt` → `Pause()` → `Seek(StopAt)` 对齐 → 触发 `PlaybackRangeCompleted`。
- **清理规则必须写清（否则预览直接坏掉）**：
  - `Load` / `Stop` / `CancelPreview` 清空 `StopAt`；
  - **`Seek` 不清空 `StopAt`**（预览流程是“先设 StopAt，再 Seek 到句首”，Seek 清空会把停止点弄丢）；
  - `Pause` 保留 `StopAt`，允许用户暂停后继续预览到句尾。
- 把判定抽成 Core 纯类 `PlaybackRangeState`（类似 `AbLoopState`），便于单测：
  ```csharp
  public sealed class PlaybackRangeState
  {
      public TimeSpan? StopAt { get; set; }
      public bool ShouldStop(TimeSpan position, out TimeSpan snapTo);
  }
  ```
- 不修改全局计时器间隔（100ms 对所有播放器行为生效，改成 50ms 影响面大）；接受 ≤100ms 过冲，到点 `Pause` 后 `Seek(EndMs)` 对齐播放头。句尾硬停如出现爆音，再考虑 20–30ms 淡出（可选）。
- `PlaybackRangeCompleted` 必须由 WinForms 定时器线程（UI 线程）触发；`StopAt` 的读写只在 UI 线程/播放器内部进行，避免跨线程竞态。

### 8.2 管理层 API（`PdfAudioTrackManager`）

```csharp
public AudioSentenceTimeline? CurrentTimeline { get; }
public event EventHandler<AudioSentenceTimeline?>? TimelineChanged;

/// <summary>加载/分析当前曲目的时间轴；命中缓存则立即发布；同一曲目并发调用复用同一 Task。</summary>
public Task EnsureTimelineAsync(bool forceReanalyze = false);

/// <summary>点句：取离 position 最近的句子并预览；continueAfter=true 时播到句尾继续。</summary>
public bool PreviewNearest(TimeSpan position, bool continueAfter = false);
public bool PreviewSentence(AudioSentenceSpan span, bool continueAfter = false);
public void CancelSentencePreview();
public AudioSentenceSpan? CurrentSentence { get; }  // 由 Position 计算，供 UI 高亮
public void SetTimelineOptions(AudioTimelineOptions options); // 触发重分析，保留手动覆盖层
```

- `EnsureTimelineAsync` 在 `LoadCore` 成功后触发（或用户开启刻度时触发）；用 `_loadGeneration` 类似机制丢弃过期结果；用 `_timelineTask` 去重，避免翻页时并发分析同一曲目。
- 切曲、切文档、关闭时 `Cancel()` CTS 并清空 `CurrentTimeline`，防止旧刻度画到新曲目上。
- 时间轴分析使用 `Task.Run`，不阻塞播放；不要给线程池线程设 `BelowNormal`（会影响全局池），需要降优先级时用专用线程。
- 分析完成/缓存命中事件必须 marshal 回 UI 线程再发 `TimelineChanged`；否则 `PdfAudioPlayerBar` 会在后台线程刷新控件。
- 预览通过 `PdfAudioPlaybackService` 转发；A-B 循环与 `LoopMode` 在预览期间**暂存并恢复**（见 §8.4）。

### 8.3 预览流程

```
点句/点刻度
 → manager.CancelSentencePreview()（若有）
 → 记住当前 A-B 设置、LoopMode、播放状态
 → player.AbLoop = null（暂停 AB）
 → player.LoopMode = None（防止最后一句触发循环）
 → player.StopAt = span.EndMs
 → player.Seek(span.StartMs)
 → player.Resume()（无加载时先 Load）
 → 到达 StopAt → Pause（内部路径，不保存进度） + Seek(EndMs) + 事件
 → 恢复 A-B、LoopMode（不恢复播放位置）
```

- 若 `span.EndMs >= player.Duration`，把 `StopAt` 设为 `Duration - 50ms`，并依赖 `PlaybackFinished` 兜底取消预览，避免“最后一句”被循环逻辑接管。
- 预览结束调用 `PdfAudioPlaybackService.Pause()`（不经过 `PdfAudioTrackManager.Pause()`），或给 manager 加 `_suppressProgressSave` 标志；否则会把句尾位置写成 `LastPositionSeconds`。

### 8.4 与现有功能冲突

| 冲突 | 处理 |
| --- | --- |
| A-B 复读已设置 | 预览期间清空 `AbLoop`，预览结束/取消后恢复原 A/B |
| 单曲/列表循环 | 预览期间 `LoopMode = None`，结束/取消后恢复原值；最后一句走 `PlaybackFinished` 兜底 |
| 倍速 0.5–2.0 | 时间戳始终是**源时间**，`VarispeedSampleProvider` 不改变 `_reader.CurrentTime`，无需换算 |
| 打点/跟读会话（若恢复） | 会话期间禁用刻度点击与预览，避免抢占播放 |
| 用户拖动 seek / Alt 单击 | 立即取消预览并清空 `StopAt` |
| 用户按空格暂停 | 保持 `StopAt`；再按继续预览；退出预览（Esc/切曲）才清空 |
| 切曲 / 翻页切曲 | 取消预览 + 取消分析 + 清空刻度 |
| 系统输出设备变化 | `OnOutputDeviceChanged` 重载会 `Load` 并清空 A/B；若正在预览，直接取消预览并提示，不尝试跨重载恢复（实现更稳） |
| 关闭窗口 | `Dispose` 中取消 CTS、清除事件、清空 `StopAt` |
| 进度保存 | 预览期间 `SavePlaybackState` 必须被抑制；只有正常播放/用户退出预览后的暂停才保存 |

---

## 9. UI 设计

### 9.1 控件替换

`PdfAudioPlayerBar.BuildTransport()`（`PdfAudioPlayerBar.cs:203`）中把 `TrackBar _track` 替换为 `AudioTimelineBar _timeline`：

- 新控件继承 `Control`，开启 `UserPaint | AllPaintingInWmPaint | OptimizedDoubleBuffer | ResizeRedraw`（与 `PdfToolbarSlider` 一致）。
- **尺寸约束**：播放条总高 64px，进度行内部可用高度约 26px；新控件按 **24px** 设计，不要写 28px（会被 TableLayoutPanel 裁切）。双层刻度在 24px 内做成上下两个 8–9px 的子轨道，不额外增高；如果产品要求更大的点击热区，再把播放条高度从 64 调到 80。
- 属性/事件（不再沿用 `Minimum/Maximum/Value` 三件套，避免把 0–1000 的旧契约带进新控件）：
  ```csharp
  public IReadOnlyList<AudioSentenceSpan> Sentences { get; set; }
  public IReadOnlyList<AudioPageAnchor> PageAnchors { get; set; }
  public TimeSpan Duration { get; set; }
  public TimeSpan Position { get; set; }
  public double AnalysisProgress { get; set; } // 0..1，<0 表示未分析
  public bool PointSentenceMode { get; set; } = true;
  public bool ShowPageLayer { get; set; }

  public event EventHandler<TimeSpan>? SeekRequested;          // 拖动/Alt 单击松手
  public event EventHandler<TimeSpan>? DragPositionChanged;    // 拖动中实时更新时间标签
  public event EventHandler<AudioSentenceSpan>? PreviewRequested; // 点句/点刻度
  public event EventHandler<AudioPageAnchor>? PageClicked;
  public event EventHandler<AudioSentenceSpan?>? MarkerHovered;
  ```
- `PdfAudioPlayerBar` 现有 `_track` 用法需要逐项迁移：`_track.Value` → `_timeline.Position`；`_track.Maximum` → `Duration`；`MouseDown/MouseUp/Scroll` → `SeekRequested/DragPositionChanged`；`_track.Enabled` → `_timeline.Enabled`；`RefreshTrack` 的空态 → `Sentences=[]; PageAnchors=[]; Position=0`。
- 开关/空态：`ShowMarkers=false` 时不画刻度，退化为普通自绘滑杆（替换不应改变现有观感）。

### 9.2 绘制规范

以 100% DPI、控件高 24px 为例（数值按 `DeviceDpi/96` 缩放）：

| 图层 | 规格 |
| --- | --- |
| 轨道底 | 圆角矩形，高 4px，`palette.SurfaceAlt`，左右各留 9px 防滑块裁切 |
| 已播放 | 同轨道，`Brand`，圆角 |
| 当前句区间 | 轨道下方 2px 细带，`BrandSoft` 60% 透明；悬停句加深 |
| 句子刻度（下层） | 1px 宽、6px 高竖线，`palette.Muted` 60%；当前句/悬停句为 `Brand` 且高 8px |
| 页码刻度（上层） | 1.5px 宽、8px 高竖线 + 2px 顶部圆点，`palette.Accent`/`Brand` 70%；当前页加粗 |
| 悬停圆点 | 3px 圆点（仅 `gapPx >= 6` 时画），`Brand` |
| 播放头 | 12px 圆，`Brand` 填充 + 2px `palette.Surface` 描边；拖动时 14px；悬停 14px |
| 分析中 | 轨道上方 1px 进度线，`Brand` 半透明；**不要占用 `_labelDuration`**，用轨道内右侧 40px 的迷你文字「43%」或状态栏 |
| 低置信 | 刻度用 `palette.Warn`（若调色板没有，用 60% `Muted` + 悬停提示文案） |

- 双层布局：控件上半区（`y < Height/2`）画页码层，下半区画句子层；播放头贯穿两层。没有页码层时，句子层占满高度，避免“轨道偏上”。
- 全部用 `Graphics.SmoothingMode.AntiAlias`；整数像素对齐避免竖线糊成 2px。
- 主题切换：`PdfSidebarTheme.ThemeChanged` 时刷新 `PaletteFor(this)` 并 `Invalidate()`。
- 无障碍：`AccessibleName = "音频进度与句子刻度"`；`TabStop=true`，支持 `←/→` 5 秒、`Shift+←/→` 1 秒、`,`/`.` 上下句；P1 再实现 `AccessibleRole.Slider` 的 RangeValue 模式。不要把“只有 AccessibleName”当成完成无障碍。

### 9.3 命中测试

```csharp
int TrackX(int ms) => EdgePadding + (int)Math.Round(ms / (double)PlayerDuration * TrackWidth);

// 1) 先判 Y 是否在页码层（上半区），且 ShowPageLayer；是 → 最近页码锚点（±6px）
// 2) 否则按 X 取最近句子（有序列表二分/线性扫描），不设“必须点中刻度”的阈值
// 3) PointSentenceMode=true：MouseUp 且未拖动 → PreviewRequested(nearest)
//    PointSentenceMode=false：命中刻度 ±5px → PreviewRequested；否则 SeekRequested
// 4) Alt+单击 → 始终 SeekRequested；拖动超过 3px → 进入拖动态，松手 SeekRequested
```

- 拖拽状态下不显示刻度悬停，避免与 seek 抢手势；拖动态实时发 `DragPositionChanged` 更新时间标签（对应现有 `_track.Scroll` 行为）。
- `Shift`/`Alt` 状态在 `MouseDown` 时记录（`ModifierKeys`），避免松手时按键已改变。
- 时间映射用**播放器实际 `Duration`**，不要用缓存里的 `Timeline.DurationMs`；两者不一致超过 1% 时以播放器为准并触发一次轻量重分析/边界缩放。

### 9.4 悬停提示

P0 用现有 `ToolTip`（`PdfAudioPlayerBar` 已有 `_tooltip`）显示多行文本：

有文本（字幕/切片）：
```
第 12 句 · 00:12.4 – 00:18.0 (5.6s)
What are you going to do this weekend?
单击预览 · Shift 单击从此处继续播放
```

无文本（按决策只显示序号和时间）：
```
第 12 句 · 00:12.4 – 00:18.0 (5.6s)
单击预览 · Shift 单击从此处继续播放
```

页码层：
```
第 8 页 · 01:12:30 起
单击跳到本页开头
```

- 文本来自字幕/切片时最多显示 2 行、每行 40 字，超出省略；无文本时不显示空行。
- 悬停变化用 150ms 去抖，避免快速划过时闪烁。
- P1 改成自绘浮动卡片（更可控，且能显示「上一句/下一句」按钮），但不必在 P0 做。

### 9.5 句子列表（P1）

- 位置：实际侧栏 `PdfAudioPanel` 扩展一个「句子」抽屉/子页，或播放条 `⋯` 菜单打开的右侧抽屉；**不要写 `PdfAudioSidebarPanel`，该面板未接入窗体**。
- 内容：序号 / 起止时间 / 文本（可空）/ 状态（当前、已播、低置信）/ 来源（切片、VAD、字幕）。
- 行为：单击行 → 预览；双击 → 从该句继续播放；当前句自动滚动；支持「单句循环」开关（复用 A-B 或循环预览）。
- 与 `SentenceShadowingForm` 的 `_listSentences` 视觉保持一致，避免再造一套列表样式；但它是独立窗口，不是可复用的侧栏页。

### 9.6 菜单入口

在 `PdfAudioPlayerBar` 的 `_btnMore` 菜单新增：

```
断句刻度 [✓]
 ├─ 点句模式 [✓]          // 关闭后：点刻度预览、点空白 seek
 ├─ 页码刻度 [✓]          // 仅整本单音频时可用
 ├─ 重新断句
 ├─ 灵敏度：精听短句（默认） / 标准 / 宽松
 ├─ 修正断句（P0：撤销 / 合并下一句 / 拆分本句）
 ├─ 应用边界到跟读切片（P1）
 ├─ 打开句子列表（P1）
 └─ 清除本曲目断句缓存（不动手动修正）
```

- 现有 `_menuAutoSegment`（打开 `VadSegmentReviewForm` 生成跟读切片）保持原样、保持隐藏；两者语义不同：**刻度只读展示**，该入口会写 `segments.json`。
- 新增隐藏开关 `PdfHiddenFeatures.EnableSentenceTimeline`，默认 true，便于出问题时一键下线入口。
- 页码层在 `MatchMode=PerPage` 时置灰（每页一个音频，页码层没有意义）；只在整本单音频模式显示。

### 9.7 最小手动修正（P0，建议范围）

点句模式解决“选哪句”，手动修正解决“边界切错”。P0 不引入完整编辑器，只提供：

| 操作 | 交互 | 存储 |
| --- | --- | --- |
| 进入修正模式 | `⋯` 菜单「修正断句」；开启后轨道显示边界手柄，点句预览暂时禁用 | `PdfAudioProfile` 不持久化（会话态） |
| 拖一个边界 | 悬停到最近边界，水平拖动；吸附到 10ms；松手写覆盖层 | `adjust` |
| 合并下一句 | 选中当前句后 `⋯` → 合并；或 `Shift+单击` 相邻句 | `merge` |
| 拆分本句 | 把播放头停在目标位置，`⋯` → 从此处拆分 | `split` |
| 撤销 | `Ctrl+Z`（会话内栈，最多 20 步） | 重写覆盖层 |
| 设为当前页起点 | 停在目标位置，`⋯` → 设为第 N 页起点 | `PageAnchors` |

- 修正模式必须显式开启，避免普通点句误拖边界。
- 每条覆盖记录带稳定 ID（优先 `SegmentId`，否则 `PageIndex + 起始时间` 的哈希）；重分析后按 ID 重新套用，找不到对应句的覆盖标记为“待确认”，不静默丢弃。
- 覆盖层只影响展示与预览；**P0 不写回 `segments.json`**（见 §7.3 风险）。
- 因为 P0 不写回，播放条修正过的边界与「跟读窗口」里的旧切片会**暂时不一致**：修正模式保存后提示一次「已应用播放条校准；跟读切片仍是旧边界，可在 P1 应用」；P1 的「应用边界到跟读切片」是闭合这一步的正规入口。

---

## 10. 代码改动清单

### 10.1 新增

| 文件 | 职责 |
| --- | --- |
| `Core/Models/Audio/AudioSentenceTimeline.cs` | 实体、选项、SchemaVersion、页码锚点、手动覆盖模型 |
| `Core/Services/Audio/SentenceBoundaryBuilder.cs` | 纯函数：切短（3–8s）/短句归并/补白/消重叠/兜底/自评 |
| `Core/Services/Audio/SentenceBoundaryResolver.cs` | 手动 > 切片 > 字幕 > VAD 的来源合并与空洞填充 |
| `Core/Services/Audio/AudioPageAnchorBuilder.cs` | 页→时间锚点：切片最早句/手动锚点；**不做页数均分** |
| `Core/Services/Audio/AudioTimelineStore.cs` | 自动缓存路径、指纹校验、原子写、进程锁 |
| `Core/Services/Audio/AudioBoundaryOverlayStore.cs` | 手动修正/页码锚点（用户数据，不随缓存清理） |
| `Core/Services/Audio/PlaybackRangeState.cs` | 「播到 B 停一次」纯判定 |
| `Core/Abstractions/Audio/IAudioSegmentAnalyzer.cs` | 流式分析器接口（P1 接 WebRTC/Silero）；P0 可不实现，直接复用 `SilenceVadAnalyzer` |
| `WinApp/Services/Audio/AudioDecodeAnalysisService.cs` | 解码一次、双阈值分析（`SentenceGapMs` / `FineGapMs`）、进度/取消 |
| `WinApp/Services/Audio/SentenceTimelineService.cs` | 缓存命中 → 分析 → 边界解析（切片/字幕/VAD）→ 手动覆盖 → 发布 |
| `WinApp/Common/UI/AudioTimelineBar.cs` | 双层刻度、点句命中、悬停、拖动、键盘 |
| `WinApp/Common/UI/MarkerHitTester.cs` | 命中/最近句纯函数，便于单测 |
| `WinApp/Forms/Pdf/AudioSentenceListPanel.cs` | P1 句子列表 |
| `LearningAssistant.Core.Tests/Services/Audio/SentenceBoundaryBuilderTests.cs` | 后处理单测（按现有测试目录约定） |
| `LearningAssistant.Core.Tests/Services/Audio/AudioTimelineStoreTests.cs` | 缓存/指纹/并发单测 |
| `LearningAssistant.Core.Tests/Services/Audio/AudioPageAnchorBuilderTests.cs` | 页码锚点/缺失页不生成/默认关闭时拒绝插值/开启时插值 Confidence < 0.5 |
| `LearningAssistant.Core.Tests/Services/Media/PlaybackRangeStateTests.cs` | 停止点单测 |
| 现有 WinApp 测试工程（若覆盖 UI 逻辑） | `MarkerHitTester` 单测；没有则新建轻量测试工程，不要写“WinApp.Tests（若存在）”这种空话 |

### 10.2 修改

| 文件 | 改动 |
| --- | --- |
| `Core/Abstractions/Media/IMediaPlayer.cs` | 增加 `StopAt`、`PlaybackRangeCompleted` |
| `WinApp/Platform/Media/NAudioMediaPlayer.cs` | `OnTick` 判定 StopAt；**`Load/Stop` 清空，`Seek` 不清空**；事件 |
| `WinApp/Services/Pdf/Audio/PdfAudioPlaybackService.cs` | 透传 `PreviewRange(start,end)` / `CancelRange()`；预览结束暂停不保存进度 |
| `WinApp/Services/Shadowing/VadSegmentService.cs` | 抽出双分析器解码入口；保留原 API 供 `VadSegmentReviewForm` 使用，避免破坏现有审查流程 |
| `WinApp/Managers/Pdf/PdfAudioTrackManager.cs` | 时间轴加载/取消/事件/点句预览；构造函数新增 `SentenceTimelineService` 参数；预览期间抑制 `SavePlaybackState` |
| `WinApp/Forms/Pdf/PdfAudioPlayerBar.cs` | 换控件、事件接线、点句开关、菜单、分析状态、悬停提示、快捷键 |
| `WinApp/Forms/Pdf/PdfAudioPanel.cs` | P1 句子抽屉/入口；若 P0 不做列表则只加菜单入口 |
| `WinApp/Forms/Pdf/PdfReaderForm.cs` | `new PdfAudioTrackManager(...)`（当前约 705 行）处补传 `SentenceTimelineService`；Esc 快捷键增加“预览中优先取消” |
| `WinApp/Common/ServiceCollectionExtensions.cs` | 注册 `SentenceTimelineService`/`AudioTimelineStore`/`AudioBoundaryOverlayStore`（管理器本身仍由窗体手工构造） |
| `WinApp/Forms/Pdf/PdfHiddenFeatures.cs` | 新增 `EnableSentenceTimeline` |
| `Core/Common/AppPaths.cs` | 新增 `AudioTimelinesDir`（`CurrentUserCacheDir/audio_timelines`）与 `AudioBoundariesDir`（`CurrentUserDir/audio_boundaries`）+ 确保目录 |
| `Core/Models/Pdf/PdfAudioProfile.cs` | 必加：按书保存 `SentenceTimelineEnabled`、`PointSentenceMode`、`ShowPageLayer`、`AudioTimelineOptions` 预设 |
| `WinApp/Services/Shadowing/ShadowingSegmentStore.cs` | P0 暴露只读查询「按曲目/页取已对齐边界 + 指纹」；`GetDocumentHash` 目前是 private，需新增公共查询入口或复制哈希逻辑；不改 Schema；P1 再增加写回方法 |

### 10.3 不建议的改动

- 不把**时间轴/页码锚点**塞进 `ShadowingSegmentManifest`：会污染跟读语义，且 `SchemaVersion` 升级影响既有数据；P0 只读取切片边界。
- 不直接用 `TrackBar` + `Tag`/自定义绘制：`TrackBar` 是原生控件，`WM_PAINT` 不可控，深色主题和 DPI 都难对齐。
- 不把整轨 PCM 读进内存做波形：违背现有流式设计，长音频会爆内存。
- **不按页数均分整本音频时长来生成页码锚点**：页与页的朗读时长差异很大，均分会产生系统性错误；没有可信锚点时宁可不画页码层。
- 不在 P0 静默写回 `segments.json`：takes 会错位，必须人工确认（P1）。

---

## 11. 关键流程

### 11.1 打开曲目 → 出刻度

```
LoadCore(track)
  → manager.ResetTimeline()
  → 读取同曲目/同页的跟读切片边界（只读，校验 TrackPath + Fingerprint）
  → 读取手动覆盖层（audio_boundaries/...manual.json）
  → timelineService.TryLoadCache(pdf, track)   // 校验 fingerprint/schema/options
      ├─ 命中 → boundaryResolver 合并切片/字幕/手动/VAD → TimelineChanged
      └─ 未命中 → 后台 AnalyzeAsync(progress, ct)
             ├─ AudioFileReader 解码 + 混单声道（8192 采样块）
             ├─ 同时 Push 到 coarse / fine 两个 SilenceVadAnalyzer
             │    （MinSegmentMilliseconds = MinSpeechMs = 150）
             ├─ Complete() → SentenceBoundaryBuilder.Build（3–8s）
             ├─ store.Save（.part → 原子改名）
             └─ boundaryResolver 合并切片/字幕/手动/VAD → 代际校验 → TimelineChanged
  → pageAnchorBuilder 用切片最早句/手动锚点算页码层；无来源的页不画
```

### 11.2 点句 / 点页码 → 预览

```
AudioTimelineBar.PreviewRequested(nearestSpan) / PageClicked(pageAnchor)
  → PdfAudioPlayerBar: manager.PreviewNearest(position, shift) 或 SeekToPageStart(page)
      → 保存 A/B + LoopMode → 清 AbLoop → LoopMode=None
      → StopAt = span.End → Seek(span.Start) → Resume
      → OnTick 到 End → Pause（不保存进度） + Seek(End) → PlaybackRangeCompleted
      → 恢复 A/B、LoopMode → UI 高亮当前句 3s
```

### 11.3 切曲/翻页

```
TrackChanged(newTrack)
  → CTS.Cancel()（分析中）
  → 清空 CurrentTimeline / PageAnchors / StopAt / 当前句
  → 重新 EnsureTimelineAsync
```

### 11.4 页码层

```
整本单音频 + ShowPageLayer：
  → 后台批量扫描 p{page}.segments.json 的最早 StartMs（校验 TrackPath/Fingerprint）
     · 300 页 = 300 次小文件读取，必须放到后台任务 + 进度，不能在建 UI 时同步做
     · 扫描结果落一份页锚点索引缓存（pdfHash + trackFingerprint + mtime），下次直接命中
  → 有手动页码锚点 → 覆盖切片锚点
  → 无锚点的页 → 默认不生成、不画（推荐，诚实）
  → 可选「推测锚点」层（默认关，显式开关才启用）：
     相邻两页都有锚点时按线性插值补中间页，Confidence < 0.5，画成灰色虚线
  → UI：上半区画页码刻度；点页码 → seek + 同步 PDF 页码
PerPage 模式：
  → 页码层置灰（每页独立曲目，页码层无意义）
```

> 不一致约束：默认行为只有一个——**无锚点不画**；插值是显式开关后的附加层，任何情况下都不按页数均分。

---

## 12. 边界与异常

| 场景 | 处理 |
| --- | --- |
| 每页一个 MP3 / 整本一个 MP3 | 时间轴按**曲目文件**缓存；整本单音频才显示页码层；PerPage 模式页码层置灰 |
| 整本音频数小时 | 后台分析 + 进度 + 可取消；播放不被阻塞；句子层只渲染当前页附近；P1 可做「边分析边发布」 |
| 全静音 / 没有静音 | 全静音 → 单句兜底、不画刻度；连续人声 → 按 `MaxSentenceMs` 硬切并标记 `vad-hard`，低置信 |
| 背景音乐/噪声 | 能量 VAD 会误判；UI 提示低置信；P1 换 WebRTC/Silero |
| 采样率/声道/格式 | 复用 `AudioFileReader`；解码失败 → 时间轴置空 + 提示「该格式暂不支持断句」，播放不受影响 |
| VBR MP3 时长估算偏差 | 边界由解码帧位置得出，不依赖 `TotalTime`；进度映射可能 ±1s；**绘制用播放器实际 `Duration`**，不用缓存值 |
| 源文件被替换/移动 | 指纹或路径变化 → 自动缓存失效并重分析；手动覆盖层保留但提示“可能偏移” |
| 跟读切片与曲目不匹配 | `Source.TrackPath`/`Fingerprint` 不匹配 → 不采用该页切片，VAD 兜底，提示一次 |
| 切片只对齐了半页 | 不整页丢弃；有效区间用切片，空洞用 VAD 填充（§7.3） |
| 切片写入回（P1） | 只改匹配的 `Segment.Id`；保留 takes；边界移动 > 300ms 提示“录音可能偏移”；不静默写 |
| 页码无锚点 | 不生成、不画；可手动“设为当前页起点”；禁止按页数均分；“推测锚点”只在显式开关（默认关）下作为灰色虚线层出现 |
| 页码锚点冲突 | 手动 > 切片；相邻页锚点倒序 → 丢弃后者并提示 |
| 字幕后加/更新 | 不重分析，仅重新叠加文本；`SubtitlePath` 与 mtime 变化时刷新 |
| 缓存损坏/版本升级 | JSON 解析失败 → 删除并重分析；`SchemaVersion` 不符 → 重分析 |
| 分析失败 | 负缓存短 TTL（如 10 分钟）+ 可手动重试；不无限重试 |
| 分析取消 | 丢弃结果，不写盘；下一次重新开始 |
| 预览时用户拖拽/Alt 单击 | 立即取消预览、清 `StopAt`，以用户 seek 为准 |
| 预览时切曲 | 取消并清空，不跨曲目恢复 |
| 预览最后一句 | `EndMs >= Duration` 时 `StopAt = Duration - 50ms`，`PlaybackFinished` 兜底取消预览 |
| 空格键 | 保持现有播放/暂停；若在预览中按空格 → 暂停/继续该句，保留 `StopAt` |
| `Esc` 冲突 | 只有播放条焦点或 `Previewing` 状态才拦截 Esc；否则交给现有翻译条/全屏逻辑 |
| 打点/跟读会话 | 会话期间禁用预览与刻度点击；刻度仍可显示 |
| 点句密度 | 3–8s 下刻度 1–3px：点句模式取最近句，不要求命中刻度；关闭点句才要求 ±5px 命中 |
| 多显示器 DPI | 用 `DeviceDpi/96` 缩放所有像素常量；`ResizeRedraw`；控件高 24px 适配 26px 行高 |
| 深色主题 | 全部颜色走 `PdfSidebarTheme.PaletteFor(this)`；禁止硬编码 |
| 归一化/空值 | `Duration <= 0` 时不画刻度、不响应命中；`Sentences` 为空时退化为普通滑杆 |

---

## 13. 性能评估

| 项目 | 估算 | 措施 |
| --- | --- | --- |
| MP3 解码 + RMS | 10 分钟音频约 1–3s；1 小时约 8–20s（NAudio，普通桌面 CPU；以实测为准） | 后台线程、进度、缓存；不阻塞播放 |
| 内存 | 两个分析器只存帧 RMS：1 小时约 2.9 MB；解码缓冲固定 32KB | 不保留 PCM |
| UI 重绘 | 3–8s 粒度下 1 小时约 450–1200 句；整本模式句子层只画当前页窗口 | 双缓冲、抽稀、按页窗口化 |
| 悬停命中 | 句首按 `StartMs` 有序，二分查找；N ≤ 9000 时可忽略 | 用二分；页码层单独数组 |
| 磁盘 | 1 小时约 75 KB JSON；手动覆盖层通常 < 10 KB | 30 天 LRU 清理自动缓存（P1）；手动层不清理 |
| 页锚点扫描 | 300 页 = 300 次小文件 JSON 读取（通常 < 100ms，机械盘/网络盘可能 > 1s） | 后台任务 + 进度；页锚点索引缓存（pdfHash + fingerprint + mtime）；缺失页不影响句子层显示 |

---

## 14. 测试计划

### 14.1 Core 单测（不依赖音频/UI）

`SentenceBoundaryBuilderTests`：

- 3 段语音 + 350ms 间隔 → 3 句。
- 3 段语音 + 200ms 间隔 → 合并为 1 句。
- 连续 15s 语音 → 按最长细遍边界拆成 ≤ 8s；无细边界 → 8s 硬切并降置信。
- 400ms 短应答夹在两句之间 → 不丢；符合规则时并入邻句。
- 150ms 极短语音 → 验证分析器 `MinSegmentMilliseconds` 设置后能保留（否则明确记录为已知限制）。
- 留白后相邻区间重叠 → 消重叠且单调递增。
- 全静音 / 0 时长 / 单句 → 兜底单句。
- 输入乱序 / 重复 / 越界 → 输出恒有序、无重叠。
- 幂等性：对输出再做一次 Build 不改变结果。
- 3–8s 目标：句长分布自评触发低置信的边界用例。

`SentenceBoundaryResolverTests`：切片/字幕/VAD 优先级、重叠取高、空洞填充、切片只覆盖部分页、切片指纹不匹配。

`AudioPageAnchorBuilderTests`：有切片锚点、手动覆盖、无锚点不生成、相邻锚点倒序丢弃、**禁止页数均分**。

`AudioTimelineStoreTests`：写入-读取 round-trip、指纹变化失效、Schema 版本失效、并发写不损坏、损坏 JSON 自愈、负缓存 TTL。

`PlaybackRangeStateTests`：未设置不停止；到达/超过停止点返回 true 并给出 snap 目标；清除后不再触发；Seek 不清除停止点（由集成测试保证）。

### 14.2 WinApp 集成

- 用合成 WAV/MP3（正弦 burst + 静音）跑 `AudioDecodeAnalysisService`，断言句数与边界。
- 命中测试抽成纯函数 `MarkerHitTester.Nearest(x, width, duration, sentences)` 后单测：任意 x 都返回最近句；页码层 Y 优先；拖拽阈值判定。
- 预览集成：`StopAt` 到点暂停、`Seek` 后仍保留、`Load/Stop` 清空、A-B/LoopMode 恢复、预览不写 `LastPositionSeconds`。
- 页码层：整本模式只画有锚点的页；PerPage 模式置灰；页锚点索引缓存命中 / fingerprint 变化失效 / 缺失页不阻塞句子层。

### 14.3 手工回归矩阵

| 维度 | 用例 |
| --- | --- |
| 音频规模 | 10 分钟 / 1 小时 / 整本书 |
| 音频类型 | 清晰朗读 / 带背景音乐 / 有噪声 / 全静音 / 单句 |
| 播放设置 | 0.5x / 1.0x / 2.0x；A-B 已设置；列表循环；最后一句预览 |
| 交互 | 悬停、点句、点刻度、Shift 单击、Alt 单击、双击、拖拽、Esc、`,/./[` |
| 点句密度 | 300px 宽 + 10 分钟 + 3–8s：任意位置单击预览正确句；拖拽仍能 seek |
| 曲目切换 | 翻页切曲、上一曲/下一曲、整篇↔按页 |
| 共享边界 | 有切片页 / 半页切片 / 切片指纹不匹配 / 无切片 |
| 页码层 | 有切片锚点 / 无锚点 / 手动锚点 / 锚点倒序 / PerPage 模式 |
| 手动修正 | 拖边界 / 合并 / 拆分 / 撤销 / 清缓存后保留 / 重分析后保留 |
| 状态 | 分析中关闭窗口、分析中切曲、源文件被替换、缓存被删、分析失败 |
| UI | 深色/浅色、100/125/150% DPI、窗口宽度 800/1200/1600 |
| 回归 | 字幕显示、跟读窗口、`VadSegmentReviewForm`、播放进度记忆 |

---

## 15. 分期实施

### P0（可用闭环，约 4–5 人日；含最小手动修正与页码层则 5–6 人日）

1. Core：实体 + `SentenceBoundaryBuilder`（3–8s）+ `SentenceBoundaryResolver` + `AudioPageAnchorBuilder` + `AudioTimelineStore` + `AudioBoundaryOverlayStore` + `PlaybackRangeState` + 单测。
2. WinApp：`AudioDecodeAnalysisService`（双阈值、`MinSpeechMs`）、`SentenceTimelineService`（缓存/取消/代际/去重）。
3. `IMediaPlayer.StopAt` + `NAudioMediaPlayer`（Load/Stop 清、Seek 不清）+ `PlaybackRangeCompleted`。
4. `AudioTimelineBar`：双层刻度/播放头/悬停/**点句模式**/拖动/键盘，替换 `TrackBar`。
5. `PdfAudioPlayerBar`：接线、tooltip、`⋯` 菜单（点句模式/页码层/重分析/灵敏度）、分析状态。
6. `PdfAudioTrackManager`：时间轴生命周期、`PreviewNearest`、A-B/LoopMode/进度保存抑制、切曲/设备变化。
7. 共享边界只读接入 + 页码层（有锚点时）+ 最小手动修正（拖一个边界/合并/拆分/撤销）。
8. 功能开关 + 手工回归。

> **建议拆成两个可独立验收的子阶段**（P0 把播放器接口、自绘控件、手动修正、页码层 4 个子系统绑在一个里程碑里，风险高、验收口径也容易糊）：
>
> - **P0a（约 2–3 人日）**：Core 模型 + 构建/合并 + 缓存 + `StopAt` + `AudioTimelineBar` 点句命中 + 预览 + 共享边界只读。验收：§1.3 的 1/2/3/4/6 条。
> - **P0b（约 2–3 人日）**：最小手动修正 + 页码层 + 覆盖率提示。验收：§1.3 的 5/7 条 + §9.7。
>
> 这样即使 P0b 延期，用户也能先拿到「点句 + 播完暂停」主行为。

> 如果排期只能保 2–3 人日：砍掉手动修正和页码层，保留“点句 + 预览 + 共享边界只读 + 缓存”，但要在验收里明确“自动断句可能需重跑灵敏度，不能手动纠错”。不建议再砍点句模式，否则 3–8s 下点击不可用。

### P1（可学习使用，约 2–3 人日）

1. 字幕文本叠加 + 字幕边界补充（含 `.srt/.lrc` mtime 刷新）。
2. 句子列表面板（`PdfAudioPanel` 抽屉；点句预览、当前句滚动、单句循环）。
3. 键盘 `,`/`.`/`[`/`]` 上下一句；`Shift+单击` 连续播放；`Esc` 优先级整理。
4. 悬停卡片（替代 ToolTip）与「+N」抽稀提示。
5. “应用边界到跟读切片”（写回 `segments.json`，takes 保护 + 确认）。
6. 缓存 LRU 清理。

### P2（质量与编辑，约 3–5 人日）

1. 完整手动编辑器（拖动刻度、增删、恢复自动、撤销栈）。
2. 可插拔分析器：WebRTC VAD / Silero VAD。
3. 导出 `.srt`（句子文本+时间），或把刻度批量转成跟读切片。
4. 与 `ShadowingSegment` 完整双向打通（`SegmentId`/锚点/切片重建）。
5. 波形/能量条叠加（长音频可视化，便于手动修边界）。

> 估算前提：现有播放/切片逻辑稳定、无需大改；若 `VadSegmentService` 抽离过程中发现耦合，P0 会再增加 1–2 人日。

---

## 16. 风险与待确认

### 16.1 风险

| 风险 | 影响 | 缓解 |
| --- | --- | --- |
| 能量 VAD 对 BGM/噪声误判 | 刻度不准、预览断在半句 | P0 明确「清晰人声优先」+ 低置信提示 + 最小手动修正；P1/P2 换引擎 |
| 3–8s 粒度 + 窄播放条 | 刻度 1–3px，无法精准点选 | **点句模式（P0 必做）** + 抽稀 + P1 句子列表 |
| 跟读切片写回破坏 takes | 用户录音错位、甚至丢失关联 | P0 只读；P1 写回前确认 + 300ms 偏移提示 + 保留 takes |
| 整本页码无时间锚点 | 页码层不可用或错误 | 只画有切片/手动来源的锚点；禁止均分；无锚点时隐藏 |
| 整本数千句渲染 | UI 卡顿、刻度糊 | 句子层按当前页窗口渲染；页码层全量；抽稀 |
| `IMediaPlayer` 接口变更 | 影响未来 VLC 实现/测试替身 | 只加一个属性/事件；目前仅 NAudio 实现；同步更新测试替身 |
| 分析阻塞/耗电 | 长音频首开体验差 | 后台 + 进度 + 可取消 + 缓存 + 开关 |
| 共享边界来源过多 | 行为难解释、难排错 | `BoundarySource` + `SourceCounts` + tooltip 显示来源；低置信提示 |
| 预览与 A-B/循环冲突 | 播放行为混乱 | 预览期间暂存 A-B、`LoopMode=None`；结束/取消恢复 |
| 预览写入阅读进度 | 打开书从“刚听的一句”开始 | 强制抑制 `SavePlaybackState`；走 `PdfAudioPlaybackService.Pause()` |
| 100ms tick 造成句尾过冲 | 听到下一句开头 | 到点 Pause 后 `Seek(EndMs)` 对齐；必要时专用预览计时器 |
| P0 范围膨胀 | 延期/质量下降 | 点句+预览+共享只读为不可砍核心；手动修正/页码层可按排期裁剪 |

### 16.2 已确认决策（原 6 问）

| 原问题 | 决策 | 文档落点 |
| --- | --- | --- |
| 点句默认行为 | 播完暂停；Shift 单击才继续 | §1.2 / §8.3 |
| 无字幕文本 | 只显示「第 N 句 + 时间」，文本为可选增强 | §1.2 / §9.4 |
| 断句粒度 | 精听短句 3–8 秒，`MaxSentenceMs=8000` | §7.2 / §7.4 |
| P0 手动修边界 | **建议 P0 做最小修正**（拖一个边界/合并/拆分/撤销）；完整编辑器 P2 | §1.0 / §15 |
| 与跟读切片共用边界 | P0 只读复用；P1 人工确认后写回；P2 完整双向 | §7.3 / §11.1 |
| 整本书页码层 | 叠一层页码起点；只画有切片/手动锚点的页，禁止均分 | §9.2 / §11.4 |

### 16.3 仍需你确认的 3 个点

1. **共用边界的写回范围**：P0 只读、P1 写回切片，这个保守解释是否符合你的“共用”预期？如果要求 P0 就双向同步，需要接受“改边界可能导致已有录音错位”，并增加失效提示/重录流程。
2. **页码锚点缺失时的策略**：当前文档默认“不画”（诚实）；若你更看重导航，可把“用相邻页插值并标灰”做成显式开关（默认关），两种都能保留，代价是多一个设置项。
3. **P0 手动修正的最低集**：拖一个边界 + 合并 + 拆分 + 撤销，是否够用？还是要加“插入新边界/删除整句”？

---

## 17. 附：最小接口草图（仅供评审）

```csharp
// Core：流式接口，不能收整轨 PCM
public interface IAudioSegmentAnalyzer
{
    string Engine { get; }
    string Version { get; }
    void Push(ReadOnlySpan<float> mono, int sampleRate);
    IReadOnlyList<VadSegment> Complete();
    void Reset();
}

// Core：来源合并
public static class SentenceBoundaryResolver
{
    public static IReadOnlyList<AudioSentenceSpan> Resolve(
        IReadOnlyList<AudioSentenceSpan> shadowing,
        IReadOnlyList<SubtitleCue> subtitles,
        IReadOnlyList<VadSegment> vad,
        AudioBoundaryOverlay? overlay,
        int durationMs,
        AudioTimelineOptions options);
}

// Core：页码锚点（禁止页数均分）
public static class AudioPageAnchorBuilder
{
    public static IReadOnlyList<AudioPageAnchor> Build(
        IReadOnlyList<AudioSentenceSpan> shadowingSpans,
        AudioBoundaryOverlay? overlay,
        int pageCount);
}

// WinApp service
public sealed class SentenceTimelineService
{
    public Task<AudioSentenceTimeline?> GetOrAnalyzeAsync(
        string pdfPath, AudioTrack track, AudioTimelineOptions options,
        IProgress<(double Ratio, int Sentences)>? progress,
        CancellationToken ct);
}

// UI control
public sealed class AudioTimelineBar : Control
{
    public IReadOnlyList<AudioSentenceSpan> Sentences { get; set; }
    public IReadOnlyList<AudioPageAnchor> PageAnchors { get; set; }
    public TimeSpan Duration { get; set; }
    public TimeSpan Position { get; set; }
    public double AnalysisProgress { get; set; }
    public bool PointSentenceMode { get; set; }
    public bool ShowPageLayer { get; set; }

    public event EventHandler<TimeSpan>? SeekRequested;
    public event EventHandler<TimeSpan>? DragPositionChanged;
    public event EventHandler<AudioSentenceSpan>? PreviewRequested;
    public event EventHandler<AudioPageAnchor>? PageClicked;
    public event EventHandler<AudioSentenceSpan?>? MarkerHovered;
}
```

---

**下一步**：先确认 §16.3 的 3 个点；确认后按 §15 的 P0 拆分任务，并优先做「点句模式 + 预览 + 共享边界只读」这条最小闭环。
