using LearningAssistant.Common;
using LearningAssistant.Common.UI;
using LearningAssistant.Forms.Pdf;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System.Security.Cryptography;
using System.Text;

namespace LearningAssistant.Managers
{
    public class PdfReaderAudioManager : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IPdfReaderFormAccess _form;
        private readonly IAudioRecorderService _recorder;
        private readonly IPdfAiService? _pdfAiService;
        private readonly IPdfAiSummaryStore? _summaryStore;
        private readonly IAnnotationService? _annotationService;
        private bool _disposed;

        private Control? _host;

        /// <summary>录音 UI 的挂载宿主（默认挂到「音频」Tab；抽屉模式下指向录音内容面板）</summary>
        public Control? MountHost { get; set; }
        private TableLayoutPanel? _layout;
        private PdfRecordingPanel? _panel;
        private bool _timestampsExpanded;
        private bool _summariesExpanded;
        private Button _buttonRecord;
        private Button _buttonStop;
        private Button _buttonPlay;
        private Button _buttonDelete;
        private Button _buttonRename;
        private Label _labelListTitle;
        private Label _labelTime;
        private TrackBar _trackBarPosition;
        private Panel _levelMeter;
        private Panel _levelFill;
        private ListBox _listRecordings;
        private Label _labelStatus;
        private Label _labelNoRecordings;
        private Label _labelTimestamps;
        private Label _labelNoTimestamps;
        private ListBox _listTimestamps;
        private Label _labelSummaries;
        private Label _labelNoSummaries;
        private ListBox _listSummaries;
        private Button _buttonGenerateSummary;
        private List<PdfAiSummaryEntry> _summaryEntries = new();
        private int _lastTimestampPage = -1;
        private System.Windows.Forms.Timer _uiTimer;
        private System.Windows.Forms.Timer _levelTimer;

        private List<AudioRecording> _recordings = new();
        private string? _currentPdfPath;
        private WaveOutEvent? _playbackOut;
        private AudioFileReader? _playbackReader;
        private bool _isPlaying;
        private bool _isDraggingPosition;
        private DateTime _lastLevelAt = DateTime.MinValue;

        private static Color _accentColor = Color.FromArgb(25, 118, 210);
        private static Color _recordRed = Color.FromArgb(244, 67, 54);
        private static Color _bgColor = Color.FromArgb(248, 248, 252);
        private static Color _textColor = Color.FromArgb(50, 50, 60);

        public PdfReaderAudioManager(ILogger logger, IPdfReaderFormAccess form, IAudioRecorderService recorder,
            IPdfAiService? pdfAiService = null, IPdfAiSummaryStore? summaryStore = null,
            IAnnotationService? annotationService = null)
        {
            _logger = logger;
            _form = form;
            _recorder = recorder;
            _pdfAiService = pdfAiService;
            _summaryStore = summaryStore;
            _annotationService = annotationService;
            _recorder.RecordingCompleted += OnRecordingCompleted;
            _recorder.PositionChanged += OnPositionChanged;
            _recorder.AudioLevelChanged += OnAudioLevelChanged;
        }

        public void Initialize()
        {
            _host = MountHost ?? _form.TabPageAudio;
            if (_host == null) return;

            CreateControls();
            UpdateUIState();
            _currentPdfPath = _form.CurrentPdfPath;
            LoadRecordings();
        }

        private void CreateControls()
        {
            if (_host == null) return;

            _panel = new PdfRecordingPanel { Dock = DockStyle.Fill };
            _host.Controls.Add(_panel);

            BindPanelControls();
            ApplySectionState();

            _uiTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _uiTimer.Tick += UiTimer_Tick;
            _uiTimer.Start();

            _levelTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _levelTimer.Tick += LevelTimer_Tick;
            _levelTimer.Start();
        }

