# 网盘视频观看记录 + 定期打开视频笔记 — 设计文档（方案稿）

> 日期：2026-11-01
> 状态：需求已确认（**仅方案，未实施**）
> 范围：`LearningAssistant.Core`（模型 / 实体 / 服务）+ `LearningAssistant.WinApp`（WebView2 采集 + 记录列表窗口 + 提醒入口）
> 关联文档：`docs/百度网盘深度融合方案.md`（方案一「播放进度记录」P1、方案二「视频笔记」）

---

## 一、背景与目标

用户的学习视频存放在百度网盘，**笔记是网盘自身提供的功能**：它是网盘视频**网页播放（非全屏）时页面右侧的笔记面板**，没有独立入口，不是本程序的 `INoteService` 笔记。

> **约束推论**：笔记面板依赖「普通网页播放态 + 播放页右侧区域」。因此程序打开视频时必须保持**网页模式（不进全屏）**、且不能把播放页挤窄导致右侧面板收起；全屏态下笔记面板不可用（视为已知限制，由用户自行退出全屏）。

因此本需求只做两件事：

1. **观看记录存储**：记住「哪个网盘视频、看到第几秒、看到什么时候、看了几次」。
2. **按记录定期打开视频笔记**：到了设定时间，自动/一键在 WebView2 里打开该网盘视频的播放页（含网盘笔记面板），并定位到上次观看位置，让用户接着看并在网盘里记笔记。

**明确不做**（避免与网盘功能冲突）：

- 不复制、不解析、不回写网盘笔记正文（本地笔记体系 `NoteItem` 保持独立，二者不双写）；
- 不下载视频到本地、不做网盘视频的本地播放（项目内暂无 `VideoResourceForm` 实现，也不在本期引入 LibVLCSharp）；
- 不覆盖网盘客户端 / 手机 App 的观看行为（**已确认：仅「本程序 WebView2 内播放」做自动采集，其它场景走手动登记**）。

### 1.1 成功标准

- 在程序内用 WebView2 播过一段网盘视频后，关闭再打开，能从「上次看到的位置」继续（播放页定位到该秒）。
- 记录列表可看到每个视频的进度、观看次数、最后观看时间，可手动修改进度。
- 距上次观看满 48h 未继续时提醒；到点提示后点「打开」→ 进入**网页播放态（非全屏）**、定位到上次进度、右侧笔记面板可直接使用。
- 提醒不重复轰炸：同一视频同一次排期只触发一次，未处理可顺延。

---

## 二、关键前提（实施前必须先验证，P0 spike）

| # | 假设 | 影响 | 验证方式 | 若不成立时的降级 |
|---|------|------|----------|------------------|
| 1 | 网盘网页版视频播放页 URL 形如 `https://pan.baidu.com/play/video#/video?path=<urlencoded path>`，并可通过参数定位起播时间（如 `&t=<秒>`） | 「定期打开并定位进度」的核心 | 手工在浏览器里试 `t=` 参数是否生效；同时抓取真实播放页 URL | 打开 URL 后注入 JS 设置 `video.currentTime = 上次进度`（见 5.5） |
| 2 | 播放页内 `document.querySelector('video')` 能拿到视频元素，`currentTime` 可读 | 自动采集进度 | WebView2 内执行脚本观察 | 退化为「手动登记进度 + 一键记录当前时间点」 |
| 3 | **笔记面板在网页播放（非全屏）态下位于播放页右侧**：打开播放页后默认可见，或需点击一次「笔记」页签/按钮才展开 | 「打开视频笔记」是否一步到位 | 观察播放页右侧结构与页签；记录展开所需的点击目标 | 只负责打开播放页并自动尝试点击展开，失败时提示用户手动展开；记录展开结果（`NotePanelOpened`） |
| 4 | WebView2 登录态可复用（`CachePaths.WebView2` 用户数据目录已持久化，见 `WebView2BrowserForm.EnsureEnvironmentAsync`） | 免重复登录 | 关闭程序后重开验证 | 首次打开引导登录一次，检测未登录时提示 |
| 5 | 笔记面板在**全屏播放**态下不可用（右侧面板被隐藏/覆盖） | 打开策略必须避免进全屏 | 播放页点全屏按钮后观察右侧面板 | 打开后绝不触发全屏；检测到全屏态时提示「退出全屏可看到笔记面板」 |

