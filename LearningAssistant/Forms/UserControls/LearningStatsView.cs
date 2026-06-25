using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls
{
    /// <summary>
    /// 学习统计视图 - 底部统计信息条和进度条
    /// </summary>
    public class LearningStatsView : UserControl
    {
        #region Controls

        private Panel _panelStatsContainer = null!;
        private Label _labelStatistics = null!;
        private ProgressBar _progressBar = null!;
        private Label _labelStudyTime = null!;
        private Label _labelScore = null!;
        private Label _labelTodayCount = null!;
        private Label _labelStreak = null!;
        private Label _labelEncouragement = null!;
        private FlowLayoutPanel _flowLayoutPanelFeatures = null!;
        private Button _buttonAchievements = null!;
        private Button _buttonChallenges = null!;
        private Button _buttonReview = null!;
        private Label _labelBadges = null!;
        private LevelBadge _levelBadge = null!;
        private FlowLayoutPanel _flowLayoutPanelBottomStats = null!;

        #endregion

        #region Public Properties

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label LabelStatistics => _labelStatistics;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ProgressBar ProgressBar => _progressBar;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label LabelStudyTime => _labelStudyTime;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label LabelScore => _labelScore;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label LabelTodayCount => _labelTodayCount;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label LabelStreak => _labelStreak;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label LabelEncouragement => _labelEncouragement;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Panel PanelStatsContainer => _panelStatsContainer;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FlowLayoutPanel FlowLayoutPanelFeatures => _flowLayoutPanelFeatures;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonAchievements => _buttonAchievements;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonChallenges => _buttonChallenges;


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonReview => _buttonReview;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label LabelBadges => _labelBadges;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public LevelBadge LevelBadge => _levelBadge;

        #endregion

        #region Events

        public event EventHandler? AchievementsClicked;
        public event EventHandler? ChallengesClicked;
        public event EventHandler? ReviewClicked;

        #endregion

        #region Initialization

        public LearningStatsView()
        {
            InitializeComponent();
            ApplyButtonStyles();
        }

        private void ApplyButtonStyles()
        {
            ApplyFeatureButtonStyle(_buttonAchievements);
            ApplyFeatureButtonStyle(_buttonChallenges);
            ApplyFeatureButtonStyle(_buttonReview);
        }

        private void ApplyFeatureButtonStyle(Button button)
        {
            button.FlatAppearance.BorderSize = 0;
            button.FlatStyle = FlatStyle.Flat;
            button.Cursor = Cursors.Hand;

            button.MouseEnter += (sender, e) =>
            {
                if (sender is Button btn)
                {
                    btn.BackColor = Color.FromArgb(240, 240, 240);
                }
            };

            button.MouseLeave += (sender, e) =>
            {
                if (sender is Button btn)
                {
                    btn.BackColor = Color.Transparent;
                }
            };
        }

        private void InitializeComponent()
        {
            _panelStatsContainer = new Panel();
            _levelBadge = new LevelBadge();
            _labelStatistics = new Label();
            _progressBar = new ProgressBar();
            _labelStudyTime = new Label();
            _labelScore = new Label();
            _labelTodayCount = new Label();
            _labelStreak = new Label();
            _labelEncouragement = new Label();
            _flowLayoutPanelFeatures = new FlowLayoutPanel();
            _buttonAchievements = new Button();
            _buttonChallenges = new Button();
            _buttonReview = new Button();
            _labelBadges = new Label();
            _flowLayoutPanelBottomStats = new FlowLayoutPanel();

            _panelStatsContainer.SuspendLayout();
            _flowLayoutPanelFeatures.SuspendLayout();
            _flowLayoutPanelBottomStats.SuspendLayout();
            SuspendLayout();

            _panelStatsContainer.Controls.Add(_levelBadge);
            _panelStatsContainer.Dock = DockStyle.Fill;
            _panelStatsContainer.Location = new Point(0, 98);
            _panelStatsContainer.Name = "_panelStatsContainer";
            _panelStatsContainer.Size = new Size(1095, 777);
            _panelStatsContainer.TabIndex = 0;
            _panelStatsContainer.BackColor = Color.White;

            _levelBadge.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _levelBadge.BackColor = Color.Transparent;
            _levelBadge.Location = new Point(966, 601);
            _levelBadge.Name = "_levelBadge";
            _levelBadge.Size = new Size(100, 136);
            _levelBadge.TabIndex = 8;

            _labelStatistics.Dock = DockStyle.Top;
            _labelStatistics.Font = new Font("微软雅黑", 11F);
            _labelStatistics.ForeColor = Color.FromArgb(80, 100, 120);
            _labelStatistics.Location = new Point(0, 43);
            _labelStatistics.Name = "_labelStatistics";
            _labelStatistics.Size = new Size(1095, 55);
            _labelStatistics.TabIndex = 1;
            _labelStatistics.TextAlign = ContentAlignment.MiddleCenter;

            _progressBar.BackColor = Color.FromArgb(240, 240, 240);
            _progressBar.Dock = DockStyle.Bottom;
            _progressBar.ForeColor = Color.FromArgb(76, 175, 80);
            _progressBar.Location = new Point(0, 909);
            _progressBar.Name = "_progressBar";
            _progressBar.Size = new Size(1095, 8);
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.TabIndex = 0;

            _labelStudyTime.Font = new Font("微软雅黑", 9F);
            _labelStudyTime.ForeColor = Color.FromArgb(70, 90, 110);
            _labelStudyTime.Location = new Point(18, 3);
            _labelStudyTime.Name = "_labelStudyTime";
            _labelStudyTime.Size = new Size(120, 26);
            _labelStudyTime.TabIndex = 2;
            _labelStudyTime.TextAlign = ContentAlignment.MiddleCenter;

            _labelScore.Font = new Font("微软雅黑", 9F);
            _labelScore.ForeColor = Color.FromArgb(70, 90, 110);
            _labelScore.Location = new Point(144, 3);
            _labelScore.Name = "_labelScore";
            _labelScore.Size = new Size(120, 26);
            _labelScore.TabIndex = 3;
            _labelScore.TextAlign = ContentAlignment.MiddleCenter;

            _labelTodayCount.Font = new Font("微软雅黑", 9F);
            _labelTodayCount.ForeColor = Color.FromArgb(70, 90, 110);
            _labelTodayCount.Location = new Point(270, 3);
            _labelTodayCount.Name = "_labelTodayCount";
            _labelTodayCount.Size = new Size(120, 26);
            _labelTodayCount.TabIndex = 4;
            _labelTodayCount.TextAlign = ContentAlignment.MiddleCenter;

            _labelStreak.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _labelStreak.ForeColor = Color.FromArgb(255, 152, 0);
            _labelStreak.Location = new Point(396, 3);
            _labelStreak.Name = "_labelStreak";
            _labelStreak.Size = new Size(100, 26);
            _labelStreak.TabIndex = 5;
            _labelStreak.TextAlign = ContentAlignment.MiddleCenter;

            _labelEncouragement.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _labelEncouragement.Font = new Font("微软雅黑", 10F, FontStyle.Italic);
            _labelEncouragement.ForeColor = Color.FromArgb(100, 150, 180);
            _labelEncouragement.Location = new Point(502, 3);
            _labelEncouragement.Name = "_labelEncouragement";
            _labelEncouragement.Size = new Size(220, 26);
            _labelEncouragement.TabIndex = 6;
            _labelEncouragement.TextAlign = ContentAlignment.MiddleRight;

            _flowLayoutPanelFeatures.Controls.Add(_buttonAchievements);
            _flowLayoutPanelFeatures.Controls.Add(_buttonChallenges);
            _flowLayoutPanelFeatures.Controls.Add(_buttonReview);
            _flowLayoutPanelFeatures.Controls.Add(_labelBadges);
            _flowLayoutPanelFeatures.Dock = DockStyle.Top;
            _flowLayoutPanelFeatures.Location = new Point(0, 0);
            _flowLayoutPanelFeatures.Name = "_flowLayoutPanelFeatures";
            _flowLayoutPanelFeatures.Padding = new Padding(10, 3, 10, 3);
            _flowLayoutPanelFeatures.Size = new Size(1095, 43);
            _flowLayoutPanelFeatures.TabIndex = 7;
            _flowLayoutPanelFeatures.WrapContents = false;
            _flowLayoutPanelFeatures.BackColor = Color.White;

            _buttonAchievements.FlatAppearance.BorderSize = 0;
            _buttonAchievements.FlatStyle = FlatStyle.Flat;
            _buttonAchievements.Font = new Font("微软雅黑", 9F);
            _buttonAchievements.ForeColor = Color.FromArgb(255, 152, 0);
            _buttonAchievements.Location = new Point(13, 6);
            _buttonAchievements.Name = "_buttonAchievements";
            _buttonAchievements.Size = new Size(70, 32);
            _buttonAchievements.TabIndex = 0;
            _buttonAchievements.Text = "🏆 成就";
            _buttonAchievements.UseVisualStyleBackColor = false;
            _buttonAchievements.Click += ButtonAchievements_Click;

            _buttonChallenges.FlatAppearance.BorderSize = 0;
            _buttonChallenges.FlatStyle = FlatStyle.Flat;
            _buttonChallenges.Font = new Font("微软雅黑", 9F);
            _buttonChallenges.ForeColor = Color.FromArgb(76, 175, 80);
            _buttonChallenges.Location = new Point(89, 6);
            _buttonChallenges.Name = "_buttonChallenges";
            _buttonChallenges.Size = new Size(70, 32);
            _buttonChallenges.TabIndex = 1;
            _buttonChallenges.Text = "🎯 挑战";
            _buttonChallenges.UseVisualStyleBackColor = false;
            _buttonChallenges.Click += ButtonChallenges_Click;

            _buttonReview.FlatAppearance.BorderSize = 0;
            _buttonReview.FlatStyle = FlatStyle.Flat;
            _buttonReview.Font = new Font("微软雅黑", 9F);
            _buttonReview.ForeColor = Color.FromArgb(33, 150, 243);
            _buttonReview.Location = new Point(165, 6);
            _buttonReview.Name = "_buttonReview";
            _buttonReview.Size = new Size(80, 32);
            _buttonReview.TabIndex = 3;
            _buttonReview.Text = "🔔 复习";
            _buttonReview.UseVisualStyleBackColor = false;
            _buttonReview.Click += ButtonReview_Click;

            _labelBadges.AutoSize = true;
            _labelBadges.Font = new Font("Segoe UI Emoji", 16F);
            _labelBadges.Location = new Point(268, 3);
            _labelBadges.Margin = new Padding(20, 0, 0, 0);
            _labelBadges.Name = "_labelBadges";
            _labelBadges.Size = new Size(66, 30);
            _labelBadges.TabIndex = 4;
            _labelBadges.Text = "🏅🎖️";
            _labelBadges.TextAlign = ContentAlignment.MiddleRight;

            _flowLayoutPanelBottomStats.Controls.Add(_labelStudyTime);
            _flowLayoutPanelBottomStats.Controls.Add(_labelScore);
            _flowLayoutPanelBottomStats.Controls.Add(_labelTodayCount);
            _flowLayoutPanelBottomStats.Controls.Add(_labelStreak);
            _flowLayoutPanelBottomStats.Controls.Add(_labelEncouragement);
            _flowLayoutPanelBottomStats.Dock = DockStyle.Bottom;
            _flowLayoutPanelBottomStats.Location = new Point(0, 875);
            _flowLayoutPanelBottomStats.Name = "_flowLayoutPanelBottomStats";
            _flowLayoutPanelBottomStats.Padding = new Padding(15, 3, 15, 3);
            _flowLayoutPanelBottomStats.Size = new Size(1095, 34);
            _flowLayoutPanelBottomStats.TabIndex = 2;
            _flowLayoutPanelBottomStats.WrapContents = false;
            _flowLayoutPanelBottomStats.BackColor = Color.White;

            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(_panelStatsContainer);
            Controls.Add(_labelStatistics);
            Controls.Add(_flowLayoutPanelBottomStats);
            Controls.Add(_flowLayoutPanelFeatures);
            Controls.Add(_progressBar);
            Name = "LearningStatsView";
            Size = new Size(1095, 950);
            _panelStatsContainer.ResumeLayout(false);
            _flowLayoutPanelFeatures.ResumeLayout(false);
            _flowLayoutPanelFeatures.PerformLayout();
            _flowLayoutPanelBottomStats.ResumeLayout(false);
            ResumeLayout(false);
        }

        private void ButtonAchievements_Click(object? sender, EventArgs e) => AchievementsClicked?.Invoke(sender, e);
        private void ButtonChallenges_Click(object? sender, EventArgs e) => ChallengesClicked?.Invoke(sender, e);
        private void ButtonReview_Click(object? sender, EventArgs e) => ReviewClicked?.Invoke(sender, e);

        #endregion

        #region Public Methods

        /// <summary>
        /// 设置统计信息文本
        /// </summary>
        public void SetStatistics(string text)
        {
            _labelStatistics.Text = text;
        }

        /// <summary>
        /// 设置进度条值
        /// </summary>
        public void SetProgressValue(int value)
        {
            if (value >= 0 && value <= _progressBar.Maximum)
                _progressBar.Value = value;
        }

        /// <summary>
        /// 设置进度条最大值
        /// </summary>
        public void SetProgressMax(int max)
        {
            if (max > 0)
                _progressBar.Maximum = max;
        }

        /// <summary>
        /// 应用主题颜色
        /// </summary>
        public void ApplyTheme(Color foreColor)
        {
            _labelStatistics.ForeColor = foreColor;
        }

        /// <summary>
        /// 设置学习时间文本
        /// </summary>
        public void SetStudyTime(string text)
        {
            _labelStudyTime.Text = text;
        }

        /// <summary>
        /// 设置分数文本
        /// </summary>
        public void SetScore(string text)
        {
            _labelScore.Text = text;
        }

        /// <summary>
        /// 设置今日学习数文本
        /// </summary>
        public void SetTodayCount(string text)
        {
            _labelTodayCount.Text = text;
        }

        /// <summary>
        /// 设置连续学习天数文本
        /// </summary>
        public void SetStreak(string text)
        {
            _labelStreak.Text = text;
        }

        /// <summary>
        /// 设置鼓励语
        /// </summary>
        public void SetEncouragement(string text)
        {
            _labelEncouragement.Text = text;
        }

        /// <summary>
        /// 更新等级信息
        /// </summary>
        public void UpdateLevel(int level, int currentXP, int xpToNextLevel, string levelTitle)
        {
            if (_levelBadge == null) return;
            _levelBadge.Level = level;
            _levelBadge.LevelTitle = levelTitle;
            _levelBadge.SetXP(currentXP, xpToNextLevel);
        }

        /// <summary>
        /// 触发升级动画
        /// </summary>
        public void TriggerLevelUp(int newLevel, string newTitle)
        {
            if (_levelBadge == null) return;
            _levelBadge.TriggerLevelUp(newLevel, newTitle);
        }

        #endregion
    }
}