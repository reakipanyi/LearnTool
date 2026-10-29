using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

/// <summary>
/// PDF 连续朗读控制条：播放/暂停、上一句/下一句、停止、续听与语速。
/// 只负责展示与输入转发，句子调度与高亮状态由 PdfReadingPlaybackManager 负责。
/// </summary>
public sealed class PdfReadingPlaybackBar : Panel
{
    private static readonly double[] SpeedOptions = { 0.6, 0.8, 1.0, 1.25, 1.5, 2.0 };

    private readonly Label _status = new();
    private readonly Button _btnPlayPause = new();
    private readonly Button _btnPrevious = new();
    private readonly Button _btnNext = new();
    private readonly Button _btnStop = new();
    private readonly Button _btnResume = new();
    private readonly ComboBox _speed = new();
    private readonly Button _btnClose = new();
    private readonly ToolTip _tooltip = new();

    private bool _playing;
    private bool _resumeAvailable;

    public PdfReadingPlaybackBar()
    {
        DoubleBuffered = true;
        Visible = false;
        Height = 44;
        Dock = DockStyle.Bottom;
        Padding = new Padding(12, 6, 10, 6);
        BuildChildren();
        ApplyTheme();
        PdfSidebarTheme.ThemeChanged += OnThemeChanged;
        SetIdle();
    }

    /// <summary>播放/暂停切换。</summary>
    public event EventHandler? PlayPauseRequested;

    /// <summary>上一句。</summary>
    public event EventHandler? PreviousRequested;

    /// <summary>下一句。</summary>
    public event EventHandler? NextRequested;

    /// <summary>停止朗读。</summary>
    public event EventHandler? StopRequested;

    /// <summary>从上次断点继续。</summary>
    public event EventHandler? ResumeRequested;

    /// <summary>语速变化（单位：倍）。</summary>
    public event EventHandler? SpeedChanged;

    /// <summary>关闭朗读条。</summary>
    public event EventHandler? CloseRequested;

    /// <summary>当前选择的语速倍数。</summary>
    public double Speed
    {
        get
        {
            int index = _speed.SelectedIndex;
            return index >= 0 && index < SpeedOptions.Length ? SpeedOptions[index] : 1.0;
        }
    }

    /// <summary>设置语速下拉框（不改动全局 TTS 配置）。</summary>
    public void SetSpeed(double speed)
    {
        int best = 0;
        double min = double.MaxValue;
        for (int i = 0; i < SpeedOptions.Length; i++)
        {
            double diff = Math.Abs(SpeedOptions[i] - speed);
            if (diff < min)
            {
                min = diff;
                best = i;
            }
        }
        _speed.SelectedIndex = best;
    }

    /// <summary>无朗读任务时的初始状态。</summary>
    public void SetIdle()
    {
        _playing = false;
        _btnPlayPause.Text = "▶ 朗读";
        _tooltip.SetToolTip(_btnPlayPause, "从本页第一句开始连续朗读");
        _status.Text = "连续朗读：逐句朗读并自动翻页";
        UpdateActionStates();
    }

    /// <summary>正在朗读某页某句。</summary>
    public void SetPlaying(int pageIndex, int sentenceIndex, int total)
    {
        _playing = true;
        _btnPlayPause.Text = "⏸ 暂停";
        _tooltip.SetToolTip(_btnPlayPause, "暂停朗读");
        _status.Text = $"第 {pageIndex + 1} 页 · 第 {sentenceIndex + 1}/{Math.Max(total, 1)} 句 · 朗读中";
        UpdateActionStates();
    }

    /// <summary>已暂停。</summary>
    public void SetPaused(int pageIndex, int sentenceIndex, int total)
    {
        _playing = true;
        _btnPlayPause.Text = "▶ 继续";
        _tooltip.SetToolTip(_btnPlayPause, "从当前句继续朗读");
        _status.Text = $"第 {pageIndex + 1} 页 · 第 {sentenceIndex + 1}/{Math.Max(total, 1)} 句 · 已暂停";
        UpdateActionStates();
    }

    /// <summary>设置是否有可用的断点续听。</summary>
    public void SetResumeAvailable(bool available)
    {
        _resumeAvailable = available;
        _btnResume.Visible = available;
        LayoutChildren();
    }

    /// <summary>刷新状态文本的兜底方法（任务结束但保留条时调用）。</summary>
    public void SetStatusText(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
            _status.Text = text;
    }