> 现有代码已具备的基础：`WebView2BrowserForm` 已能导航 `https://pan.baidu.com`、已用 JS 从页面/URL 解析网盘 `path`（`ExtractPanPathViaJsAsync` / `ExtractPanPathFromUrl`），可沿用同一套路径解析思路。

---

## 三、数据模型

### 3.1 三张表（全部继承 `UserEntityBase`，自动带 `UserId` / `CreatedAt` / `UpdatedAt` / `RowVersion`）

| 表 | 用途 | 关键字段 |
|----|------|----------|
| `PanVideoWatchRecords` | 每个网盘视频**一行**当前状态 | `CloudAccountId`、`PathKey`、`FsId`、`Title`、`DurationSec`、`LastPositionSec`、`MaxPositionSec`、`CompletedRatio`、`WatchCount`、`EffectiveWatchedSec`、`LastViewedAt`、`PageUrl`、`NotePanelOpened`、`Status`（Ok/Unavailable） |
| `PanVideoWatchSessions` | 每次观看**一段**明细（用于统计/趋势） | `RecordId`、`StartedAt`、`EndedAt`、`StartPositionSec`、`EndPositionSec`、`EffectiveWatchedSec`、`Source`（WebView/Manual） |
| `PanVideoNoteReminders` | 定期打开笔记的排期 | `RecordId`、`RepeatType`（默认 **Interval**，可选 Daily/Weekly/Once）、`RepeatDaysJson`、`TimeOfDay`、`IntervalHours`（默认 48）、`NextTriggerAt`、`LastTriggeredAt`、`Enabled`、`AutoOpen`、`ResumePosition` |

**稳定性键约定**：`PathKey = 规范化后的相对网盘路径（`/` 开头、统一分隔符）`，唯一索引 `(UserId, CloudAccountId, PathKey)`。
`CloudAccountId` 必填（取当前网盘登录账号标识，如 token 对应的 uid）——同一用户切换网盘账号时进度不会互相覆盖。
`FsId` 作为可选增强（网盘重命名/移动后仍是同一文件），从 `BaiduPanApiClient.GetFileMetaAsync` / `GetFileListAsync` 回填；**无开放平台 token 时不回填，功能不受影响**（提醒与打开流程不依赖 API）。
文件失效（被删/移动/权限失效）时把 `Status` 置 `Unavailable` 并停用其提醒，不做删除（保留历史统计）。

### 3.2 实体代码片段（`LearningAssistant.Core.Data.Database.Entities.cs` 追加）

```csharp
public class PanVideoWatchRecordEntity : UserEntityBase
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(100)] public string CloudAccountId { get; set; } = string.Empty; // 网盘账号维度
    [Required, MaxLength(1000)] public string PathKey { get; set; } = string.Empty;
    public long? FsId { get; set; }
    [MaxLength(300)] public string Title { get; set; } = string.Empty;
    public int DurationSec { get; set; }
    public int LastPositionSec { get; set; }
    public int MaxPositionSec { get; set; }
    public double CompletedRatio { get; set; }
    public int WatchCount { get; set; }
    public int EffectiveWatchedSec { get; set; }                       // 播放区间并集，回看/拖拽不虚增
    public DateTime LastViewedAt { get; set; } = DateTime.MinValue;
    [MaxLength(1500)] public string? PageUrl { get; set; }
    public bool NotePanelOpened { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "Ok";         // Ok / Unavailable
}

public class PanVideoWatchSessionEntity : UserEntityBase
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecordId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public int StartPositionSec { get; set; }
    public int EndPositionSec { get; set; }
    public int EffectiveWatchedSec { get; set; }                        // 本会话区间并集时长
    [MaxLength(20)] public string Source { get; set; } = "Manual";      // WebView / Manual
}

public class PanVideoNoteReminderEntity : UserEntityBase
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecordId { get; set; }
    [MaxLength(20)] public string RepeatType { get; set; } = "Interval"; // 默认 Interval；可选 Daily/Weekly/Once
    public string? RepeatDaysJson { get; set; }                        // 复用 Reminder 的序列化方式
    public TimeSpan TimeOfDay { get; set; } = new TimeSpan(19, 30, 0); // 仅 Daily/Weekly 使用
    public int IntervalHours { get; set; } = 48;                       // 默认：距上次观看 48h 未继续
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
  `IX_PanVideoWatchRecords_UserId_Account_PathKey`（唯一，含 `CloudAccountId`）、`IX_PanVideoWatchSessions_RecordId`、`IX_PanVideoNoteReminders_NextTriggerAt`。

---

## 四、观看记录采集与存储

### 4.1 三个入口（按可信度排序）

| 入口 | 场景 | 精度 |
|------|------|------|
| A. WebView2 自动采集 | 程序内打开网盘播放页 | 宿主每 15s 主动拉取 + 切换/关闭前补拉 |
| B. 一键登记 | 用户在记录列表点「记录当前时间点」/手填 | 手动输入秒数 |
| C. 导入登记 | 用户在别的设备看过，手填「看到 00:32:10」 | 手动输入秒数 |

### 4.2 采集方式：宿主主动拉取，不依赖页面定时器

**修正点（评审）**：不能让页面用 `setInterval` 主动 `postMessage`，因为 ① Chromium 对后台/最小化标签的定时器会节流（10s 可能变 60s+）；② `beforeunload` 在进程退出/窗口关闭时不保证执行；③ 网盘播放页是 **hash 路由（`#/video`）**，切换视频不会触发 `NavigationCompleted`。

