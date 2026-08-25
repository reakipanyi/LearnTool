using LearningAssistant.Common.UI;
using LearningAssistant.Models.Learning;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls.Common
{
    public class RecommendationCard : UserControl
    {
        private LearningRecommendation? _recommendation;
        private bool _isHovered;
        private int _borderRadius = 12;

        [Category("外观")]
        [Description("推荐数据")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public LearningRecommendation? Recommendation
        {
            get => _recommendation;
            set
            {
                _recommendation = value;
                Invalidate();
            }
        }

        [Category("外观")]
        [Description("圆角半径")]
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

        public RecommendationCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint
                   | ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Size = new Size(280, 110);
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            var rect = ClientRectangle;
            var contentRect = new Rectangle(
                rect.X + (_isHovered ? 1 : 2),
                rect.Y + (_isHovered ? 1 : 2),
                rect.Width - (_isHovered ? 2 : 4),
                rect.Height - (_isHovered ? 2 : 4)
            );

            if (_isHovered)
            {
                using var shadowPath = GdiHelper.CreateRoundedRectPath(
                    new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4),
                    _borderRadius);
                using var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0));
                g.FillPath(shadowBrush, shadowPath);
            }

            using var cardPath = GdiHelper.CreateRoundedRectPath(contentRect, _borderRadius);
            using var bgBrush = new SolidBrush(Color.White);
            g.FillPath(bgBrush, cardPath);

            var typeColor = GetTypeColor(_recommendation?.Type);
            var iconRect = new Rectangle(contentRect.X + 15, contentRect.Y + 15, 48, 48);
            using var iconBgPath = GdiHelper.CreateRoundedRectPath(iconRect, 10);
            using var iconBgBrush = new LinearGradientBrush(
                iconRect,
                typeColor.light,
                typeColor.dark,
                LinearGradientMode.ForwardDiagonal);
            g.FillPath(iconBgBrush, iconBgPath);

            var icon = GetTypeIcon(_recommendation?.Type);
            using var iconFont = new Font("Segoe UI Emoji", 20F);
            var iconSize = g.MeasureString(icon, iconFont);
            g.DrawString(icon, iconFont, Brushes.White,
                iconRect.X + (iconRect.Width - iconSize.Width) / 2,
                iconRect.Y + (iconRect.Height - iconSize.Height) / 2);

            var titleX = iconRect.Right + 12;
            var titleWidth = contentRect.Right - titleX - 10;
            var titleRect = new RectangleF(titleX, contentRect.Y + 15, titleWidth, 28);
            using var titleFont = new Font("微软雅黑", 11F, FontStyle.Bold);
            var title = _recommendation?.Title ?? "推荐学习";
            TextRenderer.DrawText(g, title, titleFont,
                Rectangle.Round(titleRect), Color.FromArgb(30, 30, 30),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            var reasonY = titleRect.Bottom + 2;
            var reasonRect = new RectangleF(titleX, reasonY, titleWidth, 36);
            using var reasonFont = new Font("微软雅黑", 9F);
            var reason = _recommendation?.Reason ?? "";
            TextRenderer.DrawText(g, reason, reasonFont,
                Rectangle.Round(reasonRect), Color.FromArgb(120, 120, 120),
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);

            var bottomY = contentRect.Bottom - 30;
            var badgeRect = new Rectangle(contentRect.X + 15, bottomY, 60, 22);
            DrawTypeBadge(g, badgeRect, _recommendation?.Type, typeColor);

            if (_recommendation?.EstimatedMinutes > 0)
            {
                var timeText = $"⏱ {_recommendation.EstimatedMinutes}分钟";
                using var timeFont = new Font("微软雅黑", 9F);
                var timeSize = g.MeasureString(timeText, timeFont);
                g.DrawString(timeText, timeFont,
                    new SolidBrush(Color.FromArgb(150, 150, 150)),
                    contentRect.Right - timeSize.Width - 12,
                    bottomY + (22 - timeSize.Height) / 2);
            }

            var btnRect = new Rectangle(contentRect.Right - 80, bottomY - 2, 70, 28);
            DrawStartButton(g, btnRect);

            using var borderPen = new Pen(Color.FromArgb(40, 0, 0, 0), 1);
            g.DrawPath(borderPen, cardPath);
        }

        private void DrawTypeBadge(Graphics g, Rectangle rect, string? type, (Color light, Color dark) colors)
        {
            using var path = GdiHelper.CreateRoundedRectPath(rect, 6);
            using var brush = new SolidBrush(Color.FromArgb(20, colors.dark));
            g.FillPath(brush, path);

            var label = GetTypeLabel(type);
            using var font = new Font("微软雅黑", 8F, FontStyle.Bold);
            TextRenderer.DrawText(g, label, font, rect, colors.dark,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void DrawStartButton(Graphics g, Rectangle rect)
        {
            var btnColor = Color.FromArgb(255, 152, 0);
            using var path = GdiHelper.CreateRoundedRectPath(rect, 6);
            using var brush = new LinearGradientBrush(
                rect,
                Color.FromArgb(255, 183, 77),
                btnColor,
                LinearGradientMode.Vertical);
            g.FillPath(brush, path);

            TextRenderer.DrawText(g, "开始",
                new Font("微软雅黑", 9F, FontStyle.Bold),
                rect, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private static string GetTypeIcon(string? type) => type switch
        {
            "review" => "🔄",
            "weakpoint" => "🎯",
            "path" => "📈",
            "explore" => "✨",
            "goal" => "🏆",
            "learn" => "📚",
            _ => "💡"
        };

        private static string GetTypeLabel(string? type) => type switch
        {
            "review" => "复习",
            "weakpoint" => "强化",
            "path" => "路径",
            "explore" => "探索",
            "goal" => "目标",
            "learn" => "学习",
            _ => "推荐"
        };

        private static (Color light, Color dark) GetTypeColor(string? type) => type switch
        {
            "review" => (Color.FromArgb(129, 212, 250), Color.FromArgb(3, 169, 244)),
            "weakpoint" => (Color.FromArgb(255, 171, 145), Color.FromArgb(255, 87, 34)),
            "path" => (Color.FromArgb(165, 214, 167), Color.FromArgb(76, 175, 80)),
            "explore" => (Color.FromArgb(186, 104, 200), Color.FromArgb(156, 39, 176)),
            "goal" => (Color.FromArgb(255, 213, 79), Color.FromArgb(255, 152, 0)),
            "learn" => (Color.FromArgb(144, 164, 174), Color.FromArgb(96, 125, 139)),
            _ => (Color.FromArgb(187, 222, 251), Color.FromArgb(66, 165, 245))
        };

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

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                StartClicked?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
