using System.Diagnostics;
using System.Globalization;
using LearningAssistant.Abstractions.Media;
using LearningAssistant.Common.UI;
using LearningAssistant.Managers;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Shadowing;

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
    private readonly Button _btnMark = new();
    private readonly Button _btnMarkAgain = new();
    private readonly Button _btnMarkEnd = new();
    private readonly Label _labelMark = new();
    private readonly System.Windows.Forms.Timer _markTimer = new() { Interval = 500 };
    private readonly ToolTip _tooltip = new();
    private readonly ContextMenuStrip _loopMenu = new();
    private readonly ContextMenuStrip _speedMenu = new();
    private readonly Panel _speedPanel = new();
    private readonly FlowLayoutPanel _speedChips = new();
    private readonly TrackBar _speedSlider = new();
    private readonly Label _speedTitle = new();
    private readonly Label _speedValue = new();
    private bool _syncingSpeed;
    private static readonly double[] SpeedPresets = { 0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0 };
    private readonly ToolStripMenuItem _menuSyncPage = new("切歌同步页码");
    private readonly ToolStripMenuItem _menuSetA = new("设复读起点 A");
    private readonly ToolStripMenuItem _menuSetB = new("设复读终点 B");
    private readonly ToolStripMenuItem _menuClearAb = new("清除 A-B 复读");

    private bool _dragging;
    private double _lastVolume = 1.0;
    /// <summary>进度条刷新时钟；按 10Hz 限频，避免高频 PositionChanged 造成无效重绘</summary>
    private readonly Stopwatch _progressClock = Stopwatch.StartNew();
    private TimeSpan _lastProgressAt;

    private ShadowingMarkSession? _markSession;
    private DateTime _markHintUntil = DateTime.MinValue;

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
        BuildSpeedMenu();

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
            ColumnCount = 6,
            RowCount = 1,
            Margin = new Padding(0)
        };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
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

        StyleMarkButton();
        _btnMark.Margin = new Padding(10, 0, 0, 0);

        buttons.Controls.Add(_btnPrev, 1, 0);
        buttons.Controls.Add(_btnPlay, 2, 0);
        buttons.Controls.Add(_btnMark, 3, 0);
        buttons.Controls.Add(_btnNext, 4, 0);

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
        _tooltip.SetToolTip(_btnSpeed, "播放速度（右键恢复 1.0x）");

        _labelMark.AutoSize = true;
        _labelMark.Font = new Font("Consolas", 8.5F);
        _labelMark.Margin = new Padding(4, 8, 2, 0);
        _labelMark.Visible = false;
        StylePill(_btnMarkAgain, "⟳ 再录", 52);
        StylePill(_btnMarkEnd, "结束", 44);
        _btnMarkAgain.Visible = false;
        _btnMarkEnd.Visible = false;

        right.Controls.Add(_btnMore);
        right.Controls.Add(_btnFollow);
        right.Controls.Add(_btnVolume);
        right.Controls.Add(_btnSpeed);
        right.Controls.Add(_labelMark);
        right.Controls.Add(_btnMarkAgain);
        right.Controls.Add(_btnMarkEnd);

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

    /// <summary>点击倍速胶囊弹出的速度面板：7 档预设 + 0.05 步进滑块。</summary>
    private void BuildSpeedMenu()
    {
        _speedPanel.Size = new Size(252, 100);
        _speedPanel.Margin = new Padding(0);

        _speedTitle.Text = "播放速度";
        _speedTitle.AutoSize = true;
        _speedTitle.Location = new Point(10, 7);
        _speedTitle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);

        _speedChips.Location = new Point(5, 28);
        _speedChips.Size = new Size(242, 28);
        _speedChips.FlowDirection = FlowDirection.LeftToRight;
        _speedChips.WrapContents = false;
        _speedChips.Margin = new Padding(0);
        foreach (double preset in SpeedPresets)
        {
            var chip = new Button
            {
                Text = FormatSpeed(preset),
                Tag = preset,
                Size = new Size(32, 26),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 8F),
                Cursor = Cursors.Hand,
                Margin = new Padding(1, 0, 1, 0),
                TabStop = false
            };
            chip.FlatAppearance.BorderSize = 0;
            chip.Click += (_, _) =>
            {
                ApplyPlaybackSpeed(preset);
                _speedMenu.Close();
            };
            _speedChips.Controls.Add(chip);
        }

        _speedSlider.Minimum = 50;
        _speedSlider.Maximum = 200;
        _speedSlider.SmallChange = 5;
        _speedSlider.LargeChange = 25;
        _speedSlider.TickStyle = TickStyle.None;
        _speedSlider.Location = new Point(5, 58);
        _speedSlider.Size = new Size(196, 32);
        _speedSlider.ValueChanged += OnSpeedSliderValueChanged;
        // 拖动/键盘调整过程中只更新读数，松手或按键抬起后再真正换挡，避免连续重建输出链
        _speedSlider.MouseUp += (_, _) => CommitSpeedSlider();
        _speedSlider.KeyUp += (_, _) => CommitSpeedSlider();

        _speedValue.Location = new Point(200, 64);
        _speedValue.Size = new Size(46, 22);
        _speedValue.TextAlign = ContentAlignment.MiddleRight;
        _speedValue.Font = new Font("Consolas", 9F);

        _speedPanel.Controls.Add(_speedTitle);
        _speedPanel.Controls.Add(_speedChips);
        _speedPanel.Controls.Add(_speedSlider);
        _speedPanel.Controls.Add(_speedValue);

        var host = new ToolStripControlHost(_speedPanel)
        {
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            AutoSize = false,
            Size = _speedPanel.Size
        };
        _speedMenu.Items.Add(host);
        _speedMenu.Padding = Padding.Empty;
        _speedMenu.ShowImageMargin = false;
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

        _btnSpeed.Click += (_, _) =>
        {
            SyncSpeedUi();
            // 播放器贴在窗口底部，面板向上弹出；取不到首选高度时按面板固定高度兜底
            int height = _speedMenu.GetPreferredSize(Size.Empty).Height;
            if (height < 20) height = _speedPanel.Height + 8;
            _speedMenu.Show(_btnSpeed, new Point(0, -height));
        };
        _btnSpeed.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Right) ApplyPlaybackSpeed(1.0);
        };

        _btnMore.Click += (_, _) => _loopMenu.Show(_btnMore, new Point(0, _btnMore.Height));

        _btnMark.Click += async (_, _) => await OnMarkClickedAsync();
        _btnMarkAgain.Click += (_, _) => StartMarkRecording();
        _btnMarkEnd.Click += async (_, _) => await FinishMarkSessionAsync();
        _markTimer.Tick += (_, _) => UpdateMarkStatus();

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

    private void OnPageHintChanged(object? sender, string? hint)
    {
        RefreshTrack();
        // 打点锁定页时翻页提示只走到 PageHint，借状态行把原因说清楚
        if (SessionActive && !string.IsNullOrWhiteSpace(hint))
            ShowMarkHint(hint);
    }

    /// <summary>侧栏或播放器自身修改设置后，统一刷新开关、按钮与曲目信息。</summary>
    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        UpdateFollowButton();
        UpdateSpeedButton();
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
        UpdateSpeedButton();
        UpdateLoopChecks();
        UpdateTransport();
        RefreshTrack();
        UpdateSessionControls();
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
        bool locked = SessionActive;
        _btnPrev.Enabled = !locked && hasTracks && _manager.CanGoPrev;
        _btnNext.Enabled = !locked && hasTracks && _manager.CanGoNext;
        _btnPlay.Enabled = !locked && (hasTracks || _manager.CurrentTrack != null);
        _btnPlay.Text = _manager.IsPlaying ? "⏸" : "▶";
        _track.Enabled = !locked;
        UpdateMarkButton();
    }

    private bool SessionActive => _markSession is { State: not ShadowingMarkState.Idle };

    private bool EnsureMarkSession()
    {
        if (_markSession != null)
            return true;
        if (string.IsNullOrWhiteSpace(_form.CurrentPdfPath))
        {
            _manager.SetHint("未打开文档，无法进入跟读打点。");
            return false;
        }

        var recorder = _form.AudioRecorderService;
        if (recorder == null)
        {
            _manager.SetHint("录音服务不可用，无法进入跟读打点。");
            return false;
        }

        _markSession = new ShadowingMarkSession(
            _manager, recorder, _form.CurrentPdfPath, _form.CurrentPageIndex);
        _markSession.StateChanged += (_, _) =>
        {
            UpdateMarkButton();
            UpdateMarkStatus();
            UpdateSessionControls();
            UpdateTransport();
        };
        _markSession.HintChanged += (_, message) => ShowMarkHint(message);
        _markSession.Completed += (_, summary) =>
            ShowMarkHint(summary.CreatedSegments > 0
                ? $"已生成 {summary.CreatedSegments} 段原声，可打开跟读练习。"
                : "本次打点没有生成原声切片。");
        return true;
    }

    private async Task OnMarkClickedAsync()
    {
        if (!EnsureMarkSession())
            return;

        var session = _markSession!;
        switch (session.State)
        {
            case ShadowingMarkState.Idle:
                session.Begin();
                break;
            case ShadowingMarkState.Playing:
                session.Mark();
                break;
            case ShadowingMarkState.Marked:
                if (session.PendingTakeCount > 0)
                    session.ResumeFromMark();
                else
                    session.StartRecording();
                break;
            case ShadowingMarkState.Recording:
                session.StopRecording();
                break;
        }

        UpdateMarkButton();
        UpdateMarkStatus();
        UpdateSessionControls();
    }

    private void StartMarkRecording()
    {
        if (_markSession is { State: ShadowingMarkState.Marked })
            _markSession.StartRecording();
        UpdateMarkButton();
        UpdateSessionControls();
    }

    private async Task FinishMarkSessionAsync()
    {
        if (_markSession == null || !SessionActive)
            return;
        _btnMarkEnd.Enabled = false;
        await _markSession.FinishAsync();
        _btnMarkEnd.Enabled = true;
        UpdateMarkButton();
        UpdateSessionControls();
    }

    private void UpdateMarkButton()
    {
        var palette = PdfSidebarTheme.PaletteFor(this);
        var state = _markSession?.State ?? ShadowingMarkState.Idle;
        _btnMark.UseVisualStyleBackColor = false;
        _btnMark.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(PdfSidebarTheme.Brand, 0.08f);
        _btnMark.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(PdfSidebarTheme.Brand, 0.16f);

        switch (state)
        {
            case ShadowingMarkState.Playing:
                _btnMark.Text = "● 打断";
                _btnMark.Size = new Size(78, 28);
                _btnMark.BackColor = PdfSidebarTheme.Danger;
                _btnMark.ForeColor = Color.White;
                _btnMark.Enabled = true;
                _tooltip.SetToolTip(_btnMark, "打断当前播放并记录时间戳");
                break;
            case ShadowingMarkState.Marked:
                bool hasTake = _markSession!.PendingTakeCount > 0;
                _btnMark.Text = hasTake ? "▶ 续播" : "⦿ 开始跟读";
                _btnMark.Size = new Size(hasTake ? 72 : 92, 28);
                _btnMark.BackColor = PdfSidebarTheme.Brand;
                _btnMark.ForeColor = Color.White;
                _btnMark.Enabled = true;
                _tooltip.SetToolTip(_btnMark, hasTake ? "从打断位置继续播放" : "开始录音");
                break;
            case ShadowingMarkState.Recording:
                _btnMark.Text = "■ 结束录音";
                _btnMark.Size = new Size(92, 28);
                _btnMark.BackColor = PdfSidebarTheme.Danger;
                _btnMark.ForeColor = Color.White;
                _btnMark.Enabled = true;
                _tooltip.SetToolTip(_btnMark, "结束本次跟读录音");
                break;
            case ShadowingMarkState.Slicing:
                _btnMark.Text = $"{_markSession!.SlicingDone}/{_markSession.SlicingTotal}";
                _btnMark.Size = new Size(56, 28);
                _btnMark.BackColor = palette.SurfaceAlt;
                _btnMark.ForeColor = palette.Muted;
                _btnMark.Enabled = false;
                _tooltip.SetToolTip(_btnMark, "正在后台切原声，请稍候");
                break;
            default:
                _btnMark.Text = "⦿ 打点";
                _btnMark.Size = new Size(78, 28);
                _btnMark.BackColor = PdfSidebarTheme.Brand;
                _btnMark.ForeColor = Color.White;
                _btnMark.Enabled = _manager.CurrentTrack != null && _form.AudioRecorderService != null;
                _tooltip.SetToolTip(_btnMark, "跟读打点：播放中点按打断并录音");
                break;
        }
    }

    private void UpdateSessionControls()
    {
        bool active = SessionActive;
        var state = _markSession?.State ?? ShadowingMarkState.Idle;

        // 会话期间隐藏常驻设置，给时间戳与结束按钮让位，避免窄窗口裁切
        foreach (var button in new[] { _btnMore, _btnFollow, _btnVolume, _btnSpeed })
            button.Visible = !active;

        _labelMark.Visible = active;
        _btnMarkAgain.Visible = active && state == ShadowingMarkState.Marked
                                && _markSession!.PendingTakeCount > 0;
        _btnMarkEnd.Visible = active && state is (ShadowingMarkState.Playing or ShadowingMarkState.Marked);

        if (active)
        {
            if (!_markTimer.Enabled) _markTimer.Start();
            UpdateMarkStatus();
        }
        else
        {
            _markTimer.Stop();
            _markHintUntil = DateTime.MinValue;
        }
    }

    /// <summary>状态行：播放中显示页码/段数，待录显示时间戳，录音显示时长，切片显示进度。</summary>
    private void UpdateMarkStatus()
    {
        if (!SessionActive)
            return;
        if (DateTime.Now < _markHintUntil)
            return;

        var session = _markSession!;
        _labelMark.ForeColor = session.State == ShadowingMarkState.Recording
            ? PdfSidebarTheme.Danger
            : PdfSidebarTheme.Brand;
        _labelMark.Text = session.State switch
        {
            ShadowingMarkState.Playing => $"P{_form.CurrentPageIndex + 1} · {session.SegmentCount}段",
            ShadowingMarkState.Marked => ShadowingMarkSession.FormatTimestamp(session.CurrentTimestampMs ?? 0),
            ShadowingMarkState.Recording => $"● {FormatTime(TimeSpan.FromMilliseconds(session.RecordingElapsedMs))}",
            ShadowingMarkState.Slicing => $"切原声 {session.SlicingDone}/{session.SlicingTotal}",
            _ => string.Empty
        };
    }

    private void ShowMarkHint(string message)
    {
        if (SessionActive)
        {
            _markHintUntil = DateTime.Now.AddSeconds(3);
            _labelMark.Visible = true;
            _labelMark.Text = message;
            _labelMark.ForeColor = PdfSidebarTheme.PaletteFor(this).Muted;
            _tooltip.SetToolTip(_labelMark, message);
            return;
        }

        // 会话结束后状态行已隐藏，用气泡提示保留结果信息
        _tooltip.Show(message, _btnMark, new Point(-60, -34), 4000);
    }

    private void StyleMarkButton()
    {
        _btnMark.Text = "⦿ 打点";
        _btnMark.Size = new Size(78, 28);
        _btnMark.FlatStyle = FlatStyle.Flat;
        _btnMark.FlatAppearance.BorderSize = 0;
        _btnMark.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold);
        _btnMark.Cursor = Cursors.Hand;
        _btnMark.Margin = new Padding(0);
        _btnMark.TabStop = false;
        _btnMark.UseVisualStyleBackColor = false;
    }

    private void UpdateVolumeButton()
    {
        bool muted = _manager.Playback.Player.Volume <= 0.01;
        _btnVolume.Text = muted ? "🔇" : "🔊";
        _tooltip.SetToolTip(_btnVolume, muted ? "取消静音" : "静音");
    }

    /// <summary>当前书级倍速；未打开书籍时按 1.0x 显示。</summary>
    private double CurrentSpeed => _manager.Profile?.PlaybackSpeed ?? 1.0;

    private void ApplyPlaybackSpeed(double speed)
    {
        _manager.SetPlaybackSpeed(speed);
        UpdateSpeedButton();
        SyncSpeedUi();
    }

    private void UpdateSpeedButton()
    {
        var palette = PdfSidebarTheme.PaletteFor(this);
        double speed = CurrentSpeed;
        _btnSpeed.Text = FormatSpeed(speed);
        bool changed = Math.Abs(speed - 1.0) > 0.001;
        _btnSpeed.BackColor = changed ? palette.BrandSoft : palette.SurfaceAlt;
        _btnSpeed.ForeColor = changed ? PdfSidebarTheme.Brand : palette.Text;
        _tooltip.SetToolTip(_btnSpeed, $"播放速度 {FormatSpeed(speed)}（右键恢复 1.0x）");
    }

    /// <summary>把书级倍速同步到面板的滑块与预设高亮。</summary>
    private void SyncSpeedUi()
    {
        var palette = PdfSidebarTheme.PaletteFor(this);
        double speed = CurrentSpeed;

        _syncingSpeed = true;
        _speedSlider.Value = Math.Clamp((int)Math.Round(speed * 100), _speedSlider.Minimum, _speedSlider.Maximum);
        _syncingSpeed = false;
        _speedValue.Text = FormatSpeed(speed);

        foreach (Button chip in _speedChips.Controls)
        {
            bool active = chip.Tag is double preset && Math.Abs(preset - speed) < 0.001;
            chip.BackColor = active ? palette.BrandSoft : palette.SurfaceAlt;
            chip.ForeColor = active ? PdfSidebarTheme.Brand : palette.Text;
            chip.FlatAppearance.MouseOverBackColor = palette.Hover;
            chip.FlatAppearance.MouseDownBackColor = palette.BrandSoft;
        }
    }

    private void OnSpeedSliderValueChanged(object? sender, EventArgs e)
    {
        if (_syncingSpeed) return;
        // 拖动中只刷新读数，真正换挡在松手时提交
        _speedValue.Text = FormatSpeed(SnapSpeed(_speedSlider.Value) / 100.0);
    }

    private void CommitSpeedSlider()
    {
        if (_syncingSpeed) return;
        int snapped = SnapSpeed(_speedSlider.Value);
        _syncingSpeed = true;
        _speedSlider.Value = snapped;
        _syncingSpeed = false;
        ApplyPlaybackSpeed(snapped / 100.0);
    }

    /// <summary>滑块最小步进 5，即 0.05x。</summary>
    private static int SnapSpeed(int value) => (int)Math.Round(value / 5.0) * 5;

    private static string FormatSpeed(double speed)
    {
        var text = speed.ToString("0.00", CultureInfo.InvariantCulture).TrimEnd('0');
        if (text.EndsWith('.')) text += "0";
        return text + "x";
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

        foreach (var button in new[] { _btnMarkAgain, _btnMarkEnd })
        {
            button.BackColor = palette.SurfaceAlt;
            button.ForeColor = palette.Text;
            button.FlatAppearance.MouseOverBackColor = palette.Hover;
            button.FlatAppearance.MouseDownBackColor = palette.BrandSoft;
        }

        if (_labelMark.BackColor != palette.Surface)
            _labelMark.BackColor = palette.Surface;
        _labelMark.ForeColor = PdfSidebarTheme.Brand;

        _loopMenu.BackColor = palette.Surface;
        _loopMenu.ForeColor = palette.Text;
        _speedMenu.BackColor = palette.Surface;
        _speedMenu.ForeColor = palette.Text;
        _speedPanel.BackColor = palette.Surface;
        _speedSlider.BackColor = palette.Surface;
        _speedTitle.ForeColor = palette.Text;
        _speedValue.ForeColor = palette.Muted;
        UpdateFollowButton();
        UpdateSpeedButton();
        UpdateLoopChecks();
        UpdateMarkButton();
        SyncSpeedUi();
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
            _markTimer.Stop();
            _markTimer.Dispose();
            _markSession?.Dispose();
            _tooltip.Dispose();
            _loopMenu.Dispose();
            _speedMenu.Dispose();
        }
        base.Dispose(disposing);
    }
}
