# 网盘视频观看记录 + 定期打开视频笔记 — 设计文档（方案稿）

> 日期：2026-11-01
> 状态：待评审（**仅方案，未实施**）
> 范围：`LearningAssistant.Core`（模型 / 实体 / 服务）+ `LearningAssistant.WinApp`（WebView2 采集 + 记录列表窗口 + 提醒入口）
> 关联文档：`docs/百度网盘深度融合方案.md`（方案一「播放进度记录」P1、方案二「视频笔记」）

---

## 一、背景与目标

用户的学习视频存放在百度网盘，**笔记是网盘自身提供的功能**（网盘视频播放页的「视频笔记 / AI 视频笔记」面板），不是本程序的 `INoteService` 笔记。

因此本需求只做两件事：

1. **观看记录存储**：记住「哪个网盘视频、看到第几秒、看到什么时候、看了几次」。
2. **按记录定期打开视频笔记**：到了设定时间，自动/一键在 WebView2 里打开该网盘视频的播放页（含网盘笔记面板），并定位到上次观看位置，让用户接着看并在网盘里记笔记。

**明确不做**（避免与网盘功能冲突）：

- 不复制、不解析、不回写网盘笔记正文（本地笔记体系 `NoteItem` 保持独立，二者不双写）；
- 不下载视频到本地、不做网盘视频的本地播放（项目内暂无 `VideoResourceForm` 实现，也不在本期引入 LibVLCSharp）；
- 不覆盖网盘客户端 / 手机 App 的观看行为（这些场景只能靠手动登记）。

### 1.1 成功标准

- 在程序内用 WebView2 播过一段网盘视频后，关闭再打开，能从「上次看到的位置」继续（播放页定位到该秒）。
- 记录列表可看到每个视频的进度、观看次数、最后观看时间，可手动修改进度。
- 设置「每天 / 每周 X 点 / 距上次观看 N 小时后」定期提醒，到点后程序提示，用户确认即打开该视频的网盘播放页（笔记面板可用）并定位进度。
- 提醒不重复轰炸：同一视频同一次排期只触发一次，未处理可顺延。

---

## 二、关键前提（实施前必须先验证，P0 spike）

| # | 假设 | 影响 | 验证方式 | 若不成立时的降级 |
|---|------|------|----------|------------------|
| 1 | 网盘网页版视频播放页 URL 形如 `https://pan.baidu.com/play/video#/video?path=<urlencoded path>`，并可通过参数定位起播时间（如 `&t=<秒>`） | 「定期打开并定位进度」的核心 | 手工在浏览器里试 `t=` 参数是否生效；同时抓取真实播放页 URL | 打开 URL 后注入 JS 设置 `video.currentTime = 上次进度`（见 5.4） |
| 2 | 播放页内 `document.querySelector('video')` 能拿到视频元素，`currentTime` 可读 | 自动采集进度 | WebView2 内执行脚本观察 | 退化为「手动登记进度 + 一键记录当前时间点」 |
| 3 | 网盘笔记面板在播放页内可打开（DOM 选择器稳定） | 「打开视频笔记」是否一步到位 | 观察播放页笔记入口元素 | 只负责打开播放页，笔记由用户点开；或按笔记入口 URL 单独打开 |
| 4 | WebView2 登录态可复用（`CachePaths.WebView2` 用户数据目录已持久化，见 `WebView2BrowserForm.EnsureEnvironmentAsync`） | 免重复登录 | 关闭程序后重开验证 | 首次打开引导登录一次，检测未登录时提示 |

> 现有代码已具备的基础：`WebView2BrowserForm` 已能导航 `https://pan.baidu.com`、已用 JS 从页面/URL 解析网盘 `path`（`ExtractPanPathViaJsAsync` / `ExtractPanPathFromUrl`），可沿用同一套路径解析思路。

---

## 三、数据模型

### 3.1 三张表（全部继承 `UserEntityBase`，自动带 `UserId` / `CreatedAt` / `UpdatedAt` / `RowVersion`）

