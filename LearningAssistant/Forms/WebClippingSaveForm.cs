using LearningAssistant.Common;
using System.ComponentModel;

namespace LearningAssistant.Forms
{
    public partial class WebClippingSaveForm : Form
    {
        private Label _labelTitle = null!;
        private Label _labelContent = null!;
        private TextBox _textBoxContent = null!;
        private Label _labelSubject = null!;
        private ComboBox _comboBoxSubject = null!;
        private Label _labelSubCategory = null!;
        private ComboBox _comboBoxSubCategory = null!;
        private Label _labelSource = null!;
        private Label _labelSourceValue = null!;
        private Label _labelUrl = null!;
        private Label _labelUrlValue = null!;
        private Button _buttonSave = null!;
        private Button _buttonCancel = null!;

        private readonly string _selectedText;
        private readonly string? _pageTitle;
        private readonly string? _pageUrl;

        public string SelectedSubject => _comboBoxSubject.SelectedItem?.ToString() ?? "语文综合";
        public string SelectedSubCategory => _comboBoxSubCategory.SelectedItem?.ToString() ?? "语文综合";
        public string Content => _textBoxContent.Text;
        public string SourceTitle => _pageTitle ?? string.Empty;
        public string SourceUrl => _pageUrl ?? string.Empty;

        private static readonly Dictionary<string, string[]> SubjectCategories = new()
        {
            ["语文"] = new[] { "识字", "短语", "成语", "诗词", "语文综合" },
            ["英语"] = new[] { "英语单词", "英语短语", "英语句子", "英语综合" },
            ["数学"] = new[] { "公式定理", "例题解析", "概念定义", "数学综合" },
            ["物理"] = new[] { "物理定律", "实验原理", "公式推导", "物理综合" },
            ["化学"] = new[] { "化学方程式", "元素性质", "实验操作", "化学综合" },
            ["历史"] = new[] { "历史事件", "人物传记", "年代记忆", "历史综合" },
            ["地理"] = new[] { "地理知识", "地图解读", "地理综合" },
            ["生物"] = new[] { "生物知识", "实验观察", "生物综合" }
        };

        public WebClippingSaveForm(string selectedText, string? pageTitle = null, string? pageUrl = null)
        {
            _selectedText = selectedText;
            _pageTitle = pageTitle;
            _pageUrl = pageUrl;
            InitializeComponent();
            InitializeSubjects();
        }

