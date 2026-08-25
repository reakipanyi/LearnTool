using LearningAssistant.Common;
using LearningAssistant.Forms.UserControls.Common;
using LearningAssistant.Forms.UserControls.Gamification;
using LearningAssistant.Services.Learning;
using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls.Learning
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
        private Panel _panelInsights = null!;
        private Label _labelInsightsTitle = null!;
        private Label _labelInsightsContent = null!;
        private Panel _panelContent = null!;
        private Label _labelListTitle = null!;
        private FlowLayoutPanel _flowLayoutPanelItems = null!;
        private Button _buttonStartReview = null!;
        private EmptyStateView? _emptyState;

        private readonly ISpacedRepetitionService? _spacedRepetitionService;
        private readonly string _userId = Constants.DefaultUserId;
        private List<ReviewItem> _dueItems = new();

        private int _dueTotal = 0;
        private int _todayTotal = 0;
        private double _retentionRate = 0;

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
        public event EventHandler? GoLearnClicked;

        public ReviewPanel()
        {
            InitializeComponent();
        }

        public ReviewPanel(ISpacedRepetitionService spacedRepetitionService, string userId = Constants.DefaultUserId) : this()
        {
            _spacedRepetitionService = spacedRepetitionService;
            _userId = userId;
            RefreshData();
        }

        private void InitializeComponent()
        {
            _panelHeader = new Panel();
            _labelSubtitle = new Label();
            _labelTitle = new Label();
            _panelStats = new Panel();
            _panelRetention = new Panel();
            _labelRetentionLabel = new Label();
            _labelRetention = new Label();
            _panelTodayCount = new Panel();
            _labelTodayLabel = new Label();
            _labelTodayCount = new Label();
            _panelDueCount = new Panel();
            _labelDueLabel = new Label();
            _labelDueCount = new Label();
            _panelInsights = new Panel();
            _labelInsightsContent = new Label();
            _labelInsightsTitle = new Label();
            _panelContent = new Panel();
            _flowLayoutPanelItems = new FlowLayoutPanel();
            _labelListTitle = new Label();
            _buttonStartReview = new Button();
            _panelChart = new Panel();
            _miniLineChart = new MiniLineChart();
            _labelChartTitle = new Label();
            _panelHeader.SuspendLayout();
            _panelStats.SuspendLayout();
            _panelRetention.SuspendLayout();
            _panelTodayCount.SuspendLayout();
            _panelDueCount.SuspendLayout();
            _panelInsights.SuspendLayout();
            _panelContent.SuspendLayout();
            _panelChart.SuspendLayout();
            SuspendLayout();
            // 
            // _panelHeader
            // 
            _panelHeader.BackColor = Color.FromArgb(33, 150, 243);
            _panelHeader.Controls.Add(_labelSubtitle);
            _panelHeader.Controls.Add(_labelTitle);
            _panelHeader.Dock = DockStyle.Top;
            _panelHeader.Location = new Point(0, 0);
            _panelHeader.Name = "_panelHeader";
            _panelHeader.Padding = new Padding(15, 10, 15, 10);
            _panelHeader.Size = new Size(450, 80);
            _panelHeader.TabIndex = 4;
            // 
            // _labelSubtitle
            // 
            _labelSubtitle.Dock = DockStyle.Top;
            _labelSubtitle.Font = new Font("微软雅黑", 9F);
            _labelSubtitle.ForeColor = Color.FromArgb(220, 235, 250);
            _labelSubtitle.Location = new Point(15, 40);
            _labelSubtitle.Name = "_labelSubtitle";
            _labelSubtitle.Size = new Size(420, 20);
            _labelSubtitle.TabIndex = 0;
            _labelSubtitle.Text = "基于 SM-2 算法，科学安排复习时间";
            // 
            // _labelTitle
            // 
            _labelTitle.Dock = DockStyle.Top;
            _labelTitle.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            _labelTitle.ForeColor = Color.White;
            _labelTitle.Location = new Point(15, 10);
            _labelTitle.Name = "_labelTitle";
            _labelTitle.Size = new Size(420, 30);
            _labelTitle.TabIndex = 1;
            _labelTitle.Text = "🔔 间隔重复复习";
            // 
            // _panelStats
            // 
            _panelStats.BackColor = Color.FromArgb(250, 250, 252);
            _panelStats.Controls.Add(_panelRetention);
            _panelStats.Controls.Add(_panelTodayCount);
            _panelStats.Controls.Add(_panelDueCount);
            _panelStats.Dock = DockStyle.Top;
            _panelStats.Location = new Point(0, 80);
            _panelStats.Name = "_panelStats";
            _panelStats.Padding = new Padding(10);
            _panelStats.Size = new Size(450, 90);
            _panelStats.TabIndex = 3;
            // 
            // _panelRetention
            // 
            _panelRetention.BackColor = Color.White;
            _panelRetention.Controls.Add(_labelRetentionLabel);
            _panelRetention.Controls.Add(_labelRetention);
            _panelRetention.Dock = DockStyle.Left;
            _panelRetention.Location = new Point(290, 10);
            _panelRetention.Margin = new Padding(5);
            _panelRetention.Name = "_panelRetention";
            _panelRetention.Size = new Size(140, 70);
            _panelRetention.TabIndex = 0;
            _panelRetention.Paint += PanelRetention_Paint;
            // 
            // _labelRetentionLabel
            // 
            _labelRetentionLabel.Dock = DockStyle.Fill;
            _labelRetentionLabel.Font = new Font("微软雅黑", 8.5F);
            _labelRetentionLabel.ForeColor = Color.FromArgb(102, 102, 102);
            _labelRetentionLabel.Location = new Point(0, 45);
            _labelRetentionLabel.Name = "_labelRetentionLabel";
            _labelRetentionLabel.Size = new Size(140, 25);
            _labelRetentionLabel.TabIndex = 0;
            _labelRetentionLabel.Text = "记忆保持率";
            _labelRetentionLabel.TextAlign = ContentAlignment.TopCenter;
            // 
            // _labelRetention
            // 
            _labelRetention.Dock = DockStyle.Top;
            _labelRetention.Font = new Font("微软雅黑", 20F, FontStyle.Bold);
            _labelRetention.ForeColor = Color.FromArgb(255, 152, 0);
            _labelRetention.Location = new Point(0, 0);
            _labelRetention.Name = "_labelRetention";
            _labelRetention.Size = new Size(140, 45);
            _labelRetention.TabIndex = 1;
            _labelRetention.Text = "0%";
            _labelRetention.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // _panelTodayCount
            // 
            _panelTodayCount.BackColor = Color.White;
            _panelTodayCount.Controls.Add(_labelTodayLabel);
            _panelTodayCount.Controls.Add(_labelTodayCount);
            _panelTodayCount.Dock = DockStyle.Left;
            _panelTodayCount.Location = new Point(150, 10);
            _panelTodayCount.Margin = new Padding(5);
            _panelTodayCount.Name = "_panelTodayCount";
            _panelTodayCount.Size = new Size(140, 70);
            _panelTodayCount.TabIndex = 1;
            _panelTodayCount.Paint += PanelTodayCount_Paint;
            // 
            // _labelTodayLabel
            // 
            _labelTodayLabel.Dock = DockStyle.Fill;
            _labelTodayLabel.Font = new Font("微软雅黑", 8.5F);
            _labelTodayLabel.ForeColor = Color.FromArgb(102, 102, 102);
            _labelTodayLabel.Location = new Point(0, 45);
            _labelTodayLabel.Name = "_labelTodayLabel";
            _labelTodayLabel.Size = new Size(140, 25);
            _labelTodayLabel.TabIndex = 0;
            _labelTodayLabel.Text = "今日已复习";
            _labelTodayLabel.TextAlign = ContentAlignment.TopCenter;
            // 
            // _labelTodayCount
            // 
            _labelTodayCount.Dock = DockStyle.Top;
            _labelTodayCount.Font = new Font("微软雅黑", 20F, FontStyle.Bold);
            _labelTodayCount.ForeColor = Color.FromArgb(76, 175, 80);
            _labelTodayCount.Location = new Point(0, 0);
            _labelTodayCount.Name = "_labelTodayCount";
            _labelTodayCount.Size = new Size(140, 45);
            _labelTodayCount.TabIndex = 1;
            _labelTodayCount.Text = "0";
            _labelTodayCount.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // _panelDueCount
            // 
            _panelDueCount.BackColor = Color.White;
            _panelDueCount.Controls.Add(_labelDueLabel);
            _panelDueCount.Controls.Add(_labelDueCount);
            _panelDueCount.Dock = DockStyle.Left;
            _panelDueCount.Location = new Point(10, 10);
            _panelDueCount.Margin = new Padding(5);
            _panelDueCount.Name = "_panelDueCount";
            _panelDueCount.Size = new Size(140, 70);
            _panelDueCount.TabIndex = 2;
            _panelDueCount.Paint += PanelDueCount_Paint;
            // 
            // _labelDueLabel
            // 
            _labelDueLabel.Dock = DockStyle.Fill;
            _labelDueLabel.Font = new Font("微软雅黑", 8.5F);
            _labelDueLabel.ForeColor = Color.FromArgb(102, 102, 102);
            _labelDueLabel.Location = new Point(0, 45);
            _labelDueLabel.Name = "_labelDueLabel";
            _labelDueLabel.Size = new Size(140, 25);
            _labelDueLabel.TabIndex = 0;
            _labelDueLabel.Text = "今日待复习";
            _labelDueLabel.TextAlign = ContentAlignment.TopCenter;
            // 
            // _labelDueCount
            // 
            _labelDueCount.Dock = DockStyle.Top;
            _labelDueCount.Font = new Font("微软雅黑", 20F, FontStyle.Bold);
            _labelDueCount.ForeColor = Color.FromArgb(244, 67, 54);
            _labelDueCount.Location = new Point(0, 0);
            _labelDueCount.Name = "_labelDueCount";
            _labelDueCount.Size = new Size(140, 45);
            _labelDueCount.TabIndex = 1;
            _labelDueCount.Text = "0";
            _labelDueCount.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // _panelInsights
            // 
            _panelInsights.BackColor = Color.FromArgb(250, 250, 252);
            _panelInsights.Controls.Add(_labelInsightsContent);
            _panelInsights.Controls.Add(_labelInsightsTitle);
            _panelInsights.Dock = DockStyle.Top;
            _panelInsights.Location = new Point(0, 260);
            _panelInsights.Name = "_panelInsights";
            _panelInsights.Padding = new Padding(15, 5, 15, 8);
            _panelInsights.Size = new Size(450, 70);
            _panelInsights.TabIndex = 1;
            // 
            // _labelInsightsContent
            // 
            _labelInsightsContent.AutoSize = true;
            _labelInsightsContent.Dock = DockStyle.Fill;
            _labelInsightsContent.Font = new Font("微软雅黑", 8.5F);
            _labelInsightsContent.ForeColor = Color.FromArgb(51, 51, 51);
            _labelInsightsContent.Location = new Point(15, 23);
            _labelInsightsContent.MaximumSize = new Size(400, 0);
            _labelInsightsContent.Name = "_labelInsightsContent";
            _labelInsightsContent.Size = new Size(113, 17);
            _labelInsightsContent.TabIndex = 0;
            _labelInsightsContent.Text = "正在分析学习数据...";
            // 
            // _labelInsightsTitle
            // 
            _labelInsightsTitle.Dock = DockStyle.Top;
            _labelInsightsTitle.Font = new Font("微软雅黑", 8.5F, FontStyle.Bold);
            _labelInsightsTitle.ForeColor = Color.FromArgb(102, 102, 102);
            _labelInsightsTitle.Location = new Point(15, 5);
            _labelInsightsTitle.Name = "_labelInsightsTitle";
            _labelInsightsTitle.Size = new Size(420, 18);
            _labelInsightsTitle.TabIndex = 1;
            _labelInsightsTitle.Text = "💡 学习洞察";
            // 
            // _panelContent
            // 
            _panelContent.BackColor = Color.White;
            _panelContent.Controls.Add(_flowLayoutPanelItems);
            _panelContent.Controls.Add(_labelListTitle);
            _panelContent.Controls.Add(_buttonStartReview);
            _panelContent.Dock = DockStyle.Fill;
            _panelContent.Location = new Point(0, 330);
            _panelContent.Name = "_panelContent";
            _panelContent.Padding = new Padding(15, 10, 15, 10);
            _panelContent.Size = new Size(450, 290);
            _panelContent.TabIndex = 0;
            // 
            // _flowLayoutPanelItems
            // 
            _flowLayoutPanelItems.AutoScroll = true;
            _flowLayoutPanelItems.BackColor = Color.Transparent;
            _flowLayoutPanelItems.Dock = DockStyle.Fill;
            _flowLayoutPanelItems.FlowDirection = FlowDirection.TopDown;
            _flowLayoutPanelItems.Location = new Point(15, 35);
            _flowLayoutPanelItems.Name = "_flowLayoutPanelItems";
            _flowLayoutPanelItems.Size = new Size(420, 205);
            _flowLayoutPanelItems.TabIndex = 0;
            _flowLayoutPanelItems.WrapContents = false;
            // 
            // _labelListTitle
            // 
            _labelListTitle.Dock = DockStyle.Top;
            _labelListTitle.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _labelListTitle.ForeColor = Color.FromArgb(51, 51, 51);
            _labelListTitle.Location = new Point(15, 10);
            _labelListTitle.Name = "_labelListTitle";
            _labelListTitle.Size = new Size(420, 25);
            _labelListTitle.TabIndex = 1;
            _labelListTitle.Text = "待复习内容";
            // 
            // _buttonStartReview
            // 
            _buttonStartReview.BackColor = Color.FromArgb(33, 150, 243);
            _buttonStartReview.Cursor = Cursors.Hand;
            _buttonStartReview.Dock = DockStyle.Bottom;
            _buttonStartReview.FlatAppearance.BorderSize = 0;
            _buttonStartReview.FlatStyle = FlatStyle.Flat;
            _buttonStartReview.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _buttonStartReview.ForeColor = Color.White;
            _buttonStartReview.Location = new Point(15, 240);
            _buttonStartReview.Name = "_buttonStartReview";
            _buttonStartReview.Size = new Size(420, 40);
            _buttonStartReview.TabIndex = 2;
            _buttonStartReview.Text = "🚀 开始复习";
            _buttonStartReview.UseVisualStyleBackColor = false;
            _buttonStartReview.Click += ButtonStartReview_Click;
            // 
            // _panelChart
            // 
            _panelChart.BackColor = Color.FromArgb(250, 250, 252);
            _panelChart.Controls.Add(_miniLineChart);
            _panelChart.Controls.Add(_labelChartTitle);
            _panelChart.Dock = DockStyle.Top;
            _panelChart.Location = new Point(0, 170);
            _panelChart.Name = "_panelChart";
            _panelChart.Padding = new Padding(15, 5, 15, 5);
            _panelChart.Size = new Size(450, 90);
            _panelChart.TabIndex = 2;
            // 
            // _miniLineChart
            // 
            _miniLineChart.BackColor = Color.Transparent;
            _miniLineChart.Dock = DockStyle.Fill;
            _miniLineChart.Location = new Point(15, 23);
            _miniLineChart.Name = "_miniLineChart";
            _miniLineChart.Size = new Size(420, 62);
            _miniLineChart.TabIndex = 0;
            // 
            // _labelChartTitle
            // 
            _labelChartTitle.Dock = DockStyle.Top;
            _labelChartTitle.Font = new Font("微软雅黑", 8.5F, FontStyle.Bold);
            _labelChartTitle.ForeColor = Color.FromArgb(102, 102, 102);
            _labelChartTitle.Location = new Point(15, 5);
            _labelChartTitle.Name = "_labelChartTitle";
            _labelChartTitle.Size = new Size(420, 18);
            _labelChartTitle.TabIndex = 1;
            _labelChartTitle.Text = "📈 7天记忆保持率趋势";
            // 
            // ReviewPanel
            // 
            BackColor = Color.White;
            Controls.Add(_panelContent);
            Controls.Add(_panelInsights);
            Controls.Add(_panelChart);
            Controls.Add(_panelStats);
            Controls.Add(_panelHeader);
            DoubleBuffered = true;
            Name = "ReviewPanel";
            Size = new Size(450, 620);
            _panelHeader.ResumeLayout(false);
            _panelStats.ResumeLayout(false);
            _panelRetention.ResumeLayout(false);
            _panelTodayCount.ResumeLayout(false);
            _panelDueCount.ResumeLayout(false);
            _panelInsights.ResumeLayout(false);
            _panelInsights.PerformLayout();
            _panelContent.ResumeLayout(false);
            _panelChart.ResumeLayout(false);
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

            _dueTotal = _dueItems.Count;
            _todayTotal = todayCount;
            _retentionRate = retention;

            _labelDueCount.Text = _dueItems.Count.ToString();
            _labelTodayCount.Text = todayCount.ToString();
            _labelRetention.Text = $"{retention:P0}";

            var trendData = GenerateMockTrendData(retention, 7);
            _miniLineChart.SetData(trendData);

            UpdateInsights(retention, _dueItems.Count, todayCount);

            UpdateItemsList();

            _panelDueCount.Invalidate();
            _panelTodayCount.Invalidate();
            _panelRetention.Invalidate();

            if (_dueItems.Count == 0)
            {
                ShowEmptyState(todayCount > 0);
            }
            else
            {
                HideEmptyState();
            }
        }

        private void PanelDueCount_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel panel) return;
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int ringSize = 50;
            int x = panel.Width - ringSize - 12;
            int y = (panel.Height - ringSize) / 2 - 5;

            float progress = 0;
            Color progressColor = Color.FromArgb(244, 67, 54);
            Color bgColor = Color.FromArgb(240, 240, 245);

            int target = Math.Max(_dueTotal + _todayTotal, 1);
            if (target > 0)
            {
                progress = Math.Min((float)_dueTotal / target, 1.0f);
            }

            DrawMiniRing(g, x, y, ringSize, progress, progressColor, bgColor);
        }

        private void PanelTodayCount_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel panel) return;
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int ringSize = 50;
            int x = panel.Width - ringSize - 12;
            int y = (panel.Height - ringSize) / 2 - 5;

            float progress = 0;
            Color progressColor = Color.FromArgb(76, 175, 80);
            Color bgColor = Color.FromArgb(240, 240, 245);

            int target = Math.Max(_todayTotal + 5, 10);
            if (target > 0)
            {
                progress = Math.Min((float)_todayTotal / target, 1.0f);
            }

            DrawMiniRing(g, x, y, ringSize, progress, progressColor, bgColor);
        }

        private void PanelRetention_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel panel) return;
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int ringSize = 50;
            int x = panel.Width - ringSize - 12;
            int y = (panel.Height - ringSize) / 2 - 5;

            float progress = (float)_retentionRate;
            Color progressColor = Color.FromArgb(255, 152, 0);
            Color bgColor = Color.FromArgb(240, 240, 245);

            DrawMiniRing(g, x, y, ringSize, progress, progressColor, bgColor);
        }

        private static void DrawMiniRing(Graphics g, int x, int y, int size, float progress, Color progressColor, Color bgColor)
        {
            float lineWidth = 4;
            float radius = (size - lineWidth) / 2f;
            float cx = x + size / 2f;
            float cy = y + size / 2f;

            var rect = new RectangleF(x + lineWidth / 2f, y + lineWidth / 2f, size - lineWidth, size - lineWidth);

            using (var bgPen = new Pen(bgColor, lineWidth))
            {
                g.DrawArc(bgPen, rect, 0, 360);
            }

            if (progress > 0)
            {
                using var progressPen = new Pen(progressColor, lineWidth);
                progressPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                progressPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                float sweepAngle = progress * 360;
                g.DrawArc(progressPen, rect, -90, sweepAngle);
            }
        }

        private void ShowEmptyState(bool hasReviewedToday)
        {
            if (_emptyState == null)
            {
                _emptyState = new EmptyStateView
                {
                    Dock = DockStyle.Fill
                };
                _emptyState.ActionClicked += OnEmptyStateActionClicked;
                _panelContent.Controls.Add(_emptyState);
            }

            if (hasReviewedToday)
            {
                _emptyState.ShowReviewCompleted();
            }
            else
            {
                _emptyState.ShowNoReviewDue();
            }

            _emptyState.Visible = true;
            _emptyState.BringToFront();
            _labelListTitle.Visible = false;
            _flowLayoutPanelItems.Visible = false;
            _buttonStartReview.Visible = false;
        }

        private void HideEmptyState()
        {
            if (_emptyState != null)
            {
                _emptyState.Visible = false;
            }
            _labelListTitle.Visible = true;
            _flowLayoutPanelItems.Visible = true;
            _buttonStartReview.Visible = true;
        }

        private void OnEmptyStateActionClicked(object? sender, EventArgs e)
        {
            GoLearnClicked?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateInsights(double retention, int dueCount, int todayReviewed)
        {
            var insights = GenerateInsights(retention, dueCount, todayReviewed);
            _labelInsightsContent.Text = insights;
        }

        private static string GenerateInsights(double retention, int dueCount, int todayReviewed)
        {
            var insights = new List<string>();

            if (retention >= 0.9)
            {
                insights.Add("🎉 记忆保持率优秀！继续保持。");
            }
            else if (retention >= 0.7)
            {
                insights.Add("👍 记忆保持率良好，坚持复习效果更佳。");
            }
            else if (retention >= 0.5)
            {
                insights.Add("📚 记忆保持率一般，建议增加复习频率。");
            }
            else
            {
                insights.Add("⚠️ 记忆保持率偏低，需要加强复习！");
            }

            if (dueCount > 10)
            {
                insights.Add($"待复习项较多（{dueCount}个），建议分批完成。");
            }
            else if (dueCount == 0)
            {
                insights.Add("今天没有待复习内容，可以学习新内容啦！");
            }

            if (todayReviewed >= 10)
            {
                insights.Add("今日已复习10+项，学习很棒！");
            }
            else if (todayReviewed == 0 && dueCount > 0)
            {
                insights.Add("今天还没开始复习，现在就开始吧！");
            }

            return string.Join(" ", insights.Take(2));
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
