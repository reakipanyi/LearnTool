using LearningAssistant.Common;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace LearningAssistant.Managers
{
    public class PdfReaderAudioManager : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IPdfReaderFormAccess _form;
        private readonly IAudioRecorderService _recorder;
        private bool _disposed;

        private TabPage? _tabPage;
        private Button _buttonRecord;
        private Button _buttonStop;
        private Button _buttonPlay;
        private Label _labelTime;
        private TrackBar _trackBarPosition;
        private Panel _levelMeter;
        private Panel _levelFill;
        private ListBox _listRecordings;
        private Label _labelStatus;
        private Label _labelNoRecordings;
        private System.Windows.Forms.Timer _uiTimer;
        private System.Windows.Forms.Timer _levelTimer;

        private List<AudioRecording> _recordings = new();
        private string? _currentPdfPath;
        private WaveOutEvent? _playbackOut;
        private AudioFileReader? _playbackReader;
        private System.Windows.Forms.Timer? _playbackTimer;
        private bool _isPlaying;
        private bool _isDraggingPosition;

        private static Color _accentColor = Color.FromArgb(25, 118, 210);
        private static Color _recordRed = Color.FromArgb(244, 67, 54);
        private static Color _bgColor = Color.FromArgb(248, 248, 252);
        private static Color _textColor = Color.FromArgb(50, 50, 60);

        public PdfReaderAudioManager(ILogger logger, IPdfReaderFormAccess form, IAudioRecorderService recorder)
        {
            _logger = logger;
            _form = form;
            _recorder = recorder;
            _recorder.RecordingCompleted += OnRecordingCompleted;
            _recorder.PositionChanged += OnPositionChanged;
        }

        public void Initialize()
        {
            _tabPage = _form.TabPageAudio;
            if (_tabPage == null) return;

            CreateControls();
            UpdateUIState();
            _currentPdfPath = _form.CurrentPdfPath;
            LoadRecordings();
        }

        private void CreateControls()
        {
            if (_tabPage == null) return;

            _tabPage.SuspendLayout();

            _labelStatus = new Label
            {
                Text = "就绪",
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                ForeColor = _textColor,
                Location = new Point(12, 10),
                Size = new Size(200, 20)
            };
            _tabPage.Controls.Add(_labelStatus);

            _buttonRecord = new Button
            {
                Text = "● 录制",
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = _recordRed,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Location = new Point(12, 36),
                Size = new Size(90, 32),
                Cursor = Cursors.Hand,
                Tag = "record"
            };
            _buttonRecord.Click += ButtonRecord_Click;
            _tabPage.Controls.Add(_buttonRecord);

            _buttonStop = new Button
            {
                Text = "■ 停止",
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(100, 100, 100),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Location = new Point(108, 36),
                Size = new Size(90, 32),
                Cursor = Cursors.Hand,
                Enabled = false,
                Tag = "stop"
            };
            _buttonStop.Click += ButtonStop_Click;
            _tabPage.Controls.Add(_buttonStop);

            _buttonPlay = new Button
            {
                Text = "▶ 播放",
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = _accentColor,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Location = new Point(204, 36),
                Size = new Size(90, 32),
                Cursor = Cursors.Hand,
                Enabled = false,
                Tag = "play"
            };
            _buttonPlay.Click += ButtonPlay_Click;
            _tabPage.Controls.Add(_buttonPlay);

            _labelTime = new Label
            {
                Text = "00:00 / 00:00",
                Font = new Font("Consolas", 9F),
                ForeColor = _textColor,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(12, 76),
                Size = new Size(282, 20)
            };
            _tabPage.Controls.Add(_labelTime);

            _trackBarPosition = new TrackBar
            {
                Location = new Point(12, 98),
                Size = new Size(282, 30),
                Minimum = 0,
                Maximum = 1000,
                TickStyle = TickStyle.None,
                Enabled = false
            };
            _trackBarPosition.MouseDown += (s, e) => _isDraggingPosition = true;
            _trackBarPosition.MouseUp += TrackBarPosition_MouseUp;
            _tabPage.Controls.Add(_trackBarPosition);

            _levelMeter = new Panel
            {
                Location = new Point(12, 130),
                Size = new Size(282, 12),
                BackColor = Color.FromArgb(220, 220, 230)
            };
            _levelFill = new Panel
            {
                Location = new Point(1, 1),
                Size = new Size(0, 10),
                BackColor = _accentColor
            };
            _levelMeter.Controls.Add(_levelFill);
            _tabPage.Controls.Add(_levelMeter);

            var labelList = new Label
            {
                Text = "录音列表",
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                ForeColor = _textColor,
                Location = new Point(12, 150),
                Size = new Size(200, 18)
            };
            _tabPage.Controls.Add(labelList);

            _labelNoRecordings = new Label
            {
                Text = "暂无录音，点击 ● 录制 开始",
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = Color.FromArgb(160, 160, 170),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(12, 170),
                Size = new Size(282, 24)
            };
            _tabPage.Controls.Add(_labelNoRecordings);

            _listRecordings = new ListBox
            {
                Location = new Point(12, 170),
                Size = new Size(282, 130),
                BorderStyle = BorderStyle.FixedSingle,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 36,
                Visible = false
            };
            _listRecordings.DrawItem += ListRecordings_DrawItem;
            _listRecordings.SelectedIndexChanged += ListRecordings_SelectedIndexChanged;
            _listRecordings.MouseDoubleClick += ListRecordings_MouseDoubleClick;
            _tabPage.Controls.Add(_listRecordings);

            var buttonDelete = new Button
            {
                Text = "删除",
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = _textColor,
                BackColor = Color.FromArgb(230, 230, 235),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Location = new Point(12, 308),
                Size = new Size(60, 26),
                Cursor = Cursors.Hand,
                Tag = "delete"
            };
            buttonDelete.Click += ButtonDelete_Click;
            _tabPage.Controls.Add(buttonDelete);

            _tabPage.ResumeLayout(false);

            _uiTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _uiTimer.Tick += UiTimer_Tick;
            _uiTimer.Start();

            _levelTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _levelTimer.Tick += LevelTimer_Tick;
            _levelTimer.Start();
        }

        private void ButtonRecord_Click(object? sender, EventArgs e)
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

        private void ButtonStop_Click(object? sender, EventArgs e)
        {
            if (_recorder.State == AudioRecorderState.Recording)
                _recorder.StopRecording();
        }

        private void ButtonPlay_Click(object? sender, EventArgs e)
        {
            if (_isPlaying)
            {
                StopPlayback();
                return;
            }

            if (_listRecordings.SelectedItem is AudioRecording selected)
                StartPlayback(selected);
        }

        private void ButtonDelete_Click(object? sender, EventArgs e)
        {
            if (_listRecordings.SelectedItem is AudioRecording selected)
            {
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

        private void OnRecordingCompleted(object? sender, AudioRecording recording)
        {
            if (_form.Form.InvokeRequired)
            {
                _form.Form.Invoke(() => OnRecordingCompleted(sender, recording));
                return;
            }

            _recordings.Add(recording);
            SaveRecordings();
            RefreshRecordingList();
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
            if (_recorder.State != AudioRecorderState.Recording) return;
            _levelFill.Width = (int)(_levelMeter.Width * 0.3f);
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
            _playbackTimer?.Stop();
            _playbackTimer?.Dispose();
            _playbackTimer = null;

            _playbackOut?.Stop();
            _playbackOut?.Dispose();
            _playbackOut = null;
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

        /// <summary>获取当前 PDF 的录音列表</summary>
        public IReadOnlyList<AudioRecording> GetRecordings()
        {
            return _recordings.AsReadOnly();
        }

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
        }

        private void ListRecordings_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _listRecordings.Items.Count) return;
            e.DrawBackground();

            var recording = (AudioRecording)_listRecordings.Items[e.Index];
            var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            var bgColor = isSelected ? Color.FromArgb(230, 244, 255) : (e.Index % 2 == 0 ? Color.White : Color.FromArgb(248, 248, 252));
            using var bgBrush = new SolidBrush(bgColor);
            e.Graphics.FillRectangle(bgBrush, e.Bounds);

            var label = string.IsNullOrEmpty(recording.Label)
                ? $"录音 {recording.CreatedAt:HH:mm:ss}"
                : recording.Label;
            var durationStr = FormatTime(TimeSpan.FromMilliseconds(recording.DurationMs));
            var displayText = $"{label}  ({durationStr})";

            using var font = new Font("Microsoft YaHei UI", 10F);
            using var textBrush = new SolidBrush(isSelected ? _accentColor : _textColor);
            e.Graphics.DrawString(displayText, font, textBrush, e.Bounds.X + 8, e.Bounds.Y + 8);

            e.DrawFocusRectangle();
        }

        private void ListRecordings_SelectedIndexChanged(object? sender, EventArgs e)
        {
            _buttonPlay.Enabled = _listRecordings.SelectedItem != null && _recorder.State == AudioRecorderState.Idle;
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
                return;
            }

            var listPath = GetRecordingListPath(pdfPath);
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
        }

        private void SaveRecordings()
        {
            var pdfPath = _form.CurrentPdfPath;
            if (string.IsNullOrEmpty(pdfPath)) return;

            var listPath = GetRecordingListPath(pdfPath);
            var dir = Path.GetDirectoryName(listPath);
            if (!string.IsNullOrEmpty(dir))
                AppPaths.EnsureDirectoryExists(dir);

            var json = System.Text.Json.JsonSerializer.Serialize(_recordings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(listPath, json);
        }

        private static string GetRecordingListPath(string pdfPath)
        {
            var dir = Path.Combine(AppPaths.AudioDir, "Recordings");
            var safeName = SanitizeFileName(Path.GetFileNameWithoutExtension(pdfPath));
            return Path.Combine(dir, $"{safeName}_recordings.json");
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
            if (_recorder.State == AudioRecorderState.Recording)
                _recorder.StopRecording();
        }
    }
}