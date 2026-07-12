using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls
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
        private Button _buttonPrevious = null!;
        private Button _buttonNext = null!;
        private Button _buttonEdit = null!;
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
            _buttonNext.BackColor = Color.FromArgb(76, 175, 80);
            _buttonNext.FlatAppearance.BorderSize = 0;
            _buttonNext.FlatAppearance.MouseDownBackColor = Color.FromArgb(66, 155, 70);
            _buttonNext.FlatAppearance.MouseOverBackColor = Color.FromArgb(86, 185, 90);
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
            _buttonKnown.BackColor = Color.FromArgb(76, 175, 80);
            _buttonKnown.FlatAppearance.BorderSize = 0;
            _buttonKnown.FlatAppearance.MouseDownBackColor = Color.FromArgb(66, 155, 70);
            _buttonKnown.FlatAppearance.MouseOverBackColor = Color.FromArgb(86, 185, 90);
            _buttonKnown.FlatStyle = FlatStyle.Flat;
            _buttonKnown.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _buttonKnown.ForeColor = Color.White;
            _buttonKnown.Location = new Point(215, 12);
            _buttonKnown.Margin = new Padding(5, 6, 5, 6);
            _buttonKnown.Name = "_buttonKnown";
            _buttonKnown.Size = new Size(100, 51);
            _buttonKnown.TabIndex = 2;
            _buttonKnown.Text = "✅ 会了";
            _buttonKnown.UseVisualStyleBackColor = false;
            _buttonKnown.Click += ButtonKnown_Click;
            // 
            // _buttonUnknown
            // 
            _buttonUnknown.BackColor = Color.FromArgb(244, 67, 54);
            _buttonUnknown.FlatAppearance.BorderSize = 0;
            _buttonUnknown.FlatAppearance.MouseDownBackColor = Color.FromArgb(234, 57, 44);
            _buttonUnknown.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 77, 64);
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
            _buttonFavorite.BackColor = Color.FromArgb(255, 193, 7);
            _buttonFavorite.FlatAppearance.BorderSize = 0;
            _buttonFavorite.FlatAppearance.MouseDownBackColor = Color.FromArgb(245, 183, 0);
            _buttonFavorite.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 203, 27);
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
            // _buttonExit
            // 
            _buttonExit.BackColor = Color.FromArgb(108, 117, 125);
            _buttonExit.FlatAppearance.BorderSize = 0;
            _buttonExit.FlatAppearance.MouseDownBackColor = Color.FromArgb(98, 107, 115);
            _buttonExit.FlatAppearance.MouseOverBackColor = Color.FromArgb(118, 127, 135);
            _buttonExit.FlatStyle = FlatStyle.Flat;
            _buttonExit.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _buttonExit.ForeColor = Color.White;
            _buttonExit.Location = new Point(679, 12);
            _buttonExit.Margin = new Padding(5, 6, 5, 6);
            _buttonExit.Name = "_buttonExit";
            _buttonExit.Size = new Size(86, 51);
            _buttonExit.TabIndex = 8;
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