# 百度网盘 AI 分析整理功能 — 设计方案（优化版）

## 一、功能概述

在 `WebView2BrowserForm` 中浏览百度网盘时，用户点击「AI 分析整理」按钮，系统自动提取当前网盘路径，递归获取目录文件列表，本地预计算重复文件和统计数据，再将精简后的数据传给 AI 分析，最后在独立窗体中展示可操作的整理建议（删除/移动/重命名），用户勾选后可批量执行。

**与初版方案的主要差异：**

| 优化点 | 初版 | 优化版 |
|--------|------|--------|
| 路径提取 | 仅 URL 解析 | JS 注入优先 + URL 解析兜底 + 手动输入 |
| Token 管理 | 笼统描述 | 明确刷新流程 + 线程安全 |
| 模型设计 | 缺 FsId/目标路径 | 补全执行所需字段 |
| AI 响应解析 | 假设返回纯 JSON | 多策略容错（JSON/markdown/纯文本） |
| 操作执行 | "逐个执行" | 分批分组 + 进度 + 错误汇总 |
| 重复检测 | 交给 AI | 本地预计算，减少 AI 负担 |
| 架构 | 单一 Service | 拆分 PromptBuilder + ResultParser |
| 进度通知 | Action\<string\> | IProgress\<PanAnalysisProgress\> |
| 主题适配 | 未提及 | 实现 IThemeable |
| 异步操作 | 未处理 | 使用 async=0 同步模式简化 |

## 二、现有代码分析

### 2.1 Baidu 目录（`LearningAssistant/Baidu/`）

| 文件 | 职责 | 关键方法/字段 |
|------|------|---------------|
| `BaiduPanApiClient.cs` | API 客户端（11 个接口） | `GetFileListRecursiveAsync(dir)` 递归列表、`ManageFileAsync(opera, fileList, async, onDup)` 文件管理 |
| `BaiduPanSmartTool.cs` | 批量重命名/统计工具 | `GetFolderStatisticsAsync()`、`GetDetailedStatisticsAsync()` |
| `BaiduPanAuthCodeManager.cs` | OAuth 授权 + Token 刷新 | `RefreshTokenAsync(refreshToken)` |
| `BaiduPanModel.cs` | 数据模型 | `BaseFileInfo`(FsId/Path/Size/Category/Md5/IsDir)、`FileManagerFileItem`(Path/Dest/NewName)、`FileOperation` 枚举 |

**关键发现：**
- `SendGetRequestAsync` 已内置限流（7 秒间隔）和重试（3 次），无需额外 AdaptiveRateLimiter
- `ManageFileAsync` 使用 `Path` 标识文件（非 FsId），Delete 只需 Path，Move 需 Path+Dest，Rename 需 Path+NewName
- `ManageFileAsync` 默认 `async=1`（异步），可传 `async=0` 改为同步模式，避免需要轮询任务状态
- `BaseFileInfo.Md5` 可用于本地重复文件检测

### 2.2 Services/Cloud 目录

| 文件 | 职责 |
|------|------|
| `ICloudStorageService.cs` | 云存储统一接口（认证、上传下载、列表、删除） |
| `BaiduNetdiskService.cs` | 实现，管理 Token 生命周期，`CloudFileInfo` 只有 5 个字段（不含 Category/Md5） |

### 2.3 AI 服务

| 接口 | 关键方法 | 适用场景 |
|------|----------|----------|
| `IAIService` | `AskQuestionAsync(question, context)` | 通用问答，context 放文件数据，question 放分析指令 |
| `IAIServiceFactory` | `Create(config)` | 可切换不同 AI 模型 |

### 2.4 配置与 DI

- `CloudStorageConfig`：`BaiduClientId/Secret/AccessToken/RefreshToken/TokenExpireTime`，已注册 Singleton
- `WebView2BrowserForm` 实现 `IThemeable`，已有深/浅色主题支持
- 已有 `isNetdiskPage` 判断逻辑，可复用控制按钮可见性

## 三、架构设计

### 3.1 整体架构

