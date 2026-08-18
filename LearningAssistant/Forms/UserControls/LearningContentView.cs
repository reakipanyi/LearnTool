using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls
{
    /// <summary>
    /// 学习内容视图 - 中间内容显示区（主内容+详情列表+笔记面板+显示详情复选框）
    /// </summary>
    public class LearningContentView : UserControl, IThemeable
    {
        #region Controls

        private Panel _panelContent = null!;
        private ListBox _listBoxDisplay = null!;
        private Label _labelContent = null!;
        private Panel _panelNotes = null!;
        private RichTextBox _richTextBoxNotes = null!;
        private Label _labelNotesTitle = null!;

        #endregion

        #region Public Properties

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Panel PanelContent => _panelContent;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ListBox ListBoxDisplay => _listBoxDisplay;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label LabelContent => _labelContent;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Panel PanelNotes => _panelNotes;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RichTextBox RichTextBoxNotes => _richTextBoxNotes;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label LabelNotesTitle => _labelNotesTitle;

        #endregion

        #region Events

        /// <summary>
        /// 内容点击事件
        /// </summary>
        public event EventHandler? ContentClicked;

        /// <summary>
        /// 详情列表点击事件
        /// </summary>
        public event EventHandler? DetailClicked;

        /// <summary>
        /// 笔记面板切换事件
        /// </summary>
        public event EventHandler? NoteToggleClicked;

        /// <summary>
        /// 笔记文本变更事件
        /// </summary>
        public event EventHandler? NoteTextChanged;

        #endregion

        #region Initialization

        public LearningContentView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _panelContent = new Panel();
            _listBoxDisplay = new ListBox();
            _labelContent = new Label();
            _panelNotes = new Panel();
            _richTextBoxNotes = new RichTextBox();
            _labelNotesTitle = new Label();
            _panelContent.SuspendLayout();
            _panelNotes.SuspendLayout();
            SuspendLayout();
            // 
            // _panelContent
            // 
            _panelContent.BackColor = Color.White;
            _panelContent.Controls.Add(_listBoxDisplay);
            _panelContent.Controls.Add(_labelContent);
            _panelContent.Dock = DockStyle.Fill;
            _panelContent.Location = new Point(0, 0);
            _panelContent.Name = "_panelContent";
            _panelContent.Size = new Size(954, 225);
            _panelContent.TabIndex = 0;
            // 
            // _listBoxDisplay
            // 
            _listBoxDisplay.BackColor = Color.White;
            _listBoxDisplay.Dock = DockStyle.Top;
            _listBoxDisplay.Font = new Font("微软雅黑", 21.75F, FontStyle.Bold, GraphicsUnit.Point, 134);
            _listBoxDisplay.ForeColor = Color.Black;
            _listBoxDisplay.Location = new Point(0, 0);
            _listBoxDisplay.Name = "_listBoxDisplay";
            _listBoxDisplay.Size = new Size(954, 160);
            _listBoxDisplay.TabIndex = 1;
            _listBoxDisplay.Visible = false;
            _listBoxDisplay.Click += ListBoxDisplay_Click;
            // 
            // _labelContent
            // 
            _labelContent.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _labelContent.BackColor = Color.Transparent;
            _labelContent.Font = new Font("微软雅黑", 48F, FontStyle.Bold, GraphicsUnit.Point, 134);
            _labelContent.ForeColor = Color.Black;
            _labelContent.Location = new Point(0, 0);
            _labelContent.Name = "_labelContent";
            _labelContent.Size = new Size(954, 225);
            _labelContent.TabIndex = 0;
            _labelContent.TextAlign = ContentAlignment.MiddleCenter;
            _labelContent.Click += LabelContent_Click;
            // 
            // _panelNotes
            // 
            _panelNotes.BackColor = Color.White;
            _panelNotes.BorderStyle = BorderStyle.None;
            _panelNotes.Controls.Add(_richTextBoxNotes);
            _panelNotes.Controls.Add(_labelNotesTitle);
            _panelNotes.Dock = DockStyle.Fill;
            _panelNotes.Location = new Point(0, 0);
            _panelNotes.Name = "_panelNotes";
            _panelNotes.Size = new Size(954, 225);
            _panelNotes.TabIndex = 0;
            _panelNotes.Visible = false;
            // 
            // _richTextBoxNotes
            // 
            _richTextBoxNotes.BackColor = Color.White;
            _richTextBoxNotes.BorderStyle = BorderStyle.None;
            _richTextBoxNotes.Dock = DockStyle.Fill;
            _richTextBoxNotes.Font = new Font("微软雅黑", 11F);
            _richTextBoxNotes.ForeColor = Color.Black;
            _richTextBoxNotes.Location = new Point(0, 34);
            _richTextBoxNotes.Name = "_richTextBoxNotes";
            _richTextBoxNotes.Size = new Size(952, 189);
            _richTextBoxNotes.TabIndex = 1;
            _richTextBoxNotes.Text = "";
            _richTextBoxNotes.TextChanged += RichTextBoxNotes_TextChanged;
            // 
            // _labelNotesTitle
            // 
            _labelNotesTitle.Dock = DockStyle.Top;
            _labelNotesTitle.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _labelNotesTitle.ForeColor = Color.Black;
            _labelNotesTitle.Location = new Point(0, 0);
            _labelNotesTitle.Name = "_labelNotesTitle";
            _labelNotesTitle.Padding = new Padding(10, 0, 0, 0);
            _labelNotesTitle.Size = new Size(952, 34);
            _labelNotesTitle.TabIndex = 0;
            _labelNotesTitle.Text = "📝 我的笔记";
            _labelNotesTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // LearningContentView
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(_panelNotes);
            Controls.Add(_panelContent);
            Name = "LearningContentView";
            Size = new Size(954, 225);
            _panelContent.ResumeLayout(false);
            _panelNotes.ResumeLayout(false);
            ResumeLayout(false);
        }

        private void ListBoxDisplay_Click(object? sender, EventArgs e) => DetailClicked?.Invoke(sender, e);

        private void LabelContent_Click(object? sender, EventArgs e) => ContentClicked?.Invoke(sender, e);

        private void RichTextBoxNotes_TextChanged(object? sender, EventArgs e) => NoteTextChanged?.Invoke(sender, e);

        #endregion

        #region Public Methods

        /// <summary>
        /// 应用主题颜色（夜间模式下中间内容区背景与文字随主题切换）
        /// </summary>
        public void ApplyTheme(ThemeColors colors)
        {
            _panelContent.BackColor = colors.Surface;
            _listBoxDisplay.BackColor = colors.Surface;
            _listBoxDisplay.ForeColor = colors.TextPrimary;
            _labelContent.ForeColor = colors.TextPrimary;
            _panelNotes.BackColor = colors.Surface;
            _richTextBoxNotes.BackColor = colors.Surface;
            _richTextBoxNotes.ForeColor = colors.TextPrimary;
            _labelNotesTitle.ForeColor = colors.TextPrimary;
        }

        /// <summary>
        /// 设置主内容文本
        /// </summary>
        public void SetContent(string text)
        {
            _labelContent.Text = text;
        }

        /// <summary>
        /// 设置详情列表项
        /// </summary>
        public void SetDetailItems(List<string> items)
        {
            _listBoxDisplay.Items.Clear();
            foreach (var item in items)
            {
                _listBoxDisplay.Items.Add(item);
            }
        }

        /// <summary>
        /// 显示/隐藏详情列表
        /// </summary>
        public void ShowDetail(bool show)
        {
            _listBoxDisplay.Visible = show;
        }

        /// <summary>
        /// 显示/隐藏笔记面板
        /// </summary>
        public void ShowNotes(bool show)
        {
            _panelNotes.Visible = show;
        }

        /// <summary>
        /// 设置笔记文本
        /// </summary>
        public void SetNotes(string text)
        {
            _richTextBoxNotes.Text = text;
        }

        /// <summary>
        /// 获取笔记文本
        /// </summary>
        public string GetNotes()
        {
            return _richTextBoxNotes.Text;
        }

        #endregion
    }
}