using System.ComponentModel;
using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

/// <summary>工作台模块。</summary>
public enum PdfWorkbenchModule
{
    Highlight,
    Translate
}

/// <summary>AI 菜单动作。</summary>
public enum PdfWorkbenchAiAction
{
    Summary,
    Ask,
    Outline,
    Notes,
    BatchOcr,
    Flashcards
}

/// <summary>
/// 「学习」页签内的双面板工作台：
/// 摘录与翻译各自是完整面板，通过顶部模块开关决定显示单个还是上下双开；
/// 联动开关只负责建立「摘录 → 翻译」的单向关联，不改变两个模块的独立性。
/// </summary>
public sealed class PdfWorkbenchPanel : UserControl, ISidebarThemeAware
{
    private const int ModuleBarHeight = 44;
    private const int BottomBarHeight = 46;

    private readonly TableLayoutPanel root;
    private readonly Panel moduleBar;
    private readonly Button highlightButton;
    private readonly Button translateButton;
    private readonly Button linkButton;
    private readonly SplitContainer split;
    private readonly Panel highlightHost;
    private readonly Panel translateHost;
    private readonly TableLayoutPanel bottomBar;
    private readonly Button shadowingButton;
    private readonly Button audioButton;
    private readonly Button aiButton;
    private readonly ContextMenuStrip shadowingMenu;
    private readonly ContextMenuStrip aiMenu;

    private bool _highlightVisible = true;
    private bool _translateVisible;
    private bool _linkEnabled;
    private bool _splitterInitialized;
    private bool _addHighlightsEnabled;
    private bool _shadowingEnabled;
    private bool _wholeBookShadowingEnabled;
    private double _splitterRatio = 0.55;

