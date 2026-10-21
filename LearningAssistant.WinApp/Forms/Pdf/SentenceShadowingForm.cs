using System.Diagnostics;
using LearningAssistant.Models.Shadowing;
using LearningAssistant.Services.Media;
using LearningAssistant.Services.Shadowing;
using LearningAssistant.Services.TTS;

namespace LearningAssistant.Forms.Pdf
{
    /// <summary>
    /// 句子跟读独立窗口：展示分句结果、逐句合成与播放。
    /// 录音与参考评分在后续步骤接入。
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
        private readonly ListBox _listSentences = new();
        private readonly Label _labelTitle = new();
        private readonly Label _labelInfo = new();
        private readonly Label _labelCurrent = new();
        private readonly Label _labelStatus = new();
        private readonly ComboBox _comboMode = new();
        private readonly Button _btnPlay = new();
        private int _currentIndex = -1;
        private PlayMode _mode = PlayMode.AutoNext;

        public SentenceShadowingForm(string pdfPath, int pageIndex, string pageText, string documentTitle)
        {
            _pdfPath = pdfPath;
            _pageIndex = pageIndex;

            Text = $"句子跟读 - {documentTitle}";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(720, 640);
            MinimumSize = new Size(600, 480);
            Font = new Font("Microsoft YaHei UI", 9.5F);
            KeyPreview = true;

            var sentences = SentenceSegmenter.Segment(pageText);
            PersistManifest(sentences);

            BuildLayout();
            FillSentenceList(sentences);

            var tts = Program.ServiceProvider?.GetService(typeof(ITTSService)) as ITTSService;
            var subtitleParser = Program.ServiceProvider?.GetService(typeof(ISubtitleParser)) as ISubtitleParser;

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

            if (sentences.Count > 0)
                SetCurrentIndex(0, play: false);

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

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 132, Padding = new Padding(16, 8, 16, 8) };

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
            btnStop.Click += (_, _) => StopPlayback();

            _labelStatus.AutoSize = false;
            _labelStatus.Location = new Point(16, 56);
            _labelStatus.Size = new Size(660, 24);
            _labelStatus.ForeColor = Color.FromArgb(100, 116, 139);

            var labelHint = new Label
            {
                Text = "快捷键：空格 播放，R 重播当前句，→ 下一句",
                AutoSize = true,
                Location = new Point(16, 90),
                ForeColor = Color.FromArgb(148, 163, 184)
            };

            bottom.Controls.Add(_comboMode);
            bottom.Controls.Add(btnPrev);
            bottom.Controls.Add(_btnPlay);
            bottom.Controls.Add(btnNext);
            bottom.Controls.Add(btnStop);
            bottom.Controls.Add(_labelStatus);
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
                FireAndForget(PlayCurrentAsync());
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

            int index = _currentIndex < 0 ? 0 : _currentIndex;
            SetCurrentIndex(index, play: true);
        }

        private void StopPlayback()
        {
            _practice?.StopPlayback();
            _labelStatus.Text = "已停止。";
        }

        private async Task PlayCurrentAsync()
        {
            if (_practice == null || _currentIndex < 0)
                return;

            _labelStatus.Text = "正在准备音频…";
            bool started = await _practice.PlaySentenceAsync(_currentIndex);
            if (!IsHandleCreated)
                return;

            _labelStatus.Text = started
                ? $"正在播放第 {_currentIndex + 1} 句。"
                : "播放失败，请检查语音服务是否可用。";
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
                int next;
                switch (_mode)
                {
                    case PlayMode.SingleLoop:
                        next = index;
                        break;
                    case PlayMode.Continuous:
                        next = (index + 1) % _practice.Sentences.Count;
                        break;
                    default:
                        if (index + 1 >= _practice.Sentences.Count)
                        {
                            _labelStatus.Text = "已播放到最后一页句子。";
                            return;
                        }
                        next = index + 1;
                        break;
                }

                bool started = await _practice.PlaySentenceAsync(next);
                if (!IsHandleCreated)
                    return;

                if (started)
                {
                    _currentIndex = next;
                    _listSentences.SelectedIndex = next;
                    _labelCurrent.Text = $"第 {next + 1} 句：{_practice.Sentences[next].Sentence.Text}";
                    _labelStatus.Text = $"正在播放第 {next + 1} 句。";
                }
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

        private void OnFormShown(object? sender, EventArgs e)
        {
            if (_practice == null || _practice.Sentences.Count == 0)
                return;

            // 单句合成实测 P50 约 6 秒，打开窗口后立即顺序预热前三句，首句优先。
            FireAndForget(_practice.PrefetchAsync(0, Math.Min(3, _practice.Sentences.Count)));
        }

        private void OnFormClosed(object? sender, FormClosedEventArgs e)
        {
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
