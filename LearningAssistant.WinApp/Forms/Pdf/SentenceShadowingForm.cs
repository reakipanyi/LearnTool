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
    public sealed partial class SentenceShadowingForm : Form
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
        private readonly ToolTip _buttonTip = new();
        private readonly Dictionary<string, string> _sentenceRecordings = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _sentenceScores = new(StringComparer.Ordinal);
        private readonly ShadowingStore _store = new();
        private readonly ShadowingSegmentStore _segmentStore = new();
        private readonly FlowLayoutPanel _takeActions = new();
        private readonly Button _btnTakePreview = new();
        private readonly Button _btnTakeChoose = new();
        private readonly Button _btnTakeDelete = new();
        private readonly WaveformThumb _waveform = new();
        private ShadowingSegmentManifest? _segmentManifest;
        private IReadOnlyList<ShadowingSegment> _segments = Array.Empty<ShadowingSegment>();
        private IReadOnlyList<string?>? _practicePresets;
        private bool _segmentMode;
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

        public SentenceShadowingForm(string pdfPath, int pageIndex, string pageText, string documentTitle, bool isDark = false)
        {
            _pdfPath = pdfPath;
            _pageIndex = pageIndex;

            InitializeComponent();

            Text = $"句子跟读 - {documentTitle}";
            BackColor = PdfSidebarTheme.Background;
            _labelInfo.Text = $"第 {_pageIndex + 1} 页";
            _comboMode.Items.AddRange(new object[] { "单句循环", "自动下一句", "整段连读" });
            _comboMode.SelectedIndex = 1;

            MountCardContents();
            WireEvents();

            var sentences = SentenceSegmenter.Segment(pageText);
            _practicePresets = TryLoadSegments();
            var practiceSentences = _segmentMode ? BuildSegmentSentences() : sentences;
            if (_segmentMode)
            {
                _labelTitle.Text = "原声跟读";
                Text = $"原声跟读 - {documentTitle}";
                _takeActions.Visible = true;
            }

            PersistManifest(sentences);

            LoadSentenceRecordings(practiceSentences);
            LoadSentenceScores(practiceSentences);
            FillSentenceList(practiceSentences);
            PdfSidebarTheme.ThemeChanged += OnSidebarThemeChanged;
            if (isDark)
            {
                BackColor = new PdfSidebarPalette(true).Background;
                PdfSidebarTheme.ApplyRecursive(this, true);
            }

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

            if (subtitleParser == null)
            {
                _labelStatus.Text = "音频播放组件不可用，暂时无法播放。";
                _btnPlay.Enabled = false;
            }
            else
            {
                // tts 允许为空：原声切片模式不依赖语音合成，缺失切片时再回退 TTS
                _practice = new ShadowingPracticeService(practiceSentences, tts, subtitleParser, _practicePresets);
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

            if (practiceSentences.Count > 0)
                SetCurrentIndex(ResolveInitialIndex(), play: false);

            _countdownTimer.Tick += OnCountdownTick;
            _recordLimitTimer.Tick += OnRecordLimitReached;

            Shown += OnFormShown;
            FormClosed += OnFormClosed;
        }

        /// <summary>
        /// 优先载入本页打点切片；加载失败或没有切片时保持旧的分句模式。
        /// 返回切片模式下的预设音频路径（null 表示无切片，需回退 TTS）。
        /// </summary>
        private IReadOnlyList<string?>? TryLoadSegments()
        {
            if (string.IsNullOrWhiteSpace(_pdfPath))
                return null;

            try
            {
                var manifest = _segmentStore.LoadSegments(_pdfPath, _pageIndex);
                if (manifest?.Segments.Count > 0)
                {
                    _segmentManifest = manifest;
                    _segments = manifest.Segments.OrderBy(s => s.StartMs).ToList();
                    _segmentMode = true;
                    foreach (var segment in _segments)
                        SyncSegmentTake(segment);
                    return _segments
                        .Select(s =>
                        {
                            string path = _segmentStore.ResolvePath(s.SlicePath);
                            return File.Exists(path) ? path : null;
                        })
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[SentenceShadowing] 读取原声切片失败: {ex.Message}");
            }

            return null;
        }

        private List<ShadowingSentence> BuildSegmentSentences()
            => _segments.Select((segment, index) => new ShadowingSentence
            {
                Id = segment.Id,
                Index = index,
                Text = string.IsNullOrWhiteSpace(segment.Text)
                    ? $"第 {index + 1} 段 · {ShadowingMarkSession.FormatTimestamp(segment.StartMs)}"
                    : segment.Text!
            }).ToList();

        /// <summary>恢复该页上次练到的切片与播放模式。</summary>
        private int ResolveInitialIndex()
        {
            if (!_segmentMode)
                return 0;

            try
            {
                var progress = _segmentStore.LoadProgress(_pdfPath);
                if (progress?.Pages.TryGetValue(_pageIndex.ToString(), out var page) != true)
                    return 0;

                if (!string.IsNullOrWhiteSpace(page.Mode))
                    _comboMode.SelectedIndex = page.Mode switch
                    {
                        "SingleLoop" => 0,
                        "Continuous" => 2,
                        _ => 1
                    };

                if (!string.IsNullOrWhiteSpace(page.LastSegmentId))
                {
                    for (int i = 0; i < _segments.Count; i++)
                    {
                        if (string.Equals(_segments[i].Id, page.LastSegmentId, StringComparison.Ordinal))
                            return i;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[SentenceShadowing] 读取跟读进度失败: {ex.Message}");
            }

            return 0;
        }

        /// <summary>优先采用手动选中的 take，否则取最高分（同分取最新）。</summary>
        private static ShadowingTake? CurrentTake(ShadowingSegment segment)
        {
            var manual = segment.Takes.FirstOrDefault(t => t.ChosenManual);
            if (manual != null)
                return manual;
            return segment.Takes.FirstOrDefault(t => t.Chosen)
                   ?? segment.Takes.OrderByDescending(t => t.CreatedAt).FirstOrDefault();
        }

        private ShadowingSegment? CurrentSegment()
        {
            if (!_segmentMode || _practice == null || _currentIndex < 0)
                return null;
            string id = _practice.Sentences[_currentIndex].Sentence.Id;
            return _segments.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.Ordinal));
        }

        /// <summary>把该段当前 take 的路径与分数同步到列表字典，供行徽标与回放使用。</summary>
        private void SyncSegmentTake(ShadowingSegment segment)
        {
            var take = CurrentTake(segment);
            if (take == null)
            {
                _sentenceRecordings.Remove(segment.Id);
                _sentenceScores.Remove(segment.Id);
                return;
            }

            string path = _segmentStore.ResolvePath(take.Path);
            if (File.Exists(path))
                _sentenceRecordings[segment.Id] = path;
            else
                _sentenceRecordings.Remove(segment.Id);

            if (take.Score.HasValue)
                _sentenceScores[segment.Id] = take.Score.Value;
            else
                _sentenceScores.Remove(segment.Id);
        }

        private async Task SaveSegmentManifestAsync()
        {
            if (!_segmentMode || _segmentManifest == null)
                return;

            _segmentManifest.Segments = _segments.ToList();
            await _segmentStore.SaveSegmentsAsync(_pdfPath, _segmentManifest).ConfigureAwait(true);
        }

        /// <summary>结束/切换时记录本页进度，失败不影响练习。</summary>
        private void PersistPracticeProgress()
        {
            // 构造期恢复模式时会先触发一次，此时还没选中段，避免把已有进度覆盖成空
            if (!_segmentMode || _currentIndex < 0)
                return;
            string? id = CurrentSegment()?.Id;
            FireAndForget(_segmentStore.SaveProgressPageAsync(
                _pdfPath, _pageIndex, id, _mode.ToString()));
        }

        /// <summary>把各卡片内容挂到卡片 Body：VS 设计器回写会丢弃 Body.Controls.Add，因此放在构造函数。</summary>
        private void MountCardContents()
        {
            MountBodyChild(listCard, _listSentences);
            MountBodyChild(currentCard, _labelCurrent);
            MountBodyChild(currentCard, navigation);
            MountBodyChild(practiceCard, grid);

            // 波形贴卡片底部，先加入避免压住上面的分数与说明
            _waveform.Dock = DockStyle.Bottom;
            _waveform.Margin = new Padding(10, 0, 10, 6);
            MountBodyChild(scoreCard, _waveform);
            MountBodyChild(scoreCard, note);
            MountBodyChild(scoreCard, _labelScore);

            MountTakeActions();
        }

        /// <summary>在录音行与状态行之间插入 take 的试听/采用/删除操作，不改 Designer 以免设计器回写丢失。</summary>
        private void MountTakeActions()
        {
            _takeActions.Dock = DockStyle.Fill;
            _takeActions.FlowDirection = FlowDirection.LeftToRight;
            _takeActions.WrapContents = false;
            _takeActions.Margin = new Padding(0, 0, 0, 2);
            _takeActions.BackColor = Color.Transparent;
            _takeActions.Visible = false;
            var palette = PdfSidebarTheme.PaletteFor(this);
            foreach (var button in new[] { _btnTakePreview, _btnTakeChoose, _btnTakeDelete })
            {
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 0;
                button.Font = new Font("Microsoft YaHei UI", 8.5F);
                button.Size = new Size(64, 26);
                button.Margin = new Padding(0, 0, 8, 0);
                button.Cursor = Cursors.Hand;
                button.BackColor = palette.SurfaceAlt;
                button.ForeColor = palette.Text;
                _takeActions.Controls.Add(button);
            }
            _btnTakePreview.Text = "试听";
            _btnTakeChoose.Text = "采用";
            _btnTakeDelete.Text = "删除";

            if (actions.Controls.Contains(_labelStatus))
                actions.Controls.Remove(_labelStatus);
            actions.RowCount = 4;
            actions.RowStyles.Clear();
            actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            actions.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            actions.Controls.Add(_takeActions, 0, 2);
            actions.Controls.Add(_labelStatus, 0, 3);
            _takeActions.Visible = _segmentMode;
        }

        private static void MountBodyChild(SidebarCard card, Control child)
        {
            if (child.Parent == null)
                card.Body.Controls.Add(child);
        }

        private void WireEvents()
        {
            _listSentences.SelectedIndexChanged += OnSentenceSelected;
            _listSentences.DoubleClick += OnSentenceDoubleClick;
            _listSentences.DrawItem += OnSentenceDrawItem;

            btnPrev.Click += (_, _) => MoveSentence(-1);
            _btnPlay.Click += (_, _) => StartPlayback();
            btnNext.Click += (_, _) => MoveSentence(1);
            btnStop.Click += (_, _) => StopAll();

            _btnRecord.Click += OnRecordButtonClicked;
            _btnReplayRecording.Click += OnReplayRecordingClicked;
            _btnTakePreview.Click += (_, _) => PreviewCurrentTake();
            _btnTakeChoose.Click += async (_, _) => await ChooseCurrentTakeAsync();
            _btnTakeDelete.Click += async (_, _) => await DeleteCurrentTakeAsync();
            _comboMode.SelectedIndexChanged += OnModeChanged;

            _progressTrack.Paint += ProgressTrack_Paint;

            _buttonTip.SetToolTip(btnPrev, "上一句");
            _buttonTip.SetToolTip(btnNext, "下一句");

            footer.Resize += (_, _) => LayoutHint();
            LayoutHint();
        }

        private void LayoutHint() => hint.Location = new Point(footer.ClientSize.Width - hint.Width, 10);

        private void ProgressTrack_Paint(object? sender, PaintEventArgs e)
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
        }

        private void OnSentenceDrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _listSentences.Items.Count)
                return;

            var row = _listSentences.Items[e.Index] as SentenceRow;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool current = row != null && row.Index == _currentIndex;
            var bounds = e.Bounds;

            var palette = PdfSidebarTheme.PaletteFor(_listSentences);

            using (var background = new SolidBrush(selected ? palette.BrandSoft : palette.Surface))
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
                _ => palette.Muted
            };

            using (var dot = new SolidBrush(dotColor))
                e.Graphics.FillEllipse(dot, bounds.X + 12, bounds.Y + 19, 10, 10);

            var indexFont = new Font("Consolas", 10.5F, FontStyle.Bold);
            TextRenderer.DrawText(e.Graphics, $"{row.Index + 1:00}", indexFont,
                new Rectangle(bounds.X + 28, bounds.Y + 15, 28, 22),
                palette.Muted, TextFormatFlags.NoPrefix);
            indexFont.Dispose();

            var textRect = new Rectangle(bounds.X + 62, bounds.Y + 8,
                Math.Max(10, bounds.Width - 124), 50);
            TextRenderer.DrawText(e.Graphics, row.Text, _listSentences.Font, textRect,
                palette.Text, TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);

            var stateFont = new Font("Microsoft YaHei UI", 9.5F);
            string stateText = row.State switch
            {
                ShadowingAudioState.Synthesizing => "合成中",
                ShadowingAudioState.Ready => "已就绪",
                ShadowingAudioState.Failed => "合成失败",
                _ => "待读"
            };
            if (_segmentMode)
            {
                stateText = row.State switch
                {
                    ShadowingAudioState.Ready => "原声切片",
                    ShadowingAudioState.Failed => "缺切片",
                    ShadowingAudioState.Synthesizing => "回退合成中",
                    _ => "待读"
                };
                if (_practice != null && row.Index < _practice.Sentences.Count)
                {
                    var state = _practice.Sentences[row.Index];
                    if (state.IsTtsFallback)
                        stateText = "TTS 回退";
                    else if (row.Index < _segments.Count && _segments[row.Index].Takes.Count > 0)
                        stateText += $" · {_segments[row.Index].Takes.Count} 次";
                }
            }
            TextRenderer.DrawText(e.Graphics, stateText, stateFont,
                new Rectangle(bounds.X + 62, bounds.Y + 56, 110, 18),
                palette.Muted, TextFormatFlags.NoPrefix);
            stateFont.Dispose();

            if (_practice != null && row.Index < _practice.Sentences.Count)
            {
                string id = _practice.Sentences[row.Index].Sentence.Id;
                if (_sentenceScores.TryGetValue(id, out int score))
                {
                    var badge = new Rectangle(bounds.Right - 58, bounds.Y + 10, 48, 22);
                    using var badgePath = GdiHelper.CreateRoundedRectPath(badge, 11);
                    using var badgeBrush = new SolidBrush(PdfSidebarTheme.Score);
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(badgeBrush, badgePath);
                    using var badgeFont = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
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
                    var state = ShadowingAudioState.Pending;
                    if (_segmentMode && i < _segments.Count)
                    {
                        string slice = _segmentStore.ResolvePath(_segments[i].SlicePath);
                        state = File.Exists(slice) ? ShadowingAudioState.Ready : ShadowingAudioState.Failed;
                    }

                    _listSentences.Items.Add(new SentenceRow
                    {
                        Index = i,
                        Text = sentences[i].Text,
                        State = state
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
                : _segmentMode
                    ? $"第 {_pageIndex + 1} 页 · 共 {sentences.Count} 段原声"
                    : $"第 {_pageIndex + 1} 页 · 共 {sentences.Count} 句";

            if (sentences.Count == 0)
                _labelStatus.Text = "当前页面没有可分句的文本，请先执行 OCR 或翻译取得文本。";
            else if (_segmentMode)
                _labelStatus.Text = "已载入原声切片，可逐段跟读或录音。";
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

        /// <summary>
        /// 载入该页上次保存的句子参考分，丢弃已不存在句子的评分。
        /// </summary>
        private void LoadSentenceScores(IReadOnlyList<ShadowingSentence> sentences)
        {
            if (string.IsNullOrWhiteSpace(_pdfPath) || sentences.Count == 0)
                return;

            try
            {
                var saved = _store.LoadSentenceScores(_pdfPath, _pageIndex);
                var validIds = new HashSet<string>(sentences.Select(s => s.Id), StringComparer.Ordinal);
                foreach (var pair in saved)
                {
                    if (!validIds.Contains(pair.Key) || pair.Value < 0 || pair.Value > 100)
                        continue;
                    _sentenceScores[pair.Key] = pair.Value;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[SentenceShadowing] 读取跟读评分失败: {ex.Message}");
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

        private void PersistSentenceScores()
        {
            if (string.IsNullOrWhiteSpace(_pdfPath))
                return;

            try
            {
                _store.SaveSentenceScores(_pdfPath, _pageIndex, _sentenceScores);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[SentenceShadowing] 保存跟读评分失败: {ex.Message}");
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
            UpdateTakeButtons();
        }

        private void UpdateTakeButtons()
        {
            if (!_segmentMode)
            {
                _takeActions.Visible = false;
                return;
            }

            _takeActions.Visible = true;
            var segment = CurrentSegment();
            bool hasTake = segment != null && CurrentTake(segment) != null;
            _btnTakePreview.Enabled = hasTake;
            _btnTakeDelete.Enabled = hasTake;
            _btnTakeChoose.Enabled = hasTake && CurrentTake(segment!)!.ChosenManual == false;
        }

        private void PreviewCurrentTake()
        {
            var take = CurrentSegment() is { } segment ? CurrentTake(segment) : null;
            if (take == null)
            {
                _labelStatus.Text = "该段还没有跟读录音。";
                return;
            }

            string path = _segmentStore.ResolvePath(take.Path);
            if (!File.Exists(path))
            {
                _labelStatus.Text = "录音文件已不存在。";
                return;
            }
            PlayRecording(path);
        }

        private async Task ChooseCurrentTakeAsync()
        {
            var segment = CurrentSegment();
            var take = segment == null ? null : CurrentTake(segment);
            if (segment == null || take == null)
                return;

            foreach (var item in segment.Takes)
            {
                item.Chosen = ReferenceEquals(item, take);
                item.ChosenManual = ReferenceEquals(item, take);
            }
            SyncSegmentTake(segment);
            RefreshTakeUi();
            _labelStatus.Text = "已采用这次跟读。";

            try
            {
                await SaveSegmentManifestAsync();
            }
            catch (Exception ex)
            {
                _labelStatus.Text = $"保存采用结果失败：{ex.Message}";
            }
        }

        private async Task DeleteCurrentTakeAsync()
        {
            var segment = CurrentSegment();
            var take = segment == null ? null : CurrentTake(segment);
            if (segment == null || take == null)
                return;

            var confirm = MessageBox.Show(
                this, "删除这次跟读录音？删除后不可恢复。", "删除跟读",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes)
                return;

            try
            {
                string path = _segmentStore.ResolvePath(take.Path);
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                _labelStatus.Text = $"删除录音文件失败：{ex.Message}";
            }

            segment.Takes.Remove(take);
            SelectBestTake(segment);
            SyncSegmentTake(segment);
            RefreshTakeUi();
            _labelStatus.Text = "已删除这次跟读。";

            try
            {
                await SaveSegmentManifestAsync();
            }
            catch (Exception ex)
            {
                _labelStatus.Text = $"保存删除结果失败：{ex.Message}";
            }
        }

        /// <summary>手动采用优先；否则取最高分，同分保留最新。</summary>
        private static void SelectBestTake(ShadowingSegment segment)
        {
            if (segment.Takes.Count == 0)
                return;

            var manual = segment.Takes.FirstOrDefault(t => t.ChosenManual);
            var best = manual ?? segment.Takes
                .OrderBy(t => t.Score ?? -1)
                .ThenBy(t => t.CreatedAt)
                .Last();
            foreach (var take in segment.Takes)
                take.Chosen = ReferenceEquals(take, best);
        }

        private void RefreshTakeUi()
        {
            _listSentences.Invalidate();
            RefreshScoreLabel();
            UpdateReplayButtonState();
            RefreshWaveform();
        }

        /// <summary>录音完成后刷新波形缩略图：仅切片模式展示当前采用 take。</summary>
        private void RefreshWaveform()
        {
            if (!_segmentMode)
            {
                _waveform.Clear();
                return;
            }

            var take = CurrentSegment() is { } segment ? CurrentTake(segment) : null;
            string? path = take == null ? null : _segmentStore.ResolvePath(take.Path);
            _waveform.LoadFromFile(path);
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
            RefreshWaveform();
            PersistPracticeProgress();

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
            PersistPracticeProgress();
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

            if (index >= 0 && _practice != null && _segmentMode)
            {
                await HandleSegmentRecordingAsync(index, recording);
            }
            else if (index >= 0 && _practice != null)
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
                        PersistSentenceScores();
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

        /// <summary>
        /// 切片模式录音落库：把临时 wav 移到 takes/，评分、限 3 次、自动选优并写回 segments.json。
        /// </summary>
        private async Task HandleSegmentRecordingAsync(int index, AudioRecording recording)
        {
            if (_practice == null)
                return;

            var sentence = _practice.Sentences[index].Sentence;
            var segment = _segments.FirstOrDefault(s => string.Equals(s.Id, sentence.Id, StringComparison.Ordinal));
            if (segment == null)
            {
                _labelStatus.Text = "找不到该时间戳对应的原声切片。";
                return;
            }

            if (string.IsNullOrWhiteSpace(recording.FilePath) || !File.Exists(recording.FilePath))
            {
                _labelStatus.Text = "录音文件缺失，未保存这次跟读。";
                return;
            }

            string destination;
            try
            {
                string relative = _segmentStore.AllocateTakeRelativePath(segment.Id, recording.CreatedAt);
                destination = _segmentStore.ResolvePath(relative);
                string? directory = Path.GetDirectoryName(destination);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);
                File.Move(recording.FilePath, destination, overwrite: true);
            }
            catch (Exception ex)
            {
                _labelStatus.Text = $"保存跟读录音失败：{ex.Message}";
                return;
            }

            int? score = null;
            string scoreKind = "reference";
            string scoreText = "参考分：—";
            int expectedMs = _practice.GetAudioDurationMs(index);
            if (expectedMs <= 0)
                expectedMs = Math.Max(0, segment.EndMs - segment.StartMs);

            var spokenText = segment.Text ?? string.Empty;
            PronunciationScoreResult? result = null;
            if (_scorer?.IsAvailable == true && !string.IsNullOrWhiteSpace(spokenText))
            {
                try
                {
                    result = await _scorer.ScoreAsync(spokenText, destination);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning($"[SentenceShadowing] 切片评分失败: {ex.Message}");
                }
            }

            bool needFallback = result == null || !result.IsValid
                                || string.IsNullOrWhiteSpace(result.RecognizedText);
            if (needFallback)
            {
                var fallback = TextSimilarityScorer.ScoreCompletion(expectedMs, recording.DurationMs);
                if (fallback.IsValid)
                {
                    result = fallback;
                    scoreText = $"参考分：{fallback.Score} / 100（按完成度）";
                }
            }
            else
            {
                scoreKind = "pronunciation";
                string missing = result!.MissingWords.Count > 0
                    ? $"（漏读 {result.MissingWords.Count} 词）"
                    : string.Empty;
                scoreText = $"参考分：{result.Score} / 100{missing}";
            }

            if (result != null && result.IsValid)
                score = result.Score;

            var take = new ShadowingTake
            {
                Id = ShadowingSegmentStore.CreateTakeId(_pageIndex),
                Path = _segmentStore.ToRelativePath(destination),
                DurationMs = recording.DurationMs,
                Score = score,
                ScoreKind = scoreKind,
                CreatedAt = recording.CreatedAt
            };
            segment.Takes.Add(take);
            TrimTakes(segment);
            SelectBestTake(segment);
            SyncSegmentTake(segment);
            RefreshTakeUi();

            if (index == _currentIndex)
                _labelScore.Text = scoreText;
            _labelStatus.Text = $"已保存第 {segment.Takes.Count} 次跟读"
                                + (score.HasValue ? $" · 参考分 {score}" : string.Empty);

            try
            {
                await SaveSegmentManifestAsync();
            }
            catch (Exception ex)
            {
                _labelStatus.Text = $"保存切片清单失败：{ex.Message}";
            }
        }

        /// <summary>每段最多保留 3 次；优先淘汰未采用的旧 take。</summary>
        private void TrimTakes(ShadowingSegment segment)
        {
            while (segment.Takes.Count > 3)
            {
                var victim = segment.Takes
                                 .OrderBy(t => t.CreatedAt)
                                 .FirstOrDefault(t => !t.Chosen)
                             ?? segment.Takes.OrderBy(t => t.CreatedAt).First();
                segment.Takes.Remove(victim);
                string path = _segmentStore.ResolvePath(victim.Path);
                try
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch
                {
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
            bool isDark = PdfSidebarTheme.IsDarkFor(this);
            BackColor = new PdfSidebarPalette(isDark).Background;
            PdfSidebarTheme.ApplyRecursive(this, isDark);
            var palette = PdfSidebarTheme.PaletteFor(this);
            foreach (var button in new[] { _btnTakePreview, _btnTakeChoose, _btnTakeDelete })
            {
                button.BackColor = palette.SurfaceAlt;
                button.ForeColor = palette.Text;
            }
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
