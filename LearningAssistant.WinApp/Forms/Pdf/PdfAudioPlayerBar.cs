using System.Diagnostics;
using LearningAssistant.Abstractions.Media;
using LearningAssistant.Common.UI;
using LearningAssistant.Managers;
using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Forms.Pdf;

/// <summary>
/// 阅读器底部配套音频播放器。只负责渲染与输入转发，播放状态全部来自 PdfAudioTrackManager。
/// </summary>
public sealed class PdfAudioPlayerBar : UserControl
{
    private readonly PdfAudioTrackManager _manager;
    private readonly IPdfReaderFormAccess _form;
    private readonly Action _onBindRequested;

    private readonly Label _labelSource = new();
    private readonly Label _labelBook = new();
    private readonly Label _labelTrack = new();
    private readonly Label _labelPage = new();
    private readonly Button _btnPrev = new();
    private readonly Button _btnPlay = new();
    private readonly Button _btnNext = new();
    private readonly TrackBar _track = new();
    private readonly Label _labelElapsed = new();
    private readonly Label _labelDuration = new();
    private readonly Button _btnSpeed = new();
    private readonly Button _btnVolume = new();
    private readonly Button _btnFollow = new();
    private readonly Button _btnMore = new();
    private readonly ToolTip _tooltip = new();
    private readonly ContextMenuStrip _loopMenu = new();
    private readonly ToolStripMenuItem _menuSyncPage = new("切歌同步页码");
    private readonly ToolStripMenuItem _menuSetA = new("设复读起点 A");
    private readonly ToolStripMenuItem _menuSetB = new("设复读终点 B");
    private readonly ToolStripMenuItem _menuClearAb = new("清除 A-B 复读");

    private bool _dragging;
    private double _lastVolume = 1.0;
    /// <summary>进度条刷新时钟；按 10Hz 限频，避免高频 PositionChanged 造成无效重绘</summary>
    private readonly Stopwatch _progressClock = Stopwatch.StartNew();
    private TimeSpan _lastProgressAt;

    private const int SliderMax = 1000;

    public PdfAudioPlayerBar(PdfAudioTrackManager manager, IPdfReaderFormAccess form, Action onBindRequested)
    {
        _manager = manager;
        _form = form;
        _onBindRequested = onBindRequested;

        Height = 64;
        Dock = DockStyle.Bottom;
        DoubleBuffered = true;
        BuildLayout();
        WireEvents();
        ApplyTheme();
        RefreshAll();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(16, 4, 16, 4)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildInfo(), 0, 0);
        root.Controls.Add(BuildTransport(), 1, 0);
        root.Controls.Add(BuildRightSide(), 2, 0);
        Controls.Add(root);

