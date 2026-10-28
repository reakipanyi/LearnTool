using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

/// <summary>
/// 阅读器顶部悬浮译条：负责展示选区译文，也复用作“暂停跟读”的状态条。
/// 只做展示与输入转发，翻译与录音逻辑仍由 PdfReaderForm 负责。
/// </summary>
public sealed class PdfTopTranslationBar : Panel
{
    private enum BarMode
    {
        Hidden,
        Translation,
        Shadowing
    }

    private readonly Label _badge = new();
    private readonly Label _original = new();
    private readonly Label _translation = new();
    private readonly Button _btnSpeak = new();
    private readonly Button _btnCopy = new();
    private readonly Button _btnPlay = new();
    private readonly Button _btnClose = new();
    private readonly ToolTip _tooltip = new();
    private readonly Font _badgeFont = new("Microsoft YaHei UI", 8.5F, FontStyle.Bold);
    private readonly Font _originalFont = new("Microsoft YaHei UI", 8.5F);
    private readonly Font _translationFont = new("Microsoft YaHei UI", 11.5F, FontStyle.Bold);
    private readonly Font _actionFont = new("Segoe UI Symbol", 11F);

    private BarMode _mode = BarMode.Hidden;
    private bool _isRecording;
    private float _audioLevel;
    private int _shadowingDurationMs;
    private bool _hasTake;

    public PdfTopTranslationBar()
    {
        DoubleBuffered = true;
        Visible = false;
        Height = 58;
        Padding = new Padding(12, 7, 10, 7);
        BuildChildren();
        ApplyTheme();
        PdfSidebarTheme.ThemeChanged += OnThemeChanged;
    }

    /// <summary>请求朗读选区原文。</summary>
    public event EventHandler? SpeakRequested;

    /// <summary>请求复制原文与译文。</summary>
    public event EventHandler? CopyRequested;

    /// <summary>请求回放最近一条跟读录音。</summary>
    public event EventHandler? PlayTakeRequested;

    /// <summary>用户关闭译条。</summary>
    public event EventHandler? CloseRequested;

    /// <summary>当前是否处于跟读状态条模式。</summary>
    public bool IsShadowingMode => _mode == BarMode.Shadowing;

    /// <summary>当前显示的上行文本（原文或跟读状态）。</summary>
    public string OriginalText => _original.Text;

    /// <summary>当前显示的下行文本（译文或跟读说明）。</summary>
    public string TranslationText => _translation.Text;

    /// <summary>当前译文对应的页码（0 基）；非译文态为 -1。</summary>
    public int PageIndex { get; private set; } = -1;

    /// <summary>展示翻译加载态。</summary>
    public void ShowTranslationLoading(int pageIndex, string originalText)
    {
        _mode = BarMode.Translation;
        PageIndex = pageIndex;
        _isRecording = false;
        _badge.Text = $"P{pageIndex + 1}";
        _original.Text = originalText;
        _translation.Text = "正在识别并翻译…";
        _translation.ForeColor = PdfSidebarTheme.Muted;
        _btnSpeak.Visible = false;
        _btnCopy.Visible = false;
        _btnPlay.Visible = false;
        _btnClose.Visible = true;
        Visible = true;
        Invalidate();
    }

    /// <summary>展示翻译结果。</summary>
    public void ShowTranslationResult(
        int pageIndex,
        string originalText,
        string translation,
        bool fromCache)
    {
        _mode = BarMode.Translation;
        PageIndex = pageIndex;
        _isRecording = false;
        _badge.Text = fromCache ? $"P{pageIndex + 1} 缓存" : $"P{pageIndex + 1}";
        _original.Text = originalText;
        _translation.Text = translation;
        _translation.ForeColor = PdfSidebarTheme.Text;
        _btnSpeak.Visible = !string.IsNullOrWhiteSpace(originalText);
        _btnCopy.Visible = !string.IsNullOrWhiteSpace(translation);
        _btnPlay.Visible = false;
        _btnClose.Visible = true;
        Visible = true;
        Invalidate();
    }

    /// <summary>展示翻译失败态。</summary>
    public void ShowTranslationFailure(int pageIndex, string originalText, string message)
    {
        _mode = BarMode.Translation;
        PageIndex = pageIndex;
        _isRecording = false;
        _badge.Text = $"P{pageIndex + 1}";
        _original.Text = originalText;
        _translation.Text = message;
        _translation.ForeColor = PdfSidebarTheme.Danger;
        _btnSpeak.Visible = false;
        _btnCopy.Visible = false;
        _btnPlay.Visible = false;
        _btnClose.Visible = true;
        Visible = true;
        Invalidate();
    }

