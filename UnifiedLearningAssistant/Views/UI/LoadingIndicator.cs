namespace LearningAssistant.Views.UI
{
    // 新增功能：中等级 - UI响应性改进，加载指示器
    public class LoadingIndicator : Control
    {
        private bool _isLoading = false;
        private int _angle = 0;
        private System.Windows.Forms.Timer? _timer;

        public LoadingIndicator()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);
            DoubleBuffered = true;
            BackColor = Color.White;
            Size = new Size(50, 50);

            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 30;
            _timer.Tick += Timer_Tick;
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    if (_isLoading)
                        _timer?.Start();
                    else
                        _timer?.Stop();
                    Invalidate();
                }
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _angle = (_angle + 10) % 360;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!_isLoading) return;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var centerX = Width / 2f;
            var centerY = Height / 2f;
            var radius = Math.Min(centerX, centerY) - 5;

            g.TranslateTransform(centerX, centerY);
            g.RotateTransform(_angle);

            for (int i = 0; i < 8; i++)
            {
                var alpha = (255 - i * 25);
                var color = Color.FromArgb(alpha, Color.FromArgb(33, 150, 243));
                using var brush = new SolidBrush(color);

                var angle = i * 45;
                var x = (float)(Math.Cos(angle * Math.PI / 180) * radius * 0.7);
                var y = (float)(Math.Sin(angle * Math.PI / 180) * radius * 0.7);

                g.FillEllipse(brush, x - 4, y - 4, 8, 8);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Stop();
                _timer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
