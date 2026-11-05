using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using LearningAssistant.Models.Audio;
using LearningAssistant.Services.Audio;

namespace LearningAssistant.Common.UI
{
    /// <summary>点句预览请求；ContinueAfter 记录 MouseDown 时的 Shift 状态。</summary>
    public sealed class SentencePreviewRequestedEventArgs
    {
        public AudioSentenceSpan Span { get; init; } = new();

        public bool ContinueAfter { get; init; }
    }

    /// <summary>拖动手动边界完成；IsStart 表示拖的是句首还是句尾。</summary>
    public sealed class BoundaryDragCompletedEventArgs
    {
        public AudioSentenceSpan Span { get; init; } = new();

        public bool IsStart { get; init; }

        public int TimeMs { get; init; }
    }

    /// <summary>
    /// 句子刻度时间轴：双层刻度（页码 + 句子）、点句命中、悬停/拖动与键盘。
    /// 绘制按像素桶预聚合，命中测试仍用全量二分，避免整本 12000 句时每帧扫描。
    /// </summary>
    public sealed class AudioTimelineBar : Control, ISidebarThemeAware
    {
        private const int EdgePaddingDips = 9;
        private const int TrackThicknessDips = 4;
        private const int ThumbIdleDips = 12;
        private const int ThumbActiveDips = 14;
        private const int DragThresholdDips = 3;
        private const int TickHitToleranceDips = 5;
        private const int PageHitToleranceDips = 6;
        private const int HoverDelayMs = 150;

        private IReadOnlyList<AudioSentenceSpan> _sentences = Array.Empty<AudioSentenceSpan>();
        private IReadOnlyList<AudioPageAnchor> _pageAnchors = Array.Empty<AudioPageAnchor>();
        private IReadOnlyList<float>? _waveform;
        private TimeSpan _duration;
        private TimeSpan _position;
        private double _analysisProgress = -1;
        private bool _pointSentenceMode = true;
        private bool _showPageLayer;
        private bool _showMarkers = true;

        private int _sentenceVersion;
        private MarkerBucket[] _buckets = Array.Empty<MarkerBucket>();
        private int _bucketWidth;
        private int _bucketDurationMs;
        private int _bucketVersion;

        private bool _hover;
        private bool _dragging;
        private bool _dragCandidate;

        private bool _correctionMode;
        private bool _boundaryDragging;
        private AudioSentenceSpan? _boundarySpan;
        private bool _boundaryIsStart;
        private int _boundaryTimeMs;
        private Point _mouseDownPoint;
        private Keys _modifiersAtDown;
        private TimeSpan _dragPosition;
        private bool _pageClickCandidate;

        private AudioSentenceSpan? _hoverSentence;
        private AudioPageAnchor? _hoverPage;
        private AudioSentenceSpan? _pendingHoverSentence;
        private System.Windows.Forms.Timer? _hoverTimer;
        private Font? _progressFont;
        private EventHandler? _themeHandler;

        public AudioTimelineBar()
        {
            SetStyle(ControlStyles.UserPaint
                     | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.ResizeRedraw
                     | ControlStyles.SupportsTransparentBackColor
                     | ControlStyles.Selectable, true);

            BackColor = Color.Transparent;
            Size = new Size(200, 24);
            TabStop = true;
            Cursor = Cursors.Hand;
            AccessibleName = "音频进度与句子刻度";

            _themeHandler = (_, _) => Invalidate();
            PdfSidebarTheme.ThemeChanged += _themeHandler;
        }

        /// <summary>拖动/Alt 单击松手，或键盘定位。</summary>
        public event EventHandler<TimeSpan>? SeekRequested;

        /// <summary>拖动中实时位置，用于更新时间标签。</summary>
        public event EventHandler<TimeSpan>? DragPositionChanged;

        /// <summary>点句/点刻度预览请求。</summary>
        public event EventHandler<SentencePreviewRequestedEventArgs>? PreviewRequested;

        public event EventHandler<AudioPageAnchor>? PageClicked;

        public event EventHandler<AudioSentenceSpan?>? MarkerHovered;

        public event EventHandler<AudioPageAnchor?>? PageHovered;

        /// <summary>修正模式下拖动边界完成（已吸附 10ms）。</summary>
        public event EventHandler<BoundaryDragCompletedEventArgs>? BoundaryDragCompleted;

