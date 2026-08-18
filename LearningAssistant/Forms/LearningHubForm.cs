using LearningAssistant.Forms.UserControls;
using LearningAssistant.Forms.UserControls.Cards;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Forms
{
    public class LearningHubForm : Form
    {
        private readonly ISpacedRepetitionService? _spacedRepetitionService;
        private readonly IUserSessionService? _userSessionService;
        private readonly ILogger<LearningHubForm>? _logger;

        public LearningHubForm(
            ISpacedRepetitionService? spacedRepetitionService = null,
            IUserSessionService? userSessionService = null,
            ILogger<LearningHubForm>? logger = null)
        {
            _spacedRepetitionService = spacedRepetitionService;
            _userSessionService = userSessionService;
            _logger = logger;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "🎯 学习中心";
            Size = new Size(600, 400);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.FromArgb(248, 249, 250);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1,
                Padding = new Padding(20)
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // 闪卡复习卡片
            var flashcardCard = CreateFeatureCard(
                "🧠 闪卡复习",
                "基于SM-2算法的间隔重复学习",
                "高效巩固记忆，智能安排复习",
                Color.FromArgb(76, 175, 80),
                1,
                OnFlashcardClick);

            mainLayout.Controls.Add(flashcardCard, 0, 0);

            Controls.Add(mainLayout);
        }

        private FeatureCard CreateFeatureCard(
            string title, string subtitle, string description,
            Color color, int order, EventHandler onClick)
        {
            var card = new FeatureCard
            {
                Title = title,
                Subtitle = subtitle,
                Description = description,
                Icon = title.Split(' ')[0],
                PrimaryColor = color,
                Margin = new Padding(10)
            };

            card.CardClicked += onClick;
            return card;
        }

        private void OnFlashcardClick(object? sender, EventArgs e)
        {
            if (_spacedRepetitionService == null || _userSessionService == null)
            {
                ShowMessage("闪卡复习服务未配置");
                return;
            }

            try
            {
                var form = new FlashcardReviewForm(
                    _spacedRepetitionService,
                    null,
                    _userSessionService);
                form.ShowDialog(this);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "打开闪卡复习失败");
                ShowMessage("打开闪卡复习失败");
            }
        }

        private void ShowMessage(string message)
        {
            MessageBox.Show(message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
