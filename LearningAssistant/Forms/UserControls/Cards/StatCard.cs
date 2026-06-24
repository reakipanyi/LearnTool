using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls.Cards
{
    /// <summary>
    /// 数据概览卡片控件
    /// 显示图标、数值、标签，支持趋势指示和悬停效果
    /// </summary>
    public class StatCard : UserControl
    {
        private string _icon = "📊";
        private string _value = "0";
        private string _label = "统计";
        private string _trend = string.Empty;
        private Color _cardColor = Color.White;
        private Color _accentColor = Color.FromArgb(63, 81, 181);
        private Color _textColor = Color.FromArgb(33, 33, 33);
        private Color _labelColor = Color.FromArgb(117, 117, 117);
        private int _cornerRadius = 12;
        private bool _isHovered;
        private int _iconSize = 32;
        private int _valueFontSize = 24;
        private TrendDirection _trendDirection = TrendDirection.None;

        /// <summary>
        /// 趋势方向
        /// </summary>
        public enum TrendDirection
        {
            None,
            Up,
            Down,
            Flat
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        [DefaultValue("📊")]
        public string Icon
        {
            get => _icon;
            set { _icon = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        [DefaultValue("0")]
        public string Value
        {
            get => _value;
            set { _value = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        [DefaultValue("统计")]
        public string Label
        {
            get => _label;
            set { _label = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        [DefaultValue("")]
        public string Trend
        {
            get => _trend;
            set { _trend = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        [DefaultValue(TrendDirection.None)]
        public TrendDirection TrendDir
        {
            get => _trendDirection;
            set { _trendDirection = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        public Color AccentColor
        {
            get => _accentColor;
            set { _accentColor = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        public Color CardColor
        {
            get => _cardColor;
            set { _cardColor = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        [DefaultValue(32)]
        public int IconSize
        {
            get => _iconSize;
            set { _iconSize = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(24)]
        public int ValueFontSize
        {
            get => _valueFontSize;
            set { _valueFontSize = value; Invalidate(); }
        }

        public event EventHandler? CardClicked;

        public StatCard()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = Color.Transparent;
            Size = new Size(200, 110);
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            Invalidate();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            CardClicked?.Invoke(this, e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var shadowOffset = _isHovered ? 6 : 3;
            var shadowAlpha = _isHovered ? 40 : 20;

            using var shadowBrush = new SolidBrush(Color.FromArgb(shadowAlpha, Color.Black));
            var shadowRect = new Rectangle(shadowOffset, shadowOffset, Width - shadowOffset * 2, Height - shadowOffset * 2);
            using var shadowPath = RoundedRect(shadowRect, _cornerRadius);
            g.FillPath(shadowBrush, shadowPath);

            var cardRect = new Rectangle(0, _isHovered ? -2 : 0, Width, Height);
            using var cardBrush = new SolidBrush(_cardColor);
            using var cardPath = RoundedRect(cardRect, _cornerRadius);
            g.FillPath(cardBrush, cardPath);

            int paddingLeft = 14;
            int paddingTop = 14;
            int iconBoxSize = 44;

            using var accentBrush = new SolidBrush(Color.FromArgb(20, _accentColor));
            var accentRect = new Rectangle(paddingLeft, paddingTop, iconBoxSize, iconBoxSize);
            using var accentPath = RoundedRect(accentRect, 10);
            g.FillPath(accentBrush, accentPath);

            var iconFont = new Font("Segoe UI Emoji", _iconSize / 2f);
            var iconSize = g.MeasureString(_icon, iconFont);
            var iconX = paddingLeft + (iconBoxSize - iconSize.Width) / 2;
            var iconY = paddingTop + (iconBoxSize - iconSize.Height) / 2;

            using var iconBrush = new SolidBrush(_accentColor);
            g.DrawString(_icon, iconFont, iconBrush, iconX, iconY);

            int textX = paddingLeft + iconBoxSize + 12;
            int textMaxWidth = Width - textX - 14;

            var valueFont = new Font("微软雅黑", _valueFontSize, FontStyle.Bold);
            var valueSize = g.MeasureString(_value, valueFont);
            float valueY = paddingTop + 2;

            using var valueBrush = new SolidBrush(_textColor);
            using var valueFormat = new StringFormat
            {
                FormatFlags = StringFormatFlags.NoWrap,
                Trimming = StringTrimming.EllipsisCharacter
            };
            var valueRect = new RectangleF(textX, valueY, textMaxWidth, valueSize.Height);
            g.DrawString(_value, valueFont, valueBrush, valueRect, valueFormat);

            var labelFont = new Font("微软雅黑", 9F);
            var labelSize = g.MeasureString(_label, labelFont);
            float labelY = valueY + valueSize.Height + 6;

            using var labelBrush = new SolidBrush(_labelColor);

            if (!string.IsNullOrEmpty(_trend))
            {
                var trendFont = new Font("微软雅黑", 8F, FontStyle.Bold);
                var arrow = _trendDirection switch
                {
                    TrendDirection.Up => "↑ ",
                    TrendDirection.Down => "↓ ",
                    _ => ""
                };
                string trendText = arrow + _trend;
                var trendSize = g.MeasureString(trendText, trendFont);

                float labelMaxWidth = textMaxWidth - trendSize.Width - 10;
                if (labelMaxWidth < 30) labelMaxWidth = 30;

                var labelRect = new RectangleF(textX, labelY, labelMaxWidth, labelSize.Height);
                using var labelFormat = new StringFormat
                {
                    FormatFlags = StringFormatFlags.NoWrap,
                    Trimming = StringTrimming.EllipsisCharacter
                };
                g.DrawString(_label, labelFont, labelBrush, labelRect, labelFormat);

                Color trendColor = _trendDirection switch
                {
                    TrendDirection.Up => Color.FromArgb(76, 175, 80),
                    TrendDirection.Down => Color.FromArgb(244, 67, 54),
                    _ => _labelColor
                };

                using var trendBrush = new SolidBrush(trendColor);
                float trendX = Width - 14 - trendSize.Width;
                var trendRect = new RectangleF(trendX, labelY, trendSize.Width, trendSize.Height);
                using var trendFormat = new StringFormat
                {
                    FormatFlags = StringFormatFlags.NoWrap,
                    Alignment = StringAlignment.Far,
                    Trimming = StringTrimming.EllipsisCharacter
                };
                g.DrawString(trendText, trendFont, trendBrush, trendRect, trendFormat);
            }
            else
            {
                var labelRect = new RectangleF(textX, labelY, textMaxWidth, labelSize.Height);
                using var labelFormat = new StringFormat
                {
                    FormatFlags = StringFormatFlags.NoWrap,
                    Trimming = StringTrimming.EllipsisCharacter
                };
                g.DrawString(_label, labelFont, labelBrush, labelRect, labelFormat);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            var r = radius;

            path.AddArc(rect.X, rect.Y, r, r, 180, 90);
            path.AddArc(rect.X + rect.Width - r, rect.Y, r, r, 270, 90);
            path.AddArc(rect.X + rect.Width - r, rect.Y + rect.Height - r, r, r, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - r, r, r, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