        /// <summary>把面板控件接入录音逻辑，事件绑定与运行时构建版本保持一致。</summary>
        private void BindPanelControls()
        {
            if (_panel == null) return;

            _layout = _panel.Root;
            _labelStatus = _panel.StatusLabel;

            _buttonRecord = _panel.RecordButton;
            _buttonRecord.Click += _buttonRecord_Click;
            _buttonStop = _panel.StopButton;
            _buttonStop.Click += _buttonStop_Click;
            _buttonPlay = _panel.PlayButton;
            _buttonPlay.Click += _buttonPlay_Click;

            _labelTime = _panel.TimeLabel;

            _trackBarPosition = _panel.PositionBar;
            _trackBarPosition.MouseDown += (_, _) => _isDraggingPosition = true;
            _trackBarPosition.MouseUp += TrackBarPosition_MouseUp;

            _levelMeter = _panel.LevelMeter;
            _levelFill = _panel.LevelFill;

            _labelNoRecordings = _panel.NoRecordingsLabel;
            _listRecordings = _panel.RecordingsList;
            _listRecordings.DrawItem += ListRecordings_DrawItem;
            _listRecordings.SelectedIndexChanged += ListRecordings_SelectedIndexChanged;
            _listRecordings.MouseDoubleClick += ListRecordings_MouseDoubleClick;

            _buttonDelete = _panel.DeleteButton;
            _buttonDelete.Click += _buttonDelete_Click;
            _buttonRename = _panel.RenameButton;
            _buttonRename.Click += _buttonRename_Click;
            _buttonRename.Enabled = false;
            _labelListTitle = _panel.ListTitle;
            _buttonGenerateSummary = _panel.GenerateSummaryButton;
            _buttonGenerateSummary.Click += ButtonGenerateSummary_Click;

            _labelTimestamps = _panel.TimestampsTitle;
            _labelTimestamps.Click += (_, _) =>
            {
                _timestampsExpanded = !_timestampsExpanded;
                ApplySectionState();
            };
            _labelNoTimestamps = _panel.NoTimestampsLabel;
            _listTimestamps = _panel.TimestampsList;
            _listTimestamps.DrawItem += ListTimestamps_DrawItem;
            _listTimestamps.MouseClick += ListTimestamps_MouseClick;

            _labelSummaries = _panel.SummariesTitle;
            _labelSummaries.Click += (_, _) =>
            {
                _summariesExpanded = !_summariesExpanded;
                ApplySectionState();
            };
            _labelNoSummaries = _panel.NoSummariesLabel;
            _listSummaries = _panel.SummariesList;
            _listSummaries.DrawItem += ListSummaries_DrawItem;
            _listSummaries.MouseClick += ListSummaries_MouseClick;
        }

        /// <summary>同步两个折叠区块的行高、可见性与标题计数。</summary>
        private void ApplySectionState()
        {
            if (_layout == null) return;

            ApplySectionRow(8, 9, _labelTimestamps, _listTimestamps, _labelNoTimestamps,
                _timestampsExpanded, "本页时间戳标注");
            ApplySectionRow(10, 11, _labelSummaries, _listSummaries, _labelNoSummaries,
                _summariesExpanded, "AI 摘要");
            _layout.PerformLayout();
        }

        private void ApplySectionRow(int titleRow, int listRow, Label title, ListBox list,
            Label empty, bool expanded, string name)
        {
            if (_layout == null) return;

            _layout.RowStyles[titleRow] = new RowStyle(SizeType.Absolute, 24F);
            _layout.RowStyles[listRow] = expanded
                ? new RowStyle(SizeType.Percent, 30F)
                : new RowStyle(SizeType.Absolute, 0F);

            bool hasItems = list.Items.Count > 0;
            list.Visible = expanded && hasItems;
            empty.Visible = expanded && !hasItems;
            title.Text = $"{(expanded ? "▾" : "▸")} {name}（{list.Items.Count}）";
        }

        private void _buttonRecord_Click(object? sender, EventArgs e)
        {
            if (_recorder.State == AudioRecorderState.Recording)
            {
                _recorder.StopRecording();
                return;
            }

            var pdfPath = _form.CurrentPdfPath;
            if (string.IsNullOrEmpty(pdfPath))
            {
                _form.ShowWarning("请先打开一个 PDF 文件");
                return;
            }

            var fileName = Path.GetFileName(pdfPath);
            _recorder.StartRecording(pdfPath, _form.CurrentPageIndex, fileName);
            _labelStatus.Text = "● 录制中...";
            _labelStatus.ForeColor = _recordRed;
            UpdateUIState();
        }

        private void _buttonStop_Click(object? sender, EventArgs e)
        {
            if (_recorder.State == AudioRecorderState.Recording)
                _recorder.StopRecording();
        }

        private void _buttonPlay_Click(object? sender, EventArgs e)
        {
            if (_isPlaying)
            {
                StopPlayback();
                return;
            }

            if (_listRecordings.SelectedItem is AudioRecording selected)
                StartPlayback(selected);
        }

