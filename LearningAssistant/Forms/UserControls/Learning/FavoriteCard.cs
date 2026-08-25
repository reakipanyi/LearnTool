using LearningAssistant.Common.UI;
using LearningAssistant.Models.Favorites;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls.Learning
{
    public class FavoriteCard : UserControl
    {
        private FavoriteItem? _favorite;
        private bool _isHovered;
        private bool _isSelected;
        private int _borderRadius = 10;

        [Category("数据")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FavoriteItem? Favorite
        {
            get => _favorite;
            set
            {
                _favorite = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("外观")]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("外观")]
        public int BorderRadius
        {
            get => _borderRadius;
            set
            {
                _borderRadius = Math.Max(0, value);
                Invalidate();
            }
        }

        public event EventHandler? CardClicked;
        public event EventHandler? DeleteClicked;
        public event EventHandler? ReviewClicked;

        public FavoriteCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint
                   | ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Size = new Size(260, 140);
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            var rect = ClientRectangle;
            var padding = 3;
            var contentRect = new Rectangle(
                rect.X + padding + (_isHovered ? 1 : 2),
                rect.Y + padding + (_isHovered ? 1 : 2),
                rect.Width - padding * 2 - (_isHovered ? 2 : 4),
                rect.Height - padding * 2 - (_isHovered ? 2 : 4)
            );

            if (_isHovered || _isSelected)
            {
                using var shadowPath = GdiHelper.CreateRoundedRectPath(
                    new Rectangle(contentRect.X + 1, contentRect.Y + 2, contentRect.Width - 2, contentRect.Height - 2),
                    _borderRadius);
                using var shadowBrush = new SolidBrush(Color.FromArgb(_isSelected ? 50 : 25, 0, 0, 0));
                g.FillPath(shadowBrush, shadowPath);
            }

            using var cardPath = GdiHelper.CreateRoundedRectPath(contentRect, _borderRadius);
            using var bgBrush = new SolidBrush(_isSelected ? Color.FromArgb(255, 248, 235) : Color.White);
            g.FillPath(bgBrush, cardPath);

            if (_isSelected)
            {
                using var selectedPen = new Pen(Color.FromArgb(255, 152, 0), 2);
                g.DrawPath(selectedPen, cardPath);
            }
            else
            {
                using var borderPen = new Pen(Color.FromArgb(30, 0, 0, 0), 1);
                g.DrawPath(borderPen, cardPath);
            }

            if (_favorite == null) return;

            var iconRect = new Rectangle(contentRect.X + 12, contentRect.Y + 12, 36, 36);
            var iconBgColor = GetTypeColor(_favorite.Type);
            using var iconPath = GdiHelper.CreateRoundedRectPath(iconRect, 8);
            using var iconBrush = new LinearGradientBrush(
                iconRect,
                iconBgColor.light,
                iconBgColor.dark,
                LinearGradientMode.ForwardDiagonal);
            g.FillPath(iconBrush, iconPath);

            var icon = GetTypeIcon(_favorite.Type);
            using var iconFont = new Font("Segoe UI Emoji", 16F);
            var iconSize = g.MeasureString(icon, iconFont);
            g.DrawString(icon, iconFont, Brushes.White,
                iconRect.X + (iconRect.Width - iconSize.Width) / 2,
                iconRect.Y + (iconRect.Height - iconSize.Height) / 2);

            var titleX = iconRect.Right + 10;
            var titleWidth = contentRect.Right - titleX - 10;
            var titleRect = new RectangleF(titleX, contentRect.Y + 14, titleWidth, 24);
            using var titleFont = new Font("微软雅黑", 10F, FontStyle.Bold);
            var title = _favorite.Title;
            TextRenderer.DrawText(g, title, titleFont,
                Rectangle.Round(titleRect),
                Color.FromArgb(33, 33, 33),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            var typeText = _favorite.TypeDisplayName;
            using var typeFont = new Font("微软雅黑", 8F);
            var typeSize = g.MeasureString(typeText, typeFont);
            var typeRect = new RectangleF(titleX, titleRect.Bottom + 2, titleWidth, 16);
            g.DrawString(typeText, typeFont,
                new SolidBrush(Color.FromArgb(150, 150, 150)),
                typeRect);

            var descY = iconRect.Bottom + 8;
            var descRect = new RectangleF(
                contentRect.X + 12,
                descY,
                contentRect.Width - 24,
                32
            );
            var desc = !string.IsNullOrEmpty(_favorite.Description)
                ? _favorite.Description
                : _favorite.Content ?? "";
            if (!string.IsNullOrEmpty(desc))
            {
                using var descFont = new Font("微软雅黑", 9F);
                TextRenderer.DrawText(g, desc, descFont,
                    Rectangle.Round(descRect),
                    Color.FromArgb(100, 100, 100),
                    TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
            }

            var bottomY = contentRect.Bottom - 24;

            if (_favorite.IsMarkedForReview)
            {
                var reviewBadgeRect = new Rectangle(contentRect.X + 12, bottomY, 55, 18);
                using var reviewPath = GdiHelper.CreateRoundedRectPath(reviewBadgeRect, 4);
                using var reviewBrush = new SolidBrush(Color.FromArgb(255, 243, 224));
                g.FillPath(reviewBrush, reviewPath);
                TextRenderer.DrawText(g, "🔄 待复习",
                    new Font("微软雅黑", 7.5F, FontStyle.Bold),
                    reviewBadgeRect,
                    Color.FromArgb(230, 126, 34),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            if (_favorite.IsPinned)
            {
                var pinX = _favorite.IsMarkedForReview ? 72 : 12;
                var pinRect = new Rectangle(contentRect.X + pinX, bottomY, 40, 18);
                using var pinPath = GdiHelper.CreateRoundedRectPath(pinRect, 4);
                using var pinBrush = new SolidBrush(Color.FromArgb(227, 242, 253));
                g.FillPath(pinBrush, pinPath);
                TextRenderer.DrawText(g, "📌 置顶",
                    new Font("微软雅黑", 7.5F, FontStyle.Bold),
                    pinRect,
                    Color.FromArgb(33, 150, 243),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            if (_favorite.Tags.Count > 0)
            {
                var tagText = string.Join(" · ", _favorite.Tags.Take(3));
                if (_favorite.Tags.Count > 3) tagText += " ...";
                using var tagFont = new Font("微软雅黑", 8F);
                var tagSize = g.MeasureString(tagText, tagFont);
                g.DrawString(tagText, tagFont,
                    new SolidBrush(Color.FromArgb(120, 120, 120)),
                    contentRect.Right - tagSize.Width - 12,
                    bottomY + 1);
            }

            var dateText = _favorite.CreatedAt.ToString("MM-dd");
            using var dateFont = new Font("微软雅黑", 8F);
            var dateSize = g.MeasureString(dateText, dateFont);
            g.DrawString(dateText, dateFont,
                new SolidBrush(Color.FromArgb(160, 160, 160)),
                contentRect.Right - dateSize.Width - 12,
                contentRect.Y + 16);
        }

        private static string GetTypeIcon(FavoriteItemType type) => type switch
        {
            FavoriteItemType.Pdf => "📄",
            FavoriteItemType.PdfPage => "📑",
            FavoriteItemType.Text => "📝",
            FavoriteItemType.Url => "🔗",
            FavoriteItemType.Image => "🖼️",
            FavoriteItemType.Note => "📒",
            _ => "📌"
        };

        private static (Color light, Color dark) GetTypeColor(FavoriteItemType type) => type switch
        {
            FavoriteItemType.Pdf => (Color.FromArgb(239, 83, 80), Color.FromArgb(211, 47, 47)),
            FavoriteItemType.PdfPage => (Color.FromArgb(255, 138, 128), Color.FromArgb(244, 67, 54)),
            FavoriteItemType.Text => (Color.FromArgb(66, 165, 245), Color.FromArgb(25, 118, 210)),
            FavoriteItemType.Url => (Color.FromArgb(102, 187, 106), Color.FromArgb(56, 142, 60)),
            FavoriteItemType.Image => (Color.FromArgb(171, 71, 188), Color.FromArgb(123, 31, 162)),
            FavoriteItemType.Note => (Color.FromArgb(255, 167, 38), Color.FromArgb(245, 124, 0)),
            _ => (Color.FromArgb(158, 158, 158), Color.FromArgb(97, 97, 97))
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
                CardClicked?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
