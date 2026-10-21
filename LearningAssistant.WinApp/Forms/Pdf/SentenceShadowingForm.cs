using System.Diagnostics;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Models.Shadowing;
using LearningAssistant.Services.Media;
using LearningAssistant.Services.Pdf;
using LearningAssistant.Services.Shadowing;
using LearningAssistant.Services.TTS;

namespace LearningAssistant.Forms.Pdf
{
    /// <summary>
    /// 句子跟读独立窗口：分句、逐句合成播放、倒计时自动录音与参考评分。
    /// </summary>
    public sealed class SentenceShadowingForm : Form
    {
        private enum PlayMode
        {
            SingleLoop,
            AutoNext,
            Continuous
        }

        private readonly string _pdfPath;
        private readonly int _pageIndex;
        private readonly ShadowingPracticeService? _practice;
        private readonly IAudioRecorderService? _recorder;
        private readonly IPronunciationScorer? _scorer;
        private readonly EventHandler<AudioRecording>? _recordingHandler;
        private readonly ListBox _listSentences = new();
        private readonly Label _labelTitle = new();
        private readonly Label _labelInfo = new();
        private readonly Label _labelCurrent = new();
        private readonly Label _labelStatus = new();
        private readonly Label _labelScore = new();
        private readonly ComboBox _comboMode = new();
        private readonly Button _btnPlay = new();
        private readonly Button _btnRecord = new();
        private readonly System.Windows.Forms.Timer _countdownTimer = new() { Interval = 700 };
        private readonly System.Windows.Forms.Timer _recordLimitTimer = new();
        private int _currentIndex = -1;
        private int _countdown;
        private int _pendingIndex = -1;
        private int _recordingIndex = -1;
        private string? _recordingSentenceId;
        private bool _suppressRecordingAdvance;
        private PlayMode _mode = PlayMode.AutoNext;

        public SentenceShadowingForm(string pdfPath, int pageIndex, string pageText, string documentTitle)
        {
            _pdfPath = pdfPath;
            _pageIndex = pageIndex;

            Text = $"句子跟读 - {documentTitle}";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(720, 680);
            MinimumSize = new Size(600, 520);
            Font = new Font("Microsoft YaHei UI", 9.5F);
            KeyPreview = true;

            var sentences = SentenceSegmenter.Segment(pageText);
            PersistManifest(sentences);

            BuildLayout();
            FillSentenceList(sentences);

            var tts = Program.ServiceProvider?.GetService(typeof(ITTSService)) as ITTSService;
            var subtitleParser = Program.ServiceProvider?.GetService(typeof(ISubtitleParser)) as ISubtitleParser;
            _recorder = Program.ServiceProvider?.GetService(typeof(IAudioRecorderService)) as IAudioRecorderService;
            _scorer = Program.ServiceProvider?.GetService(typeof(IPronunciationScorer)) as IPronunciationScorer;

            if (tts == null || subtitleParser == null)
            {
                _labelStatus.Text = "语音服务不可用，暂时无法播放。";
                _btnPlay.Enabled = false;
            }
            else
            {
                _practice = new ShadowingPracticeService(sentences, tts, subtitleParser);
                _practice.StateChanged += OnSentenceStateChanged;
                _practice.SentenceFinished += OnSentenceFinished;
                _practice.ErrorOccurred += OnPracticeError;
            }

            if (_recorder == null)
            {
                _btnRecord.Enabled = false;
                _btnRecord.Text = "录音不可用";
            }
            else
            {
                _recordingHandler = OnRecordingCompleted;
                _recorder.RecordingCompleted += _recordingHandler;
            }

            _labelScore.Visible = _scorer?.IsAvailable == true;

            if (sentences.Count > 0)
                SetCurrentIndex(0, play: false);

            _countdownTimer.Tick += OnCountdownTick;
            _recordLimitTimer.Tick += OnRecordLimitReached;

            Shown += OnFormShown;
            FormClosed += OnFormClosed;
        }

