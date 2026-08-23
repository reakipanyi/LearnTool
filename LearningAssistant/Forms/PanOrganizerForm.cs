using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Common.Themes;
using LearningAssistant.Forms.UserControls;
using LearningAssistant.Models.PanAnalysis;
using LearningAssistant.Services.PanAnalysis;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Forms;

/// <summary>
/// 网盘整理工具主窗体（P0.2 基础手动整理能力）。
/// 新增：焦点栏高亮 + 返回上级 + 新建文件夹 + Delete 删除保护校验。
/// 所有操作仍为内存快照模拟，不调真实网盘 API。
/// </summary>
public partial class PanOrganizerForm : Form, IThemeable
{
    #region === 字段 ===
    private readonly IBaiduPanAnalysisOrchestrator? _orchestrator;
    private readonly IThemeService? _themeService;
    private readonly ILogger<PanOrganizerForm>? _logger;
    private readonly PanDirectorySnapshot? _initialSnapshot;
    private readonly List<PanRecommendation> _initialRecommendations;
    private PanNavigatorPanel _focusedPanel;   // 当前焦点栏（⬆️返回上级/📁新建等按钮作用的目标）
    private string _leftPath = "/";
    private string _rightPath = "/";
    public bool ExecutedAny { get; private set; }
    private bool _isClosing;
    private IPanOrganizerExecutionService? _executionService;
    private List<PanTodoItem> _allTodos = new();
    private bool _isExecuting;
    #endregion

    #region === 构造函数 ===
    public PanOrganizerForm()
    {
        _initialRecommendations = new List<PanRecommendation>();
        _focusedPanel = navigatorLeft; // 构造兜底，运行时构造会重设
        InitializeComponent();
        InitBottomTabs();
    }

    public PanOrganizerForm(
        IBaiduPanAnalysisOrchestrator? orchestrator,
        PanDirectorySnapshot? initialSnapshot,
        List<PanRecommendation>? initialRecommendations,
        IThemeService? themeService,
        ILogger<PanOrganizerForm>? logger = null,
        IEventBus? eventBus = null)
    {
        _orchestrator = orchestrator;
        _initialSnapshot = initialSnapshot;
        _initialRecommendations = initialRecommendations ?? new List<PanRecommendation>();
        _themeService = themeService;
        _logger = logger;
        _focusedPanel = navigatorLeft;   // 默认左栏为焦点栏

        InitializeComponent();
        InitBottomTabs();
        InitCompareTab();   // P1-2: 差异对比 Tab

        Text = "🧰 网盘整理工具（P1 体验优化）";
        lblStatusLeft.Text = initialSnapshot != null
            ? $"快照：{initialSnapshot.DirectoryPath}"
            : "未传入快照（空演示模式）";
        lblStatusRight.Text = $"焦点栏：左栏 · {_initialRecommendations.Count} 条 AI 建议（P0.4 载入）";

        // 运行时：订阅左右栏事件联动
        navigatorLeft.FocusActivated += (_, __) => SetFocusedPanel(navigatorLeft);
        navigatorRight.FocusActivated += (_, __) => SetFocusedPanel(navigatorRight);
        navigatorLeft.DirectoryChanged += (_, p) => { _leftPath = p ?? "/"; UpdateStatusPath(); };
        navigatorRight.DirectoryChanged += (_, p) => { _rightPath = p ?? "/"; UpdateStatusPath(); };
        // 重命名完成 → 通知另一栏刷新
        navigatorLeft.RenameCompleted += (_, __) => SafeRefresh(navigatorRight);
        navigatorRight.RenameCompleted += (_, __) => SafeRefresh(navigatorLeft);
        // 删除请求 → 统一确认 + 保护
        navigatorLeft.DeleteRequested += OnDeleteRequested;
        navigatorRight.DeleteRequested += OnDeleteRequested;
        navigatorLeft.CanGoBackChanged += (_, _) => UpdateNavButtons(navigatorLeft, btnNavBack, btnNavForward);
        navigatorLeft.CanGoForwardChanged += (_, _) => UpdateNavButtons(navigatorLeft, btnNavBack, btnNavForward);
        navigatorRight.CanGoBackChanged += (_, _) => UpdateNavButtons(navigatorRight, btnNavBack, btnNavForward);
        navigatorRight.CanGoForwardChanged += (_, _) => UpdateNavButtons(navigatorRight, btnNavBack, btnNavForward);
        // P0.3：共享剪贴板（左右栏共用）
        var sharedClipboard = new PanClipboardState();
        navigatorLeft.SharedClipboard = sharedClipboard;
        navigatorRight.SharedClipboard = sharedClipboard;
        // P1-3: 注入 AI 建议列表 + 快照根路径（供拖拽浮窗推荐使用）
        var snapshotRoot = initialSnapshot?.DirectoryPath ?? "/";
        navigatorLeft.AIRecommendations = _initialRecommendations;
        navigatorRight.AIRecommendations = _initialRecommendations;
        navigatorLeft.SnapshotRootPath = snapshotRoot;
        navigatorRight.SnapshotRootPath = snapshotRoot;
        // P0.3：拖拽/粘贴后刷新所有栏 + 日志
        navigatorLeft.MoveRequested += OnMoveRequested;
        navigatorRight.MoveRequested += OnMoveRequested;
        navigatorLeft.RefreshAllRequested += (_, _) => { SafeRefresh(navigatorRight); SafeRefresh(navigatorLeft); };
        navigatorRight.RefreshAllRequested += (_, _) => { SafeRefresh(navigatorLeft); SafeRefresh(navigatorRight); };

        // 主题
        var colors = _themeService?.CurrentColors ?? ThemeService.GetColors(ThemeMode.Light);
        ApplyTheme(colors);
        _themeService?.RegisterThemeable(this);

        // P0.4: 初始化执行引擎 + 全量载入 AI 待办
        _executionService = new PanOrganizerExecutionService(null);
        _executionService.SetEventBus(eventBus);
        _allTodos = _executionService.BuildTodosFromRecommendations(_initialRecommendations);
        LoadAllTodosToListView();

        FormClosing += PanOrganizerForm_FormClosing;
        Load += PanOrganizerForm_Load;
    }
    #endregion

