using System.ComponentModel;
using LearningAssistant.Common;

namespace LearningAssistant.Forms.UserControls.Learning
{
    /// <summary>
    /// 学习按钮视图 - 底部操作按钮区
    /// 只保留高频主操作；统计/挑战/成就/错题/练习/编辑收纳到「更多」菜单，降低视觉干扰。
    /// </summary>
    public class LearningButtonsView : UserControl
    {
        #region Controls

        private FlowLayoutPanel _buttonsPanel = null!;
        private Button _buttonKnown = null!;
        private Button _buttonUnknown = null!;
        private Button _buttonFavorite = null!;
        private Button _buttonExit = null!;
        private Panel separator1 = null!;
        private Button _buttonPrevious = null!;
        private Button _buttonNext = null!;
        private Button _buttonMore = null!;
        private ContextMenuStrip _moreMenu = null!;
        private ToolStripMenuItem _menuPractice = null!;
        private ToolStripMenuItem _menuEdit = null!;
        private ToolStripMenuItem _menuStats = null!;
        private ToolStripMenuItem _menuChallenge = null!;
        private ToolStripMenuItem _menuAchievement = null!;
        private ToolStripMenuItem _menuWrongBook = null!;
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
        public Button ButtonMore => _buttonMore;

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
        /// 单词跟读按钮点击事件（P2）
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
            ApplyRoundedStyle(_buttonMore, 6);
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
            _buttonMore = new Button();
            _buttonExit = new Button();
            _buttonsPanel.SuspendLayout();
            SuspendLayout();
            // 
            // _buttonsPanel
            // 
            // 主次分组：导航 → 判定 → 收藏/更多工具
            _buttonsPanel.Controls.Add(_buttonPrevious);
            _buttonsPanel.Controls.Add(_buttonUnknown);
            _buttonsPanel.Controls.Add(_buttonKnown);
            _buttonsPanel.Controls.Add(_buttonNext);
            _buttonsPanel.Controls.Add(separator1);
            _buttonsPanel.Controls.Add(_buttonFavorite);
            _buttonsPanel.Controls.Add(_buttonMore);
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
            // 导航操作：中性灰，避免与「会了/不会」抢视觉焦点
            _buttonPrevious.BackColor = Color.FromArgb(108, 117, 125);
            _buttonPrevious.FlatAppearance.BorderSize = 0;
            _buttonPrevious.FlatAppearance.MouseDownBackColor = Color.FromArgb(98, 107, 115);
            _buttonPrevious.FlatAppearance.MouseOverBackColor = Color.FromArgb(118, 127, 135);
            _buttonPrevious.FlatStyle = FlatStyle.Flat;
            _buttonPrevious.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _buttonPrevious.ForeColor = Color.White;
            _buttonPrevious.Location = new Point(15, 6);
            _buttonPrevious.Margin = new Padding(5, 6, 5, 6);
            _buttonPrevious.Name = "_buttonPrevious";
            _buttonPrevious.Size = new Size(90, 51);
            _buttonPrevious.TabIndex = 0;
            _buttonPrevious.Text = "⏮ 上一项";
            _buttonPrevious.UseVisualStyleBackColor = false;
            _buttonPrevious.Click += ButtonPrevious_Click;
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
            _buttonUnknown.Location = new Point(115, 6);
            _buttonUnknown.Margin = new Padding(5, 6, 5, 6);
            _buttonUnknown.Name = "_buttonUnknown";
            _buttonUnknown.Size = new Size(100, 51);
            _buttonUnknown.TabIndex = 1;
            _buttonUnknown.Text = "❌ 不会";
            _buttonUnknown.UseVisualStyleBackColor = false;
            _buttonUnknown.Click += ButtonUnknown_Click;
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
            _buttonKnown.Location = new Point(220, 6);
            _buttonKnown.Margin = new Padding(5, 6, 5, 6);
            _buttonKnown.Name = "_buttonKnown";
            _buttonKnown.Size = new Size(160, 51);
            _buttonKnown.TabIndex = 2;
            _buttonKnown.Text = "✅ 会了";
            _buttonKnown.UseVisualStyleBackColor = false;
            _buttonKnown.Click += ButtonKnown_Click;
            // 
            // _buttonNext
            // 
            // 导航操作：中性灰，与上一项同一层级
            _buttonNext.BackColor = Color.FromArgb(108, 117, 125);
            _buttonNext.FlatAppearance.BorderSize = 0;
            _buttonNext.FlatAppearance.MouseDownBackColor = Color.FromArgb(98, 107, 115);
            _buttonNext.FlatAppearance.MouseOverBackColor = Color.FromArgb(118, 127, 135);
            _buttonNext.FlatStyle = FlatStyle.Flat;
            _buttonNext.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _buttonNext.ForeColor = Color.White;
            _buttonNext.Location = new Point(330, 6);
            _buttonNext.Margin = new Padding(5, 6, 5, 6);
            _buttonNext.Name = "_buttonNext";
            _buttonNext.Size = new Size(90, 51);
            _buttonNext.TabIndex = 3;
            _buttonNext.Text = "下一项 ⏭";
            _buttonNext.UseVisualStyleBackColor = false;
            _buttonNext.Click += ButtonNext_Click;
            // 
            // separator1
            // 
            separator1.Location = new Point(428, 9);
            separator1.Name = "separator1";
            separator1.Size = new Size(20, 45);
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
            _buttonFavorite.Location = new Point(456, 6);
            _buttonFavorite.Margin = new Padding(5, 6, 5, 6);
            _buttonFavorite.Name = "_buttonFavorite";
            _buttonFavorite.Size = new Size(86, 51);
            _buttonFavorite.TabIndex = 5;
            _buttonFavorite.Text = "⭐ 收藏";
            _buttonFavorite.UseVisualStyleBackColor = false;
            _buttonFavorite.Click += ButtonFavorite_Click;
            // 
            // _buttonMore
            // 
            // 次要工具入口：浅灰描边，不参与主操作的色彩竞争
            _buttonMore.BackColor = Color.FromArgb(241, 245, 249);
            _buttonMore.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _buttonMore.FlatAppearance.BorderSize = 1;
            _buttonMore.FlatAppearance.MouseDownBackColor = Color.FromArgb(226, 232, 240);
            _buttonMore.FlatAppearance.MouseOverBackColor = Color.FromArgb(248, 250, 252);
            _buttonMore.FlatStyle = FlatStyle.Flat;
            _buttonMore.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _buttonMore.ForeColor = Color.FromArgb(51, 65, 85);
            _buttonMore.Location = new Point(552, 6);
            _buttonMore.Margin = new Padding(5, 6, 5, 6);
            _buttonMore.Name = "_buttonMore";
            _buttonMore.Size = new Size(86, 51);
            _buttonMore.TabIndex = 6;
            _buttonMore.Text = "⋯ 更多";
            _buttonMore.UseVisualStyleBackColor = false;
            _buttonMore.Click += ButtonMore_Click;
            // 
            // _buttonExit
            // 
            // 退出按钮由 LearningForm 移到顶栏复用，这里保留创建以兼容既有布局逻辑
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
            _buttonExit.TabIndex = 7;
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