```
WebView2BrowserForm
  │  btnAiAnalyze_Click
  │  ├── 1. 提取网盘路径（JS注入 / URL解析 / 手动输入）
  │  └── 2. 打开 BaiduPanAnalysisForm
  │
  ▼
BaiduPanAnalysisForm
  │  用户点击「开始分析」
  │  ├── IProgress<PanAnalysisProgress> 进度回调
  │  └── CancellationToken 可取消
  │
  ▼
IBaiduPanAnalysisService（编排者）
  │
  ├──① GetDirectorySnapshotAsync(path, depth, progress, ct)
  │     ├── EnsureValidTokenAsync()  ← Token 检查/刷新
  │     ├── BaiduPanApiClient.GetFileListRecursiveAsync(path)
  │     ├── 本地预计算：重复文件检测（Name+Size / Md5）
  │     ├── 本地预计算：统计数据（类型分布、大小分布）
  │     └── → PanDirectorySnapshot
  │
  ├──② AnalyzeAsync(snapshot, ct)
  │     ├── PanAnalysisPromptBuilder.Build(snapshot)  ← 构建 Prompt
  │     │     └── 数据量控制：≤200 完整列表 / ≤1000 精简 / >1000 摘要+异常
  │     ├── IAIService.AskQuestionAsync(question, context)
  │     ├── PanAnalysisResultParser.Parse(aiResponse)  ← 解析响应
  │     │     └── 多策略：JSON > markdown代码块 > 纯文本兜底
  │     └── → PanAnalysisResult
  │
  └──③ ExecuteRecommendationsAsync(selected, progress, ct)
        ├── 按类型分组：Delete 批 > Move 批 > Rename 批
        ├── 每批 ≤100 个文件（API 限制）
        ├── BaiduPanApiClient.ManageFileAsync(async: 0)  同步模式
        ├── 逐批执行，失败不中断，记录结果
        └── → PanExecutionReport
```

### 3.2 接口定义

```csharp
// ── Services/Cloud/IBaiduPanAnalysisService.cs ──

public interface IBaiduPanAnalysisService
{
    bool IsAvailable { get; }

    /// <summary>获取目录快照（文件列表 + 统计 + 重复检测）</summary>
    Task<PanDirectorySnapshot> GetDirectorySnapshotAsync(
        string dirPath,
        int maxDepth = 2,
        IProgress<PanAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>AI 分析目录快照，返回整理建议</summary>
    Task<PanAnalysisResult> AnalyzeAsync(
        PanDirectorySnapshot snapshot,
        IProgress<PanAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>执行用户选中的推荐操作</summary>
    Task<PanExecutionReport> ExecuteRecommendationsAsync(
        List<PanRecommendation> selected,
        IProgress<PanAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
```

### 3.3 数据模型