        private void BuildLayout()
        {
            _labelTitle.Text = "句子跟读";
            _labelTitle.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
            _labelTitle.AutoSize = true;
            _labelTitle.Location = new Point(16, 14);

            _labelInfo.Text = $"第 {_pageIndex + 1} 页";
            _labelInfo.ForeColor = Color.FromArgb(100, 116, 139);
            _labelInfo.AutoSize = true;
            _labelInfo.Location = new Point(16, 40);

            var header = new Panel { Dock = DockStyle.Top, Height = 66 };
            header.Controls.Add(_labelTitle);
            header.Controls.Add(_labelInfo);

            _listSentences.Dock = DockStyle.Fill;
            _listSentences.IntegralHeight = false;
            _listSentences.HorizontalScrollbar = true;
            _listSentences.Font = new Font("Microsoft YaHei UI", 10F);
            _listSentences.SelectedIndexChanged += OnSentenceSelected;
            _listSentences.DoubleClick += OnSentenceDoubleClick;

            _labelCurrent.Dock = DockStyle.Top;
            _labelCurrent.Height = 58;
            _labelCurrent.Padding = new Padding(16, 8, 16, 8);
            _labelCurrent.TextAlign = ContentAlignment.MiddleLeft;
            _labelCurrent.Font = new Font("Microsoft YaHei UI", 11F);
            _labelCurrent.ForeColor = Color.FromArgb(15, 23, 42);
            _labelCurrent.AutoEllipsis = true;

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 168, Padding = new Padding(16, 8, 16, 8) };

            _comboMode.Items.AddRange(new object[] { "单句循环", "自动下一句", "整段连读" });
            _comboMode.SelectedIndex = 1;
            _comboMode.DropDownStyle = ComboBoxStyle.DropDownList;
            _comboMode.Width = 120;
            _comboMode.Location = new Point(16, 8);
            _comboMode.SelectedIndexChanged += OnModeChanged;

            var btnPrev = CreateButton("上一句", 88);
            btnPrev.Location = new Point(152, 6);
            btnPrev.Click += (_, _) => MoveSentence(-1);

            _btnPlay.Text = "播放";
            _btnPlay.Width = 96;
            _btnPlay.Height = 32;
            _btnPlay.Location = new Point(250, 6);
            _btnPlay.Click += (_, _) => StartPlayback();

            var btnNext = CreateButton("下一句", 88);
            btnNext.Location = new Point(356, 6);
            btnNext.Click += (_, _) => MoveSentence(1);

            var btnStop = CreateButton("停止", 76);
            btnStop.Location = new Point(454, 6);
            btnStop.Click += (_, _) => StopAll();

            _btnRecord.Text = "🎤 录音";
            _btnRecord.Width = 96;
            _btnRecord.Height = 32;
            _btnRecord.Location = new Point(546, 6);
            _btnRecord.Click += OnRecordButtonClicked;

            _labelStatus.AutoSize = false;
            _labelStatus.Location = new Point(16, 56);
            _labelStatus.Size = new Size(660, 24);
            _labelStatus.ForeColor = Color.FromArgb(100, 116, 139);

            _labelScore.AutoSize = true;
            _labelScore.Location = new Point(16, 84);
            _labelScore.ForeColor = Color.FromArgb(139, 92, 246);
            _labelScore.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            _labelScore.Text = "参考分：—";

            var labelHint = new Label
            {
                Text = "快捷键：空格 播放，R 重播当前句，→ 下一句",
                AutoSize = true,
                Location = new Point(16, 116),
                ForeColor = Color.FromArgb(148, 163, 184)
            };

            bottom.Controls.Add(_comboMode);
            bottom.Controls.Add(btnPrev);
            bottom.Controls.Add(_btnPlay);
            bottom.Controls.Add(btnNext);
            bottom.Controls.Add(btnStop);
            bottom.Controls.Add(_btnRecord);
            bottom.Controls.Add(_labelStatus);
            bottom.Controls.Add(_labelScore);
            bottom.Controls.Add(labelHint);

            Controls.Add(_listSentences);
            Controls.Add(_labelCurrent);
            Controls.Add(header);
            Controls.Add(bottom);
        }

        private static Button CreateButton(string text, int width) => new()
        {
            Text = text,
            Width = width,
            Height = 32
        };

