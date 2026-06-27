using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls
{
    public class CircularProgressControl : Control
    {
        private int _currentValue;
        private int _maxValue;
        private string _labelText;
        private Color _progressColor;
        private Color _backColor;
        private Color _foreColor;

        public CircularProgressControl()
        {
            _currentValue = 0;
            _maxValue = 100;
            _labelText = "0/100";
            _progressColor = Color.FromArgb(76, 175, 80);
            _backColor = Color.FromArgb(220, 220, 220);
            _foreColor = Color.Black;
            DoubleBuffered = true;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CurrentValue
        {
            get => _currentValue;
            set
            {
                _currentValue = Math.Max(0, Math.Min(value, _maxValue));
                UpdateLabel();
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int MaxValue
        {
            get => _maxValue;
            set
            {
                _maxValue = Math.Max(1, value);
                _currentValue = Math.Min(_currentValue, _maxValue);
                UpdateLabel();
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string LabelText
        {
            get => _labelText;
            set
            {
                _labelText = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ProgressColor
        {
            get => _progressColor;
            set
            {
                _progressColor = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color TrackColor
        {
            get => _backColor;
            set
            {
                _backColor = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color TextColor
        {
            get => _foreColor;
            set
            {
                _foreColor = value;
                Invalidate();
            }
        }

        private void UpdateLabel()
        {
            int percent = _maxValue > 0 ? (int)((_currentValue * 100.0) / _maxValue) : 0;
            _labelText = $"{_currentValue}/{_maxValue}\n{percent}%";
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int centerX = Width / 2;
            int centerY = Height / 2;
            int radius = Math.Min(Width, Height) / 2 - 10;
            int lineWidth = 8;

            Rectangle outerRect = new Rectangle(centerX - radius, centerY - radius, radius * 2, radius * 2);
            Rectangle innerRect = new Rectangle(centerX - radius + lineWidth, centerY - radius + lineWidth,
                                               (radius - lineWidth) * 2, (radius - lineWidth) * 2);

            using (Pen trackPen = new Pen(_backColor, lineWidth))
            {
                trackPen.StartCap = LineCap.Round;
                trackPen.EndCap = LineCap.Round;
                g.DrawArc(trackPen, outerRect, 0, 360);
            }

            if (_currentValue > 0)
            {
                float angle = (_currentValue * 360.0f) / _maxValue;

                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new Point(centerX - radius, centerY - radius),
                    new Point(centerX + radius, centerY + radius),
                    _progressColor,
                    ControlPaint.Light(_progressColor, 20)))
                {
                    using (Pen progressPen = new Pen(brush, lineWidth))
                    {
                        progressPen.StartCap = LineCap.Round;
                        progressPen.EndCap = LineCap.Round;
                        g.DrawArc(progressPen, outerRect, -90, angle);
                    }
                }
            }

            using (SolidBrush brush = new SolidBrush(_foreColor))
            {
                StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                Font titleFont = new Font("微软雅黑", 10F, FontStyle.Bold);
                Font valueFont = new Font("微软雅黑", 8F);

                g.DrawString("每日目标", titleFont, brush, centerX, centerY - 18, format);
                g.DrawString(_labelText, valueFont, brush, centerX, centerY + 5, format);

                titleFont.Dispose();
                valueFont.Dispose();
            }
        }
    }
}