```csharp
// 宿主侧：System.Windows.Forms.Timer(15s) + SourceChanged 事件触发；选择器来自 PanVideoOptions
private async Task PullVideoProgressAsync(WebView2 webView)
{
    if (!(webView.Source?.AbsoluteUri.StartsWith("https://pan.baidu.com/") ?? false)) return; // 域名白名单
    var json = await webView.CoreWebView2.ExecuteScriptAsync($@"
        (function(){{
          var v = document.querySelector('{_options.VideoSelector}'); if(!v) return {{ok:false}};
          return {{ok:true,
            path: decodeURIComponent((location.hash.match(/path=([^&]*)/)||[])[1]||''),
            pos: Math.floor(v.currentTime||0), duration: Math.floor(v.duration||0), paused: v.paused}};
        }})();");
    // 解析 → ReportProgressAsync(userId, cloudAccountId, dto) → 变化 < 5s 且 paused 未变则跳过落库
}
```

绑定与补拉：

- 绑定 `CoreWebView2.SourceChanged`（覆盖 `#/video` 路由切换，`NavigationCompleted` 不会触发）→ 重新识别播放页并立即 Pull；
- 绑定 `NavigationCompleted` → 首次 Pull（等待播放器就绪时最多重试 3 次，间隔 1.5s）；
- 标签页切换 / 窗口关闭 / 程序退出前**同步 Pull 一次**，保证最后一段进度不丢；
- 多标签页独立持有各自的 `PathKey` 与上次采样值，互不污染。

### 4.3 写入策略

- 单例 `SemaphoreSlim(1,1)` 串行化同一 `(CloudAccountId, PathKey)` 的写入（避免多标签页并发覆盖）；
- 只在 `pos` 变化 ≥ 5s 或 `paused` 状态切换时落库，避免高频写；
- **有效观看时长用「播放区间并集」计算**：会话内维护已覆盖的 `[start,end]` 区间列表并合并，回看同一段不重复计、拖进度条只计实际新覆盖区间；会话结束时把并集长度写入 `EffectiveWatchedSec`（纯函数，单测覆盖）；
- 会话合并：同一 `PathKey` 30 分钟内的上报合并为同一条 session；
- `LastPositionSec ≤ DurationSec - 15` 记为「未完成」，接近片尾则 `CompletedRatio` 置 1；
- 上报携带 `CloudAccountId`，切换网盘账号不会写串。

### 4.4 服务接口（`LearningAssistant.Core/Services/PanVideo/IPanVideoWatchRecordService.cs`）

```csharp
public interface IPanVideoWatchRecordService
{
    Task<PanVideoWatchRecord> UpsertAsync(string userId, string cloudAccountId, PanVideoProgressDto dto);
    Task<PanVideoWatchRecord?> GetAsync(string userId, string cloudAccountId, string pathKey);
    Task<List<PanVideoWatchRecord>> GetRecentAsync(string userId, int count = 50);   // 按 LastViewedAt 倒序
    Task<bool> SetPositionAsync(string userId, Guid recordId, int positionSec);      // 手动修正进度
    Task<bool> MarkUnavailableAsync(string userId, Guid recordId, string reason);    // 文件失效 → 停用提醒
    Task<bool> DeleteAsync(string userId, Guid recordId);
}
```

