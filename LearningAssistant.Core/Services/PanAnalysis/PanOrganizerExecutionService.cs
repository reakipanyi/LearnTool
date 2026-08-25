using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Models.PanAnalysis;
using LearningAssistant.Services.Baidu;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.PanAnalysis;

public interface IPanOrganizerExecutionService
{
    List<PanTodoItem> BuildTodosFromRecommendations(List<PanRecommendation> recommendations);
    List<PanTodoItem> TopologicalSort(List<PanTodoItem> todos);
    List<PanTodoBatch> MergeBatches(List<PanTodoItem> sortedTodos);
    List<PreflightCheckResult> PreflightCheck(List<PanTodoBatch> batches, PanDirectorySnapshot snapshot);
    Task<PanExecutionReport> ExecuteAsync(List<PanTodoBatch> batches, PanDirectorySnapshot snapshot, IProgress<PanExecutionProgress>? progress, CancellationToken ct = default);
    PanUndoEntry? UndoLast(PanDirectorySnapshot snapshot);
    int UndoCount { get; }
    PanExecutionReport DryRun(List<PanTodoBatch> batches, PanDirectorySnapshot snapshot);
    string GenerateSummary(PanExecutionReport report);
    void SetEventBus(IEventBus? eventBus);
}

public class PanTodoBatch
{
    public PanRecommendationType Type { get; set; }
    public string? DestinationPath { get; set; }
    public List<PanTodoItem> Items { get; set; } = new();
    public bool PreflightPassed { get; set; } = true;
    public string? PreflightWarning { get; set; }
}

public class PreflightCheckResult
{
    public PanTodoBatch Batch { get; set; } = new();
    public bool Passed { get; set; } = true;
    public string? Warning { get; set; }
    public List<string> MissingFsIds { get; set; } = new();
}

public class PanExecutionProgress
{
    public PanTodoItem? CurrentItem { get; set; }
    public PanTodoBatch? CurrentBatch { get; set; }
    public int CompletedCount { get; set; }
    public int TotalCount { get; set; }
    public PanExecutionResult? Result { get; set; }
    public string Message { get; set; } = "";
    public ProgressLogLevel LogLevel { get; set; } = ProgressLogLevel.Info;
}

public enum ProgressLogLevel { Info, Success, Warning, Error, Debug }

public class PanOrganizerExecutionService : IPanOrganizerExecutionService
{
    private readonly ILogger<PanOrganizerExecutionService>? _logger;
    private readonly IPanTokenManager? _tokenManager;
    private readonly Stack<PanUndoEntry> _undoStack = new();
    private IEventBus? _eventBus;
    public PanOrganizerExecutionService(IPanTokenManager? tokenManager = null, ILogger<PanOrganizerExecutionService>? logger = null) { _tokenManager = tokenManager; _logger = logger; }

    public int UndoCount => _undoStack.Count;

    public void SetEventBus(IEventBus? eventBus) { _eventBus = eventBus; }

    public List<PanTodoItem> BuildTodosFromRecommendations(List<PanRecommendation> recommendations)
    {
        var todos = new List<PanTodoItem>();
        if (recommendations == null) return todos;
        foreach (var rec in recommendations)
        {
            if (rec == null) continue;
            if (rec.Type == PanRecommendationType.Keep) continue;
            // P1 修复：AI 返回的 JSON 可能字段缺失，反序列化后 TargetPath/TargetName 为 null
            var targetPath = rec.TargetPath ?? "";
            var targetName = rec.TargetName ?? "";
            var todo = new PanTodoItem
            {
                SourceRecommendationId = rec.Id ?? "",
                Type = rec.Type,
                SourcePath = targetPath,
                SourceName = targetName,
                DestinationPath = rec.DestinationPath,
                NewName = rec.NewName,
                Reason = rec.Reason ?? "",
                Status = rec.IsSelected ? TodoStatus.Confirmed : TodoStatus.Skipped,
                SourceFsId = long.TryParse(rec.AffectedFileId, out var fid) ? fid : null,
                IsFolder = targetPath.EndsWith("/")
            };
            if (rec.Type == PanRecommendationType.CreateFolder) { todo.FolderName = rec.NewName ?? rec.TargetName; todo.ParentPath = rec.DestinationPath ?? "/"; }
            todos.Add(todo);
        }
        return todos;
    }