    #region === Step1: 焦点栏管理（切换高亮 + 状态栏文字）===
    /// <summary>
    /// 切换焦点栏：
    ///   - 路径栏视觉：蓝底(#e8f0fe) vs 灰底(#f7f7f7) + 蓝色边框描边
    ///   - 状态栏右标签：「焦点栏：左栏 / 右栏」
    /// </summary>
    private void SetFocusedPanel(PanNavigatorPanel panel)
    {
        try
        {
            if (panel == null) return;
            _focusedPanel = panel;
            navigatorLeft.SetFocusHighlight(ReferenceEquals(panel, navigatorLeft));
            navigatorRight.SetFocusHighlight(ReferenceEquals(panel, navigatorRight));
            UpdateStatusPath();
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "SetFocusedPanel 异常"); }
    }

    private void SafeRefresh(PanNavigatorPanel panel)
    {
        try { if (panel != null && !panel.IsDisposed) panel.RefreshCurrent(); }
        catch (Exception ex) { _logger?.LogWarning(ex, "刷新另一栏失败"); }
    }

    /// <summary>更新状态左栏「左: xxx  |  右: yyy」</summary>
    private void UpdateStatusPath()
    {
        try
        {
            string Shorten(string p, int max = 36)
            {
                if (string.IsNullOrEmpty(p)) return "/";
                if (p.Length <= max) return p;
                return "…" + p.Substring(p.Length - max + 1);
            }
            lblStatusLeft.Text = $"左: {Shorten(_leftPath)}  │  右: {Shorten(_rightPath)}";
            lblStatusRight.Text = $"焦点栏：{(ReferenceEquals(_focusedPanel, navigatorLeft) ? "左栏 ←" : "右栏 →")}"
                                + $"  · 建议 {_initialRecommendations.Count} 条";
        }
        catch { /* ignore */ }
    }
    #endregion

    #region === P0.4: AI 待办全量载入 ===
    private void LoadAllTodosToListView()
    {
        try
        {
            if (tabTodos.Controls.Count == 0 || tabTodos.Controls[0] is not ListView lstTodos) return;
            lstTodos.BeginUpdate();
            lstTodos.Items.Clear();

            // 先 DAG 排序
            var sorted = _executionService?.TopologicalSort(_allTodos) ?? _allTodos;
            foreach (var todo in sorted)
            {
                var statusStr = todo.Status switch
                {
                    TodoStatus.Confirmed => "[待执行]",
                    TodoStatus.Skipped => "[已跳过]",
                    TodoStatus.Executing => "[执行中]",
                    TodoStatus.Succeeded => "[已成功]",
                    TodoStatus.Failed => "[失败]",
                    _ => "[?]"
                };
                var typeStr = todo.Type switch
                {
                    PanRecommendationType.CreateFolder => "新建",
                    PanRecommendationType.Move => "移动",
                    PanRecommendationType.Rename => "重命名",
                    PanRecommendationType.Delete => "删除",
                    PanRecommendationType.MergeFolder => "合并",
                    _ => "-"
                };
                var item = new ListViewItem(statusStr) { Tag = todo };
                item.SubItems.Add(typeStr);
                item.SubItems.Add(todo.SourceName);
                item.SubItems.Add(todo.Type == PanRecommendationType.Move ? (todo.DestinationPath ?? "-")
                             : todo.Type == PanRecommendationType.Rename ? (todo.NewName ?? "-")
                             : todo.Type == PanRecommendationType.CreateFolder ? (todo.ParentPath + "/" + (todo.FolderName ?? "-"))
                             : "-");
                item.SubItems.Add(todo.Reason);
                // 颜色
                item.ForeColor = todo.Status switch
                {
                    TodoStatus.Succeeded => Color.Green,
                    TodoStatus.Failed => Color.Red,
                    TodoStatus.Skipped => Color.Gray,
                    _ => Color.Black
                };
                lstTodos.Items.Add(item);
            }

            // 状态栏更新
            var confirmed = _allTodos.Count(t => t.Status == TodoStatus.Confirmed);
            lblStatusRight.Text = $"待办 {confirmed} 项（共 {_allTodos.Count} 条）";
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "LoadAllTodosToListView 失败"); }
    }
    #endregion

    #region === 初始化（P0.1 保留 + P0.2 默认左栏高亮）===
    private void InitBottomTabs()
    {
        try
        {
            tabTodos.Controls.Clear();
            tabTodos.Padding = new Padding(3);
            var lstTodos = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false,
                UseCompatibleStateImageBehavior = false
            };
            lstTodos.Columns.Add("状态", 70);
            lstTodos.Columns.Add("操作", 70);
            lstTodos.Columns.Add("源文件", 180);
            lstTodos.Columns.Add("目标/新名称", 180);
            lstTodos.Columns.Add("原因", 260);
            tabTodos.Controls.Add(lstTodos);

            foreach (var rec in _initialRecommendations.Take(5))
            {
                var item = new ListViewItem(rec.PriorityDisplay);
                item.SubItems.Add(rec.TypeDisplay);
                item.SubItems.Add(rec.TargetName);
                item.SubItems.Add(rec.Type switch
                {
                    PanRecommendationType.Move => rec.DestinationPath,
                    PanRecommendationType.Rename => rec.NewName,
                    _ => "-"
                } ?? "-");
                item.SubItems.Add(rec.Reason);
                lstTodos.Items.Add(item);
            }
            if (_initialRecommendations.Count > 5)
                lstTodos.Items.Add(new ListViewItem($"... 另有 {_initialRecommendations.Count - 5} 条（P0.4 完整加载）") { ForeColor = Color.Gray });
            else if (_initialRecommendations.Count == 0)
                lstTodos.Items.Add(new ListViewItem("暂无待办。左/右栏：双击文件夹进入 / F2 重命名 / Delete 删除 / 📁新建文件夹") { ForeColor = Color.Gray });

            tabLog.Controls.Clear();
            tabLog.Padding = new Padding(3);
            var txtLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Font = new Font("Consolas", 9F)
            };
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] PanOrganizerForm 打开（P0.2 基础手动整理 · 内存模拟模式）\r\n");
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] 操作说明：双击文件夹进入 · F2 重命名 · Delete 删除 · 工具栏 📁新建文件夹 · ⬆️返回上级\r\n");
            if (_initialSnapshot != null)
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] 已载入快照：{_initialSnapshot.DirectoryPath}，文件 {_initialSnapshot.Statistics.TotalFileCount:N0} / 文件夹 {_initialSnapshot.Statistics.TotalFolderCount:N0}\r\n");
            tabLog.Controls.Add(txtLog);
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "PanOrganizerForm InitBottomTabs 失败，忽略"); }
    }

    private void PanOrganizerForm_Load(object? sender, EventArgs e)
    {
        try
        {
            var maxDistance = splitContainerMain.Width - splitContainerMain.Panel2MinSize - splitContainerMain.SplitterWidth;
            splitContainerMain.SplitterDistance = Math.Max(
                splitContainerMain.Panel1MinSize, Math.Min(splitContainerMain.Width / 2, maxDistance));
        }
        catch { /* ignore */ }

        if (_initialSnapshot != null)
        {
            var root = _initialSnapshot.DirectoryPath;
            _leftPath = root; _rightPath = root;
            navigatorLeft.LoadFromSnapshot(_initialSnapshot, root);
            navigatorRight.LoadFromSnapshot(_initialSnapshot, root);
            // Load 完成后强制焦点在左栏
            SetFocusedPanel(navigatorLeft);
        }
        else
        {
            UpdateStatusPath();
        }
    }

    private void PanOrganizerForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _isClosing = true;
        try { _themeService?.UnregisterThemeable(this); }
        catch (Exception ex) { _logger?.LogWarning(ex, "关闭时注销主题失败"); }
    }
    #endregion

    #region P1-2: 差异对比视图
    private ListView? _lstCompare;
    private Button? _btnCompare;
    private Label? _lblCompareHint;

    /// <summary>初始化「差异对比」Tab：顶部按钮 + 列表 + 提示</summary>
    private void InitCompareTab()
    {
        try
        {
            tabCompare.Controls.Clear();

            // 顶部操作栏：[🔄 开始对比] + 提示文字
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 32, Padding = new Padding(6, 4, 6, 4) };
            _btnCompare = new Button
            {
                Text = "🔄 开始对比（左 vs 右）",
                AutoSize = false,
                Size = new Size(160, 24),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(66, 133, 244),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular),
                Anchor = AnchorStyles.Left
            };
            _btnCompare.Click += BtnCompare_Click;
            _lblCompareHint = new Label
            {
                Text = "  对比左栏当前目录 vs 右栏当前目录（按文件名 + 大小匹配）",
                AutoSize = false,
                Dock = DockStyle.Fill,
                Padding = new Padding(170, 4, 4, 4),
                ForeColor = Color.Gray,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            pnlTop.Controls.Add(_btnCompare);
            pnlTop.Controls.Add(_lblCompareHint);
            tabCompare.Controls.Add(pnlTop);

            // 主列表：差异项
            _lstCompare = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            _lstCompare.Columns.Add("差异类型", 90);
            _lstCompare.Columns.Add("文件名", 200);
            _lstCompare.Columns.Add("大小", 90);
            _lstCompare.Columns.Add("来源", 80);
            _lstCompare.Columns.Add("建议操作", 220);
            _lstCompare.DoubleClick += LstCompare_DoubleClick;
            tabCompare.Controls.Add(_lstCompare);
            // 让列表填满剩余空间
            _lstCompare.BringToFront();
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "InitCompareTab 失败"); }
    }

    /// <summary>对比按钮：扫描左右栏当前目录，按 (Name + Size) 匹配分类差异</summary>
    private void BtnCompare_Click(object? sender, EventArgs e)
    {
        try
        {
            if (_initialSnapshot == null || _lstCompare == null)
            {
                MessageBox.Show("快照未加载，无法对比。", "差异对比", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var leftPath = _leftPath;
            var rightPath = _rightPath;
            if (leftPath.Equals(rightPath, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("左右栏当前是同一目录，无法对比差异。请切换其中一栏到不同目录。", "差异对比",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var leftItems = CollectDirectoryItems(_initialSnapshot, leftPath, "左栏");
            var rightItems = CollectDirectoryItems(_initialSnapshot, rightPath, "右栏");

            _lstCompare.BeginUpdate();
            _lstCompare.Items.Clear();

            int onlyLeft = 0, onlyRight = 0, sizeDiff = 0, identical = 0;
            // 按 (Name 大小写不敏感) 分组
            var grouped = leftItems.Concat(rightItems)
                .GroupBy(it => it.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var g in grouped)
            {
                var leftItemsInGroup = g.Where(x => x.Side == "左栏").ToList();
                var rightItemsInGroup = g.Where(x => x.Side == "右栏").ToList();
                if (leftItemsInGroup.Count > 0 && rightItemsInGroup.Count == 0)
                {
                    var l = leftItemsInGroup[0];
                    onlyLeft++;
                    AddCompareRow("仅左栏", l.Name, l.SizeDisplay, "左栏", "→ 生成 Move 待办（移到右栏）", Color.FromArgb(230, 126, 34));
                }
                else if (leftItemsInGroup.Count == 0 && rightItemsInGroup.Count > 0)
                {
                    var r = rightItemsInGroup[0];
                    onlyRight++;
                    AddCompareRow("仅右栏", r.Name, r.SizeDisplay, "右栏", "→ 生成 Delete 待办（左栏待删）", Color.FromArgb(230, 126, 34));
                }
                else if (leftItemsInGroup.Count > 0 && rightItemsInGroup.Count > 0)
                {
                    var l = leftItemsInGroup[0];
                    var r = rightItemsInGroup[0];
                    // 两边都有，比对大小
                    if (l.SizeBytes == r.SizeBytes)
                    {
                        identical++;
                        AddCompareRow("两边相同", l.Name, l.SizeDisplay, "两边", "✓ 已归档，可生成 Delete 待办清理左栏", Color.Green);
                    }
                    else
                    {
                        sizeDiff++;
                        AddCompareRow("大小不同", $"{l.Name}", $"{l.SizeDisplay} vs {r.SizeDisplay}", "两边", "⚠️ 可能是版本差异，需人工判断保留哪份", Color.Red);
                    }
                }
            }
            _lstCompare.EndUpdate();

            _lblCompareHint!.Text = $"  对比完成：仅左 {onlyLeft} · 仅右 {onlyRight} · 大小不同 {sizeDiff} · 完全一致 {identical}  (双击仅左/仅右行 → 生成待办)";
            AppendLog($"🔄 差异对比：[{leftPath}] vs [{rightPath}] → 仅左 {onlyLeft} · 仅右 {onlyRight} · 大小不同 {sizeDiff} · 一致 {identical}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "差异对比失败");
            AppendLog($"[ERR] 差异对比失败: {ex.Message}");
        }
    }

    private void AddCompareRow(string type, string name, string size, string source, string suggestion, Color color)
    {
        var item = new ListViewItem(type) { ForeColor = color };
        item.SubItems.Add(name);
        item.SubItems.Add(size);
        item.SubItems.Add(source);
        item.SubItems.Add(suggestion);
        _lstCompare!.Items.Add(item);
    }

    /// <summary>双击「仅左栏」行 → 生成 Move 待办；「仅右栏」行 → 生成 Delete 待办</summary>
    private void LstCompare_DoubleClick(object? sender, EventArgs e)
    {
        try
        {
            if (_lstCompare == null || _lstCompare.SelectedItems.Count == 0) return;
            var row = _lstCompare.SelectedItems[0];
            var type = row.SubItems[0].Text;
            var name = row.SubItems[1].Text;

            if (type == "仅左栏")
            {
                // 生成 Move 待办：从左栏当前目录移到右栏当前目录
                var src = _leftPath.TrimEnd('/') + "/" + name;
                var todo = new PanTodoItem
                {
                    Type = PanRecommendationType.Move,
                    SourcePath = src, SourceName = name,
                    DestinationPath = _rightPath,
                    Reason = $"对比视图：仅左栏存在 → 移动到右栏 {_rightPath}",
                    Status = TodoStatus.Confirmed
                };
                _allTodos.Add(todo);
                LoadAllTodosToListView();
                AppendLog($"➕ 对比生成 Move 待办：{name} → {_rightPath}");
                MessageBox.Show($"已生成待办：移动 {name} → {_rightPath}", "差异对比",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (type == "仅右栏")
            {
                var src = _leftPath.TrimEnd('/') + "/" + name;
                var todo = new PanTodoItem
                {
                    Type = PanRecommendationType.Delete,
                    SourcePath = src, SourceName = name,
                    Reason = $"对比视图：仅右栏存在（左栏疑似残留副本）→ 删除左栏",
                    Status = TodoStatus.Confirmed
                };
                _allTodos.Add(todo);
                LoadAllTodosToListView();
                AppendLog($"➕ 对比生成 Delete 待办：{name}（左栏）");
                MessageBox.Show($"已生成待办：删除左栏的 {name}", "差异对比",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"该差异类型不支持双击生成待办。\n建议：{row.SubItems[4].Text}", "差异对比",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "LstCompare_DoubleClick 失败"); }
    }

    /// <summary>扫描快照中指定目录下的所有条目（文件 + 文件夹），用于对比</summary>
    private List<(string Name, long SizeBytes, string SizeDisplay, string Side)> CollectDirectoryItems(
        PanDirectorySnapshot snapshot, string dirPath, string side)
    {
        var result = new List<(string, long, string, string)>();
        try
        {
            var dirPrefix = dirPath.TrimEnd('/') + "/";
            var dirRel = dirPath.StartsWith(snapshot.DirectoryPath, StringComparison.Ordinal)
                ? dirPath[snapshot.DirectoryPath.Length..].TrimStart('/')
                : "";

            // 文件夹
            foreach (var f in snapshot.Folders)
            {
                var parentRel = GetParentRelative(f.RelativePath);
                if (!parentRel.Equals(dirRel, StringComparison.OrdinalIgnoreCase)) continue;
                result.Add((f.Name, 0, "-", side));
            }
            // 文件
            foreach (var f in snapshot.Files)
            {
                if (!f.Path.StartsWith(dirPrefix, StringComparison.OrdinalIgnoreCase)) continue;
                var rest = f.Path[dirPrefix.Length..];
                if (rest.Contains('/')) continue;   // 跳过子目录
                result.Add((f.Name, f.SizeBytes, f.SizeFormatted ?? "-", side));
            }
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "CollectDirectoryItems 失败"); }
        return result;
    }

    private static string GetParentRelative(string rel)
    {
        if (string.IsNullOrEmpty(rel)) return "";
        var idx = rel.LastIndexOf('/');
        return idx < 0 ? "" : rel.Substring(0, idx);
    }
    #endregion

    #region === P1: 导航前进/后退 ===
    private void btnNavBack_Click(object sender, EventArgs e)
    {
        try { _focusedPanel?.GoBack(); }
        catch (Exception ex) { _logger?.LogWarning(ex, "后退失败"); }
    }

    private void btnNavForward_Click(object sender, EventArgs e)
    {
        try { _focusedPanel?.GoForward(); }
        catch (Exception ex) { _logger?.LogWarning(ex, "前进失败"); }
    }

    private void UpdateNavButtons(PanNavigatorPanel source, ToolStripButton btnBack, ToolStripButton btnForward)
    {
        if (_focusedPanel == null || !ReferenceEquals(source, _focusedPanel)) return;
        if (btnBack != null && !btnBack.IsDisposed) btnBack.Enabled = source.CanGoBack;
        if (btnForward != null && !btnForward.IsDisposed) btnForward.Enabled = source.CanGoForward;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Alt | Keys.Left))
        {
            if (_focusedPanel != null && _focusedPanel.CanGoBack) { _focusedPanel.GoBack(); return true; }
        }
        if (keyData == (Keys.Alt | Keys.Right))
        {
            if (_focusedPanel != null && _focusedPanel.CanGoForward) { _focusedPanel.GoForward(); return true; }
        }
        if (keyData == (Keys.Control | Keys.Z))
        {
            btnUndo_Click(this, EventArgs.Empty);
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
    #endregion

    #region === Step3: 返回上级（焦点栏→父目录 LoadFromSnapshot）===
    private void btnGoUp_Click(object sender, EventArgs e)
    {
        try
        {
            if (_focusedPanel == null || _initialSnapshot == null) return;
            var cur = _focusedPanel.CurrentPath ?? "/";
            // 保护：已经在根目录（快照根或"/"）不允许再上去
            if (cur.Equals(_initialSnapshot.DirectoryPath, StringComparison.OrdinalIgnoreCase)
                || cur == "/" || string.IsNullOrEmpty(cur))
            {
                SystemSounds_Blip();
                AppendLog($"⬆️ 返回上级：{(ReferenceEquals(_focusedPanel, navigatorLeft) ? "左栏" : "右栏")} 已在快照根目录，无法继续向上");
                return;
            }
            var parent = GetParentApiPath(cur);
            if (string.IsNullOrEmpty(parent)) parent = _initialSnapshot.DirectoryPath;
            _focusedPanel.LoadFromSnapshot(_initialSnapshot, parent);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "返回上级失败");
            MessageBox.Show($"返回上级失败：{ex.Message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static string GetParentApiPath(string path)
    {
        var trimmed = (path ?? "").TrimEnd('/');
        var idx = trimmed.LastIndexOf('/');
        return idx <= 0 ? "/" : trimmed.Substring(0, idx);
    }

    private static void SystemSounds_Blip()
    {
        try { System.Media.SystemSounds.Beep.Play(); } catch { /* ignore */ }
    }
    #endregion

    #region === Step4: 新建文件夹（焦点栏 → CreateNewFolderPlaceholder）===
    private void btnNewFolder_Click(object sender, EventArgs e)
    {
        try
        {
            if (_focusedPanel == null || _initialSnapshot == null)
            {
                MessageBox.Show("请先加载快照再新建文件夹。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _focusedPanel.CreateNewFolderPlaceholder();
            ExecutedAny = true;
            AppendLog($"📁 {(ReferenceEquals(_focusedPanel, navigatorLeft) ? "左栏" : "右栏")}：新建文件夹（请在蓝色文字上输入名称，Enter 确认）");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "新建文件夹失败");
            MessageBox.Show($"新建文件夹失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    #endregion

    #region === P0.3: 移动/粘贴/拖拽事件处理 ===
    private void OnMoveRequested(object? sender, (List<PanFileInfo> Items, string TargetPath, string Action) args)
    {
        try
        {
            var panel = sender as PanNavigatorPanel;
            var side = panel != null && ReferenceEquals(panel, navigatorRight) ? "右栏" : "左栏";
            var count = args.Items?.Count ?? 0;
            var actionLabel = args.Action switch
            {
                "Move" => "拖拽移动",
                "Cut" => "剪切",
                "Copy" => "复制",
                _ => args.Action
            };
            if (args.Action == "Move")
            {
                ExecutedAny = true;
                AppendLog($"📦 {side}：{actionLabel} {count} 项 → {args.TargetPath}");
            }
            else
            {
                AppendLog($"📋 {side}：{actionLabel} {count} 项到剪贴板（在目标栏按 Ctrl+V 粘贴）");
            }
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "OnMoveRequested 异常"); }
    }
    #endregion

    #region === Step6: Delete 删除（确认 + 3 层保护 + 级联移除子孙 + 日志 + 通知另一栏刷新）===
    private void OnDeleteRequested(object? sender, List<PanFileInfo> items)
    {
        if (items == null || items.Count == 0 || _initialSnapshot == null) return;
        var srcPanel = sender as PanNavigatorPanel ?? _focusedPanel;
        try
        {
            // ---- 保护 1：禁止删除正在浏览的目录 / 其祖先 ----
            string panelCurrent = srcPanel?.CurrentPath ?? "/";
            var blocked = new List<string>();
            foreach (var fi in items)
            {
                if (!fi.IsFolder) continue;
                var fiPath = (fi.Path ?? "").TrimEnd('/') + "/";
                var currentWithSlash = panelCurrent.TrimEnd('/') + "/";
                if (fiPath.Equals(_initialSnapshot.DirectoryPath.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase)
                    || currentWithSlash.StartsWith(fiPath, StringComparison.OrdinalIgnoreCase))
                {
                    blocked.Add(fi.Name);
                }
            }
            if (blocked.Count > 0)
            {
                MessageBox.Show(
                    $"以下文件夹禁止删除：\n  • {string.Join("\n  • ", blocked)}\n\n" +
                    "原因：是快照根目录，或当前正在浏览的目录（删除后无法继续操作）。\n请先切到别的目录再删除。",
                    "禁止删除", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            // ---- 保护 2：禁止删除根 ----
            if (items.Any(fi => fi.Path == _initialSnapshot.DirectoryPath
                                || fi.Path == "/"
                                || string.IsNullOrWhiteSpace(fi.Path)))
            {
                MessageBox.Show("禁止删除根目录。", "禁止删除", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            // ---- 统计：多少文件 + 多少文件夹 ----
            int fileCount = items.Count(i => !i.IsFolder);
            int folderCount = items.Count(i => i.IsFolder);
            long totalBytes = items.Where(i => !i.IsFolder).Sum(i => i.SizeBytes);

            // 级联文件夹子孙统计
            if (_initialSnapshot != null)
            {
                foreach (var fi in items.Where(i => i.IsFolder))
                {
                    var prefix = fi.Path.TrimEnd('/') + "/";
                    var relPrefix = fi.RelativePath?.TrimEnd('/') + "/";
                    foreach (var f in _initialSnapshot.Files)
                    {
                        if (f.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        { fileCount++; totalBytes += f.SizeBytes; }
                    }
                    foreach (var fd in _initialSnapshot.Folders)
                    {
                        if (fd.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) folderCount++;
                    }
                }
            }

            var sizeStr = FormatSize(totalBytes);
            var line = $"确认要删除 {fileCount + folderCount} 项（{fileCount:N0} 个文件 + {folderCount:N0} 个文件夹，共 {sizeStr}）？\n\n"
                     + $"⚠️  P0.2 目前为「内存模拟删除」，只修改本地快照，不会真的删除网盘中的文件。\n\n"
                     + $"删除后将无法在此窗体中恢复（关闭再打开快照将恢复原状）。";
            using var dlg = new Form
            {
                Text = "删除确认（P0.2 仅内存模拟）",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                ClientSize = new Size(480, 220),
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            var lbl = new Label
            {
                Text = line,
                Dock = DockStyle.Fill,
                Padding = new Padding(14),
                AutoSize = false
            };
            var btnOk = new Button { Text = "✅ 确认删除", DialogResult = DialogResult.Yes, Size = new Size(120, 30), Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            var btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Size = new Size(90, 30), Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            var pnl = new Panel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(10) };
            pnl.Controls.Add(btnOk); pnl.Controls.Add(btnCancel);
            btnOk.Location = new Point(pnl.ClientSize.Width - btnOk.Width - pnl.Padding.Right - btnCancel.Width - 10, 7);
            btnCancel.Location = new Point(pnl.ClientSize.Width - btnCancel.Width - pnl.Padding.Right, 7);
            dlg.Controls.Add(lbl); dlg.Controls.Add(pnl);
            dlg.AcceptButton = btnOk; dlg.CancelButton = btnCancel;
            var res = dlg.ShowDialog(this);
            if (res != DialogResult.Yes) return;

            // ✅ 真正执行删除（内存快照）
            int removedFiles = 0, removedFolders = 0;
            long removedBytes = 0;

            // 1) 从 Files 移除所有命中（含子孙）
            var toDeleteFiles = new HashSet<long>();   // fsId 列表（文件）
            var toDeleteFolderPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var fi in items)
            {
                if (fi.IsFolder)
                {
                    toDeleteFolderPaths.Add(fi.Path.TrimEnd('/'));
                    toDeleteFolderPaths.Add(fi.Path.TrimEnd('/') + "/");
                }
                else if (fi.FsId != 0)
                {
                    toDeleteFiles.Add(fi.FsId);
                }
            }
            // 级联子孙文件夹
            bool added = true;
            while (added)
            {
                added = false;
                foreach (var fd in _initialSnapshot.Folders.ToList())
                {
                    var trimmed = fd.Path.TrimEnd('/');
                    if (toDeleteFolderPaths.Contains(trimmed)) continue;
                    foreach (var root in toDeleteFolderPaths.ToList())
                    {
                        var rootTrim = root.TrimEnd('/') + "/";
                        if (fd.Path.StartsWith(rootTrim, StringComparison.OrdinalIgnoreCase))
                        {
                            toDeleteFolderPaths.Add(trimmed);
                            toDeleteFolderPaths.Add(trimmed + "/");
                            added = true;
                            break;
                        }
                    }
                }
            }
            // 2) 移除文件
            _initialSnapshot.Files = _initialSnapshot.Files.Where(f =>
            {
                bool del = (f.FsId != 0 && toDeleteFiles.Contains(f.FsId));
                if (!del)
                {
                    foreach (var root in toDeleteFolderPaths)
                    {
                        var r = root.TrimEnd('/') + "/";
                        if (!string.IsNullOrEmpty(r) && f.Path.StartsWith(r, StringComparison.OrdinalIgnoreCase))
                        { del = true; break; }
                    }
                }
                if (del) { removedFiles++; removedBytes += f.SizeBytes; }
                return !del;
            }).ToList();

            // 3) 移除文件夹
            _initialSnapshot.Folders = _initialSnapshot.Folders.Where(fd =>
            {
                var t = fd.Path.TrimEnd('/');
                if (toDeleteFolderPaths.Contains(t)) { removedFolders++; return false; }
                return true;
            }).ToList();

            // 4) 更新统计
            if (_initialSnapshot.Statistics != null)
            {
                _initialSnapshot.Statistics.TotalFileCount = Math.Max(0, _initialSnapshot.Statistics.TotalFileCount - removedFiles);
                _initialSnapshot.Statistics.TotalFolderCount = Math.Max(0, _initialSnapshot.Statistics.TotalFolderCount - removedFolders);
                _initialSnapshot.Statistics.TotalSizeBytes = Math.Max(0, _initialSnapshot.Statistics.TotalSizeBytes - removedBytes);
            }

            // 5) 两栏都刷新
            navigatorLeft.RefreshCurrent();
            navigatorRight.RefreshCurrent();
            ExecutedAny = true;

            // 6) 日志
            AppendLog($"🗑️ 删除（内存模拟）：{removedFiles:N0} 个文件 + {removedFolders:N0} 个文件夹，共 {FormatSize(removedBytes)}"
                    + $"  → {(srcPanel != null && ReferenceEquals(srcPanel, navigatorRight) ? "右栏" : "左栏")}");
            AppendLog("   ⚠️ 关闭本窗体再重新打开（重新读取快照）即可恢复所有文件");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "删除处理异常");
            MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
        return $"{len:0.##} {sizes[order]}";
    }
    #endregion

    #region === IThemeable ===
    private ThemeColors _currentColors = ThemeService.GetColors(ThemeMode.Light);
    public ThemeColors CurrentColors => _currentColors;
    public event EventHandler<ThemeColors>? ThemeChanged;

    public void ApplyTheme(ThemeColors colors)
    {
        _currentColors = colors ?? throw new ArgumentNullException(nameof(colors));
        try
        {
            BackColor = colors.Background;
            ForeColor = colors.TextPrimary;
            toolStripTop.BackColor = colors.Surface;
            toolStripTop.ForeColor = colors.TextPrimary;
            tabControlBottom.BackColor = colors.Background;
            tabControlBottom.ForeColor = colors.TextPrimary;
            statusStripBottom.BackColor = colors.Surface;
            statusStripBottom.ForeColor = colors.TextSecondary;
            splitContainerMain.BackColor = colors.Divider;
            splitMainBottom.BackColor = colors.Divider;
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "PanOrganizerForm.ApplyTheme 失败，忽略"); }
        try
        {
            navigatorLeft.ApplyTheme(colors);
            navigatorRight.ApplyTheme(colors);
            // 重新应用焦点颜色（ApplyTheme 会重置 backColor）
            SetFocusedPanel(_focusedPanel);
        }
        catch
        {
            // 设计模式下控件可能尚未初始化
        }
        ThemeChanged?.Invoke(this, colors);
    }
    #endregion

    #region === P0.4: AI 待办执行 ===
    private void btnExecuteTodos_Click(object sender, EventArgs e)
    {
        if (_isExecuting) { MessageBox.Show("正在执行中...", "提示"); return; }
        if (_initialSnapshot == null || _executionService == null) { MessageBox.Show("快照未加载", "提示"); return; }
        var confirmed = _allTodos.Where(t => t.Status == TodoStatus.Confirmed).ToList();
        if (confirmed.Count == 0) { MessageBox.Show("没有已确认的待办可执行", "提示"); return; }
        var sorted = _executionService.TopologicalSort(_allTodos);
        var batches = _executionService.MergeBatches(sorted);
        var preflight = _executionService.PreflightCheck(batches, _initialSnapshot);
        var preflightFail = preflight.Count(p => !p.Passed);
        var msg = $"即将执行 {confirmed.Count} 项待办（{batches.Count} 批次）。";
        if (preflightFail > 0) msg += $"\n⚠️ {preflightFail} 批次未通过预检（快照过时），将自动跳过。";
        msg += "\n\n确认开始执行？（内存模拟模式）";
        if (MessageBox.Show(msg, "执行确认", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
        _ = ExecuteTodosAsync(batches);
    }

    private async Task ExecuteTodosAsync(List<PanTodoBatch> batches)
    {
        _isExecuting = true;
        btnExecuteTodos.Enabled = false;
        try
        {
            var progress = new Progress<PanExecutionProgress>(p => OnExecutionProgress(p));
            var cts = new CancellationTokenSource();
            var report = await _executionService!.ExecuteAsync(batches, _initialSnapshot!, progress, cts.Token);
            navigatorLeft.RefreshCurrent();
            navigatorRight.RefreshCurrent();
            LoadAllTodosToListView();
            ExecutedAny = true;
            // P2-3: 执行完成后弹出 AI 总结
            var summary = _executionService.GenerateSummary(report);
            if (!string.IsNullOrEmpty(summary))
            {
                AppendLogColored(summary, Color.DarkBlue);
                MessageBox.Show(summary, "整理完成 AI 总结", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ExecuteTodosAsync 异常");
            AppendLog($"[ERR] 执行异常: {ex.Message}");
        }
        finally
        {
            _isExecuting = false;
            btnExecuteTodos.Enabled = true;
        }
    }

    private void OnExecutionProgress(PanExecutionProgress p)
    {
        try
        {
            var color = p.LogLevel switch
            {
                ProgressLogLevel.Success => Color.Green,
                ProgressLogLevel.Warning => Color.FromArgb(230, 126, 34),
                ProgressLogLevel.Error => Color.Red,
                ProgressLogLevel.Debug => Color.Gray,
                _ => Color.Black
            };
            AppendLogColored($"[{p.CompletedCount}/{p.TotalCount}] {p.Message}", color);
        }
        catch { }
    }

    private void AppendLogColored(string message, Color color)
    {
        try
        {
            if (tabLog.Controls.Count > 0 && tabLog.Controls[0] is RichTextBox txt && !txt.IsDisposed)
            {
                var time = DateTime.Now.ToString("HH:mm:ss");
                if (txt.TextLength > 40000) txt.Clear();
                txt.SelectionStart = txt.TextLength;
                txt.SelectionColor = color;
                txt.AppendText($"[{time}] {message}\r\n");
                txt.SelectionColor = txt.ForeColor;
                txt.ScrollToCaret();
            }
        }
        catch { }
    }
    #endregion

    #region === 工具栏按钮（Refresh/Paste/Undo 占位）===
    private void btnRefresh_Click(object sender, EventArgs e)
    {
        navigatorLeft.RefreshCurrent();
        navigatorRight.RefreshCurrent();
        AppendLog("刷新两栏目录（基于当前内存快照）");
    }

    private void btnPaste_Click(object sender, EventArgs e)
    {
        try { if (_focusedPanel != null) { _focusedPanel.PasteFromClipboard(); ExecutedAny = true; } }
        catch (Exception ex) { _logger?.LogWarning(ex, "粘贴失败"); }
    }

    private void btnUndo_Click(object sender, EventArgs e)
    {
        try
        {
            if (_executionService == null || _initialSnapshot == null) return;
            if (_executionService.UndoCount == 0)
            {
                MessageBox.Show("没有可撤销的操作。", "撤销", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var entry = _executionService.UndoLast(_initialSnapshot);
            if (entry != null)
            {
                navigatorLeft.RefreshCurrent();
                navigatorRight.RefreshCurrent();
                LoadAllTodosToListView();
                if (entry.CanUndo)
                    AppendLog($"↩️ 已撤销：{entry.OriginalOperation.Type} {entry.OriginalOperation.TargetName}");
                else
                    AppendLog($"↩️ 该操作（{entry.OriginalOperation.Type} {entry.OriginalOperation.TargetName}）不可逆，已移出撤销栈");
            }
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "撤销失败"); }
    }

    private void btnDryRun_Click(object sender, EventArgs e)
    {
        try
        {
            if (_initialSnapshot == null || _executionService == null)
            {
                MessageBox.Show("快照未加载，无法演练。", "Dry-Run", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var confirmed = _allTodos.Where(t => t.Status == TodoStatus.Confirmed).ToList();
            if (confirmed.Count == 0)
            {
                MessageBox.Show("没有已确认的待办可演练。", "Dry-Run", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var sorted = _executionService.TopologicalSort(_allTodos);
            var batches = _executionService.MergeBatches(sorted);
            var report = _executionService.DryRun(batches, _initialSnapshot);
            var summary = _executionService.GenerateSummary(report);
            AppendLogColored($"🧪 Dry-Run 演练（不修改快照）：请求 {report.TotalRequested} → 成功 {report.Succeeded} · 失败 {report.Failed}", Color.DarkCyan);
            AppendLogColored(summary, Color.DarkBlue);
            MessageBox.Show(summary, "Dry-Run 演练报告", MessageBoxButtons.OK,
                report.HasFailures ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DryRun 异常");
            AppendLog($"[ERR] 演练异常: {ex.Message}");
        }
    }
    #endregion

    #region === 辅助 ===
    private void AppendLog(string message)
    {
        try
        {
            if (tabLog.Controls.Count > 0 && tabLog.Controls[0] is RichTextBox txt && !txt.IsDisposed)
            {
                var time = DateTime.Now.ToString("HH:mm:ss");
                if (txt.TextLength > 40000) txt.Clear();
                txt.AppendText($"[{time}] {message}\r\n");
            }
        }
        catch { }
    }
    #endregion
}