| 表 | 用途 | 关键字段 |
|----|------|----------|
| `PanVideoWatchRecords` | 每个网盘视频**一行**当前状态 | `PathKey`、`FsId`、`Title`、`DurationSec`、`LastPositionSec`、`MaxPositionSec`、`CompletedRatio`、`WatchCount`、`TotalWatchedSec`、`LastViewedAt`、`PageUrl`、`NotePanelOpened` |
| `PanVideoWatchSessions` | 每次观看**一段**明细（用于统计/趋势） | `RecordId`、`StartedAt`、`EndedAt`、`StartPositionSec`、`EndPositionSec`、`WatchedSec`、`Source`（WebView/Manual） |
| `PanVideoNoteReminders` | 定期打开笔记的排期 | `RecordId`、`RepeatType`（Daily / Weekly / Interval / Once）、`RepeatDaysJson`、`TimeOfDay`、`IntervalHours`、`NextTriggerAt`、`LastTriggeredAt`、`Enabled`、`AutoOpen`、`ResumePosition` |

**稳定性键约定**：`PathKey = 规范化后的相对网盘路径（`/` 开头、统一分隔符、去重命名后缀仅作展示）`，唯一索引 `(UserId, PathKey)`。
`FsId` 作为可选增强（网盘重命名/移动后仍是同一文件），从 `BaiduPanApiClient.GetFileMetaAsync` / `GetFileListAsync` 回填；拿不到时以 `PathKey` 为准（与现有 `PanAnalysisModels` 中 `FsId` 的用法一致）。

### 3.2 实体代码片段（`LearningAssistant.Core.Data.Database.Entities.cs` 追加）

```csharp
public class PanVideoWatchRecordEntity : UserEntityBase
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(1000)] public string PathKey { get; set; } = string.Empty;
    public long? FsId { get; set; }
    [MaxLength(300)] public string Title { get; set; } = string.Empty;
    public int DurationSec { get; set; }
    public int LastPositionSec { get; set; }
    public int MaxPositionSec { get; set; }
    public double CompletedRatio { get; set; }
    public int WatchCount { get; set; }
    public int TotalWatchedSec { get; set; }
    public DateTime LastViewedAt { get; set; } = DateTime.MinValue;
    [MaxLength(1500)] public string? PageUrl { get; set; }
    public bool NotePanelOpened { get; set; }
}

public class PanVideoWatchSessionEntity : UserEntityBase
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecordId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public int StartPositionSec { get; set; }
    public int EndPositionSec { get; set; }
    public int WatchedSec { get; set; }
    [MaxLength(20)] public string Source { get; set; } = "Manual";   // WebView / Manual
}

public class PanVideoNoteReminderEntity : UserEntityBase
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecordId { get; set; }
    [MaxLength(20)] public string RepeatType { get; set; } = "Daily"; // Daily/Weekly/Interval/Once
    public string? RepeatDaysJson { get; set; }                        // 复用 Reminder 的序列化方式
    public TimeSpan TimeOfDay { get; set; } = new TimeSpan(19, 30, 0);
    public int IntervalHours { get; set; } = 48;
    public DateTime NextTriggerAt { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public bool Enabled { get; set; } = true;
    public bool AutoOpen { get; set; }                                 // true=到点自动打开，false=仅提示
    public int ResumePosition { get; set; } = -1;                      // -1=沿用记录进度
}
```

**Schema 落地方式**（沿用现有手写建表 + 修复的模式，与 `RepairReminderRepeatDaysTable` 一致）：

- `AppDbContext` 增加 3 个 `DbSet`；
- `EnsureCreated` 流程里增加 `CREATE TABLE IF NOT EXISTS ...`（含 `RowVersion` 列 `DEFAULT 0`）；
- `RepairSchema()` 增加 `RepairPanVideoWatchTables()`（幂等），并建索引：
  `IX_PanVideoWatchRecords_UserId_PathKey`（唯一）、`IX_PanVideoWatchSessions_RecordId`、`IX_PanVideoNoteReminders_NextTriggerAt`。

---

## 四、观看记录采集与存储

### 4.1 三个入口（按可信度排序）

| 入口 | 场景 | 精度 |
|------|------|------|
| A. WebView2 自动采集 | 程序内打开网盘播放页 | 每 10s 上报 + 暂停/关闭时即时上报 |
| B. 一键登记 | 用户在记录列表点「记录当前时间点」/手填 | 手动输入秒数 |
| C. 导入登记 | 用户在别的设备看过，手填「看到 00:32:10」 | 手动输入秒数 |

### 4.2 采集脚本（WebView2BrowserForm 内，仅在 `pan.baidu.com` 的播放页启用）

