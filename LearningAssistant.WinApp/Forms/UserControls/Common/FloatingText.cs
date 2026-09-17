using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls.Common
{
    public class FloatingText : Control
    {
        private string _text = string.Empty;
        private Color _textColor = Color.FromArgb(255, 152, 0);
        private int _duration = 1000;
        private int _startY = 0;
        private int _endY = 0;
        private DateTime _startTime;
        private readonly System.Windows.Forms.Timer _animationTimer = new();
        private float _currentY = 0;
        private float _currentOpacity = 1f;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new string Text
        {
            get => _text;
            set
            {
                _text = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color TextColor
        {
            get => _textColor;
            set
            {
                _textColor = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Duration
        {
            get => _duration;
            set => _duration = Math.Max(200, value);
        }

        public FloatingText()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
            DoubleBuffered = true;
            Visible = false;

            _animationTimer.Interval = 20;
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        public void ShowAt(Control parent, int x, int y, string text)
        {
            _text = text;
            _startY = y;
            _endY = y - 60;
            _currentY = y;
            _currentOpacity = 1f;
            _startTime = DateTime.Now;

            Location = new Point(x, y);
            Size = new Size(120, 30);
            Visible = true;
            BringToFront();

            if (Parent != parent)
            {
                parent.Controls.Add(this);
            }

            _animationTimer.Start();
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            double elapsed = (DateTime.Now - _startTime).TotalMilliseconds;
            double progress = Math.Min(1.0, elapsed / _duration);

            _currentY = _startY + (int)((_endY - _startY) * EaseOutCubic(progress));
            _currentOpacity = 1f - (float)progress;

            Location = new Point(Location.X, (int)_currentY);

            if (progress >= 1.0)
            {
                _animationTimer.Stop();
                Visible = false;
            }

            Invalidate();
        }

        private static double EaseOutCubic(double t)
        {
            return 1 - Math.Pow(1 - t, 3);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (string.IsNullOrEmpty(_text) || _currentOpacity <= 0) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            int alpha = (int)(255 * _currentOpacity);
            alpha = Math.Clamp(alpha, 0, 255);

            using Font font = new("微软雅黑", 12F, FontStyle.Bold);
            using Brush textBrush = new SolidBrush(Color.FromArgb(alpha, _textColor));
            using Brush shadowBrush = new SolidBrush(Color.FromArgb(alpha / 3, 0, 0, 0));

            SizeF textSize = g.MeasureString(_text, font);
            float x = (Width - textSize.Width) / 2;
            float y = (Height - textSize.Height) / 2;

            g.DrawString(_text, font, shadowBrush, x + 1, y + 1);
            g.DrawString(_text, font, textBrush, x, y);
        }
    }
}