    public List<PanTodoItem> TopologicalSort(List<PanTodoItem> todos)
    {
        if (todos == null || todos.Count == 0) return new();
        var active = todos.Where(t => t.Status == TodoStatus.Confirmed).ToList();
        var typeOrder = new Dictionary<PanRecommendationType, int>
        {
            { PanRecommendationType.CreateFolder, 0 }, { PanRecommendationType.Move, 1 },
            { PanRecommendationType.Rename, 2 }, { PanRecommendationType.MergeFolder, 3 },
            { PanRecommendationType.Delete, 4 }, { PanRecommendationType.Keep, 5 }
        };
        var sorted = active.OrderBy(t => typeOrder.TryGetValue(t.Type, out var o) ? o : 99)
                            .ThenBy(t => t.SourceName, StringComparer.OrdinalIgnoreCase).ToList();
        sorted.AddRange(todos.Where(t => t.Status == TodoStatus.Skipped));
        return sorted;
    }

    public List<PanTodoBatch> MergeBatches(List<PanTodoItem> sortedTodos)
    {
        var batches = new List<PanTodoBatch>();
        if (sortedTodos == null) return batches;
        var groups = sortedTodos.Where(t => t.Status == TodoStatus.Confirmed)
            .GroupBy(t => new { t.Type, Dest = t.DestinationPath ?? "" });
        foreach (var g in groups) batches.Add(new PanTodoBatch { Type = g.Key.Type, DestinationPath = g.Key.Dest, Items = g.ToList() });
        var typeOrder = new Dictionary<PanRecommendationType, int>
        {
            { PanRecommendationType.CreateFolder, 0 }, { PanRecommendationType.Move, 1 },
            { PanRecommendationType.Rename, 2 }, { PanRecommendationType.MergeFolder, 3 }, { PanRecommendationType.Delete, 4 }
        };
        batches.Sort((a, b) => (typeOrder.TryGetValue(a.Type, out var va) ? va : 99).CompareTo(typeOrder.TryGetValue(b.Type, out var vb) ? vb : 99));
        return batches;
    }

    public List<PreflightCheckResult> PreflightCheck(List<PanTodoBatch> batches, PanDirectorySnapshot snapshot)
    {
        var results = new List<PreflightCheckResult>();
        if (snapshot == null) return results;
        var fsIds = new HashSet<long>();
        foreach (var f in snapshot.Files) if (f.FsId > 0) fsIds.Add(f.FsId);
        foreach (var batch in batches)
        {
            var result = new PreflightCheckResult { Batch = batch };
            var missing = new List<string>();
            foreach (var item in batch.Items)
            {
                if (item.Type == PanRecommendationType.CreateFolder) continue;
                // 文件夹或 FsId<=0（占位符/默认值）时用 Path 查找；仅对真实 FsId(>0) 的文件用 FsId 查找
                if (!item.IsFolder && item.SourceFsId.HasValue && item.SourceFsId.Value > 0)
                {
                    if (!fsIds.Contains(item.SourceFsId.Value)) missing.Add($"FsId={item.SourceFsId.Value}({item.SourceName})");
                }
                else
                {
                    bool found = snapshot.Files.Any(f => f.Path.Equals(item.SourcePath, StringComparison.OrdinalIgnoreCase))
                              || snapshot.Folders.Any(f => f.Path.Equals(item.SourcePath, StringComparison.OrdinalIgnoreCase));
                    if (!found) missing.Add($"Path={item.SourcePath}({item.SourceName})");
                }
            }
            if (missing.Count > 0) { result.Passed = false; result.Warning = $"stale:{missing.Count}"; result.MissingFsIds = missing; batch.PreflightPassed = false; batch.PreflightWarning = result.Warning; }
            results.Add(result);
        }
        return results;
    }

