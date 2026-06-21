using LearningAssistant.Common.UI;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls
{
    /// <summary>
    /// 渐变进度条控件
    /// 支持圆角、渐变填充、百分比显示、动画效果
    /// </summary>
    public class ProgressBarEx : Control
    {
        private double _value;
        private double _maximum = 100;
        private int _cornerRadius;
        private Color _startColor = Color.FromArgb(33, 150, 243);
        private Color _endColor = Color.FromArgb(100, 181, 246);
        private Color _backColor = Color.FromArgb(240, 240, 245);
        private bool _showPercent;
        private bool _showValueText;
        private Color _percentColor = Color.White;
        private Color _valueTextColor = Color.FromArgb(102, 102, 102);
        private string _valueSuffix = "";
        private string _valuePrefix = "";
        private double _displayValue;
        private System.Windows.Forms.Timer? _animationTimer;
        private int _animationSpeed = 30;
        private bool _animateProgress = true;

        [Category("Behavior")]
        [DefaultValue(0d)]
        public double Value
        {
            get => _value;
            set
            {
                _value = Math.Clamp(value, 0, _maximum);
                if (_animateProgress)
                    StartAnimation();
                else
                {
                    _displayValue = _value;
                    Invalidate();
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(100d)]
        public double Maximum
        {
            get => _maximum;
            set
            {
                _maximum = Math.Max(1, value);
                _value = Math.Min(_value, _maximum);
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(0)]
        public int CornerRadius
        {
            get => _cornerRadius;
            set
            {
                _cornerRadius = Math.Max(0, value);
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        public Color StartColor
        {
            get => _startColor;
            set
            {
                _startColor = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        public Color EndColor
        {
            get => _endColor;
            set
            {
                _endColor = value;
                Invalidate();
            }
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]

        [Category("Appearance")]
        public Color BackColor2
        {
            get => _backColor;
            set
            {
                _backColor = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        [DefaultValue(false)]
        public bool ShowPercent
        {
            get => _showPercent;
            set
            {
                _showPercent = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(false)]
        public bool ShowValueText
        {
            get => _showValueText;
            set
            {
                _showValueText = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color PercentColor
        {
            get => _percentColor;
            set
            {
                _percentColor = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ValueTextColor
        {
            get => _valueTextColor;
            set
            {
                _valueTextColor = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue("")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ValueSuffix
        {
            get => _valueSuffix;
            set
            {
                _valueSuffix = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue("")]
        public string ValuePrefix
        {
            get => _valuePrefix;
            set
            {
                _valuePrefix = value;
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool AnimateProgress
        {
            get => _animateProgress;
            set => _animateProgress = value;
        }

        public ProgressBarEx()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            Size = new Size(200, 10);
            _cornerRadius = Height / 2;
            DoubleBuffered = true;
        }

        private void StartAnimation()
        {
            if (_animationTimer != null) return;

            _animationTimer = new System.Windows.Forms.Timer();
            _animationTimer.Interval = 20;
            _animationTimer.Tick += (s, e) =>
            {
                double diff = _value - _displayValue;
                if (Math.Abs(diff) < 0.5)
                {
                    _displayValue = _value;
                    _animationTimer?.Stop();
                    _animationTimer?.Dispose();
                    _animationTimer = null;
                }
                else
                {
                    double step = diff * 0.2;
                    if (Math.Abs(step) < 0.1)
                        step = Math.Sign(diff) * 0.1;
                    _displayValue += step;
                }
                Invalidate();
            };
            _animationTimer.Start();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_cornerRadius == 0)
                _cornerRadius = Height / 2;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            int radius = Math.Min(_cornerRadius, Height / 2);
            var bgRect = new Rectangle(0, 0, Width - 1, Height - 1);

            using (var bgPath = GdiHelper.CreateRoundedRectPath(bgRect, radius))
            using (var bgBrush = new SolidBrush(_backColor))
            {
                g.FillPath(bgBrush, bgPath);
            }

            double percent = _maximum > 0 ? _displayValue / _maximum : 0;
            if (percent <= 0) return;

            int progressWidth = (int)(Width * Math.Min(percent, 1.0));
            if (progressWidth < radius * 2)
                progressWidth = radius * 2;

            var progressRect = new Rectangle(0, 0, progressWidth, Height - 1);

            using var progressPath = GdiHelper.CreateRoundedRectPath(progressRect, radius);
            using var progressBrush = new LinearGradientBrush(
                progressRect, _startColor, _endColor, LinearGradientMode.Horizontal);
            g.FillPath(progressBrush, progressPath);

            if (_showPercent && percent > 0.15)
            {
                int percentVal = (int)(percent * 100);
                string percentText = $"{percentVal}%";
                var font = new Font("微软雅黑", 8F, FontStyle.Bold);

                using var sf = new StringFormat
                {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Center
                };
                using var brush = new SolidBrush(_percentColor);

                var textRect = new Rectangle(0, 0, progressWidth, Height);
                g.DrawString(percentText, font, brush, textRect, sf);
            }

            if (_showValueText)
            {
                string valueText = $"{_valuePrefix}{_value:F0}{_valueSuffix}";
                var font = new Font("微软雅黑", 8F, FontStyle.Bold);

                using var sf = new StringFormat
                {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Far
                };
                using var brush = new SolidBrush(_valueTextColor);

                var textRect = new Rectangle(0, 0, Width - 4, Height);
                g.DrawString(valueText, font, brush, textRect, sf);
            }
        }

        public void SetValue(double value, bool animate = true)
        {
            if (!animate)
            {
                _value = Math.Clamp(value, 0, _maximum);
                _displayValue = _value;
                Invalidate();
            }
            else
            {
                Value = value;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer?.Stop();
                _animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
