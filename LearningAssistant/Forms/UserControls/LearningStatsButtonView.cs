using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls
{
    public class LearningStatsButtonView : UserControl
    {

        private FlowLayoutPanel _flowLayoutPanelBottomStats = null!;
        private Button _buttonAchievements = null!;
        private Button _buttonChallenges = null!;
        private Button _buttonReview = null!;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonAchievements => _buttonAchievements;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonChallenges => _buttonChallenges;


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonReview => _buttonReview;
        #region Events


        public event EventHandler? AchievementsClicked;
        public event EventHandler? ChallengesClicked;
        public event EventHandler? ReviewClicked;

        #endregion

        public LearningStatsButtonView()
        {
            InitializeComponent();
        }


        private void InitializeComponent()
        {
            _buttonAchievements = new Button();
            _buttonChallenges = new Button();
            _buttonReview = new Button();
            _flowLayoutPanelBottomStats = new FlowLayoutPanel();
            _flowLayoutPanelBottomStats.SuspendLayout();
            SuspendLayout();
            // 
            // _buttonAchievements
            // 
            _buttonAchievements.Cursor = Cursors.Hand;
            _buttonAchievements.FlatAppearance.BorderSize = 0;
            _buttonAchievements.FlatStyle = FlatStyle.Flat;
            _buttonAchievements.Font = new Font("微软雅黑", 9F);
            _buttonAchievements.ForeColor = Color.FromArgb(255, 152, 0);
            _buttonAchievements.Location = new Point(3, 3);
            _buttonAchievements.Name = "_buttonAchievements";
            _buttonAchievements.Size = new Size(70, 30);
            _buttonAchievements.TabIndex = 0;
            _buttonAchievements.Text = "🏆 成就";
            _buttonAchievements.UseVisualStyleBackColor = false;
            _buttonAchievements.Click += ButtonAchievements_Click;
            // 
            // _buttonChallenges
            // 
            _buttonChallenges.Cursor = Cursors.Hand;
            _buttonChallenges.FlatAppearance.BorderSize = 0;
            _buttonChallenges.FlatStyle = FlatStyle.Flat;
            _buttonChallenges.Font = new Font("微软雅黑", 9F);
            _buttonChallenges.ForeColor = Color.FromArgb(76, 175, 80);
            _buttonChallenges.Location = new Point(79, 3);
            _buttonChallenges.Name = "_buttonChallenges";
            _buttonChallenges.Size = new Size(70, 30);
            _buttonChallenges.TabIndex = 1;
            _buttonChallenges.Text = "🎯 挑战";
            _buttonChallenges.UseVisualStyleBackColor = false;
            _buttonChallenges.Click += ButtonChallenges_Click;
            // 
            // _buttonReview
            // 
            _buttonReview.Cursor = Cursors.Hand;
            _buttonReview.FlatAppearance.BorderSize = 0;
            _buttonReview.FlatStyle = FlatStyle.Flat;
            _buttonReview.Font = new Font("微软雅黑", 9F);
            _buttonReview.ForeColor = Color.FromArgb(33, 150, 243);
            _buttonReview.Location = new Point(155, 3);
            _buttonReview.Name = "_buttonReview";
            _buttonReview.Size = new Size(80, 30);
            _buttonReview.TabIndex = 2;
            _buttonReview.Text = "🔔 复习";
            _buttonReview.UseVisualStyleBackColor = false;
            _buttonReview.Click += ButtonReview_Click;
            // 
            // _flowLayoutPanelBottomStats
            // 
            _flowLayoutPanelBottomStats.Controls.Add(_buttonAchievements);
            _flowLayoutPanelBottomStats.Controls.Add(_buttonChallenges);
            _flowLayoutPanelBottomStats.Controls.Add(_buttonReview);
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

        private void ButtonAchievements_Click(object? sender, EventArgs e) => AchievementsClicked?.Invoke(sender, e);
        private void ButtonChallenges_Click(object? sender, EventArgs e) => ChallengesClicked?.Invoke(sender, e);
        private void ButtonReview_Click(object? sender, EventArgs e) => ReviewClicked?.Invoke(sender, e);


    }

}