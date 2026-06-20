using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Views;

namespace LearningAssistant.Forms
{
    public partial class UserComparisonForm : Form, IThemeable
    {
        private readonly List<UserComparisonData> _comparisonData;
        private readonly IThemeService? _themeService;
        private readonly List<Label> _dynamicHeaderLabels = new List<Label>();
        private readonly List<Label> _dynamicValueLabels = new List<Label>();
        private ThemeColors? _currentColors;

        public UserComparisonForm(List<UserComparisonData> comparisonData, IThemeService? themeService = null)
        {
            _comparisonData = comparisonData;
            _themeService = themeService;
            InitializeComponent();
            subtitleLabel.Text = $"共 {_comparisonData.Count} 位玩家参与对战";

            _themeService?.RegisterThemeable(this);

            LoadComparisonData();
        }

        private void LoadComparisonData()
        {
            if (_comparisonData.Count == 0) return;

            // 动态计算每列的宽度
            int userCount = _comparisonData.Count;
            int colWidth = (panelContent.Width - 150) / userCount;

            // 标题行
            for (int i = 0; i < userCount; i++)
            {
                var user = _comparisonData[i];
                var headerLabel = new Label
                {
                    Text = user.UserId,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
                    ForeColor = _currentColors?.TextPrimary ?? Color.FromArgb(33, 33, 33),
                    Dock = DockStyle.Fill,
                    Padding = new Padding(5)
                };

                // 添加到用户标题面板
                panelUserHeaders.Controls.Add(headerLabel);
                _dynamicHeaderLabels.Add(headerLabel);
            }

            // 数据行
            var stats = new (string Label, Func<UserComparisonData, string> Value, Func<UserComparisonData, int> Winner)[]
            {
                ("🔥 连续学习天数", u => $"{u.ConsecutiveStudyDays} 天", u => u.ConsecutiveStudyDays),
                ("📅 今日学习时长", u => $"{u.TodayStudyTimeMinutes} 分钟", u => u.TodayStudyTimeMinutes),
                ("🎯 正确率", u => $"{u.AccuracyRate:F1}%", u => (int)u.AccuracyRate),
                ("✅ 已掌握词汇", u => $"{u.KnownItemsCount} 个", u => u.KnownItemsCount),
                ("⏱️ 累计学习时长", u => $"{u.TotalStudyTimeMinutes} 分钟", u => u.TotalStudyTimeMinutes),
                ("📚 总词汇量", u => $"{u.TotalItems} 个", u => u.TotalItems),
                ("🏆 成就徽章", u => $"{u.AchievementCount} 个", u => u.AchievementCount)
            };

            foreach (var (label, value, winner) in stats)
            {
                // 找到该指标的获胜者
                int maxValue = _comparisonData.Max(u => winner(u));
                bool hasWinner = maxValue > 0;

                // 标签列
                var labelPanel = new Panel
                {
                    Width = 150,
                    Height = 50,
                    Dock = DockStyle.Top,
                    Padding = new Padding(5)
                };

                var labelText = new Label
                {
                    Text = label,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Microsoft YaHei", 10),
                    ForeColor = _currentColors?.TextSecondary ?? Color.FromArgb(117, 117, 117),
                    Dock = DockStyle.Fill
                };
                labelPanel.Controls.Add(labelText);
                panelStats.Add(labelPanel);

                // 用户数据列
                var valuesPanel = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = false,
                    Dock = DockStyle.Top,
                    Height = 50,
                    Margin = new Padding(0)
                };

                for (int i = 0; i < userCount; i++)
                {
                    var user = _comparisonData[i];
                    bool isWinner = hasWinner && winner(user) == maxValue;

                    var valuePanel = new Panel
                    {
                        Width = colWidth > 0 ? colWidth : 100,
                        Height = 50,
                        Margin = new Padding(0)
                    };

                    var valueLabel = new Label
                    {
                        Text = value(user),
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = new Font("Microsoft YaHei", isWinner ? 12 : 10, isWinner ? FontStyle.Bold : FontStyle.Regular),
                        ForeColor = isWinner ? Color.FromArgb(255, 87, 34) : (_currentColors?.TextPrimary ?? Color.FromArgb(33, 33, 33)),
                        Dock = DockStyle.Fill
                    };

                    if (isWinner && userCount > 1)
                    {
                        valueLabel.Text += " 👑";
                    }

                    valuePanel.Controls.Add(valueLabel);
                    valuesPanel.Controls.Add(valuePanel);
                    _dynamicValueLabels.Add(valueLabel);
                }

                panelValues.Add(valuesPanel);
            }

