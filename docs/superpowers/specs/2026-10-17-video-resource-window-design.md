# 视频独立资源窗口 — 设计文档（spec2）

> 日期：2026-10-17
> 状态：待评审
> 范围：`LearningAssistant.WinApp`（新窗口 + `VlcMediaPlayer` 实现 + LibVLCSharp 集成）
> 前置依赖：`2026-10-17-media-kernel-design.md`（`IMediaPlayer`）
> 定位：**独立资源**（无配套 PDF，视频本身就是课程/听力材料，列表式播放），后于 spec1 交付。

---

## 一、背景与目标

英语阅读资源中有一类是纯视频课程 / 听力视频（无 PDF 对照）。这类资源需要独立的「视频资源窗口」：以目录为单位列出视频，点选即播放，具备与音频一致的四增强（倍速、AB 循环、字幕、跟读）。

**目标**：新建独立于 `PdfReaderForm` 的 `VideoResourceForm`，内嵌 `LibVLCSharp.WinForms.VideoView`，复用 `IMediaPlayer` 内核，提供视频列表 + 播放控制。

**成功标准**：

- 打开一个视频目录 → 列出其中视频文件（含可选递归）；
- 点选即播，支持倍速（不变调）、AB 循环复读、字幕（探测 sidecar）、跟读录音；
- 与 PDF 阅读器完全解耦，互不干扰。

---

## 二、已确认需求（决策记录）

| # | 决策 | 结论 |
|---|------|------|
| 1 | 资源形态 | **独立视频资源**（无 PDF 对照），以**目录**为单位组织，列表播放 |
| 2 | 播放引擎 | `LibVLCSharp`（`VideoView` + `VlcMediaPlayer`），实现 `IMediaPlayer` |
| 3 | 窗口 | 新建 `VideoResourceForm`，独立于 `PdfReaderForm`；不影响 PDF 现有逻辑 |
| 4 | 四增强 | 复用内核：倍速 / AB 循环 / 字幕 / 跟读录音（对比） |
| 5 | 字幕 | 与音频同规则：视频同名 sidecar `.srt`/`.lrc` 探测才启用 |
| 6 | 依赖引入 | 仅本项目引 `LibVLCSharp` + `LibVLCSharp.WinForms` + `VideoLAN.LibVLC.Windows`；VLC 核心库随包分发 |
| 7 | 阶段 | 后于 spec1；spec1 不引 LibVLCSharp |

---

## 三、总体架构

```
WinApp
 ├─ Forms/Media/VideoResourceForm.cs        主窗口（列表 + VideoView + 控制条）
 ├─ Forms/Media/VideoResourcePanel.cs       播放控制条（复用 SpeedSelectorControl 风格 + AB/字幕/跟读按钮）
 ├─ Managers/Media/VideoResourceManager.cs  扫描目录 → 视频列表 + 当前项状态 + 翻页式切播
 ├─ Platform/Media/VlcMediaPlayer.cs        IMediaPlayer 实现（LibVLCSharp）
 ├─ Services/Media/VideoResourceScanner.cs  （或复用 Core 扫描思路，仅扩展名不同）
 └─ Common/ServiceCollectionExtensions.cs   注册 VlcMediaPlayer + 新窗口服务

Core
 └─（复用）Abstractions/Media/* 与 Services/Media/*（字幕解析、ABLoopState）
```

**取名**：视频资源窗口用「视频」而非「媒体」命名，与套配音频（PDF 内）语义区分，避免与 spec1 的「配套音频」混淆入口。

---

## 四、资源模型（新增少量，置于 `Models/Media/`）

```csharp
public sealed class VideoResource
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string DirectoryPath { get; set; } = "";
    public long SizeBytes { get; set; }
    public string? SubtitlePath { get; set; }   // 探测到的 sidecar，无则 null
}
```

**扫描**：扩展名 `.mp4 .mkv .avi .mov .wmv .flv .webm`；默认只扫所选目录，提供 `IncludeSubDirectories` 开关；后台线程扫描 + 可取消（沿用 spec1 目录扫描约束）。

