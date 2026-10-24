using System.Drawing.Drawing2D;
using LearningAssistant.Common.UI;
using LearningAssistant.Managers;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;

namespace LearningAssistant.Forms.Pdf;

public partial class PdfAudioPanel : UserControl, ISidebarThemeAware
{
    private readonly PdfAudioTrackManager _manager;
    private readonly IPdfReaderFormAccess _form;
    private readonly PdfAudioAiMatcher? _aiMatcher;
    private readonly ToggleSwitch _chkFollow = new();
    private readonly ContextMenuStrip _menuSource = new();
    private readonly ContextMenuStrip _menuMatch = new();
    private readonly ContextMenuStrip _menuBook = new();
    private readonly ToolStripMenuItem _menuAddDir = new("添加音频目录…");
    private readonly ToolStripMenuItem _menuRescan = new("重新扫描");
    private readonly ToolStripMenuItem _menuSubDir = new("包含子目录");
    private readonly ToolStripMenuItem _menuAi = new("AI 自动匹配…");
    private readonly ToolStripMenuItem _menuRules = new("匹配规则…");
    private readonly ToolStripMenuItem _menuRulePatch = new("本书规则补丁…");
    private readonly ToolStripMenuItem _menuSwitchBook = new("在文件页切换书籍…");
    private readonly ToolStripMenuItem _menuExportDirectory = new("导出目录档案…");
    private readonly ToolStripMenuItem _menuExportBook = new("导出当前书…");
    private readonly ToolStripMenuItem _menuImport = new("导入匹配档案…");
    private readonly ToolStripMenuItem _menuReattach = new("重挂目录档案…");
    private readonly Panel _emptyPanel = new();
    private readonly Label _labelEmptyTitle = new();
    private readonly Label _labelEmptySteps = new();
    private readonly Button _btnEmptyPrimary = new();
    private readonly Button _btnEmptySecond = new();
    private Action? _emptyPrimaryAction;
    private Action? _emptySecondAction;

    private bool _suppressAutoPlay;
    private bool _suppressMode;
    private bool _suppressFollow;
    private float _statusHeight;
    private Font? _listFont;

    /// <summary>供 Visual Studio 设计器实例化；运行时请使用带依赖的构造函数。</summary>
    public PdfAudioPanel()
    {
        InitializeComponent();
        // 卡片内容区由面板自行挂载，避免 VS 设计器回写时丢失
        listCard.Body.Controls.Add(_listTracks);
        BuildLayoutExtras();
        _manager = null!;
        _form = null!;
        _aiMatcher = null;
        // 设计期也按当前字体回填一次，保证设计视图与运行预览一致
        ApplyRuntimeMetrics();
        statusPanel.Visible = false;
        root.RowStyles[0].Height = 0F;
    }

    public PdfAudioPanel(PdfAudioTrackManager manager, IPdfReaderFormAccess form,
        PdfAudioAiMatcher? aiMatcher = null) : this()
    {
        _manager = manager;
        _form = form;
        _aiMatcher = aiMatcher;
        WireEvents();
        RefreshData();
    }

    /// <summary>挂载设计器无法直接承载的控件：分段切换、开关与弹出菜单。</summary>
    private void BuildLayoutExtras()
    {
        _segments.SetItems("按页听", "整篇听");
        _chkFollow.Dock = DockStyle.Fill;
        _chkFollow.Margin = new Padding(0, 3, 0, 3);
        followRow.Controls.Add(_chkFollow, 1, 0);

        _menuSubDir.CheckOnClick = true;
        _menuSource.Items.AddRange(new ToolStripItem[]
        {
            _menuAddDir, _menuRescan, new ToolStripSeparator(), _menuSubDir
        });
        _menuSource.Opening += (_, _) =>
        {
            if (_manager == null) return;
            _menuSubDir.Checked = _manager.Profile?.IncludeSubDirectories == true;
        };

        _menuBook.Items.AddRange(new ToolStripItem[]
        {
            _menuSwitchBook, new ToolStripSeparator(),
            _menuExportDirectory, _menuExportBook, new ToolStripSeparator(), _menuImport, _menuReattach
        });
        _menuBook.Opening += (_, _) =>
        {
            bool hasBook = _manager != null && !string.IsNullOrEmpty(_form.CurrentPdfPath);
            _menuExportDirectory.Enabled = hasBook;
            _menuExportBook.Enabled = hasBook;
            _menuImport.Enabled = hasBook;
            _menuReattach.Enabled = hasBook;
            _menuSwitchBook.Enabled = hasBook;
        };

        _menuMatch.Items.AddRange(new ToolStripItem[] { _menuAi, _menuRulePatch, _menuRules });
        _menuMatch.Opening += (_, _) =>
        {
            if (_manager == null) return;
            bool single = _manager.Profile?.MatchMode == PdfAudioMatchMode.SingleAudio;
            _menuAi.Enabled = !single;
            _menuRulePatch.Enabled = !single;
            // 有书级补丁时 Rules 是合并结果，再用整表编辑器会覆盖目录规则
            _menuRules.Enabled = !single && _manager.Profile?.RulePatch == null;
            _menuRules.ToolTipText = _manager.Profile?.RulePatch == null
                ? "编辑本目录共享的匹配规则"
                : "本书已使用规则补丁，请用「本书规则补丁…」调整";
        };

        pageCard.Resize += (_, _) => pageCard.Invalidate();
        advSource.Paint += (_, e) => DrawAdvancedSeparator(e.Graphics, advSource);
        advMatch.Paint += (_, e) => DrawAdvancedSeparator(e.Graphics, advMatch);

        _tooltip.SetToolTip(_btnBind, "为这一页选择音频；整篇模式更换整篇音频");
        _tooltip.SetToolTip(_btnClearPage, "清除手动绑定，恢复规则匹配");
        _tooltip.SetToolTip(_btnPreview, "试听当前绑定，不影响绑定关系");
        _tooltip.SetToolTip(_labelAdvSource, "添加外部目录、重新扫描或切换递归扫描");
        _tooltip.SetToolTip(_labelAdvMatch, "AI 自动匹配与匹配规则设置");

        BuildEmptyState();
    }

