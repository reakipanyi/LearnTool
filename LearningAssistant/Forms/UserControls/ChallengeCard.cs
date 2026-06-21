using LearningAssistant.Common.UI;
using LearningAssistant.Models.User;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls
{
    public class ChallengeCard : UserControl
    {
        private Label _labelIcon = null!;
        private Label _labelName = null!;
        private Label _labelReward = null!;
        private Label _labelDescription = null!;
        private Label _labelProgress = null!;
        private Button _buttonClaim = null!;
        private Label _labelStatus = null!;

        private Challenge? _challenge;
        private bool _isHovered;
        private int _cornerRadius = 12;
        private int _shadowOffset = 4;
        private System.Windows.Forms.Timer? _pulseTimer;
        private int _pulsePhase = 0;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Challenge? Challenge
        {
            get => _challenge;
            set
            {
                _challenge = value;
                UpdateDisplay();
                UpdatePulseState();
            }
        }

        public event EventHandler<Challenge>? ClaimClicked;

        public ChallengeCard()
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
            _labelReward = new Label();
            _labelDescription = new Label();
            _labelProgress = new Label();
            _buttonClaim = new Button();
            _labelStatus = new Label();

            SuspendLayout();

            _labelIcon.Font = new Font("Segoe UI Emoji", 24F);
            _labelIcon.TextAlign = ContentAlignment.MiddleCenter;
            _labelIcon.BackColor = Color.Transparent;
            _labelIcon.Cursor = Cursors.Hand;
            _labelIcon.Click += (s, e) => OnCardClick();

            _labelName.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _labelName.TextAlign = ContentAlignment.MiddleLeft;
            _labelName.BackColor = Color.Transparent;
            _labelName.Cursor = Cursors.Hand;
            _labelName.Click += (s, e) => OnCardClick();

            _labelReward.Font = new Font("微软雅黑", 8.5F, FontStyle.Bold);
            _labelReward.TextAlign = ContentAlignment.MiddleRight;
            _labelReward.BackColor = Color.Transparent;
            _labelReward.ForeColor = Color.FromArgb(255, 152, 0);

            _labelDescription.Font = new Font("微软雅黑", 8F);
            _labelDescription.TextAlign = ContentAlignment.MiddleLeft;
            _labelDescription.BackColor = Color.Transparent;
            _labelDescription.ForeColor = Color.FromArgb(102, 102, 102);

            _labelProgress.Font = new Font("微软雅黑", 8.5F, FontStyle.Bold);
            _labelProgress.TextAlign = ContentAlignment.MiddleLeft;
            _labelProgress.BackColor = Color.Transparent;

            _buttonClaim.Text = "领取奖励";
            _buttonClaim.FlatStyle = FlatStyle.Flat;
            _buttonClaim.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _buttonClaim.ForeColor = Color.White;
            _buttonClaim.BackColor = Color.FromArgb(255, 152, 0);
            _buttonClaim.Cursor = Cursors.Hand;
            _buttonClaim.Visible = false;
            _buttonClaim.Click += ButtonClaim_Click;
            _buttonClaim.FlatAppearance.BorderSize = 0;

            _labelStatus.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _labelStatus.TextAlign = ContentAlignment.MiddleRight;
            _labelStatus.BackColor = Color.Transparent;
            _labelStatus.Visible = false;

            Controls.Add(_labelStatus);
            Controls.Add(_buttonClaim);
            Controls.Add(_labelProgress);
            Controls.Add(_labelDescription);
            Controls.Add(_labelReward);
            Controls.Add(_labelName);
            Controls.Add(_labelIcon);

            Size = new Size(260, 140);
            BackColor = Color.Transparent;
            DoubleBuffered = true;
            Cursor = Cursors.Hand;

            Click += (s, e) => OnCardClick();

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

        private void OnCardClick()
        {
            if (_challenge != null && _challenge.Completed && !_challenge.Claimed)
            {
                ClaimClicked?.Invoke(this, _challenge);
            }
        }

        private void ButtonClaim_Click(object? sender, EventArgs e)
        {
            if (_challenge != null && _challenge.Completed && !_challenge.Claimed)
            {
                ClaimClicked?.Invoke(this, _challenge);
            }
        }

        private void UpdateDisplay()
        {
            if (_challenge == null)
            {
                _labelIcon.Text = "🎯";
                _labelName.Text = "挑战";
                _labelDescription.Text = "";
                _labelReward.Text = "";
                _labelProgress.Text = "";
                return;
            }

            _labelIcon.Text = _challenge.Emoji;
            _labelName.Text = _challenge.Name;
            _labelDescription.Text = _challenge.Description;
            _labelReward.Text = $"🎁 +{_challenge.Reward} XP";

            int current = Math.Min(_challenge.Current, _challenge.Target);
            _labelProgress.Text = $"{current} / {_challenge.Target}";

            _buttonClaim.Visible = false;

            if (_challenge.Completed && _challenge.Claimed)
            {
                _labelStatus.Visible = true;
                _labelStatus.Text = "✓ 已领取";
            }
            else
            {
                _labelStatus.Visible = false;
            }

            Invalidate();
        }

        private void UpdatePulseState()
        {
            bool shouldPulse = _challenge?.Completed == true && !_challenge.Claimed;

            if (shouldPulse && _pulseTimer == null)
            {
                _pulseTimer = new System.Windows.Forms.Timer();
                _pulseTimer.Interval = 50;
                _pulseTimer.Tick += (s, e) =>
                {
                    _pulsePhase = (_pulsePhase + 1) % 40;
                    Invalidate();
                };
                _pulseTimer.Start();
            }
            else if (!shouldPulse && _pulseTimer != null)
            {
                _pulseTimer.Stop();
                _pulseTimer.Dispose();
                _pulseTimer = null;
                _pulsePhase = 0;
                Invalidate();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutControls();
        }

        private void LayoutControls()
        {
            int padding = 14;
            int shadowOffset = _isHovered ? 6 : 4;
            int contentLeft = padding + shadowOffset / 2;
            int contentWidth = Width - padding * 2 - shadowOffset;

            _labelIcon.SetBounds(contentLeft, padding + 8 + shadowOffset / 2,
                44, 44);

            _labelReward.SetBounds(Width - padding - 100 - shadowOffset / 2,
                padding + 10 + shadowOffset / 2,
                100, 20);

            _labelName.SetBounds(_labelIcon.Right + 8,
                padding + 10 + shadowOffset / 2,
                Width - _labelIcon.Right - 8 - padding - 100 - shadowOffset,
                24);

            _labelDescription.SetBounds(contentLeft, _labelIcon.Bottom + 6,
                contentWidth, 18);

            int buttonWidth = 80;
            int buttonHeight = 28;
            int buttonTop = Height - padding - buttonHeight - shadowOffset / 2;

            _buttonClaim.SetBounds(Width - padding - buttonWidth - shadowOffset / 2,
                buttonTop, buttonWidth, buttonHeight);

            _labelStatus.SetBounds(Width - padding - 100 - shadowOffset / 2,
                buttonTop, 100, buttonHeight);

            _labelProgress.SetBounds(contentLeft, buttonTop,
                120, buttonHeight);
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
            using (var shadowBrush = new SolidBrush(Color.FromArgb(20, 0, 0, 0)))
            {
                g.FillPath(shadowBrush, shadowPath);
            }

            using var cardPath = GdiHelper.CreateRoundedRectPath(cardRect, _cornerRadius);

            bool canClaim = _challenge?.Completed == true && !_challenge.Claimed;
            bool isClaimed = _challenge?.Claimed == true;

            if (isClaimed)
            {
                using var bgBrush = new LinearGradientBrush(
                    cardRect,
                    Color.FromArgb(245, 250, 245),
                    Color.FromArgb(235, 245, 235),
                    LinearGradientMode.Vertical);
                g.FillPath(bgBrush, cardPath);

                using var borderPen = new Pen(Color.FromArgb(200, 76, 175, 80), 1);
                g.DrawPath(borderPen, cardPath);
            }
            else if (canClaim)
            {
                int pulseAlpha = 40 + (int)(Math.Sin(_pulsePhase * Math.PI / 20) * 25 + 25);
                int glowLayers = 3;
                for (int i = glowLayers; i > 0; i--)
                {
                    int offset = i * 2;
                    int alpha = pulseAlpha * i / glowLayers;
                    using var glowPath = GdiHelper.CreateRoundedRectPath(
                        new Rectangle(cardRect.X - offset, cardRect.Y - offset,
                            cardRect.Width + offset * 2, cardRect.Height + offset * 2),
                        _cornerRadius + offset);
                    using var glowBrush = new SolidBrush(Color.FromArgb(alpha, 255, 152, 0));
                    g.FillPath(glowBrush, glowPath);
                }

                using var bgBrush = new LinearGradientBrush(
                    cardRect,
                    Color.FromArgb(255, 255, 248),
                    Color.FromArgb(255, 248, 220),
                    LinearGradientMode.Vertical);
                g.FillPath(bgBrush, cardPath);

                using var borderPen = new Pen(Color.FromArgb(255, 152, 0), 2);
                g.DrawPath(borderPen, cardPath);
            }
            else
            {
                using var bgBrush = new LinearGradientBrush(
                    cardRect,
                    Color.White,
                    Color.FromArgb(250, 250, 252),
                    LinearGradientMode.Vertical);
                g.FillPath(bgBrush, cardPath);

                using var borderPen = new Pen(Color.FromArgb(225, 225, 235));
                g.DrawPath(borderPen, cardPath);
            }

            int progressBarHeight = 8;
            int progressBarY = _labelDescription.Bottom + 10;
            int progressBarX = 14 + shadowOffset / 2;
            int progressBarWidth = Width - 28 - shadowOffset;

            double progressPercent = 0;
            if (_challenge != null && _challenge.Target > 0)
            {
                progressPercent = (double)Math.Min(_challenge.Current, _challenge.Target) /
                    _challenge.Target;
            }

            Color progressStart, progressEnd;
            if (isClaimed)
            {
                progressStart = Color.FromArgb(76, 175, 80);
                progressEnd = Color.FromArgb(46, 125, 50);
            }
            else if (canClaim)
            {
                progressStart = Color.FromArgb(255, 193, 7);
                progressEnd = Color.FromArgb(255, 152, 0);
            }
            else
            {
                progressStart = Color.FromArgb(33, 150, 243);
                progressEnd = Color.FromArgb(66, 165, 245);
            }

            g.DrawGradientProgressBar(
                new Rectangle(progressBarX, progressBarY, progressBarWidth, progressBarHeight),
                progressBarHeight / 2,
                progressPercent,
                progressStart,
                progressEnd,
                Color.FromArgb(240, 240, 245));

            if (progressPercent > 0 && progressPercent < 1)
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

            UpdateTextColors();
            DrawClaimButton(g, canClaim, isClaimed, shadowOffset);
        }

        private void DrawClaimButton(Graphics g, bool canClaim, bool isClaimed, int shadowOffset)
        {
            if (!canClaim) return;

            int buttonWidth = 80;
            int buttonHeight = 28;
            int buttonX = Width - 14 - buttonWidth - shadowOffset / 2;
            int buttonY = Height - 14 - buttonHeight - shadowOffset / 2;

            var buttonRect = new Rectangle(buttonX, buttonY, buttonWidth, buttonHeight);

            using var buttonPath = GdiHelper.CreateRoundedRectPath(buttonRect, buttonHeight / 2);
            using var buttonBrush = new LinearGradientBrush(
                buttonRect,
                Color.FromArgb(255, 193, 7),
                Color.FromArgb(255, 111, 0),
                LinearGradientMode.Vertical);
            g.FillPath(buttonBrush, buttonPath);

            using var buttonShadowPath = GdiHelper.CreateRoundedRectPath(
                new Rectangle(buttonX + 1, buttonY + 2, buttonWidth, buttonHeight),
                buttonHeight / 2);
            using var buttonShadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0));
            g.FillPath(buttonShadowBrush, buttonShadowPath);

            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            using var textBrush = new SolidBrush(Color.White);
            var buttonFont = new Font("微软雅黑", 9F, FontStyle.Bold);
            g.DrawString("领取奖励", buttonFont, textBrush, buttonRect, sf);
        }

        private void UpdateTextColors()
        {
            bool isClaimed = _challenge?.Claimed == true;
            bool canClaim = _challenge?.Completed == true && !_challenge.Claimed;

            if (isClaimed)
            {
                _labelName.ForeColor = Color.FromArgb(51, 51, 51);
                _labelDescription.ForeColor = Color.FromArgb(153, 153, 153);
                _labelProgress.ForeColor = Color.FromArgb(76, 175, 80);
                _labelStatus.ForeColor = Color.FromArgb(76, 175, 80);
                _labelReward.ForeColor = Color.FromArgb(76, 175, 80);
            }
            else if (canClaim)
            {
                _labelName.ForeColor = Color.FromArgb(51, 51, 51);
                _labelDescription.ForeColor = Color.FromArgb(102, 102, 102);
                _labelProgress.ForeColor = Color.FromArgb(255, 111, 0);
                _labelReward.ForeColor = Color.FromArgb(255, 111, 0);
            }
            else
            {
                _labelName.ForeColor = Color.FromArgb(51, 51, 51);
                _labelDescription.ForeColor = Color.FromArgb(102, 102, 102);
                _labelProgress.ForeColor = Color.FromArgb(33, 150, 243);
                _labelReward.ForeColor = Color.FromArgb(255, 152, 0);
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

        public void RefreshStatus()
        {
            UpdateDisplay();
            UpdatePulseState();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pulseTimer?.Stop();
                _pulseTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
