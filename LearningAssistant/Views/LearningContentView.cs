using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LearningAssistant.Views
{
    /// <summary>
    /// 学习内容视图 - 主内容显示区、详情面板、笔记区
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

        #region Public Controls (供 LearningForm 访问)

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
        public CheckBox CheckBoxShowDetail => _checkBoxShowDetail;

        #endregion

        #region Events

        /// <summary>点击主内容区域事件</summary>
        public event EventHandler? ContentClicked;

        /// <summary>点击详情列表事件</summary>
        public event EventHandler? DetailClicked;

        /// <summary>显示详情复选框状态变更事件</summary>
        public event EventHandler? DetailCheckChanged;

        /// <summary>点击笔记按钮事件（切换笔记区显示）</summary>
        public event EventHandler? NoteToggleClicked;

        /// <summary>笔记内容变更事件</summary>
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

            SuspendLayout();

            // 
            // _panelContent
            // 
            _panelContent.BackColor = Color.FromArgb(224, 224, 224);
            _panelContent.Controls.Add(_listBoxDisplay);
            _panelContent.Controls.Add(_labelContent);
            _panelContent.Dock = DockStyle.Fill;
            _panelContent.Location = new Point(0, 0);
            _panelContent.Name = "_panelContent";
            _panelContent.Padding = new Padding(3);
            _panelContent.Size = new Size(1089, 636);
            _panelContent.TabIndex = 0;
            _panelContent.Paint += OnPanelContentPaint;

            // 
            // _listBoxDisplay
            // 
            _listBoxDisplay.BackColor = Color.FromArgb(192, 255, 192);
            _listBoxDisplay.Dock = DockStyle.Top;
            _listBoxDisplay.Font = new Font("微软雅黑", 21.75F, FontStyle.Bold, GraphicsUnit.Point, 134);
            _listBoxDisplay.ForeColor = Color.FromArgb(100, 150, 180);
            _listBoxDisplay.Location = new Point(3, 3);
            _listBoxDisplay.Name = "_listBoxDisplay";
            _listBoxDisplay.Size = new Size(1083, 160);
            _listBoxDisplay.TabIndex = 1;
            _listBoxDisplay.Visible = false;
            _listBoxDisplay.Click += (s, e) => DetailClicked?.Invoke(this, e);

            // 
            // _labelContent
            // 
            _labelContent.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _labelContent.BackColor = Color.FromArgb(255, 248, 230);
            _labelContent.Font = new Font("微软雅黑", 48F, FontStyle.Bold, GraphicsUnit.Point, 134);
            _labelContent.ForeColor = Color.FromArgb(50, 60, 80);
            _labelContent.Location = new Point(3, 3);
            _labelContent.Name = "_labelContent";
            _labelContent.Size = new Size(1083, 630);
            _labelContent.TabIndex = 0;
            _labelContent.TextAlign = ContentAlignment.MiddleCenter;
            _labelContent.Click += (s, e) => ContentClicked?.Invoke(this, e);

            // 
            // _panelNotes
            // 
            _panelNotes.BackColor = Color.FromArgb(250, 250, 245);
            _panelNotes.BorderStyle = BorderStyle.FixedSingle;
            _panelNotes.Controls.Add(_labelNotesTitle);
            _panelNotes.Controls.Add(_richTextBoxNotes);
            _panelNotes.Name = "_panelNotes";
            _panelNotes.Size = new Size(600, 150);
            _panelNotes.TabIndex = 10;
            _panelNotes.Visible = false;

            // 
            // _labelNotesTitle
            // 
            _labelNotesTitle.BackColor = Color.FromArgb(76, 175, 80);
            _labelNotesTitle.Dock = DockStyle.Top;
            _labelNotesTitle.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _labelNotesTitle.ForeColor = Color.White;
            _labelNotesTitle.Location = new Point(0, 0);
            _labelNotesTitle.Name = "_labelNotesTitle";
            _labelNotesTitle.Size = new Size(598, 25);
            _labelNotesTitle.TabIndex = 0;
            _labelNotesTitle.Text = "📝 笔记";
            _labelNotesTitle.TextAlign = ContentAlignment.MiddleCenter;

            // 
            // _richTextBoxNotes
            // 
            _richTextBoxNotes.Dock = DockStyle.Fill;
            _richTextBoxNotes.Font = new Font("微软雅黑", 10F);
            _richTextBoxNotes.Location = new Point(0, 25);
            _richTextBoxNotes.Name = "_richTextBoxNotes";
            _richTextBoxNotes.Size = new Size(596, 123);
            _richTextBoxNotes.TabIndex = 1;
            _richTextBoxNotes.Text = "";
            _richTextBoxNotes.TextChanged += (s, e) => NoteTextChanged?.Invoke(this, e);

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
            _checkBoxShowDetail.Name = "_checkBoxShowDetail";
            _checkBoxShowDetail.Size = new Size(132, 27);
            _checkBoxShowDetail.TabIndex = 10;
            _checkBoxShowDetail.Text = "👁️ 显示答案";
            _checkBoxShowDetail.TextAlign = ContentAlignment.MiddleCenter;
            _checkBoxShowDetail.UseVisualStyleBackColor = false;
            _checkBoxShowDetail.CheckedChanged += (s, e) => DetailCheckChanged?.Invoke(this, e);

            Controls.Add(_panelContent);

            ResumeLayout(false);
        }

        #endregion

        #region Public API

        /// <summary>应用主题色</summary>
        public void ApplyTheme(Color backColor, Color foreColor, Color accentColor)
        {
            _panelContent.BackColor = Color.FromArgb(
                Math.Min(backColor.R + 30, 255),
                Math.Min(backColor.G + 30, 255),
                Math.Min(backColor.B + 30, 255));

            _labelContent.BackColor = backColor;
            _labelContent.ForeColor = foreColor;

            _checkBoxShowDetail.BackColor = backColor;
            _checkBoxShowDetail.ForeColor = foreColor;

            _panelNotes.BackColor = Color.FromArgb(
                Math.Min(backColor.R + 20, 255),
                Math.Min(backColor.G + 20, 255),
                Math.Min(backColor.B + 20, 255));
        }

        /// <summary>设置显示详情复选框是否勾选</summary>
        public void SetDetailChecked(bool isChecked)
        {
            _checkBoxShowDetail.CheckedChanged -= DetailCheckChanged;
            _checkBoxShowDetail.Checked = isChecked;
            _checkBoxShowDetail.CheckedChanged += (s, e) => DetailCheckChanged?.Invoke(this, e);
        }

        #endregion

        #region Private Paint Handler

        private void OnPanelContentPaint(object? sender, PaintEventArgs e)
        {
            // 空实现，保留占位符供将来扩展
        }

        #endregion
    }
}
