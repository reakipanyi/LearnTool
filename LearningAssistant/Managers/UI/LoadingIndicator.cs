using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Managers
{
    public static class GraphicsExtensions
    {
        public static void DrawRoundedRectangle(this Graphics g, Pen pen, RectangleF rect, float radius)
        {
            using var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y + rect.Height - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - radius, radius, radius, 90, 90);
            path.CloseAllFigures();
            g.DrawPath(pen, path);
        }
    }

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
            BackColor = Color.FromArgb(245, 245, 245);
            Size = new Size(60, 60);
            ForeColor = Color.FromArgb(66, 133, 244);

            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 25;
            _timer.Tick += Timer_Tick;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
            var radius = Math.Min(centerX, centerY) - 4;

            using var bgBrush = new SolidBrush(Color.FromArgb(200, BackColor));
            g.FillEllipse(bgBrush, centerX - radius, centerY - radius, radius * 2, radius * 2);

            g.TranslateTransform(centerX, centerY);
            g.RotateTransform(_angle);

            for (int i = 0; i < 10; i++)
            {
                var alpha = (255 - i * 22);
                var color = Color.FromArgb(alpha, ForeColor);
                using var brush = new SolidBrush(color);

                var angle = i * 36;
                var x = (float)(Math.Cos(angle * Math.PI / 180) * radius * 0.65);
                var y = (float)(Math.Sin(angle * Math.PI / 180) * radius * 0.65);

                var dotSize = 5f + (10 - i) * 0.3f;
                g.FillEllipse(brush, x - dotSize / 2, y - dotSize / 2, dotSize, dotSize);
            }

            g.ResetTransform();

            DrawCenterIcon(g, centerX, centerY, radius * 0.45f);
        }

        private void DrawCenterIcon(Graphics g, float centerX, float centerY, float iconSize)
        {
            var halfSize = iconSize / 2f;
            var pen = new Pen(ForeColor, 2f);

            // 绘制机器人头部（圆角矩形）
            var headRect = new RectangleF(centerX - halfSize + 2, centerY - halfSize, halfSize * 1.5f, halfSize);
            g.DrawRoundedRectangle(pen, headRect, 4);

            // 绘制眼睛
            var eyeSize = 4f;
            var leftEye = new PointF(centerX - 6, centerY - halfSize / 2);
            var rightEye = new PointF(centerX + 6, centerY - halfSize / 2);
            using var eyeBrush = new SolidBrush(ForeColor);
            g.FillEllipse(eyeBrush, leftEye.X - eyeSize / 2, leftEye.Y - eyeSize / 2, eyeSize, eyeSize);
            g.FillEllipse(eyeBrush, rightEye.X - eyeSize / 2, rightEye.Y - eyeSize / 2, eyeSize, eyeSize);

            // 绘制天线
            g.DrawLine(pen, centerX - 8, centerY - halfSize - 3, centerX - 12, centerY - halfSize - 8);
            g.DrawLine(pen, centerX + 8, centerY - halfSize - 3, centerX + 12, centerY - halfSize - 8);
            g.FillEllipse(eyeBrush, centerX - 13, centerY - halfSize - 10, 4, 4);
            g.FillEllipse(eyeBrush, centerX + 11, centerY - halfSize - 10, 4, 4);

            pen.Dispose();
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
