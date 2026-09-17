using LearningAssistant.Forms.UserControls.Common;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Gamification;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls.Gamification
{
    public class ChallengesPanel : UserControl
    {
        private Panel _panelHeader = null!;
        private Label _labelTitle = null!;
        private Label _labelStats = null!;
        private Label _labelCountdown = null!;
        private ProgressRingControl _progressRing = null!;
        private Panel _panelContent = null!;
        private FlowLayoutPanel _flowLayoutPanelCards = null!;
        private EmptyStateView? _emptyState;
        private System.Windows.Forms.Timer? _countdownTimer;

        private List<Challenge> _challenges = new();
        private readonly IGamificationService? _gamificationService;
        private FloatingText? _floatingText;
        private ConfettiControl? _confettiControl;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<Challenge> Challenges
        {
            get => _challenges;
            set
            {
                _challenges = value ?? new List<Challenge>();
                UpdateCards();
            }
        }

        public event EventHandler<Challenge>? ClaimRewardClicked;
        public event EventHandler<Challenge>? TaskClicked;
        public event EventHandler<(int completed, int total)>? StatsUpdated;

        public ChallengesPanel()
        {
            InitializeComponent();
        }

        public ChallengesPanel(IGamificationService gamificationService) : this()
        {
            _gamificationService = gamificationService;
        }

        private void InitializeComponent()
        {
            _panelHeader = new Panel();
            _labelTitle = new Label();
            _labelStats = new Label();
            _labelCountdown = new Label();
            _progressRing = new ProgressRingControl();
            _panelContent = new Panel();
            _flowLayoutPanelCards = new FlowLayoutPanel();

            _panelHeader.SuspendLayout();
            _panelContent.SuspendLayout();
            SuspendLayout();

            _panelHeader.Dock = DockStyle.Top;
            _panelHeader.Height = 100;
            _panelHeader.Padding = new Padding(15, 10, 15, 10);
            _panelHeader.BackColor = Color.FromArgb(250, 250, 252);

            _labelTitle.Dock = DockStyle.Top;
            _labelTitle.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            _labelTitle.ForeColor = Color.FromArgb(51, 51, 51);
            _labelTitle.Text = "🎯 每日挑战";
            _labelTitle.Height = 30;

            _labelStats.Dock = DockStyle.Top;
            _labelStats.Font = new Font("微软雅黑", 10F);
            _labelStats.ForeColor = Color.FromArgb(102, 102, 102);
            _labelStats.Text = "完成 0 / 0";
            _labelStats.Height = 25;

            _labelCountdown.Dock = DockStyle.Top;
            _labelCountdown.Font = new Font("微软雅黑", 9F);
            _labelCountdown.ForeColor = Color.FromArgb(153, 153, 153);
            _labelCountdown.Text = "⏰ 刷新倒计时: --:--:--";
            _labelCountdown.Height = 20;

            _progressRing.Size = new Size(56, 56);
            _progressRing.ProgressColor = Color.FromArgb(255, 152, 0);
            _progressRing.TrackColor = Color.FromArgb(230, 230, 235);
            _progressRing.TextColor = Color.FromArgb(102, 102, 102);
            _progressRing.CenterText = "0%";
            _progressRing.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _progressRing.Location = new Point(470, 12);

            _panelHeader.Controls.Add(_progressRing);
            _panelHeader.Controls.Add(_labelCountdown);
            _panelHeader.Controls.Add(_labelStats);
            _panelHeader.Controls.Add(_labelTitle);

            _panelContent.Dock = DockStyle.Fill;
            _panelContent.AutoScroll = true;
            _panelContent.Padding = new Padding(15, 10, 15, 10);
            _panelContent.BackColor = Color.White;

            _flowLayoutPanelCards.Dock = DockStyle.Top;
            _flowLayoutPanelCards.AutoSize = true;
            _flowLayoutPanelCards.WrapContents = true;
            _flowLayoutPanelCards.BackColor = Color.Transparent;

            _panelContent.Controls.Add(_flowLayoutPanelCards);

            Controls.Add(_panelContent);
            Controls.Add(_panelHeader);

            Size = new Size(550, 400);
            BackColor = Color.White;
            DoubleBuffered = true;

            _panelHeader.ResumeLayout(false);
            _panelContent.ResumeLayout(false);
            ResumeLayout(false);
        }

        private void UpdateCards()
        {
            _flowLayoutPanelCards.Controls.Clear();

            int completedCount = _challenges.Count(c => c.Completed);
            int claimedCount = _challenges.Count(c => c.Claimed);
            int totalCount = _challenges.Count;
            int unclaimedCount = completedCount - claimedCount;
            double completePercent = totalCount > 0 ? (double)completedCount / totalCount : 0;

            string statsText = $"完成 {completedCount} / {totalCount}";
            if (unclaimedCount > 0)
            {
                statsText += $"  ·  待领取 {unclaimedCount} 个奖励";
            }

            _labelStats.Text = statsText;
            _progressRing.Progress = (float)completePercent;
            _progressRing.CenterText = $"{(int)(completePercent * 100)}%";

            // 触发统计更新事件
            StatsUpdated?.Invoke(this, (completedCount, totalCount));

            if (_challenges.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            HideEmptyState();

            foreach (var challenge in _challenges)
            {
                var card = CreateChallengeCard(challenge);
                _flowLayoutPanelCards.Controls.Add(card);
            }
        }

        private Panel CreateChallengeCard(Challenge challenge)
        {
            Panel panel = new Panel
            {
                Size = new Size(250, 80),
                BackColor = challenge.Completed ? Color.FromArgb(230, 255, 230) : Color.FromArgb(250, 250, 252),
                Margin = new Padding(5)
            };

            // 绘制圆角边框
            panel.Paint += (s, e) =>
            {
                if (e == null) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var borderPen = new Pen(challenge.Completed ? Color.FromArgb(76, 175, 80) : Color.FromArgb(230, 230, 235), 1);
                var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                using var path = RoundedRect(rect, 8);
                g.DrawPath(borderPen, path);
            };

            // 图标和名称
            Label labelName = new Label
            {
                Text = $"{challenge.Emoji} {challenge.Name}",
                Location = new Point(15, 12),
                Size = new Size(200, 20),
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33)
            };

            // 进度
            ProgressBar progress = new ProgressBar
            {
                Location = new Point(15, 38),
                Size = new Size(150, 18),
                Maximum = Math.Max(1, challenge.Target),
                Value = Math.Min(challenge.Current, challenge.Target),
                Style = ProgressBarStyle.Continuous
            };

            // 进度文本
            Label labelProgress = new Label
            {
                Text = $"{challenge.Current}/{challenge.Target}",
                Location = new Point(170, 38),
                Size = new Size(60, 18),
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // XP奖励
            Label labelReward = new Label
            {
                Text = $"+{challenge.Reward} XP",
                Location = new Point(180, 12),
                Size = new Size(60, 20),
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 152, 0),
                TextAlign = ContentAlignment.MiddleRight
            };

            panel.Controls.Add(labelName);
            panel.Controls.Add(progress);
            panel.Controls.Add(labelProgress);
            panel.Controls.Add(labelReward);

            // 领取按钮或状态
            if (challenge.Completed && !challenge.Claimed)
            {
                Button claimBtn = new Button
                {
                    Text = "领取奖励",
                    Size = new Size(70, 28),
                    Location = new Point(175, 45),
                    BackColor = Color.FromArgb(255, 152, 0),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Tag = challenge
                };
                claimBtn.FlatAppearance.BorderSize = 0;
                claimBtn.Click += (s, e) =>
                {
                    if (s is Button btn && btn.Tag is Challenge ch)
                    {
                        OnClaimClicked(this, ch);
                    }
                };
                panel.Controls.Add(claimBtn);
            }
            else if (challenge.Claimed)
            {
                Label labelClaimed = new Label
            {
                    Text = "✓ 已领取",
                    Location = new Point(180, 45),
                    Size = new Size(55, 28),
                    Font = new Font("微软雅黑", 9F),
                    ForeColor = Color.FromArgb(76, 175, 80),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                panel.Controls.Add(labelClaimed);
            }

            return panel;
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

        private void ShowEmptyState()
        {
            if (_emptyState == null)
            {
                _emptyState = new EmptyStateView
                {
                    Dock = DockStyle.Fill
                };
                _emptyState.SetState(EmptyStateType.NoChallenges);
                _panelContent.Controls.Add(_emptyState);
                _emptyState.BringToFront();
            }
            _emptyState.Visible = true;
            _flowLayoutPanelCards.Visible = false;
        }

        private void HideEmptyState()
        {
            if (_emptyState != null)
            {
                _emptyState.Visible = false;
            }
            _flowLayoutPanelCards.Visible = true;
        }

        private void OnClaimClicked(object? sender, Challenge challenge)
        {
            ClaimRewardClicked?.Invoke(this, challenge);
            if (_gamificationService != null)
            {
                _gamificationService.ClaimChallengeReward(challenge.Id);

                if (sender is Panel card)
                {
                    ShowRewardFloatingText(card, challenge.Reward);
                }

                UpdateCards();
            }
        }

        private void OnTaskClicked(object? sender, Challenge challenge)
        {
            TaskClicked?.Invoke(this, challenge);
        }

        private void ShowRewardFloatingText(Panel card, int reward)
        {
            if (_floatingText == null)
            {
                _floatingText = new FloatingText();
                Controls.Add(_floatingText);
            }

            if (_confettiControl == null)
            {
                _confettiControl = new ConfettiControl();
                Controls.Add(_confettiControl);
            }

            var cardRect = card.RectangleToScreen(
                new Rectangle(0, 0, card.Width, card.Height));
            var panelPoint = PointToClient(cardRect.Location);

            // 显示XP浮动文字
            _floatingText.TextColor = Color.FromArgb(255, 152, 0);
            _floatingText.ShowAt(this,
                panelPoint.X + card.Width / 2 - 40,
                panelPoint.Y + card.Height / 2 - 15,
                $"+{reward} XP");

            // 显示彩带动画
            _confettiControl.StartCelebration();
        }

        public void RefreshData()
        {
            if (_gamificationService != null)
            {
                _challenges = _gamificationService.GetDailyChallenges().ToList();
            }
            UpdateCards();
            StartCountdown();
        }

        private void StartCountdown()
        {
            if (_countdownTimer == null)
            {
                _countdownTimer = new System.Windows.Forms.Timer();
                _countdownTimer.Interval = 1000;
                _countdownTimer.Tick += CountdownTimer_Tick;
            }

            UpdateCountdownDisplay();
            _countdownTimer.Start();
        }

        private void StopCountdown()
        {
            _countdownTimer?.Stop();
        }

        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            UpdateCountdownDisplay();
        }

        private void UpdateCountdownDisplay()
        {
            var now = DateTime.Now;
            var nextRefresh = DateTime.Today.AddDays(1).AddHours(6);

            if (now < DateTime.Today.AddHours(6))
            {
                nextRefresh = DateTime.Today.AddHours(6);
            }

            var timeLeft = nextRefresh - now;

            if (timeLeft.TotalSeconds <= 0)
            {
                _labelCountdown.Text = "⏰ 正在刷新...";
                RefreshData();
                return;
            }

            _labelCountdown.Text = $"⏰ 刷新倒计时: {timeLeft.Hours:D2}:{timeLeft.Minutes:D2}:{timeLeft.Seconds:D2}";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopCountdown();
                _countdownTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