        private void FillSentenceList(IReadOnlyList<ShadowingSentence> sentences)
        {
            _listSentences.BeginUpdate();
            try
            {
                _listSentences.Items.Clear();
                for (int i = 0; i < sentences.Count; i++)
                    _listSentences.Items.Add(FormatSentenceItem(i, sentences[i].Text, ShadowingAudioState.Pending));
            }
            finally
            {
                _listSentences.EndUpdate();
            }

            _labelInfo.Text = sentences.Count == 0
                ? $"第 {_pageIndex + 1} 页 · 未识别到可跟读的句子"
                : $"第 {_pageIndex + 1} 页 · 共 {sentences.Count} 句";

            if (sentences.Count == 0)
                _labelStatus.Text = "当前页面没有可分句的文本，请先执行 OCR 或翻译取得文本。";
        }

        private void PersistManifest(List<ShadowingSentence> sentences)
        {
            if (string.IsNullOrWhiteSpace(_pdfPath) || sentences.Count == 0)
                return;

            try
            {
                var manifest = new ShadowingManifest
                {
                    PdfPath = _pdfPath,
                    PageIndex = _pageIndex,
                    SegmenterVersion = SentenceSegmenter.Version,
                    Sentences = sentences
                };
                new ShadowingStore().Save(manifest);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[SentenceShadowing] 保存分句清单失败: {ex.Message}");
            }
        }

        private static string FormatSentenceItem(int index, string text, ShadowingAudioState state)
        {
            string marker = state switch
            {
                ShadowingAudioState.Synthesizing => "…",
                ShadowingAudioState.Ready => "▶",
                ShadowingAudioState.Failed => "×",
                _ => "○"
            };
            return $"{index + 1:00}  {marker}  {text}";
        }

        private void SetCurrentIndex(int index, bool play)
        {
            if (_practice == null || index < 0 || index >= _practice.Sentences.Count)
                return;

            _currentIndex = index;
            _listSentences.SelectedIndex = index;
            _labelCurrent.Text = $"第 {index + 1} 句：{_practice.Sentences[index].Sentence.Text}";

            if (play)
                FireAndForget(PlayIndexAsync(index));
        }

        private void MoveSentence(int delta)
        {
            if (_practice == null || _practice.Sentences.Count == 0)
                return;

            int start = _currentIndex < 0 ? 0 : _currentIndex;
            int next = Math.Clamp(start + delta, 0, _practice.Sentences.Count - 1);
            SetCurrentIndex(next, play: true);
        }

        private void StartPlayback()
        {
            if (_practice == null || _practice.Sentences.Count == 0)
                return;

            StopRecordingIfAny(suppressAdvance: true);
            StopCountdown();

            int index = _currentIndex < 0 ? 0 : _currentIndex;
            SetCurrentIndex(index, play: true);
        }

        private void StopAll()
        {
            StopCountdown();
            StopRecordingIfAny(suppressAdvance: true);
            _practice?.StopPlayback();
            _btnRecord.Text = "🎤 录音";
            _labelStatus.Text = "已停止。";
        }

        private async Task PlayIndexAsync(int index)
        {
            if (_practice == null || index < 0)
                return;

            _currentIndex = index;
            _listSentences.SelectedIndex = index;
            _labelCurrent.Text = $"第 {index + 1} 句：{_practice.Sentences[index].Sentence.Text}";
            _labelScore.Text = "参考分：—";

            _labelStatus.Text = "正在准备音频…";
            bool started = await _practice.PlaySentenceAsync(index);
            if (!IsHandleCreated)
                return;

            _labelStatus.Text = started
                ? $"正在播放第 {index + 1} 句。"
                : "播放失败，请检查语音服务是否可用。";
        }

        private async Task AdvanceAsync(int finishedIndex)
        {
            if (_practice == null || _practice.Sentences.Count == 0)
                return;

            int next;
            switch (_mode)
            {
                case PlayMode.SingleLoop:
                    next = finishedIndex;
                    break;
                case PlayMode.Continuous:
                    next = (finishedIndex + 1) % _practice.Sentences.Count;
                    break;
                default:
                    if (finishedIndex + 1 >= _practice.Sentences.Count)
                    {
                        _labelStatus.Text = "已播放到最后一页句子。";
                        _btnRecord.Text = "🎤 录音";
                        return;
                    }
                    next = finishedIndex + 1;
                    break;
            }

            await PlayIndexAsync(next);
        }