            // 重新布局
            ResizePanels();
        }

        private void ResizePanels()
        {
            int userCount = _comparisonData.Count;
            if (userCount == 0) return;

            int colWidth = Math.Max(100, (panelContent.Width - 150) / userCount);

            panelUserHeaders.SuspendLayout();
            foreach (Control ctrl in panelUserHeaders.Controls)
            {
                ctrl.Width = colWidth;
            }
            panelUserHeaders.ResumeLayout();

            foreach (var valuesPanel in panelValues)
            {
                valuesPanel.SuspendLayout();
                foreach (Control ctrl in valuesPanel.Controls)
                {
                    ctrl.Width = colWidth;
                }
                valuesPanel.ResumeLayout();
            }
        }
        private void InitializeComponent()
        {
            mainPanel = new Panel();
            panelContent = new Panel();
            panelUserHeaders = new FlowLayoutPanel();
            buttonPanel = new Panel();
            closeButton = new Button();
            refreshButton = new Button();
            subtitleLabel = new Label();
            titleLabel = new Label();
            panelStats = new List<Panel>();
            panelValues = new List<FlowLayoutPanel>();
            mainPanel.SuspendLayout();
            panelContent.SuspendLayout();
            buttonPanel.SuspendLayout();
            SuspendLayout();
            // 
            // mainPanel
            // 
            mainPanel.BackColor = Color.White;
            mainPanel.Controls.Add(panelContent);
            mainPanel.Controls.Add(buttonPanel);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(titleLabel);
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Location = new Point(0, 0);
            mainPanel.Name = "mainPanel";
            mainPanel.Padding = new Padding(20);
            mainPanel.Size = new Size(684, 461);
            mainPanel.TabIndex = 0;
            // 
            // panelContent
            // 
            panelContent.AutoScroll = true;
            panelContent.BackColor = Color.White;
            panelContent.Controls.Add(panelUserHeaders);
            panelContent.Dock = DockStyle.Fill;
            panelContent.Location = new Point(20, 110);
            panelContent.Name = "panelContent";
            panelContent.Size = new Size(644, 281);
            panelContent.TabIndex = 0;
            // 
            // panelUserHeaders
            // 
            panelUserHeaders.Dock = DockStyle.Top;
            panelUserHeaders.Location = new Point(0, 0);
            panelUserHeaders.Margin = new Padding(0);
            panelUserHeaders.Name = "panelUserHeaders";
            panelUserHeaders.Padding = new Padding(150, 0, 0, 0);
            panelUserHeaders.Size = new Size(644, 50);
            panelUserHeaders.TabIndex = 0;
            panelUserHeaders.WrapContents = false;
            // 
            // buttonPanel
            // 
            buttonPanel.BackColor = Color.White;
            buttonPanel.Controls.Add(closeButton);
            buttonPanel.Controls.Add(refreshButton);
            buttonPanel.Dock = DockStyle.Bottom;
            buttonPanel.Location = new Point(20, 391);
            buttonPanel.Name = "buttonPanel";
            buttonPanel.Size = new Size(644, 50);
            buttonPanel.TabIndex = 1;
            // 
            // closeButton
            // 
            closeButton.BackColor = Color.FromArgb(76, 175, 80);
            closeButton.FlatStyle = FlatStyle.Flat;
            closeButton.Font = new Font("微软雅黑", 10F);
            closeButton.ForeColor = Color.White;
            closeButton.Location = new Point(280, 8);
            closeButton.Name = "closeButton";
            closeButton.Size = new Size(100, 35);
            closeButton.TabIndex = 0;
            closeButton.Text = "关闭";
            closeButton.UseVisualStyleBackColor = false;
            closeButton.Click += closeButton_Click;
            // 
            // refreshButton
            // 
            refreshButton.BackColor = Color.FromArgb(33, 150, 243);
            refreshButton.FlatStyle = FlatStyle.Flat;
            refreshButton.Font = new Font("微软雅黑", 10F);
            refreshButton.ForeColor = Color.White;
            refreshButton.Location = new Point(400, 8);
            refreshButton.Name = "refreshButton";
            refreshButton.Size = new Size(100, 35);
            refreshButton.TabIndex = 1;
            refreshButton.Text = "刷新";
            refreshButton.UseVisualStyleBackColor = false;
            refreshButton.Click += refreshButton_Click;
            // 
            // subtitleLabel
            // 
            subtitleLabel.Dock = DockStyle.Top;
            subtitleLabel.Font = new Font("微软雅黑", 10F);
            subtitleLabel.ForeColor = Color.FromArgb(117, 117, 117);
            subtitleLabel.Location = new Point(20, 80);
            subtitleLabel.Name = "subtitleLabel";
            subtitleLabel.Size = new Size(644, 30);
            subtitleLabel.TabIndex = 2;
            subtitleLabel.Text = "共   位玩家参与对战";
            subtitleLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // titleLabel
            // 
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Font = new Font("微软雅黑", 18F, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(33, 33, 33);
            titleLabel.Location = new Point(20, 20);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(644, 60);
            titleLabel.TabIndex = 3;
            titleLabel.Text = "📊 用户学习对战";
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // UserComparisonForm
            // 
            BackColor = Color.FromArgb(250, 250, 250);
            ClientSize = new Size(684, 461);
            Controls.Add(mainPanel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "UserComparisonForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "🏆 用户对战";
            mainPanel.ResumeLayout(false);
            panelContent.ResumeLayout(false);
            buttonPanel.ResumeLayout(false);
            ResumeLayout(false);
        }
        private Panel panelContent = null!;
        private FlowLayoutPanel panelUserHeaders = null!;
        private List<Panel> panelStats = null!;
        private Panel mainPanel;
        private Panel buttonPanel;
        private Button closeButton;
        private Button refreshButton;
        private Label subtitleLabel;
        private Label titleLabel;
        private List<FlowLayoutPanel> panelValues = null!;

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {


            _dynamicHeaderLabels.Clear();
            _dynamicValueLabels.Clear();
            panelUserHeaders.Controls.Clear();
            foreach (var p in panelStats) p.Dispose();
            panelStats.Clear();
            foreach (var p in panelValues) p.Dispose();
            panelValues.Clear();
            LoadComparisonData();

        }

        public void ApplyTheme(ThemeColors colors)
        {
            _currentColors = colors;
            BackColor = colors.Background;

            if (mainPanel != null)
            {
                mainPanel.BackColor = colors.Surface;
            }

            if (panelContent != null)
            {
                panelContent.BackColor = colors.Surface;
            }

            if (buttonPanel != null)
            {
                buttonPanel.BackColor = colors.Surface;
            }

            if (subtitleLabel != null)
            {
                subtitleLabel.ForeColor = colors.TextSecondary;
            }

            if (titleLabel != null)
            {
                titleLabel.ForeColor = colors.TextPrimary;
            }

            // 更新动态创建的标签
            foreach (var label in _dynamicHeaderLabels)
            {
                label.ForeColor = colors.TextPrimary;
            }
            foreach (var label in _dynamicValueLabels)
            {
                // 保持获胜者颜色
                if (label.Text.Contains("👑"))
                {
                    label.ForeColor = Color.FromArgb(255, 87, 34);
                }
                else
                {
                    label.ForeColor = colors.TextPrimary;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _themeService?.UnregisterThemeable(this);
            }
            base.Dispose(disposing);
        }
    }
}
