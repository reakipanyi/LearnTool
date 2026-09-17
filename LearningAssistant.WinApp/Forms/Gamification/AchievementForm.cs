using LearningAssistant.Forms.UserControls;
using LearningAssistant.Forms.UserControls.Gamification;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Gamification;

namespace LearningAssistant.Forms.Gamification
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
            bool isHiddenLocked = badge.IsHidden && !badge.IsUnlocked;

            string icon = isHiddenLocked ? "❓" : badge.Icon;
            string name = isHiddenLocked ? "???" : badge.Name;
            string description = isHiddenLocked ? "这是一个神秘成就，达成特定条件后自动解锁并揭晓。" : badge.Description;
            string status = badge.IsUnlocked ? "✅ 已解锁" : (isHiddenLocked ? "🔮 隐藏成就" : "🔒 未解锁");
            string progressText = badge.IsUnlocked || isHiddenLocked
                ? ""
                : $"\n\n进度: {(_gamificationService.GetBadgeProgress().TryGetValue(badge.Id, out var val) ? val : 0)} / {badge.Requirement.TargetValue}";

            MessageBox.Show(
                $"{icon} {name}\n\n{description}\n\n{status}{progressText}",
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