统一约定：`userId` 由 WinApp 侧 `IUserSessionService.GetCurrentUserId()` 取到后传入 Core 服务（不在 Core 内部读环境变量）；`cloudAccountId` 来自网盘登录态（`BaiduPanTokenHelper` / 当前 token 的 uid）。

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
到点判定：Enabled && Status=Ok && NextTriggerAt <= now && 处于允许触发时段(QuietHours 之外)
          若到点但不在允许时段 → 顺延到当日 QuietHoursEnd 或次日 QuietHoursEnd（不半夜弹窗）
批量汇总：每次检查最多产出 **1 条汇总提醒**（「有 N 个视频该继续了」），不逐个弹窗
每日上限：默认 3 条/天（PanVideoOptions.DailyReminderLimit），超出顺延次日
排序取 Top：CompletedRatio 降序 → LastViewedAt 升序（快完成、最久未看的优先）
触发后：LastTriggeredAt = now；按 RepeatType 递推 NextTriggerAt
  Interval（默认）→ NextTriggerAt = max(LastViewedAt + IntervalHours, 上次触发时间 + 最小间隔)
                    IntervalHours 默认 48；若期间有新的观看记录（LastViewedAt 变新）则自动重置
  Daily    → 次日 TimeOfDay（若已过则再 +1 天）
  Weekly   → RepeatDays 中下一个命中日的 TimeOfDay
  Once     → Enabled = false
