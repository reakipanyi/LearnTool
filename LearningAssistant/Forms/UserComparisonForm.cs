using LearningAssistant.Views;

namespace LearningAssistant.Forms
{
    public partial class UserComparisonForm : Form
    {
        private readonly List<UserComparisonData> _comparisonData;

        public UserComparisonForm(List<UserComparisonData> comparisonData)
        {
            _comparisonData = comparisonData;
            InitializeComponent();
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
                    ForeColor = Color.FromArgb(33, 33, 33),
                    Dock = DockStyle.Fill,
                    Padding = new Padding(5)
                };

                // 添加到用户标题面板
                panelUserHeaders.Controls.Add(headerLabel);
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
                    ForeColor = Color.FromArgb(117, 117, 117),
                    Dock = DockStyle.Fill
                };
                labelPanel.Controls.Add(labelText);
                panelStats.Add(labelPanel);

                // 用户数据列
                var valuesPanel = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.LeftToRight,
                    Wrap = false,
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
                        ForeColor = isWinner ? Color.FromArgb(255, 87, 34) : Color.FromArgb(33, 33, 33),
                        Dock = DockStyle.Fill
                    };

                    if (isWinner && userCount > 1)
                    {
                        valueLabel.Text += " 👑";
                    }

                    valuePanel.Controls.Add(valueLabel);
                    valuesPanel.Controls.Add(valuePanel);
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
            this.SuspendLayout();

            // 窗体设置
            this.Text = "🏆 用户对战";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(250, 250, 250);

            // 主容器
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.White
            };

            // 标题
            var titleLabel = new Label
            {
                Text = "📊 用户学习对战",
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Height = 60
            };

            // 副标题
            var subtitleLabel = new Label
            {
                Text = $"共 {_comparisonData.Count} 位玩家参与对战",
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei", 10),
                ForeColor = Color.FromArgb(117, 117, 117),
                Height = 30
            };

            // 内容容器
            panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };

            // 用户标题面板（横向排列用户名）
            panelUserHeaders = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Wrap = false,
                Dock = DockStyle.Top,
                Height = 50,
                Margin = new Padding(0),
                Padding = new Padding(150, 0, 0, 0)
            };

            // 数据面板（垂直排列各项指标）
            panelStats = new List<Panel>();
            panelValues = new List<FlowLayoutPanel>();

            // 关闭按钮
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.White
            };

            var closeButton = new Button
            {
                Text = "关闭",
                Size = new Size(100, 35),
                Location = new Point(280, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10)
            };
            closeButton.Click += (s, e) => this.Close();

            var refreshButton = new Button
            {
                Text = "刷新",
                Size = new Size(100, 35),
                Location = new Point(400, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10)
            };
            refreshButton.Click += (s, e) =>
            {
                panelUserHeaders.Controls.Clear();
                foreach (var p in panelStats) p.Dispose();
                panelStats.Clear();
                foreach (var p in panelValues) p.Dispose();
                panelValues.Clear();
                LoadComparisonData();
            };

            buttonPanel.Controls.Add(closeButton);
            buttonPanel.Controls.Add(refreshButton);

            mainPanel.Controls.Add(panelContent);
            mainPanel.Controls.Add(buttonPanel);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(titleLabel);

            this.Controls.Add(mainPanel);

            this.ResumeLayout(false);
        }

        private Panel panelContent = null!;
        private FlowLayoutPanel panelUserHeaders = null!;
        private List<Panel> panelStats = null!;
        private List<FlowLayoutPanel> panelValues = null!;
    }
}
