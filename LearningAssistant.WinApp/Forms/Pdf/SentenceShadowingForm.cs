using System.Diagnostics;
using LearningAssistant.Abstractions.Media;
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
        private readonly string _documentTitle;
        private readonly List<ShadowingSentence> _textSentences;
        private readonly bool _wholeBook;
        private readonly List<int> _sentencePageIndexes = new();
        private readonly Dictionary<string, int> _sentencePageById = new(StringComparer.Ordinal);
        private ShadowingPracticeService? _practice;
        private readonly IAudioRecorderService? _recorder;
        private readonly IPronunciationScorer? _scorer;
        private readonly EventHandler<AudioRecording>? _recordingHandler;
        private readonly ToolTip _buttonTip = new();
        private readonly Dictionary<string, string> _sentenceRecordings = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _sentenceScores = new(StringComparer.Ordinal);
        private readonly ShadowingStore _store = new();
        private readonly ShadowingSegmentStore _segmentStore = new();
        private readonly FlowLayoutPanel _takeActions = new();
        private readonly FlowLayoutPanel _sourceBar = new();
        private readonly Label _sourceLabel = new();
        private readonly RadioButton _radioSourceSegments = new();
        private readonly RadioButton _radioSourceText = new();
        private readonly Button _btnClearSegments = new();
        private readonly Button _btnTakePreview = new();
        private readonly Button _btnTakeChoose = new();
        private readonly Button _btnTakeDelete = new();
        private readonly Button _btnCompose = new();
        private readonly WaveformThumb _waveform = new();
        private readonly ShadowingComposeService _composer = new();
        private ShadowingSegmentManifest? _segmentManifest;
        private IReadOnlyList<ShadowingSegment> _segments = Array.Empty<ShadowingSegment>();
        private IReadOnlyList<string?>? _practicePresets;
        private bool _segmentMode;
        private bool _sourceSwitching;
        private ITTSService? _tts;
        private ISubtitleParser? _subtitleParser;
        private NAudioMediaPlayer? _recordingPlayer;
        private readonly ShadowingAlignmentContext? _alignmentContext;
        private readonly IReadOnlyList<ShadowingCue> _alignmentCues;
        /// <summary>加载/保存对账后的 cue↔切片绑定表；与 _alignmentCues 等长同序，无对齐上下文时为空。</summary>
        private IReadOnlyList<CueBinding> _bindings = Array.Empty<CueBinding>();
        /// <summary>最近一次对账结果（含 Orphans/Duplicates），供状态提示与修复入口使用。</summary>
        private ReconcileResult? _bindingsDrift;
        /// <summary>对齐句数与页面句数不一致时置真：不再清空 cues，保留可对齐句并提示核对。</summary>
        private bool _cueCountMismatch;
        private readonly string? _initialSentenceText;
        private readonly AudioSliceService _audioSliceService = new();
        private readonly Button _btnAlign = new();
        private NAudioMediaPlayer? _alignmentPlayer;
        private ShadowingAlignmentSession? _alignmentSession;
        private bool _aligning;
        private bool _finishingAlignment;
        private bool _suppressListSelection;
        private long _lastAlignmentPositionTick;
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
            public int PageIndex { get; init; }
            public string Text { get; init; } = string.Empty;
            public ShadowingAudioState State { get; set; } = ShadowingAudioState.Pending;
            /// <summary>切片模式的对账徽标状态；null 表示无对账（文字模式/无对齐上下文/整本）。</summary>
            public CueBindingStatus? BindingStatus { get; init; }
            public CueUnboundReason? UnboundReason { get; init; }
        }

        /// <summary>是否为整本高亮跟读模式。</summary>
        public bool IsWholeBook => _wholeBook;

        public SentenceShadowingForm(
            string pdfPath,
            int pageIndex,
            string pageText,
            string documentTitle,
            bool isDark = false,
            ShadowingAlignmentContext? alignment = null,
            string? initialSentenceText = null)
            : this(pdfPath, pageIndex, SentenceSegmenter.Segment(pageText), null, false, documentTitle, isDark, alignment, initialSentenceText)
        {
        }

        /// <summary>
        /// 整本跟读：按全书高亮顺序逐条练习，每条录音与评分仍写回所属页。
        /// </summary>
        public SentenceShadowingForm(string pdfPath, IReadOnlyList<ShadowingHighlightSentence> highlights, int startPageIndex, string documentTitle, bool isDark = false)
            : this(
                pdfPath,
                startPageIndex,
                highlights.Select(h => h.Sentence).ToList(),
                highlights.Select(h => h.PageIndex).ToList(),
                true,
                documentTitle,
                isDark,
                null)
        {
        }

        private SentenceShadowingForm(
            string pdfPath,
            int pageIndex,
            List<ShadowingSentence> textSentences,
            IReadOnlyList<int>? sentencePageIndexes,
            bool wholeBook,
            string documentTitle,
            bool isDark,
            ShadowingAlignmentContext? alignment,
            string? initialSentenceText = null)
        {
            _pdfPath = pdfPath;
            _pageIndex = pageIndex;
            _documentTitle = documentTitle;
            _textSentences = textSentences;
            _wholeBook = wholeBook;
            // 整本模式忽略单句定位，避免跨页句号错位
            _initialSentenceText = wholeBook ? null : initialSentenceText;
            _alignmentContext = wholeBook ? null : alignment;

            for (int i = 0; i < _textSentences.Count; i++)
            {
                int sentencePage = sentencePageIndexes != null && i < sentencePageIndexes.Count
                    ? sentencePageIndexes[i]
                    : pageIndex;
                _sentencePageIndexes.Add(sentencePage);
                _sentencePageById[_textSentences[i].Id] = sentencePage;
            }

            // 本页原声对齐句与页面分句一一对应；没有音频上下文时只保留文字跟读
            var cues = _alignmentContext == null
                ? Array.Empty<ShadowingCue>()
                : ShadowingHighlightSource.BuildPageCues(
                    _textSentences, _alignmentContext.Highlights, _pageIndex);
            // 数量不等时不再清空 cues：保留可对齐句，记录差异并提示核对
            _alignmentCues = cues;
            _cueCountMismatch = _alignmentContext != null && cues.Count != _textSentences.Count;
            if (_cueCountMismatch)
            {
                Trace.TraceWarning(
                    $"[SentenceShadowing] 对齐句数({cues.Count})与页面句数({_textSentences.Count})不一致，仍按对齐句继续。");
            }

            InitializeComponent();

            Text = _wholeBook ? $"整本跟读 - {documentTitle}" : $"句子跟读 - {documentTitle}";
            BackColor = PdfSidebarTheme.Background;
            _labelInfo.Text = _wholeBook ? "整本高亮" : $"第 {_pageIndex + 1} 页";
            _comboMode.Items.AddRange(new object[] { "单句循环", "自动下一句", "整段连读" });
            _comboMode.SelectedIndex = 1;

            MountCardContents();
            MountSourceBar();
            WireEvents();

            // 整本模式使用高亮原文，不读取单页原声切片，也不写入单页分句清单
            if (!_wholeBook)
            {
                LoadSegments();
                _segmentMode = _segments.Count > 0;
                PersistManifest(_textSentences);
            }

            PdfSidebarTheme.ThemeChanged += OnSidebarThemeChanged;
            if (isDark)
            {
                BackColor = new PdfSidebarPalette(true).Background;
                PdfSidebarTheme.ApplyRecursive(this, true);
            }

            // 先解析服务，再按当前来源创建练习实例
            _tts = Program.ServiceProvider?.GetService(typeof(ITTSService)) as ITTSService;
            _subtitleParser = Program.ServiceProvider?.GetService(typeof(ISubtitleParser)) as ISubtitleParser;
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

            if (_subtitleParser != null)
            {
                _recordingPlayer = new NAudioMediaPlayer(_subtitleParser);
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

            ApplyActiveSource();

            if (_subtitleParser == null)
            {
                _labelStatus.Text = "音频播放组件不可用，暂时无法播放。";
                _btnPlay.Enabled = false;
            }

            _countdownTimer.Tick += OnCountdownTick;
            _recordLimitTimer.Tick += OnRecordLimitReached;

            Shown += OnFormShown;
            FormClosed += OnFormClosed;
        }

        /// <summary>
        /// 载入本页打点切片清单；没有切片时保持文字跟读。
        /// </summary>
        private void LoadSegments()
        {
            if (string.IsNullOrWhiteSpace(_pdfPath))
                return;

            try
            {
                var manifest = _segmentStore.LoadSegments(_pdfPath, _pageIndex);
                if (manifest?.Segments.Count > 0)
                {
                    _segmentManifest = manifest;
                    _segments = manifest.Segments.OrderBy(s => s.StartMs).ToList();
                }

                // 加载只读对账：不写盘、不删除孤立切片，只生成绑定表供列表与状态提示
                RecomputeBindings();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[SentenceShadowing] 读取原声切片失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 只读对账：以 cue 为唯一真源生成绑定表与漂移摘要；不写盘、不删除 Orphan。
        /// alignedOnce 不能用 _segments.Count > 0 判定（只剩孤儿切片时会误判为“已跳过”），
        /// 以 ShadowingProgress.Pages[page].LastSegmentId 为准；旧数据无进度记录时回退 segments.Count > 0。
        /// </summary>
        private void RecomputeBindings()
        {
            if (_alignmentCues.Count == 0)
            {
                _bindings = Array.Empty<CueBinding>();
                _bindingsDrift = null;
                return;
            }

            var progress = _segmentStore.LoadProgress(_pdfPath);
            bool alignedOnce = !string.IsNullOrWhiteSpace(
                    progress?.Pages.GetValueOrDefault(_pageIndex.ToString())?.LastSegmentId)
                || (progress == null && _segments.Count > 0);

            var result = ShadowingCueReconciler.Reconcile(
                _alignmentCues,
                _segments,
                s => File.Exists(_segmentStore.ResolvePath(s.SlicePath)),
                alignedOnce: alignedOnce);
            _bindings = result.Bindings;
            _bindingsDrift = result;
        }

        /// <summary>当前来源的行列表：行恒等于 cue（I1）；无对齐上下文时回退页面分句。</summary>
        private IReadOnlyList<ShadowingSentence> ActiveSentences()
        {
            if (_segmentMode)
                return BuildSegmentSentences();
            return _alignmentCues.Count > 0 ? BuildCueSentences() : _textSentences;
        }

        /// <summary>文字模式行同样由 cue 生成：Id = cue.AnchorKey（录音/评分键），Text = cue.Text。</summary>
        private List<ShadowingSentence> BuildCueSentences()
            => _alignmentCues.Select((cue, index) => new ShadowingSentence
            {
                Id = cue.AnchorKey,
                Index = index,
                Text = cue.Text
            }).ToList();

        /// <summary>切片模式下的预设音频路径：按 cue 顺序取绑定切片，未绑定/缺文件为 null（回退 TTS）。</summary>
        private IReadOnlyList<string?> BuildSegmentPresets()
        {
            string? Resolve(ShadowingSegment? segment)
            {
                if (segment == null)
                    return null;
                string path = _segmentStore.ResolvePath(segment.SlicePath);
                return File.Exists(path) ? path : null;
            }

            return _bindings.Count > 0
                ? _bindings.Select(b => Resolve(b.Segment)).ToList()
                : _segments.Select(s => Resolve(s)).ToList();
        }

        /// <summary>按当前来源重建练习服务、列表与录音/评分缓存。</summary>
        private void ApplyActiveSource(bool play = false)
        {
            _sentenceRecordings.Clear();
            _sentenceScores.Clear();

            var sentences = ActiveSentences();
            _practicePresets = _segmentMode ? BuildSegmentPresets() : null;
            RebuildPractice(sentences);

            if (_segmentMode)
            {
                foreach (var segment in _segments)
                    SyncSegmentTake(segment);
            }

            LoadSentenceRecordings(sentences);
            LoadSentenceScores(sentences);
            FillSentenceList(sentences);

            _labelTitle.Text = _wholeBook ? "整本跟读" : _segmentMode ? "原声跟读" : "句子跟读";
            Text = $"{_labelTitle.Text} - {_documentTitle}";
            _takeActions.Visible = _segmentMode;
            _btnCompose.Visible = _segmentMode;
            UpdateSourceBar();

            if (sentences.Count > 0)
            {
                int initialIndex = _wholeBook
                    ? ResolveWholeBookInitialIndex()
                    : _segmentMode ? ResolveInitialIndex() : ResolveTextInitialIndex(sentences);
                SetCurrentIndex(initialIndex, play);
            }
            else
            {
                _currentIndex = -1;
                _labelCurrent.Text = "请从左侧选择一句开始跟读";
                RefreshScoreLabel();
                UpdateReplayButtonState();
                RefreshWaveform();
            }
        }

        private void RebuildPractice(IReadOnlyList<ShadowingSentence> sentences)
        {
            if (_practice != null)
            {
                _practice.StateChanged -= OnSentenceStateChanged;
                _practice.SentenceFinished -= OnSentenceFinished;
                _practice.ErrorOccurred -= OnPracticeError;
                _practice.Dispose();
                _practice = null;
            }

            if (_subtitleParser == null || sentences.Count == 0)
                return;

            _practice = new ShadowingPracticeService(sentences, _tts, _subtitleParser, _practicePresets);
            _practice.StateChanged += OnSentenceStateChanged;
            _practice.SentenceFinished += OnSentenceFinished;
            _practice.ErrorOccurred += OnPracticeError;
        }

        /// <summary>切换跟读来源：原声切片 ↔ 文字跟读。</summary>
        private void SwitchSource(bool segmentMode, bool play = false)
        {
            if (_sourceSwitching || segmentMode == _segmentMode)
                return;
            if (segmentMode && _segments.Count == 0)
                return;

            _sourceSwitching = true;
            try
            {
                StopCountdown();
                StopRecordingIfAny(suppressAdvance: true);
                _recordingIndex = -1;
                _recordingSentenceId = null;
                _recordingPlayer?.Stop();
                _practice?.StopPlayback();

                _segmentMode = segmentMode;
                ApplyActiveSource(play);
            }
            finally
            {
                _sourceSwitching = false;
            }
        }

        /// <summary>切片模式行同样按 cue 生成（经绑定表）；无 cue 时回退旧行为（行=切片）。</summary>
        private List<ShadowingSentence> BuildSegmentSentences()
        {
            if (_bindings.Count > 0)
            {
                return _bindings.Select((binding, index) => new ShadowingSentence
                {
                    Id = binding.Cue.AnchorKey,
                    Index = index,
                    Text = binding.Cue.Text
                }).ToList();
            }

            return _segments.Select((segment, index) => new ShadowingSentence
            {
                Id = segment.Id,
                Index = index,
                Text = string.IsNullOrWhiteSpace(segment.Text)
                    ? $"第 {index + 1} 段 · {ShadowingMarkSession.FormatTimestamp(segment.StartMs)}"
                    : segment.Text!
            }).ToList();
        }

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
                    // 行按 cue 排序，进度经绑定表反查；无 cue 时回退按 _segments 下标
                    if (_bindings.Count > 0)
                    {
                        for (int i = 0; i < _bindings.Count; i++)
                        {
                            if (string.Equals(_bindings[i].Segment?.Id, page.LastSegmentId, StringComparison.Ordinal))
                                return i;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < _segments.Count; i++)
                        {
                            if (string.Equals(_segments[i].Id, page.LastSegmentId, StringComparison.Ordinal))
                                return i;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[SentenceShadowing] 读取跟读进度失败: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// 文字跟读模式：点读传入的句子优先定位，找不到时回落到第一句。
        /// 用去空白/小写比较，兼容分句器跨行补空格与标点差异。
        /// </summary>
        private int ResolveTextInitialIndex(IReadOnlyList<ShadowingSentence> sentences)
        {
            if (string.IsNullOrWhiteSpace(_initialSentenceText) || sentences.Count == 0)
                return 0;

            string target = PdfSentenceSegmenter.Normalize(_initialSentenceText);
            if (target.Length == 0)
                return 0;

            for (int i = 0; i < sentences.Count; i++)
            {
                if (string.Equals(PdfSentenceSegmenter.Normalize(sentences[i].Text), target, StringComparison.Ordinal))
                    return i;
            }

            for (int i = 0; i < sentences.Count; i++)
            {
                string text = PdfSentenceSegmenter.Normalize(sentences[i].Text);
                if (text.Length > 0 && (text.Contains(target, StringComparison.Ordinal) || target.Contains(text, StringComparison.Ordinal)))
                    return i;
            }

            return 0;
        }

        /// <summary>整本模式从当前阅读页对应的第一条高亮开始。</summary>
        private int ResolveWholeBookInitialIndex()
        {
            for (int i = 0; i < _sentencePageIndexes.Count; i++)
            {
                if (_sentencePageIndexes[i] >= _pageIndex)
                    return i;
            }

            return 0;
        }

        /// <summary>句子所属页码：单页模式固定为当前页，整本模式取高亮页码。</summary>
        private int ResolveSentencePage(int index)
        {
            if (!_wholeBook)
                return _pageIndex;
            return index >= 0 && index < _sentencePageIndexes.Count
                ? _sentencePageIndexes[index]
                : _pageIndex;
        }

        private void UpdateHeaderInfo(int index)
        {
            if (!_wholeBook)
                return;

            int total = _practice?.Sentences.Count ?? _textSentences.Count;
            int page = ResolveSentencePage(index);
            _labelInfo.Text = $"整本 · 第 {page + 1} 页 · 共 {total} 条高亮";
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
            return SegmentForRow(_currentIndex);
        }

        /// <summary>行下标 → 绑定切片（行 Id 已是 cue.AnchorKey，按 Id 反查 _segments 会落空）；无 cue 时回退按下标取 _segments。</summary>
        private ShadowingSegment? SegmentForRow(int rowIndex)
        {
            if (rowIndex < 0)
                return null;
            if (_bindings.Count > 0)
                return rowIndex < _bindings.Count ? _bindings[rowIndex].Segment : null;
            return rowIndex < _segments.Count ? _segments[rowIndex] : null;
        }

        /// <summary>行录音/评分缓存键：切片模式用切片 ID（takes 与切片同生死），文字模式用行 ID（锚点）。</summary>
        private string? RecordingKeyForRow(int rowIndex)
        {
            if (rowIndex < 0 || _practice == null || rowIndex >= _practice.Sentences.Count)
                return null;
            if (_segmentMode)
                return SegmentForRow(rowIndex)?.Id;
            return _practice.Sentences[rowIndex].Sentence.Id;
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

            // 合成按钮放在页脚左侧，避免 4 个按钮在窄侧栏被裁切
            _btnCompose.FlatStyle = FlatStyle.Flat;
            _btnCompose.FlatAppearance.BorderSize = 0;
            _btnCompose.Font = new Font("Microsoft YaHei UI", 8.5F);
            _btnCompose.Size = new Size(82, 24);
            _btnCompose.Location = new Point(12, 4);
            _btnCompose.Cursor = Cursors.Hand;
            _btnCompose.BackColor = palette.SurfaceAlt;
            _btnCompose.ForeColor = palette.Text;
            _btnCompose.Text = "合成成品";
            _btnCompose.Visible = false;
            footer.Controls.Add(_btnCompose);

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

        /// <summary>在标题栏右侧挂载「跟读来源」单选按钮与「清理原声」入口。</summary>
        private void MountSourceBar()
        {
            var palette = PdfSidebarTheme.PaletteFor(this);

            _sourceLabel.Text = "跟读来源";
            _sourceLabel.AutoSize = true;
            _sourceLabel.Font = new Font("Microsoft YaHei UI", 8.5F);
            _sourceLabel.ForeColor = palette.Muted;
            _sourceLabel.Margin = new Padding(0, 8, 8, 0);
            _sourceLabel.Tag = "muted";

            StyleSourceRadio(_radioSourceSegments, "原声切片", palette);
            StyleSourceRadio(_radioSourceText, "文字跟读", palette);

            _btnClearSegments.Text = "清理原声";
            _btnClearSegments.FlatStyle = FlatStyle.Flat;
            _btnClearSegments.FlatAppearance.BorderSize = 0;
            _btnClearSegments.Font = new Font("Microsoft YaHei UI", 8.5F);
            _btnClearSegments.Size = new Size(78, 26);
            _btnClearSegments.Margin = new Padding(6, 2, 0, 0);
            _btnClearSegments.Cursor = Cursors.Hand;
            _btnClearSegments.BackColor = palette.SurfaceAlt;
            _btnClearSegments.ForeColor = palette.Text;
            _buttonTip.SetToolTip(_btnClearSegments, "删除本页原声切片与该页跟读录音；文字跟读记录保留");

            _btnAlign.Text = "原声对齐";
            _btnAlign.FlatStyle = FlatStyle.Flat;
            _btnAlign.FlatAppearance.BorderSize = 0;
            _btnAlign.Font = new Font("Microsoft YaHei UI", 8.5F);
            _btnAlign.Size = new Size(78, 26);
            _btnAlign.Margin = new Padding(6, 2, 0, 0);
            _btnAlign.Cursor = Cursors.Hand;
            _btnAlign.BackColor = palette.SurfaceAlt;
            _btnAlign.ForeColor = palette.Text;
            _btnAlign.Visible = false;
            _btnAlign.Click += OnAlignClicked;
            _buttonTip.SetToolTip(_btnAlign,
                "播放本页原声，按空格结束当前句并生成原生切片；→ 跳过，Backspace 撤销，R 重播");

            _sourceBar.FlowDirection = FlowDirection.LeftToRight;
            _sourceBar.WrapContents = false;
            _sourceBar.AutoSize = true;
            _sourceBar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _sourceBar.BackColor = palette.Background;
            _sourceBar.Visible = false;
            _sourceBar.Controls.Add(_sourceLabel);
            _sourceBar.Controls.Add(_radioSourceSegments);
            _sourceBar.Controls.Add(_radioSourceText);
            _sourceBar.Controls.Add(_btnClearSegments);
            _sourceBar.Controls.Add(_btnAlign);

            header.Controls.Add(_sourceBar);
            header.Resize += (_, _) => LayoutSourceBar();
        }

        private static void StyleSourceRadio(RadioButton radio, string text, PdfSidebarPalette palette)
        {
            radio.Text = text;
            radio.AutoSize = true;
            radio.Font = new Font("Microsoft YaHei UI", 9F);
            radio.ForeColor = palette.Text;
            radio.BackColor = Color.Transparent;
            radio.Cursor = Cursors.Hand;
            radio.Margin = new Padding(0, 5, 10, 0);
        }

        private void LayoutSourceBar()
        {
            if (_sourceBar.Parent == null)
                return;

            var size = _sourceBar.PreferredSize;
            _sourceBar.Size = size;
            _sourceBar.Location = new Point(
                Math.Max(0, header.ClientSize.Width - size.Width),
                Math.Max(0, (header.ClientSize.Height - size.Height) / 2));
        }

        /// <summary>本页有音频且存在可对齐句时才允许进入原声对齐；整本模式暂不启用。</summary>
        private bool CanAlignAudio()
            => !_wholeBook
               && _alignmentContext != null
               && _subtitleParser != null
               && _alignmentCues.Count > 0
               && !string.IsNullOrWhiteSpace(_alignmentContext.SourcePath);

        /// <summary>有切片或可做原声对齐时显示来源栏；两种来源都存在时才显示单选按钮。</summary>
        private void UpdateSourceBar()
        {
            bool hasSegments = _segments.Count > 0;
            bool canSwitch = hasSegments && _textSentences.Count > 0;
            bool canAlign = CanAlignAudio();

            _sourceLabel.Visible = canSwitch;
            _radioSourceSegments.Visible = canSwitch;
            _radioSourceText.Visible = canSwitch;
            _btnClearSegments.Visible = hasSegments;
            _btnAlign.Visible = canAlign;

            // 对齐中禁止切换来源或清理切片，避免列表与打点句错位
            bool interactive = !_aligning;
            _radioSourceSegments.Enabled = interactive;
            _radioSourceText.Enabled = interactive;
            _btnClearSegments.Enabled = interactive && hasSegments;

            _btnAlign.Text = _aligning ? "完成对齐" : "原声对齐";
            _sourceBar.Visible = hasSegments || canAlign;

            if (canSwitch)
            {
                _radioSourceSegments.Checked = _segmentMode;
                _radioSourceText.Checked = !_segmentMode;
            }

            LayoutSourceBar();
        }

        /// <summary>
        /// 原声对齐：播放本页音频，空格结束当前句并生成原生切片。
        /// 与普通播放分离，空格只在“对齐模式”内表示打点，避免误切片。
        /// </summary>
        private async void OnAlignClicked(object? sender, EventArgs e)
        {
            try
            {
                if (_aligning)
                    await FinishAlignmentAsync();
                else
                    StartAlignment();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[SentenceShadowing] 原声对齐失败: {ex.Message}");
                _labelStatus.Text = $"原声对齐失败：{ex.Message}";
                _finishingAlignment = false;
                _btnAlign.Enabled = true;
            }
        }

        private void StartAlignment()
        {
            if (!CanAlignAudio() || _alignmentContext == null)
            {
                _labelStatus.Text = "本页没有可用原声或分句，无法对齐。";
                return;
            }

            // 对齐句与文字跟读句一致，先切回文字来源避免列表索引错位
            if (_segmentMode)
                SwitchSource(false);

            StopCountdown();
            StopRecordingIfAny(suppressAdvance: true);
            _recordingPlayer?.Stop();
            _practice?.StopPlayback();

            IReadOnlyList<ShadowingSegment> existing = Array.Empty<ShadowingSegment>();
            try
            {
                var manifest = _segmentStore.LoadSegments(_pdfPath, _pageIndex);
                if (manifest?.Segments.Count > 0)
                {
                    _segmentManifest = manifest;
                    existing = manifest.Segments;
                }
                else
                {
                    existing = _segments;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[SentenceShadowing] 读取已有切片失败: {ex.Message}");
                existing = _segments;
            }

            _alignmentSession = new ShadowingAlignmentSession(_pageIndex, _alignmentCues, existing);

            // 从第一句尚未对齐的位置继续；分页音频从头播放，整篇音频沿用阅读器当前位置
            int startIndex = 0;
            int startMs = Math.Max(0, _alignmentContext.StartMs);
            for (int i = 0; i < _alignmentCues.Count; i++)
            {
                var previous = _alignmentSession.FindExisting(_alignmentCues[i]);
                if (previous == null)
                    break;
                startIndex = i + 1;
                startMs = Math.Max(startMs, previous.EndMs);
            }

            if (startIndex >= _alignmentCues.Count)
            {
                startIndex = 0;
                startMs = Math.Max(0, _alignmentContext.StartMs);
            }

            _alignmentSession.Start(startMs, startIndex);
            _aligning = true;
            _btnAlign.Enabled = true;
            _btnRecord.Enabled = false;
            _btnReplayRecording.Enabled = false;
            _btnPlay.Text = "⏸ 原声";
            UpdateSourceBar();
            SetAlignmentCurrentIndex(_alignmentSession.CurrentIndex);
            _labelStatus.Text = startIndex > 0
                ? $"从第 {startIndex + 1} 句继续；空格结束本句，→ 跳过，Backspace 撤销，R 重播"
                : "空格结束本句，→ 跳过，Backspace 撤销，R 重播";

            if (_alignmentPlayer == null)
            {
                _alignmentPlayer = new NAudioMediaPlayer(_subtitleParser!);
                _alignmentPlayer.PlaybackFinished += OnAlignmentPlaybackFinished;
                _alignmentPlayer.PositionChanged += OnAlignmentPositionChanged;
            }

            _alignmentPlayer.Load(_alignmentContext.SourcePath);
            _alignmentPlayer.LoadSubtitle(_alignmentContext.SubtitlePath);
            _alignmentPlayer.Seek(TimeSpan.FromMilliseconds(startMs));
            FireAndForget(_alignmentPlayer.PlayAsync());
        }

        private void SetAlignmentCurrentIndex(int index)
        {
            if (_alignmentSession == null || _alignmentCues.Count == 0)
                return;

            int safe = Math.Clamp(index, 0, _alignmentCues.Count - 1);
            _currentIndex = safe;
            _suppressListSelection = true;
            try
            {
                _listSentences.SelectedIndex = safe;
            }
            finally
            {
                _suppressListSelection = false;
            }

            _labelCurrent.Text = _alignmentCues[safe].Text;
            _labelInfo.Text = $"原声对齐 · 第 {safe + 1}/{_alignmentCues.Count} 句";
            _labelScore.Text = "参考分：完成对齐后显示";
            _listSentences.Invalidate();
            _progressTrack.Invalidate();
            UpdateReplayButtonState();
        }

        private void MarkAlignmentCue()
        {
            if (!_aligning || _alignmentSession == null)
                return;

            int position = _alignmentPlayer != null
                ? (int)_alignmentPlayer.Position.TotalMilliseconds
                : _alignmentSession.CursorStartMs;
            var segment = _alignmentSession.Mark(position);
            if (segment == null)
            {
                _labelStatus.Text = "本句间隔不足 0.3 秒，已忽略；请继续播放后重按空格。";
                return;
            }

            if (_alignmentSession.IsCompleted)
            {
                SetAlignmentCurrentIndex(_alignmentSession.TotalCount - 1);
                _labelStatus.Text = "全部句子已标记，点击「完成对齐」生成原声切片。";
                _alignmentPlayer?.Pause();
                _btnPlay.Text = "▶ 播放";
            }
            else
            {
                SetAlignmentCurrentIndex(_alignmentSession.CurrentIndex);
                _labelStatus.Text =
                    $"已标记 {ShadowingMarkSession.FormatTimestamp(segment.StartMs)}–" +
                    $"{ShadowingMarkSession.FormatTimestamp(segment.EndMs)} · 空格继续";
            }
        }

        private void SkipAlignmentCue()
        {
            if (!_aligning || _alignmentSession == null || !_alignmentSession.Skip())
                return;

            if (_alignmentSession.IsCompleted)
            {
                SetAlignmentCurrentIndex(_alignmentSession.TotalCount - 1);
                _labelStatus.Text = "已跳过最后一句，点击「完成对齐」保存已标记切片。";
            }
            else
            {
                // Skip 已前进到下一句，文案使用前进后的序号，避免显示成“跳过的句”
                SetAlignmentCurrentIndex(_alignmentSession.CurrentIndex);
                _labelStatus.Text =
                    $"已跳过上一句；当前第 {_alignmentSession.CurrentIndex + 1}/{_alignmentSession.TotalCount} 句。";
            }
        }

        private void UndoAlignmentCue()
        {
            if (!_aligning || _alignmentSession == null || !_alignmentSession.Undo())
            {
                _labelStatus.Text = "没有可撤销的对齐记录。";
                return;
            }

            SetAlignmentCurrentIndex(_alignmentSession.CurrentIndex);
            _alignmentPlayer?.Seek(TimeSpan.FromMilliseconds(_alignmentSession.CursorStartMs));
            _labelStatus.Text = "已撤销上一句，游标回到该句起点。";
        }

        private void ReplayAlignmentFromCursor()
        {
            if (!_aligning || _alignmentPlayer == null || _alignmentSession == null)
                return;

            _alignmentPlayer.Seek(TimeSpan.FromMilliseconds(_alignmentSession.CursorStartMs));
            _lastAlignmentPositionTick = 0;
            FireAndForget(_alignmentPlayer.PlayAsync());
            _btnPlay.Text = "⏸ 原声";
        }

        private void ToggleAlignmentPlayback()
        {
            if (!_aligning || _alignmentPlayer == null)
                return;

            if (_alignmentPlayer.State == MediaPlaybackState.Playing)
            {
                _alignmentPlayer.Pause();
                _btnPlay.Text = "▶ 播放";
                return;
            }

            if (_alignmentSession != null
                && _alignmentPlayer.Position.TotalMilliseconds < _alignmentSession.CursorStartMs - 100)
            {
                _alignmentPlayer.Seek(TimeSpan.FromMilliseconds(_alignmentSession.CursorStartMs));
            }

            FireAndForget(_alignmentPlayer.PlayAsync());
            _btnPlay.Text = "⏸ 原声";
        }

        private void OnAlignmentPlaybackFinished(object? sender, EventArgs e)
        {
            if (!_aligning || sender != _alignmentPlayer)
                return;

            _btnPlay.Text = "▶ 播放";
            _labelStatus.Text = _alignmentSession?.IsCompleted == true
                ? "本页原声已播完，全部句已标记；点击「完成对齐」。"
                : "本页原声已播完；按 R 从断点重播，或继续标记。";
        }

        private void OnAlignmentPositionChanged(object? sender, TimeSpan position)
        {
            if (!_aligning || _alignmentSession == null || IsDisposed)
                return;

            long now = Environment.TickCount64;
            if (now - _lastAlignmentPositionTick < 500)
                return;
            _lastAlignmentPositionTick = now;

            if (_alignmentSession.IsCompleted)
                return;

            _labelStatus.Text =
                $"空格结束本句 · 原声 {ShadowingMarkSession.FormatTimestamp((int)position.TotalMilliseconds)}";
        }

        private async Task FinishAlignmentAsync()
        {
            if (!_aligning || _finishingAlignment)
                return;

            _finishingAlignment = true;
            _btnAlign.Enabled = false;
            _btnAlign.Text = "正在生成…";
            try
            {
                _alignmentPlayer?.Pause();
                var session = _alignmentSession;
                var created = session?.CreatedSegments.ToList() ?? new List<ShadowingSegment>();
                if (created.Count == 0)
                {
                    ExitAlignmentMode();
                    ApplyActiveSource();
                    _labelStatus.Text = "未生成新的原声切片。";
                    return;
                }

                var manifest = _segmentStore.LoadSegments(_pdfPath, _pageIndex)
                               ?? new ShadowingSegmentManifest { PageIndex = _pageIndex };
                manifest.SchemaVersion = 3;
                manifest.Source = BuildAlignmentSource(manifest.Source);
                manifest.Session = BuildAlignmentSession(manifest.Session);

                foreach (var segment in created)
                {
                    segment.SlicePath = _segmentStore.GetSliceRelativePath(segment.Id);
                    int index = manifest.Segments.FindIndex(s =>
                        string.Equals(s.Id, segment.Id, StringComparison.Ordinal));
                    if (index >= 0)
                    {
                        var previous = manifest.Segments[index];
                        // 重新对齐不覆盖已有跟读 take
                        segment.Takes = previous.Takes;
                        // 时间范围变化后旧切片已不对应本句，删除以强制重新生成
                        if (previous.StartMs != segment.StartMs || previous.EndMs != segment.EndMs)
                        {
                            DeleteStaleSlice(previous.SlicePath);
                            DeleteStaleSlice(segment.SlicePath);
                        }
                        manifest.Segments[index] = segment;
                    }
                    else
                    {
                        manifest.Segments.Add(segment);
                    }
                }

                var requests = created
                    .Where(segment => !File.Exists(_segmentStore.ResolvePath(segment.SlicePath)))
                    .Select(segment => new AudioSliceRequest(
                        segment.Id,
                        _alignmentContext!.SourcePath,
                        segment.StartMs,
                        segment.EndMs,
                        _segmentStore.ResolvePath(segment.SlicePath)))
                    .ToList();

                var failed = new HashSet<string>(StringComparer.Ordinal);
                if (requests.Count > 0)
                {
                    _labelStatus.Text = $"正在生成原声切片 0/{requests.Count}…";
                    var progress = new Progress<(int Done, int Total)>(p =>
                    {
                        if (!IsDisposed)
                            _labelStatus.Text = $"正在生成原声切片 {p.Done}/{p.Total}…";
                    });

                    var results = await _audioSliceService
                        .SliceAllAsync(requests, progress)
                        .ConfigureAwait(true);
                    foreach (var result in results.Where(result => !result.Success))
                    {
                        failed.Add(result.SegmentId);
                        var segment = manifest.Segments.FirstOrDefault(s =>
                            string.Equals(s.Id, result.SegmentId, StringComparison.Ordinal));
                        if (segment != null)
                            segment.SlicePath = string.Empty;
                        Trace.TraceWarning(
                            $"[SentenceShadowing] 原声切片失败 {result.SegmentId}: {result.Error}");
                    }
                }

                // 保存前对账：使用即将写盘的同一份 manifest 快照，不另读文件；只上报，不阻断保存
                var reconciled = ShadowingCueReconciler.Reconcile(
                    _alignmentCues,
                    manifest.Segments,
                    s => File.Exists(_segmentStore.ResolvePath(s.SlicePath)),
                    alignedOnce: true);

                await _segmentStore
                    .SaveSegmentsAsync(_pdfPath, manifest, CancellationToken.None, preserveExistingTakes: true)
                    .ConfigureAwait(true);

                _segmentManifest = manifest;
                // 保存后按 cue 顺序重建绑定表与切片列表（I4）：顺序只由 cue 决定，不再按 StartMs 排序
                _bindings = reconciled.Bindings;
                _bindingsDrift = reconciled;
                // _segments 保持全量（含孤立切片），否则下次保存清单会静默丢记录；清理只能由用户显式触发
                _segments = _bindings
                    .Where(b => b.Segment != null)
                    .Select(b => b.Segment!)
                    .Concat(reconciled.Orphans)
                    .ToList();
                _segmentMode = _segments.Count > 0;
                ExitAlignmentMode();
                ApplyActiveSource();

                string status = $"原声对齐完成：{reconciled.MatchedCount} 句有原声";
                if (reconciled.UnboundCount > 0)
                    status += $"，{reconciled.UnboundCount} 句已跳过";
                status += "。";
                if (failed.Count > 0)
                    status += $"；{failed.Count} 句切片生成失败，已回退文字跟读。";
                if (reconciled.Orphans.Count > 0)
                    status += $"；{reconciled.Orphans.Count} 条旧切片与本页句子不匹配，可清理。";
                _labelStatus.Text = status;
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[SentenceShadowing] 保存原声对齐失败: {ex.Message}");
                _labelStatus.Text = $"保存原声对齐失败：{ex.Message}";
            }
            finally
            {
                _finishingAlignment = false;
                _btnAlign.Enabled = true;
                _btnAlign.Text = _aligning ? "完成对齐" : "原声对齐";
            }
        }

        /// <summary>
        /// 删除与新打点时间范围不符的旧切片；切片服务按“文件存在即复用”工作，
        /// 不删除会导致重新对齐后仍播放旧的音频区间。
        /// </summary>
        private void DeleteStaleSlice(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return;

            try
            {
                string path = _segmentStore.ResolvePath(relativePath);
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                // 删除失败通常是被播放器占用；保留文件，记录后由人工重试
                Trace.TraceWarning($"[SentenceShadowing] 删除旧切片失败 {relativePath}: {ex.Message}");
            }
        }

        private ShadowingSegmentSource BuildAlignmentSource(ShadowingSegmentSource? existing)
        {
            var source = existing ?? new ShadowingSegmentSource();
            source.TrackPath = _alignmentContext!.SourcePath;
            source.DurationMs = AudioSliceService.ReadDurationMs(_alignmentContext.SourcePath);
            source.Fingerprint = ShadowingSegmentStore.BuildFingerprint(
                _alignmentContext.SourcePath, source.DurationMs);
            return source;
        }

        private ShadowingSegmentSession BuildAlignmentSession(ShadowingSegmentSession? existing)
        {
            var session = existing ?? new ShadowingSegmentSession();
            if (string.IsNullOrWhiteSpace(session.Id))
                session.Id = ShadowingSegmentStore.CreateSessionId(DateTime.Now);
            session.CaptureSpeed = 1.0;
            session.LockedPage = false;
            session.UpdatedAt = DateTime.Now;
            return session;
        }

        private void ExitAlignmentMode()
        {
            _aligning = false;
            _alignmentSession = null;
            _alignmentPlayer?.Pause();
            _btnPlay.Text = "▶ 播放";
            _btnRecord.Enabled = _recorder != null;
            _labelInfo.Text = _wholeBook ? _labelInfo.Text : $"第 {_pageIndex + 1} 页";
            UpdateSourceBar();
            UpdateReplayButtonState();
            RefreshScoreLabel();
        }

        private void OnSourceRadioChanged(object? sender, EventArgs e)
        {
            if (_sourceSwitching || sender is not RadioButton radio || !radio.Checked)
                return;
            if (ReferenceEquals(radio, _radioSourceSegments) == _segmentMode)
                return;

            // 对齐期间列表固定为页面分句，切到原声切片会让打点索引错位
            if (_aligning)
            {
                _labelStatus.Text = "对齐中不能切换来源；请先完成对齐。";
                UpdateSourceBar();
                return;
            }

            SwitchSource(ReferenceEquals(radio, _radioSourceSegments));
        }

        /// <summary>清理本页原声切片、跟读 take 与合成成品，并回落到文字跟读。</summary>
        private async void OnClearSegmentsClicked(object? sender, EventArgs e)
        {
            if (_aligning)
            {
                _labelStatus.Text = "对齐中不能清理切片；请先完成对齐。";
                return;
            }

            if (_segments.Count == 0)
                return;

            var confirm = MessageBox.Show(
                this,
                "清理本页原声切片？\n\n将删除本页的原声切片、全部跟读录音与合成成品。\n文字跟读的录音和评分会保留。",
                "清理原声", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes)
                return;

            _btnClearSegments.Enabled = false;
            try
            {
                await _segmentStore.DeletePageSegmentsAsync(_pdfPath, _pageIndex);
            }
            catch (Exception ex)
            {
                _labelStatus.Text = $"清理原声失败：{ex.Message}";
                return;
            }
            finally
            {
                _btnClearSegments.Enabled = true;
            }

            _segmentManifest = null;
            _segments = Array.Empty<ShadowingSegment>();

            // 切片模式需要重建列表与练习；文字模式下只刷新来源栏
            if (_segmentMode)
                SwitchSource(false);

            UpdateSourceBar();
            _labelStatus.Text = _textSentences.Count > 0
                ? "已清理本页原声，已切换到文字跟读。"
                : "已清理本页原声；本页没有可跟读文本，可先执行 OCR 或翻译。";
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
            _btnCompose.Click += async (_, _) => await ComposePageAsync();
            _comboMode.SelectedIndexChanged += OnModeChanged;

            _radioSourceSegments.CheckedChanged += OnSourceRadioChanged;
            _radioSourceText.CheckedChanged += OnSourceRadioChanged;
            _btnClearSegments.Click += OnClearSegmentsClicked;

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

            var dotBounds = new Rectangle(bounds.X + 12, bounds.Y + 19, 10, 10);
            if (row.BindingStatus is { } bindingStatus)
            {
                // 对账徽标：原声=品牌色实心，缺切片=橙色描边，已跳过=灰色实心，未对齐=淡灰虚线
                switch (bindingStatus)
                {
                    case CueBindingStatus.Matched:
                    case CueBindingStatus.Ambiguous:
                        using (var brush = new SolidBrush(PdfSidebarTheme.Brand))
                            e.Graphics.FillEllipse(brush, dotBounds);
                        break;
                    case CueBindingStatus.MissingSlice:
                        using (var pen = new Pen(Color.FromArgb(245, 158, 11), 1.5f))
                            e.Graphics.DrawEllipse(pen, dotBounds);
                        break;
                    case CueBindingStatus.Unbound when row.UnboundReason == CueUnboundReason.Skipped:
                        using (var brush = new SolidBrush(palette.Muted))
                            e.Graphics.FillEllipse(brush, dotBounds);
                        break;
                    default:
                        using (var pen = new Pen(palette.Border, 1.2f)
                               { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                            e.Graphics.DrawEllipse(pen, dotBounds);
                        break;
                }
            }
            else
            {
                Color dotColor = row.State switch
                {
                    ShadowingAudioState.Synthesizing => PdfSidebarTheme.Brand,
                    ShadowingAudioState.Ready => PdfSidebarTheme.Success,
                    ShadowingAudioState.Failed => PdfSidebarTheme.Danger,
                    _ => palette.Muted
                };

                using (var dot = new SolidBrush(dotColor))
                    e.Graphics.FillEllipse(dot, dotBounds);
            }

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
                if (row.BindingStatus is { } rowBinding)
                {
                    // 有对账：徽标文字区分 原声 / 缺切片 / 已跳过 / 未对齐
                    stateText = rowBinding switch
                    {
                        CueBindingStatus.Matched or CueBindingStatus.Ambiguous => "原声切片",
                        CueBindingStatus.MissingSlice => "缺切片",
                        CueBindingStatus.Unbound when row.UnboundReason == CueUnboundReason.Skipped => "已跳过",
                        _ => "未对齐"
                    };
                    if (_practice != null && row.Index < _practice.Sentences.Count
                        && _practice.Sentences[row.Index].IsTtsFallback
                        && rowBinding is CueBindingStatus.MissingSlice or CueBindingStatus.Unbound)
                    {
                        stateText += " · TTS";
                    }
                    var boundSegment = SegmentForRow(row.Index);
                    if (boundSegment is { Takes.Count: > 0 })
                        stateText += $" · {boundSegment.Takes.Count} 次";
                }
                else
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
                        {
                            stateText = "TTS 回退";
                        }
                        else
                        {
                            var segment = SegmentForRow(row.Index);
                            if (segment is { Takes.Count: > 0 })
                                stateText += $" · {segment.Takes.Count} 次";
                        }
                    }
                }
            }
            if (_wholeBook)
                stateText = $"{row.PageIndex + 1} 页 · {stateText}";
            TextRenderer.DrawText(e.Graphics, stateText, stateFont,
                new Rectangle(bounds.X + 62, bounds.Y + 56, 150, 18),
                palette.Muted, TextFormatFlags.NoPrefix);
            stateFont.Dispose();

            if (_practice != null && row.Index < _practice.Sentences.Count)
            {
                string? id = RecordingKeyForRow(row.Index);
                if (id != null && _sentenceScores.TryGetValue(id, out int score))
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
                    CueBindingStatus? bindingStatus = null;
                    CueUnboundReason? unboundReason = null;
                    if (_segmentMode)
                    {
                        if (i < _bindings.Count)
                        {
                            // 徽标由对账状态决定：命中=原声，缺文件=缺切片，未绑定=待读
                            var binding = _bindings[i];
                            bindingStatus = binding.Status;
                            unboundReason = binding.UnboundReason;
                            state = binding.Status switch
                            {
                                CueBindingStatus.Matched or CueBindingStatus.Ambiguous => ShadowingAudioState.Ready,
                                CueBindingStatus.MissingSlice => ShadowingAudioState.Failed,
                                _ => ShadowingAudioState.Pending
                            };
                        }
                        else if (_bindings.Count == 0 && i < _segments.Count)
                        {
                            // 无 cue（无对齐上下文）回退：按下标判断切片文件
                            string slice = _segmentStore.ResolvePath(_segments[i].SlicePath);
                            state = File.Exists(slice) ? ShadowingAudioState.Ready : ShadowingAudioState.Failed;
                        }
                    }

                    _listSentences.Items.Add(new SentenceRow
                    {
                        Index = i,
                        PageIndex = _wholeBook && i < _sentencePageIndexes.Count
                            ? _sentencePageIndexes[i]
                            : _pageIndex,
                        Text = sentences[i].Text,
                        State = state,
                        BindingStatus = bindingStatus,
                        UnboundReason = unboundReason
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
                ? _wholeBook
                    ? "整本 · 没有可跟读的高亮文本"
                    : $"第 {_pageIndex + 1} 页 · 未识别到可跟读的句子"
                : _wholeBook
                    ? $"整本 · 共 {sentences.Count} 条高亮"
                    : _segmentMode
                        ? $"第 {_pageIndex + 1} 页 · 共 {sentences.Count} 句（原声跟读）"
                        : $"第 {_pageIndex + 1} 页 · 共 {sentences.Count} 句";

            if (sentences.Count == 0)
                _labelStatus.Text = _wholeBook
                    ? "全书没有可用于跟读的高亮文本，请先划选高亮并完成识别。"
                    : "当前页面没有可分句的文本，请先执行 OCR 或翻译取得文本。";
            else if (_wholeBook)
                _labelStatus.Text = "已载入全书高亮，可逐条跟读或录音。";
            else if (_segmentMode)
                _labelStatus.Text = BuildSegmentLoadPrompt() ?? "已载入原声切片，可逐段跟读或录音。";
            else if (_cueCountMismatch)
                _labelStatus.Text = $"本页高亮与句子数量不一致（{_alignmentCues.Count}/{_textSentences.Count}），对齐时请核对。";
        }

        /// <summary>
        /// 加载后的对账提示：未对齐完或发现漂移（孤立切片）时在状态栏说明；
        /// 不弹窗、不阻塞。无异常时返回 null，沿用默认文案。
        /// </summary>
        private string? BuildSegmentLoadPrompt()
        {
            if (_bindingsDrift == null || _bindings.Count == 0)
                return null;

            // 未对齐完：尚未开始/完成对齐的句子（区别于对齐后已跳过）
            int notAligned = _bindings.Count(static b =>
                b.Status == CueBindingStatus.Unbound && b.UnboundReason == CueUnboundReason.NotAlignedYet);
            int orphans = _bindingsDrift.Orphans.Count;
            if (notAligned == 0 && orphans == 0)
                return null;

            string prompt = string.Empty;
            if (notAligned > 0)
                prompt += $"本页尚未完成对齐，{notAligned} 句还没有原声。";
            if (orphans > 0)
                prompt += $"本页有 {orphans} 条切片未匹配当前句子，仍按句子顺序显示。";
            return prompt;
        }

        private void PersistManifest(List<ShadowingSentence> sentences)
        {
            // 整本模式不生成单页分句清单，录音键按高亮 ID 独立保存
            if (_wholeBook || string.IsNullOrWhiteSpace(_pdfPath) || sentences.Count == 0)
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
        /// 历史键兼容（5.8）：先按行 Id（cue.AnchorKey）匹配，未命中回退 sentence.Id（惰性，不写回）；
        /// 切片模式的 take 缓存键保持 segment.Id 不动（SyncSegmentTake），不在此迁移。
        /// </summary>
        private void LoadSentenceRecordings(IReadOnlyList<ShadowingSentence> sentences)
        {
            if (string.IsNullOrWhiteSpace(_pdfPath) || sentences.Count == 0)
                return;

            try
            {
                var validIds = new HashSet<string>(sentences.Select(s => s.Id), StringComparer.Ordinal);
                var anchorByLegacyId = BuildLegacyRecordingKeyMap();
                foreach (int pageIndex in RecordingPages())
                {
                    var saved = _store.LoadSentenceRecordings(_pdfPath, pageIndex);
                    foreach (var pair in saved)
                    {
                        if (!File.Exists(pair.Value))
                            continue;
                        if (validIds.Contains(pair.Key))
                        {
                            _sentenceRecordings[pair.Key] = pair.Value;
                        }
                        else if (anchorByLegacyId.TryGetValue(pair.Key, out string? anchor)
                                 && validIds.Contains(anchor)
                                 && !_sentenceRecordings.ContainsKey(anchor))
                        {
                            _sentenceRecordings[anchor] = pair.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[SentenceShadowing] 读取跟读录音失败: {ex.Message}");
            }
        }

        /// <summary>旧录音/评分键（sentence.Id）→ 新键（cue.AnchorKey）的惰性回退映射；无 cue 时为空。</summary>
        private Dictionary<string, string> BuildLegacyRecordingKeyMap()
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var cue in _alignmentCues)
            {
                if (!string.IsNullOrWhiteSpace(cue.SentenceId)
                    && !string.Equals(cue.SentenceId, cue.AnchorKey, StringComparison.Ordinal))
                {
                    map.TryAdd(cue.SentenceId, cue.AnchorKey);
                }
            }
            return map;
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
                var validIds = new HashSet<string>(sentences.Select(s => s.Id), StringComparer.Ordinal);
                var anchorByLegacyId = BuildLegacyRecordingKeyMap();
                foreach (int pageIndex in RecordingPages())
                {
                    var saved = _store.LoadSentenceScores(_pdfPath, pageIndex);
                    foreach (var pair in saved)
                    {
                        if (pair.Value < 0 || pair.Value > 100)
                            continue;
                        if (validIds.Contains(pair.Key))
                        {
                            _sentenceScores[pair.Key] = pair.Value;
                        }
                        else if (anchorByLegacyId.TryGetValue(pair.Key, out string? anchor)
                                 && validIds.Contains(anchor)
                                 && !_sentenceScores.ContainsKey(anchor))
                        {
                            _sentenceScores[anchor] = pair.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[SentenceShadowing] 读取跟读评分失败: {ex.Message}");
            }
        }

        /// <summary>需要读取录音/评分的页码：单页模式只有当前页，整本模式覆盖所有高亮页。</summary>
        private IEnumerable<int> RecordingPages()
        {
            if (!_wholeBook)
                return new[] { _pageIndex };
            return _sentencePageIndexes.Distinct();
        }

        /// <summary>只把指定页的条目写回该页，避免整本模式把其他页的记录写串。</summary>
        private Dictionary<string, string> RecordingsForPage(int pageIndex)
        {
            if (!_wholeBook)
                return new Dictionary<string, string>(_sentenceRecordings, StringComparer.Ordinal);

            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in _sentenceRecordings)
            {
                if (_sentencePageById.TryGetValue(pair.Key, out int ownerPage) && ownerPage == pageIndex)
                    result[pair.Key] = pair.Value;
            }
            return result;
        }

        private Dictionary<string, int> ScoresForPage(int pageIndex)
        {
            if (!_wholeBook)
                return new Dictionary<string, int>(_sentenceScores, StringComparer.Ordinal);

            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in _sentenceScores)
            {
                if (_sentencePageById.TryGetValue(pair.Key, out int ownerPage) && ownerPage == pageIndex)
                    result[pair.Key] = pair.Value;
            }
            return result;
        }

        private void PersistSentenceRecordings(int pageIndex)
        {
            if (string.IsNullOrWhiteSpace(_pdfPath))
                return;

            try
            {
                // 整本高亮录音与单页分句录音共用页级存储，保存前合并，避免互相覆盖
                var merged = new Dictionary<string, string>(
                    _store.LoadSentenceRecordings(_pdfPath, pageIndex), StringComparer.Ordinal);
                foreach (var pair in RecordingsForPage(pageIndex))
                    merged[pair.Key] = pair.Value;
                _store.SaveSentenceRecordings(_pdfPath, pageIndex, merged);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[SentenceShadowing] 保存跟读录音路径失败: {ex.Message}");
            }
        }

        private void PersistSentenceScores(int pageIndex)
        {
            if (string.IsNullOrWhiteSpace(_pdfPath))
                return;

            try
            {
                // 整本高亮评分与单页分句评分共用页级存储，保存前合并，避免互相覆盖
                var merged = new Dictionary<string, int>(
                    _store.LoadSentenceScores(_pdfPath, pageIndex), StringComparer.Ordinal);
                foreach (var pair in ScoresForPage(pageIndex))
                    merged[pair.Key] = pair.Value;
                _store.SaveSentenceScores(_pdfPath, pageIndex, merged);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[SentenceShadowing] 保存跟读评分失败: {ex.Message}");
            }
        }

        private void UpdateReplayButtonState()
        {
            string? key = RecordingKeyForRow(_currentIndex);
            bool available = key != null
                             && _sentenceRecordings.TryGetValue(key, out var path)
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
            _btnCompose.Enabled = BoundSegmentsInCueOrder().Any(s => CurrentTake(s) != null);
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

        /// <summary>合成用句间静音：跳过句不留空洞，紧邻拼接后句间仅 0.12s 间隔（不提供开关）。</summary>
        private const int ComposeGapMs = 120;

        /// <summary>按 cue 顺序返回已绑定的切片（孤立切片不参与合成）；无 cue 时回退为 _segments 原序。</summary>
        private IEnumerable<ShadowingSegment> BoundSegmentsInCueOrder()
            => _bindings.Count > 0
                ? _bindings.Where(b => b.Segment != null).Select(b => b.Segment!)
                : _segments;

        /// <summary>
        /// 合成本页成品：按 cue 顺序紧邻拼接所有“采用”的 take（跳过句不留空洞），句间 120ms 静音；
        /// 清单记录 segmentRefs/takeRefs，源 take 删除后仍可追溯。
        /// </summary>
        private async Task ComposePageAsync()
        {
            if (!_segmentMode || _segmentManifest == null)
                return;

            var items = new List<ShadowingComposeItem>();
            foreach (var segment in BoundSegmentsInCueOrder())
            {
                var take = CurrentTake(segment);
                if (take == null)
                    continue;
                string path = _segmentStore.ResolvePath(take.Path);
                if (File.Exists(path))
                    items.Add(new ShadowingComposeItem(segment.Id, take.Id, path));
            }

            if (items.Count == 0)
            {
                _labelStatus.Text = "还没有已采用的跟读录音，先录制并采用至少一段。";
                return;
            }

            _btnCompose.Enabled = false;
            _labelStatus.Text = "正在合成跟读成品…";
            try
            {
                string relative = _segmentStore.AllocateComposedRelativePath(_pageIndex, DateTime.Now);
                string output = _segmentStore.ResolvePath(relative);
                var result = await _composer.ComposeAsync(output, items, ComposeGapMs);

                if (!result.Success)
                {
                    _labelStatus.Text = $"合成失败：{result.Error}";
                    return;
                }

                _segmentManifest.Composed.Add(new ShadowingComposedAudio
                {
                    Path = _segmentStore.ToRelativePath(result.OutputPath!),
                    SegmentCount = result.SegmentCount,
                    SegmentRefs = result.SegmentRefs.ToList(),
                    TakeRefs = result.TakeRefs.ToList(),
                    DurationMs = result.DurationMs,
                    CreatedAt = DateTime.Now
                });
                await SaveSegmentManifestAsync();

                string minutes = TimeSpan.FromMilliseconds(result.DurationMs).ToString(@"mm\:ss");
                string skipped = result.MissingSegmentIds.Count > 0
                    ? $"，跳过 {result.MissingSegmentIds.Count} 段缺失录音"
                    : string.Empty;
                _labelStatus.Text = $"已合成 {result.SegmentCount} 段 · {minutes}";
                MessageBox.Show(
                    this,
                    $"已合成 {result.SegmentCount} 段跟读成品（{minutes}）{skipped}。\n\n{result.OutputPath}",
                    "合成完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _labelStatus.Text = $"合成失败：{ex.Message}";
            }
            finally
            {
                _btnCompose.Enabled = BoundSegmentsInCueOrder().Any(s => CurrentTake(s) != null);
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

            string? id = RecordingKeyForRow(_currentIndex);
            if (id != null && _sentenceScores.TryGetValue(id, out int score))
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

            string? key = RecordingKeyForRow(_currentIndex);
            if (key == null || !_sentenceRecordings.TryGetValue(key, out var path) || !File.Exists(path))
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
            UpdateHeaderInfo(index);
            RefreshScoreLabel();
            UpdateReplayButtonState();
            RefreshWaveform();
            PersistPracticeProgress();

            if (play)
                FireAndForget(PlayIndexAsync(index));
        }

        private void MoveSentence(int delta)
        {
            if (_aligning)
            {
                if (delta > 0)
                    SkipAlignmentCue();
                else
                    _labelStatus.Text = "对齐模式下请按 Backspace 撤销上一句。";
                return;
            }

            if (_practice == null || _practice.Sentences.Count == 0)
                return;

            int start = _currentIndex < 0 ? 0 : _currentIndex;
            int next = Math.Clamp(start + delta, 0, _practice.Sentences.Count - 1);
            SetCurrentIndex(next, play: true);
        }

        private void StartPlayback()
        {
            if (_aligning)
            {
                ToggleAlignmentPlayback();
                return;
            }

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

            if (_aligning)
            {
                _alignmentPlayer?.Pause();
                _btnPlay.Text = "▶ 播放";
                _circularStatus.Recording = false;
                _circularStatus.Countdown = 0;
                _labelStatus.Text = "原声已暂停；按 R 从断点重播，或继续标记。";
                return;
            }

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
            UpdateHeaderInfo(index);
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
                        _labelStatus.Text = _wholeBook ? "已播放完全书高亮句子。" : "已播放到最后一页句子。";
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
            if (_aligning)
            {
                // 对齐必须按句顺序推进，点击列表只作查看，不改变当前句
                if (!_suppressListSelection && _alignmentSession != null)
                {
                    _suppressListSelection = true;
                    try
                    {
                        // 全部标记完成后 CurrentIndex 可能等于句数，需夹取到末句避免越界
                        int count = _listSentences.Items.Count;
                        _listSentences.SelectedIndex = count == 0
                            ? -1
                            : Math.Clamp(_alignmentSession.CurrentIndex, 0, count - 1);
                    }
                    finally
                    {
                        _suppressListSelection = false;
                    }
                }
                return;
            }

            if (_listSentences.SelectedIndex < 0 || _practice == null)
                return;

            SetCurrentIndex(_listSentences.SelectedIndex, play: false);
        }

        private void OnSentenceDoubleClick(object? sender, EventArgs e)
        {
            if (_aligning)
                return;

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
                    ResolveSentencePage(index),
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
                    PersistSentenceRecordings(ResolveSentencePage(index));
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
                        PersistSentenceScores(ResolveSentencePage(index));
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

            var segment = SegmentForRow(index);
            if (segment == null)
            {
                _labelStatus.Text = "该句没有绑定的原声切片，录音未保存；可切换到文字跟读。";
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
            // 整本模式可能从书中间打开，因此从当前句开始预热。
            int start = _currentIndex < 0 ? 0 : _currentIndex;
            int count = Math.Min(3, _practice.Sentences.Count - start);
            if (count > 0)
                FireAndForget(_practice.PrefetchAsync(start, count));
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_finishingAlignment)
            {
                e.Cancel = true;
                _labelStatus.Text = "正在生成原声切片，请稍候…";
                return;
            }

            if (_aligning && _alignmentSession?.CreatedSegments.Count > 0)
            {
                var result = MessageBox.Show(
                    this,
                    "原声对齐还未完成，是否保存已标记的切片？",
                    "原声对齐",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (result == DialogResult.Yes)
                {
                    e.Cancel = true;
                    FireAndForget(FinishAlignmentAndCloseAsync());
                    return;
                }

                // No：丢弃本次未保存的打点，已有切片与录音不受影响
                ExitAlignmentMode();
            }

            base.OnFormClosing(e);
        }

        private async Task FinishAlignmentAndCloseAsync()
        {
            await FinishAlignmentAsync();
            if (!_aligning && IsHandleCreated && !IsDisposed)
                Close();
        }

        private void OnFormClosed(object? sender, FormClosedEventArgs e)
        {
            _countdownTimer.Stop();
            _countdownTimer.Dispose();
            _recordLimitTimer.Stop();
            _recordLimitTimer.Dispose();
            PdfSidebarTheme.ThemeChanged -= OnSidebarThemeChanged;

            StopRecordingIfAny(suppressAdvance: true);

            if (_alignmentPlayer != null)
            {
                _alignmentPlayer.PlaybackFinished -= OnAlignmentPlaybackFinished;
                _alignmentPlayer.PositionChanged -= OnAlignmentPositionChanged;
                _alignmentPlayer.Dispose();
                _alignmentPlayer = null;
            }

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
            foreach (var button in new[] { _btnTakePreview, _btnTakeChoose, _btnTakeDelete, _btnCompose, _btnAlign })
            {
                button.BackColor = palette.SurfaceAlt;
                button.ForeColor = palette.Text;
            }
            Invalidate(true);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // 对齐模式独占空格：结束当前句并切片；普通模式仍为播放/暂停
            if (_aligning)
            {
                switch (keyData)
                {
                    case Keys.Space:
                        MarkAlignmentCue();
                        return true;
                    case Keys.R:
                        ReplayAlignmentFromCursor();
                        return true;
                    case Keys.Right:
                        SkipAlignmentCue();
                        return true;
                    case Keys.Back:
                        UndoAlignmentCue();
                        return true;
                    case Keys.Escape:
                        FireAndForget(FinishAlignmentAsync());
                        return true;
                }
            }

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