```csharp
// NavigationCompleted 后：检测 video 元素，存在则注入（并订阅一次 WebMessageReceived）
private async Task AttachVideoProgressAsync(WebView2 webView)
{
    const string script = @"
        (function(){
          var v = document.querySelector('video'); if(!v) return false;
          if (window.__laVideoHook) return true;               // 防重复注入
          window.__laVideoHook = true;
          var lastReport = -1, tick = function(){
            if (v.currentTime === lastReport) return;
            lastReport = v.currentTime;
            window.chrome.webview.postMessage({ kind:'panVideoProgress',
              path: decodeURIComponent((location.hash.match(/path=([^&]*)/)||[])[1]||''),
              pos: Math.floor(v.currentTime), duration: Math.floor(v.duration||0),
              paused: v.paused });
          };
          setInterval(tick, 10000);
          v.addEventListener('pause', tick); v.addEventListener('seeked', tick);
          window.addEventListener('beforeunload', tick);
          return true;
        })();";
    await webView.CoreWebView2.ExecuteScriptAsync(script);
}
```

宿主侧收到消息 → `IPanVideoWatchRecordService.ReportProgressAsync(userId, pathKey, pos, duration, source: "WebView")`，**写入策略**：

- 单例 `SemaphoreSlim(1,1)` 串行化同一 `PathKey` 的写入（避免多标签页并发覆盖）；
- 只在 `pos` 变化 ≥ 5s 或状态切换（暂停/关闭）时落库，避免高频写；
- 会话合并：同一 `PathKey` 30 分钟内的上报合并为同一条 session，`WatchedSec` 用 `|pos - 上一次 pos|` 累加（做 1.5 倍时长上限保护，防止拖动进度条灌水）；
- `LastPositionSec ≤ DurationSec - 15` 记为「未完成」，接近片尾则 `CompletedRatio` 置 1。

### 4.3 服务接口（`LearningAssistant.Core/Services/PanVideo/IPanVideoWatchRecordService.cs`）

```csharp
public interface IPanVideoWatchRecordService
{
    Task<PanVideoWatchRecord> UpsertAsync(string userId, PanVideoProgressDto dto);   // 上报进度（幂等）
    Task<PanVideoWatchRecord?> GetAsync(string userId, string pathKey);
    Task<List<PanVideoWatchRecord>> GetRecentAsync(string userId, int count = 50);   // 按 LastViewedAt 倒序
    Task<bool> SetPositionAsync(string userId, Guid recordId, int positionSec);      // 手动修正进度
    Task<bool> DeleteAsync(string userId, Guid recordId);
}
```

统一约定：`userId` 由 WinApp 侧 `IUserSessionService.GetCurrentUserId()` 取到后传入 Core 服务（不在 Core 内部读环境变量）。

---

## 五、定期打开视频笔记（调度）

### 5.1 调度形态：沿用「每分钟一次 Timer」的既有模式

`SqliteLearningReminderService` 已用 `System.Timers.Timer(1 分钟)` + `ReminderTriggered` 事件（`Core/Services/Learning/SqliteLearningReminderService.cs`）。新建独立的 `PanVideoNoteScheduler` 采用同样形态，**不改动现有提醒逻辑**：

```csharp
public interface IPanVideoNoteScheduler : IDisposable
{
    void Start();                                   // 程序启动时调用（与现有 reminderService.Start 同处）
    event EventHandler<PanVideoNoteDueEventArgs>? NoteDue;   // 到点事件，由 WinApp 消费
    Task RecomputeNextAsync(string userId, Guid reminderId); // 规则变更后重算
    Task SnoozeAsync(string userId, Guid reminderId, TimeSpan delay);  // 稍后提醒（默认 +2h）
}
```

### 5.2 触发与递推（纯逻辑，可单测）

```
到点判定：Enabled && NextTriggerAt <= now
触发后：LastTriggeredAt = now；按 RepeatType 递推 NextTriggerAt
  Daily    → 次日 TimeOfDay（若已过则再 +1 天）
  Weekly   → RepeatDays 中下一个命中日的 TimeOfDay
  Interval → 距上次观看 (LastViewedAt) 满 IntervalHours 后的下一个整点（默认 48h）
  Once     → Enabled = false
去重：同一 reminder 两次触发至少间隔 30 分钟；同一 PathKey 24h 内最多提示 1 次
```

`Interval` 模式与「根据记录」直接相关：**记录显示「上次看到 X，已过 48 小时未继续」才提醒**，这是本方案默认推荐模式。

### 5.3 到点后的用户交互（WinApp）