    public async Task<PanExecutionReport> ExecuteAsync(List<PanTodoBatch> batches, PanDirectorySnapshot snapshot, IProgress<PanExecutionProgress>? progress, CancellationToken ct = default)
    {
        var report = new PanExecutionReport { StartTime = DateTime.UtcNow };
        var allItems = batches.SelectMany(b => b.Items).Where(i => i.Status == TodoStatus.Confirmed).ToList();
        report.TotalRequested = allItems.Count;
        int completed = 0;
        void Rpt(PanTodoItem? item, PanTodoBatch? batch, PanExecutionResult? result, string msg, ProgressLogLevel level)
            => progress?.Report(new PanExecutionProgress { CurrentItem = item, CurrentBatch = batch, CompletedCount = completed, TotalCount = report.TotalRequested, Result = result, Message = msg, LogLevel = level });

        Rpt(null, null, null, _tokenManager != null ? ">>> Start (real API sync)" : ">>> Start (memory simulation, no token)", ProgressLogLevel.Info);
        foreach (var batch in batches)
        {
            if (ct.IsCancellationRequested) break;
            if (!batch.PreflightPassed)
            {
                foreach (var item in batch.Items)
                {
                    item.Status = TodoStatus.Failed;
                    var result = new PanExecutionResult { Recommendation = new() { Type = item.Type, TargetPath = item.SourcePath, TargetName = item.SourceName }, Success = false, ErrorMessage = batch.PreflightWarning };
                    item.ExecutionResult = result; report.Results.Add(result); report.Failed++; completed++;
                    Rpt(item, batch, result, $"[SKIP] {item.SourceName}", ProgressLogLevel.Warning);
                }
                continue;
            }
            Rpt(null, batch, null, $"[BATCH] {batch.Type} -> {batch.DestinationPath} ({batch.Items.Count})", ProgressLogLevel.Debug);
            foreach (var item in batch.Items)
            {
                if (ct.IsCancellationRequested) break;
                item.Status = TodoStatus.Executing;
                try
                {
                    // 1. 先调用真实百度网盘 API 同步（filemanager / mkdir）
                    var (apiOk, apiErr) = await ExecuteViaApiAsync(item, ct);
                    if (!apiOk)
                    {
                        var failResult = new PanExecutionResult { Recommendation = new() { Type = item.Type, TargetPath = item.SourcePath, TargetName = item.SourceName, DestinationPath = item.DestinationPath, NewName = item.NewName }, Success = false, ErrorMessage = "API: " + apiErr };
                        item.ExecutionResult = failResult; item.Status = TodoStatus.Failed; report.Results.Add(failResult); report.Failed++;
                        Rpt(item, batch, failResult, $"[API FAIL] {item.SourceName}->{apiErr}", ProgressLogLevel.Error);
                        completed++; continue;
                    }
                    // 2. API 成功后更新内存快照（保持 UI 一致）
                    var (success, msg) = ExecuteSingle(item, snapshot);
                    var result = new PanExecutionResult { Recommendation = new() { Type = item.Type, TargetPath = item.SourcePath, TargetName = item.SourceName, DestinationPath = item.DestinationPath, NewName = item.NewName }, Success = success, ErrorMessage = success ? null : msg };
                    item.ExecutionResult = result; item.Status = success ? TodoStatus.Succeeded : TodoStatus.Failed; report.Results.Add(result);
                    if (success) { report.Succeeded++; PushUndoEntry(item); PublishItemEvent(item, snapshot); Rpt(item, batch, result, $"[OK] {item.Type}: {item.SourceName}", ProgressLogLevel.Success); }
                    else { report.Failed++; Rpt(item, batch, result, $"[FAIL] {item.SourceName}->{msg}", ProgressLogLevel.Error); }
                }
                catch (Exception ex)
                {
                    item.Status = TodoStatus.Failed;
                    var result = new PanExecutionResult { Recommendation = new() { Type = item.Type, TargetPath = item.SourcePath, TargetName = item.SourceName }, Success = false, ErrorMessage = ex.Message };
                    item.ExecutionResult = result; report.Results.Add(result); report.Failed++;
                    Rpt(item, batch, result, $"[ERR] {item.SourceName}->{ex.Message}", ProgressLogLevel.Error);
                }
                completed++; await Task.Delay(15, ct);
            }
            if (ct.IsCancellationRequested) { Rpt(null, null, null, "[STOP] cancelled", ProgressLogLevel.Warning); break; }
        }
        report.Skipped = allItems.Count - report.Succeeded - report.Failed;
        report.EndTime = DateTime.UtcNow;
        Rpt(null, null, null, $"<<< Done: {report.Succeeded} ok / {report.Failed} fail / {report.Skipped} skip {report.Duration.TotalSeconds:F1}s", ProgressLogLevel.Info);
        PublishCompletedEvent(report);
        return report;
    }

