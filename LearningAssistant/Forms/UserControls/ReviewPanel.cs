using LearningAssistant.Services.Learning;
using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls
{
    public class ReviewPanel : UserControl
    {
        private Panel _panelHeader = null!;
        private Label _labelTitle = null!;
        private Label _labelSubtitle = null!;
        private Panel _panelStats = null!;
        private Panel _panelDueCount = null!;
        private Label _labelDueCount = null!;
        private Label _labelDueLabel = null!;
        private Panel _panelTodayCount = null!;
        private Label _labelTodayCount = null!;
        private Label _labelTodayLabel = null!;
        private Panel _panelRetention = null!;
        private Label _labelRetention = null!;
        private Label _labelRetentionLabel = null!;
        private Panel _panelChart = null!;
        private MiniLineChart _miniLineChart = null!;
        private Label _labelChartTitle = null!;
        private Panel _panelContent = null!;
        private Label _labelListTitle = null!;
        private FlowLayoutPanel _flowLayoutPanelItems = null!;
        private Button _buttonStartReview = null!;

        private readonly ISpacedRepetitionService? _spacedRepetitionService;
        private readonly string _userId = "default";
        private List<ReviewItem> _dueItems = new();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<ReviewItem> DueItems
        {
            get => _dueItems;
            set
            {
                _dueItems = value ?? new List<ReviewItem>();
                UpdateItemsList();
            }
        }

        public event EventHandler? StartReviewClicked;

        public ReviewPanel()
        {
            InitializeComponent();
        }

        public ReviewPanel(ISpacedRepetitionService spacedRepetitionService, string userId = "default") : this()
        {
            _spacedRepetitionService = spacedRepetitionService;
            _userId = userId;
            RefreshData();
        }

        private void InitializeComponent()
        {
            _panelHeader = new Panel();
            _labelTitle = new Label();
            _labelSubtitle = new Label();
            _panelStats = new Panel();
            _panelDueCount = new Panel();
            _labelDueCount = new Label();
            _labelDueLabel = new Label();
            _panelTodayCount = new Panel();
            _labelTodayCount = new Label();
            _labelTodayLabel = new Label();
            _panelRetention = new Panel();
            _labelRetention = new Label();
            _labelRetentionLabel = new Label();
            _panelContent = new Panel();
            _labelListTitle = new Label();
            _flowLayoutPanelItems = new FlowLayoutPanel();
            _buttonStartReview = new Button();

            _panelHeader.SuspendLayout();
            _panelStats.SuspendLayout();
            _panelDueCount.SuspendLayout();
            _panelTodayCount.SuspendLayout();
            _panelRetention.SuspendLayout();
            _panelContent.SuspendLayout();
            SuspendLayout();

            _panelHeader.Dock = DockStyle.Top;
            _panelHeader.Height = 80;
            _panelHeader.Padding = new Padding(15, 10, 15, 10);
            _panelHeader.BackColor = Color.FromArgb(33, 150, 243);

            _labelTitle.Dock = DockStyle.Top;
            _labelTitle.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            _labelTitle.ForeColor = Color.White;
            _labelTitle.Text = "🔔 间隔重复复习";
            _labelTitle.Height = 30;

            _labelSubtitle.Dock = DockStyle.Top;
            _labelSubtitle.Font = new Font("微软雅黑", 9F);
            _labelSubtitle.ForeColor = Color.FromArgb(220, 235, 250);
            _labelSubtitle.Text = "基于 SM-2 算法，科学安排复习时间";
            _labelSubtitle.Height = 20;

            _panelHeader.Controls.Add(_labelSubtitle);
            _panelHeader.Controls.Add(_labelTitle);

            _panelStats.Dock = DockStyle.Top;
            _panelStats.Height = 80;
            _panelStats.BackColor = Color.FromArgb(250, 250, 252);
            _panelStats.Padding = new Padding(10, 10, 10, 10);

            _panelDueCount.Dock = DockStyle.Left;
            _panelDueCount.Width = 120;
            _panelDueCount.BackColor = Color.White;
            _panelDueCount.Margin = new Padding(5);

            _labelDueCount.Dock = DockStyle.Top;
            _labelDueCount.Font = new Font("微软雅黑", 20F, FontStyle.Bold);
            _labelDueCount.ForeColor = Color.FromArgb(244, 67, 54);
            _labelDueCount.TextAlign = ContentAlignment.MiddleCenter;
            _labelDueCount.Text = "0";
            _labelDueCount.Height = 40;

            _labelDueLabel.Dock = DockStyle.Fill;
            _labelDueLabel.Font = new Font("微软雅黑", 8.5F);
            _labelDueLabel.ForeColor = Color.FromArgb(102, 102, 102);
            _labelDueLabel.TextAlign = ContentAlignment.TopCenter;
            _labelDueLabel.Text = "今日待复习";

            _panelDueCount.Controls.Add(_labelDueLabel);
            _panelDueCount.Controls.Add(_labelDueCount);

            _panelTodayCount.Dock = DockStyle.Left;
            _panelTodayCount.Width = 120;
            _panelTodayCount.BackColor = Color.White;
            _panelTodayCount.Margin = new Padding(5);

            _labelTodayCount.Dock = DockStyle.Top;
            _labelTodayCount.Font = new Font("微软雅黑", 20F, FontStyle.Bold);
            _labelTodayCount.ForeColor = Color.FromArgb(76, 175, 80);
            _labelTodayCount.TextAlign = ContentAlignment.MiddleCenter;
            _labelTodayCount.Text = "0";
            _labelTodayCount.Height = 40;

            _labelTodayLabel.Dock = DockStyle.Fill;
            _labelTodayLabel.Font = new Font("微软雅黑", 8.5F);
            _labelTodayLabel.ForeColor = Color.FromArgb(102, 102, 102);
            _labelTodayLabel.TextAlign = ContentAlignment.TopCenter;
            _labelTodayLabel.Text = "今日已复习";

            _panelTodayCount.Controls.Add(_labelTodayLabel);
            _panelTodayCount.Controls.Add(_labelTodayCount);

            _panelRetention.Dock = DockStyle.Left;
            _panelRetention.Width = 120;
            _panelRetention.BackColor = Color.White;
            _panelRetention.Margin = new Padding(5);

            _labelRetention.Dock = DockStyle.Top;
            _labelRetention.Font = new Font("微软雅黑", 20F, FontStyle.Bold);
            _labelRetention.ForeColor = Color.FromArgb(255, 152, 0);
            _labelRetention.TextAlign = ContentAlignment.MiddleCenter;
            _labelRetention.Text = "0%";
            _labelRetention.Height = 40;

            _labelRetentionLabel.Dock = DockStyle.Fill;
            _labelRetentionLabel.Font = new Font("微软雅黑", 8.5F);
            _labelRetentionLabel.ForeColor = Color.FromArgb(102, 102, 102);
            _labelRetentionLabel.TextAlign = ContentAlignment.TopCenter;
            _labelRetentionLabel.Text = "记忆保持率";

            _panelRetention.Controls.Add(_labelRetentionLabel);
            _panelRetention.Controls.Add(_labelRetention);

            _panelStats.Controls.Add(_panelRetention);
            _panelStats.Controls.Add(_panelTodayCount);
            _panelStats.Controls.Add(_panelDueCount);

            _panelChart = new Panel();
            _panelChart.Dock = DockStyle.Top;
            _panelChart.Height = 90;
            _panelChart.BackColor = Color.FromArgb(250, 250, 252);
            _panelChart.Padding = new Padding(15, 5, 15, 5);

            _labelChartTitle = new Label();
            _labelChartTitle.Dock = DockStyle.Top;
            _labelChartTitle.Font = new Font("微软雅黑", 8.5F, FontStyle.Bold);
            _labelChartTitle.ForeColor = Color.FromArgb(102, 102, 102);
            _labelChartTitle.Text = "📈 7天记忆保持率趋势";
            _labelChartTitle.Height = 18;

            _miniLineChart = new MiniLineChart();
            _miniLineChart.Dock = DockStyle.Fill;
            _miniLineChart.LineColor = Color.FromArgb(33, 150, 243);
            _miniLineChart.FillColor = Color.FromArgb(33, 150, 243);

            _panelChart.Controls.Add(_miniLineChart);
            _panelChart.Controls.Add(_labelChartTitle);

            _panelContent.Dock = DockStyle.Fill;
            _panelContent.BackColor = Color.White;
            _panelContent.Padding = new Padding(15, 10, 15, 10);

            _labelListTitle.Dock = DockStyle.Top;
            _labelListTitle.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _labelListTitle.ForeColor = Color.FromArgb(51, 51, 51);
            _labelListTitle.Text = "待复习内容";
            _labelListTitle.Height = 25;

            _flowLayoutPanelItems.Dock = DockStyle.Fill;
            _flowLayoutPanelItems.AutoScroll = true;
            _flowLayoutPanelItems.WrapContents = false;
            _flowLayoutPanelItems.FlowDirection = FlowDirection.TopDown;
            _flowLayoutPanelItems.BackColor = Color.Transparent;

            _buttonStartReview.Dock = DockStyle.Bottom;
            _buttonStartReview.Height = 40;
            _buttonStartReview.Text = "🚀 开始复习";
            _buttonStartReview.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _buttonStartReview.ForeColor = Color.White;
            _buttonStartReview.BackColor = Color.FromArgb(33, 150, 243);
            _buttonStartReview.FlatStyle = FlatStyle.Flat;
            _buttonStartReview.Cursor = Cursors.Hand;
            _buttonStartReview.Click += ButtonStartReview_Click;
            _buttonStartReview.FlatAppearance.BorderSize = 0;

            _panelContent.Controls.Add(_flowLayoutPanelItems);
            _panelContent.Controls.Add(_labelListTitle);
            _panelContent.Controls.Add(_buttonStartReview);

            Controls.Add(_panelContent);
            Controls.Add(_panelChart);
            Controls.Add(_panelStats);
            Controls.Add(_panelHeader);

            Size = new Size(450, 550);
            BackColor = Color.White;
            DoubleBuffered = true;

            _panelHeader.ResumeLayout(false);
            _panelStats.ResumeLayout(false);
            _panelDueCount.ResumeLayout(false);
            _panelTodayCount.ResumeLayout(false);
            _panelRetention.ResumeLayout(false);
            _panelContent.ResumeLayout(false);
            ResumeLayout(false);
        }

        private void ButtonStartReview_Click(object? sender, EventArgs e)
        {
            StartReviewClicked?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateItemsList()
        {
            _flowLayoutPanelItems.Controls.Clear();

            int displayCount = Math.Min(_dueItems.Count, 8);
            for (int i = 0; i < displayCount; i++)
            {
                var item = _dueItems[i];
                var itemPanel = new Panel
                {
                    Width = _flowLayoutPanelItems.Width - 20,
                    Height = 45,
                    Margin = new Padding(0, 3, 0, 3),
                    BackColor = Color.FromArgb(248, 249, 250)
                };

                var contentLabel = new Label
                {
                    Text = item.Content.Length > 25 ? item.Content.Substring(0, 25) + "..." : item.Content,
                    Location = new Point(10, 12),
                    Size = new Size(itemPanel.Width - 80, 20),
                    Font = new Font("微软雅黑", 9F),
                    ForeColor = Color.FromArgb(51, 51, 51),
                    AutoEllipsis = true
                };

                string difficultyText = item.Difficulty switch
                {
                    1 => "简单",
                    2 => "中等",
                    _ => "困难"
                };
                Color difficultyColor = item.Difficulty switch
                {
                    1 => Color.FromArgb(76, 175, 80),
                    2 => Color.FromArgb(255, 152, 0),
                    _ => Color.FromArgb(244, 67, 54)
                };

                var diffLabel = new Label
                {
                    Text = difficultyText,
                    Location = new Point(itemPanel.Width - 70, 12),
                    Size = new Size(60, 20),
                    Font = new Font("微软雅黑", 8F),
                    ForeColor = difficultyColor,
                    TextAlign = ContentAlignment.MiddleRight
                };

                itemPanel.Controls.Add(contentLabel);
                itemPanel.Controls.Add(diffLabel);

                _flowLayoutPanelItems.Controls.Add(itemPanel);
            }

            if (_dueItems.Count > 8)
            {
                var moreLabel = new Label
                {
                    Text = $"... 还有 {_dueItems.Count - 8} 项待复习",
                    Dock = DockStyle.Top,
                    Height = 25,
                    Font = new Font("微软雅黑", 8.5F),
                    ForeColor = Color.FromArgb(153, 153, 153),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                _flowLayoutPanelItems.Controls.Add(moreLabel);
            }

            if (_dueItems.Count == 0)
            {
                var emptyLabel = new Label
                {
                    Text = "🎉 今天没有待复习的内容！",
                    Dock = DockStyle.Top,
                    Height = 60,
                    Font = new Font("微软雅黑", 10F),
                    ForeColor = Color.FromArgb(76, 175, 80),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                _flowLayoutPanelItems.Controls.Add(emptyLabel);
                _buttonStartReview.Enabled = false;
                _buttonStartReview.BackColor = Color.FromArgb(180, 180, 180);
                _buttonStartReview.Text = "今日复习已完成";
            }
            else
            {
                _buttonStartReview.Enabled = true;
                _buttonStartReview.BackColor = Color.FromArgb(33, 150, 243);
                _buttonStartReview.Text = "🚀 开始复习";
            }
        }

        public void RefreshData()
        {
            if (_spacedRepetitionService == null) return;

            _dueItems = _spacedRepetitionService.GetItemsDueForReview(_userId);
            int todayCount = _spacedRepetitionService.GetTodayReviewCount(_userId);
            double retention = _spacedRepetitionService.CalculateRetentionRate(_userId);

            _labelDueCount.Text = _dueItems.Count.ToString();
            _labelTodayCount.Text = todayCount.ToString();
            _labelRetention.Text = $"{retention:P0}";

            var trendData = GenerateMockTrendData(retention, 7);
            _miniLineChart.SetData(trendData);

            UpdateItemsList();
        }

        private static List<double> GenerateMockTrendData(double currentValue, int days)
        {
            var random = new Random();
            var result = new List<double>();
            double value = currentValue;

            for (int i = 0; i < days; i++)
            {
                double variation = (random.NextDouble() - 0.5) * 0.1;
                value = Math.Clamp(value + variation, 0.3, 1.0);
                result.Add(value);
            }

            result[^1] = currentValue;
            return result;
        }
    }
}
