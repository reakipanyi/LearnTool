using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls.Learning
{
    /// <summary>
    /// 学习统计视图 - 底部统计信息条和进度条
    /// </summary>
    public class LearningStatsView : UserControl
    {
        #region Controls

        private Panel _panelStatsContainer = null!;

        private Label _labelStudyTime = null!;
        private Label _labelScore = null!;
        private Label _labelTodayCount = null!;
        private Label _labelStreak = null!;
        private Label _labelEncouragement = null!;
        private FlowLayoutPanel _flowLayoutPanelBottomStats;
        #endregion

        #region Public Properties


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



        #endregion

        #region Initialization

        public LearningStatsView()
        {
            InitializeComponent();
        }




        private void InitializeComponent()
        {
            _labelStudyTime = new Label();
            _labelScore = new Label();
            _labelTodayCount = new Label();
            _labelStreak = new Label();
            _labelEncouragement = new Label();
            _flowLayoutPanelBottomStats = new FlowLayoutPanel();
            _flowLayoutPanelBottomStats.SuspendLayout();
            SuspendLayout();
            // 
            // _labelStudyTime
            // 
            _labelStudyTime.Font = new Font("微软雅黑", 8.5F);
            _labelStudyTime.ForeColor = Color.FromArgb(70, 90, 110);
            _labelStudyTime.Location = new Point(18, 2);
            _labelStudyTime.Name = "_labelStudyTime";
            _labelStudyTime.Size = new Size(120, 24);
            _labelStudyTime.TabIndex = 2;
            _labelStudyTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // _labelScore
            // 
            _labelScore.Font = new Font("微软雅黑", 8.5F);
            _labelScore.ForeColor = Color.FromArgb(70, 90, 110);
            _labelScore.Location = new Point(144, 2);
            _labelScore.Name = "_labelScore";
            _labelScore.Size = new Size(120, 24);
            _labelScore.TabIndex = 3;
            _labelScore.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // _labelTodayCount
            // 
            _labelTodayCount.Font = new Font("微软雅黑", 8.5F);
            _labelTodayCount.ForeColor = Color.FromArgb(70, 90, 110);
            _labelTodayCount.Location = new Point(270, 2);
            _labelTodayCount.Name = "_labelTodayCount";
            _labelTodayCount.Size = new Size(120, 24);
            _labelTodayCount.TabIndex = 4;
            _labelTodayCount.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // _labelStreak
            // 
            _labelStreak.Font = new Font("微软雅黑", 8.5F, FontStyle.Bold);
            _labelStreak.ForeColor = Color.FromArgb(255, 152, 0);
            _labelStreak.Location = new Point(396, 2);
            _labelStreak.Name = "_labelStreak";
            _labelStreak.Size = new Size(100, 24);
            _labelStreak.TabIndex = 5;
            _labelStreak.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // _labelEncouragement
            // 
            _labelEncouragement.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _labelEncouragement.Font = new Font("微软雅黑", 9F, FontStyle.Italic);
            _labelEncouragement.ForeColor = Color.FromArgb(100, 150, 180);
            _labelEncouragement.Location = new Point(502, 2);
            _labelEncouragement.Name = "_labelEncouragement";
            _labelEncouragement.Size = new Size(220, 24);
            _labelEncouragement.TabIndex = 6;
            _labelEncouragement.TextAlign = ContentAlignment.MiddleRight;
            // 
            // _flowLayoutPanelBottomStats
            // 
            _flowLayoutPanelBottomStats.BackColor = Color.White;
            _flowLayoutPanelBottomStats.Controls.Add(_labelStudyTime);
            _flowLayoutPanelBottomStats.Controls.Add(_labelScore);
            _flowLayoutPanelBottomStats.Controls.Add(_labelTodayCount);
            _flowLayoutPanelBottomStats.Controls.Add(_labelStreak);
            _flowLayoutPanelBottomStats.Controls.Add(_labelEncouragement);
            _flowLayoutPanelBottomStats.Dock = DockStyle.Fill;
            _flowLayoutPanelBottomStats.Location = new Point(0, 0);
            _flowLayoutPanelBottomStats.Name = "_flowLayoutPanelBottomStats";
            _flowLayoutPanelBottomStats.Padding = new Padding(15, 2, 15, 2);
            _flowLayoutPanelBottomStats.Size = new Size(1095, 40);
            _flowLayoutPanelBottomStats.TabIndex = 2;
            _flowLayoutPanelBottomStats.WrapContents = false;
            // 
            // LearningStatsView
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(_flowLayoutPanelBottomStats);
            Name = "LearningStatsView";
            Size = new Size(1095, 40);
            _flowLayoutPanelBottomStats.ResumeLayout(false);
            ResumeLayout(false);
        }


        #endregion

        #region Public Methods


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