    /// <summary>
    /// 调用真实百度网盘 API 执行操作（Move/Rename/Delete/CreateFolder）。
    /// 成功后由调用方再调用 ExecuteSingle 更新内存快照。
    /// </summary>
    private async Task<(bool success, string? error)> ExecuteViaApiAsync(PanTodoItem item, CancellationToken ct)
    {
        if (_tokenManager == null)
        {
            _logger?.LogWarning("ExecuteViaApiAsync: _tokenManager 为 null，无法同步到网盘");
            return (false, "Token 管理器未注入，无法同步到网盘");
        }

        try
        {
            var token = await _tokenManager.EnsureValidTokenAsync(ct);
            if (string.IsNullOrEmpty(token))
            {
                _logger?.LogWarning("ExecuteViaApiAsync: 获取到的 token 为空");
                return (false, "百度网盘 Token 为空，请先授权");
            }
            _logger?.LogInformation("ExecuteViaApiAsync: token 获取成功，开始调用 API: {Type} {Path} -> {NewName}",
                item.Type, item.SourcePath, item.NewName ?? item.DestinationPath ?? "");

            using var apiClient = new BaiduPanApiClient(token);

            switch (item.Type)
            {
                case PanRecommendationType.CreateFolder:
                    {
                        var p = (item.ParentPath ?? "/").TrimEnd('/');
                        var n = item.FolderName ?? item.NewName ?? "NewFolder";
                        var fp = p + "/" + n;
                        var resp = await apiClient.CreateFolderAsync(fp);
                        return (resp.ErrorCode == 0, resp.ErrorCode == 0 ? null : $"errno={resp.ErrorCode}");
                    }
                case PanRecommendationType.Move:
                    {
                        var dest = (item.DestinationPath ?? "/").TrimEnd('/');
                        var fileList = new List<FileManagerFileItem>
                    {
                        new() { Path = item.SourcePath, Dest = dest }
                    };
                        var resp = await apiClient.ManageFileAsync(FileOperation.Move, fileList, async: 0, onDup: OnDupStrategy.NewCopy);
                        return (resp.ErrorCode == 0, resp.ErrorCode == 0 ? null : $"errno={resp.ErrorCode}");
                    }
                case PanRecommendationType.Rename:
                    {
                        var fileList = new List<FileManagerFileItem>
                    {
                        new() { Path = item.SourcePath, NewName = item.NewName ?? "" }
                    };
                        var resp = await apiClient.ManageFileAsync(FileOperation.Rename, fileList, async: 0, onDup: OnDupStrategy.Fail);
                        return (resp.ErrorCode == 0, resp.ErrorCode == 0 ? null : $"errno={resp.ErrorCode}");
                    }
                case PanRecommendationType.Delete:
                    {
                        var fileList = new List<FileManagerFileItem>
                    {
                        new() { Path = item.SourcePath }
                    };
                        var resp = await apiClient.ManageFileAsync(FileOperation.Delete, fileList, async: 0, onDup: OnDupStrategy.NewCopy);
                        return (resp.ErrorCode == 0, resp.ErrorCode == 0 ? null : $"errno={resp.ErrorCode}");
                    }
                default:
                    return (false, $"不支持的操作类型: {item.Type}");
            }
        }
        catch (PanApiException ex)
        {
            // 百度网盘业务错误（errno != 0）：ValidateResponse 抛出，携带错误码
            _logger?.LogError(ex, "ExecuteViaApiAsync 百度API业务错误: errno={ErrorCode} type={Type} path={Path}",
                ex.ErrorCode, item.Type, item.SourcePath);
            return (false, $"百度API错误(errno={ex.ErrorCode}): {ex.Message}");
        }
        catch (PanAuthException ex)
        {
            // 授权失败（token 未配置/过期/刷新失败）
            _logger?.LogError(ex, "ExecuteViaApiAsync 百度授权失败: {Message}", ex.Message);
            return (false, $"百度授权失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ExecuteViaApiAsync 异常: {Type} {Path}", item.Type, item.SourcePath);
            return (false, ex.Message);
        }
    }

