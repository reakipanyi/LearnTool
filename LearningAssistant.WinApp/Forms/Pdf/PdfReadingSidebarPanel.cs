using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

/// <summary>「目录」侧栏容器：页头内的双图标按钮切换「列表 / 缩略图」，不再使用二级页签。</summary>
public partial class PdfReadingSidebarPanel : UserControl, ISidebarThemeAware
{
    private Button? _listViewButton;
    private Button? _thumbnailViewButton;
    private Button? _autoPlayAudioButton;
    private Panel? _header;

    public PdfReadingSidebarPanel()
    {
        InitializeComponent();

        // 阶段2：移除「文件/页面」「缩略图」二级页签，改为页头内的视图切换
        root.Controls.Remove(segments);
        segments.Dispose();
        root.Controls.Add(BuildHeader(), 0, 0);

        tabs.SelectedIndexChanged += (_, _) =>
        {
            UpdateViewButtons();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        };
        UpdateViewButtons();
        UpdateAutoPlayButton();
    }

    /// <summary>视图切换后触发，宿主据此按需加载缩略图。</summary>
    public event EventHandler? SelectedIndexChanged;

    /// <summary>「自动播放音频」开关变化后触发，只有用户点击才会触发。</summary>
    public event EventHandler? AutoPlayAudioChanged;

    /// <summary>打开文档后是否自动播放配套音频；状态由宿主持久化。</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool AutoPlayAudioEnabled
    {
        get => _autoPlayAudioEnabled;
        set
        {
            if (_autoPlayAudioEnabled == value) return;
            _autoPlayAudioEnabled = value;
            UpdateAutoPlayButton();
        }
    }

    private bool _autoPlayAudioEnabled = true;

    /// <summary>当前视图索引（0 列表，1 缩略图）。</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => tabs.SelectedIndex;
        set
        {
            if (value < 0 || value >= tabs.TabPages.Count || tabs.SelectedIndex == value) return;
            tabs.SelectedIndex = value;
        }
    }

    /// <summary>列表视图子页，宿主挂载文件筛选框与目录树。</summary>
    public TabPage FilesPage => filesPage;

    /// <summary>缩略图视图子页，宿主挂载缩略图面板。</summary>
    public TabPage ThumbnailsPage => thumbnailsPage;

    public void ApplyTheme(bool isDark)
    {
        UpdateViewButtons();
        UpdateAutoPlayButton();
    }

    private Control BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 4)
        };
        _header = header;

        var title = new Label
        {
            Text = "目录",
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(84, 10)
        };

        // 靠左放置：右侧抽屉会覆盖侧栏右半区，避免视图切换按钮被遮住
        var toggle = new Panel
        {
            Dock = DockStyle.None,
            Width = 78,
            Height = 28,
            Location = new Point(0, 0),
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };

        _listViewButton = CreateViewButton("☰", "列表视图", 0);
        _thumbnailViewButton = CreateViewButton("▦", "缩略图视图", 38);
        _listViewButton.Click += (_, _) => SelectedIndex = 0;
        _thumbnailViewButton.Click += (_, _) => SelectedIndex = 1;

        toggle.Controls.Add(_listViewButton);
        toggle.Controls.Add(_thumbnailViewButton);

        // 打开文档后自动播放配套音频；双击目录中的 PDF 始终会立即播放
        _autoPlayAudioButton = new Button
        {
            Text = "▶ 自动播放",
            Font = new Font("Microsoft YaHei UI", 9F),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(104, 28),
            Location = new Point(0, 0),
            TabStop = true,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            // keep：开关态由本控件自行着色
            Tag = "keep"
        };
        _autoPlayAudioButton.FlatAppearance.BorderSize = 0;
        _autoPlayAudioButton.AccessibleName = "自动播放音频";
        _sidebarToolTip.SetToolTip(_autoPlayAudioButton, "打开 PDF 后自动播放配套音频；双击目录中的 PDF 立即播放");
        _autoPlayAudioButton.Click += (_, _) =>
        {
            AutoPlayAudioEnabled = !AutoPlayAudioEnabled;
            AutoPlayAudioChanged?.Invoke(this, EventArgs.Empty);
        };

        header.Controls.Add(title);
        header.Controls.Add(toggle);
        header.Controls.Add(_autoPlayAudioButton);
        header.Resize += (_, _) => LayoutAutoPlayAudioButton();
        return header;
    }

    /// <summary>右对齐自动播放开关；侧栏变窄时仍保留给标题的最小空间。</summary>
    private void LayoutAutoPlayAudioButton()
    {
        if (_header == null || _autoPlayAudioButton == null) return;

        int left = Math.Max(176, _header.ClientSize.Width - _autoPlayAudioButton.Width - 4);
        int top = Math.Max(0, (_header.ClientSize.Height - _autoPlayAudioButton.Height) / 2);
        _autoPlayAudioButton.Location = new Point(left, top);
    }

    private Button CreateViewButton(string glyph, string tooltip, int left)
    {
        var button = new Button
        {
            Text = glyph,
            Font = new Font("Segoe UI Symbol", 12F),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(34, 28),
            Location = new Point(left, 0),
            TabStop = true,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            // keep：由本控件按选中态统一着色，避免被通用按钮主题覆盖
            Tag = "keep"
        };
        button.FlatAppearance.BorderSize = 0;
        _sidebarToolTip.SetToolTip(button, tooltip);
        return button;
    }

    private readonly ToolTip _sidebarToolTip = new() { AutoPopDelay = 4000 };

    private void UpdateViewButtons()
    {
        if (_listViewButton == null || _thumbnailViewButton == null) return;

        var palette = PdfSidebarTheme.PaletteFor(this);
        bool listSelected = tabs.SelectedIndex == 0;

        StyleViewButton(_listViewButton, listSelected, palette);
        StyleViewButton(_thumbnailViewButton, !listSelected, palette);
    }

    private static void StyleViewButton(Button button, bool selected, PdfSidebarPalette palette)
    {
        button.BackColor = selected ? palette.BrandSoft : palette.Surface;
        button.ForeColor = selected ? PdfSidebarTheme.Brand : palette.Muted;
        button.FlatAppearance.MouseOverBackColor = palette.Hover;
        button.FlatAppearance.MouseDownBackColor = palette.BrandSoft;
    }

    /// <summary>开关开启时用品牌色标示，关闭时退回普通表面色。</summary>
    private void UpdateAutoPlayButton()
    {
        if (_autoPlayAudioButton == null) return;

        var palette = PdfSidebarTheme.PaletteFor(this);
        bool enabled = _autoPlayAudioEnabled;
        _autoPlayAudioButton.Text = enabled ? "▶ 自动播放" : "▷ 自动播放";
        _autoPlayAudioButton.BackColor = enabled ? palette.BrandSoft : palette.Surface;
        _autoPlayAudioButton.ForeColor = enabled ? PdfSidebarTheme.Brand : palette.Muted;
        _autoPlayAudioButton.FlatAppearance.MouseOverBackColor = palette.Hover;
        _autoPlayAudioButton.FlatAppearance.MouseDownBackColor = palette.BrandSoft;
        _autoPlayAudioButton.AccessibleDescription = enabled ? "已开启" : "已关闭";
    }
}