            InitializeMoreMenu();
        }

        /// <summary>
        /// 构建「更多」菜单：低频工具集中收纳，避免底部操作条被按钮淹没。
        /// </summary>
        private void InitializeMoreMenu()
        {
            _moreMenu = new ContextMenuStrip
            {
                ShowImageMargin = false,
                Font = new Font("微软雅黑", 10F),
                BackColor = Color.White
            };

            _menuPractice = CreateMoreMenuItem("🎤 单词跟读练习");
            _menuPractice.Click += (_, _) => PracticeClicked?.Invoke(this, EventArgs.Empty);

            _menuEdit = CreateMoreMenuItem("✏️ 编辑内容");
            // 与现有快捷键 E 对应，方便键盘用户发现
            _menuEdit.ShortcutKeyDisplayString = "E";
            _menuEdit.Click += (_, _) => EditClicked?.Invoke(this, EventArgs.Empty);

            _menuStats = CreateMoreMenuItem("📊 学习统计");
            _menuStats.Click += (_, _) => StatsClicked?.Invoke(this, EventArgs.Empty);

            _menuChallenge = CreateMoreMenuItem("🎯 每日挑战");
            _menuChallenge.Click += (_, _) => ChallengeClicked?.Invoke(this, EventArgs.Empty);

            _menuAchievement = CreateMoreMenuItem("🏆 成就徽章");
            _menuAchievement.Click += (_, _) => AchievementClicked?.Invoke(this, EventArgs.Empty);

            _menuWrongBook = CreateMoreMenuItem("📕 错题本");
            _menuWrongBook.Click += (_, _) => WrongBookClicked?.Invoke(this, EventArgs.Empty);

            _moreMenu.Items.Add(_menuPractice);
            _moreMenu.Items.Add(_menuEdit);
            _moreMenu.Items.Add(new ToolStripSeparator());
            _moreMenu.Items.Add(_menuStats);
            _moreMenu.Items.Add(_menuChallenge);
            _moreMenu.Items.Add(_menuAchievement);
            _moreMenu.Items.Add(_menuWrongBook);
        }

