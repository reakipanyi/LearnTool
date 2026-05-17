
namespace UnifiedLearningAssistant.Views.UI
{
    public class ProgressRingControl : Control
    {
        private float _progress = 0.5f;
        private float _targetProgress = 0.5f;
        private readonly System.Windows.Forms.Timer _animationTimer = new System.Windows.Forms.Timer();
        private Color _progressColor = Color.FromArgb(52, 199, 89);
        private Color _trackColor = Color.FromArgb(240, 240, 240);
        private Color _textColor = Color.FromArgb(33, 33, 33);
        private string _centerText = string.Empty;

        public float Progress
        {
            get => _targetProgress;
            set
            {
                _targetProgress = Math.Clamp(value, 0, 1);
                _animationTimer.Start();
            }
        }

        public Color ProgressColor
        {
            get => _progressColor;
            set
            {
                _progressColor = value;
                Invalidate();
            }
        }

        public Color TrackColor
        {
            get => _trackColor;
            set
            {
                _trackColor = value;
                Invalidate();
            }
        }

        public Color TextColor
        {
            get => _textColor;
            set
            {
                _textColor = value;
                Invalidate();
            }
        }

        public string CenterText
        {
            get => _centerText;
            set
            {
                _centerText = value;
                Invalidate();
            }
        }

        public ProgressRingControl()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            DoubleBuffered = true;
            BackColor = Color.Transparent;

            _animationTimer.Interval = 16;
            _animationTimer.Tick += OnAnimationTick;
        }

        private void OnAnimationTick(object? sender, EventArgs e)
        {
            const float animationSpeed = 0.05f;
            float diff = _targetProgress - _progress;

            if (Math.Abs(diff) < 0.001f)
            {
                _progress = _targetProgress;
                _animationTimer.Stop();
            }
            else
            {
                _progress += diff * animationSpeed;
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int size = Math.Min(Width, Height) - 20;
            int x = (Width - size) / 2;
            int y = (Height - size) / 2;
            float strokeWidth = size * 0.12f;

            using (var trackPen = new Pen(_trackColor, strokeWidth))
            {
                g.DrawEllipse(trackPen, x, y, size, size);
            }

            using (var progressPen = new Pen(_progressColor, strokeWidth))
            {
                progressPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                progressPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                float startAngle = -90;
                float sweepAngle = _progress * 360;

                g.DrawArc(progressPen, x, y, size, size, startAngle, sweepAngle);
            }

            if (!string.IsNullOrEmpty(_centerText))
            {
                using var font = new Font("Microsoft YaHei", size * 0.18f, FontStyle.Bold);
                using var textBrush = new SolidBrush(_textColor);

                var textSize = g.MeasureString(_centerText, font);
                float textX = x + (size - textSize.Width) / 2;
                float textY = y + (size - textSize.Height) / 2;

                g.DrawString(_centerText, font, textBrush, textX, textY);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

