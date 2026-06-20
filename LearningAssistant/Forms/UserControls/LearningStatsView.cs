using LearningAssistant.Common;
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
        private Button _buttonPK = null!;
        private Button _buttonReview = null!;
        private Label _labelBadges = null!;
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
        public Button ButtonPK => _buttonPK;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonReview => _buttonReview;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label LabelBadges => _labelBadges;

        #endregion

        #region Events

        public event EventHandler? AchievementsClicked;
        public event EventHandler? ChallengesClicked;
        public event EventHandler? PKClicked;
        public event EventHandler? ReviewClicked;

        #endregion

        #region Initialization

        public LearningStatsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _panelStatsContainer = new Panel();
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
            _buttonPK = new Button();
            _buttonReview = new Button();
            _labelBadges = new Label();

            _panelStatsContainer.SuspendLayout();
            SuspendLayout();

            // 
            // _panelStatsContainer
            // 
            _panelStatsContainer.Dock = DockStyle.Fill;
            _panelStatsContainer.Location = new Point(0, 0);
            _panelStatsContainer.Name = "panelStatsContainer";
            _panelStatsContainer.Size = new Size(1089, 836);
            _panelStatsContainer.TabIndex = 0;

            // 
            // _progressBar
            // 
            _progressBar.BackColor = Color.FromArgb(240, 240, 240);
            _progressBar.Dock = DockStyle.Bottom;
            _progressBar.ForeColor = Color.FromArgb(255, 140, 0);
            _progressBar.Location = new Point(0, 800);
            _progressBar.Name = "progressBar";
            _progressBar.Size = new Size(1089, 36);
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.TabIndex = 0;

            // 
            // _labelStatistics
            // 
            _labelStatistics.Dock = DockStyle.Top;
            _labelStatistics.Font = new Font("微软雅黑", 11F);
            _labelStatistics.ForeColor = Color.FromArgb(80, 100, 120);
            _labelStatistics.Location = new Point(0, 0);
            _labelStatistics.Name = "labelStatistics";
            _labelStatistics.Size = new Size(1089, 32);
            _labelStatistics.TabIndex = 1;
            _labelStatistics.TextAlign = ContentAlignment.MiddleCenter;

            // 
            // _flowLayoutPanelFeatures
            // 
            _flowLayoutPanelFeatures.Dock = DockStyle.Top;
            _flowLayoutPanelFeatures.Height = 38;
            _flowLayoutPanelFeatures.Name = "flowLayoutPanelFeatures";
            _flowLayoutPanelFeatures.Padding = new Padding(10, 3, 10, 3);
            _flowLayoutPanelFeatures.TabIndex = 7;
            _flowLayoutPanelFeatures.WrapContents = false;
            _flowLayoutPanelFeatures.Controls.Add(_buttonAchievements);
            _flowLayoutPanelFeatures.Controls.Add(_buttonChallenges);
            _flowLayoutPanelFeatures.Controls.Add(_buttonPK);
            _flowLayoutPanelFeatures.Controls.Add(_buttonReview);
            _flowLayoutPanelFeatures.Controls.Add(_labelBadges);

            // 
            // _buttonAchievements
            // 
            _buttonAchievements.FlatStyle = FlatStyle.Flat;
            _buttonAchievements.FlatAppearance.BorderSize = 0;
            _buttonAchievements.Font = new Font("微软雅黑", 9F);
            _buttonAchievements.ForeColor = Color.FromArgb(255, 152, 0);
            _buttonAchievements.Size = new Size(70, 28);
            _buttonAchievements.Text = "🏆 成就";
            _buttonAchievements.UseVisualStyleBackColor = false;
            _buttonAchievements.Click += ButtonAchievements_Click;

            // 
            // _buttonChallenges
            // 
            _buttonChallenges.FlatStyle = FlatStyle.Flat;
            _buttonChallenges.FlatAppearance.BorderSize = 0;
            _buttonChallenges.Font = new Font("微软雅黑", 9F);
            _buttonChallenges.ForeColor = Color.FromArgb(76, 175, 80);
            _buttonChallenges.Size = new Size(70, 28);
            _buttonChallenges.Text = "🎯 挑战";
            _buttonChallenges.UseVisualStyleBackColor = false;
            _buttonChallenges.Click += ButtonChallenges_Click;

            // 
            // _buttonPK
            // 
            _buttonPK.FlatStyle = FlatStyle.Flat;
            _buttonPK.FlatAppearance.BorderSize = 0;
            _buttonPK.Font = new Font("微软雅黑", 9F);
            _buttonPK.ForeColor = Color.FromArgb(244, 67, 54);
            _buttonPK.Size = new Size(70, 28);
            _buttonPK.Text = "⚔️ PK";
            _buttonPK.UseVisualStyleBackColor = false;
            _buttonPK.Click += ButtonPK_Click;

            // 
            // _buttonReview
            // 
            _buttonReview.FlatStyle = FlatStyle.Flat;
            _buttonReview.FlatAppearance.BorderSize = 0;
            _buttonReview.Font = new Font("微软雅黑", 9F);
            _buttonReview.ForeColor = Color.FromArgb(33, 150, 243);
            _buttonReview.Size = new Size(80, 28);
            _buttonReview.Text = "🔔 复习";
            _buttonReview.UseVisualStyleBackColor = false;
            _buttonReview.Click += ButtonReview_Click;

            // 
            // _labelBadges
            // 
            _labelBadges.AutoSize = true;
            _labelBadges.Font = new Font("Segoe UI Emoji", 16F);
            _labelBadges.Margin = new Padding(20, 0, 0, 0);
            _labelBadges.Name = "labelBadges";
            _labelBadges.Size = new Size(400, 32);
            _labelBadges.Text = "🏅🎖️";
            _labelBadges.TextAlign = ContentAlignment.MiddleRight;

            // 
            // _labelStudyTime
            // 
            _labelStudyTime.Font = new Font("微软雅黑", 9F);
            _labelStudyTime.ForeColor = Color.FromArgb(70, 90, 110);
            _labelStudyTime.Location = new Point(15, 750);
            _labelStudyTime.Name = "labelStudyTime";
            _labelStudyTime.Size = new Size(120, 23);
            _labelStudyTime.TabIndex = 2;

            // 
            // _labelScore
            // 
            _labelScore.Font = new Font("微软雅黑", 9F);
            _labelScore.ForeColor = Color.FromArgb(70, 90, 110);
            _labelScore.Location = new Point(145, 750);
            _labelScore.Name = "labelScore";
            _labelScore.Size = new Size(120, 23);
            _labelScore.TabIndex = 3;

            // 
            // _labelTodayCount
            // 
            _labelTodayCount.Font = new Font("微软雅黑", 9F);
            _labelTodayCount.ForeColor = Color.FromArgb(70, 90, 110);
            _labelTodayCount.Location = new Point(275, 750);
            _labelTodayCount.Name = "labelTodayCount";
            _labelTodayCount.Size = new Size(120, 23);
            _labelTodayCount.TabIndex = 4;

            // 
            // _labelStreak
            // 
            _labelStreak.Font = new Font("微软雅黑", 9F);
            _labelStreak.ForeColor = Color.FromArgb(255, 140, 0);
            _labelStreak.Location = new Point(405, 750);
            _labelStreak.Name = "labelStreak";
            _labelStreak.Size = new Size(100, 23);
            _labelStreak.TabIndex = 5;

            // 
            // _labelEncouragement
            // 
            _labelEncouragement.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _labelEncouragement.Font = new Font("微软雅黑", 10F, FontStyle.Italic);
            _labelEncouragement.ForeColor = Color.FromArgb(100, 150, 180);
            _labelEncouragement.Location = new Point(850, 750);
            _labelEncouragement.Name = "labelEncouragement";
            _labelEncouragement.Size = new Size(220, 23);
            _labelEncouragement.TabIndex = 6;
            _labelEncouragement.TextAlign = ContentAlignment.MiddleRight;

            // 
            // _flowLayoutPanelBottomStats
            // 
            _flowLayoutPanelBottomStats = new FlowLayoutPanel();
            _flowLayoutPanelBottomStats.Dock = DockStyle.Bottom;
            _flowLayoutPanelBottomStats.Height = 30;
            _flowLayoutPanelBottomStats.Name = "flowLayoutPanelBottomStats";
            _flowLayoutPanelBottomStats.Padding = new Padding(15, 3, 15, 3);
            _flowLayoutPanelBottomStats.WrapContents = false;
            _flowLayoutPanelBottomStats.Controls.Add(_labelStudyTime);
            _flowLayoutPanelBottomStats.Controls.Add(_labelScore);
            _flowLayoutPanelBottomStats.Controls.Add(_labelTodayCount);
            _flowLayoutPanelBottomStats.Controls.Add(_labelStreak);
            _flowLayoutPanelBottomStats.Controls.Add(_labelEncouragement);

            //
            // LearningStatsView
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(_panelStatsContainer);
            Controls.Add(_labelStatistics);
            Controls.Add(_flowLayoutPanelBottomStats);
            Controls.Add(_flowLayoutPanelFeatures);
            Controls.Add(_progressBar);
            Name = "LearningStatsView";
            Size = new Size(1095, 838);

            _panelStatsContainer.ResumeLayout(false);
            ResumeLayout(false);
        }

        private void ButtonAchievements_Click(object? sender, EventArgs e) => AchievementsClicked?.Invoke(sender, e);
        private void ButtonChallenges_Click(object? sender, EventArgs e) => ChallengesClicked?.Invoke(sender, e);
        private void ButtonPK_Click(object? sender, EventArgs e) => PKClicked?.Invoke(sender, e);
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

        #endregion
    }
}