        private void _buttonDelete_Click(object? sender, EventArgs e)
        {
            if (_listRecordings.SelectedItem is AudioRecording selected)
            {
                string label = string.IsNullOrEmpty(selected.Label)
                    ? $"录音 {selected.CreatedAt:HH:mm:ss}"
                    : selected.Label;
                if (!_form.ShowConfirm($"确定删除「{label}」？删除后无法恢复。", "删除录音"))
                    return;

                if (_isPlaying && _playbackReader != null &&
                    _playbackReader.FileName == selected.FilePath)
                    StopPlayback();

                try
                {
                    if (File.Exists(selected.FilePath))
                        File.Delete(selected.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete recording file: {Path}", selected.FilePath);
                }

                _recordings.Remove(selected);
                SaveRecordings();
                RefreshRecordingList();
                _form.ShowMessage("录音已删除", "删除成功");
            }
        }

        /// <summary>重命名选中的录音，名称随录音清单持久化。</summary>
        private void _buttonRename_Click(object? sender, EventArgs e)
        {
            if (_listRecordings.SelectedItem is not AudioRecording selected)
            {
                _form.ShowWarning("请先选择一条录音");
                return;
            }

            string current = string.IsNullOrEmpty(selected.Label)
                ? $"录音 {selected.CreatedAt:HH:mm:ss}"
                : selected.Label;
            using var dialog = new LearningAssistant.Platform.InputDialog("重命名录音", current);
            dialog.StartPosition = FormStartPosition.CenterParent;
            if (dialog.ShowDialog(_form.Form) != DialogResult.OK)
                return;

            string newLabel = dialog.Value?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(newLabel))
            {
                _form.ShowWarning("录音名称不能为空");
                return;
            }

            selected.Label = newLabel;
            SaveRecordings();
            RefreshRecordingList();
            _labelStatus.Text = "已重命名";
            _labelStatus.ForeColor = _textColor;
        }

        private void OnRecordingCompleted(object? sender, AudioRecording recording)
        {
            // 只处理整页跟读录音，句子跟读录音由独立跟读窗口接管
            if (recording.Scope != AudioRecordingScope.Page)
                return;

            if (_form.Form.InvokeRequired)
            {
                _form.Form.Invoke(() => OnRecordingCompleted(sender, recording));
                return;
            }

            _recordings.Add(recording);
            SaveRecordings();
            RefreshRecordingList();
            // 新录音完成后重新解析时间戳标注对应的录音
            RefreshTimestampList();
            _labelStatus.Text = "录制完成";
            _labelStatus.ForeColor = _textColor;
            UpdateUIState();
            _trackBarPosition.Value = 0;
            _levelFill.Width = 0;
        }

        private void OnPositionChanged(object? sender, TimeSpan position)
        {
            if (_form.Form.InvokeRequired)
            {
                _form.Form.Invoke(() => OnPositionChanged(sender, position));
                return;
            }

            var totalMs = _recorder.GetCurrentRecording()?.DurationMs ?? 0;
            if (totalMs > 0 && !_isDraggingPosition)
            {
                _trackBarPosition.Value = Math.Min((int)(position.TotalMilliseconds / totalMs * 1000), 1000);
            }
            _labelTime.Text = $"{FormatTime(position)} / {FormatTime(TimeSpan.FromMilliseconds(totalMs))}";
        }

        private void TrackBarPosition_MouseUp(object? sender, MouseEventArgs e)
        {
            _isDraggingPosition = false;
            if (_recorder.State == AudioRecorderState.Idle && _playbackReader != null)
            {
                var pos = (int)(_trackBarPosition.Value / 1000.0 * _playbackReader.TotalTime.TotalMilliseconds);
                _playbackReader.CurrentTime = TimeSpan.FromMilliseconds(pos);
            }
        }

        private void UiTimer_Tick(object? sender, EventArgs e)
        {
            if (_form.CurrentPdfPath != _currentPdfPath)
            {
                _currentPdfPath = _form.CurrentPdfPath;
                LoadRecordings();
            }
            else if (_form.CurrentPageIndex != _lastTimestampPage)
            {
                // 翻页后刷新当前页的时间戳标注列表
                RefreshTimestampList();
            }

            if (_isPlaying && _playbackReader != null && !_isDraggingPosition)
            {
                var pos = _playbackReader.CurrentTime;
                var total = _playbackReader.TotalTime;
                _labelTime.Text = $"{FormatTime(pos)} / {FormatTime(total)}";
                if (total.TotalMilliseconds > 0)
                    _trackBarPosition.Value = Math.Min((int)(pos.TotalMilliseconds / total.TotalMilliseconds * 1000), 1000);
            }
        }

        private void LevelTimer_Tick(object? sender, EventArgs e)
        {
            // 停止录制后清空电平条
            if (_recorder.State != AudioRecorderState.Recording)
            {
                if (_levelFill.Width != 0)
                    _levelFill.Width = 0;
                return;
            }

            // 麦克风静音或设备停止送数据时让电平条缓慢回落，避免停留在旧峰值
            if ((DateTime.UtcNow - _lastLevelAt).TotalMilliseconds >= 250)
            {
                var step = Math.Max(1, _levelMeter.ClientSize.Width / 20);
                _levelFill.Width = Math.Max(0, _levelFill.Width - step);
            }
        }

