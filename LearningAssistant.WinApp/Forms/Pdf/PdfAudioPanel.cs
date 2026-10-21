using LearningAssistant.Abstractions.Media;
using LearningAssistant.Managers;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;

namespace LearningAssistant.Forms.Pdf;

public class PdfAudioPanel : UserControl
{
    private readonly PdfAudioTrackManager _manager;
    private readonly IPdfReaderFormAccess _form;
    private EventHandler<AudioRecording>? _recordingCompletedHandler;

    private readonly Label _labelInfo = new() { AutoSize = true, Text = "本页：无音频", ForeColor = Color.FromArgb(50, 70, 90) };
    private readonly ListBox _listTracks = new() { Dock = DockStyle.Fill, IntegralHeight = false };
    private readonly Button _btnPrev = new() { Text = "⏮", Width = 44 };
    private readonly Button _btnPlay = new() { Text = "▶", Width = 56 };
    private readonly Button _btnNext = new() { Text = "⏭", Width = 44 };
    private readonly Button _btnSetA = new() { Text = "设起点", Width = 62 };
    private readonly Button _btnSetB = new() { Text = "设终点", Width = 62 };
    private readonly Button _btnClearAb = new() { Text = "取消复读", Width = 74 };
    private readonly Label _labelLoop = new() { AutoSize = true, Text = "复读：未设置", ForeColor = Color.FromArgb(100, 116, 139) };
    private readonly ToolTip _tooltip = new();
    private readonly ComboBox _comboSpeed = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 84 };
    private readonly Label _labelSubtitle = new() { AutoSize = true, Text = "", ForeColor = Color.FromArgb(139, 92, 246), MaximumSize = new Size(300, 0) };
    private readonly Button _btnFollowRead = new() { Text = "🎤 跟读", Width = 76 };
    private readonly Button _btnRules = new() { Text = "规则", Width = 56 };
    private readonly Button _btnManual = new() { Text = "手动指定", Width = 76 };
    private readonly Button _btnManualFromList = new() { Text = "从列表选", Width = 76 };
    private readonly Button _btnRescan = new() { Text = "重新扫描", Width = 76 };
    private readonly CheckBox _chkSubDir = new() { Text = "包含子目录", AutoSize = true };

    public PdfAudioPanel(PdfAudioTrackManager manager, IPdfReaderFormAccess form)
    {
        _manager = manager;
        _form = form;
        BuildLayout();
        WireEvents();
        RefreshData();
    }

    private void BuildLayout()
    {
        _comboSpeed.Items.Add("1.0x（不变调）"); // 阶段一：倍速引擎未落地，UI 禁用
        _comboSpeed.SelectedIndex = 0;
        _comboSpeed.Enabled = false;

        var transport = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        transport.Controls.AddRange(new Control[] { _btnPrev, _btnPlay, _btnNext });

        var abRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        abRow.Controls.AddRange(new Control[] { _btnSetA, _btnSetB, _btnClearAb, _labelLoop });

        var speedRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        speedRow.Controls.Add(_comboSpeed);
        speedRow.Controls.Add(new Label { AutoSize = true, Text = "倍速待技术评估", ForeColor = Color.FromArgb(148, 163, 184) });

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
        bottom.Controls.AddRange(new Control[] { _btnFollowRead, _btnRules, _btnManual, _btnManualFromList, _btnRescan, _chkSubDir });

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7 };
        root.Controls.Add(_labelInfo, 0, 0);
        root.Controls.Add(_listTracks, 0, 1);
        root.Controls.Add(transport, 0, 2);
        root.Controls.Add(abRow, 0, 3);
        root.Controls.Add(speedRow, 0, 4);
        root.Controls.Add(_labelSubtitle, 0, 5);
        root.Controls.Add(bottom, 0, 6);
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        for (int i = 2; i < 7; i++) root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Controls.Add(root);
        Padding = new Padding(6);
    }

    private void WireEvents()
    {
        _btnPlay.Click += (_, _) => { if (!_manager.PlayOrPause()) _form.ShowWarning("当前页没有可播放的音频"); };
        _btnPrev.Click += (_, _) => _manager.Prev();
        _btnNext.Click += (_, _) => _manager.Next();
        _btnSetA.Click += (_, _) => { _manager.Playback.SetAbLoop(_manager.Playback.Player.Position, _manager.Playback.Player.AbLoop.B); UpdateLoopLabel(); };
        _btnSetB.Click += (_, _) => { _manager.Playback.SetAbLoop(_manager.Playback.Player.AbLoop.A, _manager.Playback.Player.Position); UpdateLoopLabel(); };
        _btnClearAb.Click += (_, _) => { _manager.Playback.SetAbLoop(null, null); _manager.Playback.Player.LoopMode = MediaLoopMode.None; UpdateLoopLabel(); };
        _tooltip.SetToolTip(_btnSetA, "把当前播放位置设为复读起点（A）");
        _tooltip.SetToolTip(_btnSetB, "把当前播放位置设为复读终点（B），播放到 B 后自动回到 A 循环");
        _tooltip.SetToolTip(_btnClearAb, "取消复读，恢复正常播放");
        _tooltip.SetToolTip(_btnPrev, "上一曲");
        _tooltip.SetToolTip(_btnPlay, "播放 / 暂停");
        _tooltip.SetToolTip(_btnNext, "下一曲");
        _btnFollowRead.Click += (_, _) => OnFollowRead();
        _btnRules.Click += (_, _) => OnRules();
        _btnManual.Click += (_, _) => OnManual();
        _btnManualFromList.Click += (_, _) => OnManualFromList();
        _btnRescan.Click += async (_, _) =>
        {
            _btnRescan.Enabled = false;
            try { await _manager.RescanAsync(); }
            catch (Exception ex) { HandleAudioError("重新扫描失败", ex); }
            finally { _btnRescan.Enabled = true; }
        };
        _chkSubDir.CheckedChanged += async (_, _) =>
        {
            _chkSubDir.Enabled = false;
            try { await _manager.SetIncludeSubDirectoriesAsync(_chkSubDir.Checked); }
            catch (Exception ex) { HandleAudioError("切换递归扫描失败", ex); }
            finally { _chkSubDir.Enabled = true; }
        };
        _listTracks.SelectedIndexChanged += (_, _) => OnTrackSelected();
        _manager.ProfileLoaded += OnProfileLoaded;

        _manager.Playback.Player.StateChanged += OnPlayerStateChanged;
        _manager.Playback.Player.PositionChanged += OnPlayerPositionChanged;

        _recordingCompletedHandler = (_, rec) => OnRecordingCompleted(rec);
        if (_form.AudioRecorderService != null)
            _form.AudioRecorderService.RecordingCompleted += _recordingCompletedHandler;
    }

    private void OnPlayerStateChanged(object? sender, MediaPlaybackState s)
        => InvokeIfReady(() => _btnPlay.Text = s == MediaPlaybackState.Playing ? "⏸" : "▶");

    private void OnPlayerPositionChanged(object? sender, TimeSpan p) => InvokeIfReady(() => UpdateSubtitle(p));

    public void RefreshData()
    {
        var result = _manager.Result;
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
                var pages = pagesByTrack.TryGetValue(t.FilePath, out var list) ? list.OrderBy(p => p).ToList() : new List<int>();
                var pageText = pages.Count > 0 ? string.Join(",", pages.Take(4)) + (pages.Count > 4 ? "…" : "") : "未匹配";
                _listTracks.Items.Add($"第{pageText}页  {t.FileName}{(t.IsManual ? "  [手动]" : "")}");
            }
            if (result.UnmatchedPages.Count > 0)
                _listTracks.Items.Add($"未匹配页：{string.Join(",", result.UnmatchedPages.Select(p => p + 1).Take(20))}");
        }
        _listTracks.EndUpdate();
        OnCurrentPageChanged();
    }

    /// <summary>翻页时只更新与当前页相关的信息，不重建曲目列表</summary>
    public void OnCurrentPageChanged()
    {
        UpdateInfoLabel();
        UpdateLoopLabel();
        UpdateSubtitle(_manager.Playback.Player.Position);
    }

    private void UpdateInfoLabel()
    {
        var cur = CurrentTrack();
        _labelInfo.Text = cur != null ? $"本页：{cur.FileName}" : "本页：无音频";
    }

    private AudioTrack? CurrentTrack()
    {
        if (_manager.Result == null) return null;
        return _manager.Result.PageIndex.TryGetValue(_form.CurrentPageIndex, out var t) ? t : null;
    }

    private void OnTrackSelected()
    {
        var result = _manager.Result;
        if (result == null || _listTracks.SelectedIndex < 0 || _listTracks.SelectedIndex >= result.Tracks.Count) return;
        var track = result.Tracks[_listTracks.SelectedIndex];
        _manager.Playback.Load(track);
        FireAndForget(_manager.Playback.PlayAsync());
    }

    private void OnFollowRead()
    {
        var recorder = _form.AudioRecorderService;
        if (recorder == null) { _form.ShowWarning("录音服务不可用"); return; }
        if (recorder.State == AudioRecorderState.Recording)
        {
            recorder.StopRecording();
            _btnFollowRead.Text = "🎤 跟读";
            return;
        }
        if (string.IsNullOrEmpty(_form.CurrentPdfPath)) { _form.ShowWarning("请先打开 PDF"); return; }
        _manager.Playback.Pause(); // 录音与播放互斥
        recorder.StartRecording(_form.CurrentPdfPath, _form.CurrentPageIndex, Path.GetFileName(_form.CurrentPdfPath));
        _btnFollowRead.Text = "⏹ 结束跟读";
    }

    private void OnRecordingCompleted(AudioRecording recording)
    {
        if (!string.IsNullOrEmpty(recording.FilePath))
            _manager.Playback.Load(new AudioTrack
            {
                FilePath = recording.FilePath,
                FileName = Path.GetFileName(recording.FilePath),
                RelativePath = Path.GetFileName(recording.FilePath)
            });
        InvokeIfReady(() => _btnFollowRead.Text = "🎤 跟读");
    }

    private void OnRules()
    {
        if (_manager.Profile == null) { _form.ShowWarning("尚未加载 profile"); return; }
        using var dlg = new PdfAudioRuleDialog(_manager.Profile, _form, _manager);
        if (dlg.ShowDialog(_form.Form) == DialogResult.OK)
            _manager.SaveRulesAndRematch();
    }

    private void OnManual()
    {
        if (string.IsNullOrEmpty(_form.CurrentPdfPath)) { _form.ShowWarning("请先打开 PDF"); return; }
        using var ofd = new OpenFileDialog
        {
            Title = "为当前页手动指定音频",
            Filter = "音频文件|*.mp3;*.wav;*.m4a;*.aac;*.flac;*.ogg;*.wma",
            InitialDirectory = Path.GetDirectoryName(_form.CurrentPdfPath)
        };
        if (ofd.ShowDialog(_form.Form) == DialogResult.OK)
        {
            _manager.SetManualOverride(_form.CurrentPageIndex, Path.GetFileName(ofd.FileName));
            RefreshData();
        }
    }

    private void OnManualFromList()
    {
        var result = _manager.Result;
        if (result == null || result.Tracks.Count == 0) { _form.ShowWarning("当前目录没有扫描到音频，请先重新扫描"); return; }

        using var dlg = new Form
        {
            Text = $"选择音频（第 {_form.CurrentPageIndex + 1} 页）",
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(440, 360),
            Font = Font,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false
        };
        var list = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
        foreach (var t in result.Tracks)
            list.Items.Add(t.RelativePath + (t.SubtitlePath != null ? "  [有字幕]" : ""));

        var btnOk = new Button { Text = "确定", Dock = DockStyle.Bottom, Height = 36, DialogResult = DialogResult.OK };
        dlg.Controls.Add(list);
        dlg.Controls.Add(btnOk);
        dlg.AcceptButton = btnOk;

        if (dlg.ShowDialog(_form.Form) == DialogResult.OK && list.SelectedIndex >= 0)
        {
            _manager.SetManualOverride(_form.CurrentPageIndex, result.Tracks[list.SelectedIndex].RelativePath);
            RefreshData();
        }
    }

    private void UpdateLoopLabel()
    {
        var (a, b) = _manager.Playback.Player.AbLoop;
        _labelLoop.Text = (a is not null && b is not null) ? $"复读 {a:mm\\:ss} ~ {b:mm\\:ss} 循环" : "复读：未设置";
    }

    private void UpdateSubtitle(TimeSpan position)
    {
        var cues = _manager.Playback.Player.SubtitleCues;
        if (cues == null || cues.Count == 0) { _labelSubtitle.Text = ""; return; }
        var cue = cues.LastOrDefault(c => c.StartTime <= position);
        _labelSubtitle.Text = cue?.Text ?? "";
    }

    private void OnProfileLoaded(object? sender, EventArgs e) => InvokeIfReady(RefreshData);

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
            _manager.Playback.Player.StateChanged -= OnPlayerStateChanged;
            _manager.Playback.Player.PositionChanged -= OnPlayerPositionChanged;
            _manager.ProfileLoaded -= OnProfileLoaded;
            if (_recordingCompletedHandler != null && _form.AudioRecorderService != null)
                _form.AudioRecorderService.RecordingCompleted -= _recordingCompletedHandler;
        }
        base.Dispose(disposing);
    }
}