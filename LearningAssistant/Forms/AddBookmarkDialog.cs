namespace LearningAssistant.Forms
{
    public class AddBookmarkDialog : Form
    {
        private readonly string _defaultTitle;
        private readonly string _url;
        private readonly List<string> _existingCategories;

        private TextBox _textBoxTitle = null!;
        private ComboBox _comboBoxCategory = null!;
        private Button _buttonOk = null!;
        private Button _buttonCancel = null!;

        public string BookmarkTitle { get; private set; } = string.Empty;
        public string CategoryName { get; private set; } = string.Empty;

        public AddBookmarkDialog(string defaultTitle, string url, List<string> existingCategories)
        {
            _defaultTitle = defaultTitle;
            _url = url;
            _existingCategories = existingCategories ?? new List<string>();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var labelTitle = new Label
            {
                Text = "书签标题:",
                Location = new Point(20, 20),
                Size = new Size(80, 20)
            };

            _textBoxTitle = new TextBox
            {
                Text = _defaultTitle,
                Location = new Point(20, 43),
                Size = new Size(440, 23),
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            var labelUrl = new Label
            {
                Text = "URL:",
                Location = new Point(20, 75),
                Size = new Size(80, 20)
            };

            var labelUrlValue = new Label
            {
                Text = _url,
                Location = new Point(20, 98),
                Size = new Size(440, 20),
                AutoEllipsis = true,
                ForeColor = Color.Gray
            };

            var labelCategory = new Label
            {
                Text = "分类:",
                Location = new Point(20, 130),
                Size = new Size(80, 20)
            };

            _comboBoxCategory = new ComboBox
            {
                Location = new Point(20, 153),
                Size = new Size(440, 23),
                DropDownStyle = ComboBoxStyle.DropDown,
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            foreach (var category in _existingCategories)
            {
                _comboBoxCategory.Items.Add(category);
            }

            if (_existingCategories.Count > 0)
            {
                _comboBoxCategory.SelectedIndex = 0;
            }

            _buttonOk = new Button
            {
                Text = "确定",
                Location = new Point(280, 200),
                Size = new Size(85, 32),
                DialogResult = DialogResult.OK
            };
            _buttonOk.Click += ButtonOk_Click;

            _buttonCancel = new Button
            {
                Text = "取消",
                Location = new Point(375, 200),
                Size = new Size(85, 32),
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(labelTitle);
            Controls.Add(_textBoxTitle);
            Controls.Add(labelUrl);
            Controls.Add(labelUrlValue);
            Controls.Add(labelCategory);
            Controls.Add(_comboBoxCategory);
            Controls.Add(_buttonOk);
            Controls.Add(_buttonCancel);

            AcceptButton = _buttonOk;
            CancelButton = _buttonCancel;

            ClientSize = new Size(480, 250);
            Text = "添加书签";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
        }

        private void ButtonOk_Click(object? sender, EventArgs e)
        {
            var title = _textBoxTitle.Text.Trim();
            var category = _comboBoxCategory.Text.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("请输入书签标题", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _textBoxTitle.Focus();
                DialogResult = DialogResult.None;
                return;
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                MessageBox.Show("请输入或选择分类", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _comboBoxCategory.Focus();
                DialogResult = DialogResult.None;
                return;
            }

            BookmarkTitle = title;
            CategoryName = category;
        }
    }
}
