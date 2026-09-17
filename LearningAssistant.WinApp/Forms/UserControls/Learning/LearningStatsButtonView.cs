using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls.Learning
{
    public class LearningStatsButtonView : UserControl
    {
        private FlowLayoutPanel _flowLayoutPanelBottomStats = null!;
        private Label _labelUser = null!;
        private ComboBox _comboBoxUser = null!;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ComboBox ComboBoxUser => _comboBoxUser;

        public event EventHandler? UserChanged;

        public LearningStatsButtonView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _labelUser = new Label();
            _comboBoxUser = new ComboBox();
            _flowLayoutPanelBottomStats = new FlowLayoutPanel();
            _flowLayoutPanelBottomStats.SuspendLayout();
            SuspendLayout();
            // 
            // _labelUser
            // 
            _labelUser.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _labelUser.ForeColor = Color.FromArgb(60, 80, 100);
            _labelUser.Location = new Point(3, 0);
            _labelUser.Name = "_labelUser";
            _labelUser.Size = new Size(50, 24);
            _labelUser.TabIndex = 0;
            _labelUser.Text = "用户:";
            _labelUser.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _comboBoxUser
            // 
            _comboBoxUser.DropDownStyle = ComboBoxStyle.DropDownList;
            _comboBoxUser.Font = new Font("微软雅黑", 9F);
            _comboBoxUser.FormattingEnabled = true;
            _comboBoxUser.Location = new Point(59, 3);
            _comboBoxUser.Name = "_comboBoxUser";
            _comboBoxUser.Size = new Size(150, 25);
            _comboBoxUser.TabIndex = 0;
            _comboBoxUser.SelectedIndexChanged += ComboBoxUser_SelectedIndexChanged;
            // 
            // _flowLayoutPanelBottomStats
            // 
            _flowLayoutPanelBottomStats.Controls.Add(_labelUser);
            _flowLayoutPanelBottomStats.Controls.Add(_comboBoxUser);
            _flowLayoutPanelBottomStats.Dock = DockStyle.Fill;
            _flowLayoutPanelBottomStats.Location = new Point(10, 4);
            _flowLayoutPanelBottomStats.Name = "_flowLayoutPanelBottomStats";
            _flowLayoutPanelBottomStats.Size = new Size(443, 32);
            _flowLayoutPanelBottomStats.TabIndex = 0;
            // 
            // LearningStatsButtonView
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(250, 245, 235);
            Controls.Add(_flowLayoutPanelBottomStats);
            Name = "LearningStatsButtonView";
            Padding = new Padding(10, 4, 10, 4);
            Size = new Size(463, 40);
            _flowLayoutPanelBottomStats.ResumeLayout(false);
            ResumeLayout(false);
        }

        private void ComboBoxUser_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UserChanged?.Invoke(sender, e);
        }
    }
}