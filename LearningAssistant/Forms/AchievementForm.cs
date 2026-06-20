using LearningAssistant.Forms.UserControls;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Gamification;

namespace LearningAssistant.Forms
{
    public class AchievementForm : Form
    {
        private AchievementsPanel _achievementsPanel = null!;
        private readonly IGamificationService _gamificationService;

        public AchievementForm(IGamificationService gamificationService)
        {
            _gamificationService = gamificationService;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            _achievementsPanel = new AchievementsPanel();

            SuspendLayout();

            _achievementsPanel.Dock = DockStyle.Fill;

            Text = "🏆 成就系统";
            Size = new Size(680, 600);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Font = new Font("微软雅黑", 9F);
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;

            Controls.Add(_achievementsPanel);

            _achievementsPanel.BadgeClicked += OnBadgeClicked;

            ResumeLayout(false);
        }

        private void LoadData()
        {
            var badges = _gamificationService.GetAllBadges().ToList();
            var progress = _gamificationService.GetBadgeProgress();

            _achievementsPanel.Badges = badges;
            _achievementsPanel.BadgeProgress = progress;
        }

        private void OnBadgeClicked(object? sender, Badge badge)
        {
            string status = badge.IsUnlocked ? "✅ 已解锁" : "🔒 未解锁";
            string progressText = badge.IsUnlocked
                ? ""
                : $"\n\n进度: {(_gamificationService.GetBadgeProgress().TryGetValue(badge.Id, out var val) ? val : 0)} / {badge.Requirement.TargetValue}";

            MessageBox.Show(
                $"{badge.Icon} {badge.Name}\n\n{badge.Description}\n\n{status}{progressText}",
                "成就详情",
                MessageBoxButtons.OK,
                badge.IsUnlocked ? MessageBoxIcon.Information : MessageBoxIcon.Question);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _achievementsPanel.Dispose();
        }
    }
}