        /// <summary>修正模式下 Ctrl+Z 请求撤销。</summary>
        public event EventHandler? UndoRequested;

        /// <summary>波形包络（0..1，按整轨等分桶）；null 时不绘制。</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IReadOnlyList<float>? Waveform
        {
            get => _waveform;
            set
            {
                _waveform = value;
                Invalidate();
            }
        }

        /// <summary>
        /// 修正模式：显示边界手柄、禁用点句预览，拖动最近边界。
        /// 会话态，不写档案。
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CorrectionMode
        {
            get => _correctionMode;
            set
            {
                if (_correctionMode == value) return;
                _correctionMode = value;
                _boundaryDragging = false;
                _boundarySpan = null;
                ClearHover();
                Cursor = value ? Cursors.SizeWE : Cursors.Hand;
                Invalidate();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IReadOnlyList<AudioSentenceSpan> Sentences
        {
            get => _sentences;
            set
            {
                var sorted = (value ?? Array.Empty<AudioSentenceSpan>())
                    .OrderBy(span => span.StartMs)
                    .ThenBy(span => span.EndMs)
                    .ToList();

                for (int i = 0; i < sorted.Count; i++)
                    sorted[i].Index = i + 1;

                _sentences = sorted;
                _sentenceVersion++;
                RebuildBuckets();
                ClearHover(notify: false);
                Invalidate();
            }
        }

        /// <summary>
        /// 该句所在像素桶里被抽稀合并的句数（0 表示没有合并）。
        /// 只在刻度间距 &lt; 3px 时有意义，供悬停卡片显示「+N」。
        /// </summary>
        public int HiddenSentenceCountNear(AudioSentenceSpan span)
        {
            if (span == null || _sentences.Count <= 1 || DurationMs <= 0) return 0;

            int width = TrackWidth;
            if (width <= 0) return 0;

            double gapPx = width / (double)(_sentences.Count - 1);
            if (gapPx >= 3) return 0;

            EnsureBuckets();
            if (_buckets.Length == 0) return 0;

            int x = Math.Clamp(TrackX(span.StartMs) - TrackLeft, 0, _buckets.Length - 1);
            return Math.Max(0, _buckets[x].Count - 1);
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IReadOnlyList<AudioPageAnchor> PageAnchors
        {
            get => _pageAnchors;
            set
            {
                _pageAnchors = (value ?? Array.Empty<AudioPageAnchor>())
                    .OrderBy(anchor => anchor.StartMs)
                    .ToList();
                ClearHover(notify: false);
                Invalidate();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TimeSpan Duration
        {
            get => _duration;
            set
            {
                var clamped = value < TimeSpan.Zero ? TimeSpan.Zero : value;
                if (_duration == clamped) return;
                _duration = clamped;
                RebuildBuckets();
                Invalidate();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TimeSpan Position
        {
            get => _position;
            set
            {
                var clamped = value < TimeSpan.Zero
                    ? TimeSpan.Zero
                    : (value > _duration ? _duration : value);
                if (_position == clamped) return;
                _position = clamped;
                Invalidate();
            }
        }

        /// <summary>0..1；小于 0 表示未分析。</summary>
        [DefaultValue(-1d)]
        public double AnalysisProgress
        {
            get => _analysisProgress;
            set
            {
                if (Math.Abs(_analysisProgress - value) < 0.001) return;
                _analysisProgress = value;
                Invalidate();
            }
        }

        [DefaultValue(true)]
        public bool PointSentenceMode
        {
            get => _pointSentenceMode;
            set
            {
                if (_pointSentenceMode == value) return;
                _pointSentenceMode = value;
                ClearHover();
                Invalidate();
            }
        }

        [DefaultValue(false)]
        public bool ShowPageLayer
        {
            get => _showPageLayer;
            set
            {
                if (_showPageLayer == value) return;
                _showPageLayer = value;
                Invalidate();
            }
        }

        /// <summary>关闭后不画刻度，退化为普通自绘滑杆。</summary>
        [DefaultValue(true)]
        public bool ShowMarkers
        {
            get => _showMarkers;
            set
            {
                if (_showMarkers == value) return;
                _showMarkers = value;
                ClearHover();
                Invalidate();
            }
        }

        public void ApplyTheme(bool isDark) => Invalidate();

        protected override void OnPaint(PaintEventArgs e)
        {
            var palette = PdfSidebarTheme.PaletteFor(this);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int left = TrackLeft;
            int right = TrackRight;
            if (right <= left) return;

            int trackCenter = TrackCenterY;
            int trackHeight = S(TrackThicknessDips);
            int trackTop = trackCenter - trackHeight / 2;
            var trackRect = new Rectangle(left, trackTop, right - left, trackHeight);

            Color trackColor = Enabled ? palette.SurfaceAlt : Blend(palette.SurfaceAlt, palette.Background);
            using (var path = GdiHelper.CreateRoundedRectPath(trackRect, Math.Max(1, trackHeight / 2)))
            using (var brush = new SolidBrush(trackColor))
            {
                g.FillPath(brush, path);
            }

            if (!ShowMarkers || _duration <= TimeSpan.Zero)
            {
                DrawPlayedFill(g, palette, trackRect, trackHeight);
                DrawWaveform(g, palette, trackRect, trackHeight);
                DrawPlayhead(g, palette, trackCenter);
                return;
            }

            DrawCurrentSentenceOverlay(g, palette, trackRect, trackHeight);
            DrawPlayedFill(g, palette, trackRect, trackHeight);
            DrawWaveform(g, palette, trackRect, trackHeight);
            DrawSentenceTicks(g, palette, trackCenter);
            DrawBoundaryHandles(g, palette, trackCenter);
            if (ShowPageLayer && _pageAnchors.Count > 0)
                DrawPageTicks(g, palette);
            DrawAnalysisProgress(g, palette, trackTop);
            DrawPlayhead(g, palette, trackCenter);
        }

        #region 绘制

        private void DrawWaveform(Graphics g, PdfSidebarPalette palette, Rectangle trackRect, int trackHeight)
        {
            var peaks = _waveform;
            if (peaks == null || peaks.Count == 0 || DurationMs <= 0) return;

            int width = trackRect.Width;
            if (width <= 1 || trackHeight <= 2) return;

            using var pen = new Pen(Color.FromArgb(96, palette.Muted), 1);
            int center = trackRect.Y + trackHeight / 2;
            int maxHalf = Math.Max(1, trackHeight / 2 - 1);

            for (int x = 0; x < width; x++)
            {
                int index = (int)((long)x * peaks.Count / width);
                if (index >= peaks.Count) index = peaks.Count - 1;

                float peak = peaks[index];
                if (peak <= 0.004f) continue;

                int half = Math.Max(1, (int)Math.Round(Math.Min(1f, peak) * maxHalf));
                g.DrawLine(pen, trackRect.X + x, center - half, trackRect.X + x, center + half);
            }
        }

        private void DrawCurrentSentenceOverlay(Graphics g, PdfSidebarPalette palette, Rectangle trackRect, int trackHeight)
        {
            int index = CurrentSentenceIndex();
            if (index < 0) return;

            var span = _sentences[index];
            int x1 = TrackX(span.StartMs);
            int x2 = TrackX(span.EndMs);
            if (x2 <= x1) return;

            var overlay = new Rectangle(x1, trackRect.Y, x2 - x1, trackHeight);
            using var brush = new SolidBrush(Color.FromArgb(153, palette.BrandSoft));
            using var path = GdiHelper.CreateRoundedRectPath(overlay, Math.Max(1, trackHeight / 2));
            g.FillPath(brush, path);
        }

        private void DrawPlayedFill(Graphics g, PdfSidebarPalette palette, Rectangle trackRect, int trackHeight)
        {
            int x = TrackX(PositionMs);
            int width = x - trackRect.X;
            if (width <= 0) return;

            var fillRect = new Rectangle(trackRect.X, trackRect.Y, width, trackHeight);
            using var brush = new SolidBrush(Enabled ? PdfSidebarTheme.Brand : palette.Muted);
            using var path = GdiHelper.CreateRoundedRectPath(fillRect, Math.Max(1, trackHeight / 2));
            g.FillPath(brush, path);
        }

        private void DrawSentenceTicks(Graphics g, PdfSidebarPalette palette, int trackCenter)
        {
            if (_sentences.Count == 0 || _duration <= TimeSpan.Zero) return;

            int tickTop = ShowPageLayer ? S(14) : S(6);
            int tickHeight = ShowPageLayer ? S(6) : S(14);
            int hitTop = tickTop;
            int hitHeight = tickHeight;
            double gapPx = _sentences.Count > 1
                ? TrackWidth / (double)(_sentences.Count - 1)
                : TrackWidth;

            int currentIndex = CurrentSentenceIndex();
            int hoverIndex = IndexOf(_hoverSentence);

            var drawn = new HashSet<int>();
            if (gapPx >= 3)
            {
                for (int i = 0; i < _sentences.Count; i++)
                    DrawSentenceTick(g, palette, _sentences[i], i, i == currentIndex, i == hoverIndex,
                        tickTop, tickHeight, drawn);
            }
            else
            {
                EnsureBuckets();
                for (int x = 0; x < _buckets.Length; x++)
                {
                    int index = _buckets[x].FirstIndex;
                    if (index < 0) continue;
                    DrawSentenceTick(g, palette, _sentences[index], index, index == currentIndex, index == hoverIndex,
                        tickTop, tickHeight, drawn);
                }

                // 抽稀只影响绘制：当前句、悬停句、首末句始终保留
                foreach (int index in MarkerBinner.ForcedIndices(_sentences.Count, currentIndex, hoverIndex))
                {
                    DrawSentenceTick(g, palette, _sentences[index], index, index == currentIndex, index == hoverIndex,
                        tickTop, tickHeight, drawn);
                }
            }

            // 悬停圆点只在刻度不再拥挤时出现
            if (gapPx >= 6 && _hoverSentence != null)
            {
                int x = TrackX(_hoverSentence.StartMs);
                int top = hitTop + hitHeight - S(1);
                using var brush = new SolidBrush(PdfSidebarTheme.Brand);
                g.FillEllipse(brush, x - S(1.5), top - S(1.5), S(3), S(3));
            }
        }

        private void DrawSentenceTick(
            Graphics g,
            PdfSidebarPalette palette,
            AudioSentenceSpan span,
            int index,
            bool isCurrent,
            bool isHover,
            int top,
            int height,
            HashSet<int> drawn)
        {
            if (!drawn.Add(index)) return;

            int x = TrackX(span.StartMs);
            if (x < TrackLeft || x > TrackRight) return;

            bool emphasized = isCurrent || isHover;
            int actualHeight = emphasized ? height + S(2) : height;
            Color color = emphasized
                ? PdfSidebarTheme.Brand
                : Color.FromArgb(153, palette.Muted);

            using var pen = new Pen(color, 1);
            g.DrawLine(pen, x, top, x, top + actualHeight);
        }

        private void DrawBoundaryHandles(Graphics g, PdfSidebarPalette palette, int trackCenter)
        {
            if (!_correctionMode || _sentences.Count == 0 || _duration <= TimeSpan.Zero) return;

            int width = TrackRight - TrackLeft;
            bool dense = width > 0 && width / (double)_sentences.Count < 6;
            var targets = dense
                ? new[] { HoverOrCurrentSpan() }.Where(s => s != null).Select(s => s!).Distinct().ToList()
                : (IEnumerable<AudioSentenceSpan>)_sentences;

            using var startBrush = new SolidBrush(Color.FromArgb(180, PdfSidebarTheme.Brand));
            using var endBrush = new SolidBrush(Color.FromArgb(150, palette.Border));
            int handleWidth = Math.Max(2, S(2));
            int handleHeight = S(9);

            foreach (var span in targets)
            {
                int startX = TrackX(span.StartMs);
                int endX = TrackX(span.EndMs);
                g.FillRectangle(startBrush, startX - handleWidth / 2,
                    trackCenter - handleHeight / 2, handleWidth, handleHeight);
                g.FillRectangle(endBrush, endX - handleWidth / 2,
                    trackCenter - handleHeight / 2, handleWidth, handleHeight);
            }

            if (_boundaryDragging && _boundarySpan != null)
            {
                int x = TrackX(_boundaryTimeMs);
                using var pen = new Pen(PdfSidebarTheme.Brand, 2);
                g.DrawLine(pen, x, trackCenter - S(10), x, trackCenter + S(10));
            }
        }

        private AudioSentenceSpan? HoverOrCurrentSpan()
        {
            if (_hoverSentence != null) return _hoverSentence;
            int index = CurrentSentenceIndex();
            return index >= 0 && index < _sentences.Count ? _sentences[index] : null;
        }

        private void DrawPageTicks(Graphics g, PdfSidebarPalette palette)
        {
            int currentIndex = CurrentPageAnchorIndex();

            for (int i = 0; i < _pageAnchors.Count; i++)
            {
                var anchor = _pageAnchors[i];
                int x = TrackX(anchor.StartMs);
                if (x < TrackLeft || x > TrackRight) continue;

                bool emphasized = i == currentIndex || anchor == _hoverPage;
                int top = S(2);
                int height = emphasized ? S(9) : S(8);
                Color color = Color.FromArgb(178, PdfSidebarTheme.Score);
                if (anchor.IsStartEstimated)
                    color = Color.FromArgb(120, palette.Muted);

                using var pen = new Pen(color, emphasized ? 2f : 1.5f);
                g.DrawLine(pen, x, top, x, top + height);

                using var dot = new SolidBrush(color);
                int d = emphasized ? S(3) : S(2);
                g.FillEllipse(dot, x - d / 2f, top - d / 2f, d, d);
            }
        }

        private void DrawAnalysisProgress(Graphics g, PdfSidebarPalette palette, int trackTop)
        {
            if (_analysisProgress < 0 || _analysisProgress >= 1) return;

            int left = TrackLeft;
            int right = TrackRight;
            int width = (int)Math.Round((right - left) * Math.Clamp(_analysisProgress, 0d, 1d));
            if (width <= 0) return;

            using var pen = new Pen(Color.FromArgb(128, PdfSidebarTheme.Brand), 1);
            g.DrawLine(pen, left, trackTop - S(2), left + width, trackTop - S(2));

            if (right - left >= S(120))
            {
                _progressFont ??= new Font(Font.FontFamily, 7f);
                string text = $"{(int)Math.Round(_analysisProgress * 100)}%";
                var size = g.MeasureString(text, _progressFont);
                using var brush = new SolidBrush(palette.Muted);
                g.DrawString(text, _progressFont, brush, right - size.Width - S(2), trackTop - size.Height - S(1));
            }
        }

        private void DrawPlayhead(Graphics g, PdfSidebarPalette palette, int trackCenter)
        {
            int x = TrackX((int)Math.Round((_dragging ? _dragPosition : _position).TotalMilliseconds));
            int diameter = S(_dragging || _hover ? ThumbActiveDips : ThumbIdleDips);
            var rect = new Rectangle(x - diameter / 2, trackCenter - diameter / 2, diameter, diameter);

            using (var outer = new SolidBrush(ResolveSurfaceColor()))
                g.FillEllipse(outer, rect);

            var inner = Rectangle.Inflate(rect, -S(2), -S(2));
            using (var brush = new SolidBrush(Enabled ? PdfSidebarTheme.Brand : palette.Muted))
                g.FillEllipse(brush, inner);
        }

        #endregion

        #region 命中与交互

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || !Enabled) return;

            Focus();
            Capture = true;
            _mouseDownPoint = e.Location;
            _modifiersAtDown = ModifierKeys;
            _dragging = false;
            _dragCandidate = true;
            _pageClickCandidate = ShowPageLayer
                && _pageAnchors.Count > 0
                && e.Y < S(12)
                && NearestPageAnchor(e.X) != null;

            if (_correctionMode)
            {
                var hit = NearestBoundary(e.X);
                if (hit != null)
                {
                    _boundaryDragging = true;
                    _boundarySpan = hit.Value.Span;
                    _boundaryIsStart = hit.Value.IsStart;
                    _boundaryTimeMs = hit.Value.IsStart ? hit.Value.Span.StartMs : hit.Value.Span.EndMs;
                    _dragCandidate = false;
                    _pageClickCandidate = false;
                    Invalidate();
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            bool hover = ClientRectangle.Contains(e.Location);
            if (hover != _hover)
            {
                _hover = hover;
                Invalidate();
            }

            if (_boundaryDragging && _boundarySpan != null)
            {
                int ms = (int)Math.Round(PositionFromX(e.X).TotalMilliseconds / 10d) * 10;
                int minMs = _boundaryIsStart ? 0 : _boundarySpan.StartMs + 100;
                int maxMs = _boundaryIsStart ? _boundarySpan.EndMs - 100 : DurationMsValue();
                _boundaryTimeMs = Math.Clamp(ms, Math.Max(0, minMs), Math.Max(0, maxMs));
                Invalidate();
                return;
            }

            if (_dragCandidate && !_dragging)
            {
                int dx = e.X - _mouseDownPoint.X;
                int dy = e.Y - _mouseDownPoint.Y;
                if (Math.Abs(dx) >= S(DragThresholdDips) || Math.Abs(dy) >= S(DragThresholdDips))
                {
                    _dragging = true;
                    _pageClickCandidate = false;
                    ClearHover();
                }
            }

            if (_dragging)
            {
                _dragPosition = PositionFromX(e.X);
                DragPositionChanged?.Invoke(this, _dragPosition);
                Invalidate();
                return;
            }

            UpdateHover(e.Location);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;

            bool wasDragging = _dragging;
            bool wasAlt = (_modifiersAtDown & Keys.Alt) == Keys.Alt;
            bool shift = (_modifiersAtDown & Keys.Shift) == Keys.Shift;

            bool wasBoundaryDragging = _boundaryDragging;
            var boundarySpan = _boundarySpan;
            bool boundaryIsStart = _boundaryIsStart;
            int boundaryTimeMs = _boundaryTimeMs;

            _dragging = false;
            _dragCandidate = false;
            _boundaryDragging = false;
            _boundarySpan = null;
            Capture = false;

            if (wasBoundaryDragging && boundarySpan != null)
            {
                BoundaryDragCompleted?.Invoke(this, new BoundaryDragCompletedEventArgs
                {
                    Span = boundarySpan,
                    IsStart = boundaryIsStart,
                    TimeMs = boundaryTimeMs
                });
                Invalidate();
                return;
            }

            if (_correctionMode)
            {
                SeekRequested?.Invoke(this, PositionFromX(e.X));
                Invalidate();
                return;
            }

            if (wasDragging)
            {
                SeekRequested?.Invoke(this, _dragPosition);
                Invalidate();
                return;
            }

            if (_pageClickCandidate)
            {
                var anchor = NearestPageAnchor(e.X);
                _pageClickCandidate = false;
                if (anchor != null)
                {
                    PageClicked?.Invoke(this, anchor);
                    return;
                }
            }

            if (wasAlt)
            {
                SeekRequested?.Invoke(this, PositionFromX(e.X));
                return;
            }

            var nearest = NearestSentence(e.X);
            if (nearest == null)
            {
                SeekRequested?.Invoke(this, PositionFromX(e.X));
                return;
            }

            if (PointSentenceMode)
            {
                PreviewRequested?.Invoke(this, new SentencePreviewRequestedEventArgs
                {
                    Span = nearest,
                    ContinueAfter = shift
                });
                return;
            }

            // 关闭点句模式：只有靠近刻度才预览，否则 seek
            int tickX = TrackX(nearest.StartMs);
            if (Math.Abs(e.X - tickX) <= S(TickHitToleranceDips))
            {
                PreviewRequested?.Invoke(this, new SentencePreviewRequestedEventArgs
                {
                    Span = nearest,
                    ContinueAfter = shift
                });
            }
            else
            {
                SeekRequested?.Invoke(this, PositionFromX(e.X));
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (!_dragging)
            {
                _hover = false;
                ClearHover();
                Invalidate();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!Enabled || _duration <= TimeSpan.Zero) return;

            // 与旧 TrackBar 行为一致：滚轮微调，交给上层 seek
            var step = TimeSpan.FromSeconds(e.Delta > 0 ? 5 : -5);
            var target = Position + step;
            if (target < TimeSpan.Zero) target = TimeSpan.Zero;
            if (target > _duration) target = _duration;
            SeekRequested?.Invoke(this, target);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Oemcomma:
                case Keys.OemPeriod:
                case Keys.Home:
                case Keys.End:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!Enabled || _duration <= TimeSpan.Zero) return;

            if (_correctionMode && e.Control && e.KeyCode == Keys.Z)
            {
                UndoRequested?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                    int seconds = e.Shift ? 1 : 5;
                    if (e.KeyCode == Keys.Left) seconds = -seconds;
                    var target = Position + TimeSpan.FromSeconds(seconds);
                    target = target < TimeSpan.Zero ? TimeSpan.Zero : (target > _duration ? _duration : target);
                    SeekRequested?.Invoke(this, target);
                    e.Handled = true;
                    break;

                case Keys.Oemcomma:
                case Keys.OemOpenBrackets:
                    PreviewAdjacent(-1);
                    e.Handled = true;
                    break;

                case Keys.OemPeriod:
                case Keys.OemCloseBrackets:
                    PreviewAdjacent(1);
                    e.Handled = true;
                    break;

                case Keys.Home:
                    SeekRequested?.Invoke(this, TimeSpan.Zero);
                    e.Handled = true;
                    break;

                case Keys.End:
                    SeekRequested?.Invoke(this, _duration);
                    e.Handled = true;
                    break;
            }
        }

        private void PreviewAdjacent(int direction)
        {
            if (_sentences.Count == 0) return;

            int index = CurrentSentenceIndex();
            if (index < 0)
                index = direction > 0 ? 0 : _sentences.Count - 1;
            else
                index = Math.Clamp(index + direction, 0, _sentences.Count - 1);

            var span = _sentences[index];
            if (_correctionMode)
            {
                // 修正模式下不预览，只把播放头定位到相邻句首
                SeekRequested?.Invoke(this, TimeSpan.FromMilliseconds(span.StartMs));
                return;
            }

            PreviewRequested?.Invoke(this, new SentencePreviewRequestedEventArgs
            {
                Span = span,
                ContinueAfter = false
            });
        }

        private void UpdateHover(Point location)
        {
            if (_dragging) return;

            AudioPageAnchor? page = null;
            if (ShowPageLayer && _pageAnchors.Count > 0 && location.Y < S(12))
                page = NearestPageAnchor(location.X);

            AudioSentenceSpan? sentence = page == null ? NearestSentence(location.X) : null;

            if (page != _hoverPage)
            {
                _hoverPage = page;
                PageHovered?.Invoke(this, page);
                Invalidate();
            }

            if (sentence == _hoverSentence)
                return;

            // 悬停变化 150ms 去抖，避免快速划过时闪烁
            _pendingHoverSentence = sentence;
            _hoverTimer ??= CreateHoverTimer();
            _hoverTimer.Stop();
            _hoverTimer.Start();
        }

        private System.Windows.Forms.Timer CreateHoverTimer()
        {
            var timer = new System.Windows.Forms.Timer { Interval = HoverDelayMs };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                if (_dragging) return;
                if (_hoverSentence == _pendingHoverSentence) return;
                _hoverSentence = _pendingHoverSentence;
                MarkerHovered?.Invoke(this, _hoverSentence);
                Invalidate();
            };
            return timer;
        }

        private void ClearHover(bool notify = true)
        {
            _hoverTimer?.Stop();
            _pendingHoverSentence = null;
            if (_hoverSentence != null)
            {
                _hoverSentence = null;
                if (notify) MarkerHovered?.Invoke(this, null);
            }
            if (_hoverPage != null)
            {
                _hoverPage = null;
                if (notify) PageHovered?.Invoke(this, null);
            }
        }

        private (AudioSentenceSpan Span, bool IsStart)? NearestBoundary(int x)
        {
            var span = NearestSentence(x);
            if (span == null) return null;

            int tolerance = S(TickHitToleranceDips);
            int startX = TrackX(span.StartMs);
            int endX = TrackX(span.EndMs);
            bool startHit = Math.Abs(x - startX) <= tolerance;
            bool endHit = Math.Abs(x - endX) <= tolerance;

            if (!startHit && !endHit) return null;
            if (startHit && endHit)
                return (span, Math.Abs(x - startX) <= Math.Abs(x - endX));
            return (span, startHit);
        }

        private int DurationMsValue() => (int)Math.Round(_duration.TotalMilliseconds);

        private AudioSentenceSpan? NearestSentence(int x)
        {
            if (_sentences.Count == 0 || _duration <= TimeSpan.Zero) return null;

            int ms = (int)Math.Round(PositionFromX(x).TotalMilliseconds);
            return MarkerHitTester.Nearest(_sentences, ms);
        }

        private AudioPageAnchor? NearestPageAnchor(int x)
        {
            if (_pageAnchors.Count == 0 || _duration <= TimeSpan.Zero) return null;

            int ms = (int)Math.Round(PositionFromX(x).TotalMilliseconds);
            int index = CountPageStartedAt(ms);
            var candidates = new List<int>(2);
            if (index >= 0) candidates.Add(index);
            if (index + 1 < _pageAnchors.Count) candidates.Add(index + 1);

            AudioPageAnchor? best = null;
            int bestDistance = int.MaxValue;
            foreach (int i in candidates)
            {
                int distance = Math.Abs(TrackX(_pageAnchors[i].StartMs) - x);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = _pageAnchors[i];
                }
            }

            return bestDistance <= S(PageHitToleranceDips) ? best : null;
        }

        private int CountPageStartedAt(int ms)
        {
            int lo = 0;
            int hi = _pageAnchors.Count - 1;
            int result = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (_pageAnchors[mid].StartMs <= ms)
                {
                    result = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            return result;
        }

        private int CountStartedAt(int ms)
        {
            int lo = 0;
            int hi = _sentences.Count - 1;
            int result = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (_sentences[mid].StartMs <= ms)
                {
                    result = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            return result;
        }

        private int CurrentSentenceIndex()
        {
            if (_sentences.Count == 0) return -1;
            return CountStartedAt(PositionMs);
        }

        private int CurrentPageAnchorIndex()
        {
            if (_pageAnchors.Count == 0) return -1;

            int ms = PositionMs;
            int lo = 0;
            int hi = _pageAnchors.Count - 1;
            int result = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (_pageAnchors[mid].StartMs <= ms)
                {
                    result = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            return result;
        }

        private int IndexOf(AudioSentenceSpan? span)
        {
            if (span == null) return -1;
            for (int i = 0; i < _sentences.Count; i++)
            {
                if (_sentences[i].StartMs == span.StartMs && _sentences[i].EndMs == span.EndMs)
                    return i;
            }
            return -1;
        }

        #endregion

        #region 几何与桶

        private float DpiScale => DeviceDpi / 96f;

        private int S(double dips) => (int)Math.Round(dips * DpiScale);

        private int TrackLeft => S(EdgePaddingDips);

        private int TrackRight => Math.Max(TrackLeft, Width - S(EdgePaddingDips) - 1);

        private int TrackWidth => Math.Max(1, TrackRight - TrackLeft);

        private int TrackCenterY => Height / 2;

        private int DurationMs => (int)Math.Round(_duration.TotalMilliseconds);

        private int PositionMs => (int)Math.Round(_position.TotalMilliseconds);

        private int TrackX(int ms)
        {
            if (DurationMs <= 0) return TrackLeft;
            double ratio = Math.Clamp(ms / (double)DurationMs, 0d, 1d);
            return TrackLeft + (int)Math.Round(ratio * TrackWidth);
        }

        private TimeSpan PositionFromX(int x)
        {
            if (DurationMs <= 0) return TimeSpan.Zero;
            double ratio = Math.Clamp((x - TrackLeft) / (double)TrackWidth, 0d, 1d);
            return TimeSpan.FromMilliseconds(ratio * DurationMs);
        }

        private void EnsureBuckets()
        {
            if (_bucketWidth == TrackWidth && _bucketDurationMs == DurationMs && _bucketVersion == _sentenceVersion)
                return;

            int width = TrackWidth;
            if (width <= 0)
            {
                _buckets = Array.Empty<MarkerBucket>();
                _bucketWidth = width;
                _bucketDurationMs = DurationMs;
                _bucketVersion = _sentenceVersion;
                return;
            }

            _buckets = MarkerBinner.Build(_sentences, width, DurationMs);
            _bucketWidth = width;
            _bucketDurationMs = DurationMs;
            _bucketVersion = _sentenceVersion;
        }

        private void RebuildBuckets()
        {
            _bucketWidth = -1;
            EnsureBuckets();
        }

        private Color ResolveSurfaceColor()
        {
            for (Control? parent = Parent; parent != null; parent = parent.Parent)
            {
                Color back = parent.BackColor;
                if (back.A == 255 && back != Color.Transparent)
                    return back;
            }

            return PdfSidebarTheme.PaletteFor(this).IsDark
                ? Color.FromArgb(45, 45, 45)
                : Color.White;
        }

        private static Color Blend(Color foreground, Color background)
        {
            const double alpha = 0.55;
            return Color.FromArgb(
                (int)(foreground.R * alpha + background.R * (1 - alpha)),
                (int)(foreground.G * alpha + background.G * (1 - alpha)),
                (int)(foreground.B * alpha + background.B * (1 - alpha)));
        }

        #endregion

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RebuildBuckets();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            if (!Enabled)
            {
                _dragging = false;
                _dragCandidate = false;
                Capture = false;
                ClearHover();
            }
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_themeHandler != null)
                    PdfSidebarTheme.ThemeChanged -= _themeHandler;
                _themeHandler = null;

                _hoverTimer?.Stop();
                _hoverTimer?.Dispose();
                _hoverTimer = null;
                _progressFont?.Dispose();
                _progressFont = null;
            }

            base.Dispose(disposing);
        }
    }
}
