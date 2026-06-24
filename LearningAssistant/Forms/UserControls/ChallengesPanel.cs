using LearningAssistant.Models.User;
using LearningAssistant.Services.Gamification;
using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls
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

            if (_challenges.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            HideEmptyState();

            foreach (var challenge in _challenges)
            {
                var card = new ChallengeCard
                {
                    Challenge = challenge,
                    Margin = new Padding(5)
                };
                card.ClaimClicked += OnClaimClicked;
                card.TaskClicked += OnTaskClicked;
                _flowLayoutPanelCards.Controls.Add(card);
            }
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

                if (sender is ChallengeCard card)
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

        private void ShowRewardFloatingText(ChallengeCard card, int reward)
        {
            if (_floatingText == null)
            {
                _floatingText = new FloatingText();
                Controls.Add(_floatingText);
            }

            var cardRect = card.RectangleToScreen(
                new Rectangle(0, 0, card.Width, card.Height));
            var panelPoint = PointToClient(cardRect.Location);

            _floatingText.Text = $"+{reward} XP";
            _floatingText.TextColor = Color.FromArgb(255, 152, 0);
            _floatingText.ShowAt(this,
                panelPoint.X + card.Width / 2 - 40,
                panelPoint.Y + card.Height / 2 - 15,
                $"+{reward} XP");
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
