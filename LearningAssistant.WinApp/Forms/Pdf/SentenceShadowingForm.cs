using System.Diagnostics;
using LearningAssistant.Common.UI;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Models.Shadowing;
using LearningAssistant.Platform.Media;
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
        private readonly Button _btnReplayRecording = new();
        private readonly CircularStatus _circularStatus = new();
        private readonly Panel _progressTrack = new();
        private readonly Dictionary<string, string> _sentenceRecordings = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _sentenceScores = new(StringComparer.Ordinal);
        private readonly ShadowingStore _store = new();
        private NAudioMediaPlayer? _recordingPlayer;
        private readonly System.Windows.Forms.Timer _countdownTimer = new() { Interval = 700 };
        private readonly System.Windows.Forms.Timer _recordLimitTimer = new();
        private int _totalSentences;
        private int _currentIndex = -1;
        private int _countdown;
        private int _pendingIndex = -1;
        private int _recordingIndex = -1;
        private string? _recordingSentenceId;
        private bool _suppressRecordingAdvance;
        private PlayMode _mode = PlayMode.AutoNext;

        private sealed class SentenceRow
        {
            public int Index { get; init; }
            public string Text { get; init; } = string.Empty;
            public ShadowingAudioState State { get; set; } = ShadowingAudioState.Pending;
        }

        public SentenceShadowingForm(string pdfPath, int pageIndex, string pageText, string documentTitle)
        {
            _pdfPath = pdfPath;
            _pageIndex = pageIndex;

            Text = $"句子跟读 - {documentTitle}";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(860, 640);
            MinimumSize = new Size(720, 560);
            BackColor = PdfSidebarTheme.Background;
            Font = new Font("Microsoft YaHei UI", 9.5F);
            KeyPreview = true;

            var sentences = SentenceSegmenter.Segment(pageText);
            PersistManifest(sentences);

            BuildLayout();
            FillSentenceList(sentences);
            LoadSentenceRecordings(sentences);
            PdfSidebarTheme.ThemeChanged += OnSidebarThemeChanged;
            if (PdfSidebarTheme.IsDark)
                PdfSidebarTheme.ApplyRecursive(this);

            var tts = Program.ServiceProvider?.GetService(typeof(ITTSService)) as ITTSService;
            var subtitleParser = Program.ServiceProvider?.GetService(typeof(ISubtitleParser)) as ISubtitleParser;
            _recorder = Program.ServiceProvider?.GetService(typeof(IAudioRecorderService)) as IAudioRecorderService;
            try
            {
                _scorer = Program.ServiceProvider?.GetService(typeof(IPronunciationScorer)) as IPronunciationScorer;
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[SentenceShadowing] 评分器初始化失败: {ex.Message}");
                _scorer = null;
            }

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
                _recordingPlayer = new NAudioMediaPlayer(subtitleParser);
                _recordingPlayer.PlaybackFinished += OnRecordingPlaybackFinished;
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

            _btnReplayRecording.Enabled = false;
            if (_scorer?.IsAvailable != true)
                _labelScore.Text = "参考分：—（未安装英语识别器，按完成度评分）";

            if (sentences.Count > 0)
                SetCurrentIndex(0, play: false);

            _countdownTimer.Tick += OnCountdownTick;
            _recordLimitTimer.Tick += OnRecordLimitReached;

            Shown += OnFormShown;
            FormClosed += OnFormClosed;
        }

        private void BuildLayout()
        {
            SuspendLayout();

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = PdfSidebarTheme.Background,
                Padding = new Padding(16, 8, 16, 10)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 8F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildProgress(), 0, 1);
            root.Controls.Add(BuildBody(), 0, 2);
            root.Controls.Add(BuildFooter(), 0, 3);

            Controls.Add(root);
            ResumeLayout(true);
        }

        private Control BuildHeader()
        {
            var header = new Panel { Dock = DockStyle.Fill, BackColor = PdfSidebarTheme.Background };

            _labelTitle.Text = "句子跟读";
            _labelTitle.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            _labelTitle.ForeColor = PdfSidebarTheme.Text;
            _labelTitle.AutoSize = true;
            _labelTitle.Location = new Point(0, 2);

            _labelInfo.Text = $"第 {_pageIndex + 1} 页";
            _labelInfo.Font = new Font("Microsoft YaHei UI", 9F);
            _labelInfo.ForeColor = PdfSidebarTheme.Muted;
            _labelInfo.Tag = "muted";
            _labelInfo.AutoSize = true;
            _labelInfo.Location = new Point(2, 32);

            header.Controls.Add(_labelTitle);
            header.Controls.Add(_labelInfo);
            return header;
        }

        private Control BuildProgress()
        {
            _progressTrack.Dock = DockStyle.Fill;
            _progressTrack.BackColor = PdfSidebarTheme.Background;
            _progressTrack.Margin = Padding.Empty;
            _progressTrack.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int trackHeight = 6;
                var track = new Rectangle(0, (int)((e.Graphics.VisibleClipBounds.Height - trackHeight) / 2f),
                    Math.Max(0, _progressTrack.Width - 1), trackHeight);
                if (track.Width <= 0) return;

                using (var path = GdiHelper.CreateRoundedRectPath(track, 3))
                using (var brush = new SolidBrush(PdfSidebarTheme.SurfaceAlt))
                    e.Graphics.FillPath(brush, path);

                if (_totalSentences <= 0) return;
                float ratio = Math.Clamp((_currentIndex + 1f) / _totalSentences, 0f, 1f);
                var filled = new Rectangle(track.X, track.Y, Math.Max(1, (int)(track.Width * ratio)), track.Height);
                using var fillPath = GdiHelper.CreateRoundedRectPath(filled, 3);
                using var fillBrush = new SolidBrush(PdfSidebarTheme.Brand);
                e.Graphics.FillPath(fillBrush, fillPath);
            };
            return _progressTrack;
        }

        private Control BuildBody()
        {
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = PdfSidebarTheme.Background
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280F));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var listCard = new SidebarCard("句子列表")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 10, 0)
            };

            _listSentences.Dock = DockStyle.Fill;
            _listSentences.IntegralHeight = false;
            _listSentences.BorderStyle = BorderStyle.None;
            _listSentences.DrawMode = DrawMode.OwnerDrawFixed;
            _listSentences.ItemHeight = 60;
            _listSentences.Font = new Font("Microsoft YaHei UI", 9.5F);
            _listSentences.BackColor = PdfSidebarTheme.Surface;
            _listSentences.ForeColor = PdfSidebarTheme.Text;
            _listSentences.SelectedIndexChanged += OnSentenceSelected;
            _listSentences.DoubleClick += OnSentenceDoubleClick;
            _listSentences.DrawItem += OnSentenceDrawItem;
            listCard.Body.Controls.Add(_listSentences);
            body.Controls.Add(listCard, 0, 0);

            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = PdfSidebarTheme.Background
            };
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 36F));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 24F));
            right.Controls.Add(BuildCurrentSentenceCard(), 0, 0);
            right.Controls.Add(BuildPracticeCard(), 0, 1);
            right.Controls.Add(BuildScoreCard(), 0, 2);
            body.Controls.Add(right, 1, 0);

            return body;
        }

        private Control BuildCurrentSentenceCard()
        {
            var card = new SidebarCard("当前句")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8)
            };

            _labelCurrent.Dock = DockStyle.Fill;
            _labelCurrent.Padding = new Padding(4, 2, 4, 2);
            _labelCurrent.TextAlign = ContentAlignment.MiddleLeft;
            _labelCurrent.Font = new Font("Microsoft YaHei UI", 13F);
            _labelCurrent.ForeColor = PdfSidebarTheme.Text;
            _labelCurrent.AutoSize = false;
            _labelCurrent.AutoEllipsis = false;
            _labelCurrent.Text = "请从左侧选择一句开始跟读";

            var navigation = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = PdfSidebarTheme.Surface,
                Padding = new Padding(0, 4, 0, 0)
            };

            var btnPrev = SidebarUi.CreateIconButton("◀", "上一句");
            btnPrev.Size = new Size(38, 34);
            btnPrev.Margin = new Padding(0, 0, 6, 0);
            btnPrev.Click += (_, _) => MoveSentence(-1);

            _btnPlay.Text = "▶ 播放";
            _btnPlay.Size = new Size(96, 34);
            _btnPlay.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
            _btnPlay.FlatStyle = FlatStyle.Flat;
            _btnPlay.BackColor = PdfSidebarTheme.Brand;
            _btnPlay.ForeColor = Color.White;
            _btnPlay.FlatAppearance.BorderSize = 0;
            _btnPlay.Tag = "accent";
            _btnPlay.Cursor = Cursors.Hand;
            _btnPlay.Margin = new Padding(0, 0, 6, 0);
            _btnPlay.Click += (_, _) => StartPlayback();

            var btnNext = SidebarUi.CreateIconButton("▶", "下一句");
            btnNext.Size = new Size(38, 34);
            btnNext.Margin = new Padding(0, 0, 6, 0);
            btnNext.Click += (_, _) => MoveSentence(1);

            var btnStop = SidebarUi.CreateGhostButton("停止", 34);
            btnStop.Width = 64;
            btnStop.Margin = Padding.Empty;
            btnStop.Click += (_, _) => StopAll();

            navigation.Controls.Add(btnPrev);
            navigation.Controls.Add(_btnPlay);
            navigation.Controls.Add(btnNext);
            navigation.Controls.Add(btnStop);

            card.Body.Controls.Add(_labelCurrent);
            card.Body.Controls.Add(navigation);
            return card;
        }

        private Control BuildPracticeCard()
        {
            var card = new SidebarCard("跟读")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8)
            };

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = PdfSidebarTheme.Surface
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _circularStatus.Margin = new Padding(0, 4, 0, 0);
            _circularStatus.Anchor = AnchorStyles.None;
            grid.Controls.Add(_circularStatus, 0, 0);

            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = PdfSidebarTheme.Surface
            };
            actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            actions.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _btnRecord.Text = "🎤 录音";
            _btnRecord.Dock = DockStyle.Fill;
            _btnRecord.FlatStyle = FlatStyle.Flat;
            _btnRecord.BackColor = PdfSidebarTheme.Danger;
            _btnRecord.ForeColor = Color.White;
            _btnRecord.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            _btnRecord.FlatAppearance.BorderSize = 0;
            _btnRecord.Tag = "record";
            _btnRecord.Cursor = Cursors.Hand;
            _btnRecord.Margin = new Padding(0, 2, 0, 4);
            _btnRecord.Click += OnRecordButtonClicked;

            _btnReplayRecording.Text = "▶ 回放我的录音";
            _btnReplayRecording.Dock = DockStyle.Fill;
            _btnReplayRecording.FlatStyle = FlatStyle.Flat;
            _btnReplayRecording.BackColor = PdfSidebarTheme.Surface;
            _btnReplayRecording.ForeColor = PdfSidebarTheme.Text;
            _btnReplayRecording.FlatAppearance.BorderColor = PdfSidebarTheme.Border;
            _btnReplayRecording.FlatAppearance.BorderSize = 1;
            _btnReplayRecording.FlatAppearance.MouseOverBackColor = PdfSidebarTheme.Hover;
            _btnReplayRecording.Cursor = Cursors.Hand;
            _btnReplayRecording.Margin = new Padding(0, 0, 0, 4);
            _btnReplayRecording.Click += OnReplayRecordingClicked;

            _labelStatus.Dock = DockStyle.Fill;
            _labelStatus.AutoSize = false;
            _labelStatus.ForeColor = PdfSidebarTheme.Muted;
            _labelStatus.Tag = "muted";
            _labelStatus.Font = new Font("Microsoft YaHei UI", 8.5F);
            _labelStatus.Padding = new Padding(2, 2, 2, 0);

            actions.Controls.Add(_btnRecord, 0, 0);
            actions.Controls.Add(_btnReplayRecording, 0, 1);
            actions.Controls.Add(_labelStatus, 0, 2);
            grid.Controls.Add(actions, 1, 0);

            card.Body.Controls.Add(grid);
            return card;
        }

        private Control BuildScoreCard()
        {
            var card = new SidebarCard("参考分")
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };

            _labelScore.Dock = DockStyle.Top;
            _labelScore.AutoSize = false;
            _labelScore.Height = 34;
            _labelScore.TextAlign = ContentAlignment.MiddleLeft;
            _labelScore.ForeColor = PdfSidebarTheme.Score;
            _labelScore.Tag = "score";
            _labelScore.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            _labelScore.Text = "参考分：—";

            var note = new Label
            {
                Dock = DockStyle.Top,
                Text = "仅反映跟读完成度，不作为发音准确率结论。",
                Font = new Font("Microsoft YaHei UI", 8.5F),
                ForeColor = PdfSidebarTheme.Muted,
                Tag = "muted",
                AutoSize = false,
                Height = 24
            };

            card.Body.Controls.Add(note);
            card.Body.Controls.Add(_labelScore);
            return card;
        }

        private Control BuildFooter()
        {
            var footer = new Panel { Dock = DockStyle.Fill, BackColor = PdfSidebarTheme.Background };

            _comboMode.Items.Clear();
            _comboMode.Items.AddRange(new object[] { "单句循环", "自动下一句", "整段连读" });
            _comboMode.SelectedIndex = 1;
            _comboMode.DropDownStyle = ComboBoxStyle.DropDownList;
            _comboMode.FlatStyle = FlatStyle.Flat;
            _comboMode.BackColor = PdfSidebarTheme.Surface;
            _comboMode.ForeColor = PdfSidebarTheme.Text;
            _comboMode.Font = new Font("Microsoft YaHei UI", 9F);
            _comboMode.Width = 124;
            _comboMode.Location = new Point(0, 10);
            _comboMode.SelectedIndexChanged += OnModeChanged;

            var hint = new Label
            {
                Text = "空格 播放 · R 重播 · → 下一句",
                Font = new Font("Microsoft YaHei UI", 8.5F),
                ForeColor = PdfSidebarTheme.Muted,
                Tag = "muted",
                AutoSize = false,
                Height = 28,
                Width = 240,
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            void LayoutHint() => hint.Location = new Point(footer.ClientSize.Width - hint.Width, 10);
            footer.Resize += (_, _) => LayoutHint();
            LayoutHint();

            footer.Controls.Add(_comboMode);
            footer.Controls.Add(hint);
            return footer;
        }

        private void OnSentenceDrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _listSentences.Items.Count)
                return;

            var row = _listSentences.Items[e.Index] as SentenceRow;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool current = row != null && row.Index == _currentIndex;
            var bounds = e.Bounds;

            using (var background = new SolidBrush(selected ? PdfSidebarTheme.BrandSoft : PdfSidebarTheme.Surface))
                e.Graphics.FillRectangle(background, bounds);

            if (current)
            {
                using var accent = new SolidBrush(PdfSidebarTheme.Brand);
                e.Graphics.FillRectangle(accent, bounds.X, bounds.Y, 3, bounds.Height);
            }

            if (row == null) return;

            Color dotColor = row.State switch
            {
                ShadowingAudioState.Synthesizing => PdfSidebarTheme.Brand,
                ShadowingAudioState.Ready => PdfSidebarTheme.Success,
                ShadowingAudioState.Failed => PdfSidebarTheme.Danger,
                _ => PdfSidebarTheme.Muted
            };

            using (var dot = new SolidBrush(dotColor))
                e.Graphics.FillEllipse(dot, bounds.X + 10, bounds.Y + 14, 8, 8);

            var indexFont = new Font("Consolas", 8.5F, FontStyle.Bold);
            TextRenderer.DrawText(e.Graphics, $"{row.Index + 1:00}", indexFont,
                new Rectangle(bounds.X + 22, bounds.Y + 9, 22, 18),
                PdfSidebarTheme.Muted, TextFormatFlags.NoPrefix);
            indexFont.Dispose();

            var textRect = new Rectangle(bounds.X + 44, bounds.Y + 6,
                Math.Max(10, bounds.Width - 92), 34);
            TextRenderer.DrawText(e.Graphics, row.Text, _listSentences.Font, textRect,
                PdfSidebarTheme.Text, TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);

            var stateFont = new Font("Microsoft YaHei UI", 8F);
            string stateText = row.State switch
            {
                ShadowingAudioState.Synthesizing => "合成中",
                ShadowingAudioState.Ready => "已就绪",
                ShadowingAudioState.Failed => "合成失败",
                _ => "待读"
            };
            TextRenderer.DrawText(e.Graphics, stateText, stateFont,
                new Rectangle(bounds.X + 44, bounds.Y + 42, 90, 16),
                PdfSidebarTheme.Muted, TextFormatFlags.NoPrefix);
            stateFont.Dispose();

            if (_practice != null && row.Index < _practice.Sentences.Count)
            {
                string id = _practice.Sentences[row.Index].Sentence.Id;
                if (_sentenceScores.TryGetValue(id, out int score))
                {
                    var badge = new Rectangle(bounds.Right - 50, bounds.Y + 8, 42, 20);
                    using var badgePath = GdiHelper.CreateRoundedRectPath(badge, 10);
                    using var badgeBrush = new SolidBrush(PdfSidebarTheme.Score);
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(badgeBrush, badgePath);
                    using var badgeFont = new Font("Microsoft YaHei UI", 8F, FontStyle.Bold);
                    using var badgeText = new SolidBrush(Color.White);
                    using var format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    e.Graphics.DrawString(score.ToString(), badgeFont, badgeText, badge, format);
                }
            }
        }

        private void FillSentenceList(IReadOnlyList<ShadowingSentence> sentences)
        {
            _listSentences.BeginUpdate();
            try
            {
                _listSentences.Items.Clear();
                for (int i = 0; i < sentences.Count; i++)
                {
                    _listSentences.Items.Add(new SentenceRow
                    {
                        Index = i,
                        Text = sentences[i].Text,
                        State = ShadowingAudioState.Pending
                    });
                }
            }
            finally
            {
                _listSentences.EndUpdate();
            }

            _totalSentences = sentences.Count;
            _progressTrack.Invalidate();

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
                _store.Save(manifest);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[SentenceShadowing] 保存分句清单失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 载入该页上次保存的句子录音路径，丢弃已不存在或已失效的句子。
        /// </summary>
        private void LoadSentenceRecordings(IReadOnlyList<ShadowingSentence> sentences)
        {
            if (string.IsNullOrWhiteSpace(_pdfPath) || sentences.Count == 0)
                return;

            try
            {
                var saved = _store.LoadSentenceRecordings(_pdfPath, _pageIndex);
                var validIds = new HashSet<string>(sentences.Select(s => s.Id), StringComparer.Ordinal);
                foreach (var pair in saved)
                {
                    if (!validIds.Contains(pair.Key) || !File.Exists(pair.Value))
                        continue;
                    _sentenceRecordings[pair.Key] = pair.Value;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[SentenceShadowing] 读取跟读录音失败: {ex.Message}");
            }
        }

        private void PersistSentenceRecordings()
        {
            if (string.IsNullOrWhiteSpace(_pdfPath))
                return;

            try
            {
                _store.SaveSentenceRecordings(_pdfPath, _pageIndex, _sentenceRecordings);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[SentenceShadowing] 保存跟读录音路径失败: {ex.Message}");
            }
        }

        private void UpdateReplayButtonState()
        {
            bool available = _practice != null
                             && _currentIndex >= 0
                             && _currentIndex < _practice.Sentences.Count
                             && _sentenceRecordings.TryGetValue(
                                 _practice.Sentences[_currentIndex].Sentence.Id, out var path)
                             && File.Exists(path);
            _btnReplayRecording.Enabled = available;
        }

        /// <summary>
        /// 恢复当前句的参考分显示：已评分过则显示分数，否则显示占位或降级说明。
        /// </summary>
        private void RefreshScoreLabel()
        {
            if (_practice == null || _currentIndex < 0 || _currentIndex >= _practice.Sentences.Count)
            {
                _labelScore.Text = "参考分：—";
                return;
            }

            string id = _practice.Sentences[_currentIndex].Sentence.Id;
            if (_sentenceScores.TryGetValue(id, out int score))
                _labelScore.Text = $"参考分：{score} / 100";
            else
                _labelScore.Text = _scorer?.IsAvailable == true
                    ? "参考分：—"
                    : "参考分：—（未安装英语识别器，按完成度评分）";
        }

        private void OnReplayRecordingClicked(object? sender, EventArgs e)
        {
            if (_practice == null || _currentIndex < 0 || _currentIndex >= _practice.Sentences.Count)
                return;

            var sentence = _practice.Sentences[_currentIndex].Sentence;
            if (!_sentenceRecordings.TryGetValue(sentence.Id, out var path) || !File.Exists(path))
            {
                _labelStatus.Text = "当前句还没有录音，可点击「🎤 录音」开始跟读。";
                return;
            }

            PlayRecording(path);
        }

        private void PlayRecording(string path)
        {
            if (_recordingPlayer == null)
            {
                _labelStatus.Text = "录音回放不可用。";
                return;
            }

            StopCountdown();
            StopPlaybackOnly();
            try
            {
                _recordingPlayer.Load(path);
                _ = _recordingPlayer.PlayAsync();
                _labelStatus.Text = "正在回放当前句录音…";
            }
            catch (Exception ex)
            {
                _labelStatus.Text = $"录音回放失败：{ex.Message}";
            }
        }

        private void OnRecordingPlaybackFinished(object? sender, EventArgs e)
        {
            if (IsHandleCreated && !IsDisposed)
                _labelStatus.Text = "录音回放结束。";
        }

        private void SetCurrentIndex(int index, bool play)
        {
            if (_practice == null || index < 0 || index >= _practice.Sentences.Count)
                return;

            _currentIndex = index;
            _listSentences.SelectedIndex = index;
            _listSentences.Invalidate();
            _progressTrack.Invalidate();
            _labelCurrent.Text = _practice.Sentences[index].Sentence.Text;
            RefreshScoreLabel();
            UpdateReplayButtonState();

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
            _recordingPlayer?.Stop();

            int index = _currentIndex < 0 ? 0 : _currentIndex;
            SetCurrentIndex(index, play: true);
        }

        private void StopAll()
        {
            StopCountdown();
            StopRecordingIfAny(suppressAdvance: true);
            _recordingPlayer?.Stop();
            _practice?.StopPlayback();
            _btnRecord.Text = "🎤 录音";
            _circularStatus.Recording = false;
            _circularStatus.Countdown = 0;
            _labelStatus.Text = "已停止。";
        }

        private async Task PlayIndexAsync(int index)
        {
            if (_practice == null || index < 0)
                return;

            _currentIndex = index;
            _listSentences.SelectedIndex = index;
            _listSentences.Invalidate();
            _progressTrack.Invalidate();
            _labelCurrent.Text = _practice.Sentences[index].Sentence.Text;
            RefreshScoreLabel();
            UpdateReplayButtonState();

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
            if (_listSentences.Items.Count > index && _listSentences.Items[index] is SentenceRow row)
            {
                row.State = state.AudioState;
                _listSentences.Invalidate();
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
            _circularStatus.Recording = false;
            _circularStatus.Countdown = _countdown;
            _labelStatus.Text = $"准备录音：{_countdown}";
            _countdownTimer.Start();
        }

        private void OnCountdownTick(object? sender, EventArgs e)
        {
            _countdown--;
            _circularStatus.Countdown = Math.Max(0, _countdown);
            if (_countdown > 0)
            {
                _labelStatus.Text = $"准备录音：{_countdown}";
                return;
            }

            int pendingIndex = _pendingIndex;
            StopCountdown();
            StartSentenceRecording(pendingIndex);
        }

        private void StopCountdown()
        {
            _countdownTimer.Stop();
            _pendingIndex = -1;
            _circularStatus.Countdown = 0;
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
                _recordingPlayer?.Stop();
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
                _circularStatus.Recording = true;
                _circularStatus.Countdown = 0;
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
            _circularStatus.Recording = false;

            int index = _recordingIndex;
            _recordingIndex = -1;
            _recordingSentenceId = null;

            if (index >= 0 && _practice != null)
            {
                var sentence = _practice.Sentences[index].Sentence;

                // 记录本句录音，供「回放录音」与下次打开窗口使用
                if (!string.IsNullOrWhiteSpace(recording.FilePath) && File.Exists(recording.FilePath))
                {
                    _sentenceRecordings[sentence.Id] = recording.FilePath;
                    PersistSentenceRecordings();
                }
                UpdateReplayButtonState();

                if (index == _currentIndex)
                    _labelScore.Text = "参考分：计算中…";

                try
                {
                    PronunciationScoreResult? result = null;
                    if (_scorer?.IsAvailable == true)
                        result = await _scorer.ScoreAsync(sentence.Text, recording.FilePath);

                    // 无 en-US 识别器、识别结果为空或录音无效时，退回按录音完成度评分
                    bool needFallback = result == null
                                        || !result.IsValid
                                        || string.IsNullOrWhiteSpace(result.RecognizedText);

                    if (needFallback)
                    {
                        int expectedMs = _practice.GetAudioDurationMs(index);
                        var fallback = TextSimilarityScorer.ScoreCompletion(expectedMs, recording.DurationMs);
                        if (fallback.IsValid)
                        {
                            result = fallback;
                            if (IsHandleCreated && index == _currentIndex)
                                _labelScore.Text = $"参考分：{fallback.Score} / 100（按完成度）";
                        }
                        else if (IsHandleCreated && index == _currentIndex)
                        {
                            _labelScore.Text = "参考分：—（录音或原音时长不足）";
                        }
                    }
                    else if (IsHandleCreated && index == _currentIndex)
                    {
                        string missing = result!.MissingWords.Count > 0
                            ? $"（漏读 {result.MissingWords.Count} 词）"
                            : string.Empty;
                        _labelScore.Text = $"参考分：{result.Score} / 100{missing}";
                    }

                    if (result != null && result.IsValid)
                    {
                        _sentenceScores[sentence.Id] = result.Score;
                        if (IsHandleCreated) _listSentences.Invalidate();
                    }
                }
                catch (Exception ex)
                {
                    if (IsHandleCreated && index == _currentIndex)
                        _labelScore.Text = $"评分失败：{ex.Message}";
                }
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
            PdfSidebarTheme.ThemeChanged -= OnSidebarThemeChanged;

            StopRecordingIfAny(suppressAdvance: true);

            if (_recorder != null && _recordingHandler != null)
                _recorder.RecordingCompleted -= _recordingHandler;

            if (_recordingPlayer != null)
            {
                _recordingPlayer.PlaybackFinished -= OnRecordingPlaybackFinished;
                _recordingPlayer.Dispose();
                _recordingPlayer = null;
            }

            if (_practice != null)
            {
                _practice.StateChanged -= OnSentenceStateChanged;
                _practice.SentenceFinished -= OnSentenceFinished;
                _practice.ErrorOccurred -= OnPracticeError;
                _practice.Dispose();
            }
        }

        private void OnSidebarThemeChanged(object? sender, EventArgs e)
        {
            if (!IsHandleCreated || IsDisposed) return;
            BackColor = PdfSidebarTheme.Background;
            PdfSidebarTheme.ApplyRecursive(this);
            Invalidate(true);
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