    /// <summary>暂停后提示可用 S 开始跟读。</summary>
    public void ShowShadowingHint()
    {
        _mode = BarMode.Shadowing;
        _isRecording = false;
        _hasTake = false;
        _badge.Text = "跟读";
        _original.Text = "已暂停";
        _translation.Text = "按 S 开始跟读";
        _translation.ForeColor = PdfSidebarTheme.Brand;
        ShowShadowingActions(canPlay: false, canClose: true);
        Visible = true;
        Invalidate();
    }

    /// <summary>更新跟读录音中的时长与真实电平。</summary>
    public void ShowShadowingRecording(TimeSpan elapsed, float level)
    {
        _mode = BarMode.Shadowing;
        _isRecording = true;
        _hasTake = false;
        _audioLevel = Math.Clamp(level, 0F, 1F);
        _badge.Text = "跟读";
        _original.Text = $"● 跟读中 {FormatDuration(elapsed)}";
        _translation.Text = "再按空格结束并续播";
        _translation.ForeColor = PdfSidebarTheme.Danger;
        ShowShadowingActions(canPlay: false, canClose: false);
        Visible = true;
        Invalidate();
    }

    /// <summary>跟读已结束、录音仍在落盘。</summary>
    public void ShowShadowingSaving()
    {
        _mode = BarMode.Shadowing;
        _isRecording = false;
        _hasTake = false;
        _audioLevel = 0F;
        _badge.Text = "跟读";
        _original.Text = "已结束跟读";
        _translation.Text = "正在保存录音…";
        _translation.ForeColor = PdfSidebarTheme.Muted;
        ShowShadowingActions(canPlay: false, canClose: true);
        Visible = true;
        Invalidate();
    }

    /// <summary>跟读录音已保存，等待参考分或说明不出分的原因。</summary>
    public void ShowShadowingSaved(int durationMs, bool scorePending, bool scoreAvailable, bool hasTake)
    {
        _mode = BarMode.Shadowing;
        _isRecording = false;
        _hasTake = hasTake;
        _shadowingDurationMs = Math.Max(0, durationMs);
        _audioLevel = 0F;
        _badge.Text = "跟读";
        _original.Text = hasTake
            ? $"已保存 {FormatDuration(TimeSpan.FromMilliseconds(_shadowingDurationMs))}"
            : "跟读已结束";
        _translation.Text = !hasTake
            ? "录音保存失败，本次不出参考分"
            : scorePending
                ? "参考分计算中…"
                : scoreAvailable
                    ? "未取到英文原文，本次不出参考分"
                    : "未安装英语语音识别器，本次不出参考分";
        _translation.ForeColor = PdfSidebarTheme.Muted;
        ShowShadowingActions(canPlay: true, canClose: true);
        Visible = true;
        Invalidate();
    }

    /// <summary>更新参考分；null 表示本次不出分。</summary>
    public void SetShadowingScore(int? score)
    {
        if (_mode != BarMode.Shadowing)
            return;

        _translation.Text = score is >= 0
            ? $"参考分 {score} / 100"
            : "参考分不可用";
        _translation.ForeColor = score is >= 0 ? PdfSidebarTheme.Score : PdfSidebarTheme.Muted;
        Invalidate();
    }

    /// <summary>展示取消跟读后的短暂提示。</summary>
    public void ShowShadowingCancelled()
    {
        _mode = BarMode.Shadowing;
        _isRecording = false;
        _hasTake = false;
        _audioLevel = 0F;
        _badge.Text = "跟读";
        _original.Text = "已取消跟读";
        _translation.Text = "空格继续播放";
        _translation.ForeColor = PdfSidebarTheme.Muted;
        ShowShadowingActions(canPlay: false, canClose: true);
        Visible = true;
        Invalidate();
    }

    /// <summary>收起译条（不触发关闭事件）。</summary>
    public void HideBar()
    {
        _mode = BarMode.Hidden;
        PageIndex = -1;
        _isRecording = false;
        _audioLevel = 0F;
        Visible = false;
    }