```csharp
// ── Services/Cloud/PanAnalysisModels.cs ──

/// <summary>进度通知</summary>
public class PanAnalysisProgress
{
    public PanAnalysisPhase Phase { get; set; }   // Fetching / Analyzing / Executing
    public string Message { get; set; } = "";
    public int Current { get; set; }
    public int Total { get; set; }
}

public enum PanAnalysisPhase { Fetching, PreComputing, Analyzing, Executing, Completed }

/// <summary>目录快照</summary>
public class PanDirectorySnapshot
{
    public string DirectoryPath { get; set; } = "";
    public DateTime SnapshotTime { get; set; }
    public List<PanFileInfo> Files { get; set; } = new();
    public PanStatistics Statistics { get; set; } = new();
    public List<PanDuplicateGroup> Duplicates { get; set; } = new();  // 本地预计算
}

/// <summary>文件信息（内部完整版，含执行所需字段）</summary>
public class PanFileInfo
{
    public long FsId { get; set; }              // 云端ID（备用）
    public string Path { get; set; } = "";       // 完整路径（执行操作用）
    public string Name { get; set; } = "";       // 文件名
    public string RelativePath { get; set; } = ""; // 相对根目录
    public long SizeBytes { get; set; }
    public string Extension { get; set; } = "";   // 扩展名（.pdf/.mp4/...）
    public int Category { get; set; }             // 1视频 2音频 3图片 4文档 5应用 6其他 7种子
    public DateTime ModifiedTime { get; set; }
    public bool IsFolder { get; set; }
    public string? Md5 { get; set; }             // 用于重复检测
}

/// <summary>统计信息</summary>
public class PanStatistics
{
    public int TotalFileCount { get; set; }
    public int TotalFolderCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public Dictionary<string, int> CountByExtension { get; set; } = new();
    public Dictionary<string, long> SizeByExtension { get; set; } = new();
    public int TopLargestFileCount { get; set; }   // 大文件数量(>100MB)
}

/// <summary>重复文件组（本地预计算）</summary>
public class PanDuplicateGroup
{
    public string GroupKey { get; set; } = "";    // 文件名 or Md5
    public long SizeBytes { get; set; }
    public List<string> Paths { get; set; } = new();
}

/// <summary>AI 分析结果</summary>
public class PanAnalysisResult
{
    public string Summary { get; set; } = "";
    public List<PanRecommendation> Recommendations { get; set; } = new();
    public string RawAiResponse { get; set; } = "";
    public bool ParseSuccess { get; set; }        // JSON 解析是否成功
}

/// <summary>推荐操作</summary>
public class PanRecommendation
{
    public RecommendationType Type { get; set; }
    public string TargetPath { get; set; } = "";       // 源文件/文件夹路径
    public string? DestinationPath { get; set; }        // Move: 目标目录; Rename: 新名称
    public string Reason { get; set; } = "";
    public Priority Priority { get; set; }
    public bool IsSelected { get; set; }                // UI 绑定用
}

public enum RecommendationType { Delete, Move, Rename, MergeFolder, Keep }
public enum Priority { High, Medium, Low }

/// <summary>执行报告</summary>
public class PanExecutionReport
{
    public int TotalRequested { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public List<PanExecutionResult> Results { get; set; } = new();
}

public class PanExecutionResult
{
    public PanRecommendation Recommendation { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### 3.4 内部辅助类

```csharp
// ── PanAnalysisPromptBuilder（内部类，构建 AI Prompt）──
// 职责：根据 snapshot 数据量和策略，构建最优 prompt
// - ≤200 文件：完整 Markdown 表格
// - 201~1000：精简列表（名称+大小+类型，省略路径和日期）
// - >1000：摘要 + Top 50 大文件 + 全部重复组 + 异常文件

// ── PanAnalysisResultParser（内部类，解析 AI 响应）──
// 职责：多策略解析 AI 返回内容
// 策略1：直接 JsonConvert.DeserializeObject
// 策略2：提取 ```json ... ``` 代码块后解析
// 策略3：正则提取最外层 { } 后解析
// 策略4：全部失败 → 将原文放入 Summary，Recommendations 为空，ParseSuccess=false
```

## 四、Token 管理

### 4.1 Token 刷新流程

```
EnsureValidTokenAsync()
  │
  ├── 读取 CloudStorageConfig.BaiduAccessToken / BaiduTokenExpireTime
  │
  ├── Token 未过期（ExpireTime > DateTime.Now + 5分钟）？
  │     └── YES → 直接返回当前 AccessToken
  │
  ├── Token 过期，有 RefreshToken？
  │     ├── YES → 调用 BaiduPanAuthCodeManager.RefreshTokenAsync(refreshToken)
  │     │         ├── 刷新成功 → 更新 CloudStorageConfig（AccessToken/ExpireTime）
  │     │         │              持久化保存（调用 IDataPersistenceService）
  │     │         │              返回新 AccessToken
  │     │         └── 刷新失败 → 抛出 PanAuthException，引导用户重新授权
  │     └── NO  → 抛出 PanAuthException("未授权，请先完成百度网盘授权")
  │
  └── 使用 SemaphoreSlim 保证线程安全（多个请求不会同时刷新）