        Paint += (_, e) =>
        {
            var palette = PdfSidebarTheme.PaletteFor(this);
            using var pen = new Pen(palette.Border);
            e.Graphics.DrawLine(pen, 0, 0, Width, 0);
        };
    }

    private Control BuildInfo()
    {
        var info = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 0, 12, 0)
        };
        info.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        info.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 2, 0, 0)
        };
        StyleBadge(_labelSource, "配套音频");
        _labelBook.AutoSize = true;
        _labelBook.Margin = new Padding(8, 5, 0, 0);
        _labelBook.Font = new Font("Microsoft YaHei UI", 8.5F);
        _labelBook.AutoEllipsis = true;
        _labelBook.Cursor = Cursors.Hand;
        _labelBook.Click += (_, _) => _onBindRequested();
        top.Controls.Add(_labelSource);
        top.Controls.Add(_labelBook);

        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0)
        };
        _labelTrack.AutoSize = true;
        _labelTrack.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        _labelTrack.Margin = new Padding(0, 0, 8, 0);
        _labelTrack.AutoEllipsis = true;
        _labelTrack.Cursor = Cursors.Hand;
        _labelTrack.Click += (_, _) => _onBindRequested();
        StyleBadge(_labelPage, "P—");
        bottom.Controls.Add(_labelTrack);
        bottom.Controls.Add(_labelPage);

        info.Controls.Add(top, 0, 0);
        info.Controls.Add(bottom, 0, 1);
        return info;
    }

    private Control BuildTransport()
    {
        var transport = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0)
        };
        transport.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        transport.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var buttons = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            Margin = new Padding(0)
        };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        StyleTransportButton(_btnPrev, "⏮");
        StyleTransportButton(_btnPlay, "▶");
        StyleTransportButton(_btnNext, "⏭");
        _btnPlay.Font = new Font("Segoe UI Symbol", 12F, FontStyle.Bold);
        _btnPrev.Margin = new Padding(0, 0, 6, 0);
        _btnNext.Margin = new Padding(6, 0, 0, 0);

        buttons.Controls.Add(_btnPrev, 1, 0);
        buttons.Controls.Add(_btnPlay, 2, 0);
        buttons.Controls.Add(_btnNext, 3, 0);

        var progress = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0)
        };
        progress.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        progress.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        progress.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        StyleTimeLabel(_labelElapsed);
        StyleTimeLabel(_labelDuration);
        _labelElapsed.Text = "00:00";
        _labelDuration.Text = "00:00";

        _track.Minimum = 0;
        _track.Maximum = SliderMax;
        _track.TickStyle = TickStyle.None;
        _track.SmallChange = 10;
        _track.LargeChange = 100;
        _track.Dock = DockStyle.Fill;
        _track.Height = 22;
        _track.Margin = new Padding(6, 0, 6, 0);

        progress.Controls.Add(_labelElapsed, 0, 0);
        progress.Controls.Add(_track, 1, 0);
        progress.Controls.Add(_labelDuration, 2, 0);

        transport.Controls.Add(buttons, 0, 0);
        transport.Controls.Add(progress, 0, 1);
        return transport;
    }

    private Control BuildRightSide()
    {
        var right = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(8, 0, 0, 0),
            Padding = new Padding(0, 14, 0, 0)
        };

        StylePill(_btnMore, "⋯", 34);
        StylePill(_btnFollow, "跟随", 52);
        StylePill(_btnVolume, "🔊", 34);
        StylePill(_btnSpeed, "1.0x", 48);
        // 变速引擎尚未验证（设计要求不能发布可点却无效的控件），只记录书级倍速，暂不开放
        _btnSpeed.Enabled = false;
        _tooltip.SetToolTip(_btnSpeed, "变速引擎待验证，当前固定 1.0x");

        right.Controls.Add(_btnMore);
        right.Controls.Add(_btnFollow);
        right.Controls.Add(_btnVolume);
        right.Controls.Add(_btnSpeed);

        _loopMenu.Items.Add("不循环", null, (_, _) => SetLoopMode(MediaLoopMode.None));
        _loopMenu.Items.Add("列表循环", null, (_, _) => SetLoopMode(MediaLoopMode.RepeatAll));
        _loopMenu.Items.Add("单曲循环", null, (_, _) => SetLoopMode(MediaLoopMode.RepeatOne));
        _loopMenu.Items.Add(new ToolStripSeparator());
        _menuSyncPage.Click += (_, _) =>
        {
            _manager.SetSyncPageOnTrackChange(!_manager.SyncPageOnTrackChange);
            UpdateLoopChecks();
        };
        _loopMenu.Items.Add(_menuSyncPage);
        _loopMenu.Items.Add(new ToolStripSeparator());
        _loopMenu.Items.Add(_menuSetA);
        _loopMenu.Items.Add(_menuSetB);
        _loopMenu.Items.Add(_menuClearAb);
        _loopMenu.Opening += (_, _) => UpdateAbLoopMenu();
        return right;
    }

    private void WireEvents()
    {
        _btnPrev.Click += (_, _) => _manager.Prev();
        _btnNext.Click += (_, _) => _manager.Next();
        _btnPlay.Click += (_, _) =>
        {
            bool ok = _manager.IsPlaying ? _manager.Pause() : _manager.Play();
            if (!ok) _manager.SetHint("当前页无音频 · 去绑定");
        };

        _btnVolume.Click += (_, _) =>
        {
            var player = _manager.Playback.Player;
            if (player.Volume > 0.01)
            {
                _lastVolume = player.Volume;
                player.Volume = 0;
            }
            else
            {
                player.Volume = _lastVolume <= 0.01 ? 1.0 : _lastVolume;
            }
            UpdateVolumeButton();
        };

        _btnFollow.Click += (_, _) =>
        {
            _manager.SetFollowPage(!_manager.FollowPage);
            UpdateFollowButton();
        };

        _btnMore.Click += (_, _) => _loopMenu.Show(_btnMore, new Point(0, _btnMore.Height));

        _menuSetA.Click += (_, _) =>
        {
            var player = _manager.Playback.Player;
            _manager.Playback.SetAbLoop(player.Position, player.AbLoop.B);
            UpdateAbLoopMenu();
        };
        _menuSetB.Click += (_, _) =>
        {
            var player = _manager.Playback.Player;
            _manager.Playback.SetAbLoop(player.AbLoop.A, player.Position);
            UpdateAbLoopMenu();
        };
        _menuClearAb.Click += (_, _) =>
        {
            _manager.Playback.SetAbLoop(null, null);
            _manager.Playback.Player.LoopMode = MediaLoopMode.None;
            UpdateAbLoopMenu();
        };

        _track.MouseDown += (_, _) => _dragging = true;
        _track.MouseUp += (_, _) => CommitSeek();
        _track.Scroll += (_, _) =>
        {
            if (_dragging) _labelElapsed.Text = FormatTime(SliderToPosition(_track.Value));
        };

        _manager.ProfileLoaded += OnProfileLoaded;
        _manager.TrackChanged += OnTrackChanged;
        _manager.PageHintChanged += OnPageHintChanged;
        _manager.SettingsChanged += OnSettingsChanged;
        _manager.Playback.Player.PositionChanged += OnPositionChanged;
        _manager.Playback.Player.StateChanged += OnStateChanged;
        PdfSidebarTheme.ThemeChanged += OnThemeChanged;
    }

    private void OnProfileLoaded(object? sender, EventArgs e) => RefreshAll();
    private void OnTrackChanged(object? sender, AudioTrack track) => RefreshTrack();
    private void OnPageHintChanged(object? sender, string? hint) => RefreshTrack();

    /// <summary>侧栏或播放器自身修改设置后，统一刷新开关、按钮与曲目信息。</summary>
    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        UpdateFollowButton();
        UpdateLoopChecks();
        UpdateTransport();
        RefreshTrack();
    }
    /// <summary>翻页后由宿主调用，更新页徽标与空态提示。</summary>
    public void RefreshPage() => RefreshTrack();

    /// <summary>进度条按 10Hz 刷新，避免高频 PositionChanged 造成无效重绘</summary>
    private void OnPositionChanged(object? sender, TimeSpan position)
    {
        var now = _progressClock.Elapsed;
        if (now - _lastProgressAt < TimeSpan.FromMilliseconds(100)) return;
        _lastProgressAt = now;
        UpdateProgress(position);
    }
    private void OnStateChanged(object? sender, MediaPlaybackState state) => UpdateTransport();

    private void OnThemeChanged(object? sender, EventArgs e) => ApplyTheme();

    private void CommitSeek()
    {
        _dragging = false;
        var player = _manager.Playback.Player;
        if (player.Duration <= TimeSpan.Zero) return;
        var target = SliderToPosition(_track.Value);
        if (target > player.Duration) target = player.Duration;
        _manager.Playback.Seek(target);
        UpdateProgress(target);
    }

    private TimeSpan SliderToPosition(int value)
    {
        var duration = _manager.Playback.Player.Duration;
        if (duration <= TimeSpan.Zero) return TimeSpan.Zero;
        return TimeSpan.FromTicks((long)(duration.Ticks * (value / (double)SliderMax)));
    }

    private void UpdateProgress(TimeSpan position)
    {
        if (_dragging) return;
        var duration = _manager.Playback.Player.Duration;
        _labelElapsed.Text = FormatTime(position);
        _labelDuration.Text = FormatTime(duration);
        if (duration <= TimeSpan.Zero)
        {
            _track.Value = 0;
            return;
        }
        double ratio = Math.Clamp(position.TotalMilliseconds / duration.TotalMilliseconds, 0, 1);
        _track.Value = (int)Math.Round(ratio * SliderMax);
    }

    private void RefreshAll()
    {
        var result = _manager.Result;
        _labelBook.Text = string.IsNullOrWhiteSpace(_form.CurrentPdfPath)
            ? "未打开文档"
            : Path.GetFileNameWithoutExtension(_form.CurrentPdfPath);

        if (result == null || result.Tracks.Count == 0)
        {
            _labelSource.Text = "未绑定";
            _labelTrack.Text = "未绑定音频 · 去绑定";
            _labelPage.Text = "P—";
            _labelElapsed.Text = "00:00";
            _labelDuration.Text = "00:00";
            _track.Value = 0;
        }
        else
        {
            _labelSource.Text = "配套音频";
        }

        UpdateFollowButton();
        UpdateVolumeButton();
        UpdateLoopChecks();
        UpdateTransport();
        RefreshTrack();
    }

    private void RefreshTrack()
    {
        if (_manager.Result == null || _manager.Result.Tracks.Count == 0)
        {
            _labelTrack.Text = "未绑定音频 · 去绑定";
            _labelPage.Text = "P—";
            UpdateTransport();
            return;
        }

        var track = _manager.CurrentTrack ?? _manager.Result.PageIndex.GetValueOrDefault(_form.CurrentPageIndex);
        var hint = _manager.PageHint;

        if (track == null)
        {
            _labelTrack.Text = hint ?? "本页无音频 · 去绑定";
            _labelPage.Text = $"P{_form.CurrentPageIndex + 1}";
        }
        else
        {
            _labelTrack.Text = Path.GetFileNameWithoutExtension(track.FileName);
            _labelPage.Text = DescribeTrackPages(track);
        }

        if (!_manager.IsPlaying && _manager.Playback.Player.Duration > TimeSpan.Zero)
            UpdateProgress(_manager.Playback.Player.Position);
        else if (_manager.Playback.Player.Duration <= TimeSpan.Zero)
        {
            _track.Value = 0;
            _labelElapsed.Text = "00:00";
            _labelDuration.Text = "00:00";
        }

        UpdateTransport();
    }

    private string DescribeTrackPages(AudioTrack track)
    {
        var result = _manager.Result;
        if (result == null) return $"P{_form.CurrentPageIndex + 1}";

        var pages = result.PageIndex
            .Where(kv => string.Equals(kv.Value.FilePath, track.FilePath, StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Key)
            .OrderBy(p => p)
            .ToList();

        if (pages.Count == 0) return $"P{_form.CurrentPageIndex + 1}";
        if (pages.Count == result.PageIndex.Count && pages.Count > 1) return "全篇";
        if (pages.Count == 1) return $"P{pages[0] + 1}";
        return $"P{pages[0] + 1}–P{pages[^1] + 1}";
    }

    private void UpdateTransport()
    {
        bool hasTracks = (_manager.Result?.Tracks.Count ?? 0) > 0;
        _btnPrev.Enabled = hasTracks && _manager.CanGoPrev;
        _btnNext.Enabled = hasTracks && _manager.CanGoNext;
        _btnPlay.Enabled = hasTracks || _manager.CurrentTrack != null;
        _btnPlay.Text = _manager.IsPlaying ? "⏸" : "▶";
    }

    private void UpdateVolumeButton()
    {
        bool muted = _manager.Playback.Player.Volume <= 0.01;
        _btnVolume.Text = muted ? "🔇" : "🔊";
        _tooltip.SetToolTip(_btnVolume, muted ? "取消静音" : "静音");
    }

    private void UpdateFollowButton()
    {
        var palette = PdfSidebarTheme.PaletteFor(this);
        if (_manager.Profile?.PlaybackScope == PlaybackScope.WholeBook)
        {
            _btnFollow.Text = "整篇";
            _btnFollow.Enabled = false;
            _btnFollow.BackColor = palette.SurfaceAlt;
            _btnFollow.ForeColor = palette.Muted;
            _tooltip.SetToolTip(_btnFollow, "整篇音频不随翻页切换");
            return;
        }

        bool follow = _manager.FollowPage;
        _btnFollow.Enabled = true;
        _btnFollow.Text = follow ? "跟随" : "不跟随";
        _btnFollow.BackColor = follow ? palette.BrandSoft : palette.SurfaceAlt;
        _btnFollow.ForeColor = follow ? PdfSidebarTheme.Brand : palette.Text;
        _tooltip.SetToolTip(_btnFollow, follow ? "翻页自动切换音频（点击关闭）" : "翻页不切换音频（点击开启）");
    }

    /// <summary>同步 A-B 菜单可用状态；未设置端点时禁用清除。</summary>
    private void UpdateAbLoopMenu()
    {
        var (a, b) = _manager.Playback.Player.AbLoop;
        _menuClearAb.Enabled = a != null || b != null;
    }

    private void SetLoopMode(MediaLoopMode mode)
    {
        _manager.LoopMode = mode;
        UpdateLoopChecks();
        UpdateTransport();
    }

    private void UpdateLoopChecks()
    {
        if (_loopMenu.Items.Count < 3) return;
        for (int i = 0; i < 3; i++)
            ((ToolStripMenuItem)_loopMenu.Items[i]).Checked = (MediaLoopMode)i == _manager.LoopMode;
        _menuSyncPage.Checked = _manager.SyncPageOnTrackChange;
        _btnMore.BackColor = _manager.LoopMode == MediaLoopMode.None
            ? PdfSidebarTheme.PaletteFor(this).SurfaceAlt
            : PdfSidebarTheme.PaletteFor(this).BrandSoft;
        _tooltip.SetToolTip(_btnMore, $"更多：循环={_manager.LoopMode switch
        {
            MediaLoopMode.RepeatAll => "列表",
            MediaLoopMode.RepeatOne => "单曲",
            _ => "关闭"
        }}");
    }

    private void ApplyTheme()
    {
        var palette = PdfSidebarTheme.PaletteFor(this);
        BackColor = palette.Surface;
        ForeColor = palette.Text;

        foreach (var label in new[] { _labelBook, _labelTrack, _labelElapsed, _labelDuration, _labelPage })
            label.ForeColor = label == _labelTrack ? palette.Text : palette.Muted;

        _labelSource.BackColor = palette.BrandSoft;
        _labelSource.ForeColor = PdfSidebarTheme.Brand;
        _labelPage.BackColor = palette.SurfaceAlt;
        _labelPage.ForeColor = palette.Muted;

        foreach (var button in new[] { _btnPrev, _btnPlay, _btnNext, _btnSpeed, _btnVolume, _btnFollow, _btnMore })
        {
            button.BackColor = palette.SurfaceAlt;
            button.ForeColor = palette.Text;
            button.FlatAppearance.MouseOverBackColor = palette.Hover;
            button.FlatAppearance.MouseDownBackColor = palette.BrandSoft;
        }

        _loopMenu.BackColor = palette.Surface;
        _loopMenu.ForeColor = palette.Text;
        UpdateFollowButton();
        UpdateLoopChecks();
        Invalidate();
    }

    private void StyleTransportButton(Button button, string text)
    {
        button.Text = text;
        button.Size = new Size(34, 28);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Font = new Font("Segoe UI Symbol", 10F);
        button.Cursor = Cursors.Hand;
        button.Margin = new Padding(0);
        button.TabStop = false;
    }

    private void StylePill(Button button, string text, int width)
    {
        button.Text = text;
        button.Size = new Size(width, 28);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Font = new Font("Microsoft YaHei UI", 8.5F);
        button.Cursor = Cursors.Hand;
        button.Margin = new Padding(4, 0, 0, 0);
        button.TabStop = false;
    }

    private void StyleBadge(Label label, string text)
    {
        label.Text = text;
        label.AutoSize = true;
        label.Padding = new Padding(6, 2, 6, 2);
        label.Font = new Font("Microsoft YaHei UI", 8F);
        label.Margin = new Padding(0, 3, 0, 0);
    }

    private static void StyleTimeLabel(Label label)
    {
        label.AutoSize = true;
        label.Margin = new Padding(0, 3, 0, 0);
        label.Font = new Font("Consolas", 8.5F);
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time < TimeSpan.Zero) time = TimeSpan.Zero;
        return time.TotalHours >= 1
            ? $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}"
            : $"{time.Minutes:00}:{time.Seconds:00}";
    }

    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        // 构造时尚未挂到窗体，取不到夜间模式；挂载后重新取色
        ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _manager.ProfileLoaded -= OnProfileLoaded;
            _manager.TrackChanged -= OnTrackChanged;
            _manager.PageHintChanged -= OnPageHintChanged;
            _manager.SettingsChanged -= OnSettingsChanged;
            _manager.Playback.Player.PositionChanged -= OnPositionChanged;
            _manager.Playback.Player.StateChanged -= OnStateChanged;
            PdfSidebarTheme.ThemeChanged -= OnThemeChanged;
            _tooltip.Dispose();
            _loopMenu.Dispose();
        }
        base.Dispose(disposing);
    }
}