    private (bool, string?) ExecuteSingle(PanTodoItem item, PanDirectorySnapshot snapshot)
    {
        switch (item.Type)
        {
            case PanRecommendationType.CreateFolder:
                {
                    var p = (item.ParentPath ?? "/").TrimEnd('/'); var n = item.FolderName ?? item.NewName ?? "NewFolder";
                    var fp = p + "/" + n;
                    if (snapshot.Folders.Any(f => f.Path.Equals(fp, StringComparison.OrdinalIgnoreCase))) return (false, "exists");
                    var rp = fp.StartsWith(snapshot.DirectoryPath, StringComparison.Ordinal) ? fp[snapshot.DirectoryPath.Length..].TrimStart('/') : n;
                    snapshot.Folders.Add(new PanFolderInfo { Name = n, Path = fp, RelativePath = rp, Depth = rp.Count(c => c == '/') + 1 });
                    if (snapshot.Statistics != null) snapshot.Statistics.TotalFolderCount++;
                    return (true, null);
                }
            case PanRecommendationType.Move:
                {
                    var d = (item.DestinationPath ?? "/").TrimEnd('/'); var dp = d + "/";
                    var sn = item.SourceName; if (string.IsNullOrEmpty(sn) && !string.IsNullOrEmpty(item.SourcePath)) sn = item.SourcePath[(item.SourcePath.LastIndexOf('/') + 1)..];
                    var fe = snapshot.Folders.FirstOrDefault(f => f.Path.Equals(item.SourcePath, StringComparison.OrdinalIgnoreCase));
                    if (fe != null)
                    {
                        var op = fe.Path.TrimEnd('/') + "/"; var np = dp + sn + "/";
                        fe.Path = dp + "/" + sn; fe.Name = sn;
                        foreach (var s in snapshot.Folders) if (s.Path.StartsWith(op, StringComparison.OrdinalIgnoreCase)) s.Path = np + s.Path[op.Length..];
                        foreach (var f in snapshot.Files) if (f.Path.StartsWith(op, StringComparison.OrdinalIgnoreCase)) f.Path = np + f.Path[op.Length..];
                        return (true, null);
                    }
                    var fie = snapshot.Files.FirstOrDefault(f => f.Path.Equals(item.SourcePath, StringComparison.OrdinalIgnoreCase));
                    if (fie != null)
                    {
                        fie.Path = dp + "/" + sn; fie.Name = sn;
                        var rp = d.StartsWith(snapshot.DirectoryPath, StringComparison.Ordinal) ? d[snapshot.DirectoryPath.Length..].TrimStart('/') : "";
                        fie.RelativePath = (string.IsNullOrEmpty(rp) ? "" : rp + "/") + sn;
                        return (true, null);
                    }
                    return (false, "not found");
                }
            case PanRecommendationType.Rename:
                {
                    var nn = item.NewName ?? ""; if (string.IsNullOrEmpty(nn)) return (false, "empty name");
                    var fe = snapshot.Folders.FirstOrDefault(f => f.Path.Equals(item.SourcePath, StringComparison.OrdinalIgnoreCase));
                    if (fe != null)
                    {
                        var op = fe.Path.TrimEnd('/') + "/"; var pp = fe.Path[..^fe.Name.Length].TrimEnd('/'); var npath = pp + "/" + nn;
                        fe.Name = nn; fe.Path = npath; var np = npath.TrimEnd('/') + "/";
                        foreach (var s in snapshot.Folders) if (s.Path.StartsWith(op, StringComparison.OrdinalIgnoreCase)) s.Path = np + s.Path[op.Length..];
                        foreach (var f in snapshot.Files) if (f.Path.StartsWith(op, StringComparison.OrdinalIgnoreCase)) f.Path = np + f.Path[op.Length..];
                        return (true, null);
                    }
                    var fie = snapshot.Files.FirstOrDefault(f => f.Path.Equals(item.SourcePath, StringComparison.OrdinalIgnoreCase));
                    if (fie != null) { var pp = fie.Path[..^fie.Name.Length]; fie.Name = nn; fie.Path = pp + nn; return (true, null); }
                    return (false, "not found");
                }
            case PanRecommendationType.Delete:
                {
                    var fe = snapshot.Folders.FirstOrDefault(f => f.Path.Equals(item.SourcePath, StringComparison.OrdinalIgnoreCase));
                    if (fe != null)
                    {
                        var p = fe.Path.TrimEnd('/') + "/";
                        snapshot.Files = snapshot.Files.Where(f => !f.Path.StartsWith(p, StringComparison.OrdinalIgnoreCase)).ToList();
                        snapshot.Folders = snapshot.Folders.Where(f => (!f.Path.StartsWith(p, StringComparison.OrdinalIgnoreCase) || f.Path.Equals(fe.Path, StringComparison.OrdinalIgnoreCase))).ToList();
                        snapshot.Folders.Remove(fe);
                        if (snapshot.Statistics != null) snapshot.Statistics.TotalFolderCount = Math.Max(0, snapshot.Statistics.TotalFolderCount - 1);
                        return (true, null);
                    }
                    var fie = snapshot.Files.FirstOrDefault(f => f.Path.Equals(item.SourcePath, StringComparison.OrdinalIgnoreCase));
                    if (fie != null)
                    {
                        snapshot.Files.Remove(fie);
                        if (snapshot.Statistics != null) { snapshot.Statistics.TotalFileCount = Math.Max(0, snapshot.Statistics.TotalFileCount - 1); snapshot.Statistics.TotalSizeBytes = Math.Max(0, snapshot.Statistics.TotalSizeBytes - fie.SizeBytes); }
                        return (true, null);
                    }
                    return (false, "not found");
                }
            default: return (false, $"unsupported:{item.Type}");
        }
    }