    private void BuildChildren()
    {
        _badge.AutoSize = false;
        _badge.Size = new Size(78, 20);
        _badge.TextAlign = ContentAlignment.MiddleCenter;
        _badge.Font = _badgeFont;
        _badge.TabStop = false;

        _original.AutoSize = false;
        _original.AutoEllipsis = true;
        _original.Font = _originalFont;
        _original.ForeColor = PdfSidebarTheme.Muted;
        _original.TabStop = false;

        _translation.AutoSize = false;
        _translation.AutoEllipsis = true;
        _translation.Font = _translationFont;
        _translation.TabStop = false;

        StyleActionButton(_btnSpeak, "🔊");
        StyleActionButton(_btnCopy, "⧉");
        StyleActionButton(_btnPlay, "▶");
        StyleActionButton(_btnClose, "✕");
        _tooltip.SetToolTip(_btnSpeak, "朗读原文");
        _tooltip.SetToolTip(_btnCopy, "复制原文与译文");
        _tooltip.SetToolTip(_btnPlay, "回放刚才的跟读");
        _tooltip.SetToolTip(_btnClose, "关闭译条");

        _btnSpeak.Click += (_, _) => SpeakRequested?.Invoke(this, EventArgs.Empty);
        _btnCopy.Click += (_, _) => CopyRequested?.Invoke(this, EventArgs.Empty);
        _btnPlay.Click += (_, _) => PlayTakeRequested?.Invoke(this, EventArgs.Empty);
        _btnClose.Click += (_, _) =>
        {
            HideBar();
            CloseRequested?.Invoke(this, EventArgs.Empty);
        };

        Controls.Add(_badge);
        Controls.Add(_original);
        Controls.Add(_translation);
        Controls.Add(_btnSpeak);
        Controls.Add(_btnCopy);
        Controls.Add(_btnPlay);
        Controls.Add(_btnClose);
    }

    private void StyleActionButton(Button button, string glyph)
    {
        button.Text = glyph;
        button.Font = _actionFont;
        button.Size = new Size(30, 30);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.UseVisualStyleBackColor = false;
        button.TabStop = false;
        button.Cursor = Cursors.Hand;
        button.Anchor = AnchorStyles.Top | AnchorStyles.Right;
    }

    private void ShowShadowingActions(bool canPlay, bool canClose)
    {
        _btnSpeak.Visible = false;
        _btnCopy.Visible = false;
        _btnPlay.Visible = canPlay && _hasTake;
        _btnClose.Visible = canClose;
    }

    private void ApplyTheme()
    {
        var palette = PdfSidebarTheme.PaletteFor(this);
        BackColor = palette.Surface;
        _original.ForeColor = palette.Muted;
        _badge.BackColor = palette.BrandSoft;
        _badge.ForeColor = PdfSidebarTheme.Brand;
        foreach (var button in new[] { _btnSpeak, _btnCopy, _btnPlay, _btnClose })
        {
            button.BackColor = palette.Surface;
            button.ForeColor = palette.Text;
            button.FlatAppearance.MouseOverBackColor = palette.Hover;
        }

        Invalidate();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(new Action(ApplyTheme));
            return;
        }

        ApplyTheme();
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        LayoutChildren();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var palette = PdfSidebarTheme.PaletteFor(this);
        using var borderPen = new Pen(palette.Border);
        e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

        var accentColor = _isRecording ? PdfSidebarTheme.Danger : PdfSidebarTheme.Brand;
        using var accentBrush = new SolidBrush(accentColor);
        e.Graphics.FillRectangle(accentBrush, 0, 0, 4, Height);

        if (_mode == BarMode.Shadowing && _isRecording)
        {
            var levelWidth = (int)Math.Round((Width - 8) * _audioLevel);
            if (levelWidth > 0)
            {
                using var levelBrush = new SolidBrush(PdfSidebarTheme.Danger);
                e.Graphics.FillRectangle(levelBrush, 4, Height - 3, levelWidth, 3);
            }
        }
    }

    private void LayoutChildren()
    {
        if (Width <= 0)
            return;

        const int left = 12;
        const int actionSize = 30;
        const int actionGap = 2;
        var actionsWidth = actionSize * 3 + actionGap * 2;
        var textWidth = Math.Max(80, Width - left - actionsWidth - 24);

        _badge.SetBounds(left, 10, 78, 20);
        var textLeft = _badge.Right + 8;
        textWidth = Math.Max(80, Width - textLeft - actionsWidth - 18);

        _original.SetBounds(textLeft, 7, textWidth, 18);
        _translation.SetBounds(textLeft, 26, textWidth, 24);

        var right = Width - 10 - actionSize;
        _btnClose.SetBounds(right, 14, actionSize, actionSize);
        _btnPlay.SetBounds(right - actionSize - actionGap, 14, actionSize, actionSize);
        _btnCopy.SetBounds(right - (actionSize + actionGap) * 2, 14, actionSize, actionSize);
        _btnSpeak.SetBounds(right - (actionSize + actionGap) * 3, 14, actionSize, actionSize);
    }

    private static string FormatDuration(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
            elapsed = TimeSpan.Zero;

        return elapsed.TotalHours >= 1
            ? $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}"
            : $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            PdfSidebarTheme.ThemeChanged -= OnThemeChanged;
            _tooltip.Dispose();
            _badgeFont.Dispose();
            _originalFont.Dispose();
            _translationFont.Dispose();
            _actionFont.Dispose();
        }

        base.Dispose(disposing);
    }
}