- 默认 `AutoOpen = false`：托盘气泡 / 主窗口顶部提示条「该继续《课程名》了 · 上次看到 00:32:10」+「打开」「稍后」按钮；
- 点「打开」→ 打开网盘播放页（见 5.4）；「稍后」→ `SnoozeAsync(+2h)`；「今天不再提醒」→ 写 `LastTriggeredAt` 并跳过当天排期；
- `AutoOpen = true` 时：仅当程序在前台、且当前没有打开的播放页时自动导航，否则降级为提示，避免打断用户。

### 5.4 打开目标 URL 的构造（`Core/Services/PanVideo/PanVideoUrlBuilder.cs`，纯函数便于测试）

```csharp
public static class PanVideoUrlBuilder
{
    /// <param name="resumeSec">续播秒数，-1 表示不带</param>
    public static string BuildPlayUrl(string netdiskPath, int resumeSec = -1)
    {
        var encoded = Uri.EscapeDataString(NormalizePath(netdiskPath));
        var url = $"https://pan.baidu.com/play/video#/video?path={encoded}";
        if (resumeSec > 0) url += $"&t={resumeSec}";      // 需 spike 验证；不支持则由 5.5 兜底
        return url;
    }
    public static string NormalizePath(string p) => /* 统一 '/' 开头、去尾部 '/'、折叠重复分隔符 */;
}
```

### 5.5 兜底定位（若 URL 参数不生效）

打开播放页待 `NavigationCompleted` → 检测到 `video` 后执行：

```javascript
(function(){ var v=document.querySelector('video'); if(!v) return false;
  v.currentTime = Math.max(0, LAST_POS); return true; })()
```

并用 `NavigationCompleted` 后 1.5s 的短轮询（最多 3 次）等待播放器就绪，避免脚本跑在播放器初始化之前。

---

## 六、文件变更清单

### 6.1 新增（Core）

| 文件 | 内容 |
|------|------|
| `LearningAssistant.Core/Models/PanVideo/PanVideoWatchModels.cs` | `PanVideoWatchRecord` / `PanVideoWatchSession` / `PanVideoNoteReminder` / `PanVideoProgressDto` / `PanVideoNoteDueEventArgs` |
| `LearningAssistant.Core/Services/PanVideo/IPanVideoWatchRecordService.cs` | 4.3 节接口 |
| `LearningAssistant.Core/Services/PanVideo/PanVideoWatchRecordService.cs` | Upsert / 会话合并 / 进度归一 / 重试（复用 `AppDbContext` 既有并发重试） |
| `LearningAssistant.Core/Services/PanVideo/IPanVideoNoteScheduler.cs` | 5.1 节接口 |
| `LearningAssistant.Core/Services/PanVideo/PanVideoNoteScheduler.cs` | 每分钟检查 + 递推 + 去重 + 事件 |
| `LearningAssistant.Core/Services/PanVideo/PanVideoRepeatRuleCalculator.cs` | `NextTriggerAt` 计算（纯函数，单测重点） |
| `LearningAssistant.Core/Services/PanVideo/PanVideoUrlBuilder.cs` | 5.4 节 URL 构造 + 路径规范化 |

### 6.2 新增（WinApp）

| 文件 | 内容 |
|------|------|
| `LearningAssistant.WinApp/Forms/PanVideo/PanVideoRecordsForm.cs(.Designer.cs)` | 观看记录列表：进度条、观看次数、最后观看、`继续观看`、`记录当前时间点`、`设置提醒`、`删除` |
| `LearningAssistant.WinApp/Forms/PanVideo/PanVideoReminderDialog.cs` | 提醒规则编辑（Daily / Weekly / Interval / Once + 时间 + 自动打开开关） |

> UI 遵循既有约定：WinForms 控件先 `new` 再赋值、`Controls.Add` 前后顺序正确、配色沿用紫色 `#8B5CF6` 主题与统一扁平按钮；`Pan*` 目录命名与 `Forms/BaiduPan/`、`Forms/UserControls/Pan/` 保持一致风格。

### 6.3 修改（少量、可控）