去重：同一 reminder 两次触发至少间隔 30 分钟；同一 PathKey 24h 内最多提示 1 次
抑制：任一标签页正在播放网盘视频时不弹提醒（该条顺延 +2h），避免打断学习
降级：同一提醒连续 3 次未处理 → 降为「每周汇总一次」，直到用户主动处理
完结：CompletedRatio >= 1 的记录自动停用其 Interval 提醒（不再催看）
失效：Status=Unavailable 或前置校验失败 → 不弹提醒并停用
```

**提醒风暴是本方案最大的体验风险**：默认 Interval 48h 会让多条记录同时到期，因此必须有「汇总 + 每日上限 + 排序 + 抑制」四道闸，而不是只做单条去重。

**默认节奏已确认：`Interval` = 距上次观看 48h 未继续。** 判定语义是「**记录驱动的催看**」：只有当 `LastViewedAt + 48h ≤ now`（即 48 小时内没有新进度）才提醒；用户一旦打开继续看，`LastViewedAt` 更新，`NextTriggerAt` 自动后移 48h，因此不需要用户手动维护排期。

`Daily` / `Weekly` / `Once` 作为可选的显式模式保留（用户手动选择时才用），不作为默认。

### 5.3 到点后的用户交互（WinApp）

- 默认 `AutoOpen = false`：托盘气泡 / 主窗口顶部提示条「该继续《课程名》了 · 上次看到 00:32:10」+「打开」「稍后」按钮；多条到期时为**一条汇总**：`PanVideoDueListForm`（列出 N 条，可逐条打开/全部稍后）；
- 点「打开」→ 打开网盘播放页（见 5.4、5.6）；「稍后」→ `SnoozeAsync(+2h)`；「今天不再提醒」→ 写 `LastTriggeredAt` 并跳过当天排期；
- **打开幂等**：同一 `(CloudAccountId, PathKey)` 已存在标签页时，定位并复用该标签页（`WebView2BrowserForm` 已有 `TabState`/`_tabCounter`），不重复开新标签；
- **窗口宽度保障**：打开前若窗口宽度 < `PanVideoOptions.MinWindowWidth`（默认 1200px），先加宽到该值，避免右侧笔记面板被挤掉；
- `AutoOpen = true` 时：仅当程序在前台、且当前没有打开的播放页时自动导航，否则降级为提示，避免打断用户；
- **任何自动打开都不进入全屏**（原因见第二节假设 5）。

### 5.4 打开目标 URL 的构造（`Core/Services/PanVideo/PanVideoUrlBuilder.cs`，纯函数便于测试）

```csharp
public static class PanVideoUrlBuilder
{
    /// <param name="resumeSec">续播秒数，-1 表示不带</param>
    public static string BuildPlayUrl(string netdiskPath, int resumeSec = -1)
    {
        var encoded = Uri.EscapeDataString(NormalizePath(netdiskPath));
        // 注意：只构造「网页播放页」URL，不附加任何全屏参数（笔记面板需非全屏态）
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

### 5.6 网页播放态与笔记面板保障（因「笔记仅在非全屏网页态右侧可见」）

打开流程固定为 **「打开播放页 → 定位进度 → 确保右侧笔记面板处于展开态 → 不进入全屏」**：

```csharp
// WebView2BrowserForm 新增（内部方法）
private async Task<bool> EnsureNotePanelVisibleAsync(WebView2 webView)
{
    // 1) 若已处于全屏态，先退出（不主动进全屏）
    // 2) 依次尝试「笔记」页签/按钮的点击（选择器由 P0 spike 实测确定，集中在此一处）
    // 3) 返回是否成功展开；失败不报错，仅在标题栏/状态条提示“请手动展开右侧笔记”
}
```

实施要点：

- **绝不调用任何全屏 API / 不注入全屏脚本**；用户自己点全屏属已知限制（此时笔记面板不可用，退出全屏即恢复）；
- 笔记面板的**选择器与点击目标集中在 `PanVideoOptions.NoteTabSelector`**（方法内只做调用，不硬编码），网盘改版时可在设置里改、无需改码；与 4.2 的采集脚本分离，互不影响；
- 打开后把 `NotePanelOpened` 结果写入记录，便于统计「是否真正用上了笔记」；
- 打开动作必须投递到 UI 线程且**避免在 WebView2 回调延续内**启动模态窗体（现有 `WebView2BrowserForm` 已在 `btnOpenNetdisk` 处踩过 `0x80000003` 崩溃，遵循同样做法：`BeginInvoke`）。

### 5.7 提醒前置校验与失效降级

- 触发前校验文件是否仍可用：有开放平台 token 时用 `BaiduPanApiClient.GetFileListAsync` 的存在性检查（结果缓存 10 分钟，避免每次提醒都打 API）；无 token 时跳过 API 校验；
- 打开后若页面提示「文件不存在 / 无权访问」→ `MarkUnavailableAsync` + 停用该记录的提醒，并提示一次「该视频已不可访问，已暂停提醒」；
- 不删除记录，保留历史统计；文件恢复可用（同一 `PathKey` 再次获得有效进度）时自动恢复 `Status=Ok`；
- 文件重命名/移动：优先用 `FsId` 匹配并迁移 `PathKey`（需 API token）；无 token 时按「新记录」处理，旧记录标 `Unavailable`。

### 5.8 可配置项与诊断（抗网盘改版）

`LearningAssistant.Core/Models/PanVideo/PanVideoOptions.cs`（绑定到设置界面，默认值随 P0 实测结果填入）：

| 配置 | 默认 | 说明 |
|------|------|------|
| `VideoSelector` | `video` | 播放页 video 元素选择器 |
| `NoteTabSelector` | P0 实测后填入 | 右侧「笔记」页签/按钮选择器 |
| `PollIntervalSec` | 15 | 宿主主动拉取间隔 |
| `DailyReminderLimit` | 3 | 每日提醒条数上限 |
| `QuietHoursStart` / `QuietHoursEnd` | 23:00 / 08:00 | 允许触发的时段 |
| `SessionRetentionDays` | 180 | `PanVideoWatchSessions` 保留天数 |
| `MinWindowWidth` | 1200 | 打开前保证的最小窗口宽度 |

诊断：`PanVideoRecordsForm` 增加「诊断」入口（轻量对话框即可），显示最近一次 Pull 时间、`video` 是否命中、笔记面板是否展开成功、最近 20 条采集/打开日志（复用现有 `ILogger`）。网盘改版后用户/开发者可据此快速定位是「选择器失效」还是「登录态失效」。

---

## 六、文件变更清单

### 6.1 新增（Core）

| 文件 | 内容 |
|------|------|
| `LearningAssistant.Core/Models/PanVideo/PanVideoWatchModels.cs` | `PanVideoWatchRecord` / `PanVideoWatchSession` / `PanVideoNoteReminder` / `PanVideoProgressDto` / `PanVideoNoteDueEventArgs` |
| `LearningAssistant.Core/Models/PanVideo/PanVideoOptions.cs` | 5.8 节可配置项（选择器 / 轮询间隔 / 每日上限 / 保留天数 / 最小窗口宽度） |
| `LearningAssistant.Core/Services/PanVideo/IPanVideoWatchRecordService.cs` | 4.4 节接口 |
| `LearningAssistant.Core/Services/PanVideo/PanVideoWatchRecordService.cs` | Upsert / 会话合并 / 区间并集 / 账号维度隔离 / 失效标记 / 重试（复用 `AppDbContext` 既有并发重试） |
| `LearningAssistant.Core/Services/PanVideo/PanVideoCoverageCalculator.cs` | 播放区间「并集合并」纯函数（回看/拖拽去重，单测重点） |
| `LearningAssistant.Core/Services/PanVideo/IPanVideoNoteScheduler.cs` | 5.1 节接口 |
| `LearningAssistant.Core/Services/PanVideo/PanVideoNoteScheduler.cs` | 每分钟检查 + 批量汇总 + 每日上限 + 排序 + 抑制 + 降级 + 递推 + 事件 |
| `LearningAssistant.Core/Services/PanVideo/PanVideoSessionCleanupService.cs` | 按 `SessionRetentionDays` 清理明细（随调度器每分钟检查时低频执行） |
| `LearningAssistant.Core/Services/PanVideo/PanVideoRepeatRuleCalculator.cs` | `NextTriggerAt` 计算（纯函数，单测重点） |
| `LearningAssistant.Core/Services/PanVideo/PanVideoUrlBuilder.cs` | 5.4 节 URL 构造 + 路径规范化 |

### 6.2 新增（WinApp）

| 文件 | 内容 |
|------|------|
| `LearningAssistant.WinApp/Forms/PanVideo/PanVideoRecordsForm.cs(.Designer.cs)` | 观看记录列表：进度条、有效观看时长、观看次数、最后观看、`继续观看`、`记录当前时间点`、`设置提醒`、`删除`、「诊断」入口 |
| `LearningAssistant.WinApp/Forms/PanVideo/PanVideoReminderDialog.cs` | 提醒规则编辑：默认 `Interval`（48 小时，可改 1/6/12/24/48/72h）+ 可选 Daily / Weekly / Once + 自动打开开关 + 安静时段说明 |
| `LearningAssistant.WinApp/Forms/PanVideo/PanVideoDueListForm.cs` | 多条到期时的**汇总提醒**窗口：列出 N 条，可逐条打开 / 全部稍后 / 今天不再提醒 |

> UI 遵循既有约定：WinForms 控件先 `new` 再赋值、`Controls.Add` 前后顺序正确、配色沿用紫色 `#8B5CF6` 主题与统一扁平按钮；`Pan*` 目录命名与 `Forms/BaiduPan/`、`Forms/UserControls/Pan/` 保持一致风格。

### 6.3 修改（少量、可控）

| 文件 | 改动 |
|------|------|
| `LearningAssistant.Core/Data/Database/Entities.cs` | 追加 3 个实体（3.2 节） |
| `LearningAssistant.Core/Data/Database/AppDbContext.cs` | 3 个 `DbSet` + `EnsureCreated` 建表 + `RepairSchema` 调用 + `RepairPanVideoWatchTables()` + 索引 |
| `LearningAssistant.WinApp/Common/ServiceCollectionExtensions.cs` | 注册 `IPanVideoWatchRecordService`（Singleton）、`IPanVideoNoteScheduler`（Singleton） |
| `LearningAssistant.WinApp/Program.cs` | 启动 `PanVideoNoteScheduler.Start()`、退出 `DisposeService`（与现有 `reminderService` 同处，约 L80/L131） |
| `LearningAssistant.WinApp/Forms/Web/WebView2BrowserForm.cs` | ① 宿主侧 `Timer(15s)` + `SourceChanged` 主动 Pull 进度（4.2，替代页面定时器）；② 域名白名单（仅 `https://pan.baidu.com/`）；③ 新增 public `OpenPanVideoAsync(path, resumeSec)`（复用 `NavigateToUrl`，只走网页播放态、复用已有标签页、必要时加宽窗口）；④ 新增 `EnsureNotePanelVisibleAsync`（5.6，选择器取自 `PanVideoOptions`）；⑤ 标签切换/窗口关闭/程序退出前同步补拉一次 |
| 主窗口 / 托盘入口 | 增加「网盘视频记录」菜单项（打开 `PanVideoRecordsForm`）与提醒提示条/气泡 |

### 6.4 新增（测试）

| 文件 | 用例 |
|------|------|
| `LearningAssistant.Core.Tests/PanVideo/PanVideoRepeatRuleCalculatorTests.cs` | Interval 默认 48h 且基于 `LastViewedAt`、期间有新观看自动后移、安静时段顺延、去重 24h、每日上限、批量汇总只产 1 条、Top N 排序、连续 3 次未处理降级、Daily 跨日、Weekly 多选日、Once 关闭、完成记录停用提醒、DST/午夜边界 |
| `LearningAssistant.Core.Tests/PanVideo/PanVideoCoverageCalculatorTests.cs` | 顺序播放、回看同一段不重复计、拖拽跳过只计新覆盖、乱序区间合并、边界相邻区间、单次上限保护 |
| `LearningAssistant.Core.Tests/PanVideo/PanVideoWatchRecordServiceTests.cs` | 首次插入/重复上报合并、账号隔离（同 PathKey 不同 CloudAccountId 不覆盖）、完成后 `CompletedRatio=1`、并发同 PathKey 串行、失效标记并停用提醒 |
| `LearningAssistant.Core.Tests/PanVideo/PanVideoUrlBuilderTests.cs` | 中文路径编码、`resumeSec<=0` 不带 t、路径规范化 |

---

## 七、分阶段交付（每阶段可独立验收）

| 阶段 | 内容 | 验收 |
|------|------|------|
| **P0（spike，0.5 天）** | 验证第二节 5 个假设（重点：真实播放页 URL、`t=` 是否生效、右侧笔记面板是否需要点击展开、全屏时笔记是否消失）；产出结论写回本文档；不做产品代码 | 记录真实播放页 URL 与笔记面板展开方式 |
| **P1（记录存储，1.5 天）** | 3 张表 + `IPanVideoWatchRecordService` + `PanVideoRecordsForm`；先只支持**手动/一键登记**进度 | 重启后记录仍在，列表可继续观看（打开播放页） |
| **P2（自动采集，1 天）** | 宿主主动 Pull（`Timer(15s)` + `SourceChanged`）+ 切换/关闭补拉 + 区间并集 + 账号维度隔离 + 域名白名单 | 播放 60s 且不拖动 → `EffectiveWatchedSec` +60±5s；回看同一段不增加；从 10% 拖到 80% 只计新增覆盖区间 |
| **P3（定期提醒，1.5 天）** | `PanVideoNoteScheduler`（默认 Interval 48h、汇总 + 每日上限 + 排序 + 抑制 + 降级）+ 规则对话框 + 托盘/主窗口入口 + `EnsureNotePanelVisibleAsync` + 失效前置校验 + `Snooze` | 造 5 条同时到期 → 只弹 1 条汇总；当日第 4 条不再弹；打开即进入网页播放态、定位续播、右侧笔记面板可用；标记失效的记录不再提醒 |
| **P4（可选）** | 记录统计（按课程/目录聚合，接入 `ILearningStatsAggregator`）、导出 Markdown 观看清单 | 统计页可见 |

---

## 八、风险与降级

| 风险 | 说明 | 对策 |
|------|------|------|
| 网盘网页 DOM/URL 变更 | 选择器与 `path` 参数解析失效 | 选择器/URL 模板进 `PanVideoOptions` 可配置 + 诊断面板定位；失败静默降级为手动登记并写日志 |
| 登录态/会员限制 | 视频笔记可能是会员功能 | 检测未登录/无权限时提示，不阻断记录功能 |
| 切换网盘账号串数据 | 同路径覆盖彼此进度 | 唯一键含 `CloudAccountId`，上报必须携带账号标识 |
| 多标签页同视频并发 | 进度互相覆盖 | 按 `(CloudAccountId, PathKey)` 串行 + 会话时间戳合并（`LastViewedAt` 大者胜） |
| 拖动/回看导致时长虚高 | 学习时长统计失真 | 播放区间并集计算 `EffectiveWatchedSec`（回看去重），保留单次增量上限 |
| 提醒风暴 | 多条记录同时到期 | 汇总为一条 + 每日上限 + Top N 排序 + 播放中抑制 + 连续 3 次未处理降级 |
| 文件失效/被移动 | 提醒打开 404 | 触发前可用性校验（有 token 时）+ 打开后失败兜底 → `Status=Unavailable` 并停用提醒，保留历史 |
| 明细表膨胀 | `PanVideoWatchSessions` 长期增长 | `SessionRetentionDays`（默认 180）清理 + 统计走聚合 |
| 笔记面板被全屏/窄窗口挤掉 | 笔记仅非全屏网页态右侧可见 | 不主动进全屏；打开前自动加宽到 `MinWindowWidth`；展开失败提示手动操作 |
| 记录跨设备不一致 | 网盘只存文件，不存进度 | 文档明示「记录仅本机有效」，可选导出/导入 |

---

## 九、已确认的决策（2026-11-01）

| # | 决策点 | 结论 | 对方案的落地影响 |
|---|--------|------|------------------|
| 1 | 观看发生场景 | **仅「本程序 WebView2 内播放」自动采集**；网盘客户端/手机/其他浏览器 → 手动登记或「记录当前时间点」 | P2 采集范围只挂在 `WebView2BrowserForm` 的播放页；不做客户端缓存读取 |
| 2 | 「视频笔记」入口 | **网页播放（非全屏）时页面右侧的笔记面板**，非独立入口、非独立页面 | 打开必须保持网页态；新增 5.6 `EnsureNotePanelVisibleAsync` 与假设 5；`PanVideoUrlBuilder` 不带全屏参数 |
| 3 | 默认提醒节奏 | **`Interval` = 距上次观看 48h 未继续** | 5.2 以 `LastViewedAt + 48h` 为准；有新进度自动后移；`Daily/Weekly/Once` 仅作为可选模式保留 |

**实施顺序锁定**：P0 spike（只验证不改码）→ P1 记录存储 → P2 自动采集 → P3 定期提醒 → P4 可选统计。

---

## 十、方案评审增补（2026-11-04）

本轮评审共并入 11 项，按影响排序：

| # | 问题 | 处理 | 落点 |
|---|------|------|------|
| 1 | 提醒风暴：多条记录同时到期 | 汇总为一条 + 每日上限 3 + Top N 排序 + 播放中抑制 + 连续 3 次未处理降级 | 5.2 / 5.3 / `PanVideoDueListForm` |
| 2 | 页面定时器采集不可靠（后台节流、`beforeunload` 不执行、hash 路由不触发 `NavigationCompleted`） | 改宿主主动 Pull（`Timer(15s)` + `SourceChanged`）+ 切换/关闭补拉 | 4.2 |
| 3 | 切换网盘账号串数据 | 唯一键与所有查询增加 `CloudAccountId` | 3.1 / 3.2 / 4.4 |
| 4 | 文件失效后仍提醒打开 404 | `Status=Unavailable` + 前置校验 + 打开后兜底 + 停用提醒 | 5.7 |
| 5 | 学习时长统计失真（回看/拖拽） | 播放区间并集算法 → `EffectiveWatchedSec` | 4.3 / `PanVideoCoverageCalculator` |
| 6 | `PanVideoWatchSessions` 长期膨胀 | `SessionRetentionDays`（默认 180）+ 统计走聚合 | 5.8 / `PanVideoSessionCleanupService` |
| 7 | 网盘改版后选择器失效无从排查 | 选择器/URL 模板进 `PanVideoOptions` 可配置 + 诊断面板 + `ILogger` | 5.8 |
| 8 | 重复「打开」开多个标签；窄窗口挤掉笔记面板 | 复用同 `(账号,PathKey)` 标签页；打开前加宽到 `MinWindowWidth` | 5.3 |
| 9 | 与既有网盘方案文档可能两套实现 | `docs/百度网盘深度融合方案.md` 中「播放进度记录/视频笔记」章节以本文档为唯一实现，实施时在旧文档标注「由本 spec 替代」 | 第十章 |
| 10 | 验收标准不可判定 | 改为「播放 60s 不拖动 → +60±5s」「5 条同时到期只弹 1 条」等可观测断言 | 七 |
| 11 | 时间逻辑不可测 | 规则计算器用参数传入 `now`（项目无 `IClock`），不依赖系统时间 | `PanVideoRepeatRuleCalculator` |

**未采纳（记录但暂不做）**：观看记录的云同步（网盘无进度 API）、客户端/手机端观看的自动采集（不稳定）、按遗忘曲线的智能复习排序（P4 之后再评估）。

**本轮未改动**：P0 spike 的 5 个假设、Interval 48h 默认节奏、网页非全屏 + 右侧笔记面板的核心约束。