        private void OnSentenceSelected(object? sender, EventArgs e)
        {
            if (_listSentences.SelectedIndex < 0 || _practice == null)
                return;

            SetCurrentIndex(_listSentences.SelectedIndex, play: false);
        }

        private void OnSentenceDoubleClick(object? sender, EventArgs e)
        {
            if (_listSentences.SelectedIndex < 0 || _practice == null)
                return;

            SetCurrentIndex(_listSentences.SelectedIndex, play: false);
            StartPlayback();
        }

        private void OnModeChanged(object? sender, EventArgs e)
        {
            _mode = _comboMode.SelectedIndex switch
            {
                0 => PlayMode.SingleLoop,
                2 => PlayMode.Continuous,
                _ => PlayMode.AutoNext
            };
        }

        private void OnSentenceStateChanged(object? sender, int index)
        {
            if (!IsHandleCreated || _practice == null)
                return;
            if (index < 0 || index >= _practice.Sentences.Count)
                return;

            var state = _practice.Sentences[index];
            _listSentences.BeginUpdate();
            try
            {
                _listSentences.Items[index] = FormatSentenceItem(
                    index, state.Sentence.Text, state.AudioState);
            }
            finally
            {
                _listSentences.EndUpdate();
            }

            if (index == _currentIndex && state.AudioState == ShadowingAudioState.Failed)
                _labelStatus.Text = $"第 {index + 1} 句音频合成失败。";
        }

        private async void OnSentenceFinished(object? sender, int index)
        {
            if (!IsHandleCreated || _practice == null || _practice.Sentences.Count == 0)
                return;

            try
            {
                if (_recorder != null)
                {
                    StartCountdown(index);
                    return;
                }

                await AdvanceAsync(index);
            }
            catch (Exception ex)
            {
                if (IsHandleCreated)
                    _labelStatus.Text = $"自动播放失败：{ex.Message}";
            }
        }

        private void OnPracticeError(object? sender, string message)
        {
            if (IsHandleCreated)
                _labelStatus.Text = message;
        }

        private void StartCountdown(int index)
        {
            StopCountdown();
            _pendingIndex = index;
            _countdown = 3;
            _labelStatus.Text = $"准备录音：{_countdown}";
            _countdownTimer.Start();
        }

        private void OnCountdownTick(object? sender, EventArgs e)
        {
            _countdown--;
            if (_countdown > 0)
            {
                _labelStatus.Text = $"准备录音：{_countdown}";
                return;
            }

            StopCountdown();
            StartSentenceRecording(_pendingIndex);
        }

        private void StopCountdown()
        {
            _countdownTimer.Stop();
            _pendingIndex = -1;
        }

        private void StartSentenceRecording(int index)
        {
            if (_recorder == null || _practice == null
                || index < 0 || index >= _practice.Sentences.Count)
            {
                return;
            }

            if (_recorder.State == AudioRecorderState.Recording)
                return;

            try
            {
                _practice.StopPlayback();
                _recordingIndex = index;
                _recordingSentenceId = _practice.Sentences[index].Sentence.Id;

                _recorder.StartRecording(
                    _pdfPath,
                    _pageIndex,
                    Path.GetFileName(_pdfPath),
                    AudioRecordingScope.Sentence,
                    _recordingSentenceId);

                int durationMs = _practice.GetAudioDurationMs(index);
                int limitMs = Math.Max(8000, (int)(durationMs * 2.5));
                _recordLimitTimer.Interval = Math.Min(limitMs, 60000);
                _recordLimitTimer.Start();

                _btnRecord.Text = "⏹ 结束录音";
                _labelStatus.Text = $"正在录制第 {index + 1} 句…";
            }
            catch (Exception ex)
            {
                _recordingIndex = -1;
                _recordingSentenceId = null;
                _labelStatus.Text = $"启动录音失败：{ex.Message}";
            }
        }

