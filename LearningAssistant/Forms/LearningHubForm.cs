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
        private readonly ILearningAnalyticsService? _learningAnalyticsService;
        private readonly ILogger<LearningHubForm>? _logger;

        public LearningHubForm(
            ISpacedRepetitionService? spacedRepetitionService = null,
            IUserSessionService? userSessionService = null,
            ILearningAnalyticsService? learningAnalyticsService = null,
            ILogger<LearningHubForm>? logger = null)
        {
            _spacedRepetitionService = spacedRepetitionService;
            _userSessionService = userSessionService;
            _learningAnalyticsService = learningAnalyticsService;
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
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(20)
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // 闪卡复习卡片
            var flashcardCard = CreateFeatureCard(
                "🧠 闪卡复习",
                "基于SM-2算法的间隔重复学习",
                "高效巩固记忆，智能安排复习",
                Color.FromArgb(76, 175, 80),
                1,
                OnFlashcardClick);

            // 学习统计卡片
            var statsCard = CreateFeatureCard(
                "📊 学习统计",
                "全面学习数据分析",
                "学习进度 · 掌握率 · 学习趋势",
                Color.FromArgb(244, 67, 54),
                2,
                OnStatsClick);


            mainLayout.Controls.Add(flashcardCard, 0, 0);
            mainLayout.Controls.Add(statsCard, 1, 0);

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

        private void OnStatsClick(object? sender, EventArgs e)
        {
            if (_learningAnalyticsService == null || _userSessionService == null)
            {
                ShowMessage("学习统计服务未配置");
                return;
            }

            try
            {
                var form = new LearningStatsForm(_learningAnalyticsService, _userSessionService);
                form.ShowDialog(this);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "打开学习统计失败");
                ShowMessage("打开学习统计失败");
            }
        }

        private void ShowMessage(string message)
        {
            MessageBox.Show(message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    /// <summary>
    /// 学习统计窗体
    /// </summary>
    public class LearningStatsForm : Form
    {
        private readonly ILearningAnalyticsService _analyticsService;
        private readonly IUserSessionService _userSessionService;
        private string _userId => _userSessionService.CurrentUserId;

        private Label _labelTitle = null!;
        private Label _labelStreak = null!;
        private Label _labelTodayItems = null!;
        private Label _labelTotalItems = null!;
        private Label _labelAccuracy = null!;
        private Label _labelStudyTime = null!;

        private MiniLineChart _trendChart = null!;
        private MiniLineChart _forgettingCurveChart = null!;
        private FlowLayoutPanel _categoryPanel = null!;
        private Label _labelRetention = null!;
        private Label _labelTotalReviews = null!;
        private Label _labelAvgEfficiency = null!;

        public LearningStatsForm(ILearningAnalyticsService analyticsService, IUserSessionService userSessionService)
        {
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _userSessionService = userSessionService ?? throw new ArgumentNullException(nameof(userSessionService));

            Text = "📊 学习统计";
            Size = new Size(900, 650);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(248, 249, 250);
            Font = new Font("微软雅黑", 9F);

            InitializeUI();
            LoadData();
        }

        private void InitializeUI()
        {
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20)
            };

            _labelTitle = new Label
            {
                Text = "📊 学习数据概览",
                Font = new Font("微软雅黑", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                AutoSize = true,
                Location = new Point(0, 0)
            };
            mainPanel.Controls.Add(_labelTitle);

            var statsCardsPanel = new FlowLayoutPanel
            {
                Location = new Point(0, 50),
                Size = new Size(840, 100),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _labelStreak = CreateStatCard("🔥 连续学习", "0 天", Color.FromArgb(255, 152, 0));
            _labelTodayItems = CreateStatCard("📚 今日学习", "0 项", Color.FromArgb(76, 175, 80));
            _labelTotalItems = CreateStatCard("📈 累计学习", "0 项", Color.FromArgb(33, 150, 243));
            _labelAccuracy = CreateStatCard("✅ 正确率", "0%", Color.FromArgb(156, 39, 176));
            _labelStudyTime = CreateStatCard("⏱️ 学习时长", "0 分钟", Color.FromArgb(0, 188, 212));

            statsCardsPanel.Controls.Add(_labelStreak.Parent!);
            statsCardsPanel.Controls.Add(_labelTodayItems.Parent!);
            statsCardsPanel.Controls.Add(_labelTotalItems.Parent!);
            statsCardsPanel.Controls.Add(_labelAccuracy.Parent!);
            statsCardsPanel.Controls.Add(_labelStudyTime.Parent!);
            mainPanel.Controls.Add(statsCardsPanel);

            var chartsPanel = new FlowLayoutPanel
            {
                Location = new Point(0, 170),
                Size = new Size(840, 180),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            var trendPanel = CreateChartPanel("📈 近7天学习趋势", out _trendChart);
            var forgetPanel = CreateChartPanel("🧠 遗忘曲线预测", out _forgettingCurveChart);

            chartsPanel.Controls.Add(trendPanel);
            chartsPanel.Controls.Add(forgetPanel);
            mainPanel.Controls.Add(chartsPanel);

            var categoryPanelTitle = new Label
            {
                Text = "📂 分类学习统计",
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                AutoSize = true,
                Location = new Point(0, 370)
            };
            mainPanel.Controls.Add(categoryPanelTitle);

            _categoryPanel = new FlowLayoutPanel
            {
                Location = new Point(0, 400),
                Size = new Size(840, 80),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };
            mainPanel.Controls.Add(_categoryPanel);

            var reviewPanelTitle = new Label
            {
                Text = "🔄 复习效率分析",
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                AutoSize = true,
                Location = new Point(0, 500)
            };
            mainPanel.Controls.Add(reviewPanelTitle);

            var reviewStatsPanel = new FlowLayoutPanel
            {
                Location = new Point(0, 530),
                Size = new Size(840, 60),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _labelRetention = CreateSmallStatCard("记忆保留率", "0%", Color.FromArgb(76, 175, 80));
            _labelTotalReviews = CreateSmallStatCard("总复习次数", "0", Color.FromArgb(33, 150, 243));
            _labelAvgEfficiency = CreateSmallStatCard("平均每张卡耗时", "0s", Color.FromArgb(255, 152, 0));

            reviewStatsPanel.Controls.Add(_labelRetention.Parent!);
            reviewStatsPanel.Controls.Add(_labelTotalReviews.Parent!);
            reviewStatsPanel.Controls.Add(_labelAvgEfficiency.Parent!);
            mainPanel.Controls.Add(reviewStatsPanel);

            Controls.Add(mainPanel);
        }

        private Label CreateStatCard(string title, string value, Color color)
        {
            var card = new Panel
            {
                Size = new Size(160, 90),
                BackColor = Color.White,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 10, 0)
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.FromArgb(120, 120, 120),
                AutoSize = true,
                Location = new Point(10, 10)
            };
            card.Controls.Add(titleLabel);

            var valueLabel = new Label
            {
                Text = value,
                Font = new Font("微软雅黑", 18F, FontStyle.Bold),
                ForeColor = color,
                AutoSize = true,
                Location = new Point(10, 40)
            };
            card.Controls.Add(valueLabel);

            return valueLabel;
        }

        private Label CreateSmallStatCard(string title, string value, Color color)
        {
            var card = new Panel
            {
                Size = new Size(200, 50),
                BackColor = Color.White,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 10, 0)
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("微软雅黑", 8F),
                ForeColor = Color.FromArgb(120, 120, 120),
                AutoSize = true,
                Location = new Point(10, 5)
            };
            card.Controls.Add(titleLabel);

            var valueLabel = new Label
            {
                Text = value,
                Font = new Font("微软雅黑", 14F, FontStyle.Bold),
                ForeColor = color,
                AutoSize = true,
                Location = new Point(10, 22)
            };
            card.Controls.Add(valueLabel);

            return valueLabel;
        }

        private Panel CreateChartPanel(string title, out MiniLineChart chart)
        {
            var panel = new Panel
            {
                Size = new Size(410, 170),
                BackColor = Color.White,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 15, 0)
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                AutoSize = true,
                Location = new Point(10, 8)
            };
            panel.Controls.Add(titleLabel);

            chart = new MiniLineChart
            {
                Location = new Point(10, 35),
                Size = new Size(390, 120)
            };
            panel.Controls.Add(chart);

            return panel;
        }

        private void LoadData()
        {
            try
            {
                var today = DateTime.Today;
                var dailyStats = _analyticsService.GetDailyStatistics(_userId, today);
                var streak = _analyticsService.GetStudyStreak(_userId);
                var totalItems = _analyticsService.GetTotalLearnedItems(_userId, today.AddDays(-30));
                var accuracy = _analyticsService.GetAccuracyRate(_userId, today.AddDays(-30));
                var studyMinutes = _analyticsService.GetTotalStudyMinutes(_userId, today.AddDays(-30));

                _labelStreak.Text = $"{streak} 天";
                _labelTodayItems.Text = $"{dailyStats.TotalItems} 项";
                _labelTotalItems.Text = $"{totalItems} 项";
                _labelAccuracy.Text = $"{accuracy:F1}%";
                _labelStudyTime.Text = $"{studyMinutes} 分钟";

                var trendData = _analyticsService.GetLearningTrend(_userId, today.AddDays(-6), today);
                var trendValues = trendData.Select(d => (double)d.TotalItems).ToList();
                _trendChart.SetData(trendValues);
                _trendChart.Title = "每日学习项数";

                var forgettingCurve = _analyticsService.GenerateForgettingCurve(_userId, 7);
                var forgetValues = forgettingCurve.OrderBy(kv => kv.Key).Select(kv => kv.Value * 100).ToList();
                _forgettingCurveChart.SetData(forgetValues);
                _forgettingCurveChart.LineColor = Color.FromArgb(255, 152, 0);
                _forgettingCurveChart.FillColor = Color.FromArgb(255, 152, 0);
                _forgettingCurveChart.Title = "记忆保留率(%)";

                var categoryStats = _analyticsService.GetCategoryStats(_userId);
                LoadCategoryStats(categoryStats);

                var reviewStats = _analyticsService.GetReviewEfficiencyStats(_userId);
                var retentionRate = _analyticsService.CalculateRetentionRate(_userId);
                _labelRetention.Text = $"{retentionRate * 100:F1}%";
                _labelTotalReviews.Text = $"{reviewStats.TotalReviews}";
                _labelAvgEfficiency.Text = $"{reviewStats.ReviewTimePerCard:F1}s";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载统计数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadCategoryStats(Dictionary<string, int> categoryStats)
        {
            _categoryPanel.Controls.Clear();

            if (categoryStats.Count == 0)
            {
                var emptyLabel = new Label
                {
                    Text = "暂无分类数据",
                    ForeColor = Color.Gray,
                    AutoSize = true
                };
                _categoryPanel.Controls.Add(emptyLabel);
                return;
            }

            var colors = new[]
            {
                Color.FromArgb(76, 175, 80),
                Color.FromArgb(33, 150, 243),
                Color.FromArgb(255, 152, 0),
                Color.FromArgb(156, 39, 176),
                Color.FromArgb(0, 188, 212),
                Color.FromArgb(244, 67, 54),
                Color.FromArgb(96, 125, 139)
            };

            int colorIndex = 0;
            foreach (var kvp in categoryStats.OrderByDescending(kv => kv.Value))
            {
                var color = colors[colorIndex % colors.Length];
                var card = CreateSmallStatCard(kvp.Key, $"{kvp.Value} 项", color);
                _categoryPanel.Controls.Add(card.Parent!);
                colorIndex++;
            }
        }
    }
}
