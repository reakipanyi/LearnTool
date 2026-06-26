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
        private float _unlockScale = 1.0f;
        private readonly System.Windows.Forms.Timer _unlockAnimationTimer = new System.Windows.Forms.Timer();

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
                bool wasUnlocked = _isUnlocked;
                _isUnlocked = value;
                if (!wasUnlocked && value)
                {
                    StartUnlockAnimation();
                }
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

            _unlockAnimationTimer.Interval = 16;
            _unlockAnimationTimer.Tick += OnUnlockAnimationTick;
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
            _labelIcon.Click += OnCardClicked;
            _labelIcon.Cursor = Cursors.Hand;

            _labelName.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _labelName.TextAlign = ContentAlignment.MiddleCenter;
            _labelName.BackColor = Color.Transparent;
            _labelName.Click += OnCardClicked;
            _labelName.Cursor = Cursors.Hand;

            _labelCategory.Font = new Font("微软雅黑", 7.5F);
            _labelCategory.TextAlign = ContentAlignment.MiddleCenter;
            _labelCategory.BackColor = Color.Transparent;
            _labelCategory.Click += OnCardClicked;
            _labelCategory.Cursor = Cursors.Hand;

            _labelDescription.Font = new Font("微软雅黑", 8F);
            _labelDescription.TextAlign = ContentAlignment.MiddleCenter;
            _labelDescription.BackColor = Color.Transparent;
            _labelDescription.Click += OnCardClicked;
            _labelDescription.Cursor = Cursors.Hand;

            _labelProgressText.Font = new Font("微软雅黑", 8F, FontStyle.Bold);
            _labelProgressText.TextAlign = ContentAlignment.MiddleCenter;
            _labelProgressText.BackColor = Color.Transparent;
            _labelProgressText.Click += OnCardClicked;
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

            Click += OnCardClicked;

            ResumeLayout(false);
        }

        private void OnCardClicked(object? sender, EventArgs e)
        {
            CardClicked?.Invoke(this, e);
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

            bool isHiddenLocked = _badge.IsHidden && !_isUnlocked;

            _labelIcon.Text = _isUnlocked ? _badge.Icon : (isHiddenLocked ? "❓" : "🔒");
            _labelName.Text = isHiddenLocked ? "???" : _badge.Name;
            _labelDescription.Text = isHiddenLocked ? "这是一个神秘成就，解锁后揭晓" : _badge.Description;
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

            if (_unlockScale != 1.0f)
            {
                float offsetX = (Width - Width * _unlockScale) / 2;
                float offsetY = (Height - Height * _unlockScale) / 2;
                g.TranslateTransform(offsetX, offsetY);
                g.ScaleTransform(_unlockScale, _unlockScale);
            }

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

            bool isHiddenLocked = _badge != null && _badge.IsHidden && !_isUnlocked;

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
            else if (isHiddenLocked)
            {
                using var bgBrush = new LinearGradientBrush(
                    cardRect,
                    Color.FromArgb(45, 45, 55),
                    Color.FromArgb(30, 30, 40),
                    LinearGradientMode.Vertical);
                g.FillPath(bgBrush, cardPath);

                using var borderPen = new Pen(Color.FromArgb(80, 80, 95), 1.5f);
                g.DrawPath(borderPen, cardPath);

                DrawMysteryPattern(g, cardRect);
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

            if (_badge != null && !isHiddenLocked)
            {
                DrawStarRating(g, cardRect, _badge.Rarity);
            }

            int progressBarHeight = 6;
            int progressBarY = Height - 50 - shadowOffset / 2;
            int progressBarX = 20 + shadowOffset / 2;
            int progressBarWidth = Width - 40 - shadowOffset;


            if (!isHiddenLocked)
            {
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
            }

            DrawStatusBadge(g, cardRect);

            UpdateTextColors();

            g.ResetTransform();
        }

        private void DrawStatusBadge(Graphics g, Rectangle cardRect)
        {
            string badgeText;
            Color badgeBackColor;
            Color badgeTextColor;

            bool isHiddenLocked = _badge != null && _badge.IsHidden && !_isUnlocked;

            if (_isUnlocked)
            {
                badgeText = "✓ 已解锁";
                badgeBackColor = Color.FromArgb(120, 255, 255, 255);
                badgeTextColor = Color.White;
            }
            else if (isHiddenLocked)
            {
                badgeText = "?";
                badgeBackColor = Color.FromArgb(60, 60, 75);
                badgeTextColor = Color.FromArgb(160, 160, 180);
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
            bool isHiddenLocked = _badge != null && _badge.IsHidden && !_isUnlocked;

            if (_isUnlocked)
            {
                _labelIcon.ForeColor = Color.White;
                _labelName.ForeColor = Color.White;
                _labelCategory.ForeColor = Color.FromArgb(220, 255, 255, 255);
                _labelDescription.ForeColor = Color.FromArgb(200, 255, 255, 255);
                _labelProgressText.ForeColor = Color.White;
            }
            else if (isHiddenLocked)
            {
                _labelIcon.ForeColor = Color.FromArgb(120, 120, 140);
                _labelName.ForeColor = Color.FromArgb(160, 160, 180);
                _labelCategory.ForeColor = Color.FromArgb(100, 100, 120);
                _labelDescription.ForeColor = Color.FromArgb(120, 120, 140);
                _labelProgressText.ForeColor = Color.FromArgb(100, 100, 120);
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
                if (isHiddenLocked)
                {
                    _labelProgressText.Text = "???";
                }
                else
                {
                    int target = _badge.Requirement.TargetValue;
                    int current = Math.Min(_currentValue, target);
                    _labelProgressText.Text = _isUnlocked
                        ? "✓ 已解锁"
                        : $"{current} / {target}";
                }
            }
        }

        private void DrawMysteryPattern(Graphics g, Rectangle cardRect)
        {
            using var brush = new SolidBrush(Color.FromArgb(15, 255, 255, 255));
            var rand = new Random(cardRect.GetHashCode());

            for (int i = 0; i < 8; i++)
            {
                int x = cardRect.X + rand.Next(cardRect.Width);
                int y = cardRect.Y + rand.Next(cardRect.Height);
                int size = rand.Next(4, 10);
                g.FillEllipse(brush, x, y, size, size);
            }

            using var qFont = new Font("Segoe UI Emoji", 14F);
            using var qBrush = new SolidBrush(Color.FromArgb(20, 255, 255, 255));
            for (int i = 0; i < 3; i++)
            {
                int x = cardRect.X + 20 + rand.Next(Math.Max(1, cardRect.Width - 40));
                int y = cardRect.Y + 30 + rand.Next(Math.Max(1, cardRect.Height - 60));
                g.DrawString("?", qFont, qBrush, x, y);
            }
        }

        private void DrawStarRating(Graphics g, Rectangle cardRect, BadgeRarity rarity)
        {
            int starCount = (int)rarity + 1;
            int maxStars = 5;
            int starSize = 12;
            int spacing = 2;
            int totalWidth = maxStars * starSize + (maxStars - 1) * spacing;
            int startX = cardRect.X + (cardRect.Width - totalWidth) / 2;
            int y = cardRect.Y + 8;

            Color activeColor = _isUnlocked
                ? Color.FromArgb(255, 215, 0)
                : Color.FromArgb(200, 200, 200);
            Color inactiveColor = Color.FromArgb(220, 220, 220);

            for (int i = 0; i < maxStars; i++)
            {
                bool isActive = i < starCount;
                Color starColor = isActive ? activeColor : inactiveColor;
                int x = startX + i * (starSize + spacing);

                DrawStar(g, x, y, starSize, starColor);
            }
        }

        private static void DrawStar(Graphics g, int x, int y, int size, Color color)
        {
            float cx = x + size / 2f;
            float cy = y + size / 2f;
            float outerR = size / 2f;
            float innerR = outerR * 0.45f;

            using var path = new GraphicsPath();
            for (int i = 0; i < 5; i++)
            {
                float outerAngle = (i * 72 - 90) * (float)Math.PI / 180;
                float innerAngle = ((i * 72) + 36 - 90) * (float)Math.PI / 180;

                float outerX = cx + outerR * (float)Math.Cos(outerAngle);
                float outerY = cy + outerR * (float)Math.Sin(outerAngle);
                float innerX = cx + innerR * (float)Math.Cos(innerAngle);
                float innerY = cy + innerR * (float)Math.Sin(innerAngle);

                if (i == 0)
                    path.AddLine(outerX, outerY, innerX, innerY);
                else
                    path.AddLine(innerX, innerY, outerX, outerY);
            }
            path.CloseFigure();

            using var brush = new SolidBrush(color);
            g.FillPath(brush, path);
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

        private void StartUnlockAnimation()
        {
            _unlockScale = 1.15f;
            _unlockAnimationTimer.Start();
        }

        private void OnUnlockAnimationTick(object? sender, EventArgs e)
        {
            _unlockScale -= 0.015f;
            if (_unlockScale <= 1.0f)
            {
                _unlockScale = 1.0f;
                _unlockAnimationTimer.Stop();
            }
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _unlockAnimationTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