        private void OnRecordLimitReached(object? sender, EventArgs e)
        {
            _recordLimitTimer.Stop();
            _recorder?.StopRecording();
        }

        private void OnRecordButtonClicked(object? sender, EventArgs e)
        {
            if (_recorder == null)
                return;

            if (_recorder.State == AudioRecorderState.Recording)
            {
                _recordLimitTimer.Stop();
                _recorder.StopRecording();
                return;
            }

            StopCountdown();
            StopPlaybackOnly();
            StartSentenceRecording(_currentIndex < 0 ? 0 : _currentIndex);
        }

        private async void OnRecordingCompleted(object? sender, AudioRecording recording)
        {
            if (recording.Scope != AudioRecordingScope.Sentence)
                return;
            if (!string.IsNullOrEmpty(_recordingSentenceId)
                && !string.Equals(recording.SentenceId, _recordingSentenceId, StringComparison.Ordinal))
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnRecordingCompleted(sender, recording)));
                return;
            }

            _recordLimitTimer.Stop();
            _btnRecord.Text = "🎤 录音";

            int index = _recordingIndex;
            _recordingIndex = -1;
            _recordingSentenceId = null;

            try
            {
                if (index >= 0 && _practice != null && _scorer?.IsAvailable == true)
                {
                    _labelScore.Text = "正在评分…";
                    var sentence = _practice.Sentences[index].Sentence;
                    var result = await _scorer.ScoreAsync(sentence.Text, recording.FilePath);
                    if (IsHandleCreated)
                    {
                        _labelScore.Text = result == null
                            ? "参考分：—（录音过短或无法识别）"
                            : $"参考分：{result.Score}（漏读 {result.MissingWords.Count} 词）";
                    }
                }
            }
            catch (Exception ex)
            {
                if (IsHandleCreated)
                    _labelScore.Text = $"评分失败：{ex.Message}";
            }

            bool shouldAdvance = !_suppressRecordingAdvance;
            _suppressRecordingAdvance = false;

            if (shouldAdvance && index >= 0)
            {
                try
                {
                    await AdvanceAsync(index);
                }
                catch (Exception ex)
                {
                    if (IsHandleCreated)
                        _labelStatus.Text = $"自动播放失败：{ex.Message}";
                }
            }
        }

        private void StopRecordingIfAny(bool suppressAdvance)
        {
            _recordLimitTimer.Stop();
            if (_recorder?.State != AudioRecorderState.Recording)
                return;

            _suppressRecordingAdvance = suppressAdvance;
            _recorder.StopRecording();
        }

        private void StopPlaybackOnly()
        {
            _practice?.StopPlayback();
        }

        private void OnFormShown(object? sender, EventArgs e)
        {
            if (_practice == null || _practice.Sentences.Count == 0)
                return;

            // 单句合成实测 P50 约 6 秒，打开窗口后立即顺序预热前三句，首句优先。
            FireAndForget(_practice.PrefetchAsync(0, Math.Min(3, _practice.Sentences.Count)));
        }

        private void OnFormClosed(object? sender, FormClosedEventArgs e)
        {
            _countdownTimer.Stop();
            _countdownTimer.Dispose();
            _recordLimitTimer.Stop();
            _recordLimitTimer.Dispose();

            StopRecordingIfAny(suppressAdvance: true);

            if (_recorder != null && _recordingHandler != null)
                _recorder.RecordingCompleted -= _recordingHandler;

            if (_practice != null)
            {
                _practice.StateChanged -= OnSentenceStateChanged;
                _practice.SentenceFinished -= OnSentenceFinished;
                _practice.ErrorOccurred -= OnPracticeError;
                _practice.Dispose();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Space)
            {
                StartPlayback();
                return true;
            }

            if (keyData == Keys.R)
            {
                StartPlayback();
                return true;
            }

            if (keyData == Keys.Right)
            {
                MoveSentence(1);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private async void FireAndForget(Task task)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Trace.TraceError($"[SentenceShadowing] 异步操作失败: {ex.Message}");
                if (IsHandleCreated)
                    _labelStatus.Text = $"操作失败：{ex.Message}";
            }
        }
    }
}