        /// <summary>录音电平回调在采集线程触发，切到 UI 线程后按真实 RMS 更新电平条</summary>
        private void OnAudioLevelChanged(object? sender, float level)
        {
            var form = _form.Form;
            if (form.IsDisposed || !form.IsHandleCreated) return;

            try
            {
                if (form.InvokeRequired)
                {
                    form.BeginInvoke(() => ApplyAudioLevel(level));
                    return;
                }
            }
            catch (InvalidOperationException)
            {
                return;
            }

            ApplyAudioLevel(level);
        }

        private void ApplyAudioLevel(float level)
        {
            if (_levelFill.IsDisposed || _recorder.State != AudioRecorderState.Recording) return;

            _lastLevelAt = DateTime.UtcNow;
            var width = _levelMeter.ClientSize.Width;
            _levelFill.Width = (int)(width * Math.Clamp(level, 0f, 1f));
            // 接近满幅时提示过载，避免持续爆音却不自知
            _levelFill.BackColor = level >= 0.95f ? _recordRed : _accentColor;
        }

        private void StartPlayback(AudioRecording recording)
        {
            try
            {
                if (!File.Exists(recording.FilePath))
                {
                    _form.ShowWarning("录音文件不存在");
                    return;
                }

                StopPlayback();
                _playbackReader = new AudioFileReader(recording.FilePath);
                _playbackOut = new WaveOutEvent();
                _playbackOut.PlaybackStopped += OnPlaybackStopped;
                _playbackOut.Init(_playbackReader);
                _playbackOut.Play();
                _isPlaying = true;

                _buttonPlay.Text = "■ 停止";
                _buttonPlay.BackColor = _recordRed;
                _trackBarPosition.Enabled = true;
                _labelStatus.Text = "▶ 播放中...";
                _labelStatus.ForeColor = _accentColor;
                _labelTime.Text = $"00:00 / {FormatTime(_playbackReader.TotalTime)}";
                _trackBarPosition.Value = 0;
                _trackBarPosition.Maximum = 1000;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start playback");
                _form.ShowWarning("无法播放录音");
            }
        }

        private void StopPlayback()
        {
            if (_playbackOut != null)
            {
                // 先退订再停止，避免 Stop 异步触发的 PlaybackStopped 干扰后续新播放状态
                _playbackOut.PlaybackStopped -= OnPlaybackStopped;
                _playbackOut.Stop();
                _playbackOut.Dispose();
                _playbackOut = null;
            }
            _playbackReader?.Dispose();
            _playbackReader = null;
            _isPlaying = false;

            _buttonPlay.Text = "▶ 播放";
            _buttonPlay.BackColor = _accentColor;
            _trackBarPosition.Enabled = false;
            _labelStatus.Text = "就绪";
            _labelStatus.ForeColor = _textColor;
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            if (_form.Form.InvokeRequired)
            {
                _form.Form.Invoke(() => OnPlaybackStopped(sender, e));
                return;
            }
            _isPlaying = false;
            _buttonPlay.Text = "▶ 播放";
            _buttonPlay.BackColor = _accentColor;
            _trackBarPosition.Enabled = false;
            _labelStatus.Text = "播放完成";
            _labelStatus.ForeColor = _textColor;
        }

        /// <summary>跳转到指定录音的指定时间位置</summary>
        public void SeekToPosition(AudioRecording recording, long timestampMs)
        {
            if (_playbackReader != null && _playbackReader.FileName == recording.FilePath)
            {
                _playbackReader.CurrentTime = TimeSpan.FromMilliseconds(timestampMs);
                return;
            }

            StartPlayback(recording);
            if (_playbackReader != null)
                _playbackReader.CurrentTime = TimeSpan.FromMilliseconds(timestampMs);
        }

        /// <summary>获取当前音频时间戳(毫秒)，录制时标注创建调用</summary>
        public long? GetCurrentTimestampMs()
        {
            if (_recorder.State == AudioRecorderState.Recording)
                return _recorder.CurrentPositionMs;
            return null;
        }

        /// <summary>获取当前录音 Id，未在录制时返回 null</summary>
        public string? GetCurrentRecordingId()
        {
            if (_recorder.State == AudioRecorderState.Recording)
                return _recorder.GetCurrentRecording()?.Id;
            return null;
        }

        /// <summary>获取当前 PDF 的录音列表</summary>
        public IReadOnlyList<AudioRecording> GetRecordings()
        {
            return _recordings.AsReadOnly();
        }

        #region 时间戳标注列表

        /// <summary>时间戳标注列表项：展示文本 + 对应录音与音频位置</summary>
        private sealed class TimestampEntry
        {
            public string Display { get; init; } = string.Empty;
            public AudioRecording? Recording { get; init; }
            public long TimestampMs { get; init; }
        }

