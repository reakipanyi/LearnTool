using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls.Learning
{
    /// <summary>
    /// 学习按钮视图 - 底部操作按钮区
    /// </summary>
    public class LearningButtonsView : UserControl
    {
        #region Controls

        private FlowLayoutPanel _buttonsPanel = null!;
        private Button _buttonKnown = null!;
        private Button _buttonUnknown = null!;
        private Button _buttonFavorite = null!;
        private Button _buttonExit = null!;
        private Panel separator1;
        private Panel separator2;
        private Panel separator3;
        private Button _buttonPrevious = null!;
        private Button _buttonNext = null!;
        private Button _buttonEdit = null!;
        private Button _buttonStats = null!;
        private Button _buttonChallenge = null!;
        private Button _buttonAchievement = null!;
        private Button _buttonWrongBook = null!;
        private Button _buttonPractice = null!;
        private readonly Dictionary<Button, Size> _originalButtonSizes = new Dictionary<Button, Size>();

        #endregion

        #region Public Properties

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FlowLayoutPanel ButtonsPanel => _buttonsPanel;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonKnown => _buttonKnown;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonUnknown => _buttonUnknown;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonFavorite => _buttonFavorite;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonExit => _buttonExit;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonPrevious => _buttonPrevious;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonNext => _buttonNext;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonEdit => _buttonEdit;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonStats => _buttonStats;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonChallenge => _buttonChallenge;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonAchievement => _buttonAchievement;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonWrongBook => _buttonWrongBook;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonPractice => _buttonPractice;

        #endregion

        #region Events

        /// <summary>
        /// 已知按钮点击事件
        /// </summary>
        public event EventHandler? KnownClicked;

        /// <summary>
        /// 未知按钮点击事件
        /// </summary>
        public event EventHandler? UnknownClicked;

        /// <summary>
        /// 下一个按钮点击事件
        /// </summary>
        public event EventHandler? NextClicked;

        /// <summary>
        /// 上一项按钮点击事件
        /// </summary>
        public event EventHandler? PreviousClicked;

        /// <summary>
        /// 编辑按钮点击事件
        /// </summary>
        public event EventHandler? EditClicked;

        /// <summary>
        /// 收藏按钮点击事件
        /// </summary>
        public event EventHandler? FavoriteClicked;

        /// <summary>
        /// 退出按钮点击事件
        /// </summary>
        public event EventHandler? ExitClicked;

        /// <summary>
        /// 学习统计按钮点击事件
        /// </summary>
        public event EventHandler? StatsClicked;

        /// <summary>
        /// 每日挑战按钮点击事件
        /// </summary>
        public event EventHandler? ChallengeClicked;

        /// <summary>
        /// 成就徽章按钮点击事件
        /// </summary>
        public event EventHandler? AchievementClicked;

        /// <summary>
        /// 错题本按钮点击事件
        /// </summary>
        public event EventHandler? WrongBookClicked;

        /// <summary>
        /// 单词跟读/听写按钮点击事件（P2）
        /// </summary>
        public event EventHandler? PracticeClicked;

        #endregion

        #region Initialization

        public LearningButtonsView()
        {
            InitializeComponent();
            ApplyRoundedStyles();
        }

        private void ApplyRoundedStyles()
        {
            ApplyRoundedStyle(_buttonKnown, 6);
            ApplyRoundedStyle(_buttonUnknown, 6);
            ApplyRoundedStyle(_buttonFavorite, 6);
            ApplyRoundedStyle(_buttonExit, 6);
            ApplyRoundedStyle(_buttonPrevious, 6);
            ApplyRoundedStyle(_buttonNext, 6);
            ApplyRoundedStyle(_buttonEdit, 6);
            ApplyRoundedStyle(_buttonStats, 6);
            ApplyRoundedStyle(_buttonChallenge, 6);
            ApplyRoundedStyle(_buttonAchievement, 6);
            ApplyRoundedStyle(_buttonWrongBook, 6);
            ApplyRoundedStyle(_buttonPractice, 6);
        }

        private void ApplyRoundedStyle(Button button, int radius)
        {
            if (!_originalButtonSizes.ContainsKey(button))
            {
                _originalButtonSizes[button] = button.Size;
            }

            button.Paint += (sender, e) =>
            {
                if (sender is Button btn)
                {
                    using var path = new System.Drawing.Drawing2D.GraphicsPath();
                    path.AddArc(0, 0, radius, radius, 180, 90);
                    path.AddArc(btn.Width - radius, 0, radius, radius, 270, 90);
                    path.AddArc(btn.Width - radius, btn.Height - radius, radius, radius, 0, 90);
                    path.AddArc(0, btn.Height - radius, radius, radius, 90, 90);
                    path.CloseAllFigures();
                    btn.Region = new Region(path);
                }
            };

            button.MouseEnter += (sender, e) =>
            {
                if (sender is Button btn)
                {
                    btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(btn.BackColor, 10);
                }
            };

            button.MouseDown += (sender, e) =>
            {
                if (sender is Button btn)
                {
                    AnimateButton(btn, 0.95f);
                }
            };

            button.MouseUp += (sender, e) =>
            {
                if (sender is Button btn)
                {
                    AnimateButton(btn, 1.0f);
                }
            };
        }

        private void AnimateButton(Button button, float scale)
        {
            if (button == null || button.IsDisposed) return;
            if (!_originalButtonSizes.TryGetValue(button, out var originalSize))
            {
                originalSize = button.Size;
                _originalButtonSizes[button] = originalSize;
            }
            int newWidth = (int)(originalSize.Width * scale);
            int newHeight = (int)(originalSize.Height * scale);
            int offsetX = (originalSize.Width - newWidth) / 2;
            int offsetY = (originalSize.Height - newHeight) / 2;
            button.Size = new Size(newWidth, newHeight);
            button.Location = new Point(button.Location.X + offsetX, button.Location.Y + offsetY);
        }

        private void InitializeComponent()
        {
            _buttonsPanel = new FlowLayoutPanel();
            _buttonPrevious = new Button();
            _buttonNext = new Button();
            _buttonKnown = new Button();
            _buttonUnknown = new Button();
            separator1 = new Panel();
            _buttonFavorite = new Button();
            _buttonEdit = new Button();
            separator2 = new Panel();
            _buttonStats = new Button();
            _buttonChallenge = new Button();
            _buttonAchievement = new Button();
            _buttonWrongBook = new Button();
            _buttonPractice = new Button();
            separator3 = new Panel();
            _buttonExit = new Button();
            _buttonsPanel.SuspendLayout();
            SuspendLayout();
            // 
            // _buttonsPanel
            // 
            _buttonsPanel.Controls.Add(_buttonPrevious);
            _buttonsPanel.Controls.Add(_buttonNext);
            _buttonsPanel.Controls.Add(_buttonKnown);
            _buttonsPanel.Controls.Add(_buttonUnknown);
            _buttonsPanel.Controls.Add(separator1);
            _buttonsPanel.Controls.Add(_buttonFavorite);
            _buttonsPanel.Controls.Add(_buttonEdit);
            _buttonsPanel.Controls.Add(separator2);
            _buttonsPanel.Controls.Add(_buttonStats);
            _buttonsPanel.Controls.Add(_buttonChallenge);
            _buttonsPanel.Controls.Add(_buttonAchievement);
            _buttonsPanel.Controls.Add(_buttonWrongBook);
            _buttonsPanel.Controls.Add(_buttonPractice);
            _buttonsPanel.Controls.Add(separator3);
            _buttonsPanel.Controls.Add(_buttonExit);
            _buttonsPanel.Dock = DockStyle.Fill;
            _buttonsPanel.Location = new Point(0, 0);
            _buttonsPanel.Name = "_buttonsPanel";
            _buttonsPanel.Padding = new Padding(10, 6, 10, 6);
            _buttonsPanel.Size = new Size(1172, 80);
            _buttonsPanel.TabIndex = 0;
            _buttonsPanel.WrapContents = false;
            // 
            // _buttonPrevious
            // 
            _buttonPrevious.BackColor = Color.FromArgb(108, 117, 125);
            _buttonPrevious.FlatAppearance.BorderSize = 0;
            _buttonPrevious.FlatAppearance.MouseDownBackColor = Color.FromArgb(98, 107, 115);
            _buttonPrevious.FlatAppearance.MouseOverBackColor = Color.FromArgb(118, 127, 135);
            _buttonPrevious.FlatStyle = FlatStyle.Flat;
            _buttonPrevious.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _buttonPrevious.ForeColor = Color.White;
            _buttonPrevious.Location = new Point(15, 12);
            _buttonPrevious.Margin = new Padding(5, 6, 5, 6);
            _buttonPrevious.Name = "_buttonPrevious";
            _buttonPrevious.Size = new Size(90, 51);
            _buttonPrevious.TabIndex = 0;
            _buttonPrevious.Text = "⏮ 上一项";
            _buttonPrevious.UseVisualStyleBackColor = false;
            _buttonPrevious.Click += ButtonPrevious_Click;
            // 
            // _buttonNext
            // 
            // 语义色：绿色 #22C55E = 成功/主操作（与设计系统 Success 对齐）
            _buttonNext.BackColor = Color.FromArgb(34, 197, 94);
            _buttonNext.FlatAppearance.BorderSize = 0;
            _buttonNext.FlatAppearance.MouseDownBackColor = Color.FromArgb(22, 163, 74);
            _buttonNext.FlatAppearance.MouseOverBackColor = Color.FromArgb(74, 222, 128);
            _buttonNext.FlatStyle = FlatStyle.Flat;
            _buttonNext.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _buttonNext.ForeColor = Color.White;
            _buttonNext.Location = new Point(115, 12);
            _buttonNext.Margin = new Padding(5, 6, 5, 6);
            _buttonNext.Name = "_buttonNext";
            _buttonNext.Size = new Size(90, 51);
            _buttonNext.TabIndex = 1;
            _buttonNext.Text = "下一项 ⏭";
            _buttonNext.UseVisualStyleBackColor = false;
            _buttonNext.Click += ButtonNext_Click;
            // 
            // _buttonKnown
            // 
            // 主操作：绿色实心 #22C55E，加大加粗突出（Width 约为普通按钮的1.6倍，字号14F bold）
            _buttonKnown.BackColor = Color.FromArgb(34, 197, 94);
            _buttonKnown.FlatAppearance.BorderSize = 0;
            _buttonKnown.FlatAppearance.MouseDownBackColor = Color.FromArgb(22, 163, 74);
            _buttonKnown.FlatAppearance.MouseOverBackColor = Color.FromArgb(74, 222, 128);
            _buttonKnown.FlatStyle = FlatStyle.Flat;
            _buttonKnown.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            _buttonKnown.ForeColor = Color.White;
            _buttonKnown.Location = new Point(215, 12);
            _buttonKnown.Margin = new Padding(5, 6, 5, 6);
            _buttonKnown.Name = "_buttonKnown";
            _buttonKnown.Size = new Size(160, 51);
            _buttonKnown.TabIndex = 2;
            _buttonKnown.Text = "✅ 会了";
            _buttonKnown.UseVisualStyleBackColor = false;
            _buttonKnown.Click += ButtonKnown_Click;
            // 
            // _buttonUnknown
            // 
            // 危险操作：红色实心 #EF4444（与设计系统 Error 对齐），保持普通尺寸
            _buttonUnknown.BackColor = Color.FromArgb(239, 68, 68);
            _buttonUnknown.FlatAppearance.BorderSize = 0;
            _buttonUnknown.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 38, 38);
            _buttonUnknown.FlatAppearance.MouseOverBackColor = Color.FromArgb(248, 113, 113);
            _buttonUnknown.FlatStyle = FlatStyle.Flat;
            _buttonUnknown.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _buttonUnknown.ForeColor = Color.White;
            _buttonUnknown.Location = new Point(325, 12);
            _buttonUnknown.Margin = new Padding(5, 6, 5, 6);
            _buttonUnknown.Name = "_buttonUnknown";
            _buttonUnknown.Size = new Size(100, 51);
            _buttonUnknown.TabIndex = 3;
            _buttonUnknown.Text = "❌ 不会";
            _buttonUnknown.UseVisualStyleBackColor = false;
            _buttonUnknown.Click += ButtonUnknown_Click;
            // 
            // separator1
            // 
            separator1.Location = new Point(433, 9);
            separator1.Name = "separator1";
            separator1.Size = new Size(20, 51);
            separator1.TabIndex = 4;
            // 
            // _buttonFavorite
            // 
            // 语义色：橙色 #F59E0B = 收藏（与设计系统 Warning/Favorite 对齐）
            _buttonFavorite.BackColor = Color.FromArgb(245, 158, 11);
            _buttonFavorite.FlatAppearance.BorderSize = 0;
            _buttonFavorite.FlatAppearance.MouseDownBackColor = Color.FromArgb(217, 119, 6);
            _buttonFavorite.FlatAppearance.MouseOverBackColor = Color.FromArgb(251, 191, 36);
            _buttonFavorite.FlatStyle = FlatStyle.Flat;
            _buttonFavorite.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _buttonFavorite.ForeColor = Color.White;
            _buttonFavorite.Location = new Point(461, 12);
            _buttonFavorite.Margin = new Padding(5, 6, 5, 6);
            _buttonFavorite.Name = "_buttonFavorite";
            _buttonFavorite.Size = new Size(86, 51);
            _buttonFavorite.TabIndex = 5;
            _buttonFavorite.Text = "⭐ 收藏";
            _buttonFavorite.UseVisualStyleBackColor = false;
            _buttonFavorite.Click += ButtonFavorite_Click;
            // 
            // _buttonEdit
            // 
            _buttonEdit.BackColor = Color.FromArgb(52, 152, 219);
            _buttonEdit.FlatAppearance.BorderSize = 0;
            _buttonEdit.FlatAppearance.MouseDownBackColor = Color.FromArgb(42, 142, 209);
            _buttonEdit.FlatAppearance.MouseOverBackColor = Color.FromArgb(62, 162, 229);
            _buttonEdit.FlatStyle = FlatStyle.Flat;
            _buttonEdit.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _buttonEdit.ForeColor = Color.White;
            _buttonEdit.Location = new Point(557, 12);
            _buttonEdit.Margin = new Padding(5, 6, 5, 6);
            _buttonEdit.Name = "_buttonEdit";
            _buttonEdit.Size = new Size(86, 51);
            _buttonEdit.TabIndex = 6;
            _buttonEdit.Text = "✏️ 编辑";
            _buttonEdit.UseVisualStyleBackColor = false;
            _buttonEdit.Click += ButtonEdit_Click;
            // 
            // separator2
            // 
            separator2.Location = new Point(651, 9);
            separator2.Name = "separator2";
            separator2.Size = new Size(20, 51);
            separator2.TabIndex = 7;
            // 
            // _buttonStats
            // 
            // 语义色：紫色 #8B5CF6 = 主色（与设计系统 Primary 对齐）
            _buttonStats.BackColor = Color.FromArgb(139, 92, 246);
            _buttonStats.FlatAppearance.BorderSize = 0;
            _buttonStats.FlatAppearance.MouseDownBackColor = Color.FromArgb(124, 58, 237);
            _buttonStats.FlatAppearance.MouseOverBackColor = Color.FromArgb(167, 139, 250);
            _buttonStats.FlatStyle = FlatStyle.Flat;
            _buttonStats.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _buttonStats.ForeColor = Color.White;
            _buttonStats.Margin = new Padding(5, 6, 5, 6);
            _buttonStats.Name = "_buttonStats";
            _buttonStats.Size = new Size(86, 51);
            _buttonStats.TabIndex = 9;
            _buttonStats.Text = "📊 统计";
            _buttonStats.UseVisualStyleBackColor = false;
            _buttonStats.Click += ButtonStats_Click;
            // 
            // _buttonChallenge
            // 
            // 语义色：橙色 #F59E0B = 警告/挑战（与设计系统 Warning 对齐）
            _buttonChallenge.BackColor = Color.FromArgb(245, 158, 11);
            _buttonChallenge.FlatAppearance.BorderSize = 0;
            _buttonChallenge.FlatAppearance.MouseDownBackColor = Color.FromArgb(217, 119, 6);
            _buttonChallenge.FlatAppearance.MouseOverBackColor = Color.FromArgb(251, 191, 36);
            _buttonChallenge.FlatStyle = FlatStyle.Flat;
            _buttonChallenge.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _buttonChallenge.ForeColor = Color.White;
            _buttonChallenge.Margin = new Padding(5, 6, 5, 6);
            _buttonChallenge.Name = "_buttonChallenge";
            _buttonChallenge.Size = new Size(86, 51);
            _buttonChallenge.TabIndex = 10;
            _buttonChallenge.Text = "🎯 挑战";
            _buttonChallenge.UseVisualStyleBackColor = false;
            _buttonChallenge.Click += ButtonChallenge_Click;
            // 
            // _buttonAchievement
            // 
            // 语义色：橙色 #F59E0B = 成就/收藏（与设计系统 Warning/Favorite 对齐）
            _buttonAchievement.BackColor = Color.FromArgb(245, 158, 11);
            _buttonAchievement.FlatAppearance.BorderSize = 0;
            _buttonAchievement.FlatAppearance.MouseDownBackColor = Color.FromArgb(217, 119, 6);
            _buttonAchievement.FlatAppearance.MouseOverBackColor = Color.FromArgb(251, 191, 36);
            _buttonAchievement.FlatStyle = FlatStyle.Flat;
            _buttonAchievement.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _buttonAchievement.ForeColor = Color.White;
            _buttonAchievement.Margin = new Padding(5, 6, 5, 6);
            _buttonAchievement.Name = "_buttonAchievement";
            _buttonAchievement.Size = new Size(86, 51);
            _buttonAchievement.TabIndex = 11;
            _buttonAchievement.Text = "🏆 成就";
            _buttonAchievement.UseVisualStyleBackColor = false;
            _buttonAchievement.Click += ButtonAchievement_Click;
            // 
            // _buttonWrongBook
            // 
            // 语义色：红色 #EF4444 = 危险/错题（与设计系统 Error 对齐）
            _buttonWrongBook.BackColor = Color.FromArgb(239, 68, 68);
            _buttonWrongBook.FlatAppearance.BorderSize = 0;
            _buttonWrongBook.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 38, 38);
            _buttonWrongBook.FlatAppearance.MouseOverBackColor = Color.FromArgb(248, 113, 113);
            _buttonWrongBook.FlatStyle = FlatStyle.Flat;
            _buttonWrongBook.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _buttonWrongBook.ForeColor = Color.White;
            _buttonWrongBook.Margin = new Padding(5, 6, 5, 6);
            _buttonWrongBook.Name = "_buttonWrongBook";
            _buttonWrongBook.Size = new Size(86, 51);
            _buttonWrongBook.TabIndex = 12;
            _buttonWrongBook.Text = "📕 错题";
            _buttonWrongBook.UseVisualStyleBackColor = false;
            _buttonWrongBook.Click += ButtonWrongBook_Click;
            // 
            // _buttonPractice
            // 
            // 语义色：蓝色 #3B82F6 = 练习入口，与统计/挑战/错题工具区分
            _buttonPractice.BackColor = Color.FromArgb(59, 130, 246);
            _buttonPractice.FlatAppearance.BorderSize = 0;
            _buttonPractice.FlatAppearance.MouseDownBackColor = Color.FromArgb(37, 99, 235);
            _buttonPractice.FlatAppearance.MouseOverBackColor = Color.FromArgb(96, 165, 250);
            _buttonPractice.FlatStyle = FlatStyle.Flat;
            _buttonPractice.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _buttonPractice.ForeColor = Color.White;
            _buttonPractice.Margin = new Padding(5, 6, 5, 6);
            _buttonPractice.Name = "_buttonPractice";
            _buttonPractice.Size = new Size(86, 51);
            _buttonPractice.TabIndex = 13;
            _buttonPractice.Text = "🎤 练习";
            _buttonPractice.UseVisualStyleBackColor = false;
            _buttonPractice.Click += ButtonPractice_Click;
            // 
            // separator3
            // 
            separator3.Name = "separator3";
            separator3.Size = new Size(20, 51);
            separator3.TabIndex = 14;
            // 
            // _buttonExit
            // 
            _buttonExit.BackColor = Color.FromArgb(108, 117, 125);
            _buttonExit.FlatAppearance.BorderSize = 0;
            _buttonExit.FlatAppearance.MouseDownBackColor = Color.FromArgb(98, 107, 115);
            _buttonExit.FlatAppearance.MouseOverBackColor = Color.FromArgb(118, 127, 135);
            _buttonExit.FlatStyle = FlatStyle.Flat;
            _buttonExit.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _buttonExit.ForeColor = Color.White;
            _buttonExit.Margin = new Padding(5, 6, 5, 6);
            _buttonExit.Name = "_buttonExit";
            _buttonExit.Size = new Size(86, 51);
            _buttonExit.TabIndex = 14;
            _buttonExit.Text = "🏠 返回";
            _buttonExit.UseVisualStyleBackColor = false;
            _buttonExit.Click += ButtonExit_Click;
            // 
            // LearningButtonsView
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(_buttonsPanel);
            Name = "LearningButtonsView";
            Size = new Size(1172, 80);
            _buttonsPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        private void ButtonPrevious_Click(object? sender, EventArgs e) => PreviousClicked?.Invoke(sender, e);

        private void ButtonNext_Click(object? sender, EventArgs e) => NextClicked?.Invoke(sender, e);

        private void ButtonEdit_Click(object? sender, EventArgs e) => EditClicked?.Invoke(sender, e);

        private void ButtonKnown_Click(object? sender, EventArgs e) => KnownClicked?.Invoke(sender, e);

        private void ButtonUnknown_Click(object? sender, EventArgs e) => UnknownClicked?.Invoke(sender, e);

        private void ButtonFavorite_Click(object? sender, EventArgs e) => FavoriteClicked?.Invoke(sender, e);

        private void ButtonExit_Click(object? sender, EventArgs e) => ExitClicked?.Invoke(sender, e);

        private void ButtonStats_Click(object? sender, EventArgs e) => StatsClicked?.Invoke(sender, e);

        private void ButtonChallenge_Click(object? sender, EventArgs e) => ChallengeClicked?.Invoke(sender, e);

        private void ButtonAchievement_Click(object? sender, EventArgs e) => AchievementClicked?.Invoke(sender, e);

        private void ButtonWrongBook_Click(object? sender, EventArgs e) => WrongBookClicked?.Invoke(sender, e);

        private void ButtonPractice_Click(object? sender, EventArgs e) => PracticeClicked?.Invoke(sender, e);

        #endregion

        #region Public Methods

        /// <summary>
        /// 启用/禁用所有按钮
        /// </summary>
        public void EnableButtons(bool enabled)
        {
            _buttonPrevious.Enabled = enabled;
            _buttonNext.Enabled = enabled;
            _buttonKnown.Enabled = enabled;
            _buttonUnknown.Enabled = enabled;
            _buttonFavorite.Enabled = enabled;
            _buttonEdit.Enabled = enabled;
            _buttonStats.Enabled = enabled;
            _buttonChallenge.Enabled = enabled;
            _buttonAchievement.Enabled = enabled;
            _buttonWrongBook.Enabled = enabled;
            _buttonPractice.Enabled = enabled;
            _buttonExit.Enabled = enabled;
        }

        /// <summary>
        /// 设置已知按钮文本
        /// </summary>
        public void SetKnownButtonText(string text)
        {
            _buttonKnown.Text = text;
        }

        /// <summary>
        /// 设置未知按钮文本
        /// </summary>
        public void SetUnknownButtonText(string text)
        {
            _buttonUnknown.Text = text;
        }

        /// <summary>
        /// 设置收藏按钮状态
        /// </summary>
        public void SetFavoriteState(bool isFavorite)
        {
            _buttonFavorite.Text = isFavorite ? "⭐ 已收藏" : "⭐ 收藏";
        }

        #endregion
    }
}