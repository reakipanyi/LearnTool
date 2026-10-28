using System.ComponentModel;
using System.Text.RegularExpressions;
using LearningAssistant.Common.UI;
using LearningAssistant.Forms.UserControls.Common;

namespace LearningAssistant.Forms.Pdf;

/// <summary>翻译面板的内容来源。</summary>
public enum PdfTranslateSource
{
    Page,
    Highlight
}

/// <summary>「翻译」侧栏面板：页头 + 来源切换 + 原文/译文卡片 + 操作区 + 选项区。</summary>
public partial class PdfTranslatePanel : UserControl, ISidebarThemeAware
{
    /// <summary>目标语言：显示名与百度翻译语言代码一一对应</summary>
    private static readonly (string Label, string Code)[] TargetLanguages =
    {
        ("自动", "auto"),
        ("中文", "zh"),
        ("English", "en"),
        ("日本語", "jp"),
        ("한국어", "kor"),
        ("Français", "fra"),
        ("Deutsch", "de"),
        ("Español", "spa"),
        ("Русский", "ru")
    };

    private readonly TableLayoutPanel sourceBar;
    private readonly Button pageSourceButton;
    private readonly Button highlightSourceButton;
    private readonly Label sourceBadge;
    private string _pageOriginalText = string.Empty;
    private string _pageTranslationText = string.Empty;
    private string _highlightOriginalText = string.Empty;
    private string _highlightTranslationText = string.Empty;
    private int _highlightPageIndex = -1;
    private bool _switchingSource;
    private PdfTranslateSource _source = PdfTranslateSource.Page;

    public PdfTranslatePanel()
    {
        InitializeComponent();
        InitTargetLanguageOptions();
        sourceBar = BuildSourceBar(out pageSourceButton, out highlightSourceButton, out sourceBadge);
        InsertSourceBar();
        // 卡片内容区由面板自行挂载：VS 设计器无法序列化自定义容器的 Body，
        // 放在构造里可保证设计视图与运行期都能看到文本框。
        originalCard.Body.Controls.Add(originalBox);
        translationCard.Body.Controls.Add(translationBox);
        _tip.SetToolTip(ocrButton, "仅识别本页文字，不翻译；无文本层时自动调用 OCR");
        _tip.SetToolTip(shadowingButton, "用当前页原文直接开始分句跟读");
        _tip.SetToolTip(addToLearningButton, "把选区或本页原文加入个人学习库");
        _tip.SetToolTip(autoTranslateCheck, "识别后自动翻译原文");
        _tip.SetToolTip(autoSpeakCheck, "识别后自动朗读原文");
        _tip.SetToolTip(aiPolishCheck, "使用 AI 术语表润色译文，保持术语前后一致（失败时自动保留机器译文）");
        _tip.SetToolTip(targetLanguageCombo, "选择译文目标语言；「自动」表示中文译英文、其余译中文");
        _tip.SetToolTip(glossaryButton, "查看/编辑本文档的翻译术语表，并查看术语在当前页的命中次数");
        _tip.SetToolTip(reTranslateButton, "按当前原文（识别结果可直接修改）重新翻译本页");
        _tip.SetToolTip(translationBox, "点击译文段落，可定位对应原文段落");
        _tip.SetToolTip(originalBox, "识别结果可直接编辑；修改后点「重新翻译」生效。点击段落可定位对应译文");

        // 卡片右上角的朗读按钮：默认 ghost 样式过于浅淡，这里恢复为清晰可见的强调样式
        StylePlayButton(originalCard, "🔊 播放原文");
        StylePlayButton(translationCard, "🔊 播放译文");
        _tip.SetToolTip(originalCard.ActionButton!, "朗读左侧原文（按原文语言自动选择音色）");
        _tip.SetToolTip(translationCard.ActionButton!, "朗读右侧译文（按译文语言自动选择音色）");

        // 段落级联动：两侧段数一致时才接管点击，避免译文被合并/拆分时定位错乱
        originalBox.MouseClick += (_, e) => AlignParagraph(originalBox, translationBox, e.Location);
        translationBox.MouseClick += (_, e) => AlignParagraph(translationBox, originalBox, e.Location);

        body.Resize += (_, _) =>
        {
            // 设计器内不重排行列样式，避免 VS 把运行期结果写回 Designer 文件
            if (!DesignMode) LayoutCards();
        };
    }

