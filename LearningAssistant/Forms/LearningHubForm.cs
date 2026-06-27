using LearningAssistant.Forms.UserControls;
using LearningAssistant.Forms.UserControls.Cards;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.KnowledgeGraph;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Quiz;
using LearningAssistant.Services.Speech;
using Microsoft.Extensions.Logging;
using System.Windows.Forms;
using System.Drawing;

namespace LearningAssistant.Forms
{
    public class LearningHubForm : Form
    {
        private readonly ISpacedRepetitionService? _spacedRepetitionService;
        private readonly IConversationContextService? _conversationContextService;
        private readonly IUserSessionService? _userSessionService;
        private readonly IKnowledgeGraphService? _knowledgeGraphService;
        private readonly IVoiceRecallService? _voiceRecallService;
        private readonly ILogger<LearningHubForm>? _logger;

        public LearningHubForm(
            ISpacedRepetitionService? spacedRepetitionService = null,
            IConversationContextService? conversationContextService = null,
            IUserSessionService? userSessionService = null,
            IKnowledgeGraphService? knowledgeGraphService = null,
            IVoiceRecallService? voiceRecallService = null,
            ILogger<LearningHubForm>? logger = null)
        {
            _spacedRepetitionService = spacedRepetitionService;
            _conversationContextService = conversationContextService;
            _userSessionService = userSessionService;
            _knowledgeGraphService = knowledgeGraphService;
            _voiceRecallService = voiceRecallService;
            _logger = logger;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "🎯 学习中心";
            Size = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.FromArgb(248, 249, 250);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(20)
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            // 闪卡复习卡片
            var flashcardCard = CreateFeatureCard(
                "🧠 闪卡复习",
                "基于SM-2算法的间隔重复学习",
                "高效巩固记忆，智能安排复习",
                Color.FromArgb(76, 175, 80),
                1,
                OnFlashcardClick);

            // AI导师卡片
            var mentorCard = CreateFeatureCard(
                "🤖 AI导师",
                "智能答疑与引导式学习",
                "苏格拉底引导 · 费曼检验 · 薄弱点诊断",
                Color.FromArgb(33, 150, 243),
                2,
                OnMentorClick);

            // 语音回忆卡片
            var voiceCard = CreateFeatureCard(
                "🎙️ 语音回忆",
                "语音交互学习模式",
                "朗读问题 · 语音回答 · AI评估",
                Color.FromArgb(156, 39, 176),
                3,
                OnVoiceRecallClick);

            // 知识图谱卡片
            var graphCard = CreateFeatureCard(
                "🌐 知识图谱",
                "3D可视化知识网络",
                "掌握程度可视化 · 学习路径推荐",
                Color.FromArgb(0, 188, 212),
                4,
                OnKnowledgeGraphClick);

            // 测验引擎卡片
            var quizCard = CreateFeatureCard(
                "📝 智能测验",
                "AI生成练习题",
                "多种题型 · 自动评分 · 错题分析",
                Color.FromArgb(255, 152, 0),
                5,
                OnQuizClick);

            // 学习统计卡片
            var statsCard = CreateFeatureCard(
                "📊 学习统计",
                "全面学习数据分析",
                "学习进度 · 掌握率 · 学习趋势",
                Color.FromArgb(244, 67, 54),
                6,
                OnStatsClick);

            mainLayout.Controls.Add(flashcardCard, 0, 0);
            mainLayout.Controls.Add(mentorCard, 1, 0);
            mainLayout.Controls.Add(voiceCard, 2, 0);
            mainLayout.Controls.Add(graphCard, 0, 1);
            mainLayout.Controls.Add(quizCard, 1, 1);
            mainLayout.Controls.Add(statsCard, 2, 1);

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
                    _conversationContextService!,
                    _userSessionService);
                form.ShowDialog(this);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "打开闪卡复习失败");
                ShowMessage("打开闪卡复习失败");
            }
        }

        private void OnMentorClick(object? sender, EventArgs e)
        {
            if (_conversationContextService == null)
            {
                ShowMessage("AI导师服务未配置");
                return;
            }

            var mentorForm = new MentorDialogForm(_conversationContextService);
            mentorForm.ShowDialog(this);
        }

        private void OnVoiceRecallClick(object? sender, EventArgs e)
        {
            if (_voiceRecallService == null)
            {
                ShowMessage("语音回忆服务未配置");
                return;
            }

            var voiceForm = new VoiceRecallForm(_voiceRecallService);
            voiceForm.ShowDialog(this);
        }

        private void OnKnowledgeGraphClick(object? sender, EventArgs e)
        {
            if (_knowledgeGraphService == null || _userSessionService == null)
            {
                ShowMessage("知识图谱服务未配置");
                return;
            }

            var graphForm = new KnowledgeGraphForm(_knowledgeGraphService, _userSessionService);
            graphForm.ShowDialog(this);
        }

        private void OnQuizClick(object? sender, EventArgs e)
        {
            ShowMessage("智能测验功能开发中...");
        }

        private void OnStatsClick(object? sender, EventArgs e)
        {
            ShowMessage("学习统计功能开发中...");
        }

        private void ShowMessage(string message)
        {
            MessageBox.Show(message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    /// <summary>
    /// 导师对话弹窗
    /// </summary>
    public class MentorDialogForm : Form
    {
        private readonly MentorAIPanel _mentorPanel;

        public MentorDialogForm(IConversationContextService contextService)
        {
            Text = "🤖 AI导师";
            Size = new Size(600, 500);
            StartPosition = FormStartPosition.CenterScreen;

            _mentorPanel = new MentorAIPanel
            {
                Dock = DockStyle.Fill,
                ContextService = contextService
            };

            Controls.Add(_mentorPanel);
        }
    }

    /// <summary>
    /// 语音回忆弹窗
    /// </summary>
    public class VoiceRecallForm : Form
    {
        private readonly IVoiceRecallService _voiceRecallService;

        public VoiceRecallForm(IVoiceRecallService voiceRecallService)
        {
            Text = "🎙️ 语音回忆";
            Size = new Size(500, 400);
            StartPosition = FormStartPosition.CenterScreen;
            _voiceRecallService = voiceRecallService;

            InitializeUI();
        }

        private void InitializeUI()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                Padding = new Padding(20)
            };

            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 15));

            var instructionLabel = new Label
            {
                Text = "🎙️ 点击下方按钮开始语音回忆\n\n系统将朗读问题，请口头回答",
                Font = new Font("微软雅黑", 14F),
                ForeColor = Color.FromArgb(80, 80, 80),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            var resultLabel = new Label
            {
                Text = "",
                Font = new Font("微软雅黑", 12F),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            var buttonStart = new Button
            {
                Text = "🎤 开始语音回忆",
                Font = new Font("微软雅黑", 12F),
                BackColor = Color.FromArgb(156, 39, 176),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand
            };

            buttonStart.Click += async (s, e) =>
            {
                buttonStart.Enabled = false;
                resultLabel.Text = "正在准备问题...";

                await Task.Delay(1000);

                resultLabel.Text = "此功能需要结合具体学习内容使用，请在学习界面调用";
                buttonStart.Enabled = true;
            };

            layout.Controls.Add(instructionLabel, 0, 0);
            layout.Controls.Add(resultLabel, 0, 1);
            layout.Controls.Add(buttonStart, 0, 2);

            Controls.Add(layout);
        }
    }

    /// <summary>
    /// 知识图谱弹窗
    /// </summary>
    public class KnowledgeGraphForm : Form
    {
        private readonly KnowledgeGraphView _graphView;

        public KnowledgeGraphForm(IKnowledgeGraphService graphService, IUserSessionService userSessionService)
        {
            Text = "🌐 知识图谱";
            Size = new Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;

            _graphView = new KnowledgeGraphView
            {
                Dock = DockStyle.Fill
            };
            _graphView.SetService(graphService);
            _graphView.SetUserId(userSessionService.GetCurrentUserId());

            Controls.Add(_graphView);

            Load += async (s, e) => await _graphView.LoadGraphAsync();
        }
    }
}
