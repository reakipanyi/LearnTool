using LearningAssistant.Services.Web;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Forms.Bookmark
{
    public class BookmarkManagerForm : Form
    {
        private readonly IWebBookmarkService _bookmarkService;
        private readonly ILogger? _logger;


        private Panel _panelLeft;
        private Panel _panelRight;
        private Panel _panelButtons;
        private Label _lblCategories;
        private TreeView _treeView;
        private Button _btnAddCategory;
        private Button _btnRenameCategory;
        private Button _btnDeleteCategory;
        private Label _lblBookmarkTitle;
        private TextBox _txtBookmarkTitle;
        private Label _lblBookmarkUrl;
        private TextBox _txtBookmarkUrl;
        private Label _lblBookmarkActions;
        private Button _btnEditBookmark;
        private Label _lblVisitCount;
        private Label _lblLastVisited;
        private Label _lblCreatedAt;
        private Button _btnMoveBookmark;
        private Button _btnDeleteBookmark;
        private Button _btnClose;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _lblStatus;
        private Panel panel2;
        private Panel panel1;
        private readonly Font _boldUiFont = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        public event EventHandler? BookmarksChanged;

        public BookmarkManagerForm(IWebBookmarkService bookmarkService, ILogger? logger = null)
        {
            _bookmarkService = bookmarkService ?? throw new ArgumentNullException(nameof(bookmarkService));
            _logger = logger;
            InitializeComponent();
            LoadBookmarks();
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            _panelLeft = new Panel();
            _lblCategories = new Label();
            _treeView = new TreeView();
            _panelRight = new Panel();
            panel2 = new Panel();
            _panelButtons = new Panel();
            _btnDeleteCategory = new Button();
            _btnRenameCategory = new Button();
            _btnAddCategory = new Button();
            panel1 = new Panel();
            _lblBookmarkTitle = new Label();
            _txtBookmarkTitle = new TextBox();
            _lblBookmarkUrl = new Label();
            _btnDeleteBookmark = new Button();
            _txtBookmarkUrl = new TextBox();
            _btnMoveBookmark = new Button();
            _lblBookmarkActions = new Label();
            _btnEditBookmark = new Button();
            _btnClose = new Button();
            _lblVisitCount = new Label();
            _lblLastVisited = new Label();
            _lblCreatedAt = new Label();
            _statusStrip = new StatusStrip();
            _lblStatus = new ToolStripStatusLabel();
            _panelLeft.SuspendLayout();
            _panelRight.SuspendLayout();
            panel2.SuspendLayout();
            _panelButtons.SuspendLayout();
            panel1.SuspendLayout();
            _statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // _panelLeft
            // 
            _panelLeft.Controls.Add(_panelButtons);
            _panelLeft.Controls.Add(_lblCategories);
            _panelLeft.Controls.Add(_treeView);
            _panelLeft.Dock = DockStyle.Left;
            _panelLeft.Location = new Point(0, 0);
            _panelLeft.Name = "_panelLeft";
            _panelLeft.Size = new Size(300, 540);
            _panelLeft.TabIndex = 0;
            // 
            // _lblCategories
            // 
            _lblCategories.Location = new Point(10, 10);
            _lblCategories.Name = "_lblCategories";
            _lblCategories.Size = new Size(100, 20);
            _lblCategories.TabIndex = 0;
            _lblCategories.Text = "书签分类";
            // 
            // _treeView
            // 
            _treeView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _treeView.HideSelection = false;
            _treeView.Location = new Point(10, 35);
            _treeView.Name = "_treeView";
            _treeView.Size = new Size(284, 433);
            _treeView.TabIndex = 1;
            _treeView.AfterSelect += TreeView_AfterSelect;
            // 
            // _panelRight
            // 
            _panelRight.Controls.Add(panel2);
            _panelRight.Controls.Add(_btnClose);
            _panelRight.Controls.Add(_panelLeft);
            _panelRight.Dock = DockStyle.Fill;
            _panelRight.Location = new Point(0, 0);
            _panelRight.Name = "_panelRight";
            _panelRight.Size = new Size(712, 540);
            _panelRight.TabIndex = 1;
            // 
            // panel2
            // 
            panel2.Controls.Add(panel1);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(300, 0);
            panel2.Name = "panel2";
            panel2.Size = new Size(412, 540);
            panel2.TabIndex = 11;
            // 
            // _panelButtons
            // 
            _panelButtons.Controls.Add(_btnDeleteCategory);
            _panelButtons.Controls.Add(_btnRenameCategory);
            _panelButtons.Controls.Add(_btnAddCategory);
            _panelButtons.Dock = DockStyle.Bottom;
            _panelButtons.Location = new Point(0, 474);
            _panelButtons.Name = "_panelButtons";
            _panelButtons.Padding = new Padding(10, 5, 10, 5);
            _panelButtons.Size = new Size(300, 66);
            _panelButtons.TabIndex = 0;
            // 
            // _btnDeleteCategory
            // 
            _btnDeleteCategory.Location = new Point(170, 5);
            _btnDeleteCategory.Name = "_btnDeleteCategory";
            _btnDeleteCategory.Size = new Size(87, 30);
            _btnDeleteCategory.TabIndex = 2;
            _btnDeleteCategory.Text = "🗑️ 删除分类";
            _btnDeleteCategory.Click += BtnDeleteCategory_Click;
            // 
            // _btnRenameCategory
            // 
            _btnRenameCategory.Location = new Point(90, 5);
            _btnRenameCategory.Name = "_btnRenameCategory";
            _btnRenameCategory.Size = new Size(75, 30);
            _btnRenameCategory.TabIndex = 1;
            _btnRenameCategory.Text = "✏️ 重命名";
            _btnRenameCategory.Click += BtnRenameCategory_Click;
            // 
            // _btnAddCategory
            // 
            _btnAddCategory.Location = new Point(10, 5);
            _btnAddCategory.Name = "_btnAddCategory";
            _btnAddCategory.Size = new Size(75, 30);
            _btnAddCategory.TabIndex = 0;
            _btnAddCategory.Text = "➕ 新分类";
            _btnAddCategory.Click += BtnAddCategory_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(_lblBookmarkTitle);
            panel1.Controls.Add(_txtBookmarkTitle);
            panel1.Controls.Add(_lblBookmarkUrl);
            panel1.Controls.Add(_btnDeleteBookmark);
            panel1.Controls.Add(_txtBookmarkUrl);
            panel1.Controls.Add(_btnMoveBookmark);
            panel1.Controls.Add(_lblBookmarkActions);
            panel1.Controls.Add(_btnEditBookmark);
            panel1.Controls.Add(_lblVisitCount);
            panel1.Controls.Add(_lblLastVisited);
            panel1.Controls.Add(_lblCreatedAt);
            panel1.Location = new Point(3, 41);
            panel1.Name = "panel1";
            panel1.Size = new Size(406, 522);
            panel1.TabIndex = 10;
            // 
            // _lblBookmarkTitle
            // 
            _lblBookmarkTitle.Location = new Point(21, 19);
            _lblBookmarkTitle.Name = "_lblBookmarkTitle";
            _lblBookmarkTitle.Size = new Size(80, 20);
            _lblBookmarkTitle.TabIndex = 1;
            _lblBookmarkTitle.Text = "书签标题:";
            // 
            // _txtBookmarkTitle
            // 
            _txtBookmarkTitle.BackColor = SystemColors.Window;
            _txtBookmarkTitle.Location = new Point(21, 42);
            _txtBookmarkTitle.Name = "_txtBookmarkTitle";
            _txtBookmarkTitle.ReadOnly = true;
            _txtBookmarkTitle.Size = new Size(380, 23);
            _txtBookmarkTitle.TabIndex = 2;
            // 
            // _lblBookmarkUrl
            // 
            _lblBookmarkUrl.Location = new Point(21, 79);
            _lblBookmarkUrl.Name = "_lblBookmarkUrl";
            _lblBookmarkUrl.Size = new Size(80, 20);
            _lblBookmarkUrl.TabIndex = 3;
            _lblBookmarkUrl.Text = "URL:";
            // 
            // _btnDeleteBookmark
            // 
            _btnDeleteBookmark.Enabled = false;
            _btnDeleteBookmark.Location = new Point(241, 179);
            _btnDeleteBookmark.Name = "_btnDeleteBookmark";
            _btnDeleteBookmark.Size = new Size(100, 32);
            _btnDeleteBookmark.TabIndex = 8;
            _btnDeleteBookmark.Text = "🗑️ 删除书签";
            _btnDeleteBookmark.Click += BtnDeleteBookmark_Click;
            // 
            // _txtBookmarkUrl
            // 
            _txtBookmarkUrl.BackColor = SystemColors.Window;
            _txtBookmarkUrl.Location = new Point(21, 102);
            _txtBookmarkUrl.Name = "_txtBookmarkUrl";
            _txtBookmarkUrl.ReadOnly = true;
            _txtBookmarkUrl.Size = new Size(380, 23);
            _txtBookmarkUrl.TabIndex = 4;
            // 
            // _lblVisitCount
            // 
            _lblVisitCount.AutoSize = true;
            _lblVisitCount.Font = new Font("微软雅黑", 9F);
            _lblVisitCount.ForeColor = Color.FromArgb(102, 102, 102);
            _lblVisitCount.Location = new Point(21, 140);
            _lblVisitCount.Name = "_lblVisitCount";
            _lblVisitCount.Size = new Size(80, 17);
            _lblVisitCount.TabIndex = 10;
            _lblVisitCount.Text = "👁 访问次数: 0";
            // 
            // _lblLastVisited
            // 
            _lblLastVisited.AutoSize = true;
            _lblLastVisited.Font = new Font("微软雅黑", 9F);
            _lblLastVisited.ForeColor = Color.FromArgb(102, 102, 102);
            _lblLastVisited.Location = new Point(160, 140);
            _lblLastVisited.Name = "_lblLastVisited";
            _lblLastVisited.Size = new Size(100, 17);
            _lblLastVisited.TabIndex = 11;
            _lblLastVisited.Text = "⏱ 最后访问: -";
            // 
            // _lblCreatedAt
            // 
            _lblCreatedAt.AutoSize = true;
            _lblCreatedAt.Font = new Font("微软雅黑", 9F);
            _lblCreatedAt.ForeColor = Color.FromArgb(102, 102, 102);
            _lblCreatedAt.Location = new Point(21, 165);
            _lblCreatedAt.Name = "_lblCreatedAt";
            _lblCreatedAt.Size = new Size(100, 17);
            _lblCreatedAt.TabIndex = 12;
            _lblCreatedAt.Text = "📅 创建时间: -";
            // 
            // _lblBookmarkActions
            // 
            _lblBookmarkActions.Location = new Point(21, 200);
            _lblBookmarkActions.Name = "_lblBookmarkActions";
            _lblBookmarkActions.Size = new Size(100, 20);
            _lblBookmarkActions.TabIndex = 5;
            _lblBookmarkActions.Text = "书签操作";
            // 
            // _btnEditBookmark
            // 
            _btnEditBookmark.Enabled = false;
            _btnEditBookmark.Location = new Point(21, 230);
            _btnEditBookmark.Name = "_btnEditBookmark";
            _btnEditBookmark.Size = new Size(100, 32);
            _btnEditBookmark.TabIndex = 6;
            _btnEditBookmark.Text = "✏️ 编辑书签";
            _btnEditBookmark.Click += BtnEditBookmark_Click;
            // 
            // _btnMoveBookmark
            // 
            _btnMoveBookmark.Enabled = false;
            _btnMoveBookmark.Location = new Point(131, 230);
            _btnMoveBookmark.Name = "_btnMoveBookmark";
            _btnMoveBookmark.Size = new Size(100, 32);
            _btnMoveBookmark.TabIndex = 7;
            _btnMoveBookmark.Text = "📂 移动分类";
            _btnMoveBookmark.Click += BtnMoveBookmark_Click;
            // 
            // _btnDeleteBookmark
            // 
            _btnDeleteBookmark.Enabled = false;
            _btnDeleteBookmark.Location = new Point(241, 230);
            _btnDeleteBookmark.Name = "_btnDeleteBookmark";
            _btnDeleteBookmark.Size = new Size(100, 32);
            _btnDeleteBookmark.TabIndex = 8;
            _btnDeleteBookmark.Text = "🗑️ 删除书签";
            _btnDeleteBookmark.Click += BtnDeleteBookmark_Click;
            // 
            // _btnClose
            // 
            _btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnClose.Location = new Point(812, 910);
            _btnClose.Name = "_btnClose";
            _btnClose.Size = new Size(100, 32);
            _btnClose.TabIndex = 9;
            _btnClose.Text = "关闭";
            _btnClose.Click += _btnClose_Click;
            // 
            // _statusStrip
            // 
            _statusStrip.Items.AddRange(new ToolStripItem[] { _lblStatus });
            _statusStrip.Location = new Point(0, 518);
            _statusStrip.Name = "_statusStrip";
            _statusStrip.Size = new Size(712, 22);
            _statusStrip.TabIndex = 2;
            // 
            // _lblStatus
            // 
            _lblStatus.Name = "_lblStatus";
            _lblStatus.Size = new Size(32, 17);
            _lblStatus.Text = "就绪";
            // 
            // BookmarkManagerForm
            // 
            ClientSize = new Size(712, 540);
            Controls.Add(_statusStrip);
            Controls.Add(_panelRight);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "BookmarkManagerForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "📚 书签管理器";
            _panelLeft.ResumeLayout(false);
            _panelRight.ResumeLayout(false);
            panel2.ResumeLayout(false);
            _panelButtons.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            _statusStrip.ResumeLayout(false);
            _statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

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
                _lblVisitCount.Text = $"👁 访问次数: {info.Bookmark.VisitCount}";
                _lblLastVisited.Text = info.Bookmark.LastVisited != default
                    ? $"⏱ 最后访问: {info.Bookmark.LastVisited:yyyy-MM-dd HH:mm}"
                    : "⏱ 最后访问: -";
                _lblCreatedAt.Text = $"📅 创建时间: {info.Bookmark.CreatedAt:yyyy-MM-dd}";
                SetCategoryButtonsEnabled(false);
                SetBookmarkButtonsEnabled(true);
            }
        }

        private void ClearBookmarkDetails()
        {
            _txtBookmarkTitle.Text = string.Empty;
            _txtBookmarkUrl.Text = string.Empty;
            _lblVisitCount.Text = "👁 访问次数: 0";
            _lblLastVisited.Text = "⏱ 最后访问: -";
            _lblCreatedAt.Text = "📅 创建时间: -";
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

        private void _btnClose_Click(object sender, EventArgs e)
        {
            Close();
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
            _btnOk.Click += AcceptInput_Click;

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

        private void AcceptInput_Click(object? sender, EventArgs e)
        {
            InputText = _textBox.Text;
            DialogResult = DialogResult.OK;
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
            _btnOk.Click += SaveBookmark_Click;

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

        private void SaveBookmark_Click(object? sender, EventArgs e)
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
            _btnOk.Click += ConfirmCategorySelection_Click;

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

        private void ConfirmCategorySelection_Click(object? sender, EventArgs e)
        {
            SelectedCategory = _comboBox.Text;
            DialogResult = DialogResult.OK;
        }
    }
}
