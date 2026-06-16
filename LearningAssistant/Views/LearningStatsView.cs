using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using LearningAssistant.Views.UI;

namespace LearningAssistant.Views
{
    /// <summary>
    /// 学习统计视图 - 底部统计信息条、进度条和记忆曲线图表
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
        private ChartControl _chartMemoryCurve = null!;
        private Label _labelAISummary = null!;

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
        public ChartControl ChartMemoryCurve => _chartMemoryCurve;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label LabelAISummary => _labelAISummary;

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
            _chartMemoryCurve = new ChartControl();
            _labelAISummary = new Label();

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
            // _chartMemoryCurve
            //
            _chartMemoryCurve.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _chartMemoryCurve.BackColor = Color.FromArgb(250, 250, 255);
            _chartMemoryCurve.ChartType = ChartType.Line;
            _chartMemoryCurve.Font = new Font("微软雅黑", 9F);
            _chartMemoryCurve.ForeColor = Color.FromArgb(80, 100, 120);
            _chartMemoryCurve.Location = new Point(15, 40);
            _chartMemoryCurve.Name = "chartMemoryCurve";
            _chartMemoryCurve.Size = new Size(1050, 350);
            _chartMemoryCurve.TabIndex = 7;
            _chartMemoryCurve.Title = "记忆曲线 - 近7天学习进度";

            //
            // _labelAISummary
            //
            _labelAISummary.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _labelAISummary.Font = new Font("微软雅黑", 10F);
            _labelAISummary.ForeColor = Color.FromArgb(70, 90, 110);
            _labelAISummary.Location = new Point(15, 400);
            _labelAISummary.Name = "labelAISummary";
            _labelAISummary.Size = new Size(1050, 100);
            _labelAISummary.TabIndex = 8;
            _labelAISummary.Text = "📝 AI学习总结：暂无数据";
            _labelAISummary.AutoEllipsis = true;

            //
            // _panelStatsContainer
            //
            _panelStatsContainer.Controls.Add(_labelAISummary);
            _panelStatsContainer.Controls.Add(_chartMemoryCurve);
            _panelStatsContainer.Controls.Add(_labelEncouragement);
            _panelStatsContainer.Controls.Add(_labelStreak);
            _panelStatsContainer.Controls.Add(_labelTodayCount);
            _panelStatsContainer.Controls.Add(_labelScore);
            _panelStatsContainer.Controls.Add(_labelStudyTime);
            _panelStatsContainer.Controls.Add(_labelStatistics);
            _panelStatsContainer.Controls.Add(_progressBar);
            _panelStatsContainer.ResumeLayout(false);
            ResumeLayout(false);
        }

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
        /// 设置记忆曲线数据
        /// </summary>
        public void SetMemoryCurveData(double[] values, string[] labels, Color[] colors)
        {
            if (_chartMemoryCurve != null && values != null && values.Length > 0)
            {
                _chartMemoryCurve.SetData(values, labels, colors);
            }
        }

        /// <summary>
        /// 设置AI学习总结
        /// </summary>
        public void SetAISummary(string summary)
        {
            if (_labelAISummary != null)
            {
                _labelAISummary.Text = $"📝 AI学习总结：{summary}";
            }
        }

        #endregion
    }
}