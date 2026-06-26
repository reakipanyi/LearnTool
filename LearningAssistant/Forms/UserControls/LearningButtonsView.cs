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
        private Button _buttonPronounce = null!;
        private Button _buttonFavorite = null!;
        private Button _buttonNote = null!;
        private Button _buttonExit = null!;
        private Button _buttonAIAsk = null!;
        private Panel separator1;
        private Panel separator2;
        private Panel separator3;
        private Button _buttonFeynman = null!;

        #endregion

        #region Public Properties

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FlowLayoutPanel ButtonsPanel => _buttonsPanel;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonKnown => _buttonKnown;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonUnknown => _buttonUnknown;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonPronounce => _buttonPronounce;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonFavorite => _buttonFavorite;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonNote => _buttonNote;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonExit => _buttonExit;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonAIAsk => _buttonAIAsk;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonFeynman => _buttonFeynman;

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
        /// 发音按钮点击事件
        /// </summary>
        public event EventHandler? PronounceClicked;

        /// <summary>
        /// 收藏按钮点击事件
        /// </summary>
        public event EventHandler? FavoriteClicked;

        /// <summary>
        /// 笔记按钮点击事件
        /// </summary>
        public event EventHandler? NoteClicked;

        /// <summary>
        /// 退出按钮点击事件
        /// </summary>
        public event EventHandler? ExitClicked;

        /// <summary>
        /// AI问答按钮点击事件
        /// </summary>
        public event EventHandler? AIAskClicked;

        /// <summary>
        /// 费曼学习按钮点击事件
        /// </summary>
        public event EventHandler? FeynmanClicked;

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
            ApplyRoundedStyle(_buttonPronounce, 6);
            ApplyRoundedStyle(_buttonFavorite, 6);
            ApplyRoundedStyle(_buttonNote, 6);
            ApplyRoundedStyle(_buttonExit, 6);
            ApplyRoundedStyle(_buttonAIAsk, 6);
            ApplyRoundedStyle(_buttonFeynman, 6);
        }

        private void ApplyRoundedStyle(Button button, int radius)
        {
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
            var originalSize = button.Size;
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
            _buttonKnown = new Button();
            _buttonUnknown = new Button();
            separator1 = new Panel();
            _buttonPronounce = new Button();
            separator2 = new Panel();
            _buttonFavorite = new Button();
            _buttonNote = new Button();
            separator3 = new Panel();
            _buttonExit = new Button();
            _buttonAIAsk = new Button();
            _buttonFeynman = new Button();
            _buttonsPanel.SuspendLayout();
            SuspendLayout();
            // 
            // _buttonsPanel
            // 
            _buttonsPanel.Controls.Add(_buttonKnown);
            _buttonsPanel.Controls.Add(_buttonUnknown);
            _buttonsPanel.Controls.Add(separator1);
            _buttonsPanel.Controls.Add(_buttonPronounce);
            _buttonsPanel.Controls.Add(separator2);
            _buttonsPanel.Controls.Add(_buttonFavorite);
            _buttonsPanel.Controls.Add(_buttonNote);
            _buttonsPanel.Controls.Add(separator3);
            _buttonsPanel.Controls.Add(_buttonExit);
            _buttonsPanel.Controls.Add(_buttonAIAsk);
            _buttonsPanel.Controls.Add(_buttonFeynman);
            _buttonsPanel.Dock = DockStyle.Fill;
            _buttonsPanel.Location = new Point(0, 0);
            _buttonsPanel.Name = "_buttonsPanel";
            _buttonsPanel.Padding = new Padding(10, 6, 10, 6);
            _buttonsPanel.Size = new Size(1448, 80);
            _buttonsPanel.TabIndex = 0;
            _buttonsPanel.WrapContents = false;
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
            _buttonKnown.Location = new Point(15, 12);
            _buttonKnown.Margin = new Padding(5, 6, 5, 6);
            _buttonKnown.Name = "_buttonKnown";
            _buttonKnown.Size = new Size(130, 51);
            _buttonKnown.TabIndex = 0;
            _buttonKnown.Text = "✅ 会了 [K/1]";
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
            _buttonUnknown.Location = new Point(155, 12);
            _buttonUnknown.Margin = new Padding(5, 6, 5, 6);
            _buttonUnknown.Name = "_buttonUnknown";
            _buttonUnknown.Size = new Size(130, 51);
            _buttonUnknown.TabIndex = 1;
            _buttonUnknown.Text = "❌ 不会 [U/2]";
            _buttonUnknown.UseVisualStyleBackColor = false;
            _buttonUnknown.Click += ButtonUnknown_Click;
            // 
            // separator1
            // 
            separator1.Location = new Point(293, 9);
            separator1.Name = "separator1";
            separator1.Size = new Size(33, 51);
            separator1.TabIndex = 2;
            // 
            // _buttonPronounce
            // 
            _buttonPronounce.BackColor = Color.FromArgb(0, 188, 212);
            _buttonPronounce.FlatAppearance.BorderSize = 0;
            _buttonPronounce.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 178, 202);
            _buttonPronounce.FlatAppearance.MouseOverBackColor = Color.FromArgb(10, 198, 222);
            _buttonPronounce.FlatStyle = FlatStyle.Flat;
            _buttonPronounce.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _buttonPronounce.ForeColor = Color.White;
            _buttonPronounce.Location = new Point(334, 12);
            _buttonPronounce.Margin = new Padding(5, 6, 5, 6);
            _buttonPronounce.Name = "_buttonPronounce";
            _buttonPronounce.Size = new Size(130, 51);
            _buttonPronounce.TabIndex = 2;
            _buttonPronounce.Text = "🔊 发音 [Space]";
            _buttonPronounce.UseVisualStyleBackColor = false;
            _buttonPronounce.Click += ButtonPronounce_Click;
            // 
            // separator2
            // 
            separator2.Location = new Point(472, 9);
            separator2.Name = "separator2";
            separator2.Size = new Size(33, 51);
            separator2.TabIndex = 3;
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
            _buttonFavorite.Location = new Point(513, 12);
            _buttonFavorite.Margin = new Padding(5, 6, 5, 6);
            _buttonFavorite.Name = "_buttonFavorite";
            _buttonFavorite.Size = new Size(130, 51);
            _buttonFavorite.TabIndex = 4;
            _buttonFavorite.Text = "⭐ 收藏";
            _buttonFavorite.UseVisualStyleBackColor = false;
            _buttonFavorite.Click += ButtonFavorite_Click;
            // 
            // _buttonNote
            // 
            _buttonNote.BackColor = Color.FromArgb(76, 175, 80);
            _buttonNote.FlatAppearance.BorderSize = 0;
            _buttonNote.FlatAppearance.MouseDownBackColor = Color.FromArgb(66, 155, 70);
            _buttonNote.FlatAppearance.MouseOverBackColor = Color.FromArgb(86, 185, 90);
            _buttonNote.FlatStyle = FlatStyle.Flat;
            _buttonNote.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _buttonNote.ForeColor = Color.White;
            _buttonNote.Location = new Point(653, 12);
            _buttonNote.Margin = new Padding(5, 6, 5, 6);
            _buttonNote.Name = "_buttonNote";
            _buttonNote.Size = new Size(130, 51);
            _buttonNote.TabIndex = 5;
            _buttonNote.Text = "📝 笔记";
            _buttonNote.UseVisualStyleBackColor = false;
            _buttonNote.Click += ButtonNote_Click;
            // 
            // separator3
            // 
            separator3.Location = new Point(791, 9);
            separator3.Name = "separator3";
            separator3.Size = new Size(33, 51);
            separator3.TabIndex = 6;
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
            _buttonExit.Location = new Point(832, 12);
            _buttonExit.Margin = new Padding(5, 6, 5, 6);
            _buttonExit.Name = "_buttonExit";
            _buttonExit.Size = new Size(130, 51);
            _buttonExit.TabIndex = 5;
            _buttonExit.Text = "🏠 返回";
            _buttonExit.UseVisualStyleBackColor = false;
            _buttonExit.Click += ButtonExit_Click;
            // 
            // _buttonAIAsk
            // 
            _buttonAIAsk.BackColor = Color.FromArgb(0, 120, 215);
            _buttonAIAsk.FlatAppearance.BorderSize = 0;
            _buttonAIAsk.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 110, 205);
            _buttonAIAsk.FlatAppearance.MouseOverBackColor = Color.FromArgb(10, 130, 225);
            _buttonAIAsk.FlatStyle = FlatStyle.Flat;
            _buttonAIAsk.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _buttonAIAsk.ForeColor = Color.White;
            _buttonAIAsk.Location = new Point(972, 12);
            _buttonAIAsk.Margin = new Padding(5, 6, 5, 6);
            _buttonAIAsk.Name = "_buttonAIAsk";
            _buttonAIAsk.Size = new Size(130, 51);
            _buttonAIAsk.TabIndex = 7;
            _buttonAIAsk.Text = "🤖 AI问答";
            _buttonAIAsk.UseVisualStyleBackColor = false;
            _buttonAIAsk.Click += ButtonAIAsk_Click;
            // 
            // _buttonFeynman
            // 
            _buttonFeynman.BackColor = Color.FromArgb(147, 112, 219);
            _buttonFeynman.FlatAppearance.BorderSize = 0;
            _buttonFeynman.FlatAppearance.MouseDownBackColor = Color.FromArgb(137, 102, 209);
            _buttonFeynman.FlatAppearance.MouseOverBackColor = Color.FromArgb(157, 122, 229);
            _buttonFeynman.FlatStyle = FlatStyle.Flat;
            _buttonFeynman.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _buttonFeynman.ForeColor = Color.White;
            _buttonFeynman.Location = new Point(1112, 12);
            _buttonFeynman.Margin = new Padding(5, 6, 5, 6);
            _buttonFeynman.Name = "_buttonFeynman";
            _buttonFeynman.Size = new Size(130, 51);
            _buttonFeynman.TabIndex = 8;
            _buttonFeynman.Text = "\U0001f9e0 费曼";
            _buttonFeynman.UseVisualStyleBackColor = false;
            _buttonFeynman.Click += ButtonFeynman_Click;
            // 
            // LearningButtonsView
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(_buttonsPanel);
            Name = "LearningButtonsView";
            Size = new Size(1448, 80);
            _buttonsPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        private void ButtonKnown_Click(object? sender, EventArgs e) => KnownClicked?.Invoke(sender, e);

        private void ButtonUnknown_Click(object? sender, EventArgs e) => UnknownClicked?.Invoke(sender, e);

        private void ButtonPronounce_Click(object? sender, EventArgs e) => PronounceClicked?.Invoke(sender, e);

        private void ButtonFavorite_Click(object? sender, EventArgs e) => FavoriteClicked?.Invoke(sender, e);

        private void ButtonNote_Click(object? sender, EventArgs e) => NoteClicked?.Invoke(sender, e);

        private void ButtonExit_Click(object? sender, EventArgs e) => ExitClicked?.Invoke(sender, e);

        private void ButtonAIAsk_Click(object? sender, EventArgs e) => AIAskClicked?.Invoke(sender, e);

        private void ButtonFeynman_Click(object? sender, EventArgs e) => FeynmanClicked?.Invoke(sender, e);

        #endregion

        #region Public Methods

        /// <summary>
        /// 启用/禁用所有按钮
        /// </summary>
        public void EnableButtons(bool enabled)
        {
            _buttonKnown.Enabled = enabled;
            _buttonUnknown.Enabled = enabled;
            _buttonPronounce.Enabled = enabled;
            _buttonFavorite.Enabled = enabled;
            _buttonNote.Enabled = enabled;
            _buttonExit.Enabled = enabled;
            _buttonAIAsk.Enabled = enabled;
            _buttonFeynman.Enabled = enabled;
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