    #region === P2-1: 撤销栈 ===

    /// <summary>成功执行后构建并压入撤销条目。CreateFolder/Move/Rename 可逆；Delete 不可逆（不入栈）。</summary>
    private void PushUndoEntry(PanTodoItem item)
    {
        try
        {
            var entry = BuildUndoEntry(item);
            if (entry != null) _undoStack.Push(entry);
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "PushUndoEntry 失败，忽略"); }
    }

    private static PanUndoEntry? BuildUndoEntry(PanTodoItem item)
    {
        var original = new PanRecommendation
        {
            Type = item.Type,
            TargetPath = item.SourcePath,
            TargetName = item.SourceName,
            DestinationPath = item.DestinationPath,
            NewName = item.NewName
        };
        var reverse = new PanRecommendation();
        bool canUndo = true;

        switch (item.Type)
        {
            case PanRecommendationType.CreateFolder:
                {
                    var createdPath = (item.ParentPath ?? "/").TrimEnd('/') + "/" + (item.FolderName ?? item.NewName ?? "NewFolder");
                    reverse.Type = PanRecommendationType.Delete;
                    reverse.TargetPath = createdPath;
                    reverse.TargetName = item.FolderName ?? item.NewName ?? "NewFolder";
                    break;
                }
            case PanRecommendationType.Move:
                {
                    var dest = (item.DestinationPath ?? "/").TrimEnd('/');
                    var movedPath = dest + "/" + item.SourceName;
                    reverse.Type = PanRecommendationType.Move;
                    reverse.TargetPath = movedPath;
                    reverse.TargetName = item.SourceName;
                    reverse.DestinationPath = GetParentPath(item.SourcePath);
                    break;
                }
            case PanRecommendationType.Rename:
                {
                    var srcParent = item.SourcePath[..^item.SourceName.Length].TrimEnd('/');
                    var renamedPath = srcParent + "/" + (item.NewName ?? "");
                    reverse.Type = PanRecommendationType.Rename;
                    reverse.TargetPath = renamedPath;
                    reverse.NewName = item.SourceName;
                    break;
                }
            default:
                // Delete / MergeFolder / Keep: 不可逆（内存模型中删除已移除列表项，无法简单还原）
                canUndo = false;
                break;
        }

        return new PanUndoEntry { OriginalOperation = original, ReverseOperation = reverse, CanUndo = canUndo };
    }

    public PanUndoEntry? UndoLast(PanDirectorySnapshot snapshot)
    {
        if (snapshot == null || _undoStack.Count == 0) return null;
        var entry = _undoStack.Pop();
        if (!entry.CanUndo) return entry;
        try
        {
            var reverseTodo = new PanTodoItem
            {
                Type = entry.ReverseOperation.Type,
                SourcePath = entry.ReverseOperation.TargetPath,
                SourceName = entry.ReverseOperation.TargetName,
                DestinationPath = entry.ReverseOperation.DestinationPath,
                NewName = entry.ReverseOperation.NewName,
                Status = TodoStatus.Confirmed
            };
            ExecuteSingle(reverseTodo, snapshot);
            _logger?.LogDebug("撤销成功：{Type} {Name}", entry.OriginalOperation.Type, entry.OriginalOperation.TargetName);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "撤销执行失败：{Type} {Name}", entry.OriginalOperation.Type, entry.OriginalOperation.TargetName);
        }
        return entry;
    }

    private static string GetParentPath(string path)
    {
        var trimmed = (path ?? "").TrimEnd('/');
        var idx = trimmed.LastIndexOf('/');
        return idx <= 0 ? "/" : trimmed.Substring(0, idx);
    }

    #endregion

    #region === P2-2: Dry-Run 演练模式 ===

    /// <summary>演练模式：基于快照静态校验每项操作是否会成功，不修改快照、不入撤销栈、不发布事件。</summary>
    public PanExecutionReport DryRun(List<PanTodoBatch> batches, PanDirectorySnapshot snapshot)
    {
        var report = new PanExecutionReport { StartTime = DateTime.UtcNow, IsDryRun = true };
        if (batches == null || snapshot == null)
        {
            report.EndTime = DateTime.UtcNow;
            return report;
        }
        var allItems = batches.SelectMany(b => b.Items).Where(i => i.Status == TodoStatus.Confirmed).ToList();
        report.TotalRequested = allItems.Count;

        foreach (var item in allItems)
        {
            var (wouldSucceed, reason) = PredictSingle(item, snapshot);
            var result = new PanExecutionResult
            {
                Recommendation = new() { Type = item.Type, TargetPath = item.SourcePath, TargetName = item.SourceName, DestinationPath = item.DestinationPath, NewName = item.NewName },
                Success = wouldSucceed,
                ErrorMessage = wouldSucceed ? null : reason
            };
            report.Results.Add(result);
            if (wouldSucceed) report.Succeeded++; else report.Failed++;
        }
        report.Skipped = allItems.Count - report.Succeeded - report.Failed;
        report.EndTime = DateTime.UtcNow;
        return report;
    }

    /// <summary>静态预测单条操作是否会成功（不修改快照）。</summary>
    private static (bool, string?) PredictSingle(PanTodoItem item, PanDirectorySnapshot snapshot)
    {
        switch (item.Type)
        {
            case PanRecommendationType.CreateFolder:
                {
                    var fp = (item.ParentPath ?? "/").TrimEnd('/') + "/" + (item.FolderName ?? item.NewName ?? "NewFolder");
                    return snapshot.Folders.Any(f => f.Path.Equals(fp, StringComparison.OrdinalIgnoreCase))
                        ? (false, "目标已存在")
                        : (true, null);
                }
            case PanRecommendationType.Rename:
                return string.IsNullOrEmpty(item.NewName) ? (false, "新名称为空") : (true, null);
            case PanRecommendationType.Move:
            case PanRecommendationType.Delete:
            case PanRecommendationType.MergeFolder:
                {
                    var found = snapshot.Files.Any(f => f.Path.Equals(item.SourcePath, StringComparison.OrdinalIgnoreCase))
                             || snapshot.Folders.Any(f => f.Path.Equals(item.SourcePath, StringComparison.OrdinalIgnoreCase));
                    return found ? (true, null) : (false, "源路径不存在（快照可能过时）");
                }
            default:
                return (false, $"不支持:{item.Type}");
        }
    }

    #endregion

    #region === P2-3: 整理完成 AI 总结 ===

    /// <summary>根据执行报告生成结构化总结文本（比 PanExecutionReport.Summary 更详细）。</summary>
    public string GenerateSummary(PanExecutionReport report)
    {
        if (report == null) return "";
        var sb = new System.Text.StringBuilder();
        var tag = report.IsDryRun ? "【Dry-Run 演练】" : "【执行完成】";
        sb.AppendLine($"{tag} 整理报告");
        sb.AppendLine($"──────────────────────────────");
        sb.AppendLine($"耗时：{report.Duration.TotalSeconds:F1}s  |  成功率：{report.SuccessRate:F1}%");
        sb.AppendLine($"请求 {report.TotalRequested} 项 → 成功 {report.Succeeded} · 失败 {report.Failed} · 跳过 {report.Skipped}");

        // 按操作类型分组统计
        var byType = report.Results.GroupBy(r => r.Recommendation.Type);
        if (byType.Any())
        {
            sb.AppendLine($"──────────────────────────────");
            sb.AppendLine("操作明细：");
            foreach (var g in byType)
            {
                var ok = g.Count(r => r.Success);
                var fail = g.Count(r => !r.Success);
                var label = g.Key switch
                {
                    PanRecommendationType.CreateFolder => "📁 新建文件夹",
                    PanRecommendationType.Move => "📦 移动",
                    PanRecommendationType.Rename => "✏️ 重命名",
                    PanRecommendationType.Delete => "🗑️ 删除",
                    PanRecommendationType.MergeFolder => "📂 合并",
                    _ => g.Key.ToString()
                };
                sb.AppendLine($"  {label}：{ok} 成功" + (fail > 0 ? $" / {fail} 失败" : ""));
            }
        }

        // 失败项明细（最多 5 条）
        var failures = report.Results.Where(r => !r.Success).Take(5).ToList();
        if (failures.Count > 0)
        {
            sb.AppendLine($"──────────────────────────────");
            sb.AppendLine($"失败明细（前 {failures.Count} 条）：");
            foreach (var f in failures)
                sb.AppendLine($"  ✗ {f.Recommendation.TargetName} → {f.ErrorMessage}");
        }

        // 建议性总结
        sb.AppendLine($"──────────────────────────────");
        if (report.HasFailures)
            sb.AppendLine(report.IsDryRun ? "⚠️ 演练发现部分操作将失败，建议修正后再正式执行。" : "⚠️ 本次执行存在失败项，建议检查日志后重试失败项。");
        else if (report.Succeeded > 0)
            sb.AppendLine(report.IsDryRun ? "✅ 演练通过：所有操作预计可成功执行。" : "✅ 全部操作执行成功，整理完成。");
        else
            sb.AppendLine("ℹ️ 无已确认待办执行。");

        return sb.ToString();
    }

    #endregion

    #region === P2-4: 事件总线发布 ===

    private void PublishItemEvent(PanTodoItem item, PanDirectorySnapshot snapshot)
    {
        if (_eventBus == null) return;
        try
        {
            switch (item.Type)
            {
                case PanRecommendationType.CreateFolder:
                    {
                        var fp = (item.ParentPath ?? "/").TrimEnd('/') + "/" + (item.FolderName ?? item.NewName ?? "NewFolder");
                        _eventBus.Publish(new PanFolderCreatedEvent { FolderPath = fp, ParentPath = item.ParentPath ?? "/" });
                        break;
                    }
                case PanRecommendationType.Move:
                    {
                        var dest = (item.DestinationPath ?? "/").TrimEnd('/');
                        var newPath = dest + "/" + item.SourceName;
                        _eventBus.Publish(new PanFileMovedEvent { File = new() { Path = newPath, Name = item.SourceName, IsFolder = item.IsFolder }, OldPath = item.SourcePath, NewPath = newPath });
                        break;
                    }
                case PanRecommendationType.Rename:
                    {
                        var srcParent = item.SourcePath[..^item.SourceName.Length].TrimEnd('/');
                        var newPath = srcParent + "/" + (item.NewName ?? "");
                        _eventBus.Publish(new PanFileRenamedEvent { OldPath = item.SourcePath, NewPath = newPath, IsFolder = item.IsFolder });
                        break;
                    }
                case PanRecommendationType.Delete:
                    _eventBus.Publish(new PanFileDeletedEvent { File = new() { Path = item.SourcePath, Name = item.SourceName, IsFolder = item.IsFolder } });
                    break;
            }
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "PublishItemEvent 失败，忽略"); }
    }

    private void PublishCompletedEvent(PanExecutionReport report)
    {
        if (_eventBus == null) return;
        try { _eventBus.Publish(new PanOrganizeCompletedEvent { Report = report, Summary = GenerateSummary(report) }); }
        catch (Exception ex) { _logger?.LogWarning(ex, "PublishCompletedEvent 失败，忽略"); }
    }

    #endregion
}