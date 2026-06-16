using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LearningAssistant.Views
{
    /// <summary>
    /// 学习内容视图 - 中间内容显示区（主内容+详情列表+笔记面板+显示详情复选框）
    /// </summary>
    public class LearningContentView : UserControl
    {
        #region Controls

        private Panel _panelContent = null!;
        private ListBox _listBoxDisplay = null!;
        private Label _labelContent = null!;
        private Panel _panelNotes = null!;
        private RichTextBox _richTextBoxNotes = null!;
        private Label _labelNotesTitle = null!;
        private CheckBox _checkBoxShowDetail = null!;

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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CheckBox CheckBoxShowDetail => _checkBoxShowDetail;

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
        /// 显示详情复选框变更事件
        /// </summary>
        public event EventHandler? DetailCheckChanged;

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
            _checkBoxShowDetail = new CheckBox();

            _panelContent.SuspendLayout();
            _panelNotes.SuspendLayout();
            SuspendLayout();

            //
            // _panelContent
            //
            _panelContent.BackColor = Color.FromArgb(224, 224, 224);
            _panelContent.Controls.Add(_listBoxDisplay);
            _panelContent.Controls.Add(_labelContent);
            _panelContent.Dock = DockStyle.Fill;
            _panelContent.Location = new Point(3, 3);
            _panelContent.Name = "panelContent";
            _panelContent.Size = new Size(1089, 636);
            _panelContent.TabIndex = 0;

            //
            // _listBoxDisplay
            //
            _listBoxDisplay.BackColor = Color.FromArgb(192, 255, 192);
            _listBoxDisplay.Dock = DockStyle.Top;
            _listBoxDisplay.Font = new Font("微软雅黑", 21.75F, FontStyle.Bold, GraphicsUnit.Point, 134);
            _listBoxDisplay.ForeColor = Color.FromArgb(100, 150, 180);
            _listBoxDisplay.Location = new Point(0, 0);
            _listBoxDisplay.Name = "listBoxDisplay";
            _listBoxDisplay.Size = new Size(1089, 160);
            _listBoxDisplay.TabIndex = 1;
            _listBoxDisplay.Visible = false;
            _listBoxDisplay.Click += (s, e) => DetailClicked?.Invoke(s, e);

            //
            // _labelContent
            //
            _labelContent.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _labelContent.BackColor = Color.FromArgb(255, 248, 230);
            _labelContent.Font = new Font("微软雅黑", 48F, FontStyle.Bold, GraphicsUnit.Point, 134);
            _labelContent.ForeColor = Color.FromArgb(50, 60, 80);
            _labelContent.Location = new Point(0, 0);
            _labelContent.Name = "labelContent";
            _labelContent.Size = new Size(1089, 636);
            _labelContent.TabIndex = 0;
            _labelContent.TextAlign = ContentAlignment.MiddleCenter;
            _labelContent.Click += (s, e) => ContentClicked?.Invoke(s, e);

            //
            // _panelNotes
            //
            _panelNotes.BackColor = Color.FromArgb(255, 253, 238);
            _panelNotes.BorderStyle = BorderStyle.FixedSingle;
            _panelNotes.Controls.Add(_richTextBoxNotes);
            _panelNotes.Controls.Add(_labelNotesTitle);
            _panelNotes.Dock = DockStyle.Fill;
            _panelNotes.Location = new Point(3, 645);
            _panelNotes.Name = "panelNotes";
            _panelNotes.Size = new Size(1089, 1);
            _panelNotes.TabIndex = 0;
            _panelNotes.Visible = false;

            //
            // _richTextBoxNotes
            //
            _richTextBoxNotes.BackColor = Color.FromArgb(255, 253, 238);
            _richTextBoxNotes.Dock = DockStyle.Fill;
            _richTextBoxNotes.Font = new Font("微软雅黑", 11F);
            _richTextBoxNotes.ForeColor = Color.FromArgb(60, 80, 100);
            _richTextBoxNotes.Location = new Point(0, 30);
            _richTextBoxNotes.Name = "richTextBoxNotes";
            _richTextBoxNotes.Size = new Size(1087, 0);
            _richTextBoxNotes.TabIndex = 1;
            _richTextBoxNotes.Text = "";
            _richTextBoxNotes.TextChanged += (s, e) => NoteTextChanged?.Invoke(s, e);

            //
            // _labelNotesTitle
            //
            _labelNotesTitle.Dock = DockStyle.Top;
            _labelNotesTitle.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _labelNotesTitle.ForeColor = Color.FromArgb(139, 119, 101);
            _labelNotesTitle.Location = new Point(0, 0);
            _labelNotesTitle.Name = "labelNotesTitle";
            _labelNotesTitle.Padding = new Padding(10, 0, 0, 0);
            _labelNotesTitle.Size = new Size(1087, 30);
            _labelNotesTitle.TabIndex = 0;
            _labelNotesTitle.Text = "📝 我的笔记";
            _labelNotesTitle.TextAlign = ContentAlignment.MiddleLeft;

            //
            // _checkBoxShowDetail
            //
            _checkBoxShowDetail.BackColor = Color.FromArgb(250, 245, 235);
            _checkBoxShowDetail.FlatAppearance.BorderSize = 0;
            _checkBoxShowDetail.FlatAppearance.MouseDownBackColor = Color.FromArgb(66, 165, 70);
            _checkBoxShowDetail.FlatAppearance.MouseOverBackColor = Color.FromArgb(86, 185, 90);
            _checkBoxShowDetail.FlatStyle = FlatStyle.Flat;
            _checkBoxShowDetail.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _checkBoxShowDetail.ForeColor = Color.Black;
            _checkBoxShowDetail.Location = new Point(333, 8);
            _checkBoxShowDetail.Name = "checkBoxShowDetail";
            _checkBoxShowDetail.Size = new Size(132, 27);
            _checkBoxShowDetail.TabIndex = 0;
            _checkBoxShowDetail.Text = "👁️ 显示答案";
            _checkBoxShowDetail.TextAlign = ContentAlignment.MiddleCenter;
            _checkBoxShowDetail.UseVisualStyleBackColor = false;
            _checkBoxShowDetail.CheckedChanged += (s, e) => DetailCheckChanged?.Invoke(s, e);

            _panelContent.ResumeLayout(false);
            _panelNotes.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        #region Public Methods

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

        /// <summary>
        /// 设置显示详情复选框状态
        /// </summary>
        public void SetShowDetailChecked(bool checked)
        {
            _checkBoxShowDetail.Checked = checked;
        }

        #endregion
    }
}