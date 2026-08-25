

using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.PanAnalysis;
using Microsoft.Extensions.Logging;
using LearningAssistant.Forms.Learning;

namespace LearningAssistant.Forms.UserControls.Pan;

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
   public event EventHandler<PanMoveEventArgs>? Moved;          // 移动完成通知
   public event EventHandler<string>? MessageLogged;            // 日志消息通知
   /// <summary>手工操作生成待办（重命名/移动/删除/新建）→ 主窗体加入 _allTodos，由"执行待办"统一同步网盘</summary>
   public event EventHandler<PanTodoItem>? TodoGenerated;
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

   #region === 路径比较辅助（解决 Ordinal 大小写敏感 + 尾斜杠不统一 导致的路径切换不刷新）===
   private static string NormalizePath(string? p)
   {
       if (string.IsNullOrEmpty(p)) return "/";
       var t = p.Trim().Replace('\\', '/');
       if (!t.StartsWith('/')) t = "/" + t;
       if (t.Length > 1) t = t.TrimEnd('/');
       return t;
   }
   private static bool PathEqualIgnoreCase(string? a, string? b)
       => string.Equals(NormalizePath(a), NormalizePath(b), StringComparison.OrdinalIgnoreCase);
   private static bool PathStartsWithIgnoreCase(string? path, string? prefix)
   {
       var p = NormalizePath(path);
       var pre = NormalizePath(prefix);
       if (pre == "/") return true;
       if (p.Equals(pre, StringComparison.OrdinalIgnoreCase)) return true;
       return p.StartsWith(pre + "/", StringComparison.OrdinalIgnoreCase);
   }
   private static string? TrimPrefixIgnoreCase(string? path, string? prefix)
   {
       if (path == null) return null;
       var p = NormalizePath(path);
       var pre = NormalizePath(prefix);
       if (pre == "/") return p.TrimStart('/');
       if (p.Equals(pre, StringComparison.OrdinalIgnoreCase)) return "";
       if (p.StartsWith(pre + "/", StringComparison.OrdinalIgnoreCase))
           return p[(pre.Length + 1)..];
       // 不匹配时返回原始 path，避免清空导致默认命中根
       return p;
   }
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

       if (string.IsNullOrEmpty(path)) return;
       // 修复：统一尾斜杠，避免「/a/b」和「/a/b/」被视为不同路径导致不刷新
       var snapshotDir = (_snapshot.DirectoryPath ?? "/").TrimEnd('/') + "/";
       var normalized = path.TrimEnd('/') + "/";
       if (normalized != "/") normalized = normalized.TrimEnd('/');  // 根路径特殊处理
       // 若快照目录是 /a 而目标是 /a/b，直接用 path（NavTo 会走 LoadFromSnapshot，内部再切 relative）
       if (normalized != CurrentPath)
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
                   // ------- 普通重命名：生成待办（不直接改快照，由"执行待办"统一同步网盘 + 更新快照） -------
                   EmitRenameTodo(fi, newName);
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

           var snapshotDir = _snapshot.DirectoryPath ?? "/";
           var rootLabel = snapshotDir.TrimEnd('/');
           rootLabel = rootLabel.Length == 0 ? "/" : rootLabel[(rootLabel.LastIndexOf('/') + 1)..];
           if (string.IsNullOrEmpty(rootLabel)) rootLabel = "/";

           var rootNode = new TreeNode($"{rootLabel}  ({_snapshot.Statistics?.TotalFileCount ?? 0:N0} 个文件, {_snapshot.Statistics?.TotalSizeFormatted ?? "-"})")
           { Tag = snapshotDir };
           treeFolders.Nodes.Add(rootNode);

           var nodeMap = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase) { [""] = rootNode };

           // 修复：使用 PathStartsWithIgnoreCase（大小不敏感 + 斜杠不敏感）
           var targetRelRooted = PathEqualIgnoreCase(targetPath, snapshotDir) ? "" : TrimPrefixIgnoreCase(targetPath, snapshotDir) ?? "";
           // scope 过滤：只要非 scopePath 子孙，且不是 scopePath 的祖先（展开树需要祖先链）
           var scopeRel = targetRelRooted;

           foreach (var folder in _snapshot.Folders
                        .OrderBy(f => f.Depth)
                        .ThenBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase))
           {
               if (folder == null) continue;
               var relPath = folder.RelativePath ?? "";
               if (!string.IsNullOrEmpty(scopeRel)
                   && !string.IsNullOrEmpty(relPath)
                   && !relPath.StartsWith(scopeRel, StringComparison.OrdinalIgnoreCase)
                   && !scopeRel.StartsWith(relPath, StringComparison.OrdinalIgnoreCase))
               {
                   continue;
               }
               var parentRel = GetParentRelative(relPath);
               var parentNode = nodeMap.TryGetValue(parentRel, out var p) ? p : rootNode;
               var node = new TreeNode(folder.Name ?? "") { Tag = folder };
               parentNode.Nodes.Add(node);
               nodeMap[relPath] = node;
           }

           rootNode.Expand();
           if (nodeMap.TryGetValue(targetRelRooted, out var tnSel)) { tnSel.Expand(); treeFolders.SelectedNode = tnSel; }
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

           var snapshotDir = _snapshot.DirectoryPath ?? "/";
           var targetRel = PathEqualIgnoreCase(targetPath, snapshotDir) ? "" : (TrimPrefixIgnoreCase(targetPath, snapshotDir) ?? "");
           // 修复：用 OrdinalIgnoreCase 的相对路径比较，避免按绝对路径长度切片的大小写错位问题
           var targetNorm = NormalizePath(targetPath);

           foreach (var folder in _snapshot.Folders)
           {
               if (folder == null) continue;
               var parentRel = GetParentRelative(folder.RelativePath ?? "");
               if (!parentRel.Equals(targetRel, StringComparison.OrdinalIgnoreCase)) continue;
               var item = new ListViewItem("📁 " + (folder.Name ?? ""))
               {
                   Tag = new PanFileInfo
                   {
                       Path = folder.Path ?? "", Name = folder.Name ?? "", RelativePath = folder.RelativePath ?? "",
                       IsFolder = true, SizeBytes = 0, Category = 6, FsId = -2
                   }
               };
               item.SubItems.Add("-");
               item.SubItems.Add("文件夹");
               item.SubItems.Add("-");
               lstFiles.Items.Add(item);
           }

           foreach (var file in _snapshot.Files)
           {
               if (file == null || string.IsNullOrEmpty(file.Path)) continue;
               var fileNorm = NormalizePath(file.Path);
               // 用相对路径匹配：file.RelativePath 的父级 = targetRel
               var relF = file.RelativePath ?? "";
               if (!string.IsNullOrEmpty(relF) && relF.Contains('/'))
               {
                   var parentRelF = GetParentRelative(relF);
                   if (!parentRelF.Equals(targetRel, StringComparison.OrdinalIgnoreCase)) continue;
               }
               else
               {
                   // 无 "/" 表示 root-level 文件：targetRel 必须为空
                   if (!string.IsNullOrEmpty(targetRel)) continue;
               }
               var item = new ListViewItem(file.Name ?? "") { Tag = file };
               item.SubItems.Add(file.SizeFormatted ?? "-");
               item.SubItems.Add(file.CategoryName ?? "-");
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

   // ---- 右键上下文菜单 ----
   private void InitContextMenu()
   {
       try
       {
           var cms = new ContextMenuStrip();
           cms.Items.Add("✂️ 剪切", null, (s, e) => CutToClipboard());
           cms.Items.Add("📋 复制", null, (s, e) => CopyToClipboard());
           cms.Items.Add("📥 粘贴", null, (s, e) => PasteFromClipboard());
           cms.Items.Add(new ToolStripSeparator());
           cms.Items.Add("✏️ 重命名 (F2)", null, (s, e) =>
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
           cms.Items.Add("🗑️ 删除 (Delete)", null, (s, e) =>
           {
               if (lstFiles.SelectedItems.Count > 0)
                   DeleteRequested?.Invoke(this, SelectedFiles);
           });
           lstFiles.ContextMenuStrip = cms;
       }
       catch (Exception ex) { _logger?.LogWarning(ex, "InitContextMenu 失败"); }
   }

   // ---- 文件夹重命名弹窗 ----
   private void ShowFolderRenameDialog(PanFileInfo fi)
   {
       try
       {
           using var dlg = new FolderRenameDialog(_snapshot, fi);
           if (dlg.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.NewName))
           {
               // 不直接改快照 → 生成 Rename 待办，由"执行待办"统一同步网盘 + 更新快照
               EmitRenameTodo(fi, dlg.NewName);
           }
       }
       catch (Exception ex) { _logger?.LogWarning(ex, "ShowFolderRenameDialog 失败"); }
   }

   /// <summary>生成重命名待办并通知主窗体（不直接改快照，等执行待办时统一处理）</summary>
   private void EmitRenameTodo(PanFileInfo fi, string newName)
   {
       try
       {
           var todo = new PanTodoItem
           {
               Type = PanRecommendationType.Rename,
               SourcePath = fi.Path,
               SourceName = fi.Name,
               NewName = newName,
               IsFolder = fi.IsFolder,
               SourceFsId = fi.FsId,
               Reason = $"手工重命名：{fi.Name} → {newName}",
               Status = TodoStatus.Confirmed
           };
           TodoGenerated?.Invoke(this, todo);
           MessageLogged?.Invoke(this, $"📝 已生成重命名待办：{fi.Name} → {newName}（点击「执行待办」同步到网盘）");
       }
       catch (Exception ex) { _logger?.LogWarning(ex, "EmitRenameTodo 失败"); }
   }

   // ---- 剪贴板操作 ----
   private void CutToClipboard()
   {
       try
       {
           var items = SelectedFiles;
           if (items.Count == 0) return;
           if (SharedClipboard == null) SharedClipboard = new PanClipboardState();
           SharedClipboard.Action = ClipboardAction.Cut;
           SharedClipboard.Items = items.ToList();
           SharedClipboard.SourceDirectory = CurrentPath;
           MessageLogged?.Invoke(this, $"✂️ 剪切 {items.Count} 项到剪贴板");
       }
       catch (Exception ex) { _logger?.LogWarning(ex, "CutToClipboard 失败"); }
   }

   private void CopyToClipboard()
   {
       try
       {
           var items = SelectedFiles;
           if (items.Count == 0) return;
           if (SharedClipboard == null) SharedClipboard = new PanClipboardState();
           SharedClipboard.Action = ClipboardAction.Copy;
           SharedClipboard.Items = items.ToList();
           SharedClipboard.SourceDirectory = CurrentPath;
           MessageLogged?.Invoke(this, $"📋 复制 {items.Count} 项到剪贴板");
       }
       catch (Exception ex) { _logger?.LogWarning(ex, "CopyToClipboard 失败"); }
   }

   public void PasteFromClipboard()
   {
       try
       {
           if (SharedClipboard == null || SharedClipboard.Items.Count == 0)
           {
               MessageLogged?.Invoke(this, "剪贴板为空");
               return;
           }
           var action = SharedClipboard.Action == ClipboardAction.Cut ? "Move" : "Copy";
           MoveRequested?.Invoke(this, (SharedClipboard.Items, CurrentPath, action));
       }
       catch (Exception ex) { _logger?.LogWarning(ex, "PasteFromClipboard 失败"); }
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
                    var suffix = "_" + DateTime.Now.ToString("HHmmss");
                    var ext = fi.IsFolder ? "" : System.IO.Path.GetExtension(oldName);
                    var nameNoExt = fi.IsFolder ? oldName : System.IO.Path.GetFileNameWithoutExtension(oldName);
                    newPath = targetPrefix + nameNoExt + suffix + ext;
                }

                // 实际移动：直接改 PanFileInfo / PanFolderInfo 的路径（内存快照）
                if (fi.IsFolder)
                {
                    var folder = _snapshot.Folders.FirstOrDefault(f => f.Path != null && f.Path.Equals(oldPath, StringComparison.OrdinalIgnoreCase));
                    if (folder != null)
                    {
                        // 级联更新子目录的 RelativePath/Path
                        var oldFolderPrefix = oldPath.TrimEnd('/') + "/";
                        var newFolderPrefix = newPath.TrimEnd('/') + "/";
                        foreach (var sub in _snapshot.Folders)
                        {
                            if (sub.Path == null) continue;
                            if (sub.Path.Equals(oldPath, StringComparison.OrdinalIgnoreCase))
                            {
                                var newSubRel = string.IsNullOrEmpty(newFolderPrefix.Trim('/')) ? "" : newFolderPrefix.TrimEnd('/');
                                if (newSubRel.StartsWith(_snapshot.DirectoryPath, StringComparison.OrdinalIgnoreCase))
                                    newSubRel = newSubRel[_snapshot.DirectoryPath.Length..].TrimStart('/');
                                sub.Path = newPath;
                                sub.RelativePath = newSubRel;
                                sub.Name = System.IO.Path.GetFileName(newPath.TrimEnd('/'));
                            }
                            else if (sub.Path.StartsWith(oldFolderPrefix, StringComparison.OrdinalIgnoreCase))
                            {
                                var rest = sub.Path[oldFolderPrefix.Length..];
                                var newSubPath = newFolderPrefix + rest;
                                var newSubRel = newSubPath;
                                if (newSubRel.StartsWith(_snapshot.DirectoryPath, StringComparison.OrdinalIgnoreCase))
                                    newSubRel = newSubRel[_snapshot.DirectoryPath.Length..].TrimStart('/');
                                sub.Path = newSubPath;
                                sub.RelativePath = newSubRel;
                            }
                        }
                        // 级联更新文件的 Path
                        foreach (var f in _snapshot.Files)
                        {
                            if (f.Path == null) continue;
                            if (f.Path.StartsWith(oldFolderPrefix, StringComparison.OrdinalIgnoreCase))
                            {
                                f.Path = newFolderPrefix + f.Path[oldFolderPrefix.Length..];
                                var rel = f.Path;
                                if (rel.StartsWith(_snapshot.DirectoryPath, StringComparison.OrdinalIgnoreCase))
                                    rel = rel[_snapshot.DirectoryPath.Length..].TrimStart('/');
                                f.RelativePath = rel;
                            }
                        }
                    }
                }
                else
                {
                    var file = _snapshot.Files.FirstOrDefault(f => f.Path != null && f.Path.Equals(oldPath, StringComparison.OrdinalIgnoreCase));
                    if (file != null)
                    {
                        file.Path = newPath;
                        file.Name = System.IO.Path.GetFileName(newPath);
                        var rel = newPath;
                        if (rel.StartsWith(_snapshot.DirectoryPath, StringComparison.OrdinalIgnoreCase))
                            rel = rel[_snapshot.DirectoryPath.Length..].TrimStart('/');
                        file.RelativePath = rel;
                    }
                }
                moved++;
                movedNames.Add(oldName + " → " + System.IO.Path.GetFileName(newPath.TrimEnd('/')));
            }

            if (moved > 0)
            {
                Moved?.Invoke(this, new PanMoveEventArgs { Items = items, DestinationPath = target });
                RefreshAllRequested?.Invoke(this, EventArgs.Empty);
                var log = $"🧭 移动（内存模拟）{moved:N0} 项 → {target}";
                foreach (var n in movedNames.Take(8)) log += "\r\n   · " + n;
                if (movedNames.Count > 8) log += $"\r\n   · 等 {movedNames.Count} 项";
                if (MessageLogged != null) MessageLogged(this, log);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ExecuteMove 内存模拟移动失败");
        }
    }
    #endregion  // === P0.3: 拖拽初始化 ===

    #region === IThemeable ===
    private ThemeColors _currentColors = ThemeService.GetColors(ThemeMode.Light);

    public void ApplyTheme(ThemeColors colors)
    {
        if (colors == null) return;
        _currentColors = colors;
        try
        {
            BackColor = colors.Surface;
            ForeColor = colors.TextPrimary;

            if (treeFolders != null)
            {
                treeFolders.BackColor = colors.Background;
                treeFolders.ForeColor = colors.TextPrimary;
            }
            if (lstFiles != null)
            {
                lstFiles.BackColor = colors.Background;
                lstFiles.ForeColor = colors.TextPrimary;
            }
            if (cboPath != null)
            {
                cboPath.BackColor = colors.Surface;
                cboPath.ForeColor = colors.TextPrimary;
            }
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "PanNavigatorPanel.ApplyTheme 失败，忽略"); }
    }
    #endregion
}