        private void InitializeComponent()
        {
            _labelTitle = new Label();
            _labelContent = new Label();
            _textBoxContent = new TextBox();
            _labelSubject = new Label();
            _comboBoxSubject = new ComboBox();
            _labelSubCategory = new Label();
            _comboBoxSubCategory = new ComboBox();
            _labelSource = new Label();
            _labelSourceValue = new Label();
            _labelUrl = new Label();
            _labelUrlValue = new Label();
            _buttonSave = new Button();
            _buttonCancel = new Button();

            SuspendLayout();

            _labelTitle.Text = "📥 保存为学习卡片";
            _labelTitle.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            _labelTitle.ForeColor = Color.FromArgb(51, 51, 51);
            _labelTitle.Location = new Point(20, 20);
            _labelTitle.Size = new Size(400, 30);

            _labelContent.Text = "内容";
            _labelContent.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _labelContent.ForeColor = Color.FromArgb(102, 102, 102);
            _labelContent.Location = new Point(20, 60);
            _labelContent.Size = new Size(60, 20);

            _textBoxContent.Text = _selectedText;
            _textBoxContent.Multiline = true;
            _textBoxContent.ScrollBars = ScrollBars.Vertical;
            _textBoxContent.Font = new Font("微软雅黑", 9F);
            _textBoxContent.Location = new Point(20, 85);
            _textBoxContent.Size = new Size(460, 100);
            _textBoxContent.BorderStyle = BorderStyle.FixedSingle;

            _labelSubject.Text = "学科";
            _labelSubject.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _labelSubject.ForeColor = Color.FromArgb(102, 102, 102);
            _labelSubject.Location = new Point(20, 195);
            _labelSubject.Size = new Size(60, 20);

            _comboBoxSubject.DropDownStyle = ComboBoxStyle.DropDownList;
            _comboBoxSubject.Font = new Font("微软雅黑", 9F);
            _comboBoxSubject.Location = new Point(80, 192);
            _comboBoxSubject.Size = new Size(180, 28);
            _comboBoxSubject.SelectedIndexChanged += ComboBoxSubject_SelectedIndexChanged;

            _labelSubCategory.Text = "分类";
            _labelSubCategory.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _labelSubCategory.ForeColor = Color.FromArgb(102, 102, 102);
            _labelSubCategory.Location = new Point(280, 195);
            _labelSubCategory.Size = new Size(60, 20);

            _comboBoxSubCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            _comboBoxSubCategory.Font = new Font("微软雅黑", 9F);
            _comboBoxSubCategory.Location = new Point(340, 192);
            _comboBoxSubCategory.Size = new Size(140, 28);

            _labelSource.Text = "来源";
            _labelSource.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _labelSource.ForeColor = Color.FromArgb(102, 102, 102);
            _labelSource.Location = new Point(20, 235);
            _labelSource.Size = new Size(60, 20);

            _labelSourceValue.Text = string.IsNullOrEmpty(_pageTitle) ? "未知页面" : _pageTitle;
            _labelSourceValue.Font = new Font("微软雅黑", 8.5F);
            _labelSourceValue.ForeColor = Color.FromArgb(153, 153, 153);
            _labelSourceValue.Location = new Point(80, 237);
            _labelSourceValue.Size = new Size(400, 18);
            _labelSourceValue.AutoEllipsis = true;

            _labelUrl.Text = "链接";
            _labelUrl.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _labelUrl.ForeColor = Color.FromArgb(102, 102, 102);
            _labelUrl.Location = new Point(20, 260);
            _labelUrl.Size = new Size(60, 20);

            _labelUrlValue.Text = string.IsNullOrEmpty(_pageUrl) ? "-" : _pageUrl;
            _labelUrlValue.Font = new Font("微软雅黑", 8.5F);
            _labelUrlValue.ForeColor = Color.FromArgb(153, 153, 153);
            _labelUrlValue.Location = new Point(80, 262);
            _labelUrlValue.Size = new Size(400, 18);
            _labelUrlValue.AutoEllipsis = true;

            _buttonSave.Text = "💾 保存";
            _buttonSave.FlatStyle = FlatStyle.Flat;
            _buttonSave.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _buttonSave.ForeColor = Color.White;
            _buttonSave.BackColor = Color.FromArgb(108, 92, 231);
            _buttonSave.Location = new Point(280, 300);
            _buttonSave.Size = new Size(100, 36);
            _buttonSave.FlatAppearance.BorderSize = 0;
            _buttonSave.Cursor = Cursors.Hand;
            _buttonSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_textBoxContent.Text))
                {
                    MessageBox.Show("内容不能为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };

            _buttonCancel.Text = "取消";
            _buttonCancel.FlatStyle = FlatStyle.Flat;
            _buttonCancel.Font = new Font("微软雅黑", 10F);
            _buttonCancel.ForeColor = Color.FromArgb(102, 102, 102);
            _buttonCancel.BackColor = Color.FromArgb(245, 245, 248);
            _buttonCancel.Location = new Point(390, 300);
            _buttonCancel.Size = new Size(90, 36);
            _buttonCancel.FlatAppearance.BorderSize = 0;
            _buttonCancel.Cursor = Cursors.Hand;
            _buttonCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.Add(_buttonCancel);
            Controls.Add(_buttonSave);
            Controls.Add(_labelUrlValue);
            Controls.Add(_labelUrl);
            Controls.Add(_labelSourceValue);
            Controls.Add(_labelSource);
            Controls.Add(_comboBoxSubCategory);
            Controls.Add(_labelSubCategory);
            Controls.Add(_comboBoxSubject);
            Controls.Add(_labelSubject);
            Controls.Add(_textBoxContent);
            Controls.Add(_labelContent);
            Controls.Add(_labelTitle);

            Text = "保存剪藏";
            ClientSize = new Size(500, 350);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Font = new Font("微软雅黑", 9F);

            ResumeLayout(false);
        }

        private void InitializeSubjects()
        {
            _comboBoxSubject.Items.Clear();
            foreach (var subject in SubjectCategories.Keys)
            {
                _comboBoxSubject.Items.Add(subject);
            }
            if (_comboBoxSubject.Items.Count > 0)
                _comboBoxSubject.SelectedIndex = 0;
        }

        private void ComboBoxSubject_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string? selected = _comboBoxSubject.SelectedItem?.ToString();
            _comboBoxSubCategory.Items.Clear();

            if (selected != null && SubjectCategories.TryGetValue(selected, out var categories))
            {
                foreach (var cat in categories)
                {
                    _comboBoxSubCategory.Items.Add(cat);
                }
                if (_comboBoxSubCategory.Items.Count > 0)
                    _comboBoxSubCategory.SelectedIndex = 0;
            }
        }
    }
}
