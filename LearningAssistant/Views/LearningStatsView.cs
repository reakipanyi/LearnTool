using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LearningAssistant.Views
{
    /// <summary>
    /// 学习统计视图 - 底部统计信息条
    /// </summary>
    public class LearningStatsView : UserControl
    {
        #region Controls

        private Label _labelStatistics = null!;

        #endregion

        #region Public Controls

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label LabelStatistics => _labelStatistics;

        #endregion

        #region Initialization

        public LearningStatsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _labelStatistics = new Label();

            SuspendLayout();

            // 
            // _labelStatistics
            // 
            _labelStatistics.Dock = DockStyle.Fill;
            _labelStatistics.Font = new Font("微软雅黑", 11F);
            _labelStatistics.ForeColor = Color.FromArgb(80, 100, 120);
            _labelStatistics.Location = new Point(0, 0);
            _labelStatistics.Name = "_labelStatistics";
            _labelStatistics.Size = new Size(1089, 32);
            _labelStatistics.TabIndex = 3;
            _labelStatistics.TextAlign = ContentAlignment.MiddleLeft;

            Controls.Add(_labelStatistics);

            ResumeLayout(false);
        }

        #endregion

        #region Public API

        /// <summary>应用主题色</summary>
        public void ApplyTheme(Color foreColor, Color backColor)
        {
            _labelStatistics.ForeColor = foreColor;
            _labelStatistics.BackColor = backColor;
        }

        /// <summary>设置统计信息文本</summary>
        public void SetStatistics(string text) => _labelStatistics.Text = text;

        #endregion
    }
}
