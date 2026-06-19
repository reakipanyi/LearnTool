namespace LearningAssistant.Forms.Bookmark
{
    public partial class AddBookmarkDialog : Form
    {
        private readonly string _defaultTitle;
        private readonly string _url;
        private readonly List<string> _existingCategories;

        public string BookmarkTitle { get; private set; } = string.Empty;
        public string CategoryName { get; private set; } = string.Empty;

        public AddBookmarkDialog(string defaultTitle, string url, List<string> existingCategories)
        {
            _defaultTitle = defaultTitle;
            _url = url;
            _existingCategories = existingCategories ?? new List<string>();
            InitializeComponent();
            InitializeDynamicData();
        }

        private void InitializeDynamicData()
        {
            _textBoxTitle.Text = _defaultTitle;
            _labelUrlValue.Text = _url;

            foreach (var category in _existingCategories)
            {
                _comboBoxCategory.Items.Add(category);
            }

            if (_existingCategories.Count > 0)
            {
                _comboBoxCategory.SelectedIndex = 0;
            }
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

        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this._labelTitle = new Label();
            this._textBoxTitle = new TextBox();
            this._labelUrl = new Label();
            this._labelUrlValue = new Label();
            this._labelCategory = new Label();
            this._comboBoxCategory = new ComboBox();
            this._buttonOk = new Button();
            this._buttonCancel = new Button();
            this.SuspendLayout();
            //
            // _labelTitle
            //
            this._labelTitle.AutoSize = true;
            this._labelTitle.Location = new System.Drawing.Point(20, 20);
            this._labelTitle.Name = "_labelTitle";
            this._labelTitle.Size = new System.Drawing.Size(68, 17);
            this._labelTitle.TabIndex = 0;
            this._labelTitle.Text = "书签标题:";
            //
            // _textBoxTitle
            //
            this._textBoxTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this._textBoxTitle.Location = new System.Drawing.Point(20, 43);
            this._textBoxTitle.Name = "_textBoxTitle";
            this._textBoxTitle.Size = new System.Drawing.Size(440, 23);
            this._textBoxTitle.TabIndex = 1;
            //
            // _labelUrl
            //
            this._labelUrl.AutoSize = true;
            this._labelUrl.Location = new System.Drawing.Point(20, 75);
            this._labelUrl.Name = "_labelUrl";
            this._labelUrl.Size = new System.Drawing.Size(39, 17);
            this._labelUrl.TabIndex = 2;
            this._labelUrl.Text = "URL:";
            //
            // _labelUrlValue
            //
            this._labelUrlValue.AutoEllipsis = true;
            this._labelUrlValue.ForeColor = System.Drawing.Color.Gray;
            this._labelUrlValue.Location = new System.Drawing.Point(20, 98);
            this._labelUrlValue.Name = "_labelUrlValue";
            this._labelUrlValue.Size = new System.Drawing.Size(440, 17);
            this._labelUrlValue.TabIndex = 3;
            //
            // _labelCategory
            //
            this._labelCategory.AutoSize = true;
            this._labelCategory.Location = new System.Drawing.Point(20, 130);
            this._labelCategory.Name = "_labelCategory";
            this._labelCategory.Size = new System.Drawing.Size(39, 17);
            this._labelCategory.TabIndex = 4;
            this._labelCategory.Text = "分类:";
            //
            // _comboBoxCategory
            //
            this._comboBoxCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this._comboBoxCategory.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this._comboBoxCategory.Location = new System.Drawing.Point(20, 153);
            this._comboBoxCategory.Name = "_comboBoxCategory";
            this._comboBoxCategory.Size = new System.Drawing.Size(440, 23);
            this._comboBoxCategory.TabIndex = 5;
            //
            // _buttonOk
            //
            this._buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._buttonOk.Location = new System.Drawing.Point(280, 200);
            this._buttonOk.Name = "_buttonOk";
            this._buttonOk.Size = new System.Drawing.Size(85, 32);
            this._buttonOk.TabIndex = 6;
            this._buttonOk.Text = "确定";
            this._buttonOk.UseVisualStyleBackColor = true;
            this._buttonOk.Click += new System.EventHandler(this.ButtonOk_Click);
            //
            // _buttonCancel
            //
            this._buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._buttonCancel.Location = new System.Drawing.Point(375, 200);
            this._buttonCancel.Name = "_buttonCancel";
            this._buttonCancel.Size = new System.Drawing.Size(85, 32);
            this._buttonCancel.TabIndex = 7;
            this._buttonCancel.Text = "取消";
            this._buttonCancel.UseVisualStyleBackColor = true;
            //
            // AddBookmarkDialog
            //
            this.AcceptButton = this._buttonOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._buttonCancel;
            this.ClientSize = new System.Drawing.Size(480, 250);
            this.Controls.Add(this._buttonCancel);
            this.Controls.Add(this._buttonOk);
            this.Controls.Add(this._comboBoxCategory);
            this.Controls.Add(this._labelCategory);
            this.Controls.Add(this._labelUrlValue);
            this.Controls.Add(this._labelUrl);
            this.Controls.Add(this._textBoxTitle);
            this.Controls.Add(this._labelTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddBookmarkDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "添加书签";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private Label _labelTitle;
        private TextBox _textBoxTitle;
        private Label _labelUrl;
        private Label _labelUrlValue;
        private Label _labelCategory;
        private ComboBox _comboBoxCategory;
        private Button _buttonOk;
        private Button _buttonCancel;
    }
}
