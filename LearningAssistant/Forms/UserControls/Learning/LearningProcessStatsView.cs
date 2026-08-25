using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls.Learning
{

    /// <summary>
    /// 学习统计视图 - 底部统计信息条和进度条
    /// </summary>
    public class LearningProcessStatsView : UserControl
    {
        #region Controls


        private Label _labelStatistics = null!;
        private ProgressBar _progressBar = null!;


        #endregion

        #region Public Properties

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label LabelStatistics => _labelStatistics;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ProgressBar ProgressBar => _progressBar;



        #endregion

        #region Initialization

        public LearningProcessStatsView()
        {
            InitializeComponent();
        }




        private void InitializeComponent()
        {
            _labelStatistics = new Label();
            _progressBar = new ProgressBar();
            SuspendLayout();
            // 
            // _labelStatistics
            // 
            _labelStatistics.Dock = DockStyle.Bottom;
            _labelStatistics.Font = new Font("微软雅黑", 10F);
            _labelStatistics.ForeColor = Color.FromArgb(80, 100, 120);
            _labelStatistics.Location = new Point(0, 47);
            _labelStatistics.Name = "_labelStatistics";
            _labelStatistics.Size = new Size(1814, 17);
            _labelStatistics.TabIndex = 1;
            _labelStatistics.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // _progressBar
            // 
            _progressBar.BackColor = Color.FromArgb(240, 240, 240);
            _progressBar.Dock = DockStyle.Top;
            _progressBar.ForeColor = Color.FromArgb(76, 175, 80);
            _progressBar.Location = new Point(0, 0);
            _progressBar.Name = "_progressBar";
            _progressBar.Size = new Size(1814, 26);
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.TabIndex = 0;
            // 
            // LearningProcessStatsView
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(_labelStatistics);
            Controls.Add(_progressBar);
            Name = "LearningProcessStatsView";
            Size = new Size(1814, 64);
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

        #endregion
    }

}