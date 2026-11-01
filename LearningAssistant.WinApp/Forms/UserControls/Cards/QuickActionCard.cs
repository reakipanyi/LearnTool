using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls.Cards
{
    /// <summary>
    /// 首页快捷入口卡片控件。
    /// 与 StatCard 共用同一套视觉语言：圆角底、柔和阴影、着色图标块；
    /// 悬停时卡片轻微抬升并显示描边，键盘聚焦时用主色描边提示。
    /// </summary>
    public class QuickActionCard : UserControl
    {
        private const int IconBoxSize = 40;
        private const int PaddingLeft = 14;
        private const int PaddingRight = 14;

        private readonly Font _iconFont = new("Segoe UI Emoji", 18F);
        private readonly Font _titleFont = new("微软雅黑", 11F, FontStyle.Bold);
        private readonly Font _arrowFont = new("Segoe UI", 12F);

        private string _icon = "⚡";
        private string _title = "快捷入口";
        private Color _cardColor = Color.White;
        private Color _hoverColor = Color.FromArgb(248, 249, 252);
        private Color _textColor = Color.FromArgb(33, 33, 33);
        private Color _accentColor = Color.FromArgb(99, 102, 241);
        private Color _borderColor = Color.FromArgb(28, 33, 33, 33);
        private int _cornerRadius = 12;
        private bool _isHovered;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        [DefaultValue("⚡")]
        public string Icon
        {
            get => _icon;
            set { _icon = value ?? string.Empty; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        [DefaultValue("快捷入口")]
        public string Title
        {
            get => _title;
            set { _title = value ?? string.Empty; Invalidate(); }
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
        public Color HoverColor
        {
            get => _hoverColor;
            set { _hoverColor = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        public Color TextColor
        {
            get => _textColor;
            set { _textColor = value; Invalidate(); }
        }

        /// <summary>图标着色，用于区分入口类型，不铺满整张卡片。</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        public Color AccentColor
        {
            get => _accentColor;
            set { _accentColor = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Appearance")]
        [DefaultValue(12)]
        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = value; Invalidate(); }
        }

        public event EventHandler? ActionClicked;

        public QuickActionCard()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.Selectable,
                true);

            BackColor = Color.Transparent;
            Size = new Size(176, 84);
            Cursor = Cursors.Hand;
            TabStop = true;
            DoubleBuffered = true;
            AccessibleRole = AccessibleRole.PushButton;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            // 与统计卡一致：Tab 聚焦后回车/空格即可打开入口
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                ActionClicked?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
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
            ActionClicked?.Invoke(this, e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int shadowOffset = _isHovered ? 4 : 3;
            int shadowAlpha = _isHovered ? 34 : 18;

            // 阴影与卡片本体分开绘制；悬停只加深阴影，不位移卡片，避免圆角被控件边界裁掉
            using (var shadowBrush = new SolidBrush(Color.FromArgb(shadowAlpha, Color.Black)))
            using (var shadowPath = RoundedRect(
                new Rectangle(shadowOffset, shadowOffset, Width - shadowOffset * 2, Height - shadowOffset * 2),
                _cornerRadius))
            {
                g.FillPath(shadowBrush, shadowPath);
            }

            var cardRect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var cardPath = RoundedRect(cardRect, _cornerRadius);
            using (var cardBrush = new SolidBrush(_isHovered ? _hoverColor : _cardColor))
            {
                g.FillPath(cardBrush, cardPath);
            }

            // 常态用分隔线色描边，悬停/聚焦时换成强调色
            bool highlighted = _isHovered || Focused;
            var borderColor = highlighted ? _accentColor : _borderColor;
            int borderAlpha = highlighted ? 120 : borderColor.A;
            using (var borderPen = new Pen(
                highlighted ? Color.FromArgb(borderAlpha, borderColor) : borderColor, highlighted ? 1.5f : 1f))
            {
                g.DrawPath(borderPen, cardPath);
            }

            // 左侧着色图标块，与 StatCard 的图标处理保持一致
            int iconTop = (Height - IconBoxSize) / 2;
            var iconRect = new Rectangle(PaddingLeft, iconTop, IconBoxSize, IconBoxSize);
            using (var iconBackBrush = new SolidBrush(Color.FromArgb(_isHovered ? 40 : 26, _accentColor)))
            using (var iconBackPath = RoundedRect(iconRect, 10))
            {
                g.FillPath(iconBackBrush, iconBackPath);
            }

            var iconSize = g.MeasureString(_icon, _iconFont);
            using (var iconBrush = new SolidBrush(_accentColor))
            {
                g.DrawString(
                    _icon,
                    _iconFont,
                    iconBrush,
                    PaddingLeft + (IconBoxSize - iconSize.Width) / 2,
                    iconTop + (IconBoxSize - iconSize.Height) / 2);
            }

            // 标题：单行垂直居中，超出宽度时省略号截断
            int textX = PaddingLeft + IconBoxSize + 12;
            int arrowWidth = 14;
            int textMaxWidth = Math.Max(10, Width - textX - PaddingRight - arrowWidth);
            using (var titleBrush = new SolidBrush(_textColor))
            using (var titleFormat = new StringFormat
            {
                FormatFlags = StringFormatFlags.NoWrap,
                Trimming = StringTrimming.EllipsisCharacter,
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center
            })
            {
                var titleRect = new RectangleF(textX, 0, textMaxWidth, Height);
                g.DrawString(_title, _titleFont, titleBrush, titleRect, titleFormat);
            }

            // 右侧箭头作为“可打开”的轻提示，悬停时右移 1px
            var arrowSize = g.MeasureString("›", _arrowFont);
            float arrowX = Width - PaddingRight - arrowSize.Width + (_isHovered ? 1 : 0);
            using (var arrowBrush = new SolidBrush(Color.FromArgb(highlighted ? 200 : 90, _accentColor)))
            {
                g.DrawString(
                    "›",
                    _arrowFont,
                    arrowBrush,
                    arrowX,
                    (Height - arrowSize.Height) / 2);
            }

            if (Focused)
            {
                using var focusPen = new Pen(_accentColor, 2);
                g.DrawPath(focusPen, cardPath);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _iconFont.Dispose();
                _titleFont.Dispose();
                _arrowFont.Dispose();
            }
            base.Dispose(disposing);
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            var r = Math.Max(1, Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2));

            path.AddArc(rect.X, rect.Y, r, r, 180, 90);
            path.AddArc(rect.X + rect.Width - r, rect.Y, r, r, 270, 90);
            path.AddArc(rect.X + rect.Width - r, rect.Y + rect.Height - r, r, r, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - r, r, r, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
