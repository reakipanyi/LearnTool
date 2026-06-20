using LearningAssistant.Forms.UserControls;
using LearningAssistant.Services.Gamification;

namespace LearningAssistant.Forms
{
    public class ChallengeForm : Form
    {
        private ChallengesPanel _challengesPanel = null!;
        private readonly IGamificationService _gamificationService;

        public ChallengeForm(IGamificationService gamificationService)
        {
            _gamificationService = gamificationService;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            _challengesPanel = new ChallengesPanel(_gamificationService);

            SuspendLayout();

            _challengesPanel.Dock = DockStyle.Fill;
            _challengesPanel.ClaimRewardClicked += OnClaimRewardClicked;

            Text = "🎯 每日挑战";
            Size = new Size(550, 450);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Font = new Font("微软雅黑", 9F);
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;

            Controls.Add(_challengesPanel);

            ResumeLayout(false);
        }

        private void LoadData()
        {
            _challengesPanel.RefreshData();
        }

        private void OnClaimRewardClicked(object? sender, Models.User.Challenge e)
        {
            _challengesPanel.RefreshData();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _challengesPanel.Dispose();
        }
    }
}