| 文件 | 改动 |
|------|------|
| `LearningAssistant.Core/Data/Database/Entities.cs` | 追加 3 个实体（3.2 节） |
| `LearningAssistant.Core/Data/Database/AppDbContext.cs` | 3 个 `DbSet` + `EnsureCreated` 建表 + `RepairSchema` 调用 + `RepairPanVideoWatchTables()` + 索引 |
| `LearningAssistant.WinApp/Common/ServiceCollectionExtensions.cs` | 注册 `IPanVideoWatchRecordService`（Singleton）、`IPanVideoNoteScheduler`（Singleton） |
| `LearningAssistant.WinApp/Program.cs` | 启动 `PanVideoNoteScheduler.Start()`、退出 `DisposeService`（与现有 `reminderService` 同处，约 L80/L131） |
| `LearningAssistant.WinApp/Forms/Web/WebView2BrowserForm.cs` | ① `NavigationCompleted` 检测播放页并注入采集脚本；② `WebMessageReceived` 处理 `panVideoProgress`；③ 新增 public `OpenPanVideoAsync(path, resumeSec)`（复用 `NavigateToUrl`）；④ 关闭/切换标签时 flush 最后一次进度 |
| 主窗口 / 托盘入口 | 增加「网盘视频记录」菜单项（打开 `PanVideoRecordsForm`）与提醒提示条/气泡 |

### 6.4 新增（测试）

| 文件 | 用例 |
|------|------|
| `LearningAssistant.Core.Tests/PanVideo/PanVideoRepeatRuleCalculatorTests.cs` | Daily 跨日、Weekly 多选日、Interval 基于 `LastViewedAt`、Once 关闭、时间已过顺延、DST/午夜边界 |
| `LearningAssistant.Core.Tests/PanVideo/PanVideoWatchRecordServiceTests.cs` | 首次插入/重复上报合并、快进保护、完成后 `CompletedRatio=1`、并发同 PathKey 串行 |
| `LearningAssistant.Core.Tests/PanVideo/PanVideoUrlBuilderTests.cs` | 中文路径编码、`resumeSec<=0` 不带 t、路径规范化 |

---

## 七、分阶段交付（每阶段可独立验收）

| 阶段 | 内容 | 验收 |
|------|------|------|
| **P0（spike，0.5 天）** | 验证第二节 4 个假设；产出结论写回本文档；不做产品代码 | 记录真实播放页 URL、能否定位秒数、能否读到 `currentTime` |
| **P1（记录存储，1.5 天）** | 3 张表 + `IPanVideoWatchRecordService` + `PanVideoRecordsForm`；先只支持**手动/一键登记**进度 | 重启后记录仍在，列表可继续观看（打开播放页） |
| **P2（自动采集，1 天）** | WebView2 注入脚本 + WebMessage 上报 + 关闭 flush + 快进保护 | 播放 5 分钟后关闭，记录自动 +300s 左右 |
| **P3（定期提醒，1.5 天）** | `PanVideoNoteScheduler` + 规则对话框 + 托盘/主窗口入口 + `Snooze` | 设 Interval=1 小时后到点提示，点打开即定位续播 |
| **P4（可选）** | 记录统计（按课程/目录聚合，接入 `ILearningStatsAggregator`）、导出 Markdown 观看清单 | 统计页可见 |

---

## 八、风险与降级

| 风险 | 说明 | 对策 |
|------|------|------|
| 网盘网页 DOM/URL 变更 | 选择器与 `path` 参数解析失效 | 采集与 URLs 集中在 `PanVideoUrlBuilder` + 单一注入点；失败静默降级为手动登记，并写日志 |
| 登录态/会员限制 | 视频笔记可能是会员功能 | 检测未登录/无权限时提示，不阻断记录功能 |
| 多标签页同视频并发 | 进度互相覆盖 | 按 `PathKey` 串行 + 有会话的时间戳合并（`LastViewedAt` 大者胜） |
| 拖动进度条灌水 | `WatchedSec` 虚高 | 单次上报增量上限 1.5 × 上报间隔 |
| 提醒打扰 | 到点自动打开会打断 | 默认 `AutoOpen=false`；同视频 24h 内最多提示一次 |
| 记录跨设备不一致 | 网盘只存文件，不存进度 | 文档明示「记录仅本机有效」，可选导出/导入 |

---

## 九、待确认的 3 个决策点

1. **观看发生的场景**：只在「本程序 WebView2 内」算受控采集（推荐），网盘客户端/手机上的观看通过手动登记？还是希望程序去读网盘客户端的本地缓存（不稳定，不推荐）？
2. **「视频笔记」的具体入口**：是播放页内右侧笔记面板（打开播放页即可见）；还是网盘里有独立的笔记页面/文件夹（需要单独 URL 或文件 API 读取）？——决定 P0 spike 的验证对象与 `PanVideoUrlBuilder` 形态。
3. **默认提醒节奏**：推荐 `Interval`（距上次观看 48 小时未继续才提醒，22:00 提示，手动确认打开）；是否要同时提供 `Daily/Weekly` 固定时间？
