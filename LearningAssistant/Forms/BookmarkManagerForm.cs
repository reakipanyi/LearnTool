using LearningAssistant.Services.Web;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Forms
{
    public class BookmarkManagerForm : Form
    {
        private readonly IWebBookmarkService _bookmarkService;
        private readonly ILogger? _logger;

        private TreeView _treeView = null!;
        private Panel _panelLeft = null!;
        private Panel _panelRight = null!;
        private Panel _panelButtons = null!;
        private Button _btnAddCategory = null!;
        private Button _btnRenameCategory = null!;
        private Button _btnDeleteCategory = null!;
        private Button _btnDeleteBookmark = null!;
        private Button _btnMoveBookmark = null!;
        private Button _btnEditBookmark = null!;
        private Button _btnClose = null!;
        private Label _lblBookmarkTitle = null!;
        private Label _lblBookmarkUrl = null!;
        private TextBox _txtBookmarkTitle = null!;
        private TextBox _txtBookmarkUrl = null!;
        private StatusStrip _statusStrip = null!;
        private ToolStripStatusLabel _lblStatus = null!;

        public event EventHandler? BookmarksChanged;

        public BookmarkManagerForm(IWebBookmarkService bookmarkService, ILogger? logger = null)
        {
            _bookmarkService = bookmarkService ?? throw new ArgumentNullException(nameof(bookmarkService));
            _logger = logger;
            InitializeComponent();
            LoadBookmarks();
        }

        private void InitializeComponent()
        {
            _panelLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 300
            };

            _panelRight = new Panel
            {
                Dock = DockStyle.Fill
            };

            _panelButtons = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(10, 5, 10, 5)
            };

            var lblCategories = new Label
            {
                Text = "书签分类",
                Location = new Point(10, 10),
                Size = new Size(100, 20),
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold)
            };

            _treeView = new TreeView
            {
                Location = new Point(10, 35),
                Size = new Size(280, 480),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                HideSelection = false,
                ShowLines = true,
                ShowPlusMinus = true
            };
            _treeView.AfterSelect += TreeView_AfterSelect;

            var btnAddCategory = new Button
            {
                Text = "➕ 新分类",
                Size = new Size(75, 30),
                Location = new Point(10, 5),
                Parent = _panelButtons
            };
            btnAddCategory.Click += BtnAddCategory_Click;
            _btnAddCategory = btnAddCategory;

            var btnRenameCategory = new Button
            {
                Text = "✏️ 重命名",
                Size = new Size(75, 30),
                Location = new Point(90, 5),
                Parent = _panelButtons
            };
            btnRenameCategory.Click += BtnRenameCategory_Click;
            _btnRenameCategory = btnRenameCategory;

            var btnDeleteCategory = new Button
            {
                Text = "🗑️ 删除分类",
                Size = new Size(75, 30),
                Location = new Point(170, 5),
                Parent = _panelButtons
            };
            btnDeleteCategory.Click += BtnDeleteCategory_Click;
            _btnDeleteCategory = btnDeleteCategory;

            _lblBookmarkTitle = new Label
            {
                Text = "书签标题:",
                Location = new Point(20, 60),
                Size = new Size(80, 20)
            };

            _txtBookmarkTitle = new TextBox
            {
                Location = new Point(20, 83),
                Size = new Size(380, 23),
                ReadOnly = true,
                BackColor = SystemColors.Window
            };

            _lblBookmarkUrl = new Label
            {
                Text = "URL:",
                Location = new Point(20, 120),
                Size = new Size(80, 20)
            };

            _txtBookmarkUrl = new TextBox
            {
                Location = new Point(20, 143),
                Size = new Size(380, 23),
                ReadOnly = true,
                BackColor = SystemColors.Window
            };

            var lblBookmarkActions = new Label
            {
                Text = "书签操作",
                Location = new Point(20, 190),
                Size = new Size(100, 20),
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold)
            };

            _btnEditBookmark = new Button
            {
                Text = "✏️ 编辑书签",
                Size = new Size(100, 32),
                Location = new Point(20, 220),
                Enabled = false
            };
            _btnEditBookmark.Click += BtnEditBookmark_Click;

            _btnMoveBookmark = new Button
            {
                Text = "📂 移动分类",
                Size = new Size(100, 32),
                Location = new Point(130, 220),
                Enabled = false
            };
            _btnMoveBookmark.Click += BtnMoveBookmark_Click;

            _btnDeleteBookmark = new Button
            {
                Text = "🗑️ 删除书签",
                Size = new Size(100, 32),
                Location = new Point(240, 220),
                Enabled = false
            };
            _btnDeleteBookmark.Click += BtnDeleteBookmark_Click;

            _btnClose = new Button
            {
                Text = "关闭",
                Size = new Size(100, 32),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            _btnClose.Click += (s, e) => Close();

            _statusStrip = new StatusStrip();
            _lblStatus = new ToolStripStatusLabel
            {
                Text = "就绪"
            };
            _statusStrip.Items.Add(_lblStatus);

            _panelLeft.Controls.Add(lblCategories);
            _panelLeft.Controls.Add(_treeView);

            _panelRight.Controls.Add(_panelButtons);
            _panelRight.Controls.Add(_lblBookmarkTitle);
            _panelRight.Controls.Add(_txtBookmarkTitle);
            _panelRight.Controls.Add(_lblBookmarkUrl);
            _panelRight.Controls.Add(_txtBookmarkUrl);
            _panelRight.Controls.Add(lblBookmarkActions);
            _panelRight.Controls.Add(_btnEditBookmark);
            _panelRight.Controls.Add(_btnMoveBookmark);
            _panelRight.Controls.Add(_btnDeleteBookmark);
            _panelRight.Controls.Add(_btnClose);
            _btnClose.Location = new Point(300, 470);

            Controls.Add(_panelRight);
            Controls.Add(_panelLeft);
            Controls.Add(_statusStrip);

            ClientSize = new Size(760, 560);
            Text = "📚 书签管理器";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
        }

        private void LoadBookmarks()
        {
            _treeView.Nodes.Clear();
            var categories = _bookmarkService.GetAllCategories();

            foreach (var category in categories)
            {
                var categoryNode = new TreeNode($"📁 {category.Name}")
                {
                    Tag = new BookmarkNodeInfo
                    {
                        Type = NodeType.Category,
                        CategoryName = category.Name
                    }
                };

                foreach (var bookmark in category.Bookmarks)
                {
                    var bookmarkNode = new TreeNode($"{bookmark.Icon} {bookmark.Title}")
                    {
                        Tag = new BookmarkNodeInfo
                        {
                            Type = NodeType.Bookmark,
                            CategoryName = category.Name,
                            Bookmark = bookmark
                        }
                    };
                    categoryNode.Nodes.Add(bookmarkNode);
                }

                _treeView.Nodes.Add(categoryNode);
            }

            _treeView.ExpandAll();
            UpdateStatus($"共 {categories.Count} 个分类，{categories.Sum(c => c.Bookmarks.Count)} 个书签");
        }

        private void TreeView_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is not BookmarkNodeInfo info)
            {
                ClearBookmarkDetails();
                SetCategoryButtonsEnabled(false);
                SetBookmarkButtonsEnabled(false);
                return;
            }

            if (info.Type == NodeType.Category)
            {
                ClearBookmarkDetails();
                SetCategoryButtonsEnabled(true);
                SetBookmarkButtonsEnabled(false);
            }
            else if (info.Type == NodeType.Bookmark && info.Bookmark != null)
            {
                _txtBookmarkTitle.Text = info.Bookmark.Title;
                _txtBookmarkUrl.Text = info.Bookmark.Url;
                SetCategoryButtonsEnabled(false);
                SetBookmarkButtonsEnabled(true);
            }
        }

        private void ClearBookmarkDetails()
        {
            _txtBookmarkTitle.Text = string.Empty;
            _txtBookmarkUrl.Text = string.Empty;
        }

        private void SetCategoryButtonsEnabled(bool enabled)
        {
            _btnRenameCategory.Enabled = enabled;
            _btnDeleteCategory.Enabled = enabled;
        }

        private void SetBookmarkButtonsEnabled(bool enabled)
        {
            _btnEditBookmark.Enabled = enabled;
            _btnMoveBookmark.Enabled = enabled;
            _btnDeleteBookmark.Enabled = enabled;
        }

        private void BtnAddCategory_Click(object? sender, EventArgs e)
        {
            using var dialog = new InputDialog("新建分类", "请输入分类名称:", "");
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                var categoryName = dialog.InputText.Trim();
                if (string.IsNullOrWhiteSpace(categoryName))
                {
                    MessageBox.Show("分类名称不能为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var categories = _bookmarkService.GetAllCategories();
                if (categories.Any(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("该分类已存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var bookmark = new WebBookmarkItem
                {
                    Title = "示例书签",
                    Url = "https://example.com",
                    Icon = "🔗"
                };
                _bookmarkService.AddBookmark(categoryName, bookmark);
                _bookmarkService.RemoveBookmark(bookmark.Url);

                LoadBookmarks();
                BookmarksChanged?.Invoke(this, EventArgs.Empty);
                UpdateStatus($"已添加分类：{categoryName}");
            }
        }

        private void BtnRenameCategory_Click(object? sender, EventArgs e)
        {
            if (_treeView.SelectedNode?.Tag is not BookmarkNodeInfo info || info.Type != NodeType.Category)
                return;

            var oldName = info.CategoryName;
            using var dialog = new InputDialog("重命名分类", "请输入新的分类名称:", oldName);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                var newName = dialog.InputText.Trim();
                if (string.IsNullOrWhiteSpace(newName))
                {
                    MessageBox.Show("分类名称不能为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (newName.Equals(oldName, StringComparison.OrdinalIgnoreCase))
                    return;

                var categories = _bookmarkService.GetAllCategories();
                if (categories.Any(c => c.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("该分类已存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    var oldCategory = categories.First(c => c.Name == oldName);
                    var newCategory = new WebBookmarkCategory
                    {
                        Name = newName,
                        Icon = oldCategory.Icon,
                        Bookmarks = oldCategory.Bookmarks
                    };

                    var allBookmarks = _bookmarkService.GetAllBookmarks();
                    foreach (var bookmark in oldCategory.Bookmarks.ToList())
                    {
                        _bookmarkService.AddBookmark(newName, bookmark);
                    }

                    foreach (var bookmark in oldCategory.Bookmarks.ToList())
                    {
                        _bookmarkService.RemoveBookmark(bookmark.Url);
                    }

                    LoadBookmarks();
                    BookmarksChanged?.Invoke(this, EventArgs.Empty);
                    UpdateStatus($"已将分类「{oldName}」重命名为「{newName}」");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "重命名分类失败");
                    MessageBox.Show($"重命名分类失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnDeleteCategory_Click(object? sender, EventArgs e)
        {
            if (_treeView.SelectedNode?.Tag is not BookmarkNodeInfo info || info.Type != NodeType.Category)
                return;

            var categoryName = info.CategoryName;
            var result = MessageBox.Show(
                $"确定要删除分类「{categoryName}」吗？\n该分类下的所有书签也将被删除。",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                var categories = _bookmarkService.GetAllCategories();
                var category = categories.FirstOrDefault(c => c.Name == categoryName);
                if (category != null)
                {
                    foreach (var bookmark in category.Bookmarks.ToList())
                    {
                        _bookmarkService.RemoveBookmark(bookmark.Url);
                    }
                }

                LoadBookmarks();
                BookmarksChanged?.Invoke(this, EventArgs.Empty);
                UpdateStatus($"已删除分类：{categoryName}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "删除分类失败");
                MessageBox.Show($"删除分类失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEditBookmark_Click(object? sender, EventArgs e)
        {
            if (_treeView.SelectedNode?.Tag is not BookmarkNodeInfo info || info.Type != NodeType.Bookmark || info.Bookmark == null)
                return;

            var bookmark = info.Bookmark;
            using var dialog = new EditBookmarkDialog(bookmark.Title, bookmark.Url, bookmark.Icon);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    var updatedBookmark = new WebBookmarkItem
                    {
                        Title = dialog.BookmarkTitle,
                        Url = dialog.BookmarkUrl,
                        Icon = dialog.BookmarkIcon
                    };

                    _bookmarkService.UpdateBookmark(bookmark.Url, updatedBookmark);
                    LoadBookmarks();
                    BookmarksChanged?.Invoke(this, EventArgs.Empty);
                    UpdateStatus($"已更新书签：{updatedBookmark.Title}");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "编辑书签失败");
                    MessageBox.Show($"编辑书签失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnMoveBookmark_Click(object? sender, EventArgs e)
        {
            if (_treeView.SelectedNode?.Tag is not BookmarkNodeInfo info || info.Type != NodeType.Bookmark || info.Bookmark == null)
                return;

            var bookmark = info.Bookmark;
            var currentCategory = info.CategoryName;
            var categories = _bookmarkService.GetAllCategories()
                .Select(c => c.Name)
                .Where(n => !n.Equals(currentCategory, StringComparison.OrdinalIgnoreCase))
                .ToList();

            using var dialog = new SelectCategoryDialog(categories, currentCategory);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                var targetCategory = dialog.SelectedCategory;
                if (string.IsNullOrWhiteSpace(targetCategory))
                {
                    MessageBox.Show("请选择目标分类", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    _bookmarkService.AddBookmark(targetCategory, bookmark);
                    _bookmarkService.RemoveBookmark(bookmark.Url);

                    LoadBookmarks();
                    BookmarksChanged?.Invoke(this, EventArgs.Empty);
                    UpdateStatus($"已将「{bookmark.Title}」移动到「{targetCategory}」");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "移动书签失败");
                    MessageBox.Show($"移动书签失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnDeleteBookmark_Click(object? sender, EventArgs e)
        {
            if (_treeView.SelectedNode?.Tag is not BookmarkNodeInfo info || info.Type != NodeType.Bookmark || info.Bookmark == null)
                return;

            var bookmark = info.Bookmark;
            var result = MessageBox.Show(
                $"确定要删除书签「{bookmark.Title}」吗？",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                _bookmarkService.RemoveBookmark(bookmark.Url);
                LoadBookmarks();
                BookmarksChanged?.Invoke(this, EventArgs.Empty);
                UpdateStatus($"已删除书签：{bookmark.Title}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "删除书签失败");
                MessageBox.Show($"删除书签失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStatus(string message)
        {
            _lblStatus.Text = message;
        }

        private class BookmarkNodeInfo
        {
            public NodeType Type { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public WebBookmarkItem? Bookmark { get; set; }
        }

        private enum NodeType
        {
            Category,
            Bookmark
        }
    }

    public class InputDialog : Form
    {
        private TextBox _textBox = null!;
        private Button _btnOk = null!;
        private Button _btnCancel = null!;

        public string InputText { get; private set; } = string.Empty;

        public InputDialog(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent(title, prompt, defaultValue);
        }

        private void InitializeComponent(string title, string prompt, string defaultValue)
        {
            var label = new Label
            {
                Text = prompt,
                Location = new Point(20, 20),
                Size = new Size(360, 20)
            };

            _textBox = new TextBox
            {
                Text = defaultValue,
                Location = new Point(20, 45),
                Size = new Size(360, 23),
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            _btnOk = new Button
            {
                Text = "确定",
                Size = new Size(80, 30),
                Location = new Point(220, 85),
                DialogResult = DialogResult.OK
            };
            _btnOk.Click += (s, e) =>
            {
                InputText = _textBox.Text;
                DialogResult = DialogResult.OK;
            };

            _btnCancel = new Button
            {
                Text = "取消",
                Size = new Size(80, 30),
                Location = new Point(310, 85),
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(label);
            Controls.Add(_textBox);
            Controls.Add(_btnOk);
            Controls.Add(_btnCancel);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            ClientSize = new Size(400, 130);
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
        }
    }

    public class EditBookmarkDialog : Form
    {
        private TextBox _txtTitle = null!;
        private TextBox _txtUrl = null!;
        private TextBox _txtIcon = null!;
        private Button _btnOk = null!;
        private Button _btnCancel = null!;

        public string BookmarkTitle { get; private set; } = string.Empty;
        public string BookmarkUrl { get; private set; } = string.Empty;
        public string BookmarkIcon { get; private set; } = string.Empty;

        public EditBookmarkDialog(string title, string url, string icon)
        {
            InitializeComponent(title, url, icon);
        }

        private void InitializeComponent(string title, string url, string icon)
        {
            var lblTitle = new Label
            {
                Text = "标题:",
                Location = new Point(20, 20),
                Size = new Size(60, 20)
            };

            _txtTitle = new TextBox
            {
                Text = title,
                Location = new Point(20, 43),
                Size = new Size(360, 23),
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            var lblUrl = new Label
            {
                Text = "URL:",
                Location = new Point(20, 80),
                Size = new Size(60, 20)
            };

            _txtUrl = new TextBox
            {
                Text = url,
                Location = new Point(20, 103),
                Size = new Size(360, 23),
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            var lblIcon = new Label
            {
                Text = "图标:",
                Location = new Point(20, 140),
                Size = new Size(60, 20)
            };

            _txtIcon = new TextBox
            {
                Text = icon,
                Location = new Point(20, 163),
                Size = new Size(80, 23),
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            var lblIconHint = new Label
            {
                Text = "（可使用 emoji 图标，如 🔗📚🌐）",
                Location = new Point(110, 166),
                Size = new Size(200, 20),
                ForeColor = Color.Gray
            };

            _btnOk = new Button
            {
                Text = "确定",
                Size = new Size(80, 30),
                Location = new Point(220, 210),
                DialogResult = DialogResult.OK
            };
            _btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_txtTitle.Text))
                {
                    MessageBox.Show("标题不能为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }
                if (string.IsNullOrWhiteSpace(_txtUrl.Text))
                {
                    MessageBox.Show("URL 不能为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }
                BookmarkTitle = _txtTitle.Text.Trim();
                BookmarkUrl = _txtUrl.Text.Trim();
                BookmarkIcon = string.IsNullOrWhiteSpace(_txtIcon.Text) ? "🔗" : _txtIcon.Text.Trim();
                DialogResult = DialogResult.OK;
            };

            _btnCancel = new Button
            {
                Text = "取消",
                Size = new Size(80, 30),
                Location = new Point(310, 210),
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(lblTitle);
            Controls.Add(_txtTitle);
            Controls.Add(lblUrl);
            Controls.Add(_txtUrl);
            Controls.Add(lblIcon);
            Controls.Add(_txtIcon);
            Controls.Add(lblIconHint);
            Controls.Add(_btnOk);
            Controls.Add(_btnCancel);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            ClientSize = new Size(400, 260);
            Text = "编辑书签";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
        }
    }

    public class SelectCategoryDialog : Form
    {
        private ComboBox _comboBox = null!;
        private Button _btnOk = null!;
        private Button _btnCancel = null!;

        public string SelectedCategory { get; private set; } = string.Empty;

        public SelectCategoryDialog(List<string> categories, string currentCategory)
        {
            InitializeComponent(categories, currentCategory);
        }

        private void InitializeComponent(List<string> categories, string currentCategory)
        {
            var label = new Label
            {
                Text = $"当前分类：{currentCategory}\n请选择目标分类：",
                Location = new Point(20, 15),
                Size = new Size(360, 40)
            };

            _comboBox = new ComboBox
            {
                Location = new Point(20, 60),
                Size = new Size(360, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            foreach (var category in categories)
            {
                _comboBox.Items.Add(category);
            }

            if (_comboBox.Items.Count > 0)
            {
                _comboBox.SelectedIndex = 0;
            }

            _btnOk = new Button
            {
                Text = "确定",
                Size = new Size(80, 30),
                Location = new Point(220, 105),
                DialogResult = DialogResult.OK
            };
            _btnOk.Click += (s, e) =>
            {
                SelectedCategory = _comboBox.Text;
                DialogResult = DialogResult.OK;
            };

            _btnCancel = new Button
            {
                Text = "取消",
                Size = new Size(80, 30),
                Location = new Point(310, 105),
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(label);
            Controls.Add(_comboBox);
            Controls.Add(_btnOk);
            Controls.Add(_btnCancel);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            ClientSize = new Size(400, 155);
            Text = "移动书签 - 选择分类";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
        }
    }
}
