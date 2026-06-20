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
        }

        private void InitializeComponent()
        {
            _buttonsPanel = new FlowLayoutPanel();
            _buttonKnown = new Button();
            _buttonUnknown = new Button();
            _buttonPronounce = new Button();
            _buttonFavorite = new Button();
            _buttonNote = new Button();
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
            _buttonsPanel.Controls.Add(_buttonPronounce);
            _buttonsPanel.Controls.Add(_buttonFavorite);
            _buttonsPanel.Controls.Add(_buttonNote);
            _buttonsPanel.Controls.Add(_buttonExit);
            _buttonsPanel.Controls.Add(_buttonAIAsk);
            _buttonsPanel.Controls.Add(_buttonFeynman);
            _buttonsPanel.Dock = DockStyle.Fill;
            _buttonsPanel.Location = new Point(3, 645);
            _buttonsPanel.Name = "buttonsFlowLayoutPanel";
            _buttonsPanel.Padding = new Padding(10, 5, 10, 5);
            _buttonsPanel.Size = new Size(1089, 65);
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
            _buttonKnown.Location = new Point(15, 10);
            _buttonKnown.Margin = new Padding(5);
            _buttonKnown.Name = "buttonKnown";
            _buttonKnown.Size = new Size(130, 45);
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
            _buttonUnknown.Location = new Point(155, 10);
            _buttonUnknown.Margin = new Padding(5);
            _buttonUnknown.Name = "buttonUnknown";
            _buttonUnknown.Size = new Size(130, 45);
            _buttonUnknown.TabIndex = 1;
            _buttonUnknown.Text = "❌ 不会 [U/2]";
            _buttonUnknown.UseVisualStyleBackColor = false;
            _buttonUnknown.Click += ButtonUnknown_Click;


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
            _buttonPronounce.Location = new Point(295, 10);
            _buttonPronounce.Margin = new Padding(5);
            _buttonPronounce.Name = "buttonPronounce";
            _buttonPronounce.Size = new Size(140, 45);
            _buttonPronounce.TabIndex = 2;
            _buttonPronounce.Text = "🔊 发音 [Space]";
            _buttonPronounce.UseVisualStyleBackColor = false;
            _buttonPronounce.Click += ButtonPronounce_Click;

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
            _buttonFavorite.Location = new Point(605, 10);
            _buttonFavorite.Margin = new Padding(5);
            _buttonFavorite.Name = "buttonFavorite";
            _buttonFavorite.Size = new Size(105, 45);
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
            _buttonNote.Location = new Point(720, 10);
            _buttonNote.Margin = new Padding(5);
            _buttonNote.Name = "buttonNote";
            _buttonNote.Size = new Size(105, 45);
            _buttonNote.TabIndex = 5;
            _buttonNote.Text = "📝 笔记";
            _buttonNote.UseVisualStyleBackColor = false;
            _buttonNote.Click += ButtonNote_Click;

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
            _buttonExit.Location = new Point(675, 10);
            _buttonExit.Margin = new Padding(5);
            _buttonExit.Name = "buttonExit";
            _buttonExit.Size = new Size(105, 45);
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
            _buttonAIAsk.Location = new Point(950, 10);
            _buttonAIAsk.Margin = new Padding(5);
            _buttonAIAsk.Name = "buttonAIAsk";
            _buttonAIAsk.Size = new Size(105, 45);
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
            _buttonFeynman.Location = new Point(1065, 10);
            _buttonFeynman.Margin = new Padding(5);
            _buttonFeynman.Name = "buttonFeynman";
            _buttonFeynman.Size = new Size(105, 45);
            _buttonFeynman.TabIndex = 8;
            _buttonFeynman.Text = "🧠 费曼";
            _buttonFeynman.UseVisualStyleBackColor = false;
            _buttonFeynman.Click += ButtonFeynman_Click;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(_buttonsPanel);  // 关键：将 _buttonsPanel 添加到 Controls
            Name = "LearningButtonsView";
            Size = new Size(1095, 71);
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