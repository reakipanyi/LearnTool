using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Config;
using LearningAssistant.Models.PanAnalysis;
using LearningAssistant.Services;
using LearningAssistant.Services.Cloud;
using LearningAssistant.Services.PanAnalysis;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Forms.Pdf
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
        private readonly ICloudStorageService? _cloudStorageService;
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

        /// <summary>当前打标的完整列表（未筛选），供筛选/重置使用</summary>
        private List<PanFileTag> _allFileTags = new();

        /// <summary>ToolTip 组件已加入 components 容器的标记（避免后续重复绑定）</summary>
        private static bool components_ForToolTipInitialized;

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
            IPanAnalysisPromptBuilder? promptBuilder = null,
            ICloudStorageService? cloudStorageService = null)
        {
            _orchestrator = orchestrator;
            _directoryPath = directoryPath;
            _themeService = themeService;
            _logger = logger;
            _aiPanelPopupService = aiPanelPopupService;
            _aiConfig = aiConfig;
            _promptBuilder = promptBuilder;
            _cloudStorageService = cloudStorageService;
            InitializeComponent();

            // 运行时初始化（依赖构造参数，需放在 InitializeComponent 之后）
            // ---- 打开整理工具快捷键 + ToolTip 悬停提示（P0.1 补修：Ctrl+Shift+O 全局有效）----
            var organizerToolTip = new ToolTip
            {
                AutoPopDelay = 3000,
                InitialDelay = 400,
                ReshowDelay = 200,
                ShowAlways = true
            };
            organizerToolTip.SetToolTip(btnOpenOrganizer, "快捷键：Ctrl + Shift + O（全局有效，任意焦点下按此组合键打开整理工具）");
            if (!components_ForToolTipInitialized)
            {
                // ToolTip 组件需随窗体释放，挂到 Dispose 链上
                components = components ?? new System.ComponentModel.Container();
                components.Add(organizerToolTip);
                components_ForToolTipInitialized = true;
            }

            txtPath.Text = _directoryPath;
            AppendLog($"准备分析目录：{_directoryPath}");
            AppendLog("点击「开始分析」获取目录快照并生成整理建议。");

            InitRecommendationsList();
            InitFileTagsList();

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
        /// 打开网盘整理工具（PanOrganizerForm 骨架 v0.1 版）。
        /// P0.1 仅展示双栏浏览 + 预览前 5 条 AI 建议，不执行实际操作。
        /// </summary>
        private void btnOpenOrganizer_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_orchestrator == null || _themeService == null)
                {
                    MessageBox.Show("整理工具需要先初始化分析服务。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // P1 优化：不再拦截 _snapshot == null——允许空模式打开，PanOrganizerForm 内部提供「输入路径拉取快照」按钮
                List<PanRecommendation> recs;
                if (_analysisResult?.Recommendations != null)
                    recs = _analysisResult.Recommendations.ToList();
                else
                    recs = new List<PanRecommendation>();

                var organizer = new PanOrganizerForm(
                    orchestrator: _orchestrator,
                    initialSnapshot: _snapshot,
                    initialRecommendations: recs,
                    themeService: _themeService,
                    logger: null as ILogger<PanOrganizerForm>);   // 不同泛型，传 null 即可（不会崩，P0.4 再做专用日志）

                organizer.FormClosed += (_, args) =>
                {
                    if (organizer.ExecutedAny)
                        AppendLog("📌 整理工具执行过操作，建议重新分析以刷新快照。");
                };
                organizer.Show(this);   // 非模态，可同时看 AI 建议与整理窗口
                if (_snapshot != null)
                    AppendLog($"🧰 已打开网盘整理工具（快照：{_snapshot.DirectoryPath}，AI 建议 {recs.Count} 条预览）");
                else
                    AppendLog("🧰 已打开网盘整理工具（空模式：请在整理工具内点击「📥 拉取快照」开始整理）");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "打开整理工具 PanOrganizerForm 失败");
                MessageBox.Show($"打开整理工具失败：{ex}\n\n--- InnerException ---\n{ex.InnerException}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 覆写消息循环：捕获「打开整理工具」快捷键 Ctrl + Shift + O（O=Organizer，语义清晰）。
        /// 全局 Form 级生效：即使焦点在 TextBox/ListView 上也能触发。
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // 捕获 Ctrl + Shift + O  → 打开整理工具
            if (keyData == (Keys.Control | Keys.Shift | Keys.O))
            {
                try
                {
                    if (btnOpenOrganizer != null && btnOpenOrganizer.Enabled && !btnOpenOrganizer.IsDisposed)
                    {
                        btnOpenOrganizer.PerformClick();
                        return true;   // 已处理，不再向下传递
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "快捷键 Ctrl+Shift+O 触发打开整理工具失败");
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
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
            lstFileTags.Items.Clear();
            _allFileTags = new List<PanFileTag>();
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

                // 更新「AI 发送内容」标签页（指令 + 目录内容，供手动复制到 AI 网页）
                UpdateAiPayloadTab();

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
                DisplayFileTags(_analysisResult?.FileTags);
                txtSummary.Text = _analysisResult?.Summary ?? string.Empty;
                AppendLog($"分析完成：共 {(_analysisResult?.Recommendations?.Count ?? 0)} 条建议，{(_analysisResult?.FileTags?.Count ?? 0)} 个文件打标，耗时 {_analysisResult?.AnalysisDuration?.TotalSeconds:F1}s");
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

        #region 文件打标（展示 + 筛选 + 批量整理）

        /// <summary>初始化整理建议列表的列（在构造函数中调用一次）</summary>
        private void InitRecommendationsList()
        {
            try
            {
                lstRecommendations.Columns.Clear();
                lstRecommendations.Columns.Add("选择", 40);
                lstRecommendations.Columns.Add("操作", 70);
                lstRecommendations.Columns.Add("优先级", 60);
                lstRecommendations.Columns.Add("文件名", 150);
                lstRecommendations.Columns.Add("目标", 120);
                lstRecommendations.Columns.Add("原因", 300);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "初始化整理建议列表失败");
            }
        }

        /// <summary>初始化打标列表的列（在构造函数中调用一次）</summary>
        private void InitFileTagsList()
        {
            try
            {
                lstFileTags.Columns.Clear();
                lstFileTags.Columns.Add("科目", 70);
                lstFileTags.Columns.Add("价值观", 60);
                lstFileTags.Columns.Add("年龄段", 70);
                lstFileTags.Columns.Add("质量", 50);
                lstFileTags.Columns.Add("文件名", 150);
                lstFileTags.Columns.Add("内容摘要", 160);
                lstFileTags.Columns.Add("同类对比", 180);
                lstFileTags.Columns.Add("路径", 220);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "初始化文件打标列表失败");
            }
        }

        /// <summary>展示 AI 打标结果，并填充筛选下拉框</summary>
        private void DisplayFileTags(List<PanFileTag>? tags)
        {
            if (tags == null) return;
            try
            {
                _allFileTags = tags;

                // 填充筛选下拉框（去重、保持出现顺序）
                PopulateTagFilter(cmbTagSubject, tags.Select(t => t.Subject));
                PopulateTagFilter(cmbTagValues, tags.Select(t => t.ValuesOrientation));
                PopulateTagFilter(cmbTagAge, tags.Select(t => t.AgeRange));

                ApplyTagFilter();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "展示文件打标结果失败");
            }
        }

        /// <summary>填充单个筛选下拉框：首项「全部」，后续为去重后的取值（保留出现顺序）</summary>
        private static void PopulateTagFilter(ComboBox combo, IEnumerable<string> values)
        {
            var distinct = values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct()
                .ToList();
            combo.BeginUpdate();
            combo.Items.Clear();
            combo.Items.Add("全部");
            foreach (var v in distinct)
                combo.Items.Add(v);
            combo.SelectedIndex = 0;
            combo.EndUpdate();
        }

        /// <summary>按当前筛选条件刷新打标列表</summary>
        private void ApplyTagFilter()
        {
            try
            {
                var subject = cmbTagSubject.SelectedItem?.ToString() ?? "全部";
                var values = cmbTagValues.SelectedItem?.ToString() ?? "全部";
                var age = cmbTagAge.SelectedItem?.ToString() ?? "全部";

                var filtered = _allFileTags
                    .Where(t =>
                        (subject == "全部" || string.Equals(t.Subject, subject, StringComparison.OrdinalIgnoreCase)) &&
                        (values == "全部" || string.Equals(t.ValuesOrientation, values, StringComparison.OrdinalIgnoreCase)) &&
                        (age == "全部" || string.Equals(t.AgeRange, age, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                lstFileTags.BeginUpdate();
                lstFileTags.Items.Clear();
                foreach (var tag in filtered)
                {
                    var item = new ListViewItem(tag.Subject);
                    item.SubItems.Add(tag.ValuesOrientation);
                    item.SubItems.Add(tag.AgeRange);
                    item.SubItems.Add(tag.Quality);
                    item.SubItems.Add(tag.TargetName);
                    item.SubItems.Add(tag.ContentSummary);
                    item.SubItems.Add(tag.ComparisonNote);
                    item.SubItems.Add(tag.TargetPath);
                    item.Tag = tag;
                    item.Checked = tag.IsSelected;
                    lstFileTags.Items.Add(item);
                }
                lstFileTags.EndUpdate();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "应用文件打标筛选失败");
                try { lstFileTags.EndUpdate(); } catch { /* 忽略 */ }
            }
        }

        private void btnTagFilter_Click(object? sender, EventArgs e)
        {
            ApplyTagFilter();
        }

        private void btnTagReset_Click(object? sender, EventArgs e)
        {
            try
            {
                if (cmbTagSubject.Items.Count > 0) cmbTagSubject.SelectedIndex = 0;
                if (cmbTagValues.Items.Count > 0) cmbTagValues.SelectedIndex = 0;
                if (cmbTagAge.Items.Count > 0) cmbTagAge.SelectedIndex = 0;
                ApplyTagFilter();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "重置文件打标筛选失败");
            }
        }

        /// <summary>读取打标列表中勾选的项</summary>
        private List<PanFileTag> GetSelectedFileTags()
        {
            var result = new List<PanFileTag>();
            try
            {
                foreach (ListViewItem item in lstFileTags.Items)
                {
                    if (item.Checked && item.Tag is PanFileTag tag)
                    {
                        tag.IsSelected = true;
                        result.Add(tag);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "读取选中打标文件失败");
            }
            return result;
        }

        /// <summary>将打标文件转为删除/移动的推荐操作并执行</summary>
        private async Task ExecuteTaggedActionsAsync(
            List<PanFileTag> tags,
            PanRecommendationType type,
            string? destinationPath)
        {
            if (_orchestrator == null || !tags.Any()) return;

            var recommendations = tags.Select(t => new PanRecommendation
            {
                Type = type,
                TargetPath = t.TargetPath,
                TargetName = t.TargetName,
                DestinationPath = destinationPath,
                Reason = $"文件打标批量整理（{t.Subject}/{t.ValuesOrientation}/{t.AgeRange}）",
                Priority = PanPriority.Medium,
                AffectedFileId = t.Id
            }).ToList();

            _cts = new CancellationTokenSource();
            btnDeleteTagged.Enabled = false;
            btnMoveTagged.Enabled = false;
            btnExecute.Enabled = false;
            btnStartAnalysis.Enabled = false;
            btnCancel.Enabled = true;

            var progress = new Progress<PanAnalysisProgress>(UpdateProgress);

            try
            {
                var report = await _orchestrator.ExecuteRecommendationsAsync(
                    recommendations, progress, _cts.Token);
                if (IsDisposed || _isClosing) return;

                AppendLog($"打标批量整理完成：成功 {report.Succeeded}，失败 {report.Failed}，跳过 {report.Skipped}");
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
                _logger?.LogError(ex, "百度网盘打标批量整理失败");
                AppendLog($"执行错误：{ex.Message}");
                MessageBox.Show($"执行失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed)
                {
                    try
                    {
                        btnDeleteTagged.Enabled = true;
                        btnMoveTagged.Enabled = true;
                        btnExecute.Enabled = lstRecommendations.Items.Count > 0;
                        btnStartAnalysis.Enabled = true;
                        btnCancel.Enabled = false;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "重置打标批量整理按钮状态失败");
                    }
                }
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async void btnDeleteTagged_Click(object? sender, EventArgs e)
        {
            try
            {
                var selected = GetSelectedFileTags();
                if (!selected.Any())
                {
                    MessageBox.Show("请先勾选要删除的文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var confirm = MessageBox.Show(
                    $"将删除以下 {selected.Count} 个文件（进入回收站，可恢复）：\n" +
                    string.Join("\n", selected.Take(10).Select(t => $"  - {t.TargetName}")) +
                    (selected.Count > 10 ? $"\n  ... 等共 {selected.Count} 个" : "") +
                    "\n\n确认执行？",
                    "删除确认",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;

                await ExecuteTaggedActionsAsync(selected, PanRecommendationType.Delete, null);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "批量删除打标文件失败");
                MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnMoveTagged_Click(object? sender, EventArgs e)
        {
            try
            {
                var selected = GetSelectedFileTags();
                if (!selected.Any())
                {
                    MessageBox.Show("请先勾选要移动的文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var dialog = new Bookmark.InputDialog(
                    "移动到目录",
                    "请输入目标目录完整路径（以 / 结尾）：",
                    GetParentApiPath(_currentAnalysisPath) + "/");
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                var destination = dialog.InputText?.Trim();
                if (string.IsNullOrEmpty(destination))
                {
                    MessageBox.Show("目标目录不能为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!destination.EndsWith("/"))
                    destination += "/";

                var confirm = MessageBox.Show(
                    $"将移动以下 {selected.Count} 个文件到：\n{destination}\n\n确认执行？",
                    "移动确认",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;

                await ExecuteTaggedActionsAsync(selected, PanRecommendationType.Move, destination);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "批量移动打标文件失败");
                MessageBox.Show($"移动失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

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
        /// 当 AI 服务不可用时，提示用户手动使用「AI 发送内容」标签页。
        /// 不自动打开 AI 面板，由用户点击「打开 AI 面板」按钮手动发起。
        /// </summary>
        private void ShowWebAiFallback()
        {
            if (IsDisposed || _isClosing) return;

            AppendLog("AI 自动分析不可用（未配置 API Key 或所有 AI 服务均调用失败）");
            AppendLog("您可切换到「📤 AI 发送内容」标签页：复制指令与目录内容手动粘贴到 AI 网页，或点击「打开 AI 面板」手动发起分析");

            try
            {
                MessageBox.Show(
                    "AI 自动分析不可用（未配置 API Key 或所有 AI 服务均调用失败）。\n\n" +
                    "请切换到「📤 AI 发送内容」标签页，点击「打开 AI 面板」手动进行分析；\n" +
                    "或复制其中的指令与目录内容后，粘贴到浏览器 AI 网页。",
                    "AI 服务不可用",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "提示 AI 服务不可用失败");
            }
        }

        /// <summary>
        /// 生成发送给 AI 的指令提示词（供网页版 AI 面板与「AI 发送内容」标签页共用）。
        /// 优先复用 PanAnalysisPromptBuilder 的完整 System Prompt（含严格 JSON 输出格式），
        /// 保证手动粘贴到 AI 网页后返回的 JSON 可被「📥 解析AI结果」直接解析；
        /// 未注入时回退到内置提示词。
        /// </summary>
        private string GetAiInstructionPrompt()
        {
            if (_promptBuilder != null)
            {
                try
                {
                    return _promptBuilder.BuildSystemPrompt();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "构建 AI 指令失败，使用内置提示词");
                }
            }

            return "请帮我分析以下百度网盘目录结构，要求：\n" +
                   "1) 针对文件整理、分类、清理、重复文件等给出具体建议；\n" +
                   "2) 请联网检索网评（豆瓣评分、家长测评等），对能识别的教材/影视/小说等做文件打标：内容摘要、科目、价值观取向、适合年龄段、内容质量（优/良/中/差）、同类资源对比；\n" +
                   "3) 无法联网或查不到网评时，基于文件名/路径推断并在 reason 中注明。\n" +
                   "请按 JSON 返回（summary + recommendations + fileTags）。";
        }

        /// <summary>
        /// 打开网页版 AI 面板，并携带「指令 + 目录内容」供 AI 分析。
        /// </summary>
        private void OpenAiPanelWithPayload()
        {
            if (IsDisposed || _isClosing || _snapshot == null || _aiPanelPopupService == null) return;

            try
            {
                var context = BuildSnapshotContext(_snapshot);
                var prompt = GetAiInstructionPrompt();
                // 网页版 AI 地址跟随用户当前配置的 provider，而非固定使用豆包
                var provider = _aiConfig?.Provider ?? "doubao";
                var aiUrl = AiConfig.Providers.GetValueOrDefault(provider)?.WebViewUrl
                            ?? "https://www.doubao.com/chat";
                _aiPanelPopupService.ShowAIAbilityPanel(this, prompt, aiUrl, context);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "打开网页版 AI 面板失败");
            }
        }

        /// <summary>
        /// 更新「AI 发送内容」标签页：展示将发送给 AI 的指令与目录内容，供手动复制到浏览器。
        /// </summary>
        private void UpdateAiPayloadTab()
        {
            if (txtAiPayload == null || txtAiPayload.IsDisposed) return;
            try
            {
                var context = _snapshot != null ? BuildSnapshotContext(_snapshot) : string.Empty;
                txtAiPayload.Text =
                    "【发送给 AI 的指令】\r\n" + GetAiInstructionPrompt() +
                    "\r\n\r\n【目录内容（复制到 AI 网页输入框）】\r\n" + context;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "更新 AI 发送内容标签页失败");
            }
        }

        private void btnCopyAiPayload_Click(object? sender, EventArgs e)
        {
            var text = txtAiPayload?.Text ?? string.Empty;
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("AI 发送内容为空，请先完成分析。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Clipboard.SetText(text);
            AppendLog("AI 发送内容已复制到剪贴板");
        }

        private void btnOpenAiPanel_Click(object? sender, EventArgs e)
        {
            if (_snapshot == null)
            {
                MessageBox.Show("请先完成分析，再打开 AI 面板。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            OpenAiPanelWithPayload();
            AppendLog("已打开网页版 AI 面板");
        }

        #region 手动解析 AI 结果（无需 AI API）

        private void btnClearParseInput_Click(object? sender, EventArgs e)
        {
            txtAiResultInput?.Clear();
            AppendLog("已清空解析输入框");
        }

        /// <summary>
        /// 在解析输入框填入一份符合解析格式的示例 JSON，方便了解「整理建议」与「文件打标」的结构。
        /// </summary>
        private void btnSampleJson_Click(object? sender, EventArgs e)
        {
            txtAiResultInput.Text = """
            {
              "summary": "目录整体结构清晰，存在少量重复文件与无意义临时文件，建议清理后按科目/年龄段分类归档。",
              "recommendations": [
                {
                  "type": "Delete",
                  "targetPath": "/学习/教材/数学必修一_副本.pdf",
                  "destinationPath": null,
                  "newName": null,
                  "reason": "与主文件重复，建议删除进入回收站",
                  "priority": "High"
                },
                {
                  "type": "Move",
                  "targetPath": "/下载/数学练习册.pdf",
                  "destinationPath": "/学习/教材/",
                  "newName": null,
                  "reason": "文件类型与目录语义不匹配，移入教材目录",
                  "priority": "Medium"
                },
                {
                  "type": "Rename",
                  "targetPath": "/下载/新建文件夹",
                  "destinationPath": null,
                  "newName": "数学教辅整理",
                  "reason": "无意义名称，改为可辨识名称",
                  "priority": "Low"
                }
              ],
              "fileTags": [
                {
                  "targetPath": "/学习/教材/数学必修一.pdf",
                  "contentSummary": "高中数学必修一教材",
                  "subject": "数学",
                  "valuesOrientation": "中性",
                  "ageRange": "13-18",
                  "quality": "优",
                  "comparisonNote": "人教A版经典教材，内容权威",
                  "reason": "依据文件名推断，未见有效网评"
                },
                {
                  "targetPath": "/影视/动画/宝宝巴士儿歌合集.mp4",
                  "contentSummary": "低龄儿童儿歌动画合集",
                  "subject": "艺术",
                  "valuesOrientation": "积极",
                  "ageRange": "6-12",
                  "quality": "良",
                  "comparisonNote": "适合学龄前儿童，语言启蒙",
                  "reason": "依据文件名推断"
                }
              ]
            }
            """;
            AppendLog("已填入示例 JSON，可点击「解析并填充」查看效果");
        }

        /// <summary>
        /// 将手动粘贴的 AI 返回内容（JSON，可含 Markdown 代码块）解析并填充到界面：
        /// 摘要 → 摘要框；整理建议 → 建议列表；文件打标 → 打标列表与筛选下拉框。
        /// 全程不调用 AI API，保证在未配置 API Key 时也能正常工作。
        /// </summary>
        private void btnParseAiResult_Click(object? sender, EventArgs e)
        {
            try
            {
                var raw = txtAiResultInput?.Text ?? string.Empty;
                if (string.IsNullOrWhiteSpace(raw))
                {
                    MessageBox.Show("请先在下方粘贴 AI 返回的 JSON 结果。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 1. 解析 AI 结果（复用与自动分析相同的解析器，支持纯 JSON / Markdown 代码块 / 正则提取）
                var parser = new Services.PanAnalysis.PanAnalysisResultParser(
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<Services.PanAnalysis.PanAnalysisResultParser>.Instance);
                var result = parser.Parse(raw);

                if (!result.ParseSuccess)
                {
                    AppendLog($"解析 AI 结果失败：{result.ParseError ?? "无法解析为 JSON"}");
                    MessageBox.Show(
                        "解析失败：无法从粘贴内容中识别出有效的 JSON 结果。\n\n" +
                        "请确认粘贴的是 AI 返回的 JSON（可包含 Markdown 代码块），\n" +
                        "需包含字段：summary、recommendations、fileTags。\n\n" +
                        "提示：可切换到「📤 AI 发送内容」标签页复制完整指令与目录内容重新向 AI 提问。",
                        "解析失败",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                // 2. 将 AI 返回的路径与当前快照的真实文件路径对齐，保证整理/删除/移动能命中真实文件
                var reconciled = ReconcileTargetPaths(result);

                // 3. 填充界面
                _analysisResult = reconciled;
                txtSummary.Text = reconciled.Summary ?? string.Empty;
                DisplayRecommendations(reconciled.Recommendations);
                DisplayFileTags(reconciled.FileTags);
                btnExecute.Enabled = reconciled.Recommendations.Count > 0;

                AppendLog($"解析成功：{reconciled.Recommendations.Count} 条建议，{reconciled.FileTags.Count} 个文件打标，已填充到界面。");

                if (reconciled.Recommendations.Count == 0 && reconciled.FileTags.Count == 0)
                {
                    AppendLog("⚠️ 未能从粘贴内容中提取到任何建议或打标：请确认 JSON 含 recommendations / fileTags 数组，且每条含 targetPath（完整路径或文件名均可）。可点击「示例」查看格式。");
                }

                if (_snapshot == null)
                    AppendLog("提示：尚未执行「开始分析」，无法将路径映射到真实网盘文件，批量整理可能失败。");

                // 4. 自动切换到「整理建议」标签页，方便直接勾选执行
                try
                {
                    tabControl.SelectedTab = tabRecommendations;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "切换到整理建议标签页失败");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "手动解析 AI 结果失败");
                MessageBox.Show($"解析失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 将 AI 返回的 targetPath 与当前快照中的真实文件路径对齐。
        /// AI 上下文中的路径多为相对路径（甚至可能被 AI 截断/拼接），而执行删除/移动需要完整 API 路径。
        /// 依次尝试：完整路径 → 相对路径 → 路径末尾匹配 → 文件名唯一匹配。
        /// </summary>
        private PanAnalysisResult ReconcileTargetPaths(PanAnalysisResult result)
        {
            if (_snapshot == null || _snapshot.Files.Count == 0)
                return result;

            try
            {
                // 建立查找索引：完整路径 / 相对路径 / 文件名（多候选）
                var byPath = new Dictionary<string, PanFileInfo>(StringComparer.OrdinalIgnoreCase);
                var byRelative = new Dictionary<string, PanFileInfo>(StringComparer.OrdinalIgnoreCase);
                var byName = new Dictionary<string, List<PanFileInfo>>(StringComparer.OrdinalIgnoreCase);
                foreach (var file in _snapshot.Files)
                {
                    byPath[file.Path] = file;
                    if (!string.IsNullOrEmpty(file.RelativePath))
                        byRelative[file.RelativePath] = file;
                    if (!string.IsNullOrEmpty(file.Name))
                    {
                        if (!byName.TryGetValue(file.Name, out var list))
                        {
                            list = new List<PanFileInfo>();
                            byName[file.Name] = list;
                        }
                        list.Add(file);
                    }
                }

                PanFileInfo? Resolve(string? targetPath)
                {
                    if (string.IsNullOrWhiteSpace(targetPath)) return null;
                    var tp = targetPath.Trim();

                    if (byPath.TryGetValue(tp, out var f)) return f;
                    if (byRelative.TryGetValue(tp, out f)) return f;

                    // 路径末尾匹配（AI 可能带了根目录前缀或丢失前缀）
                    var normalized = tp.TrimEnd('/');
                    f = _snapshot.Files.FirstOrDefault(x =>
                        x.RelativePath.EndsWith(normalized, StringComparison.OrdinalIgnoreCase) ||
                        x.Path.EndsWith(normalized, StringComparison.OrdinalIgnoreCase));
                    if (f != null) return f;

                    // 仅文件名匹配（唯一时采用，避免误命中同名文件）
                    var name = Path.GetFileName(normalized);
                    if (!string.IsNullOrEmpty(name) &&
                        byName.TryGetValue(name, out var candidates) && candidates.Count == 1)
                        return candidates[0];

                    return null;
                }

                foreach (var rec in result.Recommendations)
                {
                    var file = Resolve(rec.TargetPath);
                    if (file != null)
                    {
                        rec.TargetPath = file.Path;
                        rec.TargetName = file.Name;
                        rec.AffectedFileId = file.FsId.ToString();
                    }
                    else if (string.IsNullOrEmpty(rec.TargetName))
                    {
                        rec.TargetName = Path.GetFileName(rec.TargetPath);
                    }
                }

                foreach (var tag in result.FileTags)
                {
                    var file = Resolve(tag.TargetPath);
                    if (file != null)
                    {
                        tag.TargetPath = file.Path;
                        tag.TargetName = file.Name;
                    }
                    else if (string.IsNullOrEmpty(tag.TargetName))
                    {
                        tag.TargetName = Path.GetFileName(tag.TargetPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "对齐 AI 结果路径失败，保留原路径");
            }

            return result;
        }

        #endregion

        private void btnCopySummary_Click(object? sender, EventArgs e)
        {
            var text = txtSummary?.Text ?? string.Empty;
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("摘要为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Clipboard.SetText(text);
            AppendLog("摘要已复制到剪贴板");
        }

        private void btnSaveSummary_Click(object? sender, EventArgs e)
        {
            var text = txtSummary?.Text ?? string.Empty;
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("摘要为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Filter = "文本文件|*.txt|所有文件|*.*",
                Title = "保存分析摘要",
                FileName = $"目录分析摘要_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                File.WriteAllText(dialog.FileName, text, System.Text.Encoding.UTF8);
                AppendLog($"摘要已保存到：{dialog.FileName}");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "保存摘要失败");
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 将摘要内容持久化并上传到网盘当前分析目录。
        /// </summary>
        private async void btnUploadSummary_Click(object? sender, EventArgs e)
        {
            var text = txtSummary?.Text ?? string.Empty;
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("摘要为空，无法上传。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_cloudStorageService == null)
            {
                MessageBox.Show("云存储服务不可用，无法上传到网盘。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_currentAnalysisPath))
            {
                MessageBox.Show("当前分析目录为空，无法确定上传位置。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var fileName = $"目录分析摘要_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var localPath = Path.Combine(Path.GetTempPath(), fileName);
            var cloudPath = _currentAnalysisPath.TrimEnd('/') + "/" + fileName;

            try
            {
                File.WriteAllText(localPath, text, System.Text.Encoding.UTF8);
                AppendLog($"正在上传摘要到：{cloudPath}");

                btnUploadSummary.Enabled = false;
                var ok = await _cloudStorageService.UploadFileAsync(localPath, cloudPath);
                if (ok)
                {
                    AppendLog($"摘要已上传到网盘：{cloudPath}");
                    MessageBox.Show($"摘要已上传到：\n{cloudPath}", "上传成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    AppendLog("上传失败：云服务返回失败");
                    MessageBox.Show("上传失败，请检查网盘授权与网络后重试。", "上传失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "上传摘要到网盘失败");
                AppendLog($"上传错误：{ex.Message}");
                MessageBox.Show($"上传失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try { File.Delete(localPath); } catch { /* 忽略清理失败 */ }
                if (!IsDisposed) btnUploadSummary.Enabled = true;
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
            if (lstFileTags != null)
            {
                lstFileTags.BackColor = colors.Background;
                lstFileTags.ForeColor = colors.TextPrimary;
            }
            if (cmbDepth != null) cmbDepth.BackColor = colors.Background;
            if (cmbTagSubject != null) cmbTagSubject.BackColor = colors.Background;
            if (cmbTagValues != null) cmbTagValues.BackColor = colors.Background;
            if (cmbTagAge != null) cmbTagAge.BackColor = colors.Background;
            if (tabControl != null) tabControl.BackColor = colors.Background;
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
