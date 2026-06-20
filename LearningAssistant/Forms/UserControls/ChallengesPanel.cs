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
        private Panel _panelContent = null!;
        private FlowLayoutPanel _flowLayoutPanelCards = null!;

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
            _panelContent = new Panel();
            _flowLayoutPanelCards = new FlowLayoutPanel();

            _panelHeader.SuspendLayout();
            _panelContent.SuspendLayout();
            SuspendLayout();

            _panelHeader.Dock = DockStyle.Top;
            _panelHeader.Height = 70;
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
            _labelStats.Text = $"完成 {completedCount} / {_challenges.Count}  |  已领取 {claimedCount}";

            foreach (var challenge in _challenges)
            {
                var card = new ChallengeCard
                {
                    Challenge = challenge,
                    Margin = new Padding(5)
                };
                card.ClaimClicked += OnClaimClicked;
                _flowLayoutPanelCards.Controls.Add(card);
            }
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
        }
    }
}
