using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LearningAssistant.Views
{
    /// <summary>
    /// 学习按钮视图 - 底部操作按钮区
    /// </summary>
    public class LearningButtonsView : UserControl
    {
        #region Controls

        private FlowLayoutPanel _buttonsFlowLayoutPanel = null!;
        private Button _buttonKnown = null!;
        private Button _buttonUnknown = null!;
        private Button _buttonNext = null!;
        private Button _buttonPronounce = null!;
        private Button _buttonFavorite = null!;
        private Button _buttonNote = null!;
        private Button _buttonExit = null!;
        private Button _buttonAIAsk = null!;

        #endregion

        #region Public Controls

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FlowLayoutPanel ButtonsPanel => _buttonsFlowLayoutPanel;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonKnown => _buttonKnown;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonUnknown => _buttonUnknown;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonNext => _buttonNext;

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

        #endregion

        #region Events

        /// <summary>已知按钮点击</summary>
        public event EventHandler? KnownClicked;

        /// <summary>未知按钮点击</summary>
        public event EventHandler? UnknownClicked;

        /// <summary>下一个按钮点击</summary>
        public event EventHandler? NextClicked;

        /// <summary>发音按钮点击</summary>
        public event EventHandler? PronounceClicked;

        /// <summary>收藏按钮点击</summary>
        public event EventHandler? FavoriteClicked;

        /// <summary>笔记按钮点击</summary>
        public event EventHandler? NoteClicked;

        /// <summary>退出按钮点击</summary>
        public event EventHandler? ExitClicked;

        /// <summary>AI问答按钮点击</summary>
        public event EventHandler? AIAskClicked;

        #endregion

        #region Initialization

        public LearningButtonsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _buttonsFlowLayoutPanel = new FlowLayoutPanel();
            _buttonKnown = new Button();
            _buttonUnknown = new Button();
            _buttonNext = new Button();
            _buttonPronounce = new Button();
            _buttonFavorite = new Button();
            _buttonNote = new Button();
            _buttonExit = new Button();
            _buttonAIAsk = new Button();

            SuspendLayout();

            // 
            // _buttonsFlowLayoutPanel
            // 
            _buttonsFlowLayoutPanel.Controls.Add(_buttonKnown);
            _buttonsFlowLayoutPanel.Controls.Add(_buttonUnknown);
            _buttonsFlowLayoutPanel.Controls.Add(_buttonNext);
            _buttonsFlowLayoutPanel.Controls.Add(_buttonPronounce);
            _buttonsFlowLayoutPanel.Controls.Add(_buttonFavorite);
            _buttonsFlowLayoutPanel.Controls.Add(_buttonNote);
            _buttonsFlowLayoutPanel.Controls.Add(_buttonExit);
            _buttonsFlowLayoutPanel.Controls.Add(_buttonAIAsk);
            _buttonsFlowLayoutPanel.Dock = DockStyle.Fill;
            _buttonsFlowLayoutPanel.Location = new Point(0, 0);
            _buttonsFlowLayoutPanel.Name = "_buttonsFlowLayoutPanel";
            _buttonsFlowLayoutPanel.Padding = new Padding(10, 5, 10, 5);
            _buttonsFlowLayoutPanel.Size = new Size(1089, 65);
            _buttonsFlowLayoutPanel.TabIndex = 4;
            _buttonsFlowLayoutPanel.WrapContents = false;

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
            _buttonKnown.Name = "_buttonKnown";
            _buttonKnown.Size = new Size(130, 45);
            _buttonKnown.TabIndex = 4;
            _buttonKnown.Text = "✅ 会了 [K/1]";
            _buttonKnown.UseVisualStyleBackColor = false;
            _buttonKnown.Click += (s, e) => KnownClicked?.Invoke(this, e);

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
            _buttonUnknown.Name = "_buttonUnknown";
            _buttonUnknown.Size = new Size(130, 45);
            _buttonUnknown.TabIndex = 5;
            _buttonUnknown.Text = "❌ 不会 [U/2]";
            _buttonUnknown.UseVisualStyleBackColor = false;
            _buttonUnknown.Click += (s, e) => UnknownClicked?.Invoke(this, e);

            // 
            // _buttonNext
            // 
            _buttonNext.BackColor = Color.FromArgb(33, 150, 243);
            _buttonNext.FlatAppearance.BorderSize = 0;
            _buttonNext.FlatAppearance.MouseDownBackColor = Color.FromArgb(23, 140, 233);
            _buttonNext.FlatAppearance.MouseOverBackColor = Color.FromArgb(43, 160, 253);
            _buttonNext.FlatStyle = FlatStyle.Flat;
            _buttonNext.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _buttonNext.ForeColor = Color.White;
            _buttonNext.Location = new Point(295, 10);
            _buttonNext.Margin = new Padding(5);
            _buttonNext.Name = "_buttonNext";
            _buttonNext.Size = new Size(150, 45);
            _buttonNext.TabIndex = 6;
            _buttonNext.Text = "➡ 下一个 [Enter]";
            _buttonNext.UseVisualStyleBackColor = false;
            _buttonNext.Click += (s, e) => NextClicked?.Invoke(this, e);

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
            _buttonPronounce.Location = new Point(455, 10);
            _buttonPronounce.Margin = new Padding(5);
            _buttonPronounce.Name = "_buttonPronounce";
            _buttonPronounce.Size = new Size(140, 45);
            _buttonPronounce.TabIndex = 7;
            _buttonPronounce.Text = "🔊 发音 [Space]";
            _buttonPronounce.UseVisualStyleBackColor = false;
            _buttonPronounce.Click += (s, e) => PronounceClicked?.Invoke(this, e);

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
            _buttonFavorite.Name = "_buttonFavorite";
            _buttonFavorite.Size = new Size(105, 45);
            _buttonUnknown.TabIndex = 12;
            _buttonFavorite.Text = "⭐ 收藏";
            _buttonFavorite.UseVisualStyleBackColor = false;
            _buttonFavorite.Click += (s, e) => FavoriteClicked?.Invoke(this, e);

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
            _buttonNote.Name = "_buttonNote";
            _buttonNote.Size = new Size(105, 45);
            _buttonNote.TabIndex = 13;
            _buttonNote.Text = "📝 笔记";
            _buttonNote.UseVisualStyleBackColor = false;
            _buttonNote.Click += (s, e) => NoteClicked?.Invoke(this, e);

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
            _buttonExit.Location = new Point(835, 10);
            _buttonExit.Margin = new Padding(5);
            _buttonExit.Name = "_buttonExit";
            _buttonExit.Size = new Size(105, 45);
            _buttonExit.TabIndex = 8;
            _buttonExit.Text = "🏠 返回";
            _buttonExit.UseVisualStyleBackColor = false;
            _buttonExit.Click += (s, e) => ExitClicked?.Invoke(this, e);

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
            _buttonAIAsk.Name = "_buttonAIAsk";
            _buttonAIAsk.Size = new Size(105, 45);
            _buttonAIAsk.TabIndex = 14;
            _buttonAIAsk.Text = "🤖 AI问答";
            _buttonAIAsk.UseVisualStyleBackColor = false;
            _buttonAIAsk.Click += (s, e) => AIAskClicked?.Invoke(this, e);

            Controls.Add(_buttonsFlowLayoutPanel);

            ResumeLayout(false);
        }

        #endregion

        #region Public API

        /// <summary>应用主题色</summary>
        public void ApplyTheme(Color backColor)
        {
            _buttonsFlowLayoutPanel.BackColor = backColor;
        }

        /// <summary>启用/禁用所有按钮</summary>
        public void SetButtonsEnabled(bool enabled)
        {
            _buttonKnown.Enabled = enabled;
            _buttonUnknown.Enabled = enabled;
            _buttonNext.Enabled = enabled;
            _buttonPronounce.Enabled = enabled;
            _buttonFavorite.Enabled = enabled;
            _buttonNote.Enabled = enabled;
            _buttonExit.Enabled = enabled;
            _buttonAIAsk.Enabled = enabled;
        }

        /// <summary>设置收藏按钮文本</summary>
        public void SetFavoriteButtonText(string text) => _buttonFavorite.Text = text;

        #endregion
    }
}