        /// <summary>刷新当前页的时间戳标注列表（仅显示录制期间创建的标注）</summary>
        public void RefreshTimestampList()
        {
            if (_listTimestamps == null || _labelNoTimestamps == null || _host == null) return;
            if (_host.IsDisposed) return;

            if (_form.Form.InvokeRequired)
            {
                try { _form.Form.Invoke(() => RefreshTimestampList()); } catch { }
                return;
            }

            var pdfPath = _form.CurrentPdfPath;
            var pageIndex = _form.CurrentPageIndex;
            _lastTimestampPage = pageIndex;

            var entries = new List<TimestampEntry>();
            var presenter = _form.Presenter;
            if (!string.IsNullOrEmpty(pdfPath) && presenter != null)
            {
                try
                {
                    foreach (var text in presenter.GetCurrentPageTextsForPage(pageIndex))
                    {
                        if (!text.AudioTimestampMs.HasValue) continue;
                        entries.Add(new TimestampEntry
                        {
                            Display = $"文字：{SummarizeText(text.Content)}  ·  {FormatTime(TimeSpan.FromMilliseconds(text.AudioTimestampMs.Value))}",
                            Recording = ResolveRecording(text.AudioRecordingId, pageIndex),
                            TimestampMs = text.AudioTimestampMs.Value
                        });
                    }

                    foreach (var stroke in presenter.GetCurrentPageStrokesForPage(pageIndex))
                    {
                        if (!stroke.AudioTimestampMs.HasValue) continue;
                        entries.Add(new TimestampEntry
                        {
                            Display = $"{DescribeShape(stroke.ShapeType)}  ·  {FormatTime(TimeSpan.FromMilliseconds(stroke.AudioTimestampMs.Value))}",
                            Recording = ResolveRecording(stroke.AudioRecordingId, pageIndex),
                            TimestampMs = stroke.AudioTimestampMs.Value
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to build timestamp annotation list");
                }
            }

            var ordered = entries.OrderBy(x => x.TimestampMs).ToList();
            _listTimestamps.BeginUpdate();
            _listTimestamps.Items.Clear();
            foreach (var item in ordered)
                _listTimestamps.Items.Add(item);
            _listTimestamps.EndUpdate();

            var hasItems = ordered.Count > 0;
            _listTimestamps.Visible = hasItems;
            _labelNoTimestamps.Visible = !hasItems;
            ApplySectionState();
        }

        /// <summary>优先按录音 Id 精确匹配；旧数据无 Id 时回退到同页最近一次录音</summary>
        private AudioRecording? ResolveRecording(string? recordingId, int pageIndex)
        {
            if (!string.IsNullOrEmpty(recordingId))
            {
                var byId = _recordings.FirstOrDefault(r => r.Id == recordingId);
                if (byId != null) return byId;
            }

            return _recordings
                .Where(r => r.PageIndex == pageIndex)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault();
        }

        private void ListTimestamps_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _listTimestamps.Items.Count) return;
            e.DrawBackground();

            if (_listTimestamps.Items[e.Index] is not TimestampEntry entry) return;

            var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var palette = PdfSidebarTheme.PaletteFor(_listTimestamps);
            var bgColor = isSelected ? palette.BrandSoft : (e.Index % 2 == 0 ? palette.Surface : palette.SurfaceAlt);
            using var bgBrush = new SolidBrush(bgColor);
            e.Graphics.FillRectangle(bgBrush, e.Bounds);

            using var font = new Font("Microsoft YaHei UI", 9F);
            using var textBrush = new SolidBrush(isSelected ? PdfSidebarTheme.Brand : palette.Text);
            var rect = new Rectangle(e.Bounds.X + 6, e.Bounds.Y + 7, Math.Max(10, e.Bounds.Width - 12), e.Bounds.Height - 10);
            var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            e.Graphics.DrawString(entry.Display, font, textBrush, rect, format);

            e.DrawFocusRectangle();
        }

        private void ListTimestamps_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            int index = _listTimestamps.IndexFromPoint(e.Location);
            if (index < 0 || index >= _listTimestamps.Items.Count) return;
            if (_listTimestamps.Items[index] is not TimestampEntry entry) return;

            if (entry.Recording == null)
            {
                _form.ShowWarning("未找到该标注对应的录音");
                return;
            }

            // 同步选中录音列表项，保证播放按钮状态与当前跳转的录音一致
            _listRecordings.SelectedItem = entry.Recording;
            SeekToPosition(entry.Recording, entry.TimestampMs);
        }

