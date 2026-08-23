using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.PanAnalysis;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Forms.UserControls;

/// <summary>
/// 单栏导航面板（P0.2 手动整理能力）。
/// 支持：焦点高亮、TreeView 切目录、F2 重命名（ListView LabelEdit）、Delete 删除、新建文件夹（占位→编辑）。
/// P0.2 所有操作均为**纯内存快照模拟**，不调用真实网盘 API。
/// </summary>
public partial class PanNavigatorPanel : UserControl, IThemeable
{
    private readonly ILogger? _logger;
    private PanDirectorySnapshot? _snapshot;
    private bool _isFocused;
    private Color _focusColor = Color.FromArgb(232, 240, 254);   // 焦点蓝底（方案色 #e8f0fe）
    private Color _unfocusColor = Color.FromArgb(247, 247, 247); // 非焦点灰底
    private Color _focusBorder = Color.FromArgb(66, 133, 244);  // 焦点边框 Google Blue

    #region P1: 搜索过滤
    private const string SearchPlaceholder = "🔍 搜索文件名";
    private bool _isSearchPlaceholderActive = true;
    private string _searchKeyword = "";
    #endregion

    #region === 公开属性 ===
    public string CurrentPath { get; private set; } = "/";

    #region P1: 导航前进/后退栈
    private readonly Stack<string> _backStack = new();
    private readonly Stack<string> _forwardStack = new();
    private bool _isNavigating;  // 后退/前进操作时不压栈

    /// <summary>是否可后退</summary>
    public bool CanGoBack => _backStack.Count > 0;
    /// <summary>是否可前进</summary>
    public bool CanGoForward => _forwardStack.Count > 0;

    public event EventHandler? CanGoBackChanged;
    public event EventHandler? CanGoForwardChanged;
    #endregion
    public TreeView FolderTree => treeFolders;
    public ListView FileList => lstFiles;
    public ComboBox PathSelector => cboPath;
    public bool IsFocusedPanel => _isFocused;

    public List<PanFileInfo> SelectedFiles
    {
        get
        {
            var result = new List<PanFileInfo>();
            try
            {
                foreach (ListViewItem item in lstFiles.SelectedItems)
                    if (item.Tag is PanFileInfo fi) result.Add(fi);
            }
            catch (Exception ex) { _logger?.LogWarning(ex, "读取 SelectedFiles 失败"); }
            return result;
        }
    }
    #endregion

    #region === 公开事件（P0.2 开始由主窗体订阅，驱动跨栏联动与日志）===
    public event EventHandler<string>? DirectoryChanged;      // 切目录（主窗体更新状态栏路径）
    public event EventHandler? FocusActivated;                // 本面板被激活为主窗体焦点栏
    public event EventHandler<PanFileInfo>? RenameCompleted;  // 单个文件/文件夹重命名完毕（主窗体通知另一栏重刷）
    public event EventHandler<List<PanFileInfo>>? DeleteRequested;  // 请求删除 → 主窗体统一弹确认 + 保护校验
    public event EventHandler? CreateFolderRequested;         // P0.4
    #endregion

    #region P0.3：拖拽 + 剪贴板 + 移动事件
    public event EventHandler<(List<PanFileInfo> Items, string TargetPath, string Action)>? MoveRequested;
    public event EventHandler? RefreshAllRequested;
    #endregion

    #region 兼容：旧事件声明
    public event EventHandler<List<PanFileInfo>>? FilesDragStart;
    public event EventHandler<(List<PanFileInfo> Items, string TargetPath)>? FilesDropped;
    public event EventHandler<PanFileInfo>? RenameRequested;
    #endregion

    /// <summary>共享剪贴板状态（由主窗体注入，左右栏共用）</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public PanClipboardState? SharedClipboard { get; set; }

    /// <summary>P1-3: AI 建议列表（由主窗体注入，用于拖拽推荐路径）</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public List<PanRecommendation>? AIRecommendations { get; set; }

    /// <summary>P1-3: 快照根路径（用于拼推荐路径绝对路径）</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string SnapshotRootPath { get; set; } = "/";

    private const string DragDataFormat = "PanFileInfoList";
    private PanDropHintWindow? _hintWindow;

    /// <summary>设计器专用无参构造</summary>
    public PanNavigatorPanel()
    {
        InitializeComponent();
        InitFileListColumns();
        InitDragDrop();
        InitContextMenu();
        InitSearchBox();
    }

    public PanNavigatorPanel(ILogger? logger) : this() { _logger = logger; }

