using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls.Cards
{
    /// <summary>
    /// 功能入口卡片控件
    /// 大图标 + 标题 + 描述，渐变背景，悬停浮起效果
    /// </summary>
    public class FeatureCard : UserControl
    {
        private string _icon = "📚";
        private string _title = "功能名称";
        private string _subtitle = string.Empty;
        private string _description = "功能描述";
        private Color _startColor = Color.FromArgb(99, 102, 241);
        private Color _endColor = Color.FromArgb(139, 92, 246);
        private Color _textColor = Color.White;
        private int _cornerRadius = 16;
        private bool _isHovered;
        private int _iconSize = 48;
        private LinearGradientMode _gradientMode = LinearGradientMode.Vertical;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        [DefaultValue("📚")]
        public string Icon
        {
            get => _icon;
            set { _icon = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        [DefaultValue("功能名称")]
        public string Title
        {
            get => _title;
            set { _title = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        public string Subtitle
        {
            get => _subtitle;
            set { _subtitle = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        [DefaultValue("功能描述")]
        public string Description
        {
            get => _description;
            set { _description = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        public Color PrimaryColor
        {
            get => _startColor;
            set { _startColor = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        public Color StartColor
        {
            get => _startColor;
            set { _startColor = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        public Color EndColor
        {
            get => _endColor;
            set { _endColor = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        public Color TextColor
        {
            get => _textColor;
            set { _textColor = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        [DefaultValue(16)]
        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(48)]
        public int IconSize
        {
            get => _iconSize;
            set { _iconSize = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(LinearGradientMode.Vertical)]
        public LinearGradientMode GradientMode
        {
            get => _gradientMode;
            set { _gradientMode = value; Invalidate(); }
        }

        public event EventHandler? CardClicked;

        public FeatureCard()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = Color.Transparent;
            Size = new Size(160, 150);
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
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            var shadowOffset = _isHovered ? 10 : 5;
            var shadowAlpha = _isHovered ? 50 : 25;
            var yOffset = _isHovered ? -4 : 0;

            using var shadowBrush = new SolidBrush(Color.FromArgb(shadowAlpha, Color.Black));
            var shadowRect = new Rectangle(
                shadowOffset, shadowOffset - yOffset, Width - shadowOffset * 2, Height - shadowOffset * 2);
            using var shadowPath = RoundedRect(shadowRect, _cornerRadius);
            g.FillPath(shadowBrush, shadowPath);

            var cardRect = new Rectangle(0, yOffset, Width - 1, Height - 1 - yOffset);
            using var gradientBrush = new LinearGradientBrush(cardRect, _startColor, _endColor, _gradientMode);
            using var cardPath = RoundedRect(cardRect, _cornerRadius);
            g.FillPath(gradientBrush, cardPath);

            var iconFont = new Font("Segoe UI Emoji", _iconSize / 1.5f);
            var iconSize = g.MeasureString(_icon, iconFont);
            var iconX = (Width - iconSize.Width) / 2;
            var iconY = 20;

            using var iconShadowBrush = new SolidBrush(Color.FromArgb(50, Color.White));
            g.FillEllipse(iconShadowBrush, iconX - 8, iconY - 4, iconSize.Width + 16, iconSize.Height + 8);

            using var iconBrush = new SolidBrush(_textColor);
            g.DrawString(_icon, iconFont, iconBrush, iconX, iconY);

            var titleFont = new Font("微软雅黑", 12F, FontStyle.Bold);
            var titleSize = g.MeasureString(_title, titleFont);
            var titleX = (Width - titleSize.Width) / 2;
            var titleY = iconY + iconSize.Height + 10;

            using var titleBrush = new SolidBrush(_textColor);
            g.DrawString(_title, titleFont, titleBrush, titleX, titleY);

            var descFont = new Font("微软雅黑", 9F);
            var descSize = g.MeasureString(_description, descFont);
            var descY = titleY + titleSize.Height + 6;
            var maxDescWidth = Width - 20;

            string displayDesc = _description;
            if (descSize.Width > maxDescWidth)
            {
                while (descSize.Width > maxDescWidth && displayDesc.Length > 1)
                {
                    displayDesc = displayDesc.Substring(0, displayDesc.Length - 1);
                    descSize = g.MeasureString(displayDesc + "...", descFont);
                }
                displayDesc += "...";
            }

            var descX = (Width - descSize.Width) / 2;

            using var descBrush = new SolidBrush(Color.FromArgb(200, _textColor));
            g.DrawString(displayDesc, descFont, descBrush, descX, descY);

            if (_isHovered)
            {
                using var borderPen = new Pen(Color.FromArgb(80, Color.White), 2);
                g.DrawPath(borderPen, cardPath);
            }
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // FeatureCard
            // 
            Name = "FeatureCard";
            Size = new Size(160, 150);
            ResumeLayout(false);

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
