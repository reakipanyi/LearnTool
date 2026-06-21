using LearningAssistant.Common.UI;
using LearningAssistant.Services.Learning;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls
{
    public class PomodoroTimer : UserControl
    {
        private IPomodoroService? _pomodoroService;
        private System.Windows.Forms.Timer? _uiTimer;
        private TimeSpan _displayTime = TimeSpan.FromMinutes(25);
        private PomodoroState _displayState = PomodoroState.Idle;
        private int _completedCount;
        private int _borderRadius = 16;
        private float _progressAngle = 360f;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IPomodoroService? PomodoroService
        {
            get => _pomodoroService;
            set
            {
                if (_pomodoroService != null)
                {
                    _pomodoroService.StateChanged -= OnStateChanged;
                    _pomodoroService.Tick -= OnTick;
                }

                _pomodoroService = value;

                if (_pomodoroService != null)
                {
                    _pomodoroService.StateChanged += OnStateChanged;
                    _pomodoroService.Tick += OnTick;
                    UpdateDisplay();
                }
            }
        }

        [Category("外观")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int BorderRadius
        {
            get => _borderRadius;
            set
            {
                _borderRadius = Math.Max(0, value);
                Invalidate();
            }
        }

        public event EventHandler? StartClicked;
        public event EventHandler? PauseClicked;
        public event EventHandler? ResetClicked;
        public event EventHandler? SkipClicked;

        public PomodoroTimer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint
                   | ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Size = new Size(280, 340);

            _uiTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _uiTimer.Tick += (s, e) => Invalidate();
            _uiTimer.Start();
        }

        private void OnStateChanged(object? sender, PomodoroStateChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnStateChanged(sender, e)));
                return;
            }

            _displayState = e.NewState;
            _displayTime = e.Duration;
            UpdateProgress();
            Invalidate();
        }

        private void OnTick(object? sender, TimeSpan remaining)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnTick(sender, remaining)));
                return;
            }

            _displayTime = remaining;
            UpdateProgress();
        }

        private void UpdateDisplay()
        {
            if (_pomodoroService == null) return;

            _displayState = _pomodoroService.CurrentState;
            _displayTime = _pomodoroService.TimeRemaining;
            _completedCount = _pomodoroService.TodayCompletedPomodoros;
            UpdateProgress();
            Invalidate();
        }

        private void UpdateProgress()
        {
            if (_pomodoroService == null || _pomodoroService.TotalDuration.TotalSeconds == 0)
            {
                _progressAngle = 360f;
                return;
            }

            var elapsed = _pomodoroService.TotalDuration - _displayTime;
            var progress = (float)(elapsed.TotalSeconds / _pomodoroService.TotalDuration.TotalSeconds);
            _progressAngle = 360f * (1f - progress);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            var rect = ClientRectangle;
            var padding = 10;
            var contentRect = new Rectangle(
                rect.X + padding,
                rect.Y + padding,
                rect.Width - padding * 2,
                rect.Height - padding * 2
            );

            using var cardPath = GdiHelper.CreateRoundedRectPath(contentRect, _borderRadius);
            using var bgBrush = new LinearGradientBrush(
                contentRect,
                Color.FromArgb(255, 248, 250),
                Color.FromArgb(255, 240, 245),
                LinearGradientMode.Vertical);
            g.FillPath(bgBrush, cardPath);

            var colors = GetStateColors(_displayState);

            var centerX = contentRect.X + contentRect.Width / 2f;
            var circleY = contentRect.Y + 70;
            var circleRadius = Math.Min(contentRect.Width, 120) / 2f;
            var circleRect = new RectangleF(
                centerX - circleRadius,
                circleY - circleRadius,
                circleRadius * 2,
                circleRadius * 2
            );

            using var bgCirclePen = new Pen(Color.FromArgb(30, 0, 0, 0), 10);
            g.DrawEllipse(bgCirclePen, circleRect);

            if (_displayState != PomodoroState.Idle)
            {
                using var progressPen = new Pen(colors.primary, 10);
                progressPen.StartCap = LineCap.Round;
                progressPen.EndCap = LineCap.Round;
                g.DrawArc(progressPen, circleRect, -90, _progressAngle);
            }
            else
            {
                using var progressPen = new Pen(colors.primary, 10);
                g.DrawEllipse(progressPen, circleRect);
            }

            var timeText = FormatTime(_displayState == PomodoroState.Idle
                ? TimeSpan.FromMinutes(_pomodoroService?.Settings.StudyMinutes ?? 25)
                : _displayTime);

            using var timeFont = new Font("Consolas", 28F, FontStyle.Bold);
            var timeSize = g.MeasureString(timeText, timeFont);
            g.DrawString(timeText, timeFont,
                new SolidBrush(colors.text),
                centerX - timeSize.Width / 2,
                circleY - timeSize.Height / 2 - 5);

            var stateText = GetStateText(_displayState);
            using var stateFont = new Font("微软雅黑", 10F);
            var stateSize = g.MeasureString(stateText, stateFont);
            g.DrawString(stateText, stateFont,
                new SolidBrush(Color.FromArgb(120, 120, 120)),
                centerX - stateSize.Width / 2,
                circleY + 35);

            var titleY = contentRect.Y + 15;
            var titleText = "🍅 专注模式";
            using var titleFont = new Font("微软雅黑", 13F, FontStyle.Bold);
            var titleSize = g.MeasureString(titleText, titleFont);
            g.DrawString(titleText, titleFont,
                new SolidBrush(Color.FromArgb(50, 50, 50)),
                centerX - titleSize.Width / 2,
                titleY);

            var countText = $"今日完成 {_completedCount} 个番茄";
            using var countFont = new Font("微软雅黑", 9F);
            var countSize = g.MeasureString(countText, countFont);
            g.DrawString(countText, countFont,
                new SolidBrush(Color.FromArgb(150, 150, 150)),
                centerX - countSize.Width / 2,
                contentRect.Bottom - 75);

            var btnY = contentRect.Bottom - 55;
            var btnWidth = 70;
            var btnHeight = 36;
            var btnSpacing = 10;
            var totalBtnWidth = btnWidth * 2 + btnSpacing;
            var btnStartX = centerX - totalBtnWidth / 2;

            if (_displayState == PomodoroState.Idle || _displayState == PomodoroState.Paused)
            {
                DrawButton(g,
                    new Rectangle((int)(btnStartX), btnY, btnWidth, btnHeight),
                    "开始",
                    Color.FromArgb(76, 175, 80),
                    Color.FromArgb(76, 175, 80));
            }
            else
            {
                DrawButton(g,
                    new Rectangle((int)(btnStartX), btnY, btnWidth, btnHeight),
                    "暂停",
                    Color.FromArgb(255, 152, 0),
                    Color.FromArgb(255, 152, 0));
            }

            DrawButton(g,
                new Rectangle((int)(btnStartX + btnWidth + btnSpacing), btnY, btnWidth, btnHeight),
                "重置",
                Color.FromArgb(158, 158, 158),
                Color.FromArgb(158, 158, 158));

            using var borderPen = new Pen(Color.FromArgb(25, 0, 0, 0), 1);
            g.DrawPath(borderPen, cardPath);
        }

        private static void DrawButton(Graphics g, Rectangle rect, string text, Color startColor, Color endColor)
        {
            using var path = GdiHelper.CreateRoundedRectPath(rect, 8);
            using var brush = new LinearGradientBrush(rect, startColor, endColor, LinearGradientMode.Vertical);
            g.FillPath(brush, path);

            TextRenderer.DrawText(g, text,
                new Font("微软雅黑", 10F, FontStyle.Bold),
                rect, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private static string FormatTime(TimeSpan time)
        {
            return $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}";
        }

        private static string GetStateText(PomodoroState state) => state switch
        {
            PomodoroState.Idle => "准备开始",
            PomodoroState.Studying => "专注学习中",
            PomodoroState.ShortBreak => "短休息",
            PomodoroState.LongBreak => "长休息",
            PomodoroState.Paused => "已暂停",
            _ => ""
        };

        private static (Color primary, Color text) GetStateColors(PomodoroState state) => state switch
        {
            PomodoroState.Studying => (Color.FromArgb(255, 87, 34), Color.FromArgb(50, 50, 50)),
            PomodoroState.ShortBreak => (Color.FromArgb(76, 175, 80), Color.FromArgb(50, 50, 50)),
            PomodoroState.LongBreak => (Color.FromArgb(33, 150, 243), Color.FromArgb(50, 50, 50)),
            PomodoroState.Paused => (Color.FromArgb(255, 152, 0), Color.FromArgb(50, 50, 50)),
            _ => (Color.FromArgb(158, 158, 158), Color.FromArgb(80, 80, 80))
        };

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button != MouseButtons.Left) return;

            var contentRect = new Rectangle(10, 10, Width - 20, Height - 20);
            var centerX = contentRect.X + contentRect.Width / 2f;
            var btnY = contentRect.Bottom - 55;
            var btnWidth = 70;
            var btnHeight = 36;
            var btnSpacing = 10;
            var totalBtnWidth = btnWidth * 2 + btnSpacing;
            var btnStartX = centerX - totalBtnWidth / 2;

            var startBtnRect = new Rectangle((int)btnStartX, btnY, btnWidth, btnHeight);
            var resetBtnRect = new Rectangle((int)(btnStartX + btnWidth + btnSpacing), btnY, btnWidth, btnHeight);

            if (startBtnRect.Contains(e.Location))
            {
                if (_displayState == PomodoroState.Idle || _displayState == PomodoroState.Paused)
                    StartClicked?.Invoke(this, EventArgs.Empty);
                else
                    PauseClicked?.Invoke(this, EventArgs.Empty);
            }
            else if (resetBtnRect.Contains(e.Location))
            {
                ResetClicked?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _uiTimer?.Stop();
                _uiTimer?.Dispose();

                if (_pomodoroService != null)
                {
                    _pomodoroService.StateChanged -= OnStateChanged;
                    _pomodoroService.Tick -= OnTick;
                }
            }
            base.Dispose(disposing);
        }
    }
}