    /// <summary>P1: 搜索框占位文字 + GotFocus/LostFocus 处理</summary>
    private void InitSearchBox()
    {
        try
        {
            txtSearch.Text = SearchPlaceholder;
            txtSearch.ForeColor = Color.Gray;
            txtSearch.GotFocus += TxtSearch_GotFocus;
            txtSearch.LostFocus += TxtSearch_LostFocus;
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "InitSearchBox 失败"); }
    }

    private void TxtSearch_GotFocus(object? sender, EventArgs e)
    {
        try
        {
            if (_isSearchPlaceholderActive)
            {
                txtSearch.Text = "";
                txtSearch.ForeColor = SystemColors.WindowText;
                _isSearchPlaceholderActive = false;
            }
        }
        catch { /* ignore */ }
    }

    private void TxtSearch_LostFocus(object? sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(txtSearch.Text))
            {
                txtSearch.Text = SearchPlaceholder;
                txtSearch.ForeColor = Color.Gray;
                _isSearchPlaceholderActive = true;
                _searchKeyword = "";
            }
        }
        catch { /* ignore */ }
    }

    private void InitFileListColumns()
    {
        try
        {
            lstFiles.Columns.Clear();
            lstFiles.Columns.Add("名称", 180);
            lstFiles.Columns.Add("大小", 90);
            lstFiles.Columns.Add("类型", 80);
            lstFiles.Columns.Add("修改时间", 140);
            lstFiles.View = View.Details;
            lstFiles.FullRowSelect = true;
            lstFiles.GridLines = true;
            lstFiles.HideSelection = false;
            lstFiles.UseCompatibleStateImageBehavior = false;
            lstFiles.LabelEdit = true;   // ✅ P0.2：允许 F2 原地重命名
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "PanNavigatorPanel InitFileListColumns 失败"); }
    }

    /// <summary>
    /// 激活/取消焦点栏视觉效果。主窗体调用。
    /// 焦点：pnlTop 蓝底 + 1px 蓝色边框；非焦点：浅灰底 + 浅灰边
    /// </summary>
    public void SetFocusHighlight(bool isFocused)
    {
        _isFocused = isFocused;
        try
        {
            pnlTop.BackColor = isFocused ? _focusColor : _unfocusColor;
            pnlTop.Padding = new Padding(1, 1, 1, 1);
            // 伪边框：外层 Panel 的 Padding + BackColor 实现 1px 描边
            splitMain.BackColor = isFocused ? _focusBorder : Color.FromArgb(220, 220, 220);
            // 给用户视觉强提示：焦点栏 ComboBox 有 focus ring
            if (!string.IsNullOrEmpty(cboPath.Text)) { /* 颜色足够 */ }
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "SetFocusHighlight 失败"); }
    }

    #region === 快照加载（P0.1 基础 + P0.2 切目录兼容）===
    /// <summary>导航到指定路径（压入后退栈，清空前进栈）。用户手动切目录时调用。</summary>
    public void NavigateTo(string path)
    {
        if (string.IsNullOrEmpty(path) || path == CurrentPath) return;
        if (!_isNavigating)
        {
            _backStack.Push(CurrentPath);
            _forwardStack.Clear();
            CanGoBackChanged?.Invoke(this, EventArgs.Empty);
            CanGoForwardChanged?.Invoke(this, EventArgs.Empty);
        }
        if (_snapshot != null) LoadFromSnapshot(_snapshot, path);
    }

    /// <summary>后退</summary>
    public void GoBack()
    {
        if (_backStack.Count == 0) return;
        _isNavigating = true;
        try
        {
            var prev = _backStack.Pop();
            _forwardStack.Push(CurrentPath);
            CanGoBackChanged?.Invoke(this, EventArgs.Empty);
            CanGoForwardChanged?.Invoke(this, EventArgs.Empty);
            if (_snapshot != null) LoadFromSnapshot(_snapshot, prev);
        }
        finally { _isNavigating = false; }
    }

    /// <summary>前进</summary>
    public void GoForward()
    {
        if (_forwardStack.Count == 0) return;
        _isNavigating = true;
        try
        {
            var next = _forwardStack.Pop();
            _backStack.Push(CurrentPath);
            CanGoBackChanged?.Invoke(this, EventArgs.Empty);
            CanGoForwardChanged?.Invoke(this, EventArgs.Empty);
            if (_snapshot != null) LoadFromSnapshot(_snapshot, next);
        }
        finally { _isNavigating = false; }
    }

    public void LoadFromSnapshot(PanDirectorySnapshot? snapshot, string targetPath)
    {
        if (snapshot == null) { _logger?.LogWarning("LoadFromSnapshot 收到 null snapshot"); return; }
        _snapshot = snapshot;
        CurrentPath = targetPath ?? "/";
        try
        {
            cboPath.BeginUpdate();
            cboPath.Items.Clear();
            cboPath.Items.Add(CurrentPath);
            cboPath.SelectedIndex = 0;
            cboPath.EndUpdate();

            BuildFolderTreeFromSnapshot(targetPath);
            PopulateFileListFromSnapshot(targetPath);
            DirectoryChanged?.Invoke(this, CurrentPath);
        }
        catch (Exception ex) { _logger?.LogError(ex, "PanNavigatorPanel.LoadFromSnapshot 失败 路径={Path}", targetPath); }
    }

    /// <summary>只刷新 ListView（例如完成重命名/删除后，不想重刷 TreeView）</summary>
    public void RefreshFileListOnly() { if (_snapshot != null) PopulateFileListFromSnapshot(CurrentPath); }

    public void RefreshCurrent() { if (_snapshot != null) LoadFromSnapshot(_snapshot, CurrentPath); }
    #endregion

    #region === Step2：TreeView 选择节点 → 切目录 ===
    private void treeFolders_AfterSelect(object sender, TreeViewEventArgs e)
    {
        if (e.Node == null || _snapshot == null) return;
        string? path = null;
        try
        {
            if (e.Node.Tag is string s) path = s;
            else if (e.Node.Tag is PanFolderInfo f) path = f.Path;
        }
        catch { /* ignore */ }

        if (!string.IsNullOrEmpty(path) && path != CurrentPath)
        {
            ActivateFocus();
            NavigateTo(path);
        }
    }

    private void cboPath_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_snapshot == null || cboPath.SelectedIndex < 0) return;
        var p = cboPath.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(p) && p != CurrentPath)
        {
            ActivateFocus();
            NavigateTo(p);
        }
    }

    private void txtSearch_TextChanged(object? sender, EventArgs e)
    {
        try
        {
            if (_isSearchPlaceholderActive) return;   // 占位态不触发过滤
            var kw = (txtSearch.Text ?? "").Trim();
            if (kw == _searchKeyword) return;
            _searchKeyword = kw;
            // 重新过滤当前 ListView（不重新加载快照，避免闪烁）
            ApplySearchFilter();
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "txtSearch_TextChanged 过滤失败"); }
    }

    /// <summary>P1: 按当前搜索关键词过滤 lstFiles（不重读快照，原地隐藏/显示）</summary>
    private void ApplySearchFilter()
    {
        try
        {
            var kw = _searchKeyword;
            lstFiles.BeginUpdate();
            foreach (ListViewItem item in lstFiles.Items)
            {
                if (string.IsNullOrEmpty(kw)) { item.Checked = item.Checked; }
                // 大小写不敏感匹配 Name（去掉 📁 前缀）
                var name = item.Text;
                if (name.StartsWith("📁 ")) name = name.Substring(2);
                bool match = string.IsNullOrEmpty(kw)
                          || name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0;
                // 用 ForeColor 暗示未命中（不直接移除，保留选中状态）
                if (!match)
                {
                    item.ForeColor = Color.LightGray;
                    item.BackColor = Color.FromArgb(248, 248, 248);
                }
                else
                {
                    // 恢复默认色（文件夹蓝色 / 文件默认）
                    if (name != item.Text) item.ForeColor = Color.FromArgb(66, 133, 244);
                    else item.ForeColor = SystemColors.WindowText;
                    item.BackColor = SystemColors.Window;
                }
            }
            lstFiles.EndUpdate();
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "ApplySearchFilter 失败"); }
    }

    /// <summary>P1: 清空搜索（外部调用，例如切换目录时）</summary>
    public void ClearSearch()
    {
        try
        {
            _searchKeyword = "";
            if (!_isSearchPlaceholderActive)
            {
                txtSearch.Text = "";
                TxtSearch_LostFocus(this, EventArgs.Empty);
            }
        }
        catch { /* ignore */ }
    }
    #endregion

    #region === Step4：新建文件夹（插入「📁 新建文件夹」→ 原地 LabelEdit → Enter 写入快照）===
    /// <summary>
    /// 在当前目录下插入一个新文件夹占位并进入重命名编辑态。
    /// 当用户按 Enter/失去焦点 → AfterLabelEdit 真正写入 PanDirectorySnapshot.Folders
    /// </summary>
    public void CreateNewFolderPlaceholder()
    {
        if (_snapshot == null) { _logger?.LogWarning("快照未加载，无法新建文件夹"); return; }
        try
        {
            ActivateFocus();
            // 1. 先处理同目录已存在「新建文件夹」重名（自动加数字后缀）
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ListViewItem it in lstFiles.Items) names.Add(it.Text);
            var baseName = "新建文件夹";
            var finalName = baseName;
            var suffix = 1;
            while (names.Contains("📁 " + finalName)) { finalName = $"{baseName}{suffix}"; suffix++; }

            // 2. 插入 ListViewItem（标记 IsNewFolderPlaceholder = true，写在 Tag 里）
            var placeholder = new PanFileInfo
            {
                Path = CurrentPath.TrimEnd('/') + "/" + finalName,
                Name = finalName,
                RelativePath = (CurrentPath.StartsWith(_snapshot.DirectoryPath, StringComparison.Ordinal)
                    ? CurrentPath[_snapshot.DirectoryPath.Length..].TrimStart('/') + "/"
                    : "") + finalName,
                IsFolder = true, Category = 6, SizeBytes = 0,
                FsId = -1   // 标记：占位 fsId=-1，表示未写入快照 Folders 的占位
            };
            var item = new ListViewItem("📁 " + finalName)
            {
                Tag = placeholder,
                ForeColor = Color.FromArgb(66, 133, 244)   // 蓝色：未确认
            };
            item.SubItems.Add("(新建)");
            item.SubItems.Add("文件夹");
            item.SubItems.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            lstFiles.Items.Add(item);
            item.Selected = true;
            lstFiles.EnsureVisible(item.Index);
            // 3. 进入原地编辑（去掉 📁 前缀让用户只改文字部分）
            item.BeginEdit();
        }
        catch (Exception ex) { _logger?.LogError(ex, "新建文件夹占位失败"); }
    }
    #endregion

    #region === Step5：F2 重命名（BeforeLabelEdit/AfterLabelEdit 校验 + 更新快照）===
    private static readonly char[] _illegalChars = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

    private void lstFiles_KeyDown(object sender, KeyEventArgs e)
    {
        try
        {
            ActivateFocus();
            if (e.KeyCode == Keys.F2)
            {
                // F2 → 文件走原地编辑；文件夹走弹窗（P0.5 FolderRenameDialog）
                if (lstFiles.SelectedItems.Count > 0)
                {
                    var item = lstFiles.SelectedItems[0];
                    if (item.Tag is PanFileInfo fi && fi.IsFolder)
                    {
                        // 文件夹 → 弹出 FolderRenameDialog
                        ShowFolderRenameDialog(fi);
                    }
                    else
                    {
                        // 文件 → 原地 LabelEdit
                        item.BeginEdit();
                    }
                }
                e.Handled = true; e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                if (lstFiles.SelectedItems.Count > 0)
                    DeleteRequested?.Invoke(this, SelectedFiles);
                e.Handled = true; e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.X)
            {
                CutToClipboard();
                e.Handled = true; e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.C)
            {
                CopyToClipboard();
                e.Handled = true; e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.V)
            {
                PasteFromClipboard();
                e.Handled = true; e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.A)
            {
                foreach (ListViewItem it in lstFiles.Items) it.Selected = true;
                e.Handled = true; e.SuppressKeyPress = true;
            }
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "lstFiles_KeyDown 异常"); }
    }

    private void lstFiles_BeforeLabelEdit(object sender, LabelEditEventArgs e)
    {
        // 允许编辑：任何项都 OK；注意占位项也要允许
    }

    private void lstFiles_AfterLabelEdit(object sender, LabelEditEventArgs e)
    {
        if (e.Item < 0 || _snapshot == null) return;
        var item = lstFiles.Items[e.Item];
        var oldDisplay = item.Text;
        var newName = string.IsNullOrEmpty(e.Label) ? oldDisplay : e.Label.Trim();

        // P0.2 LabelEdit 里用户编辑的是完整显示（📁 xxx）→ 去掉前缀拿真实名字
        var prefixFolder = "📁 ";
        var isFolder = oldDisplay.StartsWith(prefixFolder);
        if (isFolder)
        {
            // 如果用户删掉了前缀也允许
            if (newName.StartsWith(prefixFolder))
                newName = newName.Substring(prefixFolder.Length).Trim();
            oldDisplay = oldDisplay.Substring(prefixFolder.Length);
        }
        // 空名称 → 保持原名（CancelEdit 后不变）
        if (string.IsNullOrWhiteSpace(newName)) { e.CancelEdit = true; return; }
        // 非法字符校验
        if (newName.IndexOfAny(_illegalChars) >= 0)
        {
            MessageBox.Show($"名称不能包含非法字符：\\ / : * ? \" < > |", "重命名失败",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.CancelEdit = true; return;
        }
        // 长度限制 ≤ 255
        if (newName.Length > 255) { MessageBox.Show("名称过长（≤ 255 字符）", "重命名失败",
            MessageBoxButtons.OK, MessageBoxIcon.Warning); e.CancelEdit = true; return; }

        // 同目录重名校验（快照 Folders/Files 里不区分大小写比对）
        if (IsNameConflict(newName, isFolder, item.Tag as PanFileInfo))
        {
            MessageBox.Show($"同目录已存在名为「{newName}」的项，请换个名称。", "重命名失败",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.CancelEdit = true; return;
        }

        // ✅ 通过校验 → 取消 LabelEdit 自动改文字（我们手动重刷 UI，确保快照和 UI 一致）
        e.CancelEdit = true;

        try
        {
            if (item.Tag is PanFileInfo fi)
            {
                bool isPlaceholder = (fi.FsId == -1);   // CreateNewFolderPlaceholder 打了 fsId=-1 标记
                if (isPlaceholder)
                {
                    // ------- 新建文件夹：真正写入 PanDirectorySnapshot.Folders -------
                    var parentRel = fi.RelativePath.StartsWith("/") ? fi.RelativePath.Substring(1) : fi.RelativePath;
                    var idxSlash = parentRel.LastIndexOf('/');
                    parentRel = idxSlash < 0 ? "" : parentRel.Substring(0, idxSlash);
                    var newRel = (string.IsNullOrEmpty(parentRel) ? "" : parentRel + "/") + newName;
                    var newFolder = new PanFolderInfo
                    {
                        Name = newName,
                        Path = CurrentPath.TrimEnd('/') + "/" + newName,
                        RelativePath = newRel,
                        Depth = (string.IsNullOrEmpty(parentRel) ? 1 : (parentRel.Count(c => c == '/') + 2))
                    };
                    _snapshot.Folders.Add(newFolder);
                    if (_snapshot.Statistics != null) _snapshot.Statistics.TotalFolderCount++;
                    // 如果新建文件夹的路径刚好在本栏 scope 里，TreeView 要同步插入（否则下一次 LoadFromSnapshot 会看到）
                    RefreshCurrent();
                }
                else
                {
                    // ------- 普通重命名：更新快照 Files 或 Folders 对应条目 Name/Path/RelativePath -------
                    if (fi.IsFolder)
                    {
                        // 1) 找 Folders 里相对路径一致的那个（或者 Name+Path 完全匹配）
                        var target = _snapshot.Folders.FirstOrDefault(f => f.Path.Equals(fi.Path, StringComparison.OrdinalIgnoreCase));
                        if (target != null)
                        {
                            var parentRel = GetParentRelative(target.RelativePath);
                            var newRel = (string.IsNullOrEmpty(parentRel) ? "" : parentRel + "/") + newName;
                            var oldPrefix = target.Path.Substring(0, target.Path.Length - target.Name.Length);
                            target.Name = newName;
                            target.RelativePath = newRel;
                            target.Path = oldPrefix + newName;

                            // 2) 级联：所有**该文件夹下**的 Files/Folders Path + RelativePath 也要同步替换前缀
                            var oldFolderPathPrefix = fi.Path.TrimEnd('/') + "/";
                            var newFolderPathPrefix = target.Path.TrimEnd('/') + "/";
                            var oldRelPrefix = fi.RelativePath.TrimEnd('/') + "/";
                            var newRelPrefix = target.RelativePath.TrimEnd('/') + "/";
                            foreach (var f in _snapshot.Files)
                            {
                                if (f.Path.StartsWith(oldFolderPathPrefix, StringComparison.OrdinalIgnoreCase))
                                {
                                    f.Path = newFolderPathPrefix + f.Path.Substring(oldFolderPathPrefix.Length);
                                }
                                if (f.RelativePath.StartsWith(oldRelPrefix, StringComparison.OrdinalIgnoreCase))
                                {
                                    f.RelativePath = newRelPrefix + f.RelativePath.Substring(oldRelPrefix.Length);
                                    // 同步更新文件 Name（其实文件 Name 没变，是父目录变了；但 RelativePath 前缀是对的就 OK）
                                }
                            }
                            foreach (var subFolder in _snapshot.Folders)
                            {
                                if (subFolder == target) continue;
                                if (subFolder.Path.StartsWith(oldFolderPathPrefix, StringComparison.OrdinalIgnoreCase))
                                {
                                    subFolder.Path = newFolderPathPrefix + subFolder.Path.Substring(oldFolderPathPrefix.Length);
                                }
                                if (subFolder.RelativePath.StartsWith(oldRelPrefix, StringComparison.OrdinalIgnoreCase))
                                {
                                    subFolder.RelativePath = newRelPrefix + subFolder.RelativePath.Substring(oldRelPrefix.Length);
                                }
                            }
                            RefreshCurrent();
                        }
                    }
                    else
                    {
                        // 文件重命名：只改 Files 列表（不级联，因为文件没有子内容）
                        var target = _snapshot.Files.FirstOrDefault(f =>
                            f.Path.Equals(fi.Path, StringComparison.OrdinalIgnoreCase) && f.FsId == fi.FsId);
                        if (target != null)
                        {
                            var oldPrefix = target.Path.Substring(0, target.Path.Length - target.Name.Length);
                            var oldExt = target.Extension ?? "";
                            // 用户输入的新名可能带扩展名，也可能不带 → 我们保留扩展名逻辑（如果新名已有扩展名就用用户输入的）
                            var userHasExt = !string.IsNullOrEmpty(System.IO.Path.GetExtension(newName));
                            target.Name = newName;
                            target.Extension = userHasExt ? System.IO.Path.GetExtension(newName) : oldExt;
                            target.Path = oldPrefix + newName;
                            // RelativePath 同步
                            if (!string.IsNullOrEmpty(target.RelativePath))
                            {
                                var p = target.RelativePath.LastIndexOf('/');
                                target.RelativePath = p < 0 ? newName : target.RelativePath.Substring(0, p + 1) + newName;
                            }
                            RefreshFileListOnly();
                        }
                    }
                    RenameCompleted?.Invoke(this, fi);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "重命名/新建文件夹写入快照失败");
            MessageBox.Show($"写入失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>判断同目录下是否已有同名项（files/folders 合并比对，忽略大小写）</summary>
    private bool IsNameConflict(string newName, bool isFolder, PanFileInfo? current)
    {
        if (_snapshot == null) return false;
        var currentPath = CurrentPath.TrimEnd('/') + "/";
        var newPath = currentPath + newName;
        foreach (var f in _snapshot.Folders)
        {
            if (current != null && f.Path.Equals(current.Path, StringComparison.OrdinalIgnoreCase)) continue;
            if (f.Path.Equals(newPath, StringComparison.OrdinalIgnoreCase)) return true;
        }
        if (!isFolder)
        {
            foreach (var f in _snapshot.Files)
            {
                if (current != null && f.Path.Equals(current.Path, StringComparison.OrdinalIgnoreCase) && f.FsId == current.FsId) continue;
                if (f.Path.Equals(newPath, StringComparison.OrdinalIgnoreCase)) return true;
            }
        }
        return false;
    }
    #endregion

    #region === Step5 扩展：双击文件夹 → 进入下一级 ===
    private void lstFiles_DoubleClick(object sender, EventArgs e)
    {
        if (lstFiles.SelectedItems.Count == 0 || _snapshot == null) return;
        if (lstFiles.SelectedItems[0].Tag is PanFileInfo fi && fi.IsFolder)
        {
            ActivateFocus();
            LoadFromSnapshot(_snapshot, fi.Path);
        }
    }

    /// <summary>激活焦点：标记本栏为焦点栏 + 通知主窗体切换高亮</summary>
    private void ActivateFocus()
    {
        try { FocusActivated?.Invoke(this, EventArgs.Empty); }
        catch (Exception ex) { _logger?.LogWarning(ex, "激活焦点栏失败"); }
    }

    /// <summary>任何鼠标动作都激活焦点（主窗体据此知道「返回上级」作用在哪一栏）</summary>
    private void AnyControl_MouseDown(object? sender, MouseEventArgs e) => ActivateFocus();
    #endregion

    #region === 快照 -> UI 映射（P0.1 基础保留 + 修复：新增文件后正确显示）===
    private void BuildFolderTreeFromSnapshot(string targetPath)
    {
        if (_snapshot == null) return;
        try
        {
            treeFolders.BeginUpdate();
            treeFolders.Nodes.Clear();

            var rootLabel = _snapshot.DirectoryPath.TrimEnd('/');
            rootLabel = rootLabel.Length == 0 ? "/" : rootLabel[(rootLabel.LastIndexOf('/') + 1)..];
            if (string.IsNullOrEmpty(rootLabel)) rootLabel = "/";

            var rootNode = new TreeNode($"{rootLabel}  ({_snapshot.Statistics.TotalFileCount:N0} 个文件, {_snapshot.Statistics.TotalSizeFormatted})")
            { Tag = _snapshot.DirectoryPath };
            treeFolders.Nodes.Add(rootNode);

            var nodeMap = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase) { [""] = rootNode };

            var scopePath = targetPath == _snapshot.DirectoryPath ? ""
                : (targetPath.StartsWith(_snapshot.DirectoryPath, StringComparison.Ordinal)
                    ? targetPath[_snapshot.DirectoryPath.Length..].TrimStart('/')
                    : "");

            foreach (var folder in _snapshot.Folders
                         .OrderBy(f => f.Depth)
                         .ThenBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(scopePath)
                    && !folder.RelativePath.StartsWith(scopePath, StringComparison.OrdinalIgnoreCase)
                    && !scopePath.StartsWith(folder.RelativePath, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(folder.RelativePath))
                {
                    continue;
                }
                var parentRel = GetParentRelative(folder.RelativePath);
                var parentNode = nodeMap.TryGetValue(parentRel, out var p) ? p : rootNode;
                var node = new TreeNode(folder.Name) { Tag = folder };
                parentNode.Nodes.Add(node);
                nodeMap[folder.RelativePath] = node;
            }

            rootNode.Expand();
            var targetRel = targetPath.StartsWith(_snapshot.DirectoryPath, StringComparison.Ordinal)
                ? targetPath[_snapshot.DirectoryPath.Length..].TrimStart('/')
                : "";
            if (nodeMap.TryGetValue(targetRel, out var tn)) { tn.Expand(); treeFolders.SelectedNode = tn; }
        }
        finally { try { treeFolders.EndUpdate(); } catch { /* ignore */ } }
    }

    private void PopulateFileListFromSnapshot(string targetPath)
    {
        if (_snapshot == null) return;
        try
        {
            lstFiles.BeginUpdate();
            lstFiles.Items.Clear();

            var targetRel = targetPath.StartsWith(_snapshot.DirectoryPath, StringComparison.Ordinal)
                ? targetPath[_snapshot.DirectoryPath.Length..].TrimStart('/')
                : "";
            var targetPrefix = targetPath.TrimEnd('/') + "/";

            foreach (var folder in _snapshot.Folders)
            {
                var parentRel = GetParentRelative(folder.RelativePath);
                if (!parentRel.Equals(targetRel, StringComparison.OrdinalIgnoreCase)) continue;
                var item = new ListViewItem("📁 " + folder.Name)
                {
                    Tag = new PanFileInfo
                    {
                        Path = folder.Path, Name = folder.Name, RelativePath = folder.RelativePath,
                        IsFolder = true, SizeBytes = 0, Category = 6, FsId = -2  // fsId=-2 = 文件夹
                    }
                };
                item.SubItems.Add("-");
                item.SubItems.Add("文件夹");
                item.SubItems.Add("-");
                lstFiles.Items.Add(item);
            }

            foreach (var file in _snapshot.Files)
            {
                if (!file.Path.StartsWith(targetPrefix, StringComparison.OrdinalIgnoreCase)) continue;
                var rest = file.Path[targetPrefix.Length..];
                if (rest.Contains('/')) continue;
                var item = new ListViewItem(file.Name) { Tag = file };
                item.SubItems.Add(file.SizeFormatted);
                item.SubItems.Add(file.CategoryName);
                item.SubItems.Add(file.ServerModifiedTime?.ToString("yyyy-MM-dd HH:mm") ?? "-");
                lstFiles.Items.Add(item);
            }
        }
        finally { try { lstFiles.EndUpdate(); } catch { /* ignore */ } }
    }

    private static string GetParentRelative(string rel)
    {
        if (string.IsNullOrEmpty(rel)) return "";
        var idx = rel.LastIndexOf('/');
        return idx < 0 ? "" : rel.Substring(0, idx);
    }
    #endregion

    #region === P0.3: 拖拽初始化 ===
    private void InitDragDrop()
    {
        try
        {
            // ListView 既是拖拽源也是拖放目标
            lstFiles.AllowDrop = true;
            // TreeView 是拖放目标（拖到文件夹节点上）
            treeFolders.AllowDrop = true;
            // pnlTop 拖放目标（拖到路径栏 = 放到当前目录）
            pnlTop.AllowDrop = true;
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "InitDragDrop 失败"); }
    }

    // ---- 拖拽源：ListView ItemDrag → 启动 DoDragDrop ----
    private void lstFiles_ItemDrag(object sender, ItemDragEventArgs e)
    {
        try
        {
            var items = SelectedFiles;
            if (items.Count == 0) return;
            ActivateFocus();

            // P1-3: 先尝试弹出 AI 推荐浮窗（同步阻塞，用户选择或取消后才继续）
            var hintChosen = TryShowDropHintAndWait(items);
            if (hintChosen != null)
            {
                // 用户选了推荐路径 → 直接执行移动，跳过 DoDragDrop
                ExecuteMove(items, hintChosen.TargetPath);
                return;
            }
            // 用户取消浮窗 → 走原生拖拽流程
            var data = new DataObject();
            data.SetData(DragDataFormat, items);
            data.SetData("SourcePath", CurrentPath);
            DragDropEffects effect = DoDragDrop(data, DragDropEffects.Move);
            if (effect == DragDropEffects.Move)
            {
                // 拖拽移动成功 → 刷新
                RefreshAllRequested?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "lstFiles_ItemDrag 失败"); }
    }

    /// <summary>P1-3: 弹出 AI 推荐浮窗并等待用户选择（同步阻塞）</summary>
    private PanDropHint? TryShowDropHintAndWait(List<PanFileInfo> items)
    {
        try
        {
            var hints = BuildDropHints(items);
            if (hints.Count == 0) return null;   // 无推荐 → 直接走原生拖拽

            var cursorPos = Cursor.Position;
            var tcs = new TaskCompletionSource<PanDropHint?>();
            _hintWindow = new PanDropHintWindow();
            _hintWindow.HintSelected += (_, hint) => tcs.TrySetResult(hint);
            _hintWindow.HintCancelled += (_, _) => tcs.TrySetResult(null);
            _hintWindow.FormClosed += (_, _) => tcs.TrySetResult(null);
            _hintWindow.ShowHints(hints, cursorPos);

            // 在拖拽期间轮询浮窗状态（DoDragDrop 阻塞主线程，这里用 Application.DoEvents 模式）
            // 注意：WinForms 拖拽期间无法接收鼠标点击，所以改为「拖拽开始前先选」
            // 此处浮窗已显示，主线程会处理消息直到用户选择或取消
            while (!tcs.Task.IsCompleted)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(20);
                // 用户移动鼠标远离浮窗 → 自动取消（视为放弃浮窗，走原生拖拽）
                if (_hintWindow != null && !_hintWindow.IsDisposed)
                {
                    var dist = Math.Abs(Cursor.Position.X - _hintWindow.Location.X - _hintWindow.Width / 2)
                             + Math.Abs(Cursor.Position.Y - _hintWindow.Location.Y - _hintWindow.Height / 2);
                    if (dist > 400)   // 鼠标远离浮窗
                    {
                        _hintWindow.Close();
                        return null;
                    }
                }
            }
            return tcs.Task.Result;
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "TryShowDropHintAndWait 失败"); return null; }
        finally
        {
            try { _hintWindow?.Close(); _hintWindow = null; } catch { /* ignore */ }
        }
    }

    /// <summary>P1-3: 基于文件特征 + AI 建议构建推荐路径列表</summary>
    private List<PanDropHint> BuildDropHints(List<PanFileInfo> items)
    {
        var hints = new List<PanDropHint>();
        if (items == null || items.Count == 0) return hints;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. 优先：AI 建议中匹配的项
        if (AIRecommendations != null && AIRecommendations.Count > 0)
        {
            foreach (var rec in AIRecommendations.Where(r => r.Type == PanRecommendationType.Move && !string.IsNullOrEmpty(r.DestinationPath)))
            {
                foreach (var fi in items)
                {
                    if (string.IsNullOrEmpty(rec.TargetPath)) continue;
                    if (!fi.Path.Equals(rec.TargetPath, StringComparison.OrdinalIgnoreCase)) continue;
                    var dest = rec.DestinationPath!;
                    if (!seen.Add(dest)) continue;
                    hints.Add(new PanDropHint
                    {
                        TargetPath = dest,
                        DisplayPath = ShortenPath(dest),
                        SourceLabel = "AI",
                        IsFromAI = true,
                        Reason = rec.Reason
                    });
                    break;
                }
            }
        }

        // 2. 启发式：按扩展名分类推荐
        if (_snapshot != null)
        {
            foreach (var fi in items.Take(3))   // 用前 3 个文件推断（避免重复）
            {
                var ext = (fi.Extension ?? "").ToLowerInvariant();
                var suggested = SuggestPathByExtension(ext, fi.Name);
                if (suggested != null && seen.Add(suggested.TargetPath))
                    hints.Add(suggested);
            }
        }

        // 3. 快照中已有的同科目目录（基于文件名关键词）
        if (_snapshot != null)
        {
            foreach (var fi in items.Take(2))
            {
                var keywordHints = SuggestPathByFilenameKeyword(fi.Name);
                foreach (var h in keywordHints)
                {
                    if (seen.Add(h.TargetPath)) hints.Add(h);
                }
            }
        }

        return hints.Take(4).ToList();   // 最多 4 条
    }

    private PanDropHint? SuggestPathByExtension(string ext, string fileName)
    {
        // 按扩展名推荐归类目录
        var category = ext switch
        {
            ".pdf" or ".doc" or ".docx" or ".txt" or ".ppt" or ".pptx" => "学习资料",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => "图片",
            ".mp4" or ".avi" or ".mov" or ".mkv" or ".flv" => "视频",
            ".mp3" or ".wav" or ".flac" or ".aac" => "音频",
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "压缩包",
            ".exe" or ".msi" => "安装包",
            ".xlsx" or ".xls" or ".csv" => "表格",
            _ => null
        };
        if (category == null) return null;
        var dest = SnapshotRootPath.TrimEnd('/') + "/" + category + "/";
        return new PanDropHint
        {
            TargetPath = dest,
            DisplayPath = "/" + category + "/",
            SourceLabel = "启发式",
            IsFromAI = false,
            Reason = $"按扩展名 {ext} 推荐 → {category}/"
        };
    }

    private List<PanDropHint> SuggestPathByFilenameKeyword(string fileName)
    {
        var result = new List<PanDropHint>();
        if (string.IsNullOrEmpty(fileName)) return result;
        var lower = fileName.ToLowerInvariant();

        // 科目关键词匹配
        var subjects = new (string Keyword, string Folder)[]
        {
            ("数学", "学习资料/数学"),
            ("语文", "学习资料/语文"),
            ("英语", "学习资料/英语"),
            ("物理", "学习资料/物理"),
            ("化学", "学习资料/化学"),
            ("生物", "学习资料/生物"),
            ("历史", "学习资料/历史"),
            ("地理", "学习资料/地理"),
            ("政治", "学习资料/政治"),
            ("math", "学习资料/数学"),
            ("english", "学习资料/英语"),
        };
        foreach (var (kw, folder) in subjects)
        {
            if (lower.Contains(kw))
            {
                var dest = SnapshotRootPath.TrimEnd('/') + "/" + folder + "/";
                result.Add(new PanDropHint
                {
                    TargetPath = dest,
                    DisplayPath = "/" + folder + "/",
                    SourceLabel = "关键词",
                    IsFromAI = false,
                    Reason = $"文件名含「{kw}」→ {folder}/"
                });
                break;   // 一个文件只匹配一个科目
            }
        }
        return result;
    }

    private static string ShortenPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return "/";
        if (path.Length <= 30) return path;
        // 保留首尾，中间省略
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 2) return path;
        return "/" + parts[0] + "/…/" + parts[^1] + "/";
    }

    // ---- 拖放目标：TreeView ----
    private void treeFolders_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data!.GetDataPresent(DragDataFormat))
            e.Effect = DragDropEffects.Move;
        else
            e.Effect = DragDropEffects.None;
    }

    private void treeFolders_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data!.GetDataPresent(DragDataFormat)) { e.Effect = DragDropEffects.None; return; }
        var targetPath = GetDropTargetPath_FromTree(sender, e);
        var items = (List<PanFileInfo>)e.Data.GetData(DragDataFormat)!;
        var srcPath = (string?)e.Data.GetData("SourcePath") ?? "";
        if (IsDropForbidden(items, targetPath, srcPath))
            e.Effect = DragDropEffects.None;
        else
            e.Effect = DragDropEffects.Move;
    }

    private void treeFolders_DragDrop(object sender, DragEventArgs e)
    {
        if (!e.Data!.GetDataPresent(DragDataFormat)) return;
        var items = (List<PanFileInfo>)e.Data.GetData(DragDataFormat)!;
        var srcPath = (string?)e.Data.GetData("SourcePath") ?? "";
        var targetPath = GetDropTargetPath_FromTree(sender, e);
        if (IsDropForbidden(items, targetPath, srcPath)) return;
        ExecuteMove(items, targetPath);
    }

    // ---- 拖放目标：ListView / pnlTop（拖到空白处 = 当前目录）----
    private void lstFiles_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data!.GetDataPresent(DragDataFormat))
            e.Effect = DragDropEffects.Move;
        else
            e.Effect = DragDropEffects.None;
    }

    private void lstFiles_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data!.GetDataPresent(DragDataFormat)) { e.Effect = DragDropEffects.None; return; }
        var items = (List<PanFileInfo>)e.Data.GetData(DragDataFormat)!;
        var srcPath = (string?)e.Data.GetData("SourcePath") ?? "";
        // 拖到 ListView 空白 → 目标 = 当前目录
        if (IsDropForbidden(items, CurrentPath, srcPath))
            e.Effect = DragDropEffects.None;
        else
            e.Effect = DragDropEffects.Move;
    }

    private void lstFiles_DragDrop(object sender, DragEventArgs e)
    {
        if (!e.Data!.GetDataPresent(DragDataFormat)) return;
        var items = (List<PanFileInfo>)e.Data.GetData(DragDataFormat)!;
        var srcPath = (string?)e.Data.GetData("SourcePath") ?? "";
        if (IsDropForbidden(items, CurrentPath, srcPath)) return;
        ExecuteMove(items, CurrentPath);
    }

    // ---- 拖放辅助：获取 TreeView 拖放目标路径 ----
    private string GetDropTargetPath_FromTree(object sender, DragEventArgs e)
    {
        try
        {
            var pt = treeFolders.PointToClient(new Point(e.X, e.Y));
            var node = treeFolders.GetNodeAt(pt);
            if (node == null) return CurrentPath;
            if (node.Tag is string s) return s;
            if (node.Tag is PanFolderInfo f) return f.Path;
            return CurrentPath;
        }
        catch { return CurrentPath; }
    }

    // ---- 拖放保护：禁止拖到自身或子目录 ----
    private static bool IsDropForbidden(List<PanFileInfo> items, string targetPath, string srcPath)
    {
        if (items == null || items.Count == 0) return true;
        var target = (targetPath ?? "").TrimEnd('/') + "/";
        foreach (var fi in items)
        {
            var srcTrim = (fi.Path ?? "").TrimEnd('/');
            // 禁止拖到自身
            if (srcTrim.Equals((targetPath ?? "").TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                return true;
            // 禁止拖到自己的子目录（移动文件夹到自己的子目录 = 无限递归）
            if (fi.IsFolder)
            {
                var srcWithSlash = srcTrim + "/";
                if (target.StartsWith(srcWithSlash, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            // 禁止拖到源目录本身（同目录移动无意义）
            if (srcPath.TrimEnd('/').Equals((targetPath ?? "").TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // ---- 执行移动（内存快照更新 Path/RelativePath + 级联子孙）----
    private void ExecuteMove(List<PanFileInfo> items, string targetPath)
    {
        if (_snapshot == null || items.Count == 0) return;
        try
        {
            var target = (targetPath ?? "/").TrimEnd('/');
            var targetPrefix = target + "/";
            var targetRel = target.StartsWith(_snapshot.DirectoryPath, StringComparison.Ordinal)
                ? target[_snapshot.DirectoryPath.Length..].TrimStart('/')
                : "";
            int moved = 0;
            var movedNames = new List<string>();

            foreach (var fi in items)
            {
                var oldPath = fi.Path;
                var oldName = fi.Name;
                var newPath = targetPrefix + oldName;

                // 同路径跳过
                if (oldPath.Equals(newPath, StringComparison.OrdinalIgnoreCase)) continue;

                // 目标重名检查
                bool conflict = _snapshot.Folders.Any(f => f.Path.Equals(newPath, StringComparison.OrdinalIgnoreCase))
                             || _snapshot.Files.Any(f => f.Path.Equals(newPath, StringComparison.OrdinalIgnoreCase));
                if (conflict)
                {
                    // 自动加 (2) 后缀
                    var ext = fi.IsFolder ? "" : (System.IO.Path.GetExtension(oldName) ?? "");
                    var nameNoExt = fi.IsFolder ? oldName : System.IO.Path.GetFileNameWithoutExtension(oldName);
                    int suffix = 2;
                    while (true)
                    {
                        var tryName = fi.IsFolder ? $"{nameNoExt}({suffix})" : $"{nameNoExt}({suffix}){ext}";
                        var tryPath = targetPrefix + tryName;
                        if (!_snapshot.Folders.Any(f => f.Path.Equals(tryPath, StringComparison.OrdinalIgnoreCase))
                            && !_snapshot.Files.Any(f => f.Path.Equals(tryPath, StringComparison.OrdinalIgnoreCase)))
                        { newPath = tryPath; oldName = tryName; break; }
                        suffix++;
                    }
                }

                if (fi.IsFolder)
                {
                    // 文件夹移动：更新 Folders 里该条目 + 级联所有子孙
                    var oldFolderTrim = oldPath.TrimEnd('/');
                    var newFolderTrim = newPath.TrimEnd('/');
                    var oldPrefix = oldFolderTrim + "/";
                    var newPrefix = newFolderTrim + "/";

                    // 更新文件夹本身
                    var folderEntry = _snapshot.Folders.FirstOrDefault(f => f.Path.Equals(oldPath, StringComparison.OrdinalIgnoreCase));
                    if (folderEntry != null)
                    {
                        var parentRel = GetParentRelative(targetRel);
                        folderEntry.Path = newFolderTrim;
                        folderEntry.Name = oldName;
                        folderEntry.RelativePath = (string.IsNullOrEmpty(parentRel) ? "" : parentRel + "/") + oldName;
                        folderEntry.Depth = string.IsNullOrEmpty(targetRel) ? 1 : (targetRel.Count(c => c == '/') + 2);
                    }
                    // 级联子孙文件夹
                    foreach (var sub in _snapshot.Folders)
                    {
                        if (sub.Path.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            sub.Path = newPrefix + sub.Path.Substring(oldPrefix.Length);
                            sub.RelativePath = folderEntry!.RelativePath.TrimEnd('/') + "/" + sub.RelativePath.Substring((folderEntry.RelativePath.TrimEnd('/') + "/").Length);
                        }
                    }
                    // 级联子孙文件
                    foreach (var file in _snapshot.Files)
                    {
                        if (file.Path.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            file.Path = newPrefix + file.Path.Substring(oldPrefix.Length);
                            var oldRelPrefix = (fi.RelativePath ?? "").TrimEnd('/') + "/";
                            var newRelPrefix = folderEntry!.RelativePath.TrimEnd('/') + "/";
                            if (!string.IsNullOrEmpty(oldRelPrefix) && file.RelativePath.StartsWith(oldRelPrefix, StringComparison.OrdinalIgnoreCase))
                                file.RelativePath = newRelPrefix + file.RelativePath.Substring(oldRelPrefix.Length);
                        }
                    }
                    moved++; movedNames.Add(oldName + "/");
                }
                else
                {
                    // 文件移动：只更新 Files 里该条目
                    var fileEntry = _snapshot.Files.FirstOrDefault(f => f.Path.Equals(oldPath, StringComparison.OrdinalIgnoreCase) && f.FsId == fi.FsId);
                    if (fileEntry != null)
                    {
                        fileEntry.Path = newPath;
                        fileEntry.Name = oldName;
                        var parentRel = GetParentRelative(targetRel);
                        fileEntry.RelativePath = (string.IsNullOrEmpty(parentRel) ? "" : parentRel + "/") + oldName;
                    }
                    moved++; movedNames.Add(oldName);
                }
            }

            if (moved > 0)
            {
                RefreshCurrent();
                RefreshAllRequested?.Invoke(this, EventArgs.Empty);
                MoveRequested?.Invoke(this, (items, targetPath, "Move"));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ExecuteMove 失败");
            MessageBox.Show($"移动失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    #endregion

    #region === P0.3: 剪贴板（Ctrl+X/C/V）===
    private void CutToClipboard()
    {
        var items = SelectedFiles;
        if (items.Count == 0) return;
        if (SharedClipboard == null) SharedClipboard = new PanClipboardState();
        SharedClipboard.Action = ClipboardAction.Cut;
        SharedClipboard.Items = items.ToList();
        SharedClipboard.SourceDirectory = CurrentPath;
        // 视觉提示：剪切项变灰
        foreach (ListViewItem it in lstFiles.SelectedItems) it.ForeColor = Color.Gray;
        MoveRequested?.Invoke(this, (items, CurrentPath, "Cut"));
    }

    private void CopyToClipboard()
    {
        var items = SelectedFiles;
        if (items.Count == 0) return;
        if (SharedClipboard == null) SharedClipboard = new PanClipboardState();
        SharedClipboard.Action = ClipboardAction.Copy;
        SharedClipboard.Items = items.ToList();
        SharedClipboard.SourceDirectory = CurrentPath;
        MoveRequested?.Invoke(this, (items, CurrentPath, "Copy"));
    }

    public void PasteFromClipboard()
    {
        if (SharedClipboard == null || SharedClipboard.Items.Count == 0)
        {
            SystemSounds_Blip();
            return;
        }
        if (SharedClipboard.Action == ClipboardAction.Cut)
        {
            // 剪切粘贴 = 移动
            if (IsDropForbidden(SharedClipboard.Items, CurrentPath, SharedClipboard.SourceDirectory ?? ""))
            {
                MessageBox.Show("无法粘贴：目标目录是源目录或源目录的子目录。", "粘贴失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ExecuteMove(SharedClipboard.Items, CurrentPath);
            SharedClipboard.Action = ClipboardAction.None;
            SharedClipboard.Items.Clear();
        }
        else if (SharedClipboard.Action == ClipboardAction.Copy)
        {
            // 复制粘贴 = 在目标目录创建副本（内存模拟）
            ExecuteCopy(SharedClipboard.Items, CurrentPath);
        }
    }

    private void ExecuteCopy(List<PanFileInfo> items, string targetPath)
    {
        if (_snapshot == null) return;
        try
        {
            var target = (targetPath ?? "/").TrimEnd('/');
            var targetPrefix = target + "/";
            var targetRel = target.StartsWith(_snapshot.DirectoryPath, StringComparison.Ordinal)
                ? target[_snapshot.DirectoryPath.Length..].TrimStart('/')
                : "";
            int copied = 0;

            foreach (var fi in items)
            {
                var newName = fi.Name;
                var newPath = targetPrefix + newName;

                // 重名自动加后缀
                while (_snapshot.Files.Any(f => f.Path.Equals(newPath, StringComparison.OrdinalIgnoreCase))
                    || _snapshot.Folders.Any(f => f.Path.Equals(newPath, StringComparison.OrdinalIgnoreCase)))
                {
                    var ext = fi.IsFolder ? "" : (System.IO.Path.GetExtension(newName) ?? "");
                    var nameNoExt = fi.IsFolder ? newName : System.IO.Path.GetFileNameWithoutExtension(newName);
                    var suffix = 2;
                    while (true)
                    {
                        var tryName = fi.IsFolder ? $"{nameNoExt}({suffix})" : $"{nameNoExt}({suffix}){ext}";
                        var tryPath = targetPrefix + tryName;
                        if (!_snapshot.Files.Any(f => f.Path.Equals(tryPath, StringComparison.OrdinalIgnoreCase))
                            && !_snapshot.Folders.Any(f => f.Path.Equals(tryPath, StringComparison.OrdinalIgnoreCase)))
                        { newName = tryName; newPath = tryPath; break; }
                        suffix++;
                    }
                    break;
                }

                var parentRel = GetParentRelative(targetRel);
                var newRel = (string.IsNullOrEmpty(parentRel) ? "" : parentRel + "/") + newName;

                if (fi.IsFolder)
                {
                    _snapshot.Folders.Add(new PanFolderInfo
                    {
                        Name = newName, Path = newPath, RelativePath = newRel,
                        Depth = string.IsNullOrEmpty(targetRel) ? 1 : (targetRel.Count(c => c == '/') + 2)
                    });
                    if (_snapshot.Statistics != null) _snapshot.Statistics.TotalFolderCount++;
                }
                else
                {
                    var copy = new PanFileInfo
                    {
                        FsId = new Random().Next(100000, int.MaxValue), // 模拟新 FsId
                        Name = newName, Path = newPath, RelativePath = newRel,
                        SizeBytes = fi.SizeBytes, Extension = fi.Extension,
                        Category = fi.Category, IsFolder = false,
                        ServerModifiedTime = DateTime.UtcNow, IsPotentialDuplicate = false, IsJunkFile = false
                    };
                    _snapshot.Files.Add(copy);
                    if (_snapshot.Statistics != null)
                    {
                        _snapshot.Statistics.TotalFileCount++;
                        _snapshot.Statistics.TotalSizeBytes += fi.SizeBytes;
                    }
                }
                copied++;
            }

            if (copied > 0)
            {
                RefreshCurrent();
                RefreshAllRequested?.Invoke(this, EventArgs.Empty);
                MoveRequested?.Invoke(this, (items, targetPath, "Copy"));
            }
        }
        catch (Exception ex) { _logger?.LogError(ex, "ExecuteCopy 失败"); }
    }

    private static void SystemSounds_Blip()
    {
        try { System.Media.SystemSounds.Beep.Play(); } catch { /* ignore */ }
    }
    #endregion

    #region === P0.3: 右键菜单 ContextMenuStrip ===
    private ContextMenuStrip? _ctxMenu;

    private void InitContextMenu()
    {
        try
        {
            _ctxMenu = new ContextMenuStrip(components);
            _ctxMenu.Items.Add("✂️ 剪切  Ctrl+X", null, (_, _) => CutToClipboard());
            _ctxMenu.Items.Add("📋 复制  Ctrl+C", null, (_, _) => CopyToClipboard());
            _ctxMenu.Items.Add("📎 粘贴  Ctrl+V", null, (_, _) => PasteFromClipboard());
            _ctxMenu.Items.Add(new ToolStripSeparator());
            _ctxMenu.Items.Add("✏️ 重命名  F2", null, (_, _) =>
            {
                if (lstFiles.SelectedItems.Count > 0)
                {
                    var item = lstFiles.SelectedItems[0];
                    if (item.Tag is PanFileInfo fi && fi.IsFolder)
                        ShowFolderRenameDialog(fi);
                    else
                        item.BeginEdit();
                }
            });
            _ctxMenu.Items.Add("🗑️ 删除  Delete", null, (_, _) =>
            {
                if (lstFiles.SelectedItems.Count > 0) DeleteRequested?.Invoke(this, SelectedFiles);
            });
            _ctxMenu.Items.Add(new ToolStripSeparator());
            _ctxMenu.Items.Add("📁 新建文件夹", null, (_, _) => CreateNewFolderPlaceholder());
            _ctxMenu.Items.Add("🔍 全选  Ctrl+A", null, (_, _) =>
            {
                foreach (ListViewItem it in lstFiles.Items) it.Selected = true;
            });
            lstFiles.ContextMenuStrip = _ctxMenu;
            treeFolders.ContextMenuStrip = _ctxMenu;

            // 右键菜单 Opening：根据选中状态禁用/启用菜单项
            _ctxMenu.Opening += (_, _) =>
            {
                bool hasSelection = lstFiles.SelectedItems.Count > 0;
                _ctxMenu.Items[0]!.Enabled = hasSelection;   // 剪切
                _ctxMenu.Items[1]!.Enabled = hasSelection;   // 复制
                _ctxMenu.Items[2]!.Enabled = SharedClipboard != null && SharedClipboard.Items.Count > 0;  // 粘贴
                _ctxMenu.Items[4]!.Enabled = hasSelection && lstFiles.SelectedItems.Count == 1;  // 重命名
                _ctxMenu.Items[5]!.Enabled = hasSelection;   // 删除
            };
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "InitContextMenu 失败"); }
    }
    #endregion

    #region === P0.5: 文件夹重命名弹窗 ===
    /// <summary>
    /// 弹出 FolderRenameDialog，用户确认后执行重命名（含大小后缀生成最终名称）。
    /// 支持追加大小后缀：_[3.25 GB] / _(3.25GB) / 【3.25GB】 / -3.25GB / 前缀模式
    /// </summary>
    private void ShowFolderRenameDialog(PanFileInfo folderInfo)
    {
        try
        {
            if (_snapshot == null) return;
            using var dlg = new FolderRenameDialog(_snapshot, folderInfo, GetLastRenameOptions());
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.Result != null)
            {
                // 保存用户偏好（下次打开弹窗时恢复）
                SaveLastRenameOptions(dlg.Result);

                // 根据用户选择生成最终名称（基础名 + 大小后缀）
                var baseName = dlg.NewName;
                var suffix = BuildSizeSuffixFromOptions(dlg.Result);
                var finalName = string.IsNullOrEmpty(suffix)
                    ? baseName
                    : (dlg.Result.Position == SuffixPosition.Prefix || dlg.Result.SuffixFormat == FolderSizeSuffixFormat.PrefixGB
                       ? suffix + baseName : baseName + suffix);

                // 执行重命名（复用 P0.2 的快照更新逻辑）
                RenameFolderInSnapshot(folderInfo, finalName);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ShowFolderRenameDialog 失败");
            MessageBox.Show($"重命名失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>从 FolderRenameOptions 生成大小后缀字符串</summary>
    private static string BuildSizeSuffixFromOptions(FolderRenameOptions opts)
    {
        if (!opts.AppendSizeSuffix) return "";
        var sizeStr = FormatSizeHuman(opts.ComputedSizeBytes, opts.DecimalPlaces);
        return opts.SuffixFormat switch
        {
            FolderSizeSuffixFormat.ParenthesisGB => $"_({sizeStr.Replace(" ", "")})",
            FolderSizeSuffixFormat.BracketGB     => $"_[{sizeStr}]",
            FolderSizeSuffixFormat.ChineseBracket => $"【{sizeStr.Replace(" ", "")}】",
            FolderSizeSuffixFormat.HyphenGB       => $"-{sizeStr.Replace(" ", "")}",
            FolderSizeSuffixFormat.PrefixGB      => $"[{sizeStr}]",
            _ => ""
        };
    }

    /// <summary>格式化字节数为人类可读（与 FolderRenameDialog.FormatHumanReadable 一致）</summary>
    private static string FormatSizeHuman(long bytes, int decimalPlaces = 2)
    {
        if (bytes < 0) return "0 B";
        double len = bytes;
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        while (len >= 1024 && order < units.Length - 1) { order++; len /= 1024; }
        var fmt = "0." + new string('0', decimalPlaces);
        return $"{len.ToString(fmt)} {units[order]}";
    }

    /// <summary>在快照中执行文件夹重命名（级联子孙 Path/RelativePath）</summary>
    private void RenameFolderInSnapshot(PanFileInfo folderInfo, string newName)
    {
        if (_snapshot == null) return;

        // 非法字符校验
        char[] illegal = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
        if (newName.IndexOfAny(illegal) >= 0)
        {
            MessageBox.Show($"名称不能包含非法字符：\\ / : * ? \" < > |", "重命名失败",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(newName)) return;
        if (newName.Length > 255)
        {
            MessageBox.Show("名称过长（≤ 255 字符）", "重命名失败",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // 同目录重名检查
        var parentPath = folderInfo.Path.Substring(0, folderInfo.Path.Length - folderInfo.Name.Length);
        var newPath = parentPath.TrimEnd('/') + "/" + newName;
        if (IsNameConflict(newName, true, folderInfo))
        {
            MessageBox.Show($"同目录已存在名为「{newName}」的项。", "重命名失败",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // 更新 Folders 里该条目 + 级联所有子孙
        var target = _snapshot.Folders.FirstOrDefault(f => f.Path.Equals(folderInfo.Path, StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            // 可能是 ListView 里 fsId=-2 的文件夹条目（从 Folders 映射来的）
            target = _snapshot.Folders.FirstOrDefault(f =>
                f.Path.Equals(folderInfo.Path, StringComparison.OrdinalIgnoreCase));
        }
        if (target != null)
        {
            var oldPrefix = folderInfo.Path.TrimEnd('/') + "/";
            var newPrefix = newPath.TrimEnd('/') + "/";
            var parentRel = GetParentRelative(target.RelativePath);
            var newRel = (string.IsNullOrEmpty(parentRel) ? "" : parentRel + "/") + newName;

            target.Name = newName;
            target.Path = newPath;
            var oldRelPrefix = target.RelativePath.TrimEnd('/') + "/";
            target.RelativePath = newRel;

            // 级联子孙文件夹
            foreach (var sub in _snapshot.Folders)
            {
                if (sub == target) continue;
                if (sub.Path.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase))
                    sub.Path = newPrefix + sub.Path[oldPrefix.Length..];
                if (!string.IsNullOrEmpty(oldRelPrefix) && sub.RelativePath.StartsWith(oldRelPrefix, StringComparison.OrdinalIgnoreCase))
                    sub.RelativePath = oldRelPrefix.Length > 0
                        ? newRel.TrimEnd('/') + "/" + sub.RelativePath[oldRelPrefix.Length..]
                        : sub.RelativePath;
            }
            // 级联子孙文件
            foreach (var file in _snapshot.Files)
            {
                if (file.Path.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase))
                    file.Path = newPrefix + file.Path[oldPrefix.Length..];
                if (!string.IsNullOrEmpty(oldRelPrefix) && file.RelativePath.StartsWith(oldRelPrefix, StringComparison.OrdinalIgnoreCase))
                    file.RelativePath = oldRelPrefix.Length > 0
                        ? newRel.TrimEnd('/') + "/" + file.RelativePath[oldRelPrefix.Length..]
                        : file.RelativePath;
            }

            RefreshCurrent();
            RenameCompleted?.Invoke(this, folderInfo);
            MoveRequested?.Invoke(this, (new List<PanFileInfo> { folderInfo }, newPath, "Rename"));
        }
    }

    // ---- 用户偏好持久化（简化版：存到 Properties.Settings.Default）----
    // P1 阶段可改为 IUserSettingsService，目前先用静态字段临时缓存
    private static FolderRenameOptions? _lastOptionsCache;
    private static FolderRenameOptions GetLastRenameOptions()
    {
        return _lastOptionsCache ?? new FolderRenameOptions
        {
            AppendSizeSuffix = false,
            SuffixFormat = FolderSizeSuffixFormat.BracketGB,
            DecimalPlaces = 2,
            Position = SuffixPosition.Suffix
        };
    }
    private static void SaveLastRenameOptions(FolderRenameOptions opts)
    {
        _lastOptionsCache = new FolderRenameOptions
        {
            AppendSizeSuffix = opts.AppendSizeSuffix,
            SuffixFormat = opts.SuffixFormat,
            DecimalPlaces = opts.DecimalPlaces,
            Position = opts.Position,
            ShowCountInPreview = opts.ShowCountInPreview
        };
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
            cboPath.BackColor = colors.Surface; cboPath.ForeColor = colors.TextPrimary;
            txtSearch.BackColor = colors.Surface; txtSearch.ForeColor = colors.TextPrimary;
            lblPathCaption.ForeColor = colors.TextSecondary;
            treeFolders.BackColor = colors.Surface; treeFolders.ForeColor = colors.TextPrimary;
            treeFolders.LineColor = colors.Divider;
            lstFiles.BackColor = colors.Surface; lstFiles.ForeColor = colors.TextPrimary;
            splitMain.BackColor = _isFocused ? _focusBorder : colors.Divider;   // 焦点边框优先于主题
            pnlTop.BackColor = _isFocused ? _focusColor : _unfocusColor;       // 焦点路径栏色
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "PanNavigatorPanel.ApplyTheme 失败，忽略"); }
        ThemeChanged?.Invoke(this, colors);
    }
    #endregion
}