    /// <summary>运行期按实际宽度决定卡片上下堆叠还是左右对照。</summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        LayoutCards();
    }

    /// <summary>页头，宿主用于更新副标题状态。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SidebarHeaderBar Header => header;

    /// <summary>原文卡片，宿主为卡片右上角朗读按钮绑定事件。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SidebarCard OriginalCard => originalCard;

    /// <summary>译文卡片，宿主为卡片右上角朗读按钮绑定事件。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SidebarCard TranslationCard => translationCard;

    /// <summary>原文文本框。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SyncedTextBox OriginalBox => originalBox;

    /// <summary>译文文本框。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SyncedTextBox TranslationBox => translationBox;

    /// <summary>整页识别（OCR）按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button OcrButton => ocrButton;

    /// <summary>句子跟读按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button ShadowingButton => shadowingButton;

    /// <summary>添加到学习按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button AddToLearningButton => addToLearningButton;

    /// <summary>识别后自动翻译。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CheckBox AutoTranslateCheck => autoTranslateCheck;

    /// <summary>识别后自动朗读。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CheckBox AutoSpeakCheck => autoSpeakCheck;

    /// <summary>AI 润色。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CheckBox AiPolishCheck => aiPolishCheck;

    /// <summary>语速选择。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SpeedSelectorControl TtsSpeedSelector => speedSelector;

    /// <summary>当前目标语言代码（百度翻译语言码）；auto 表示按原文自动判定。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string TargetLanguageCode
    {
        get
        {
            int index = targetLanguageCombo.SelectedIndex;
            return index >= 0 && index < TargetLanguages.Length ? TargetLanguages[index].Code : "auto";
        }
        set
        {
            int index = Array.FindIndex(TargetLanguages,
                item => string.Equals(item.Code, value, StringComparison.OrdinalIgnoreCase));
            targetLanguageCombo.SelectedIndex = index >= 0 ? index : 0;
        }
    }

    /// <summary>目标语言下拉框，供宿主在切换时刷新状态。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ComboBox TargetLanguageCombo => targetLanguageCombo;

    /// <summary>术语表管理按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button GlossaryButton => glossaryButton;

    /// <summary>按当前原文（可直接编辑）重新翻译按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button ReTranslateButton => reTranslateButton;

    /// <summary>把卡片右上角的朗读按钮做成边框 + 品牌色的强调样式，保证在明暗主题下都能一眼看到。</summary>
    private static void StylePlayButton(SidebarCard card, string text)
    {
        var button = card.ActionButton;
        if (button == null) return;

        button.Text = text;
        button.AutoSize = false;
        button.Size = new Size(110, 26);
        button.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = PdfSidebarTheme.Border;
        button.FlatAppearance.MouseOverBackColor = PdfSidebarTheme.BrandSoft;
        button.BackColor = PdfSidebarTheme.Surface;
        button.ForeColor = PdfSidebarTheme.Brand;
        button.Cursor = Cursors.Hand;
        // keep：主题刷新时保留自定义强调色，只更新表面色与边框
        button.Tag = "keep";
        button.Location = new Point(card.Width - button.Width - 12, 6);
        // SidebarCard 的 _body 面板 Dock=Fill 会盖住后加入的按钮，必须提到最前，否则按钮不可见
        button.BringToFront();
    }

    /// <summary>填充目标语言下拉框，默认「自动」。</summary>
    private void InitTargetLanguageOptions()
    {
        foreach (var option in TargetLanguages)
            targetLanguageCombo.Items.Add(option.Label);
        targetLanguageCombo.SelectedIndex = 0;
    }

    /// <summary>当前内容来源：整页或选中摘录。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PdfTranslateSource Source => _source;

    /// <summary>当前摘录所属页；无摘录时为 -1。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int HighlightPageIndex => _highlightPageIndex;

    /// <summary>来源切换完成事件；宿主据此修正文本来源页与页头副标题。</summary>
    public event EventHandler? SourceChanged;

    /// <summary>切换来源；先把当前文本存回原槽位，再载入目标槽位，互不覆盖。</summary>
    public void SwitchSource(PdfTranslateSource source)
    {
        if (_source == source)
        {
            UpdateSourceBar();
            return;
        }

        SaveCurrentSlot();
        _source = source;
        LoadCurrentSlot();
        UpdateSourceBar();
        SourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>切回整页槽。</summary>
    public void ShowPageSource() => SwitchSource(PdfTranslateSource.Page);

    /// <summary>把一条摘录送入「摘录」槽并切换过去。</summary>
    public void ShowHighlightSource(int pageIndex, string original, string translation)
    {
        SaveCurrentSlot();
        _source = PdfTranslateSource.Highlight;
        _highlightPageIndex = pageIndex;
        _highlightOriginalText = original ?? string.Empty;
        _highlightTranslationText = translation ?? string.Empty;
        LoadCurrentSlot();
        UpdateSourceBar();
        SourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>页级流程写入原文：切回整页槽，避免覆盖摘录槽内容。</summary>
    public void SetPageOriginalText(string text)
    {
        _pageOriginalText = text ?? string.Empty;
        if (_source != PdfTranslateSource.Page)
            SwitchSource(PdfTranslateSource.Page);
        else
            SetBoxText(originalBox, _pageOriginalText);
    }

    /// <summary>页级流程写入译文：只更新整页槽，不打断用户正在查看的摘录。</summary>
    public void SetPageTranslationText(string text)
    {
        _pageTranslationText = text ?? string.Empty;
        if (_source == PdfTranslateSource.Page)
            SetBoxText(translationBox, _pageTranslationText);
    }

    /// <summary>更新当前摘录槽的译文（例如翻译回包）。</summary>
    public void SetHighlightTranslation(int pageIndex, string original, string translation)
    {
        // 来源页或原文已变化时按新摘录处理，避免把旧译文贴到新内容上
        if (_source == PdfTranslateSource.Highlight && _highlightPageIndex == pageIndex)
        {
            _highlightTranslationText = translation ?? string.Empty;
            SetBoxText(translationBox, _highlightTranslationText);
            return;
        }

        ShowHighlightSource(pageIndex, original, translation);
    }

    public void ApplyTheme(bool isDark) => UpdateSourceBar();

    private void SaveCurrentSlot()
    {
        if (_switchingSource) return;
        if (_source == PdfTranslateSource.Page)
        {
            _pageOriginalText = originalBox.Text;
            _pageTranslationText = translationBox.Text;
        }
        else
        {
            _highlightOriginalText = originalBox.Text;
            _highlightTranslationText = translationBox.Text;
        }
    }

    private void LoadCurrentSlot()
    {
        _switchingSource = true;
        try
        {
            originalBox.Text = _source == PdfTranslateSource.Page
                ? _pageOriginalText
                : _highlightOriginalText;
            translationBox.Text = _source == PdfTranslateSource.Page
                ? _pageTranslationText
                : _highlightTranslationText;
        }
        finally
        {
            _switchingSource = false;
        }
    }

    private void SetBoxText(TextBox box, string text)
    {
        if (string.Equals(box.Text, text, StringComparison.Ordinal)) return;
        _switchingSource = true;
        try
        {
            box.Text = text;
        }
        finally
        {
            _switchingSource = false;
        }
    }

    private static TableLayoutPanel BuildSourceBar(
        out Button pageButton,
        out Button highlightButton,
        out Label badge)
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = new Padding(2, 1, 2, 1),
            BackColor = Color.Transparent,
            Name = "sourceBar"
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        pageButton = CreateSourceButton("📄 整页", "查看整页原文与译文");
        highlightButton = CreateSourceButton("✍ 摘录", "查看选中摘录的原文与译文");
        badge = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Microsoft YaHei UI", 8.5F),
            AutoEllipsis = true,
            Margin = new Padding(2, 0, 0, 0),
            Text = "整页"
        };

        bar.Controls.Add(pageButton, 0, 0);
        bar.Controls.Add(highlightButton, 1, 0);
        bar.Controls.Add(badge, 2, 0);
        return bar;
    }

    private static Button CreateSourceButton(string text, string accessibleName)
        => new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei UI", 8.5F),
            Margin = new Padding(0, 0, 2, 0),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Tag = "keep",
            AccessibleName = accessibleName
        };

    /// <summary>把来源切换条插到页头下方，并把原有行整体下移一行。</summary>
    private void InsertSourceBar()
    {
        root.RowCount += 1;
        root.RowStyles.Insert(1, new RowStyle(SizeType.Absolute, 30F));
        foreach (var control in new Control[] { body, actions, extras, options })
            root.SetRow(control, root.GetRow(control) + 1);
        root.Controls.Add(sourceBar, 0, 1);

        pageSourceButton.Click += (_, _) => ShowPageSource();
        highlightSourceButton.Click += (_, _) => SwitchSource(PdfTranslateSource.Highlight);
        UpdateSourceBar();
    }

    private void UpdateSourceBar()
    {
        var palette = PdfSidebarTheme.PaletteFor(this);
        bool isDark = PdfSidebarTheme.IsDarkFor(this);
        StyleSourceButton(pageSourceButton, _source == PdfTranslateSource.Page, palette, isDark);
        StyleSourceButton(highlightSourceButton, _source == PdfTranslateSource.Highlight, palette, isDark);
        highlightSourceButton.Enabled = _source == PdfTranslateSource.Highlight
            || !string.IsNullOrWhiteSpace(_highlightOriginalText);
        sourceBadge.ForeColor = palette.Muted;
        sourceBadge.Text = _source == PdfTranslateSource.Highlight
            ? (_highlightPageIndex >= 0 ? $"第 {_highlightPageIndex + 1} 页摘录" : "当前摘录")
            : "整页";
    }

    private static void StyleSourceButton(
        Button button,
        bool selected,
        PdfSidebarPalette palette,
        bool isDark)
    {
        button.BackColor = selected ? palette.BrandSoft : palette.SurfaceAlt;
        button.ForeColor = selected ? PdfSidebarTheme.Brand : palette.Muted;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = selected ? PdfSidebarTheme.Brand : palette.Border;
        button.FlatAppearance.MouseOverBackColor = selected ? palette.BrandSoft : palette.Hover;
        button.Cursor = button.Enabled ? Cursors.Hand : Cursors.Default;
    }

    /// <summary>窄侧栏上下堆叠、宽侧栏左右对照；高度不足（双开）时强制左右对照。</summary>
    private void LayoutCards()
    {
        bool wide = body.ClientSize.Width >= 420 || body.ClientSize.Height < 260;
        body.SuspendLayout();
        body.ColumnStyles.Clear();
        body.RowStyles.Clear();

        if (wide)
        {
            body.ColumnCount = 2;
            body.RowCount = 1;
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            body.SetCellPosition(originalCard, new TableLayoutPanelCellPosition(0, 0));
            body.SetCellPosition(translationCard, new TableLayoutPanelCellPosition(1, 0));
            originalCard.Margin = new Padding(0, 0, 2, 0);
            translationCard.Margin = new Padding(2, 0, 0, 0);
        }
        else
        {
            body.ColumnCount = 1;
            body.RowCount = 2;
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            body.SetCellPosition(originalCard, new TableLayoutPanelCellPosition(0, 0));
            body.SetCellPosition(translationCard, new TableLayoutPanelCellPosition(0, 1));
            originalCard.Margin = new Padding(0, 0, 0, 2);
            translationCard.Margin = new Padding(0, 2, 0, 0);
        }

        body.ResumeLayout(true);
        LayoutBottomRows();
    }

    /// <summary>底部两行按可用宽度换行，并按换行后的实际行数分配行高；操作按钮行固定为三等分。</summary>
    private void LayoutBottomRows()
    {
        extras.WrapContents = true;
        options.WrapContents = true;

        int available = root.ClientSize.Width - root.Padding.Horizontal;
        if (available <= 0) available = root.Width - root.Padding.Horizontal;

        SetRowHeight(2, 38);
        SetRowHeight(3, Math.Max(36, MeasureFlowHeight(extras, available) + 2));
        SetRowHeight(4, Math.Max(30, MeasureFlowHeight(options, available) + 2));
    }

    /// <summary>按 FlowLayoutPanel 的换行规则估算所需高度，供固定行高的表格分配空间。</summary>
    private static int MeasureFlowHeight(FlowLayoutPanel flow, int availableWidth)
    {
        int paddingLeft = flow.Padding.Left;
        int limit = availableWidth - flow.Padding.Right;
        int x = paddingLeft;
        int y = flow.Padding.Top;
        int lineHeight = 0;

        foreach (Control child in flow.Controls)
        {
            if (!child.Visible) continue;
            Padding margin = child.Margin;
            int width = child.Width + margin.Horizontal;
            int height = child.Height + margin.Vertical;
            if (x > paddingLeft && x + width > limit)
            {
                y += lineHeight;
                x = paddingLeft;
                lineHeight = 0;
            }

            x += width;
            lineHeight = Math.Max(lineHeight, height);
        }

        return y + lineHeight + flow.Padding.Bottom;
    }

    private void SetRowHeight(int index, int height)
    {
        if (index >= root.RowStyles.Count) return;
        if (Math.Abs(root.RowStyles[index].Height - height) < 0.5f) return;
        root.RowStyles[index].Height = height;
    }

    /// <summary>点击某一段落时，选中另一侧相同序号的段落并滚动到可见位置；段数不一致时保持原行为。</summary>
    private static void AlignParagraph(SyncedTextBox source, SyncedTextBox target, Point location)
    {
        var sourceSpans = BuildParagraphSpans(source.Text);
        var targetSpans = BuildParagraphSpans(target.Text);
        if (sourceSpans.Count == 0 || sourceSpans.Count != targetSpans.Count) return;

        int charIndex = source.GetCharIndexFromPosition(location);
        int index = 0;
        for (int i = 0; i < sourceSpans.Count; i++)
        {
            if (sourceSpans[i].Start > charIndex) break;
            index = i;
        }

        var (start, length) = targetSpans[index];
        target.SuppressScrollSync = true;
        target.SelectionStart = start;
        target.SelectionLength = length;
        target.ScrollToCaret();
        // 滚动完成后恢复同步，避免这次定位被比例滚动逻辑反向覆盖
        if (target.IsHandleCreated && !target.IsDisposed)
            target.BeginInvoke(new Action(() => target.SuppressScrollSync = false));
    }

    /// <summary>按空行切分段落，返回每段在文本框中的字符偏移。</summary>
    private static List<(int Start, int Length)> BuildParagraphSpans(string? text)
    {
        var spans = new List<(int Start, int Length)>();
        if (string.IsNullOrWhiteSpace(text)) return spans;

        int start = 0;
        foreach (Match separator in Regex.Matches(text, @"\r?\n[ \t]*\r?\n"))
        {
            AddSpan(text, start, separator.Index, spans);
            start = separator.Index + separator.Length;
        }
        AddSpan(text, start, text.Length, spans);
        return spans;
    }

    private static void AddSpan(string text, int start, int end, List<(int Start, int Length)> spans)
    {
        while (start < end && char.IsWhiteSpace(text[start])) start++;
        while (end > start && char.IsWhiteSpace(text[end - 1])) end--;
        if (end > start) spans.Add((start, end - start));
    }
}

/// <summary>带纵向滚动通知的多行文本框，用于原文/译文同步滚动。</summary>
public sealed class SyncedTextBox : TextBox
{
    private const int WM_VSCROLL = 0x0115;
    private const int WM_MOUSEWHEEL = 0x020A;

    public event EventHandler? VerticalScrolled;

    /// <summary>为 true 时宿主跳过比例滚动同步，供段落级定位临时接管滚动。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SuppressScrollSync { get; set; }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == WM_VSCROLL || m.Msg == WM_MOUSEWHEEL)
        {
            VerticalScrolled?.Invoke(this, EventArgs.Empty);
        }
    }
}