    /// <summary>列表卡片内的空态引导：没有音频或没有匹配时给出三步操作。</summary>
    private void BuildEmptyState()
    {
        _emptyPanel.Dock = DockStyle.Fill;
        _emptyPanel.Visible = false;
        _emptyPanel.Padding = new Padding(2);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10, 12, 8, 8)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _labelEmptyTitle.AutoSize = true;
        _labelEmptyTitle.Font = new Font(Font.FontFamily, Font.Size + 0.5F, FontStyle.Bold);
        _labelEmptyTitle.Margin = new Padding(0, 0, 0, 6);

        _labelEmptySteps.AutoSize = false;
        _labelEmptySteps.Dock = DockStyle.Fill;
        _labelEmptySteps.TextAlign = ContentAlignment.TopLeft;
        _labelEmptySteps.Margin = new Padding(0, 0, 0, 6);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0)
        };

        foreach (var button in new[] { _btnEmptyPrimary, _btnEmptySecond })
        {
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.MinimumSize = new Size(96, 30);
            button.Padding = new Padding(10, 2, 10, 2);
            button.Margin = new Padding(0, 4, 8, 0);
            actions.Controls.Add(button);
        }

        _btnEmptyPrimary.Click += (_, _) => _emptyPrimaryAction?.Invoke();
        _btnEmptySecond.Click += (_, _) => _emptySecondAction?.Invoke();

        layout.Controls.Add(_labelEmptyTitle, 0, 0);
        layout.Controls.Add(_labelEmptySteps, 0, 1);
        layout.Controls.Add(actions, 0, 2);
        _emptyPanel.Controls.Add(layout);

        listCard.Body.Controls.Add(_emptyPanel);
        _emptyPanel.BringToFront();
    }

    /// <summary>按当前字体回填设计期占位尺寸，保持与动态布局一致的视觉效果。</summary>
    private void ApplyRuntimeMetrics()
    {
        _listFont ??= new Font("Microsoft YaHei UI", 9.5F);
        _listTracks.ItemHeight = Math.Max(28, _listFont.Height + 12);

        int fontHeight = _labelPageTrack.Font.Height;
        int buttonHeight = Math.Max(28, fontHeight + 11);
        int bookHeight = Math.Max(34, fontHeight + 17);
        int followHeight = Math.Max(26, fontHeight + 9);
        int advancedHeight = Math.Max(28, fontHeight + 11);

        _statusHeight = _labelInfo.Font.Height + 12;
        root.RowStyles[1].Height = bookHeight;
        root.RowStyles[3].Height = followHeight;
        root.RowStyles[4].Height = 18 + (fontHeight + 7) + 18 + buttonHeight + 20;
        root.RowStyles[6].Height = advancedHeight * 2;

        foreach (var button in new[] { _btnBind, _btnClearPage, _btnPreview })
            button.Height = buttonHeight;
        _btnBookSwitch.Height = bookHeight - 6;
        advancedPanel.RowStyles[0].Height = advancedHeight;
        advancedPanel.RowStyles[1].Height = advancedHeight;
    }

    private void WireEvents()
    {
        _labelBook.Click += (_, _) => ShowBookMenu();
        _btnBookSwitch.Click += (_, _) => ShowBookMenu();
        _tooltip.SetToolTip(_btnBookSwitch, "切换书籍，或导入/导出匹配档案");

        _segments.SelectedIndexChanged += (_, _) => OnModeSegmentChanged();
        _chkFollow.CheckedChanged += (_, _) =>
        {
            if (_suppressFollow) return;
            _manager.SetFollowPage(_chkFollow.Checked);
        };

        _btnBind.Click += (_, _) => OnBind();
        _btnClearPage.Click += (_, _) => OnClearPage();
        _btnPreview.Click += (_, _) => OnPreview();

        advSource.Click += (_, _) => ShowSourceMenu();
        _labelAdvSource.Click += (_, _) => ShowSourceMenu();
        _labelAdvSourceChevron.Click += (_, _) => ShowSourceMenu();
        advMatch.Click += (_, _) => ShowMatchMenu();
        _labelAdvMatch.Click += (_, _) => ShowMatchMenu();
        _labelAdvMatchChevron.Click += (_, _) => ShowMatchMenu();

        _menuAi.Click += async (_, _) => await OnAiMatchAsync();
        _menuRules.Click += (_, _) => OnRules();
        _menuRulePatch.Click += (_, _) => OnRulePatch();
        _menuSwitchBook.Click += (_, _) => ShowFilesTab();
        _menuExportDirectory.Click += (_, _) => OnExport(exportDirectory: true);
        _menuExportBook.Click += (_, _) => OnExport(exportDirectory: false);
        _menuImport.Click += async (_, _) => await OnImportAsync();
        _menuReattach.Click += (_, _) => ShowReattachMenu();
        _menuAddDir.Click += async (_, _) => await OnAddDirectoryAsync();
        _menuRescan.Click += async (_, _) =>
        {
            _menuRescan.Enabled = false;
            try { await _manager.RescanAsync(); }
            catch (Exception ex) { HandleAudioError("重新扫描失败", ex); }
            finally { _menuRescan.Enabled = true; }
        };
        // CheckOnClick 只切换勾选，不改变扫描行为；点击后读取勾选状态再执行
        _menuSubDir.Click += async (_, _) =>
        {
            _menuSubDir.Enabled = false;
            try { await _manager.SetIncludeSubDirectoriesAsync(_menuSubDir.Checked); }
            catch (Exception ex) { HandleAudioError("切换递归扫描失败", ex); }
            finally { _menuSubDir.Enabled = true; }
        };

        _listTracks.SelectedIndexChanged += (_, _) => OnTrackSelected();
        _listTracks.DrawItem += ListTracks_DrawItem;

        _manager.ProfileLoaded += OnProfileLoaded;
        _manager.TrackChanged += OnTrackChanged;
        _manager.PageHintChanged += OnPageHintChanged;
        _manager.SettingsChanged += OnSettingsChanged;
    }

    private void ShowFilesTab()
    {
        var tabs = _form.TabControlLeft;
        if (tabs == null || tabs.TabPages.Count == 0) return;
        tabs.SelectedIndex = 0;
    }

    private void ShowBookMenu()
    {
        if (_manager?.Profile == null) return;
        _menuBook.Show(_btnBookSwitch, new Point(0, _btnBookSwitch.Height));
    }

    private void ShowSourceMenu()
    {
        if (_manager.Profile == null) return;
        _menuSource.Show(advSource, new Point(0, advSource.Height));
    }

    private void ShowMatchMenu()
    {
        if (_manager.Profile == null) return;
        _menuMatch.Show(advMatch, new Point(0, advMatch.Height));
    }

    private void ListTracks_DrawItem(object? sender, DrawItemEventArgs e)
    {
        var palette = PdfSidebarTheme.PaletteFor(this);
        bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

        using (var background = new SolidBrush(selected ? palette.BrandSoft : palette.Surface))
            e.Graphics.FillRectangle(background, e.Bounds);

        if (e.Index < 0 || e.Index >= _listTracks.Items.Count) return;

        using (var pen = new Pen(palette.Border))
            e.Graphics.DrawLine(pen, e.Bounds.Left + 4, e.Bounds.Bottom - 1, e.Bounds.Right - 4, e.Bounds.Bottom - 1);

        // 正在播放的曲目用左侧色条标识，避免列表滚动后看不出当前曲目
        int playingIndex = _manager?.CurrentPlaylistIndex ?? -1;
        if (e.Index == playingIndex)
        {
            using var bar = new SolidBrush(PdfSidebarTheme.Brand);
            e.Graphics.FillRectangle(bar, e.Bounds.Left, e.Bounds.Top + 6, 3, e.Bounds.Height - 12);
        }

        var textRect = new Rectangle(e.Bounds.Left + 10, e.Bounds.Top,
            Math.Max(10, e.Bounds.Width - 16), e.Bounds.Height);
        Color foreColor = selected
            ? (palette.IsDark ? Color.White : PdfSidebarTheme.Brand)
            : palette.Text;
        _listFont ??= new Font("Microsoft YaHei UI", 9.5F);
        TextRenderer.DrawText(e.Graphics, _listTracks.Items[e.Index]?.ToString() ?? string.Empty,
            _listFont, textRect, foreColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    public void ApplyTheme(bool isDark)
    {
        var palette = new PdfSidebarPalette(isDark);
        BackColor = palette.Background;
        root.BackColor = palette.Background;

        statusPanel.BackColor = palette.Surface;
        _labelInfo.ForeColor = palette.Text;

        bookBar.BackColor = palette.Background;
        _labelBook.ForeColor = palette.Text;
        _btnBookSwitch.BackColor = palette.Surface;
        _btnBookSwitch.ForeColor = palette.Muted;
        _btnBookSwitch.FlatAppearance.BorderColor = palette.Border;

        _segments.ApplyTheme(isDark);

        followRow.BackColor = palette.Background;
        followLabel.ForeColor = palette.Muted;
        _chkFollow.Invalidate();

        pageCard.ApplyTheme(isDark);
        _labelPageTitle.ForeColor = palette.Muted;
        _labelPageSource.ForeColor = PdfSidebarTheme.Brand;
        _labelPageTrack.ForeColor = palette.Text;
        _labelPageMeta.ForeColor = palette.Muted;
        StylePageButton(_btnBind, palette, primary: true);
        StylePageButton(_btnClearPage, palette, primary: false);
        StylePageButton(_btnPreview, palette, primary: false);

        listCard.ApplyTheme(isDark);
        _listTracks.BackColor = palette.Surface;
        _listTracks.ForeColor = palette.Text;
        _listTracks.Invalidate();

        advancedPanel.BackColor = palette.Background;
        advSource.BackColor = palette.Background;
        advMatch.BackColor = palette.Background;
        _labelAdvSource.ForeColor = palette.Muted;
        _labelAdvMatch.ForeColor = palette.Muted;
        _labelAdvSourceChevron.ForeColor = palette.Muted;
        _labelAdvMatchChevron.ForeColor = palette.Muted;
        advSource.Invalidate();
        advMatch.Invalidate();

        _menuSource.BackColor = palette.Surface;
        _menuSource.ForeColor = palette.Text;
        _menuMatch.BackColor = palette.Surface;
        _menuMatch.ForeColor = palette.Text;

        _emptyPanel.BackColor = palette.Surface;
        _labelEmptyTitle.ForeColor = palette.Text;
        _labelEmptySteps.ForeColor = palette.Muted;
        StylePageButton(_btnEmptyPrimary, palette, primary: true);
        StylePageButton(_btnEmptySecond, palette, primary: false);
    }

    private static void StylePageButton(Button button, PdfSidebarPalette palette, bool primary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = palette.Border;
        button.BackColor = primary ? PdfSidebarTheme.Brand : palette.Surface;
        button.ForeColor = primary ? Color.White : palette.Text;
        button.FlatAppearance.MouseOverBackColor = primary
            ? Color.FromArgb(92, 78, 200)
            : palette.Hover;
    }

    private void DrawAdvancedSeparator(Graphics graphics, Control row)
    {
        var palette = PdfSidebarTheme.PaletteFor(this);
        using var pen = new Pen(palette.Border) { DashStyle = DashStyle.Dash };
        graphics.DrawLine(pen, 2, 0, Math.Max(2, row.Width - 4), 0);
    }

    /// <summary>切换播放范围；整篇听需要先选定一个整篇音频文件。</summary>
    private void OnModeSegmentChanged()
    {
        if (_suppressMode) return;
        if (_manager.Profile == null)
        {
            SyncModeControls();
            return;
        }

        if (_segments.SelectedIndex == 1)
        {
            if (_manager.Profile.PlaybackScope != PlaybackScope.WholeBook)
            {
                var path = PickAudioPath("选择整篇音频");
                if (path == null)
                {
                    SyncModeControls();
                    return;
                }
                _manager.SetPlaybackScope(PlaybackScope.WholeBook, path);
            }
        }
        else if (_manager.Profile.PlaybackScope == PlaybackScope.WholeBook)
        {
            if (!_form.ShowConfirm("切回按页听后，整篇音频选择会被移除；按页绑定与匹配规则保留。", "切换播放范围"))
            {
                SyncModeControls();
                return;
            }
            _manager.SetPlaybackScope(PlaybackScope.FollowPage);
        }

        SyncModeControls();
        RefreshData();
    }

    private void SyncModeControls()
    {
        if (_manager == null) return;
        bool whole = _manager.Profile?.PlaybackScope == PlaybackScope.WholeBook;

        _suppressMode = true;
        try { _segments.SelectedIndex = whole ? 1 : 0; }
        finally { _suppressMode = false; }

        _suppressFollow = true;
        try
        {
            _chkFollow.Checked = _manager.FollowPage;
            _chkFollow.Enabled = !whole;
        }
        finally { _suppressFollow = false; }

        followLabel.Text = whole ? "整篇音频不随翻页切换" : "翻页自动跟随";
        _tooltip.SetToolTip(followLabel, whole
            ? "整篇听模式下播放整本音频，翻页不会切换曲目"
            : "翻页时自动切换到该页绑定的音频");
    }

    public void RefreshData()
    {
        if (_manager == null) return;
        UpdateBookLabel();
        SyncModeControls();

        var result = _manager.Result;
        bool singleMode = _manager.Profile?.MatchMode == PdfAudioMatchMode.SingleAudio;

        // 先按曲目反查页码，避免对每个曲目重复扫描整个 PageIndex（O(T×P)）
        var pagesByTrack = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        if (result != null)
        {
            foreach (var kv in result.PageIndex)
            {
                if (!pagesByTrack.TryGetValue(kv.Value.FilePath, out var pages))
                    pagesByTrack[kv.Value.FilePath] = pages = new List<int>();
                pages.Add(kv.Key + 1);
            }
        }

        _listTracks.BeginUpdate();
        _listTracks.Items.Clear();
        if (result != null)
        {
            foreach (var t in result.Tracks)
            {
                if (singleMode)
                {
                    _listTracks.Items.Add($"全篇  {t.FileName}{(t.IsSingleAudio ? "  [整篇音频]" : "")}");
                    continue;
                }
                var pages = pagesByTrack.TryGetValue(t.FilePath, out var list) ? list.OrderBy(p => p).ToList() : new List<int>();
                var pageText = pages.Count > 0 ? string.Join(",", pages.Take(4)) + (pages.Count > 4 ? "…" : "") : "未匹配";
                // 外部目录曲目用目录名辅助区分同名文件
                var folderHint = Path.IsPathRooted(t.RelativePath)
                    ? $"  ({Path.GetFileName(Path.GetDirectoryName(t.FilePath))})"
                    : "";
                _listTracks.Items.Add($"第{pageText}页  {t.FileName}{folderHint}{(t.IsManual ? "  [手动]" : "")}");
            }
            if (!singleMode && result.UnmatchedPages.Count > 0)
                _listTracks.Items.Add($"未匹配页：{string.Join(",", result.UnmatchedPages.Select(p => p + 1).Take(20))}");
        }
        _listTracks.EndUpdate();

        int count = result?.Tracks.Count ?? 0;
        int matched = result?.PageIndex.Count ?? 0;
        listCard.Title = count == 0
            ? "曲目 · 未扫描到音频"
            : matched == 0 && !singleMode
                ? $"曲目 {count} · 还未匹配页码"
                : $"曲目 {count} · 已匹配 {matched}/{_manager.PageCount}";

        UpdateEmptyState(singleMode, count, matched);
        UpdatePageCard();
        SelectCurrentTrackInList();
    }

    /// <summary>无音频/无匹配时用引导替代空列表；有内容时恢复列表。</summary>
    private void UpdateEmptyState(bool singleMode, int trackCount, int matched)
    {
        if (singleMode && trackCount == 0)
        {
            ShowEmptyState("还没有选择整篇音频",
                "① 点上方「整篇听」\r\n② 选择整本书共用的音频文件\r\n③ 之后翻页不会切换曲目",
                "选择整篇音频…", OnBind);
            return;
        }

        if (trackCount == 0)
        {
            ShowEmptyState("还没有可播放的音频",
                "① 把音频放到 PDF 同目录，或添加音频目录\r\n② 文件名不规律时，用匹配规则或 AI 自动匹配\r\n③ 仍有遗漏时，在当前页卡片手动绑定",
                "添加音频目录…", () => _ = OnAddDirectoryAsync(),
                "匹配规则…", OpenRulesEntry);
            return;
        }

        if (!singleMode && matched == 0)
        {
            ShowEmptyState($"已扫描到 {trackCount} 个音频，但还没有匹配页码",
                "① 用「匹配规则」按文件名提取页码\r\n② 或让 AI 自动匹配生成建议\r\n③ 也可以在当前页卡片手动绑定",
                "匹配规则…", OpenRulesEntry,
                _aiMatcher == null ? null : "AI 自动匹配…",
                _aiMatcher == null ? null : () => _ = OnAiMatchAsync());
            return;
        }

        HideEmptyState();
    }

    private void ShowEmptyState(string title, string steps, string primaryText, Action primary,
        string? secondText = null, Action? second = null)
    {
        _labelEmptyTitle.Text = title;
        _labelEmptySteps.Text = steps;

        _emptyPrimaryAction = primary;
        _btnEmptyPrimary.Text = primaryText;
        _btnEmptyPrimary.Visible = true;

        _emptySecondAction = second;
        _btnEmptySecond.Text = secondText ?? "";
        _btnEmptySecond.Visible = secondText != null && second != null;

        _emptyPanel.Visible = true;
        _listTracks.Visible = false;
        _emptyPanel.BringToFront();
    }

    private void HideEmptyState()
    {
        _emptyPanel.Visible = false;
        _listTracks.Visible = true;
    }

    /// <summary>有书级补丁时规则入口应打开补丁编辑器，避免覆盖目录规则。</summary>
    private void OpenRulesEntry()
    {
        if (_manager.Profile?.RulePatch != null) OnRulePatch();
        else OnRules();
    }

    private void UpdateBookLabel()
    {
        var path = _form?.CurrentPdfPath;
        bool hasBook = !string.IsNullOrWhiteSpace(path);
        _labelBook.Text = hasBook ? Path.GetFileNameWithoutExtension(path) : "未打开文档";
        _btnBookSwitch.Enabled = hasBook;
        _tooltip.SetToolTip(_labelBook, hasBook ? path : "打开 PDF 后可配置配套音频");
    }

    /// <summary>翻页时只更新与当前页相关的信息，不重建曲目列表</summary>
    public void OnCurrentPageChanged()
    {
        UpdatePageCard();
    }

    private void UpdatePageCard()
    {
        if (_manager == null) return;
        bool singleMode = _manager.Profile?.MatchMode == PdfAudioMatchMode.SingleAudio;
        var result = _manager.Result;

        AudioTrack? track;
        string title;
        if (singleMode)
        {
            track = result?.Tracks.FirstOrDefault();
            title = "整篇听 · 全篇";
        }
        else
        {
            track = result != null && result.PageIndex.TryGetValue(_form.CurrentPageIndex, out var t) ? t : null;
            title = $"当前页 · P{_form.CurrentPageIndex + 1}";
        }

        _labelPageTitle.Text = title;
        _labelPageSource.Tag = track == null ? "muted" : "accent";
        _labelPageSource.Text = singleMode ? "全篇" : track == null ? "未绑定" : track.IsManual ? "手动" : "自动";
        _labelPageTrack.Text = track?.FileName ?? (singleMode ? "未选择整篇音频" : "本页未绑定音频");
        _labelPageSource.ForeColor = track == null
            ? PdfSidebarTheme.PaletteFor(this).Muted
            : PdfSidebarTheme.Brand;

        if (track == null)
        {
            _labelPageMeta.Text = _manager.PageHint
                ?? (singleMode ? "点「更换绑定」选择整篇音频" : "点「更换绑定」选择这一页的音频");
        }
        else
        {
            var meta = new List<string>();
            var duration = DescribeDuration(track);
            if (!string.IsNullOrEmpty(duration)) meta.Add(duration);
            if (!singleMode)
            {
                var coverage = DescribeCoverage(track);
                if (!string.IsNullOrEmpty(coverage)) meta.Add(coverage);
            }
            _labelPageMeta.Text = meta.Count > 0 ? string.Join(" · ", meta) : "已绑定音频";
        }

        _btnBind.Text = singleMode ? "更换音频" : "更换绑定";
        // 只有手动/AI 绑定才存在可清除的覆盖项，自动匹配结果清除后仍会重新命中
        _btnClearPage.Enabled = !singleMode && track?.IsManual == true;
        _btnPreview.Enabled = track != null;
    }

    private string DescribeDuration(AudioTrack track)
    {
        if (track.DurationMs > 0) return FormatTime(TimeSpan.FromMilliseconds(track.DurationMs));
        // 正在加载的曲目可直接读播放器时长，避免等待元数据重新解析
        if (_manager.CurrentTrack != null
            && string.Equals(_manager.CurrentTrack.FilePath, track.FilePath, StringComparison.OrdinalIgnoreCase)
            && _manager.Playback.Player.Duration > TimeSpan.Zero)
            return FormatTime(_manager.Playback.Player.Duration);
        return "";
    }

    private string DescribeCoverage(AudioTrack track)
    {
        var result = _manager.Result;
        if (result == null) return "";
        var pages = result.PageIndex
            .Where(kv => string.Equals(kv.Value.FilePath, track.FilePath, StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Key)
            .OrderBy(p => p)
            .ToList();
        if (pages.Count <= 1) return "";
        if (pages.Count >= _manager.PageCount) return "覆盖全篇";
        return $"覆盖 P{pages[0] + 1}–P{pages[^1] + 1}";
    }

    private static string FormatTime(TimeSpan time)
        => time.TotalHours >= 1
            ? $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}"
            : $"{time.Minutes:00}:{time.Seconds:00}";

    private void OnTrackSelected()
    {
        var result = _manager.Result;
        if (result == null || _listTracks.SelectedIndex < 0 || _listTracks.SelectedIndex >= result.Tracks.Count) return;
        if (_suppressAutoPlay) return;
        var track = result.Tracks[_listTracks.SelectedIndex];
        _manager.Load(track);
        FireAndForget(_manager.Playback.PlayAsync());
    }

    /// <summary>切曲后同步当前页卡片与列表高亮。</summary>
    private void OnTrackChanged(object? sender, AudioTrack track)
        => InvokeIfReady(() =>
        {
            UpdatePageCard();
            SelectTrackInList(track);
            _listTracks.Invalidate();
        });

    private void OnPageHintChanged(object? sender, string? hint)
        => InvokeIfReady(UpdatePageCard);

    private void OnSettingsChanged(object? sender, EventArgs e)
        => InvokeIfReady(() =>
        {
            SyncModeControls();
            UpdatePageCard();
        });

    private void OnProfileLoaded(object? sender, EventArgs e) => InvokeIfReady(() =>
    {
        RefreshData();
        // 档案损坏优先提示，且管理器已禁止回写，避免覆盖原文件
        var error = _manager.LoadErrorMessage;
        if (!string.IsNullOrEmpty(error))
        {
            ShowStatus(error, isError: true);
            return;
        }
        // 打开档案时若发生播放范围自动纠正，在这里提示一次，不弹模态框
        ShowStatus(_manager.ScopeCorrectionMessage, isError: false);
    });

    private void ShowStatus(string? message, bool isError)
    {
        bool show = !string.IsNullOrWhiteSpace(message);
        statusPanel.Visible = show;
        root.RowStyles[0].Height = show ? _statusHeight : 0F;
        if (!show) return;
        _labelInfo.Text = message;
        _labelInfo.ForeColor = isError
            ? PdfSidebarTheme.Danger
            : PdfSidebarTheme.PaletteFor(this).Muted;
    }

    /// <summary>更换当前页绑定（整篇模式下更换整篇音频）。</summary>
    private void OnBind()
    {
        if (_manager.Profile == null) return;
        bool singleMode = _manager.Profile.MatchMode == PdfAudioMatchMode.SingleAudio;

        if (singleMode)
        {
            var single = PickAudioPath("更换整篇音频");
            if (single == null) return;
            _manager.SetPlaybackScope(PlaybackScope.WholeBook, single);
            RefreshData();
            return;
        }

        var path = PickAudioPath($"第 {_form.CurrentPageIndex + 1} 页 · 选择音频");
        if (path == null) return;
        _manager.SetManualOverride(_form.CurrentPageIndex, path);
        RefreshData();
    }

    private void OnClearPage()
    {
        bool singleMode = _manager.Profile?.MatchMode == PdfAudioMatchMode.SingleAudio;
        if (singleMode || _manager.Profile == null) return;
        _manager.ClearPageOverride(_form.CurrentPageIndex);
        RefreshData();
    }

    private void OnPreview()
    {
        bool singleMode = _manager.Profile?.MatchMode == PdfAudioMatchMode.SingleAudio;
        AudioTrack? track;
        if (singleMode)
        {
            track = _manager.Result?.Tracks.FirstOrDefault();
        }
        else
        {
            track = _manager.Result?.PageIndex.GetValueOrDefault(_form.CurrentPageIndex);
        }
        if (track == null) return;
        if (!_manager.Preview(track)) _form.ShowWarning("当前音频无法试听");
    }

    /// <summary>搜索式音频选择器：可搜索已扫描曲目，也可选取目录外文件。</summary>
    private string? PickAudioPath(string title)
    {
        if (string.IsNullOrEmpty(_form.CurrentPdfPath))
        {
            _form.ShowWarning("请先打开 PDF");
            return null;
        }

        var tracks = _manager.Result?.Tracks ?? new List<AudioTrack>();
        var visible = new List<AudioTrack>();
        using var dialog = new Form
        {
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(460, 420),
            Font = Font,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            FormBorderStyle = FormBorderStyle.FixedDialog
        };

        var filterBox = new TextBox { Dock = DockStyle.Top, PlaceholderText = "搜索文件名或页码（如 P12）" };
        var list = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 44,
            Padding = new Padding(0, 6, 0, 0)
        };
        var btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Size = new Size(72, 30), Margin = new Padding(6, 0, 0, 0) };
        var btnOk = new Button { Text = "选择", Size = new Size(72, 30), Margin = new Padding(6, 0, 4, 0) };
        var btnLocal = new Button { Text = "选择本地文件…", Size = new Size(112, 30), Margin = new Padding(0) };
        bottom.Controls.Add(btnCancel);
        bottom.Controls.Add(btnOk);
        bottom.Controls.Add(btnLocal);

        void Fill()
        {
            var key = filterBox.Text.Trim();
            list.BeginUpdate();
            list.Items.Clear();
            visible.Clear();
            foreach (var track in tracks)
            {
                if (!MatchesFilter(track, key)) continue;
                visible.Add(track);
                var pageText = DescribeTrackPages(track);
                list.Items.Add($"{pageText}  {track.FileName}{(track.IsManual ? "  [手动]" : "")}");
            }
            list.EndUpdate();
            if (list.Items.Count > 0) list.SelectedIndex = 0;
        }

        filterBox.TextChanged += (_, _) => Fill();
        list.DoubleClick += (_, _) => { dialog.DialogResult = DialogResult.OK; dialog.Close(); };
        btnOk.Click += (_, _) => dialog.DialogResult = DialogResult.OK;
        bool localRequested = false;
        btnLocal.Click += (_, _) => { localRequested = true; dialog.DialogResult = DialogResult.OK; };

        dialog.Controls.Add(list);
        dialog.Controls.Add(filterBox);
        dialog.Controls.Add(bottom);
        dialog.AcceptButton = btnOk;
        dialog.CancelButton = btnCancel;
        Fill();

        if (dialog.ShowDialog(_form.Form) != DialogResult.OK) return null;

        if (localRequested)
        {
            using var ofd = new OpenFileDialog
            {
                Title = title,
                Filter = "音频文件|*.mp3;*.wav;*.m4a;*.aac;*.flac;*.ogg;*.wma",
                InitialDirectory = Path.GetDirectoryName(_form.CurrentPdfPath)
            };
            return ofd.ShowDialog(_form.Form) == DialogResult.OK ? ToProfilePath(ofd.FileName) : null;
        }

        if (list.SelectedIndex < 0 || list.SelectedIndex >= visible.Count) return null;
        return visible[list.SelectedIndex].RelativePath;
    }

    private bool MatchesFilter(AudioTrack track, string key)
    {
        if (string.IsNullOrEmpty(key)) return true;
        if (track.FileName.Contains(key, StringComparison.OrdinalIgnoreCase)) return true;
        if (track.RelativePath.Contains(key, StringComparison.OrdinalIgnoreCase)) return true;
        // 支持输入 P12 直接定位这一页绑定的曲目
        if (key.Length > 1 && char.ToUpperInvariant(key[0]) == 'P'
            && int.TryParse(key[1..], out var page)
            && _manager.Result != null
            && _manager.Result.PageIndex.TryGetValue(page - 1, out var resolved))
            return string.Equals(resolved.FilePath, track.FilePath, StringComparison.OrdinalIgnoreCase);
        return false;
    }

    private string DescribeTrackPages(AudioTrack track)
    {
        var pages = _manager.Result?.PageIndex
            .Where(kv => string.Equals(kv.Value.FilePath, track.FilePath, StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Key + 1)
            .OrderBy(p => p)
            .ToList() ?? new List<int>();
        if (pages.Count == 0) return "未匹配";
        if (pages.Count >= _manager.PageCount && _manager.PageCount > 0) return "全篇";
        return pages.Count == 1 ? $"P{pages[0]}" : $"P{pages[0]}–P{pages[^1]}";
    }

    private void OnRules()
    {
        if (_manager.Profile == null) { _form.ShowWarning("尚未加载 profile"); return; }
        using var dlg = new PdfAudioRuleDialog(_manager.Profile, _form, _manager);
        if (dlg.ShowDialog(_form.Form) == DialogResult.OK)
            _manager.SaveRulesAndRematch();
    }

    /// <summary>编辑书级规则补丁：禁用目录规则、追加规则或完全独立。</summary>
    private void OnRulePatch()
    {
        if (_manager.Profile == null) { _form.ShowWarning("尚未加载 profile"); return; }
        using var dlg = new PdfAudioRulePatchDialog(_manager.Profile);
        if (dlg.ShowDialog(_form.Form) != DialogResult.OK) return;

        _manager.ApplyRulePatch(dlg.Patch);
        RefreshData();
        ShowStatus(dlg.Patch == null ? "已恢复继承目录规则" : "已应用本书规则补丁", isError: false);
    }

    /// <summary>导出目录档案或当前书档案，由用户选择保存位置。</summary>
    private void OnExport(bool exportDirectory)
    {
        var result = exportDirectory ? _manager.ExportDirectory() : _manager.ExportBook();
        if (!result.Success || string.IsNullOrEmpty(result.Json))
        {
            _form.ShowWarning(result.Error ?? "当前没有可导出的匹配档案");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = exportDirectory ? "导出目录档案" : "导出当前书",
            Filter = "音频匹配档案|*.json",
            FileName = result.SuggestedFileName ?? "audio.json",
            InitialDirectory = Path.GetDirectoryName(_form.CurrentPdfPath) ?? ""
        };
        if (dialog.ShowDialog(_form.Form) != DialogResult.OK) return;

        try
        {
            File.WriteAllText(dialog.FileName, result.Json);
            _form.ShowMessage($"已导出匹配档案：{dialog.FileName}", "导出匹配档案");
        }
        catch (Exception ex)
        {
            HandleAudioError("导出匹配档案失败", ex);
        }
    }

    /// <summary>选择档案 → 校验 → 差异预览 → 确认后按书合并。</summary>
    private async Task OnImportAsync()
    {
        if (string.IsNullOrEmpty(_form.CurrentPdfPath)) { _form.ShowWarning("请先打开 PDF"); return; }

        using var dialog = new OpenFileDialog
        {
            Title = "导入匹配档案",
            Filter = "音频匹配档案|*.json|所有文件|*.*",
            InitialDirectory = Path.GetDirectoryName(_form.CurrentPdfPath) ?? ""
        };
        if (dialog.ShowDialog(_form.Form) != DialogResult.OK) return;

        string json;
        try
        {
            json = File.ReadAllText(dialog.FileName);
        }
        catch (Exception ex)
        {
            HandleAudioError("读取匹配档案失败", ex);
            return;
        }

        var preview = _manager.PreviewImport(json);
        if (!preview.Success)
        {
            _form.ShowWarning($"导入失败：{preview.Error}");
            return;
        }
        if (!ShowImportPreview(preview)) return;

        _menuImport.Enabled = false;
        UseWaitCursor = true;
        try
        {
            await _manager.ApplyImportAsync(preview);
            var error = _manager.LoadErrorMessage;
            if (!string.IsNullOrEmpty(error))
            {
                ShowStatus(error, isError: true);
                _form.ShowWarning(error);
                return;
            }
            RefreshData();
            OnCurrentPageChanged();
            ShowStatus($"已导入{preview.Title}：{preview.Summary}", isError: false);
        }
        catch (Exception ex)
        {
            HandleAudioError("导入匹配档案失败", ex);
        }
        finally
        {
            _menuImport.Enabled = true;
            UseWaitCursor = false;
        }
    }

    /// <summary>列出疑似同一目录的旧档案，选中后确认重挂。</summary>
    private void ShowReattachMenu()
    {
        var candidates = _manager.FindReattachCandidates();
        if (candidates.Count == 0)
        {
            ShowStatus("没有找到重叠率超过 80% 的旧目录档案", isError: false);
            return;
        }

        var menu = new ContextMenuStrip { ShowImageMargin = false };
        foreach (var candidate in candidates)
        {
            var item = new ToolStripMenuItem(
                $"{candidate.DirectoryPath} · 重合 {candidate.OverlapRatio:P0}（{candidate.SharedBooks} 本）");
            item.Click += async (_, _) => await OnReattachAsync(candidate);
            menu.Items.Add(item);
        }
        menu.Closed += (_, _) => menu.Dispose();
        menu.Show(_menuBook, _menuBook.PointToClient(Cursor.Position));
    }

    private async Task OnReattachAsync(PdfAudioDirectoryReattachCandidate candidate)
    {
        var confirm = MessageBox.Show(this,
            $"将把「{candidate.DirectoryPath}」的档案重挂到当前目录，并覆盖当前目录已有档案。\n继续？",
            "重挂目录档案", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        try
        {
            bool ok = await _manager.ReattachDirectoryAsync(candidate.ProfilePath);
            if (!ok)
            {
                ShowStatus("重挂失败：档案已不存在", isError: true);
                return;
            }
            RefreshData();
            OnCurrentPageChanged();
            ShowStatus($"目录档案已重挂：{candidate.DirectoryPath}", isError: false);
        }
        catch (Exception ex)
        {
            HandleAudioError("重挂目录档案失败", ex);
        }
    }

    /// <summary>差异预览确认框：展示类型、摘要、逐书差异与警告。</summary>
    private bool ShowImportPreview(PdfAudioImportPreview preview)
    {
        using var dialog = new Form
        {
            Text = "导入匹配档案",
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(560, 460),
            MinimumSize = new Size(480, 360),
            Font = Font,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(14, 12, 14, 10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        root.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            Text = $"{preview.Title} · {preview.Summary}",
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 0);

        var items = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            SelectionMode = SelectionMode.None,
            Margin = new Padding(0, 0, 0, 6)
        };
        foreach (var item in preview.Items) items.Items.Add(item);
        root.Controls.Add(items, 0, 1);

        var warningText = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Height = 64,
            Text = string.Join(Environment.NewLine, preview.Warnings),
            ForeColor = Color.FromArgb(180, 100, 0),
            BorderStyle = BorderStyle.FixedSingle,
            Visible = preview.Warnings.Count > 0
        };
        root.Controls.Add(warningText, 0, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };
        var btnImport = new Button { Text = "导入", Size = new Size(76, 30), DialogResult = DialogResult.OK, Margin = new Padding(6, 0, 0, 0) };
        var btnCancel = new Button { Text = "取消", Size = new Size(76, 30), DialogResult = DialogResult.Cancel, Margin = new Padding(6, 0, 0, 0) };
        buttons.Controls.Add(btnImport);
        buttons.Controls.Add(btnCancel);
        root.Controls.Add(buttons, 0, 3);

        dialog.Controls.Add(root);
        dialog.AcceptButton = btnImport;
        dialog.CancelButton = btnCancel;
        return dialog.ShowDialog(_form.Form) == DialogResult.OK;
    }

    /// <summary>选择外部音频目录并合并到当前曲目列表</summary>
    private async Task OnAddDirectoryAsync()
    {
        if (string.IsNullOrEmpty(_form.CurrentPdfPath)) { _form.ShowWarning("请先打开 PDF"); return; }

        using var dialog = new FolderBrowserDialog
        {
            Description = "选择包含音频文件的目录（自动包含子目录）",
            ShowNewFolderButton = false
        };
        if (dialog.ShowDialog(_form.Form) != DialogResult.OK) return;

        _menuAddDir.Enabled = false;
        try
        {
            var added = await _manager.AddExternalDirectoryAsync(dialog.SelectedPath);
            RefreshData();
            _form.ShowMessage($"已添加音频目录：{dialog.SelectedPath}\n新增 {added} 个音频文件", "添加音频目录");
        }
        catch (Exception ex)
        {
            HandleAudioError("添加音频目录失败", ex);
        }
        finally
        {
            _menuAddDir.Enabled = true;
        }
    }

    /// <summary>调用 AI 匹配当前 PDF 页与曲目，预览确认后写入手动匹配</summary>
    private async Task OnAiMatchAsync()
    {
        if (string.IsNullOrEmpty(_form.CurrentPdfPath)) { _form.ShowWarning("请先打开 PDF"); return; }
        var result = _manager.Result;
        if (result == null || result.Tracks.Count == 0)
        {
            _form.ShowWarning("当前没有可匹配的音频，请先添加音频目录或重新扫描");
            return;
        }
        if (_aiMatcher == null)
        {
            _form.ShowWarning("AI 服务不可用，请先在设置中配置 AI 服务");
            return;
        }

        _menuAi.Enabled = false;
        _menuAddDir.Enabled = false;
        UseWaitCursor = true;
        try
        {
            var progress = new Progress<string>(message => InvokeIfReady(() => ShowStatus(message, isError: false)));
            var suggestions = await _aiMatcher.SuggestAsync(
                _form.CurrentPdfPath, _manager.PageCount, result.Tracks, progress);
            if (suggestions.Count == 0)
            {
                _form.ShowWarning("AI 没有返回可用的匹配建议，可能是页面无可提取文本或音频文件名缺少线索");
                return;
            }

            using var dialog = new PdfAudioAiMatchDialog(suggestions, _manager.PageCount);
            if (dialog.ShowDialog(_form.Form) != DialogResult.OK) return;

            _manager.ApplyManualMatches(dialog.SelectedMatches);
            _manager.OnPageChanged(_form.CurrentPageIndex);
            RefreshData();
            _form.ShowMessage($"已应用 {dialog.SelectedMatches.Count} 条 AI 匹配结果", "AI 自动匹配");
        }
        catch (Exception ex)
        {
            HandleAudioError("AI 自动匹配失败", ex);
        }
        finally
        {
            _menuAi.Enabled = true;
            _menuAddDir.Enabled = true;
            UseWaitCursor = false;
            var error = _manager.LoadErrorMessage;
            ShowStatus(!string.IsNullOrEmpty(error) ? error : _manager.ScopeCorrectionMessage,
                isError: !string.IsNullOrEmpty(error));
            OnCurrentPageChanged();
        }
    }

    /// <summary>把指定曲目在列表中高亮并滚动到可见位置（不触发自动播放）</summary>
    private void SelectTrackInList(AudioTrack track)
    {
        var result = _manager.Result;
        if (result == null) return;
        var index = result.Tracks.FindIndex(t =>
            string.Equals(t.FilePath, track.FilePath, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return;
        _suppressAutoPlay = true;
        try
        {
            _listTracks.SelectedIndex = index;
            _listTracks.TopIndex = Math.Max(0, index - 2);
        }
        finally
        {
            _suppressAutoPlay = false;
        }
    }

    private void SelectCurrentTrackInList()
    {
        bool singleMode = _manager.Profile?.MatchMode == PdfAudioMatchMode.SingleAudio;
        var track = _manager.CurrentTrack
            ?? (singleMode ? _manager.Result?.Tracks.FirstOrDefault() : null)
            ?? (_manager.Result?.PageIndex.GetValueOrDefault(_form.CurrentPageIndex));
        if (track != null) SelectTrackInList(track);
    }

    private string ToProfilePath(string fullPath)
    {
        var pdfDir = Path.GetDirectoryName(_form.CurrentPdfPath ?? "");
        if (string.IsNullOrEmpty(pdfDir)) return fullPath;
        var baseDir = Path.GetFullPath(pdfDir);
        var target = Path.GetFullPath(fullPath);
        if (target.StartsWith(baseDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return Path.GetRelativePath(baseDir, target);
        return target;
    }

    private void HandleAudioError(string message, Exception ex)
    {
        System.Diagnostics.Trace.TraceError($"{message}：{ex}");
        _form.ShowWarning($"{message}：{ex.Message}");
    }

    private void InvokeIfReady(Action action)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) { try { BeginInvoke(action); } catch { } }
        else action();
    }

    private static void FireAndForget(Task task)
        => _ = task.ContinueWith(t => System.Diagnostics.Trace.TraceError($"音频播放异常：{t.Exception}"), TaskContinuationOptions.OnlyOnFaulted);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_manager != null)
            {
                _manager.ProfileLoaded -= OnProfileLoaded;
                _manager.TrackChanged -= OnTrackChanged;
                _manager.PageHintChanged -= OnPageHintChanged;
                _manager.SettingsChanged -= OnSettingsChanged;
            }
            _menuSource.Dispose();
            _menuMatch.Dispose();
            _menuBook.Dispose();
            _listFont?.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>轻量开关：胶囊轨道 + 圆形滑块，避免侧栏使用系统 CheckBox 的外观。</summary>
    private sealed class ToggleSwitch : CheckBox
    {
        public ToggleSwitch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            AutoSize = false;
            Size = new Size(36, 18);
            Cursor = Cursors.Hand;
            Text = string.Empty;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var palette = PdfSidebarTheme.PaletteFor(this);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var track = new Rectangle(1, 2, Math.Max(10, Width - 2), Math.Max(10, Height - 4));
            using (var path = GdiHelper.CreateRoundedRectPath(track, track.Height / 2))
            using (var background = new SolidBrush(Enabled
                ? (Checked ? PdfSidebarTheme.Brand : palette.SurfaceAlt)
                : palette.Border))
            {
                e.Graphics.FillPath(background, path);
            }

            int knobSize = Math.Max(8, track.Height - 4);
            int knobX = Checked ? track.Right - knobSize - 2 : track.Left + 2;
            using (var knob = new SolidBrush(Checked ? Color.White : palette.Muted))
            {
                e.Graphics.FillEllipse(knob, knobX, track.Top + 2, knobSize, knobSize);
            }
        }
    }

    /// <summary>当前页卡片：品牌浅底 + 圆角描边，与普通白色卡片区分层级。</summary>
    private sealed class PageCardPanel : Panel, ISidebarThemeAware
    {
        public PageCardPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        public void ApplyTheme(bool isDark)
        {
            BackColor = new PdfSidebarPalette(isDark).BrandSoft;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            using var path = GdiHelper.CreateRoundedRectPath(rect, 10);
            using (var background = new SolidBrush(BackColor))
                e.Graphics.FillPath(background, path);
            using (var border = new Pen(Color.FromArgb(95, PdfSidebarTheme.Brand)))
                e.Graphics.DrawPath(border, path);
        }
    }
}