        private static ToolStripMenuItem CreateMoreMenuItem(string text) =>
            new ToolStripMenuItem(text)
            {
                Padding = new Padding(4, 3, 4, 3)
            };

        private void ButtonPrevious_Click(object? sender, EventArgs e) => PreviousClicked?.Invoke(sender, e);

        private void ButtonNext_Click(object? sender, EventArgs e) => NextClicked?.Invoke(sender, e);

        private void ButtonKnown_Click(object? sender, EventArgs e) => KnownClicked?.Invoke(sender, e);

        private void ButtonUnknown_Click(object? sender, EventArgs e) => UnknownClicked?.Invoke(sender, e);

        private void ButtonFavorite_Click(object? sender, EventArgs e) => FavoriteClicked?.Invoke(sender, e);

        private void ButtonExit_Click(object? sender, EventArgs e) => ExitClicked?.Invoke(sender, e);

        private void ButtonMore_Click(object? sender, EventArgs e)
        {
            if (_moreMenu.Items.Count == 0) return;
            // 底部操作条贴近窗口下沿，菜单向上展开
            _moreMenu.Show(_buttonMore, new Point(0, -_moreMenu.PreferredSize.Height));
        }

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
            _buttonMore.Enabled = enabled;
            _buttonExit.Enabled = enabled;

            _menuPractice.Enabled = enabled;
            _menuEdit.Enabled = enabled;
            _menuStats.Enabled = enabled;
            _menuChallenge.Enabled = enabled;
            _menuAchievement.Enabled = enabled;
            _menuWrongBook.Enabled = enabled;
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

        /// <summary>
        /// 应用主题：低频工具入口（更多/菜单）随主题弱化，避免在深色背景上过于抢眼。
        /// </summary>
        public void ApplyTheme(ThemeColors colors)
        {
            bool dark = colors.ThemeMode == ThemeMode.Dark;

            _buttonMore.BackColor = dark ? colors.Surface : Color.FromArgb(241, 245, 249);
            _buttonMore.ForeColor = dark ? colors.TextSecondary : Color.FromArgb(51, 65, 85);
            _buttonMore.FlatAppearance.BorderColor = dark ? colors.Divider : Color.FromArgb(203, 213, 225);
            _buttonMore.FlatAppearance.MouseOverBackColor = dark
                ? ControlPaint.Light(colors.Surface, 0.08f)
                : Color.FromArgb(248, 250, 252);
            _buttonMore.FlatAppearance.MouseDownBackColor = dark
                ? ControlPaint.Dark(colors.Surface, 0.06f)
                : Color.FromArgb(226, 232, 240);

            _moreMenu.BackColor = dark ? colors.SurfaceElevated : Color.White;
            _moreMenu.ForeColor = colors.TextPrimary;
            foreach (ToolStripItem item in _moreMenu.Items)
            {
                item.ForeColor = colors.TextPrimary;
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _moreMenu?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