    private void BuildChildren()
    {
        _status.AutoSize = false;
        _status.AutoEllipsis = true;
        _status.Font = new Font("Microsoft YaHei UI", 9F);
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.TabStop = false;

        StyleButton(_btnPlayPause, "▶ 朗读", 82);
        StyleButton(_btnPrevious, "⏮", 32);
        StyleButton(_btnNext, "⏭", 32);
        StyleButton(_btnStop, "⏹", 32);
        StyleButton(_btnResume, "续听", 52);
        StyleButton(_btnClose, "✕", 28);

        _tooltip.SetToolTip(_btnPrevious, "上一句");
        _tooltip.SetToolTip(_btnNext, "下一句");
        _tooltip.SetToolTip(_btnStop, "停止朗读");
        _tooltip.SetToolTip(_btnResume, "从上次听读位置继续");
        _tooltip.SetToolTip(_btnClose, "关闭朗读条并停止朗读");

        _speed.DropDownStyle = ComboBoxStyle.DropDownList;
        _speed.FlatStyle = FlatStyle.Flat;
        _speed.Font = new Font("Microsoft YaHei UI", 9F);
        _speed.Size = new Size(66, 26);
        _speed.TabStop = false;
        foreach (double option in SpeedOptions)
            _speed.Items.Add($"{option:0.##}x");
        _speed.SelectedIndex = 2;
        _tooltip.SetToolTip(_speed, "朗读语速");
        _speed.SelectedIndexChanged += (_, _) => SpeedChanged?.Invoke(this, EventArgs.Empty);

        _btnPlayPause.Click += (_, _) => PlayPauseRequested?.Invoke(this, EventArgs.Empty);
        _btnPrevious.Click += (_, _) => PreviousRequested?.Invoke(this, EventArgs.Empty);
        _btnNext.Click += (_, _) => NextRequested?.Invoke(this, EventArgs.Empty);
        _btnStop.Click += (_, _) => StopRequested?.Invoke(this, EventArgs.Empty);
        _btnResume.Click += (_, _) => ResumeRequested?.Invoke(this, EventArgs.Empty);
        _btnClose.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);

        Controls.Add(_status);
        Controls.Add(_btnPlayPause);
        Controls.Add(_btnPrevious);
        Controls.Add(_btnNext);
        Controls.Add(_btnStop);
        Controls.Add(_btnResume);
        Controls.Add(_speed);
        Controls.Add(_btnClose);
    }

    private void StyleButton(Button button, string text, int width)
    {
        button.Text = text;
        button.Font = new Font("Microsoft YaHei UI", 9F);
        button.Size = new Size(width, 28);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.UseVisualStyleBackColor = false;
        button.TabStop = false;
        button.Cursor = Cursors.Hand;
    }

    private void UpdateActionStates()
    {
        _btnPrevious.Enabled = _playing;
        _btnNext.Enabled = _playing;
        _btnStop.Enabled = _playing;
        _btnResume.Enabled = _resumeAvailable && !_playing;
        _btnPlayPause.Enabled = true;
        LayoutChildren();
    }

    private void ApplyTheme()
    {
        var palette = PdfSidebarTheme.PaletteFor(this);
        BackColor = palette.Surface;
        _status.ForeColor = palette.Muted;

        foreach (var button in new[] { _btnPlayPause, _btnPrevious, _btnNext, _btnStop, _btnResume, _btnClose })
        {
            button.BackColor = palette.Surface;
            button.ForeColor = palette.Text;
            button.FlatAppearance.MouseOverBackColor = palette.Hover;
        }

        _btnPlayPause.BackColor = PdfSidebarTheme.BrandSoft;
        _btnPlayPause.ForeColor = PdfSidebarTheme.Brand;
        _btnResume.ForeColor = PdfSidebarTheme.Brand;

        _speed.BackColor = palette.FieldBackground;
        _speed.ForeColor = palette.Text;

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
        e.Graphics.DrawLine(borderPen, 0, 0, Width, 0);

        using var accentBrush = new SolidBrush(PdfSidebarTheme.Brand);
        e.Graphics.FillRectangle(accentBrush, 0, 0, 4, Height);
    }

    private void LayoutChildren()
    {
        if (Width <= 0)
            return;

        const int gap = 6;
        int right = Width - 10;

        _btnClose.SetBounds(right - _btnClose.Width, 8, _btnClose.Width, 28);
        right = _btnClose.Left - gap;

        _speed.SetBounds(right - _speed.Width, 8, _speed.Width, 26);
        right = _speed.Left - gap;

        if (_btnResume.Visible)
        {
            _btnResume.SetBounds(right - _btnResume.Width, 8, _btnResume.Width, 28);
            right = _btnResume.Left - gap;
        }

        _btnStop.SetBounds(right - _btnStop.Width, 8, _btnStop.Width, 28);
        right = _btnStop.Left - gap;

        _btnNext.SetBounds(right - _btnNext.Width, 8, _btnNext.Width, 28);
        right = _btnNext.Left - gap;

        _btnPrevious.SetBounds(right - _btnPrevious.Width, 8, _btnPrevious.Width, 28);
        right = _btnPrevious.Left - gap;

        _btnPlayPause.SetBounds(right - _btnPlayPause.Width, 8, _btnPlayPause.Width, 28);

        int statusLeft = 14;
        int statusWidth = Math.Max(80, _btnPlayPause.Left - statusLeft - 12);
        _status.SetBounds(statusLeft, 8, statusWidth, 28);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            PdfSidebarTheme.ThemeChanged -= OnThemeChanged;
            _tooltip.Dispose();
        }

        base.Dispose(disposing);
    }
}
