using LearningAssistant.Common.UI;
using LearningAssistant.Models.User;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls
{
    public class AchievementCard : UserControl
    {
        private Label _labelIcon = null!;
        private Label _labelName = null!;
        private Label _labelDescription = null!;
        private Label _labelProgressText = null!;
        private Label _labelCategory = null!;

        private Badge? _badge;
        private int _currentValue;
        private bool _isUnlocked;
        private bool _isHovered;
        private int _cornerRadius = 12;
        private int _shadowOffset = 4;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Badge? Badge
        {
            get => _badge;
            set
            {
                _badge = value;
                UpdateDisplay();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CurrentValue
        {
            get => _currentValue;
            set
            {
                _currentValue = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsUnlocked
        {
            get => _isUnlocked;
            set
            {
                _isUnlocked = value;
                Invalidate();
            }
        }

        public event EventHandler? CardClicked;

        public AchievementCard()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.ResizeRedraw,
                true);
            InitializeComponent();
            SetupHoverEffect();
        }

        private void InitializeComponent()
        {
            _labelIcon = new Label();
            _labelName = new Label();
            _labelDescription = new Label();
            _labelProgressText = new Label();
            _labelCategory = new Label();

            SuspendLayout();

            _labelIcon.Font = new Font("Segoe UI Emoji", 32F);
            _labelIcon.TextAlign = ContentAlignment.MiddleCenter;
            _labelIcon.BackColor = Color.Transparent;
            _labelIcon.Click += (s, e) => CardClicked?.Invoke(this, e);
            _labelIcon.Cursor = Cursors.Hand;

            _labelName.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _labelName.TextAlign = ContentAlignment.MiddleCenter;
            _labelName.BackColor = Color.Transparent;
            _labelName.Click += (s, e) => CardClicked?.Invoke(this, e);
            _labelName.Cursor = Cursors.Hand;

            _labelCategory.Font = new Font("微软雅黑", 7.5F);
            _labelCategory.TextAlign = ContentAlignment.MiddleCenter;
            _labelCategory.BackColor = Color.Transparent;
            _labelCategory.Click += (s, e) => CardClicked?.Invoke(this, e);
            _labelCategory.Cursor = Cursors.Hand;

            _labelDescription.Font = new Font("微软雅黑", 8F);
            _labelDescription.TextAlign = ContentAlignment.MiddleCenter;
            _labelDescription.BackColor = Color.Transparent;
            _labelDescription.Click += (s, e) => CardClicked?.Invoke(this, e);
            _labelDescription.Cursor = Cursors.Hand;

            _labelProgressText.Font = new Font("微软雅黑", 8F, FontStyle.Bold);
            _labelProgressText.TextAlign = ContentAlignment.MiddleCenter;
            _labelProgressText.BackColor = Color.Transparent;
            _labelProgressText.Click += (s, e) => CardClicked?.Invoke(this, e);
            _labelProgressText.Cursor = Cursors.Hand;

            Controls.Add(_labelProgressText);
            Controls.Add(_labelDescription);
            Controls.Add(_labelCategory);
            Controls.Add(_labelName);
            Controls.Add(_labelIcon);

            Size = new Size(170, 195);
            BackColor = Color.Transparent;
            DoubleBuffered = true;
            Cursor = Cursors.Hand;

            Click += (s, e) => CardClicked?.Invoke(this, e);

            ResumeLayout(false);
        }

        private void SetupHoverEffect()
        {
            MouseEnter += (s, e) =>
            {
                _isHovered = true;
                Invalidate();
            };

            MouseLeave += (s, e) =>
            {
                _isHovered = false;
                Invalidate();
            };
        }

        private void UpdateDisplay()
        {
            if (_badge == null)
            {
                _labelIcon.Text = "🏅";
                _labelName.Text = "未知成就";
                _labelDescription.Text = "";
                _labelCategory.Text = "";
                _labelProgressText.Text = "";
                return;
            }

            _labelIcon.Text = _isUnlocked ? _badge.Icon : "🔒";
            _labelName.Text = _badge.Name;
            _labelDescription.Text = _badge.Description;
            _labelCategory.Text = GetCategoryText(_badge.Category);

            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutControls();
        }

        private void LayoutControls()
        {
            int padding = 12;
            int contentWidth = Width - padding * 2 - _shadowOffset;

            _labelIcon.SetBounds(padding + _shadowOffset / 2, padding + 10 + _shadowOffset / 2,
                contentWidth, 55);

            _labelName.SetBounds(padding + _shadowOffset / 2, _labelIcon.Bottom + 2,
                contentWidth, 22);

            _labelCategory.SetBounds(padding + _shadowOffset / 2, _labelName.Bottom,
                contentWidth, 16);

            _labelDescription.SetBounds(padding + _shadowOffset / 2, _labelCategory.Bottom + 2,
                contentWidth, 28);

            _labelProgressText.SetBounds(padding + _shadowOffset / 2,
                Height - padding - 20 - _shadowOffset / 2,
                contentWidth, 16);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            int shadowOffset = _isHovered ? 6 : 4;
            Rectangle cardRect = new(shadowOffset / 2, shadowOffset / 2,
                Width - shadowOffset - 1, Height - shadowOffset - 1);

            using (var shadowPath = GdiHelper.CreateRoundedRectPath(
                new Rectangle(shadowOffset, shadowOffset, Width - shadowOffset - 1, Height - shadowOffset - 1),
                _cornerRadius))
            using (var shadowBrush = new SolidBrush(Color.FromArgb(25, 0, 0, 0)))
            {
                g.FillPath(shadowBrush, shadowPath);
            }

            using var cardPath = GdiHelper.CreateRoundedRectPath(cardRect, _cornerRadius);

            if (_isUnlocked && _badge != null)
            {
                var rarityColors = GetRarityGradientColors(_badge.Rarity);

                if (_badge.Rarity >= BadgeRarity.Rare)
                {
                    int glowLayers = _badge.Rarity >= BadgeRarity.Legendary ? 4 : 3;
                    for (int i = glowLayers; i > 0; i--)
                    {
                        int offset = i;
                        int alpha = 25 * i / glowLayers;
                        using var glowPath = GdiHelper.CreateRoundedRectPath(
                            new Rectangle(cardRect.X - offset, cardRect.Y - offset,
                                cardRect.Width + offset * 2, cardRect.Height + offset * 2),
                            _cornerRadius + offset);
                        using var glowBrush = new SolidBrush(Color.FromArgb(alpha, rarityColors.GlowColor));
                        g.FillPath(glowBrush, glowPath);
                    }
                }

                using var gradientBrush = new LinearGradientBrush(
                    cardRect,
                    rarityColors.StartColor,
                    rarityColors.EndColor,
                    LinearGradientMode.Vertical);
                g.FillPath(gradientBrush, cardPath);

                using var borderPen = new Pen(
                    _badge.Rarity >= BadgeRarity.Rare ? rarityColors.GlowColor : Color.FromArgb(80, 255, 255, 255),
                    _badge.Rarity >= BadgeRarity.Legendary ? 2f : 1f);
                g.DrawPath(borderPen, cardPath);
            }
            else
            {
                using var bgBrush = new LinearGradientBrush(
                    cardRect,
                    Color.FromArgb(248, 248, 250),
                    Color.FromArgb(240, 240, 245),
                    LinearGradientMode.Vertical);
                g.FillPath(bgBrush, cardPath);

                using var borderPen = new Pen(Color.FromArgb(220, 220, 230));
                g.DrawPath(borderPen, cardPath);
            }

            int progressBarHeight = 6;
            int progressBarY = Height - 50 - shadowOffset / 2;
            int progressBarX = 20 + shadowOffset / 2;
            int progressBarWidth = Width - 40 - shadowOffset;

            double progressPercent = 0;
            if (_badge != null && _badge.Requirement.TargetValue > 0)
            {
                progressPercent = (double)Math.Min(_currentValue, _badge.Requirement.TargetValue) /
                    _badge.Requirement.TargetValue;
            }

            Color progressStart, progressEnd;
            Color progressBg;

            if (_isUnlocked && _badge != null)
            {
                progressStart = Color.FromArgb(200, 255, 255, 255);
                progressEnd = Color.White;
                progressBg = Color.FromArgb(40, 255, 255, 255);
            }
            else
            {
                progressStart = Color.FromArgb(33, 150, 243);
                progressEnd = Color.FromArgb(100, 181, 246);
                progressBg = Color.FromArgb(230, 230, 235);
            }

            g.DrawGradientProgressBar(
                new Rectangle(progressBarX, progressBarY, progressBarWidth, progressBarHeight),
                progressBarHeight / 2,
                progressPercent,
                progressStart,
                progressEnd,
                progressBg);

            if (!_isUnlocked && progressPercent > 0)
            {
                int percentVal = (int)(progressPercent * 100);
                string percentText = $"{percentVal}%";
                var percentFont = new Font("微软雅黑", 7.5F, FontStyle.Bold);
                var percentColor = Color.FromArgb(102, 102, 102);

                using var sf = new StringFormat
                {
                    Alignment = StringAlignment.Far,
                    LineAlignment = StringAlignment.Near
                };
                using var brush = new SolidBrush(percentColor);
                g.DrawString(percentText, percentFont, brush,
                    new Rectangle(progressBarX, progressBarY - 16, progressBarWidth, 14), sf);
            }

            DrawStatusBadge(g, cardRect);

            UpdateTextColors();
        }

        private void DrawStatusBadge(Graphics g, Rectangle cardRect)
        {
            string badgeText;
            Color badgeBackColor;
            Color badgeTextColor;

            if (_isUnlocked)
            {
                badgeText = "✓ 已解锁";
                badgeBackColor = Color.FromArgb(120, 255, 255, 255);
                badgeTextColor = Color.White;
            }
            else
            {
                badgeText = "🔒 未解锁";
                badgeBackColor = Color.FromArgb(235, 235, 240);
                badgeTextColor = Color.FromArgb(120, 120, 130);
            }

            var badgeFont = new Font("微软雅黑", 7.5F, FontStyle.Bold);
            var textSize = g.MeasureString(badgeText, badgeFont);
            int badgeWidth = (int)textSize.Width + 14;
            int badgeHeight = 20;
            int badgeX = cardRect.Right - badgeWidth - 12;
            int badgeY = cardRect.Y + 10;

            var badgeRect = new Rectangle(badgeX, badgeY, badgeWidth, badgeHeight);

            using var badgePath = GdiHelper.CreateRoundedRectPath(badgeRect, badgeHeight / 2);
            using var badgeBrush = new SolidBrush(badgeBackColor);
            g.FillPath(badgeBrush, badgePath);

            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            using var textBrush = new SolidBrush(badgeTextColor);
            g.DrawString(badgeText, badgeFont, textBrush, badgeRect, sf);
        }

        private void UpdateTextColors()
        {
            if (_isUnlocked)
            {
                _labelIcon.ForeColor = Color.White;
                _labelName.ForeColor = Color.White;
                _labelCategory.ForeColor = Color.FromArgb(220, 255, 255, 255);
                _labelDescription.ForeColor = Color.FromArgb(200, 255, 255, 255);
                _labelProgressText.ForeColor = Color.White;
            }
            else
            {
                _labelIcon.ForeColor = Color.FromArgb(180, 180, 190);
                _labelName.ForeColor = Color.FromArgb(120, 120, 130);
                _labelCategory.ForeColor = Color.FromArgb(160, 160, 170);
                _labelDescription.ForeColor = Color.FromArgb(140, 140, 150);
                _labelProgressText.ForeColor = Color.FromArgb(120, 120, 130);
            }

            if (_badge != null)
            {
                int target = _badge.Requirement.TargetValue;
                int current = Math.Min(_currentValue, target);
                _labelProgressText.Text = _isUnlocked
                    ? "✓ 已解锁"
                    : $"{current} / {target}";
            }
        }

        private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;

            if (diameter > rect.Width) diameter = rect.Width;
            if (diameter > rect.Height) diameter = rect.Height;

            Rectangle arcRect = new(rect.Location, new Size(diameter, diameter));

            path.AddArc(arcRect, 180, 90);

            arcRect.X = rect.Right - diameter;
            path.AddArc(arcRect, 270, 90);

            arcRect.Y = rect.Bottom - diameter;
            path.AddArc(arcRect, 0, 90);

            arcRect.X = rect.Left;
            path.AddArc(arcRect, 90, 90);

            path.CloseFigure();
            return path;
        }

        private string GetCategoryText(BadgeCategory category)
        {
            return category switch
            {
                BadgeCategory.Learning => "📚 学习",
                BadgeCategory.Consistency => "🔥 坚持",
                BadgeCategory.Mastery => "🏆 精通",
                BadgeCategory.Special => "⭐ 特殊",
                _ => ""
            };
        }

        private struct GradientColors
        {
            public Color StartColor;
            public Color EndColor;
            public Color GlowColor;
        }

        private static GradientColors GetRarityGradientColors(BadgeRarity rarity)
        {
            return rarity switch
            {
                BadgeRarity.Common => new GradientColors
                {
                    StartColor = Color.FromArgb(158, 158, 158),
                    EndColor = Color.FromArgb(97, 97, 97),
                    GlowColor = Color.FromArgb(189, 189, 189)
                },
                BadgeRarity.Uncommon => new GradientColors
                {
                    StartColor = Color.FromArgb(76, 175, 80),
                    EndColor = Color.FromArgb(46, 125, 50),
                    GlowColor = Color.FromArgb(102, 187, 106)
                },
                BadgeRarity.Rare => new GradientColors
                {
                    StartColor = Color.FromArgb(33, 150, 243),
                    EndColor = Color.FromArgb(21, 101, 192),
                    GlowColor = Color.FromArgb(66, 165, 245)
                },
                BadgeRarity.Epic => new GradientColors
                {
                    StartColor = Color.FromArgb(156, 39, 176),
                    EndColor = Color.FromArgb(106, 27, 154),
                    GlowColor = Color.FromArgb(171, 71, 188)
                },
                BadgeRarity.Legendary => new GradientColors
                {
                    StartColor = Color.FromArgb(255, 193, 7),
                    EndColor = Color.FromArgb(255, 111, 0),
                    GlowColor = Color.FromArgb(255, 213, 79)
                },
                _ => new GradientColors
                {
                    StartColor = Color.FromArgb(158, 158, 158),
                    EndColor = Color.FromArgb(97, 97, 97),
                    GlowColor = Color.Gray
                }
            };
        }
    }
}
