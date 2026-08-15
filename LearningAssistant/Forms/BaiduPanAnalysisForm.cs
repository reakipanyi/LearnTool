using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Config;
using LearningAssistant.Models.PanAnalysis;
using LearningAssistant.Services;
using LearningAssistant.Services.PanAnalysis;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Forms
{
    /// <summary>
    /// 百度网盘 AI 分析窗体
    /// </summary>
    public partial class BaiduPanAnalysisForm : Form, IThemeable
    {
        private readonly IBaiduPanAnalysisOrchestrator _orchestrator;
        private readonly string _directoryPath;
        private readonly IThemeService _themeService;
        private readonly ILogger<BaiduPanAnalysisForm>? _logger;
        private readonly IAIPanelPopupService? _aiPanelPopupService;
        private readonly AiConfig? _aiConfig;
        private readonly IPanAnalysisPromptBuilder? _promptBuilder;
        private CancellationTokenSource? _cts;
        private PanDirectorySnapshot? _snapshot;
        private PanAnalysisResult? _analysisResult;
        private bool _isClosing;

        /// <summary>首次分析的根目录路径（用于限制「返回上级」的范围）</summary>
        private string _rootAnalysisPath = "";

        /// <summary>当前正在/最近分析的目录路径（用于「返回上级」导航）</summary>
        private string _currentAnalysisPath = "";

        /// <summary>是否已成功执行过任何操作（供父窗体判断是否刷新）</summary>
        public bool ExecutedAny { get; private set; }

        /// <summary>
        /// 设计视图专用无参构造函数。
        /// 仅用于 Visual Studio 设计器预览，运行时请使用带参构造函数。
        /// </summary>
        public BaiduPanAnalysisForm()
        {
            _directoryPath = string.Empty;
            _orchestrator = null!;
            _themeService = null!;
            _logger = null;
            InitializeComponent();
        }

        public BaiduPanAnalysisForm(
            IBaiduPanAnalysisOrchestrator orchestrator,
            string directoryPath,
            IThemeService themeService,
            ILogger<BaiduPanAnalysisForm>? logger = null,
            IAIPanelPopupService? aiPanelPopupService = null,
            AiConfig? aiConfig = null,
            IPanAnalysisPromptBuilder? promptBuilder = null)
        {
            _orchestrator = orchestrator;
            _directoryPath = directoryPath;
            _themeService = themeService;
            _logger = logger;
            _aiPanelPopupService = aiPanelPopupService;
            _aiConfig = aiConfig;
            _promptBuilder = promptBuilder;
            InitializeComponent();

            // 运行时初始化（依赖构造参数，需放在 InitializeComponent 之后）
            txtPath.Text = _directoryPath;
            AppendLog($"准备分析目录：{_directoryPath}");
            AppendLog("点击「开始分析」获取目录快照并生成整理建议。");

            _themeService?.RegisterThemeable(this);
            ApplyTheme(_themeService?.CurrentColors ?? ThemeService.GetColors(ThemeMode.Light));

            // 窗体关闭时取消进行中的分析/执行，避免关闭后续集访问已释放控件
            FormClosing += BaiduPanAnalysisForm_FormClosing;
        }

        #region 事件处理

        private void BaiduPanAnalysisForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _isClosing = true;
            try
            {
                _cts?.Cancel();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "关闭窗体时取消分析任务失败");
            }
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            try
            {
                _cts?.Cancel();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "取消分析任务失败");
            }
        }

        /// <summary>
        /// 窗体加载后安全设置 SplitContainer 分隔位置，
        /// 避免初始化阶段宽度为默认值(150)时 150 - Panel2MinSize &lt; 0 越界抛出 InvalidOperationException
        /// </summary>
        private void BaiduPanAnalysisForm_Load(object? sender, EventArgs e)
        {
            try
            {
                var maxDistance = contentPanel.Width - contentPanel.Panel2MinSize - contentPanel.SplitterWidth;
                contentPanel.SplitterDistance = Math.Max(contentPanel.Panel1MinSize,
                    Math.Min(320, maxDistance));
            }
            catch
            {
                // 布局尺寸异常时保持默认分隔位置
            }
        }

        private async void btnStartAnalysis_Click(object sender, EventArgs e)
        {
            try
            {
                if (_orchestrator == null)
                {
                    AppendLog("错误：分析服务未初始化（Orchestrator 为 null）。");
                    MessageBox.Show("分析服务未初始化，无法开始分析。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                await RunAnalysisFlowAsync(_directoryPath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "开始分析时发生未预期异常");
                AppendLog($"未预期错误：{ex.Message}");
                MessageBox.Show($"分析失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 完整的分析流程（获取快照 → 本地统计 → AI 分析 → 展示结果）。
        /// 供「开始分析」按钮与子目录深入分析/返回上级共用。
        /// </summary>
        private async Task RunAnalysisFlowAsync(string path)
        {
            _cts = new CancellationTokenSource();
            _currentAnalysisPath = path;
            if (string.IsNullOrEmpty(_rootAnalysisPath))
                _rootAnalysisPath = path;

            // 禁用按钮、清空上次结果
            btnStartAnalysis.Enabled = false;
            btnCancel.Enabled = true;
            btnExecute.Enabled = false;
            lstRecommendations.Items.Clear();
            txtSummary.Clear();
            txtLog.Clear();
            txtPath.Text = path;
            UpdateGoUpButton();

            var progress = new Progress<PanAnalysisProgress>(UpdateProgress);

            try
            {
                var options = new AnalysisOptions
                {
                    MaxDepth = ParseDepth(),
                    DetectDuplicates = chkDetectDuplicates.Checked,
                    DetectJunkFiles = true,
                    UseCache = chkUseCache.Checked
                };

                // 获取快照
                AppendLog($"正在获取目录快照：{path}");
                _snapshot = await _orchestrator.GetSnapshotAsync(
                    path, options, progress, _cts.Token);
                if (IsDisposed || _isClosing) return;

                // 展示目录树（供子目录深入分析）
                BuildFolderTree();

                // 截断提示
                if (!_snapshot.IsComplete && _snapshot.Scope.MaxFileCount > 0)
                {
                    AppendLog($"⚠️ 文件数已达上限 {_snapshot.Scope.MaxFileCount:N0}，快照被截断（仅分析部分数据，可对子目录逐一深入分析）");
                }

                // 检查 AI API Key 是否配置
                var apiKey = _aiConfig?.ApiKey ?? string.Empty;
                if (string.IsNullOrEmpty(apiKey) || apiKey.Length <= 10)
                {
                    AppendLog("AI 分析不可用（未配置 API Key）");
                    DisplayStatistics(_snapshot.Statistics);
                    ShowWebAiFallback();
                    AppendLog("分析完成（仅本地统计，无 AI 建议）");
                    btnExecute.Enabled = false;
                    return;
                }

                // AI 分析
                AppendLog("正在调用 AI 进行分析...");
                _analysisResult = await _orchestrator.AnalyzeDirectoryAsync(
                    path, options, progress, _cts.Token);
                if (IsDisposed || _isClosing) return;

                // 展示结果
                DisplayStatistics(_snapshot.Statistics);
                DisplayRecommendations(_analysisResult?.Recommendations);
                txtSummary.Text = _analysisResult?.Summary ?? string.Empty;
                AppendLog($"分析完成：共 {(_analysisResult?.Recommendations?.Count ?? 0)} 条建议，耗时 {_analysisResult?.AnalysisDuration?.TotalSeconds:F1}s");
            }
            catch (OperationCanceledException)
            {
                AppendLog("分析已取消");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "百度网盘 AI 分析失败，目录: {Path}", path);
                AppendLog($"错误：{ex.Message}");

                // AI 服务不可用时，提供网页版替代方案
                if (ex.Message.Contains("所有AI服务都不可用"))
                {
                    ShowWebAiFallback();
                }
                else
                {
                    MessageBox.Show($"分析失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                // 窗体可能已关闭，重置按钮状态需先判断并包裹异常，避免 async void 内异常导致进程崩溃
                if (!IsDisposed)
                {
                    try
                    {
                        btnStartAnalysis.Enabled = true;
                        btnCancel.Enabled = false;
                        btnExecute.Enabled = lstRecommendations.Items.Count > 0;
                        UpdateGoUpButton();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "重置分析按钮状态失败");
                    }
                }
                _cts?.Dispose();
                _cts = null;
            }
        }

        /// <summary>根据当前分析路径更新「返回上级」按钮可用性</summary>
        private void UpdateGoUpButton()
        {
            if (btnGoUp == null) return;
            btnGoUp.Enabled = !string.IsNullOrEmpty(_currentAnalysisPath)
                              && _currentAnalysisPath != _rootAnalysisPath
                              && !string.IsNullOrEmpty(GetParentApiPath(_currentAnalysisPath));
        }

        /// <summary>返回当前目录的上一级（网盘 API 路径，根级时返回自身）</summary>
        private static string GetParentApiPath(string path)
        {
            var trimmed = path.TrimEnd('/');
            var idx = trimmed.LastIndexOf('/');
            if (idx <= 0) return trimmed; // 已是顶层
            return trimmed.Substring(0, idx);
        }

        /// <summary>返回文件相对路径的直接父目录（"" 表示根目录）</summary>
        private static string GetParentPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return "";
            var idx = relativePath.LastIndexOf('/');
            return idx >= 0 ? relativePath.Substring(0, idx) : "";
        }

        private async void btnGoUp_Click(object sender, EventArgs e)
        {
            try
            {
                if (_orchestrator == null || string.IsNullOrEmpty(_currentAnalysisPath)) return;
                var parent = GetParentApiPath(_currentAnalysisPath);
                if (parent == _currentAnalysisPath || string.IsNullOrEmpty(parent)) return;
                await RunAnalysisFlowAsync(parent);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "返回上级目录分析失败");
                AppendLog($"错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 双击目录树节点 → 对指定目录进行深入分析（渐进式分析）。
        /// 子目录节点 = 深入子目录；根节点 = 重新分析当前快照根目录。
        /// </summary>
        private async void treeFolders_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            try
            {
                if (_orchestrator == null) return;

                string? targetPath = null;
                var isSubFolder = false;
                if (e.Node?.Tag is PanFolderInfo folder)
                {
                    targetPath = folder.Path;
                    isSubFolder = true;
                }
                else if (e.Node?.Tag is string rootPath && !string.IsNullOrEmpty(rootPath))
                {
                    targetPath = rootPath;
                }

                if (string.IsNullOrEmpty(targetPath)) return;

                var confirm = MessageBox.Show(
                    $"将针对该目录进行分析：\n{targetPath}\n\n" +
                    (isSubFolder ? "（子目录深入分析，文件数受上限控制，避免大目录拉取过慢）" : "（重新分析当前目录）"),
                    "分析该目录",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes) return;

                await RunAnalysisFlowAsync(targetPath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "深入分析子目录失败");
                AppendLog($"错误：{ex.Message}");
            }
        }

        private async void btnExecute_Click(object sender, EventArgs e)
        {
            try
            {
                if (_orchestrator == null)
                {
                    MessageBox.Show("分析服务未初始化，无法执行操作。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var selected = GetSelectedRecommendations();
                if (!selected.Any())
                {
                    MessageBox.Show("请至少选择一项操作", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var summary = $"将执行以下操作：\n" +
                              $"- 删除：{selected.Count(r => r.Type == PanRecommendationType.Delete)} 项\n" +
                              $"- 移动：{selected.Count(r => r.Type == PanRecommendationType.Move)} 项\n" +
                              $"- 重命名：{selected.Count(r => r.Type == PanRecommendationType.Rename)} 项\n\n" +
                              $"删除操作会进入回收站（可恢复），确认执行？";

                if (MessageBox.Show(summary, "执行确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                _cts = new CancellationTokenSource();
                btnExecute.Enabled = false;
                btnStartAnalysis.Enabled = false;
                btnCancel.Enabled = true;

                var progress = new Progress<PanAnalysisProgress>(UpdateProgress);

                try
                {
                    var report = await _orchestrator.ExecuteRecommendationsAsync(
                        selected, progress, _cts.Token);
                    if (IsDisposed || _isClosing) return;

                    AppendLog($"执行完成：成功 {report.Succeeded}，失败 {report.Failed}，跳过 {report.Skipped}");
                    if (report.Failed > 0)
                    {
                        foreach (var failed in report.Results.Where(r => !r.Success))
                        {
                            AppendLog($"  ✗ {failed.Recommendation.TargetName}: {failed.ErrorMessage}");
                        }
                    }
                    MessageBox.Show(report.Summary, "执行结果", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    ExecutedAny = report.Succeeded > 0;
                }
                catch (OperationCanceledException)
                {
                    AppendLog("执行已取消");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "百度网盘执行整理操作失败");
                    AppendLog($"执行错误：{ex.Message}");
                    MessageBox.Show($"执行失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "执行操作时发生未预期异常");
                MessageBox.Show($"执行失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed)
                {
                    try
                    {
                        btnExecute.Enabled = true;
                        btnStartAnalysis.Enabled = true;
                        btnCancel.Enabled = false;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "重置执行按钮状态失败");
                    }
                }
                _cts?.Dispose();
                _cts = null;
            }
        }

        #endregion

        #region 展示与辅助

        private void UpdateProgress(PanAnalysisProgress progress)
        {
            if (IsDisposed || _isClosing) return;
            try
            {
                var phaseName = progress.Phase switch
                {
                    PanAnalysisPhase.Initializing => "初始化",
                    PanAnalysisPhase.Fetching => "获取文件",
                    PanAnalysisPhase.PreComputing => "预计算",
                    PanAnalysisPhase.Analyzing => "AI 分析",
                    PanAnalysisPhase.Executing => "执行",
                    PanAnalysisPhase.Completed => "完成",
                    PanAnalysisPhase.Failed => "失败",
                    _ => progress.Phase.ToString()
                };
                AppendLog($"[{phaseName}] {progress.Message}");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "更新分析进度失败");
            }
        }

        /// <summary>
        /// 安全解析分析深度，避免 ComboBox 无选中项时强转崩溃。
        /// </summary>
        private int ParseDepth()
        {
            try
            {
                if (cmbDepth.SelectedItem is int depth) return depth;
                if (int.TryParse(cmbDepth.SelectedItem?.ToString(), out var parsed)) return parsed;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "解析分析深度失败，使用默认值 2");
            }
            return 2;
        }

        private void DisplayStatistics(PanStatistics? stats)
        {
            if (stats == null) return;
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("===== 目录统计 =====");
                if (_snapshot is { IsComplete: false, Scope.MaxFileCount: > 0 })
                {
                    sb.AppendLine($"⚠️ 文件数已达上限 {_snapshot.Scope.MaxFileCount:N0}，以下统计仅基于已遍历的部分文件");
                    sb.AppendLine();
                }
                sb.AppendLine($"文件数：{stats.TotalFileCount:N0}");
                sb.AppendLine($"文件夹数：{stats.TotalFolderCount:N0}");
                sb.AppendLine($"总大小：{stats.TotalSizeFormatted}");
                sb.AppendLine($"无意义文件：{stats.JunkFileCount} 个（{stats.JunkSizeFormatted}）");
                sb.AppendLine($"大文件（>100MB）：{stats.LargeFiles.Count} 个");
                sb.AppendLine($"0 字节文件：{stats.ZeroByteFiles.Count} 个");
                sb.AppendLine();
                sb.AppendLine("===== 文件类型分布 =====");
                foreach (var kv in stats.CountByExtension.OrderByDescending(k => k.Value).Take(15))
                {
                    var size = stats.SizeByExtension.GetValueOrDefault(kv.Key, 0);
                    sb.AppendLine($"{kv.Key}: {kv.Value} 个（{FormatSize(size)}）");
                }
                sb.AppendLine();
                sb.AppendLine($"===== 重复文件组（{_snapshot?.Duplicates.Count ?? 0} 组） =====");
                if (_snapshot != null)
                {
                    foreach (var group in _snapshot.Duplicates.Take(20))
                    {
                        sb.AppendLine($"[{group.DisplayName}] 副本×{group.FileCount}（{group.SizeFormatted}）");
                    }
                }
                txtLog.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "展示目录统计失败");
            }
        }

        /// <summary>
        /// 根据当前快照的目录信息构建目录树，双击子目录节点可深入分析（渐进式分析）。
        /// </summary>
        private void BuildFolderTree()
        {
            try
            {
                treeFolders.BeginUpdate();
                treeFolders.Nodes.Clear();
                if (_snapshot == null)
                {
                    treeFolders.EndUpdate();
                    return;
                }

                var rootLabel = _snapshot.DirectoryPath.TrimEnd('/');
                rootLabel = rootLabel.Substring(rootLabel.LastIndexOf('/') + 1);
                if (string.IsNullOrEmpty(rootLabel)) rootLabel = "/";

                var rootNode = new TreeNode(
                    $"{rootLabel}  ({_snapshot.Statistics.TotalFileCount:N0} 个文件, {_snapshot.Statistics.TotalSizeFormatted})");
                rootNode.Tag = _snapshot.DirectoryPath; // 根节点 Tag 存路径，双击可重新分析
                treeFolders.Nodes.Add(rootNode);

                // 相对路径 -> 节点 映射（父目录先于子目录，Folders 已按 Depth 升序）
                var nodeMap = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase)
                {
                    [""] = rootNode
                };

                foreach (var folder in _snapshot.Folders
                             .OrderBy(f => f.Depth)
                             .ThenBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase))
                {
                    var parentRel = GetParentPath(folder.RelativePath);
                    if (!nodeMap.TryGetValue(parentRel, out var parentNode))
                        continue;

                    var node = new TreeNode(folder.Name);
                    node.Tag = folder; // 子目录节点 Tag 存 PanFolderInfo
                    parentNode.Nodes.Add(node);
                    nodeMap[folder.RelativePath] = node;
                }

                rootNode.Expand();
                treeFolders.EndUpdate();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "构建目录树失败");
                try { treeFolders.EndUpdate(); } catch { /* 忽略 */ }
            }
        }

        private void DisplayRecommendations(List<PanRecommendation>? recommendations)
        {
            if (recommendations == null) return;
            try
            {
                lstRecommendations.BeginUpdate();
                lstRecommendations.Items.Clear();
                foreach (var rec in recommendations)
                {
                    var item = new ListViewItem(rec.IsSelected ? "☑" : "☐");
                    item.SubItems.Add(rec.TypeDisplay);
                    item.SubItems.Add(rec.PriorityDisplay);
                    item.SubItems.Add(rec.TargetName);
                    var target = rec.Type switch
                    {
                        PanRecommendationType.Move => rec.DestinationPath,
                        PanRecommendationType.Rename => rec.NewName,
                        PanRecommendationType.MergeFolder => rec.DestinationPath,
                        _ => "-"
                    };
                    item.SubItems.Add(target ?? "-");
                    item.SubItems.Add(rec.Reason);
                    item.Tag = rec;
                    item.Checked = rec.IsSelected;
                    lstRecommendations.Items.Add(item);
                }
                lstRecommendations.EndUpdate();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "展示分析建议失败");
                try { lstRecommendations.EndUpdate(); } catch { /* 忽略 */ }
            }
        }

        private List<PanRecommendation> GetSelectedRecommendations()
        {
            var result = new List<PanRecommendation>();
            try
            {
                foreach (ListViewItem item in lstRecommendations.Items)
                {
                    if (item.Checked && item.Tag is PanRecommendation rec)
                    {
                        rec.IsSelected = true;
                        result.Add(rec);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "读取选中建议失败");
            }
            return result;
        }

        private void AppendLog(string message)
        {
            if (txtLog == null || txtLog.IsDisposed) return;
            var time = DateTime.Now.ToString("HH:mm:ss");
            if (txtLog.TextLength > 20000)
                txtLog.Clear();
            txtLog.AppendText($"[{time}] {message}\r\n");
        }

        private static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// 当 AI 服务不可用时，提供网页版 AI 手动分析的替代方案
        /// </summary>
        private void ShowWebAiFallback()
        {
            if (IsDisposed || _isClosing) return;

            try
            {
                var useWebAi = MessageBox.Show(
                    "AI 自动分析不可用（未配置 API Key 或所有 AI 服务均调用失败）。\n\n" +
                    "是否使用网页版 AI 手动分析？\n" +
                    "（将打开 AI 网页，您可手动粘贴目录信息进行分析）",
                    "AI 服务不可用",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (useWebAi == DialogResult.Yes && _snapshot != null && _aiPanelPopupService != null)
                {
                    var context = BuildSnapshotContext(_snapshot);
                    var prompt = "请帮我分析以下百度网盘目录结构，针对文件整理、分类、清理等给出具体建议：";
                    // 网页版 AI 地址跟随用户当前配置的 provider，而非固定使用豆包
                    var provider = _aiConfig?.Provider ?? "doubao";
                    var aiUrl = AiConfig.Providers.GetValueOrDefault(provider)?.WebViewUrl
                                ?? "https://www.doubao.com/chat";
                    _aiPanelPopupService.ShowAIAbilityPanel(this, prompt, aiUrl, context);
                    AppendLog("已打开网页版 AI 面板，可手动粘贴目录信息进行分析");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "打开网页版 AI 面板失败");
                MessageBox.Show($"打开网页版 AI 面板失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 将目录快照构造成可供网页版 AI 分析的文字上下文。
        /// 优先复用 PanAnalysisPromptBuilder 的上下文构建逻辑，避免重复实现；
        /// 未注入时回退到本地实现。
        /// </summary>
        private string BuildSnapshotContext(PanDirectorySnapshot? snapshot)
        {
            if (snapshot == null) return string.Empty;

            try
            {
                if (_promptBuilder != null)
                {
                    return _promptBuilder.BuildUserPrompt(snapshot);
                }

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"目录：{snapshot.DirectoryPath}");
                sb.AppendLine($"文件数：{snapshot.Statistics?.TotalFileCount ?? 0:N0}");
                sb.AppendLine($"文件夹数：{snapshot.Statistics?.TotalFolderCount ?? 0:N0}");
                sb.AppendLine($"总大小：{snapshot.Statistics?.TotalSizeFormatted ?? "-"}");
                if (!snapshot.IsComplete && snapshot.Scope.MaxFileCount > 0)
                    sb.AppendLine($"⚠️ 文件数已达上限 {snapshot.Scope.MaxFileCount:N0}，快照被截断，仅为基础分析样本");
                sb.AppendLine();

                // 目录结构（分层）
                sb.AppendLine("【目录结构】");
                sb.AppendLine(BuildFallbackDirectoryTree(snapshot));
                sb.AppendLine();

                // 文件类型分布
                if (snapshot.Statistics?.CountByExtension is { Count: > 0 } extCount)
                {
                    sb.AppendLine("【文件类型分布】");
                    foreach (var kv in extCount.OrderByDescending(k => k.Value).Take(15))
                    {
                        var size = snapshot.Statistics.SizeByExtension.GetValueOrDefault(kv.Key, 0);
                        sb.AppendLine($"{kv.Key}: {kv.Value} 个（{FormatSize(size)}）");
                    }
                    sb.AppendLine();
                }

                // 重复文件组
                if (snapshot.Duplicates is { Count: > 0 })
                {
                    sb.AppendLine($"【重复文件组（{snapshot.Duplicates.Count} 组）】");
                    foreach (var group in snapshot.Duplicates.Take(20))
                    {
                        sb.AppendLine($"[{group.DisplayName}] 副本×{group.FileCount}（{group.SizeFormatted}）");
                        foreach (var file in group.Files.Take(10))
                        {
                            sb.AppendLine($"  - {file.RelativePath}（{file.SizeFormatted}）");
                        }
                        if (group.Files.Count > 10)
                            sb.AppendLine($"  - ... 等共 {group.Files.Count} 个");
                    }
                    sb.AppendLine();
                }

                // 重点关注文件：大文件 + 可疑文件
                sb.AppendLine("【重点关注文件】");
                foreach (var file in snapshot.Files.Where(f => !f.IsFolder).OrderByDescending(f => f.SizeBytes).Take(30))
                    sb.AppendLine($"- 大文件：{file.RelativePath}（{file.SizeFormatted}）");
                foreach (var file in snapshot.Files
                             .Where(f => !f.IsFolder && (f.IsJunkFile || f.IsPotentialDuplicate))
                             .Take(50))
                    sb.AppendLine($"- {(file.IsJunkFile ? "无意义" : "疑似重复")}：{file.RelativePath}（{file.SizeFormatted}）");

                // 小目录时附加完整文件列表
                if (snapshot.Files.Count <= 200)
                {
                    sb.AppendLine();
                    sb.AppendLine($"【文件列表（共 {snapshot.Files.Count} 个）】");
                    foreach (var file in snapshot.Files.Take(200))
                    {
                        sb.AppendLine($"{file.RelativePath} | {file.SizeFormatted} | {file.CategoryName}");
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "构建网页版 AI 上下文失败");
                return snapshot.DirectoryPath;
            }
        }

        /// <summary>
        /// 构建网页版 AI 兜底用的目录树（带各目录文件数与大小），与 AI Prompt 的目录结构保持一致。
        /// </summary>
        private string BuildFallbackDirectoryTree(PanDirectorySnapshot snapshot)
        {
            const int maxFoldersShown = 150;
            var sb = new System.Text.StringBuilder();

            var rootName = snapshot.DirectoryPath.TrimEnd('/');
            var rootLabel = rootName.Substring(rootName.LastIndexOf('/') + 1);
            if (string.IsNullOrEmpty(rootLabel)) rootLabel = "/";

            // 汇总每个目录下直接文件数与大小
            var dirAgg = new Dictionary<string, (int Count, long Size)>();
            foreach (var file in snapshot.Files.Where(f => !f.IsFolder))
            {
                var parent = GetParentPath(file.RelativePath);
                var cur = dirAgg.GetValueOrDefault(parent);
                dirAgg[parent] = (cur.Count + 1, cur.Size + file.SizeBytes);
            }

            var rootStat = dirAgg.GetValueOrDefault("");
            sb.AppendLine($"{rootLabel}/  ({rootStat.Count:N0} 个文件, {FormatSize(rootStat.Size)})");

            foreach (var folder in snapshot.Folders
                         .Where(f => !string.IsNullOrEmpty(f.RelativePath))
                         .OrderBy(f => f.Depth)
                         .ThenBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
                         .Take(maxFoldersShown))
            {
                var stat = dirAgg.GetValueOrDefault(folder.RelativePath);
                var indent = new string(' ', Math.Max(0, folder.Depth - 1) * 2);
                sb.AppendLine($"{indent}└─ {folder.Name}/  ({stat.Count:N0} 个文件, {FormatSize(stat.Size)})");
            }

            if (snapshot.Folders.Count > maxFoldersShown)
                sb.AppendLine($"... 还有 {snapshot.Folders.Count - maxFoldersShown} 个子目录未展示");

            return sb.ToString();
        }

        #endregion

        #region 主题

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;
            ForeColor = colors.TextPrimary;

            if (txtLog != null) txtLog.BackColor = colors.Background;
            if (txtSummary != null)
            {
                txtSummary.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : SystemColors.Info;
                txtSummary.ForeColor = colors.TextPrimary;
            }
            if (lstRecommendations != null)
            {
                lstRecommendations.BackColor = colors.Background;
                lstRecommendations.ForeColor = colors.TextPrimary;
            }
            if (cmbDepth != null) cmbDepth.BackColor = colors.Background;
            if (txtPath != null) txtPath.BackColor = colors.Surface;
            if (treeFolders != null)
            {
                treeFolders.BackColor = colors.Background;
                treeFolders.ForeColor = colors.TextPrimary;
            }
        }

        #endregion
    }
}
