using LearningAssistant.Abstractions.Media;
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
    private EventHandler<AudioRecording>? _recordingCompletedHandler;
    private bool _suppressAutoPlay;
    private Font? _listFont;

    /// <summary>供 Visual Studio 设计器实例化；运行时请使用带依赖的构造函数。</summary>
    public PdfAudioPanel()
    {
        InitializeComponent();
        _manager = null!;
        _form = null!;
        _aiMatcher = null;
        // 设计期也按当前字体回填一次，保证设计视图与运行预览一致
        ApplyRuntimeMetrics();
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

    /// <summary>按当前字体回填设计期占位尺寸，保持与动态布局一致的视觉效果。</summary>
    private void ApplyRuntimeMetrics()
    {
        _listFont ??= new Font("Microsoft YaHei UI", 9.5F);
        _listTracks.ItemHeight = Math.Max(28, _listFont.Height + 12);

        int baseFontHeight = Control.DefaultFont.Height;
        int buttonHeight = Math.Max(30, baseFontHeight + 14);
        int iconSize = Math.Max(34, baseFontHeight + 18);
        int comboHeight = Math.Max(26, _comboSpeed.Font.Height + 10);

        _labelInfo.Height = Math.Max(24, _labelInfo.Font.Height + 8);
        int statusHeight = _labelInfo.Height + 14;
        int playbackHeight = 40 + (iconSize + 6) + (buttonHeight + 6) + 22 + (comboHeight + 6) + 24;
        int sourceHeight = 40 + 4 * (buttonHeight + 6) + 4;

        root.RowStyles[0].Height = statusHeight + 4;
        root.RowStyles[2].Height = playbackHeight + 4;
        root.RowStyles[3].Height = sourceHeight;

        playback.RowStyles[0].Height = iconSize + 6;
        playback.RowStyles[1].Height = buttonHeight + 6;
        playback.RowStyles[2].Height = 22F;
        playback.RowStyles[3].Height = comboHeight + 6;

        for (int i = 0; i < sourceGrid.RowStyles.Count; i++)
            sourceGrid.RowStyles[i].Height = buttonHeight + 6;

        StyleTransportButton(_btnPrev, iconSize, iconSize, primary: false);
        StyleTransportButton(_btnPlay, iconSize + 12, iconSize, primary: true);
        StyleTransportButton(_btnNext, iconSize, iconSize, primary: false);
    }

    /// <summary>播放区图标按钮：播放/暂停为主按钮，前后曲为次级按钮。</summary>
    private static void StyleTransportButton(Button button, int width, int height, bool primary)
    {
        button.AutoSize = false;
        button.Size = new Size(width, height);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = PdfSidebarTheme.Border;
        button.BackColor = primary ? PdfSidebarTheme.Brand : PdfSidebarTheme.Surface;
        button.ForeColor = primary ? Color.White : PdfSidebarTheme.Text;
        button.Font = new Font("Microsoft YaHei UI", primary ? 11F : 10F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
        button.Margin = new Padding(3, 0, 3, 0);
        button.Tag = primary ? "accent" : "transport";
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
        _labelInfo.ForeColor = palette.Text;
        _labelSubtitle.ForeColor = PdfSidebarTheme.Score;
        _listTracks.BackColor = palette.Surface;
        _listTracks.ForeColor = palette.Text;
        _comboSpeed.BackColor = palette.Surface;
        _comboSpeed.ForeColor = palette.Text;
        _listTracks.Invalidate();
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
        _btnAddDir.Click += async (_, _) => await OnAddDirectoryAsync();
        _btnAiMatch.Click += async (_, _) => await OnAiMatchAsync();
        _tooltip.SetToolTip(_btnAddDir, "选择一个外部音频目录，扫描后合并到当前曲目列表");
        _tooltip.SetToolTip(_btnAiMatch, "让 AI 根据 PDF 各页内容与音频文件名自动匹配页码，确认后应用");
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
        _listTracks.DrawItem += ListTracks_DrawItem;
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
                // 外部目录曲目用目录名辅助区分同名文件
                var folderHint = Path.IsPathRooted(t.RelativePath)
                    ? $"  ({Path.GetFileName(Path.GetDirectoryName(t.FilePath))})"
                    : "";
                _listTracks.Items.Add($"第{pageText}页  {t.FileName}{folderHint}{(t.IsManual ? "  [手动]" : "")}");
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
        if (_suppressAutoPlay) return;
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
            _btnFollowRead.Text = "● 整页录音";
            return;
        }
        if (string.IsNullOrEmpty(_form.CurrentPdfPath)) { _form.ShowWarning("请先打开 PDF"); return; }
        _manager.Playback.Pause(); // 录音与播放互斥
        recorder.StartRecording(_form.CurrentPdfPath, _form.CurrentPageIndex, Path.GetFileName(_form.CurrentPdfPath));
        _btnFollowRead.Text = "■ 结束录音";
    }

    private void OnRecordingCompleted(AudioRecording recording)
    {
        // 句子跟读录音由跟读窗口消费，不能污染整页音频与录音列表
        if (recording.Scope != AudioRecordingScope.Page)
            return;

        if (!string.IsNullOrEmpty(recording.FilePath))
            _manager.Playback.Load(new AudioTrack
            {
                FilePath = recording.FilePath,
                FileName = Path.GetFileName(recording.FilePath),
                RelativePath = Path.GetFileName(recording.FilePath)
            });
        InvokeIfReady(() => _btnFollowRead.Text = "● 整页录音");
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
            // 目录内文件存相对路径，目录外保留绝对路径，避免解析到错误的同名文件
            _manager.SetManualOverride(_form.CurrentPageIndex, ToProfilePath(ofd.FileName));
            RefreshData();
            var resolved = _manager.Result?.PageIndex.GetValueOrDefault(_form.CurrentPageIndex);
            if (resolved != null) SelectTrackInList(resolved);
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
            var resolved = _manager.Result?.PageIndex.GetValueOrDefault(_form.CurrentPageIndex);
            if (resolved != null) SelectTrackInList(resolved);
        }
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

        _btnAddDir.Enabled = false;
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
            _btnAddDir.Enabled = true;
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

        _btnAiMatch.Enabled = false;
        _btnAddDir.Enabled = false;
        var originalText = _btnAiMatch.Text;
        UseWaitCursor = true;
        try
        {
            var progress = new Progress<string>(message => InvokeIfReady(() => _labelInfo.Text = message));
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
            _btnAiMatch.Text = originalText;
            _btnAiMatch.Enabled = true;
            _btnAddDir.Enabled = true;
            UseWaitCursor = false;
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
            if (_manager != null)
            {
                _manager.Playback.Player.StateChanged -= OnPlayerStateChanged;
                _manager.Playback.Player.PositionChanged -= OnPlayerPositionChanged;
                _manager.ProfileLoaded -= OnProfileLoaded;
            }
            if (_recordingCompletedHandler != null && _form != null && _form.AudioRecorderService != null)
                _form.AudioRecorderService.RecordingCompleted -= _recordingCompletedHandler;
            _listFont?.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }
}