        /// <summary>刷新全书 AI 摘要列表（可按页跳转并从时间戳回放）</summary>
        public void RefreshSummaryList()
        {
            if (_listSummaries == null || _labelNoSummaries == null || _host == null) return;
            if (_host.IsDisposed) return;

            if (_form.Form.InvokeRequired)
            {
                try { _form.Form.Invoke(() => RefreshSummaryList()); } catch { }
                return;
            }

            var pdfPath = _form.CurrentPdfPath;
            _summaryEntries = string.IsNullOrEmpty(pdfPath) || _summaryStore == null
                ? new List<PdfAiSummaryEntry>()
                : _summaryStore.GetEntries(pdfPath)
                    .OrderBy(e => e.PageIndex)
                    .ThenBy(e => e.TimestampMs ?? long.MaxValue)
                    .ToList();

            _listSummaries.BeginUpdate();
            _listSummaries.Items.Clear();
            foreach (var entry in _summaryEntries)
                _listSummaries.Items.Add(entry);
            _listSummaries.EndUpdate();

            var hasItems = _summaryEntries.Count > 0;
            _listSummaries.Visible = hasItems;
            _labelNoSummaries.Visible = !hasItems;
            ApplySectionState();
        }

        private void ListSummaries_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _listSummaries.Items.Count) return;
            e.DrawBackground();

            if (_listSummaries.Items[e.Index] is not PdfAiSummaryEntry entry) return;

            var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var palette = PdfSidebarTheme.PaletteFor(_listSummaries);
            var bgColor = isSelected ? palette.BrandSoft : (e.Index % 2 == 0 ? palette.Surface : palette.SurfaceAlt);
            using var bgBrush = new SolidBrush(bgColor);
            e.Graphics.FillRectangle(bgBrush, e.Bounds);

            var anchorText = entry.TimestampMs.HasValue
                ? $"P{entry.PageIndex + 1} · {FormatTime(TimeSpan.FromMilliseconds(entry.TimestampMs.Value))}"
                : $"P{entry.PageIndex + 1} · 无时间锚点";

            using var anchorFont = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold);
            using var textFont = new Font("Microsoft YaHei UI", 9F);
            using var anchorBrush = new SolidBrush(isSelected ? _accentColor : Color.FromArgb(120, 120, 130));
            using var textBrush = new SolidBrush(palette.Text);

            var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            e.Graphics.DrawString(anchorText, anchorFont, anchorBrush,
                new RectangleF(e.Bounds.X + 6, e.Bounds.Y + 4, e.Bounds.Width - 12, 16), format);
            e.Graphics.DrawString(SummarizeText(entry.Text, 32), textFont, textBrush,
                new RectangleF(e.Bounds.X + 6, e.Bounds.Y + 22, e.Bounds.Width - 12, 18), format);

            e.DrawFocusRectangle();
        }

        private async void ListSummaries_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            int index = _listSummaries.IndexFromPoint(e.Location);
            if (index < 0 || index >= _listSummaries.Items.Count) return;
            if (_listSummaries.Items[index] is not PdfAiSummaryEntry entry) return;

            try
            {
                var presenter = _form.Presenter;
                if (presenter != null && entry.PageIndex >= 0 && entry.PageIndex < presenter.PageCount
                    && entry.PageIndex != _form.CurrentPageIndex)
                {
                    await presenter.RenderPage(entry.PageIndex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to navigate to summary page");
            }

            var recording = ResolveRecording(entry.RecordingId, entry.PageIndex);
            if (recording == null)
            {
                _form.ShowWarning("未找到该摘要对应的录音");
                return;
            }

            _listRecordings.SelectedItem = recording;
            SeekToPosition(recording, entry.TimestampMs ?? 0);
        }

        private async void ButtonGenerateSummary_Click(object? sender, EventArgs e)
        {
            if (_summaryStore == null || _annotationService == null)
            {
                _form.ShowWarning("AI 摘要服务未初始化");
                return;
            }
            if (_pdfAiService == null)
            {
                _form.ShowWarning("AI 服务不可用，请先完成 AI 配置");
                return;
            }

            var pdfPath = _form.CurrentPdfPath;
            if (string.IsNullOrEmpty(pdfPath))
            {
                _form.ShowWarning("请先打开一个 PDF 文件");
                return;
            }

            var presenter = _form.Presenter;
            int pageCount = presenter?.PageCount ?? 0;
            if (pageCount <= 0)
            {
                _form.ShowWarning("文档页数不可用");
                return;
            }

            _buttonGenerateSummary.Enabled = false;
            var previousCursor = _form.Form.Cursor;
            _form.Form.Cursor = Cursors.WaitCursor;
            try
            {
                var annotationService = _annotationService;
                var items = await Task.Run(() => CollectDocumentAnnotations(annotationService, pdfPath, pageCount));
                if (items.Count == 0)
                {
                    _form.ShowWarning("当前文档还没有可整理的标注");
                    return;
                }

                var result = await _pdfAiService.GenerateSummaryEntriesAsync(pdfPath, items);
                if (!result.Success)
                {
                    _form.ShowWarning(result.ErrorMessage ?? "AI 摘要生成失败");
                    return;
                }

                _summaryStore.AddEntries(pdfPath, result.Entries);
                RefreshSummaryList();
                _form.ShowMessage($"已生成 {result.Entries.Count} 条 AI 摘要", "AI 摘要");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate AI summary entries");
                _form.ShowWarning("AI 摘要生成失败");
            }
            finally
            {
                _buttonGenerateSummary.Enabled = true;
                _form.Form.Cursor = previousCursor;
            }
        }

        /// <summary>采集全书标注摘要（仅页码、时间戳与录音 Id）</summary>
        private static List<PdfAnnotationDigestItem> CollectDocumentAnnotations(
            IAnnotationService annotationService, string pdfPath, int pageCount)
        {
            var items = new List<PdfAnnotationDigestItem>();
            for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                if (items.Count >= PdfAnnotationDigestBuilder.MaxItems) break;

                PdfAnnotation? annotation;
                try
                {
                    annotation = annotationService.GetAnnotation(pdfPath, pageIndex);
                }
                catch
                {
                    continue;
                }

                items.AddRange(PdfAnnotationDigestBuilder.Collect(annotation, pageIndex));
            }

            return items.Take(PdfAnnotationDigestBuilder.MaxItems).ToList();
        }

        private static string SummarizeText(string? content) => SummarizeText(content, 24);

        private static string SummarizeText(string? content, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(content)) return "（空文字）";
            var normalized = content.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return normalized.Length <= maxLength ? normalized : normalized[..maxLength] + "…";
        }

        private static string DescribeShape(string? shapeType)
        {
            return shapeType switch
            {
                "Rectangle" => "矩形标注",
                "Ellipse" => "椭圆标注",
                "Arrow" => "箭头标注",
                "Mosaic" => "马赛克标注",
                null or "" => "笔划标注",
                _ => "形状标注"
            };
        }

        #endregion

        private void UpdateUIState()
        {
            var isRecording = _recorder.State == AudioRecorderState.Recording;
            _buttonRecord.Text = isRecording ? "■ 停止" : "● 录制";
            _buttonRecord.BackColor = isRecording ? Color.FromArgb(100, 100, 100) : _recordRed;
            _buttonStop.Enabled = isRecording;
            _buttonPlay.Enabled = !isRecording && _listRecordings.SelectedItem != null;
            _trackBarPosition.Enabled = _isPlaying;
        }

        private void RefreshRecordingList()
        {
            var hasItems = _recordings.Count > 0;
            _listRecordings.Visible = hasItems;
            _labelNoRecordings.Visible = !hasItems;

            _listRecordings.Items.Clear();
            foreach (var r in _recordings.OrderByDescending(r => r.CreatedAt))
                _listRecordings.Items.Add(r);

            _buttonPlay.Enabled = _listRecordings.SelectedItem != null && _recorder.State == AudioRecorderState.Idle;
            if (_buttonRename != null)
                _buttonRename.Enabled = _listRecordings.SelectedItem != null;
            if (_labelListTitle != null)
                _labelListTitle.Text = $"录音列表（{_recordings.Count}）";
        }

        private void ListRecordings_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _listRecordings.Items.Count) return;
            e.DrawBackground();

            var recording = (AudioRecording)_listRecordings.Items[e.Index];
            var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            var palette = PdfSidebarTheme.PaletteFor(_listRecordings);
            var bgColor = isSelected ? palette.BrandSoft : (e.Index % 2 == 0 ? palette.Surface : palette.SurfaceAlt);
            using var bgBrush = new SolidBrush(bgColor);
            e.Graphics.FillRectangle(bgBrush, e.Bounds);

            var label = string.IsNullOrEmpty(recording.Label)
                ? $"录音 {recording.CreatedAt:HH:mm:ss}"
                : recording.Label;
            var durationStr = FormatTime(TimeSpan.FromMilliseconds(recording.DurationMs));
            var displayText = $"{label}  ({durationStr})";

            using var font = new Font("Microsoft YaHei UI", 10F);
            using var textBrush = new SolidBrush(isSelected ? PdfSidebarTheme.Brand : palette.Text);
            e.Graphics.DrawString(displayText, font, textBrush, e.Bounds.X + 8, e.Bounds.Y + 8);

            e.DrawFocusRectangle();
        }

        private void ListRecordings_SelectedIndexChanged(object? sender, EventArgs e)
        {
            _buttonPlay.Enabled = _listRecordings.SelectedItem != null && _recorder.State == AudioRecorderState.Idle;
            if (_buttonRename != null)
                _buttonRename.Enabled = _listRecordings.SelectedItem != null;
        }

        private void ListRecordings_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            int index = _listRecordings.IndexFromPoint(e.Location);
            if (index >= 0 && _listRecordings.Items[index] is AudioRecording selected)
                StartPlayback(selected);
        }

        private void LoadRecordings()
        {
            var pdfPath = _form.CurrentPdfPath;
            if (string.IsNullOrEmpty(pdfPath))
            {
                _recordings.Clear();
                RefreshRecordingList();
                RefreshSummaryList();
                return;
            }

            var listPath = GetRecordingListPath(pdfPath);
            MigrateLegacyRecordingList(pdfPath, listPath);
            try
            {
                if (File.Exists(listPath))
                {
                    var json = File.ReadAllText(listPath);
                    _recordings = System.Text.Json.JsonSerializer.Deserialize<List<AudioRecording>>(json) ?? new List<AudioRecording>();
                    _recordings.RemoveAll(r => !File.Exists(r.FilePath));
                }
                else
                {
                    _recordings.Clear();
                }
            }
            catch
            {
                _recordings.Clear();
            }
            RefreshRecordingList();
            RefreshTimestampList();
            RefreshSummaryList();
        }

        private void SaveRecordings()
        {
            var pdfPath = _form.CurrentPdfPath;
            if (string.IsNullOrEmpty(pdfPath)) return;

            var listPath = GetRecordingListPath(pdfPath);
            WriteRecordingList(listPath, _recordings);
        }

        /// <summary>录音清单按 PDF 路径哈希存储，避免不同目录下同名 PDF 共用同一份清单</summary>
        private static string GetRecordingListPath(string pdfPath)
        {
            var dir = Path.Combine(AppPaths.AudioDir, "Recordings");
            var safeName = SanitizeFileName(Path.GetFileNameWithoutExtension(pdfPath));
            return Path.Combine(dir, $"{safeName}_{GetPathHash(pdfPath)}_recordings.json");
        }

        /// <summary>旧版仅按文件名存储的清单路径，仅用于一次性迁移</summary>
        private static string GetLegacyRecordingListPath(string pdfPath)
        {
            var dir = Path.Combine(AppPaths.AudioDir, "Recordings");
            var safeName = SanitizeFileName(Path.GetFileNameWithoutExtension(pdfPath));
            return Path.Combine(dir, $"{safeName}_recordings.json");
        }

        /// <summary>把旧清单中属于当前文档的条目迁移到新路径；旧文件保留，供其他同名文档各自迁移</summary>
        private void MigrateLegacyRecordingList(string pdfPath, string newPath)
        {
            try
            {
                if (File.Exists(newPath)) return;
                var legacyPath = GetLegacyRecordingListPath(pdfPath);
                if (!File.Exists(legacyPath)) return;

                var legacy = System.Text.Json.JsonSerializer.Deserialize<List<AudioRecording>>(
                    File.ReadAllText(legacyPath));
                if (legacy == null || legacy.Count == 0) return;

                var normalized = NormalizePath(pdfPath);
                var owned = legacy
                    .Where(r => string.IsNullOrEmpty(r.PdfPath) || NormalizePath(r.PdfPath) == normalized)
                    .ToList();
                if (owned.Count == 0) return;

                WriteRecordingList(newPath, owned);
                _logger.LogInformation("已迁移 {Count} 条旧版录音记录: {Path}", owned.Count, newPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "迁移旧版录音清单失败: {Path}", pdfPath);
            }
        }

        private static string GetPathHash(string pdfPath)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(NormalizePath(pdfPath)));
            return Convert.ToHexString(hash)[..12];
        }

        private static string NormalizePath(string path)
        {
            try
            {
                return Path.GetFullPath(path).ToLowerInvariant();
            }
            catch
            {
                return path.ToLowerInvariant();
            }
        }

        private static void WriteRecordingList(string path, List<AudioRecording> recordings)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                AppPaths.EnsureDirectoryExists(dir);

            var json = System.Text.Json.JsonSerializer.Serialize(recordings,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        private static string FormatTime(TimeSpan time)
        {
            return time.TotalHours >= 1
                ? $"{Math.Floor(time.TotalHours):00}:{time.Minutes:00}:{time.Seconds:00}"
                : $"{time.Minutes:00}:{time.Seconds:00}";
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder(name.Length);
            foreach (var c in name)
                sb.Append(invalid.Contains(c) ? '_' : c);
            return sb.ToString();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _uiTimer?.Stop();
            _uiTimer?.Dispose();
            _levelTimer?.Stop();
            _levelTimer?.Dispose();
            StopPlayback();
            _recorder.AudioLevelChanged -= OnAudioLevelChanged;
            if (_recorder.State == AudioRecorderState.Recording)
                _recorder.StopRecording();
        }
    }
}