> 视频资源的列表/收藏/进度持久化**不在本期**（见 §八）；本期按目录临时列举，打开窗口即扫描最新目录状态。

---

## 五、UI 设计

### 5.1 `VideoResourceForm`（主窗口）

三段式布局：

- 左侧：视频列表（文件名 + 时长 + 字幕有无标记）；
- 中间：`VideoView` 播放区域（保持 16:9 比例，黑底）；
- 底部：复用 `VideoResourcePanel` 控制条：▶/⏸、进度条（可拖拽 Seek）、倍速控件（复用 `SpeedSelectorControl` 交互风格）、AB 循环（设 A / 设 B / 清除）、字幕开关 + 字幕文本高亮行、跟读按钮。

### 5.2 字幕与跟读

- 字幕：边栏底部常驻「当前句」高亮；点击历史句 `Seek(Cue.StartTime)` 跳转；开关控制显隐。
- 跟读：复用 `NAudioRecorderService`，「播原音 → 录音 → 手动回放对比」，与播放互斥由本窗口协调。

---

## 六、`VlcMediaPlayer`（IMediaPlayer 实现）要点

- 依赖注入 `LibVLC`（`VideoView.MediaPlayer` 或自建 `VlcMediaPlayer` + 绑定 `VideoView.MediaPlayer`）；
- `Speed` → `SetRate(0.6–2.0)`；
- AB 循环 → `TimeChanged` 订阅 + `AbLoopState.ShouldLoopBack` + `Seek(A)`；
- 字幕 → `SubtitleParser` 解析 sidecar 暴露 `SubtitleCues`；LibVLC 原生字幕渲染仅作补充，点句跳转以 `SubtitleParser` 结果为准（保证与音频侧一致）；
- 生命周期：窗口关闭 `Dispose` 释放 `LibVLC`/`VlcMediaPlayer`，避免占用设备。

---

## 七、文件清单

**新增文件 — WinApp**

- `LearningAssistant.WinApp/Models/Media/VideoResource.cs`
- `LearningAssistant.WinApp/Services/Media/VideoResourceScanner.cs`（+ 接口，或复用 Core 通用目录枚举）
- `LearningAssistant.WinApp/Platform/Media/VlcMediaPlayer.cs`
- `LearningAssistant.WinApp/Managers/Media/VideoResourceManager.cs`
- `LearningAssistant.WinApp/Forms/Media/VideoResourceForm.cs`
- `LearningAssistant.WinApp/Forms/Media/VideoResourcePanel.cs`

**新增文件 — Core**（仅当扫描逻辑需与 spec1 共享时）

- `LearningAssistant.Core/Services/Media/ICompanionFileScanner.cs`（可选：音频/视频/字幕 sidecar 统一枚举）

**修改文件**

- `LearningAssistant.WinApp/LearningAssistant.WinApp.csproj` → 加入 LibVLCSharp 包引用
- `LearningAssistant.WinApp/Common/ServiceCollectionExtensions.cs` → 注册 `VlcMediaPlayer`、视频窗口服务

---

## 八、明确不做（Out of Scope）

- 视频列表收藏、播放进度持久化（跨会话记忆上次进度）；
- 视频转码、格式转换、在线流媒体/HTTP 流；
- 弹幕、画中画、多音轨/多字幕轨切换界面（仅探测单一 sidecar 字幕）；
- 视频内容上传给 AI。

---

## 九、风险与取舍

| 风险 | 取舍 / 缓解 |
|------|-------------|
| LibVLCSharp 原生库分发体积 | 仅 spec2 引入；spec1 不引 |
| 不同显卡/平台 VideoView 渲染差异 | 提供「软解/硬解」可切换或异常降级提示；不做跨平台承诺（仅 Windows） |
| 字幕编码/对齐与音频侧不一致 | 统一走 Core `SubtitleParser`，点句跳转语义一致 |
| 跟读与视频播放互斥 | 由 `VideoResourceManager` 统一协调 |