    public PdfWorkbenchPanel()
    {
        root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = new Padding(6, 2, 6, 6),
            Name = "workbenchRoot"
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, ModuleBarHeight));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, BottomBarHeight));

        moduleBar = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Name = "moduleBar"
        };

        highlightButton = CreateModuleButton("✍️ 摘录", "显示或收起摘录面板");
        translateButton = CreateModuleButton("🌐 翻译", "显示或收起翻译面板");
        linkButton = CreateModuleButton("🔗 联动", "开启后选中摘录会自动送入翻译面板");
        linkButton.AccessibleName = "摘录到翻译联动开关";

        highlightButton.Click += (_, _) => ToggleModule(PdfWorkbenchModule.Highlight);
        translateButton.Click += (_, _) => ToggleModule(PdfWorkbenchModule.Translate);
        linkButton.Click += (_, _) => LinkEnabled = !LinkEnabled;

        moduleBar.Controls.Add(highlightButton);
        moduleBar.Controls.Add(translateButton);
        moduleBar.Controls.Add(linkButton);
        moduleBar.Resize += (_, _) => LayoutModuleBar();

        split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 6,
            Panel1MinSize = 96,
            Panel2MinSize = 96,
            Margin = Padding.Empty,
            Name = "moduleSplit"
        };
        highlightHost = CreateHost("highlightHost");
        translateHost = CreateHost("translateHost");
        split.Panel1.Controls.Add(highlightHost);
        split.Panel2.Controls.Add(translateHost);
        split.Resize += (_, _) => EnsureSplitterRatio();
        split.SplitterMoved += (_, _) =>
        {
            _splitterInitialized = true;
            if (split.Height > 0)
                _splitterRatio = Math.Clamp((double)split.SplitterDistance / split.Height, 0.2, 0.8);
            StateChanged?.Invoke(this, EventArgs.Empty);
        };

        bottomBar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = new Padding(0, 5, 0, 0),
            Name = "bottomBar"
        };
        for (int i = 0; i < 3; i++)
            bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
        bottomBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        shadowingButton = CreateBottomButton("✂ 跟读", "本页或整本跟读");
        audioButton = CreateBottomButton("🎧 音频", "打开配套音频");
        aiButton = CreateBottomButton("✨ AI", "AI 工具、闪卡与加入学习");
        shadowingButton.Margin = new Padding(0, 0, 2, 0);
        audioButton.Margin = new Padding(2, 0, 2, 0);
        aiButton.Margin = new Padding(2, 0, 0, 0);

        shadowingMenu = BuildShadowingMenu();
        aiMenu = BuildAiMenu();
        shadowingButton.Click += (_, _) => ShowMenu(shadowingButton, shadowingMenu);
        audioButton.Click += (_, _) => AudioRequested?.Invoke(this, EventArgs.Empty);
        aiButton.Click += (_, _) => ShowMenu(aiButton, aiMenu);

        bottomBar.Controls.Add(shadowingButton, 0, 0);
        bottomBar.Controls.Add(audioButton, 1, 0);
        bottomBar.Controls.Add(aiButton, 2, 0);

        root.Controls.Add(moduleBar, 0, 0);
        root.Controls.Add(split, 0, 1);
        root.Controls.Add(bottomBar, 0, 2);
        Controls.Add(root);

        AutoScaleMode = AutoScaleMode.Font;
        BackColor = PdfSidebarTheme.Background;
        Margin = Padding.Empty;
        Name = "PdfWorkbenchPanel";
        Size = new Size(300, 700);

        ApplyModuleState(notify: false);
    }

    /// <summary>模块可见性或联动状态变化，宿主用于持久化。</summary>
    public event EventHandler? StateChanged;

    public event EventHandler? AudioRequested;

    public event EventHandler? ShadowingRequested;

    public event EventHandler? WholeBookShadowingRequested;

    public event EventHandler<PdfWorkbenchAiAction>? AiActionRequested;

    public event EventHandler? AddHighlightsRequested;

    public event EventHandler? FlashcardsRequested;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool LinkEnabled
    {
        get => _linkEnabled;
        set
        {
            if (_linkEnabled == value) return;
            _linkEnabled = value;
            ApplyModuleState(notify: true);
        }
    }

    public bool IsModuleVisible(PdfWorkbenchModule module)
        => module == PdfWorkbenchModule.Highlight ? _highlightVisible : _translateVisible;

    /// <summary>双开时上下分屏比例，供侧栏状态持久化。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double SplitterRatio
    {
        get => _splitterRatio;
        set
        {
            _splitterRatio = Math.Clamp(value, 0.2, 0.8);
            _splitterInitialized = false;
            EnsureSplitterRatio(force: true);
        }
    }

    /// <summary>把两个独立面板装入工作台；只允许挂载一次，避免重复 Parent 抢焦点。</summary>
    public void AttachContent(Control highlight, Control translate)
    {
        AttachToHost(highlightHost, highlight, "摘录面板");
        AttachToHost(translateHost, translate, "翻译面板");
        ApplyModuleState(notify: false);
    }

    public void ShowModule(PdfWorkbenchModule module)
    {
        SetModuleVisible(module, true);
        // 显式打开时把焦点交给对应面板，方便键盘用户继续操作
        var host = module == PdfWorkbenchModule.Highlight ? highlightHost : translateHost;
        if (host.Controls.Count > 0) host.Controls[0].Focus();
    }

    public void ToggleModule(PdfWorkbenchModule module)
        => SetModuleVisible(module, !IsModuleVisible(module));

    public void SetModuleVisible(PdfWorkbenchModule module, bool visible)
    {
        if (!visible)
        {
            // 至少保留一个模块，避免出现空白工作台
            bool otherVisible = module == PdfWorkbenchModule.Highlight ? _translateVisible : _highlightVisible;
            if (!otherVisible) return;
        }

        if (module == PdfWorkbenchModule.Highlight)
        {
            if (_highlightVisible == visible) return;
            _highlightVisible = visible;
        }
        else
        {
            if (_translateVisible == visible) return;
            _translateVisible = visible;
        }

        ApplyModuleState(notify: true);
    }

    /// <summary>恢复持久化的模块/联动/分屏比例；非法组合（全关）回落为仅摘录。</summary>
    public void RestoreState(bool translateVisible, bool linkEnabled, double splitterRatio = 0.55)
    {
        _highlightVisible = !translateVisible;
        _translateVisible = translateVisible;
        _linkEnabled = linkEnabled;
        _splitterRatio = Math.Clamp(splitterRatio, 0.2, 0.8);
        _splitterInitialized = false;
        ApplyModuleState(notify: false);
    }

    public void SetAddHighlightsEnabled(bool enabled) => _addHighlightsEnabled = enabled;

    public void SetShadowingEnabled(bool currentPageEnabled, bool wholeBookEnabled)
    {
        _shadowingEnabled = currentPageEnabled;
        _wholeBookShadowingEnabled = wholeBookEnabled;
        UpdateMenuState();
    }

    public void ApplyTheme(bool isDark)
    {
        var palette = new PdfSidebarPalette(isDark);
        BackColor = palette.Background;
        root.BackColor = palette.Background;
        moduleBar.BackColor = palette.Background;
        bottomBar.BackColor = palette.Background;
        split.BackColor = palette.Background;
        highlightHost.BackColor = palette.Background;
        translateHost.BackColor = palette.Background;

        StyleModuleButton(highlightButton, _highlightVisible, palette);
        StyleModuleButton(translateButton, _translateVisible, palette);
        StyleLinkButton(linkButton, _linkEnabled, palette, isDark);
        foreach (var button in new[] { shadowingButton, audioButton, aiButton })
        {
            button.BackColor = palette.SurfaceAlt;
            button.ForeColor = palette.Text;
            button.FlatAppearance.BorderColor = palette.Border;
            button.FlatAppearance.MouseOverBackColor = palette.Hover;
        }
    }

    private void ApplyModuleState(bool notify)
    {
        // 双开时上下分屏，单开时折叠另一侧；先展开再折叠，避免 SplitContainer 尺寸非法
        if (_highlightVisible && _translateVisible)
        {
            split.Panel1Collapsed = false;
            split.Panel2Collapsed = false;
            highlightHost.Visible = true;
            translateHost.Visible = true;
            split.IsSplitterFixed = false;
            EnsureSplitterRatio(force: !_splitterInitialized);
        }
        else if (_highlightVisible)
        {
            split.Panel2Collapsed = true;
            split.Panel1Collapsed = false;
            highlightHost.Visible = true;
            translateHost.Visible = false;
            _splitterInitialized = false;
        }
        else
        {
            split.Panel1Collapsed = true;
            split.Panel2Collapsed = false;
            highlightHost.Visible = false;
            translateHost.Visible = true;
            _splitterInitialized = false;
        }

        ApplyModuleButtonState();
        if (notify) StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void EnsureSplitterRatio(bool force = false)
    {
        if (!_highlightVisible || !_translateVisible) return;
        if (_splitterInitialized && !force) return;
        if (split.Height <= 0) return;

        int min = split.Panel1MinSize + split.Panel2MinSize + split.SplitterWidth;
        if (split.Height <= min) return;

        int distance = (int)(split.Height * _splitterRatio);
        distance = Math.Max(split.Panel1MinSize, Math.Min(distance, split.Height - split.Panel2MinSize - split.SplitterWidth));
        try
        {
            split.SplitterDistance = distance;
            _splitterInitialized = true;
        }
        catch (InvalidOperationException)
        {
            // 尺寸尚未稳定时交给下一次 Resize 处理
        }
    }

    private void ApplyModuleButtonState()
    {
        var palette = PdfSidebarTheme.PaletteFor(this);
        bool isDark = PdfSidebarTheme.IsDarkFor(this);
        StyleModuleButton(highlightButton, _highlightVisible, palette);
        StyleModuleButton(translateButton, _translateVisible, palette);
        StyleLinkButton(linkButton, _linkEnabled, palette, isDark);
        UpdateMenuState();
    }

    private void UpdateMenuState()
    {
        if (shadowingMenu.Items.Count >= 2)
        {
            shadowingMenu.Items[0].Enabled = _shadowingEnabled;
            shadowingMenu.Items[1].Enabled = _wholeBookShadowingEnabled;
        }
        if (aiMenu.Items.Count > 0)
        {
            foreach (ToolStripItem item in aiMenu.Items)
            {
                if (item is ToolStripMenuItem { Tag: "edit" } menuItem)
                    menuItem.Enabled = _addHighlightsEnabled;
            }
        }
    }

    private void LayoutModuleBar()
    {
        int width = moduleBar.ClientSize.Width;
        if (width <= 0) return;

        int gap = 6;
        int linkWidth = Math.Min(84, Math.Max(64, width / 4));
        int available = Math.Max(120, width - linkWidth - gap * 2);
        int moduleWidth = Math.Max(64, available / 2);

        highlightButton.SetBounds(0, 6, moduleWidth, 30);
        translateButton.SetBounds(moduleWidth + gap, 6, moduleWidth, 30);
        linkButton.SetBounds(width - linkWidth, 6, linkWidth, 30);
    }

    private static Button CreateModuleButton(string text, string accessibleName)
        => new()
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            Size = new Size(88, 30),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            TabStop = true,
            Tag = "keep",
            AccessibleName = accessibleName
        };

    private static Button CreateBottomButton(string text, string accessibleName)
        => new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei UI", 9F),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            TabStop = true,
            Tag = "secondary",
            AccessibleName = accessibleName
        };

    private static Panel CreateHost(string name)
        => new()
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            BackColor = PdfSidebarTheme.Background,
            Name = name
        };

    private static void AttachToHost(Panel host, Control content, string accessibleName)
    {
        if (host.Controls.Contains(content)) return;

        if (content.Parent != null)
            content.Parent.Controls.Remove(content);

        content.Dock = DockStyle.Fill;
        content.Margin = Padding.Empty;
        content.Visible = true;
        content.AccessibleName = accessibleName;
        host.Controls.Add(content);
    }

    private static void StyleModuleButton(Button button, bool selected, PdfSidebarPalette palette)
    {
        button.BackColor = selected ? palette.BrandSoft : palette.SurfaceAlt;
        button.ForeColor = selected ? PdfSidebarTheme.Brand : palette.Muted;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = selected ? PdfSidebarTheme.Brand : palette.Border;
        button.FlatAppearance.MouseOverBackColor = selected ? palette.BrandSoft : palette.Hover;
    }

    private static void StyleLinkButton(Button button, bool enabled, PdfSidebarPalette palette, bool isDark)
    {
        button.BackColor = enabled
            ? (isDark ? Color.FromArgb(6, 78, 59) : Color.FromArgb(220, 252, 231))
            : palette.SurfaceAlt;
        button.ForeColor = enabled
            ? (isDark ? Color.FromArgb(167, 243, 208) : Color.FromArgb(22, 101, 52))
            : palette.Muted;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = enabled
            ? (isDark ? Color.FromArgb(16, 185, 129) : Color.FromArgb(134, 239, 172))
            : palette.Border;
        button.FlatAppearance.MouseOverBackColor = enabled
            ? (isDark ? Color.FromArgb(6, 95, 70) : Color.FromArgb(187, 247, 208))
            : palette.Hover;
    }

    private ContextMenuStrip BuildShadowingMenu()
    {
        var menu = new ContextMenuStrip { ShowImageMargin = false };
        var current = new ToolStripMenuItem("本页跟读");
        current.Click += (_, _) => ShadowingRequested?.Invoke(this, EventArgs.Empty);
        var wholeBook = new ToolStripMenuItem("整本跟读");
        wholeBook.Click += (_, _) => WholeBookShadowingRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(current);
        menu.Items.Add(wholeBook);
        return menu;
    }

    private ContextMenuStrip BuildAiMenu()
    {
        var menu = new ContextMenuStrip { ShowImageMargin = false };
        menu.Items.Add(CreateAiItem("AI 总结本页", PdfWorkbenchAiAction.Summary));
        menu.Items.Add(CreateAiItem("全文问答", PdfWorkbenchAiAction.Ask));
        menu.Items.Add(CreateAiItem("生成大纲", PdfWorkbenchAiAction.Outline));
        menu.Items.Add(CreateAiItem("导出学习笔记", PdfWorkbenchAiAction.Notes));
        menu.Items.Add(CreateAiItem("批量 OCR", PdfWorkbenchAiAction.BatchOcr));
        menu.Items.Add(new ToolStripSeparator());

        var flashcards = new ToolStripMenuItem("生成闪卡");
        flashcards.Click += (_, _) => FlashcardsRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(flashcards);

        var addHighlights = new ToolStripMenuItem("本页摘录加入学习") { Tag = "edit" };
        addHighlights.Click += (_, _) => AddHighlightsRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(addHighlights);

        return menu;
    }

    private ToolStripMenuItem CreateAiItem(string text, PdfWorkbenchAiAction action)
    {
        var item = new ToolStripMenuItem(text);
        item.Click += (_, _) => AiActionRequested?.Invoke(this, action);
        return item;
    }

    private static void ShowMenu(Button button, ContextMenuStrip menu)
    {
        if (menu.Items.Count == 0) return;
        menu.Show(button, new Point(0, -menu.PreferredSize.Height));
    }
}