```

### 4.2 BaiduPanApiClient 生命周期

- **不缓存** `BaiduPanApiClient` 实例（它内部创建 `HttpClient`，缓存可能导致 Socket 占用）
- 每次 `GetDirectorySnapshotAsync` / `ExecuteRecommendationsAsync` 创建新实例，用完 `Dispose`
- Token 变化时自动使用新 Token

## 五、路径提取（三层降级策略）

### 5.1 策略一：JavaScript 注入（优先）

百度网盘使用 SPA 路由，URL 的 hash fragment 随用户操作动态变化。通过 `ExecuteScriptAsync` 直接从页面获取当前路径最可靠：

```csharp
private async Task<string?> ExtractPanPathViaJsAsync()
{
    if (CurrentWebView?.CoreWebView2 == null) return null;

    // 从 URL hash 中提取 path 参数
    string js = @"
        (function() {
            var hash = window.location.hash || '';
            var match = hash.match(/path=([^&]*)/);
            if (match) return decodeURIComponent(match[1]);
            var search = window.location.search || '';
            match = search.match(/path=([^&]*)/);
            if (match) return decodeURIComponent(match[1]);
            return '';
        })()";

    var result = await CurrentWebView.CoreWebView2.ExecuteScriptAsync(js);
    // ExecuteScriptAsync 返回 JSON 字符串（带引号），需反序列化
    var path = JsonConvert.DeserializeObject<string>(result);
    return string.IsNullOrEmpty(path) ? null : path;
}
```

### 5.2 策略二：URL 解析（兜底）

```csharp
private static string? ExtractPanPathFromUrl(string url)
{
    // 处理 hash fragment 中的 path（SPA 路由）
    if (url.Contains("#") && url.Contains("path="))
    {
        var hashPart = url.Substring(url.IndexOf('#'));
        return ExtractPathFromQuery(hashPart);
    }
    // 处理普通 query string
    if (url.Contains("path="))
    {
        return ExtractPathFromQuery(url);
    }
    return null;
}

private static string? ExtractPathFromQuery(string queryString)
{
    var match = Regex.Match(queryString, @"path=([^&]*)");
    if (match.Success)
        return Uri.UnescapeDataString(match.Groups[1].Value);
    return null;
}
```

### 5.3 策略三：手动输入

```csharp
// 前两种策略都失败时，弹出输入框
string panPath = InputDialog.Show("请输入网盘目录路径", "例如：/学习资料/英语", "/");
```

### 5.4 按钮点击完整逻辑

```csharp
private async void btnAiAnalyze_Click(object sender, EventArgs e)
{
    var url = CurrentWebView?.Source?.ToString();
    if (string.IsNullOrEmpty(url) || !url.StartsWith(Urls.BaiduNetdisk))
    {
        MessageBox.Show("请先浏览到百度网盘文件页面", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return;
    }

    // 三层降级提取路径
    string panPath = await ExtractPanPathViaJsAsync()
                  ?? ExtractPanPathFromUrl(url)
                  ?? PromptManualPathInput();

    if (string.IsNullOrEmpty(panPath))
        return;

    var form = new BaiduPanAnalysisForm(_analysisService, panPath, _themeService);
    form.Show(this);
}
```

## 六、AI Prompt 设计

### 6.1 System Prompt（含 few-shot 示例）

```
你是网盘文件整理专家。分析用户提供的文件列表，给出结构化的整理建议。

## 分析维度
1. 无意义文件：.DS_Store / Thumbs.db / desktop.ini / ~$开头临时文件 / .tmp / .bak / 0字节文件 / 空文件夹
2. 重复文件：已在"重复文件组"中列出，建议保留最新版本，删除其余
3. 分类混乱：文件类型与目录语义不匹配（如 .mp4 在 /文档/ 目录下）
4. 命名问题：乱码文件名 / "新建文件夹" / 过长文件名(>100字符) / 无意义随机字符串
5. 空间优化：>500MB 的大文件标记，建议确认是否需要
6. 目录结构：同级文件>100个建议拆分子目录，层级>5建议扁平化

## 输出格式（严格 JSON）
{
  "summary": "2-3句话总体评估",
  "recommendations": [
    {
      "type": "Delete",
      "targetPath": "/学习资料/英语/.DS_Store",
      "destinationPath": null,
      "reason": "macOS 系统文件，无实际用途",
      "priority": "High"
    },
    {
      "type": "Move",
      "targetPath": "/学习资料/英语/课程录像.mp4",
      "destinationPath": "/视频/课程/",
      "reason": "视频文件放在文档目录中",
      "priority": "Medium"
    },
    {
      "type": "Rename",
      "targetPath": "/学习资料/英语/新建文件夹",
      "destinationPath": "课程资料",
      "reason": "文件夹名无意义",
      "priority": "Low"
    }
  ]
}

## 注意
- type=Delete 时 destinationPath 为 null
- type=Move 时 destinationPath 为目标目录（以 / 结尾）
- type=Rename 时 destinationPath 为新名称（不含路径）
- 只返回 JSON，不要附加其他文字
```

### 6.2 User Prompt 模板（自适应数据量）

```
分析以下百度网盘目录：

目录：{DirectoryPath}
文件数：{TotalFileCount} | 文件夹数：{TotalFolderCount} | 总大小：{TotalSizeFormatted}

## 文件类型分布
pdf: 23个 (45.2 MB), mp4: 5个 (1.2 GB), docx: 10个 (12 MB), 其他: 8个 (3 MB)

## 本地检测到的重复文件
[1] "课件.pdf" (3个副本, 共 9 MB)
    /学习资料/英语/课件.pdf
    /学习资料/英语/backup/课件.pdf
    /学习资料/英语/旧文件/课件.pdf

## 文件列表
| 文件名 | 大小 | 类型 | 修改时间 |
|--------|------|------|----------|
| 英语单词.pdf | 2.3 MB | pdf | 2024-01-15 |
| .DS_Store | 6 KB | 其他 | 2024-03-20 |
| 新建文件夹 | - | 文件夹 | 2024-02-10 |
| ...（最多 {MaxFiles} 行）|
```

### 6.3 数据量控制策略

| 文件数量 | Prompt 策略 | 预估 Token |
|----------|------------|-----------|
| ≤ 200 | 完整列表（名称+路径+大小+类型+日期） | ~4K |
| 201~1000 | 精简列表（名称+大小+类型，省略路径和日期） | ~6K |
| 1001~5000 | 统计摘要 + Top50 大文件 + 全部重复组 + 可疑文件 | ~3K |
| > 5000 | 仅二级目录统计 + 重复组 + 可疑文件 | ~2K |

> **可疑文件定义**：系统文件名、0 字节文件、"新建文件夹"、乱码文件名（含非打印字符）、超长文件名(>100字符)

## 七、AI 响应解析（多策略容错）

```csharp
public static PanAnalysisResult Parse(string aiResponse)
{
    // 策略1：直接 JSON 反序列化
    if (TryParseJson(aiResponse, out var result))
        return result with { ParseSuccess = true };

    // 策略2：提取 ```json ... ``` 代码块
    var jsonBlock = ExtractMarkdownCodeBlock(aiResponse, "json");
    if (jsonBlock != null && TryParseJson(jsonBlock, out result))
        return result with { ParseSuccess = true };

    // 策略3：正则提取最外层 { ... }
    var jsonStr = ExtractOutermostJson(aiResponse);
    if (jsonStr != null && TryParseJson(jsonStr, out result))
        return result with { ParseSuccess = true };

    // 策略4：兜底 — 作为纯文本展示
    return new PanAnalysisResult
    {
        Summary = "AI 返回格式无法自动解析，请查看原始回复：",
        RawAiResponse = aiResponse,
        ParseSuccess = false
    };
}
```

## 八、操作执行

### 8.1 执行流程

```
ExecuteRecommendationsAsync(selected, progress, ct)
  │
  ├── 按类型分组：deletes / moves / renames
  │
  ├── 执行 Delete 批（每批 ≤100 个）
  │     ├── 构建 List<FileManagerFileItem>（只需 Path）
  │     ├── ManageFileAsync(FileOperation.Delete, batch, async: 0)
  │     ├── 记录每批结果
  │     └── progress.Report(Executing, "删除第 X/Y 批", current, total)
  │
  ├── 执行 Move 批（每批 ≤100 个）
  │     ├── 构建 List<FileManagerFileItem>（Path + Dest）
  │     ├── ManageFileAsync(FileOperation.Move, batch, async: 0, onDup: NewCopy)
  │     └── ...
  │
  ├── 执行 Rename 批（逐个执行，因为每个新名称不同）
  │     ├── 每个：ManageFileAsync(FileOperation.Rename, [item], async: 0)
  │     └── ...
  │
  └── 返回 PanExecutionReport（成功数/失败数/明细）
```

### 8.2 关键决策

| 决策点 | 选择 | 原因 |
|--------|------|------|
| 同步/异步模式 | `async: 0`（同步） | BaiduPanApiClient 无任务状态查询接口，同步模式简单可靠 |
| 批次大小 | ≤100 个/批 | 百度 API 限制 |
| 重复策略 | Delete: 不涉及; Move: `NewCopy`; Rename: `Fail` | Move 用 NewCopy 避免覆盖已有文件；Rename 用 Fail 避免意外覆盖 |
| 错误处理 | 失败不中断，记录后继续 | 最大化执行成功量 |
| 执行前确认 | 弹窗显示操作摘要（N个删除/M个移动/K个重命名） | 防止误操作 |

### 8.3 执行后刷新

```csharp
// 执行完成后，刷新 WebView2 页面
if (report.Succeeded > 0 && CurrentWebView?.CoreWebView2 != null)
{
    await CurrentWebView.CoreWebView2.Reload();
}
```

## 九、窗体设计

### 9.1 BaiduPanAnalysisForm 布局

```
┌──────────────────────────────────────────────────────────┐
│  百度网盘 AI 分析整理                               [×]  │
├──────────────────────────────────────────────────────────┤
│  📁 路径：/学习资料/英语        深度：[1级 ▼]  [开始分析] │
├──────────────────────────────────────────────────────────┤
│  ┌─ 进度 ──────────────────────────────────────────────┐ │
│  │ ████████████░░░░░░░░ 60%  正在调用 AI 分析...       │ │
│  │ ✓ 获取文件列表（1,234 个文件，3.2 GB）              │ │
│  │ ✓ 本地预计算：发现 3 组重复文件                     │ │
│  │ ▶ AI 分析中...                                      │ │
│  └──────────────────────────────────────────────────────┘ │
├──────────────────────────────────────────────────────────┤
│  ┌─ 统计概览 ──────────────────────────────────────────┐ │
│  │ 文件 1,234 | 文件夹 56 | 大小 3.2 GB | 重复 3组15个 │ │
│  │ pdf:23  mp4:5  docx:10  其他:8                      │ │
│  └──────────────────────────────────────────────────────┘ │
├──────────────────────────────────────────────────────────┤
│  ┌─ AI 建议 ───────────────────────── [全选] [反选] ───┐ │
│  │                                                      │ │
│  │  📊 总体评估                                         │ │
│  │  该目录共有 1,234 个文件，发现 3 组重复文件和...    │ │
│  │                                                      │ │
│  │  🔴 高优先级                                         │ │
│  │  ☐ [删除] .DS_Store (12个)         原因：系统文件    │ │
│  │  ☐ [删除] 重复课件.pdf (2个副本)   原因：重复文件    │ │
│  │                                                      │ │
│  │  🟡 中优先级                                         │ │
│  │  ☐ [移动] 课程录像.mp4 → /视频/    原因：分类混乱    │ │
│  │  ☐ [移动] 英语音频.mp3 → /音频/    原因：分类混乱    │ │
│  │                                                      │ │
│  │  🟢 低优先级                                         │ │
│  │  ☐ [重命名] 新建文件夹 → 课程资料  原因：命名无意义  │ │
│  │                                                      │ │
│  │  [执行选中操作 (5项)]  [导出报告]  [复制到剪贴板]   │ │
│  └──────────────────────────────────────────────────────┘ │
├──────────────────────────────────────────────────────────┤
│  日志                                                     │
│  [10:30:01] 开始分析 /学习资料/英语（深度: 2级）          │
│  [10:30:03] 获取到 1,234 个文件                           │
│  [10:30:04] 检测到 3 组重复文件（15个文件，可释放 45 MB） │
│  [10:30:06] AI 分析完成，返回 15 条建议                   │
└──────────────────────────────────────────────────────────┘
```

### 9.2 关键交互

1. **分析深度选择**：下拉框选择 1级/2级/3级/全部递归
2. **可取消**：分析过程中显示「取消」按钮，触发 CancellationToken
3. **推荐项分组**：按优先级（高/中/低）分组展示，支持全选/反选
4. **执行前确认**：点击「执行选中操作」→ 弹窗显示摘要 → 确认后执行
5. **执行进度**：弹窗显示批次进度（"删除第 1/3 批..."）
6. **导出报告**：保存为 Markdown 文件（含统计、建议列表、执行结果）
7. **主题适配**：实现 `IThemeable`，深色/浅色主题切换

### 9.3 WebView2BrowserForm 扩展

```csharp
// 工具栏新增按钮（Designer.cs）
private ToolStripSeparator toolStripSeparatorAi;
private ToolStripButton btnAiAnalyze;
// Text: "AI分析整理"  DisplayStyle: ImageAndText

// 按钮可见性控制（复用现有逻辑）
bool isNetdiskPage = CurrentWebView.Source?.ToString()?.StartsWith(Urls.BaiduNetdisk) ?? false;
btnOpenNetdisk.Visible = !isNetdiskPage;
btnAiAnalyze.Visible = isNetdiskPage;  // 仅在百度网盘页面显示
```

## 十、DI 注册

```csharp
// ServiceCollectionExtensions.cs — 扩展 AddCloudServices
public static IServiceCollection AddCloudServices(this IServiceCollection services, AppConfig appConfig)
{
    // 已有
    services.AddSingleton<ICloudStorageService, BaiduNetdiskService>();

    // 新增
    services.AddSingleton<IBaiduPanAnalysisService, BaiduPanAnalysisService>();

    return services;
}

// WebView2BrowserForm 构造函数新增参数
public WebView2BrowserForm(
    ICloudStorageService? cloudStorageService = null,
    IBaiduPanAnalysisService? analysisService = null,   // 新增
    IThemeService? themeService = null,
    ILogger? logger = null,
    // ... 其他已有参数
)
```

## 十一、完整工作流程

```
用户浏览百度网盘 → 进入某目录
    │
    ├── 点击「AI分析整理」
    │
    ▼ ① 路径提取
    ExtractPanPathViaJsAsync()  ──成功──→ panPath
    │失败
    ExtractPanPathFromUrl(url)  ──成功──→ panPath
    │失败
    PromptManualPathInput()     ──成功──→ panPath
    │
    ▼ ② 打开分析窗体
    BaiduPanAnalysisForm(analysisService, panPath)
    │
    ├── 用户选择分析深度，点击「开始分析」
    │
    ▼ ③ 获取快照
    EnsureValidTokenAsync()  ──→ accessToken
    BaiduPanApiClient.GetFileListRecursiveAsync(panPath)
    │  内部自动分页（每页1000条，7秒间隔限流）
    │
    ├── 转换为 PanFileInfo 列表
    ├── 本地预计算：重复检测（按 Name+Size 分组，有 Md5 则用 Md5）
    ├── 本地预计算：统计（类型分布、大小分布、大文件标记）
    │
    ▼ ④ AI 分析
    PanAnalysisPromptBuilder.Build(snapshot)
    │  根据 fileCount 选择策略（完整/精简/摘要）
    │
    IAIService.AskQuestionAsync(question, context)
    │
    PanAnalysisResultParser.Parse(response)
    │  JSON → markdown代码块 → 正则提取 → 纯文本兜底
    │
    ▼ ⑤ 展示结果
    按优先级分组展示推荐项
    │
    ├── 用户勾选，点击「执行选中操作」
    │
    ▼ ⑥ 执行操作
    确认弹窗（"将执行 N个删除/M个移动/K个重命名"）
    │  确认
    │
    分批执行（Delete批 → Move批 → Rename逐个）
    │  每批 ≤100，async=0 同步模式
    │  失败不中断，记录结果
    │
    ▼ ⑦ 完成
    PanExecutionReport 展示（成功X/失败Y）
    │
    ├── 自动刷新 WebView2 页面
    └── 可选：导出 Markdown 报告
```

## 十二、优化建议

### 12.1 性能

1. **本地预计算**：重复文件检测和统计数据在本地完成，减少 AI 工作量和 Token 消耗
2. **IMemoryCache 缓存**：同一目录的 Snapshot 缓存 5 分钟，AnalysisResult 缓存 10 分钟
3. **数据量自适应**：根据文件数量自动选择 Prompt 策略，避免 Token 超限
4. **限流复用**：`BaiduPanApiClient.SendGetRequestAsync` 已内置 7 秒间隔限流 + 3 次重试，无需额外处理

### 12.2 用户体验

1. **三层路径提取**：JS注入 → URL解析 → 手动输入，确保路径总能获取
2. **分析深度可选**：1级/2级/3级/全部，用户根据需要选择
3. **渐进式展示**：先显示统计概览和重复检测，再异步加载 AI 建议
4. **执行前确认**：弹窗显示操作摘要，防止误操作
5. **主题适配**：实现 IThemeable，与主窗体风格一致
6. **导出报告**：支持 Markdown 格式导出

### 12.3 安全

1. **删除保护**：百度 API 的 delete 操作会进入回收站，可在网盘网页端恢复
2. **Move 策略**：`OnDupStrategy.NewCopy`，目标已存在时自动重命名而非覆盖
3. **Token 脱敏**：日志中 AccessToken 显示为 `abc...xyz`（前3后3）
4. **批量确认**：执行 >10 个操作时需二次确认

### 12.4 扩展性

1. **Prompt 模板化**：System Prompt 可通过配置文件自定义
2. **AI 模型可选**：复用 `IAIServiceFactory`，支持切换不同 AI 提供商
3. **规则引擎**：未来可将本地预计算规则（重复检测、无意义文件识别）提取为可配置规则

## 十三、实现步骤

| 步骤 | 内容 | 文件 |
|------|------|------|
| 1 | 创建数据模型 | `Services/Cloud/PanAnalysisModels.cs` |
| 2 | 创建接口 | `Services/Cloud/IBaiduPanAnalysisService.cs` |
| 3 | 实现 PromptBuilder + ResultParser | `Services/Cloud/PanAnalysisPromptBuilder.cs`、`PanAnalysisResultParser.cs` |
| 4 | 实现 BaiduPanAnalysisService | `Services/Cloud/BaiduPanAnalysisService.cs` |
| 5 | DI 注册 | `Common/ServiceCollectionExtensions.cs` |
| 6 | 创建 BaiduPanAnalysisForm | `Forms/BaiduPanAnalysisForm.cs` + `.Designer.cs` |
| 7 | 扩展 WebView2BrowserForm | 添加按钮 + 路径提取 + 点击事件 |
| 8 | WindowManager 传递依赖 | `Managers/WindowManager.cs` |
| 9 | 测试：Token 刷新 | 验证过期 Token 自动刷新 |
| 10 | 测试：路径提取 | 验证三种策略覆盖各种 URL 格式 |
| 11 | 测试：AI 解析 | 验证多策略容错解析 |
| 12 | 测试：操作执行 | 验证批量删除/移动/重命名 |

## 十四、风险与缓解

| 风险 | 影响 | 缓解 |
|------|------|------|
| Token 过期 | API 调用 401 | 自动刷新，刷新失败引导重新授权 |
| 文件过多 AI 超限 | AI 拒绝或截断 | 数据量自适应策略（5.3），>5000 降级为摘要 |
| AI 返回非 JSON | 无法解析推荐项 | 四层解析策略，兜底纯文本展示 |
| 百度 API 限流 | 429 Too Many Requests | API 客户端已内置 7 秒限流 + 3 次重试 |
| WebView2 URL 格式变化 | 路径提取失败 | JS注入 → URL解析 → 手动输入 三层降级 |
| 误删文件 | 数据丢失 | 百度 delete 进回收站可恢复 + 执行前确认弹窗 |
| async=0 超时 | 大批量操作卡住 | 每批 ≤100 个，单批超时 30 秒（HttpClient 默认） |

## 十五、文件清单

### 新增文件

```
LearningAssistant/
├── Services/Cloud/
│   ├── IBaiduPanAnalysisService.cs           # 接口
│   ├── BaiduPanAnalysisService.cs            # 实现（编排者）
│   ├── PanAnalysisModels.cs                  # 全部数据模型
│   ├── PanAnalysisPromptBuilder.cs           # Prompt 构建（内部类）
│   └── PanAnalysisResultParser.cs            # 响应解析（内部类）
├── Forms/
│   ├── BaiduPanAnalysisForm.cs               # 分析窗体
│   └── BaiduPanAnalysisForm.Designer.cs      # 窗体设计器
```

### 修改文件

```
LearningAssistant/
├── Common/
│   └── ServiceCollectionExtensions.cs        # DI 注册
├── Forms/
│   ├── WebView2BrowserForm.cs                # 按钮 + 路径提取 + 事件
│   └── WebView2BrowserForm.Designer.cs       # 按钮控件定义
├── Managers/
│   └── WindowManager.cs                      # 传递 IBaiduPanAnalysisService
```
