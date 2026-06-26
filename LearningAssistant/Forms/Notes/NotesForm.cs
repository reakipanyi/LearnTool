using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.Notes
{
    public partial class NotesForm : Form, IThemeable
    {
        private readonly INoteService _noteService;
        private readonly ILogger<NotesForm>? _logger;
        private readonly IThemeService? _themeService;
        private readonly string _userId;
        private List<NoteItem> _allNotes = new();
        private List<NoteItem> _filteredNotes = new();
        private NoteItem? _currentNote;
        private string _currentCategory = "全部";
        private string _currentTag = "";
        private bool _isFavoriteFilter = false;
        private bool _isEditing = false;

        private SplitContainer splitContainerMain;
        private SplitContainer splitContainerLeft;
        private Panel panelCategory;
        private Label labelCategoryTitle;
        private ListBox listBoxCategories;
        private Panel panelTag;
        private Label labelTagTitle;
        private ListBox listBoxTags;
        private Panel panelMiddle;
        private Panel panelSearchBar;
        private TextBox textBoxSearch;
        private Button buttonNewNote;
        private ListBox listBoxNotes;
        private Panel panelPagination;
        private Label labelPagination;
        private Button buttonPrevPage;
        private Button buttonNextPage;
        private Panel panelEditor;
        private Label labelEditorTitle;
        private TextBox textBoxNoteTitle;
        private Label labelTitleLabel;
        private Label labelCategoryLabel;
        private ComboBox comboBoxCategory;
        private Label labelTagsLabel;
        private TextBox textBoxTags;
        private Label labelContentLabel;
        private RichTextBox richTextBoxContent;
        private Panel panelEditorButtons;
        private Button buttonSave;
        private Button buttonDelete;
        private Button buttonFavorite;
        private Button buttonExport;
        private Label labelNoteInfo;

        private int _currentPage = 1;
        private const int PageSize = 10;

        public NotesForm(
            INoteService noteService,
            ILogger<NotesForm>? logger = null,
            IThemeService? themeService = null,
            string? userId = null)
        {
            _noteService = noteService ?? throw new ArgumentNullException(nameof(noteService));
            _logger = logger;
            _themeService = themeService;
            _userId = userId ?? Environment.UserName;

            InitializeComponent();
            _themeService?.RegisterThemeable(this);
            LoadNotes();
            LoadCategories();
            LoadTags();
            UpdateNotesList();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "📝 笔记管理";
            this.Size = new Size(1100, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 245, 250);
            this.Font = new Font("微软雅黑", 9F);
            this.MinimumSize = new Size(900, 550);

            splitContainerMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 280,
                SplitterWidth = 1,
                BackColor = Color.FromArgb(230, 230, 240)
            };

            splitContainerLeft = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300,
                SplitterWidth = 1,
                BackColor = Color.FromArgb(230, 230, 240)
            };

            panelCategory = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            labelCategoryTitle = new Label
            {
                Text = "📂 分类",
                Font = new Font("微软雅黑", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Location = new Point(12, 12),
                AutoSize = true
            };

            listBoxCategories = new ListBox
            {
                Location = new Point(12, 40),
                Size = new Size(240, 250),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(50, 50, 50),
                Font = new Font("微软雅黑", 10F),
                Cursor = Cursors.Hand
            };
            listBoxCategories.SelectedIndexChanged += ListBoxCategories_SelectedIndexChanged;

            panelCategory.Controls.Add(labelCategoryTitle);
            panelCategory.Controls.Add(listBoxCategories);

            panelTag = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            labelTagTitle = new Label
            {
                Text = "🏷️ 标签",
                Font = new Font("微软雅黑", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Location = new Point(12, 12),
                AutoSize = true
            };

            listBoxTags = new ListBox
            {
                Location = new Point(12, 40),
                Size = new Size(240, 250),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(50, 50, 50),
                Font = new Font("微软雅黑", 10F),
                Cursor = Cursors.Hand
            };
            listBoxTags.SelectedIndexChanged += ListBoxTags_SelectedIndexChanged;

            panelTag.Controls.Add(labelTagTitle);
            panelTag.Controls.Add(listBoxTags);

            splitContainerLeft.Panel1.Controls.Add(panelCategory);
            splitContainerLeft.Panel2.Controls.Add(panelTag);

            panelMiddle = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 248, 252)
            };

            panelSearchBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.White
            };

            textBoxSearch = new TextBox
            {
                Location = new Point(12, 12),
                Size = new Size(200, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("微软雅黑", 10F),
                PlaceholderText = "🔍 搜索笔记..."
            };
            textBoxSearch.TextChanged += TextBoxSearch_TextChanged;

            buttonNewNote = new Button
            {
                Text = "+ 新建笔记",
                Location = new Point(220, 10),
                Size = new Size(100, 32),
                BackColor = Color.FromArgb(63, 81, 181),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold)
            };
            buttonNewNote.FlatAppearance.BorderSize = 0;
            buttonNewNote.Click += ButtonNewNote_Click;

            panelSearchBar.Controls.Add(textBoxSearch);
            panelSearchBar.Controls.Add(buttonNewNote);

            listBoxNotes = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 248, 252),
                ForeColor = Color.FromArgb(50, 50, 50),
                Font = new Font("微软雅黑", 10F),
                ItemHeight = 60,
                Cursor = Cursors.Hand
            };
            listBoxNotes.SelectedIndexChanged += ListBoxNotes_SelectedIndexChanged;
            listBoxNotes.DrawMode = DrawMode.OwnerDrawFixed;
            listBoxNotes.DrawItem += ListBoxNotes_DrawItem;

            panelPagination = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.White
            };

            buttonPrevPage = new Button
            {
                Text = "上一页",
                Location = new Point(12, 8),
                Size = new Size(70, 26),
                BackColor = Color.FromArgb(240, 240, 245),
                ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F)
            };
            buttonPrevPage.FlatAppearance.BorderSize = 0;
            buttonPrevPage.Click += (s, e) => { if (_currentPage > 1) { _currentPage--; UpdateNotesList(); } };

            labelPagination = new Label
            {
                Text = "1 / 1",
                Location = new Point(90, 12),
                Size = new Size(80, 20),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("微软雅黑", 9F)
            };

            buttonNextPage = new Button
            {
                Text = "下一页",
                Location = new Point(175, 8),
                Size = new Size(70, 26),
                BackColor = Color.FromArgb(240, 240, 245),
                ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F)
            };
            buttonNextPage.FlatAppearance.BorderSize = 0;
            buttonNextPage.Click += (s, e) =>
            {
                int totalPages = Math.Max(1, (int)Math.Ceiling((double)_filteredNotes.Count / PageSize));
                if (_currentPage < totalPages) { _currentPage++; UpdateNotesList(); }
            };

            panelPagination.Controls.Add(buttonPrevPage);
            panelPagination.Controls.Add(labelPagination);
            panelPagination.Controls.Add(buttonNextPage);

            panelMiddle.Controls.Add(listBoxNotes);
            panelMiddle.Controls.Add(panelPagination);
            panelMiddle.Controls.Add(panelSearchBar);

            panelEditor = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            labelEditorTitle = new Label
            {
                Text = "📝 编辑区",
                Font = new Font("微软雅黑", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Location = new Point(20, 15),
                AutoSize = true
            };

            labelTitleLabel = new Label
            {
                Text = "标题:",
                Location = new Point(20, 50),
                AutoSize = true,
                ForeColor = Color.FromArgb(80, 80, 80),
                Font = new Font("微软雅黑", 9F)
            };

            textBoxNoteTitle = new TextBox
            {
                Location = new Point(20, 70),
                Size = new Size(440, 30),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("微软雅黑", 10F),
                Enabled = false
            };

            labelCategoryLabel = new Label
            {
                Text = "分类:",
                Location = new Point(20, 110),
                AutoSize = true,
                ForeColor = Color.FromArgb(80, 80, 80),
                Font = new Font("微软雅黑", 9F)
            };

            comboBoxCategory = new ComboBox
            {
                Location = new Point(20, 130),
                Size = new Size(200, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 9F),
                Enabled = false
            };
            comboBoxCategory.Items.AddRange(new object[] {
                "识字", "组词", "短语", "成语", "诗词",
                "英语单词", "英语短语", "英语句子",
                "其他"
            });

            labelTagsLabel = new Label
            {
                Text = "标签 (逗号分隔):",
                Location = new Point(230, 110),
                AutoSize = true,
                ForeColor = Color.FromArgb(80, 80, 80),
                Font = new Font("微软雅黑", 9F)
            };

            textBoxTags = new TextBox
            {
                Location = new Point(230, 130),
                Size = new Size(230, 30),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("微软雅黑", 9F),
                Enabled = false
            };

            labelContentLabel = new Label
            {
                Text = "内容:",
                Location = new Point(20, 175),
                AutoSize = true,
                ForeColor = Color.FromArgb(80, 80, 80),
                Font = new Font("微软雅黑", 9F)
            };

            richTextBoxContent = new RichTextBox
            {
                Location = new Point(20, 195),
                Size = new Size(440, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("微软雅黑", 10F),
                Enabled = false
            };

            labelNoteInfo = new Label
            {
                Text = "",
                Location = new Point(20, 455),
                Size = new Size(440, 20),
                ForeColor = Color.FromArgb(150, 150, 150),
                Font = new Font("微软雅黑", 8F)
            };

            panelEditorButtons = new Panel
            {
                Location = new Point(20, 485),
                Size = new Size(440, 45),
                BackColor = Color.Transparent
            };

            buttonSave = new Button
            {
                Text = "💾 保存",
                Location = new Point(0, 0),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(63, 81, 181),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                Enabled = false
            };
            buttonSave.FlatAppearance.BorderSize = 0;
            buttonSave.Click += ButtonSave_Click;

            buttonDelete = new Button
            {
                Text = "🗑️ 删除",
                Location = new Point(108, 0),
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                Enabled = false
            };
            buttonDelete.FlatAppearance.BorderSize = 0;
            buttonDelete.Click += ButtonDelete_Click;

            buttonFavorite = new Button
            {
                Text = "☆ 收藏",
                Location = new Point(206, 0),
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                Enabled = false
            };
            buttonFavorite.FlatAppearance.BorderSize = 0;
            buttonFavorite.Click += ButtonFavorite_Click;

            buttonExport = new Button
            {
                Text = "📤 导出",
                Location = new Point(304, 0),
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                Enabled = false
            };
            buttonExport.FlatAppearance.BorderSize = 0;
            buttonExport.Click += ButtonExport_Click;

            panelEditorButtons.Controls.Add(buttonSave);
            panelEditorButtons.Controls.Add(buttonDelete);
            panelEditorButtons.Controls.Add(buttonFavorite);
            panelEditorButtons.Controls.Add(buttonExport);

            panelEditor.Controls.Add(labelEditorTitle);
            panelEditor.Controls.Add(labelTitleLabel);
            panelEditor.Controls.Add(textBoxNoteTitle);
            panelEditor.Controls.Add(labelCategoryLabel);
            panelEditor.Controls.Add(comboBoxCategory);
            panelEditor.Controls.Add(labelTagsLabel);
            panelEditor.Controls.Add(textBoxTags);
            panelEditor.Controls.Add(labelContentLabel);
            panelEditor.Controls.Add(richTextBoxContent);
            panelEditor.Controls.Add(labelNoteInfo);
            panelEditor.Controls.Add(panelEditorButtons);

            splitContainerMain.Panel1.Controls.Add(splitContainerLeft);
            splitContainerMain.Panel2.Controls.Add(panelMiddle);

            SplitContainer splitContainerRight = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 320,
                SplitterWidth = 1,
                BackColor = Color.FromArgb(230, 230, 240)
            };

            splitContainerRight.Panel1.Controls.Add(panelMiddle);
            splitContainerRight.Panel2.Controls.Add(panelEditor);

            splitContainerMain.Panel2.Controls.Add(splitContainerRight);

            this.Controls.Add(splitContainerMain);

            splitContainerMain.SplitterDistance = 260;
            splitContainerRight.SplitterDistance = 320;

            this.ResumeLayout(false);
        }

        private void ListBoxNotes_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _filteredNotes.Count) return;

            var note = _filteredNotes[e.Index];
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color bgColor = isSelected ? Color.FromArgb(230, 230, 250) : Color.White;
            Color textColor = isSelected ? Color.FromArgb(63, 63, 180) : Color.FromArgb(50, 50, 50);
            Color subTextColor = Color.FromArgb(150, 150, 150);

            using var bgBrush = new SolidBrush(bgColor);
            g.FillRectangle(bgBrush, e.Bounds);

            int padding = 10;
            int titleY = e.Bounds.Y + padding;

            string title = string.IsNullOrEmpty(note.Title) ? "(无标题)" : note.Title;
            if (note.IsFavorite) title = "⭐ " + title;

            using var titleFont = new Font("微软雅黑", 10F, FontStyle.Bold);
            using var titleBrush = new SolidBrush(textColor);
            var titleRect = new RectangleF(padding, titleY, e.Bounds.Width - padding * 2, 22);
            using var titleFormat = new StringFormat
            {
                FormatFlags = StringFormatFlags.NoWrap,
                Trimming = StringTrimming.EllipsisCharacter
            };
            g.DrawString(title, titleFont, titleBrush, titleRect, titleFormat);

            string summary = note.Content.Length > 40 ? note.Content.Substring(0, 40) + "..." : note.Content;
            if (string.IsNullOrEmpty(summary)) summary = "暂无内容";

            using var summaryFont = new Font("微软雅黑", 8F);
            using var summaryBrush = new SolidBrush(subTextColor);
            var summaryRect = new RectangleF(padding, titleY + 22, e.Bounds.Width - padding * 2, 18);
            g.DrawString(summary, summaryFont, summaryBrush, summaryRect, titleFormat);

            string meta = $"{note.Category} | {note.CreatedAt:yyyy-MM-dd}";
            if (!string.IsNullOrEmpty(note.RelatedItemTitle))
                meta += $" | 关联: {note.RelatedItemTitle}";

            var metaRect = new RectangleF(padding, titleY + 40, e.Bounds.Width - padding * 2, 16);
            g.DrawString(meta, summaryFont, summaryBrush, metaRect, titleFormat);
        }

        private void LoadNotes()
        {
            try
            {
                _allNotes = _noteService.GetNotes(_userId);
                _filteredNotes = new List<NoteItem>(_allNotes);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载笔记失败");
                MessageBox.Show($"加载笔记失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadCategories()
        {
            try
            {
                var categories = _noteService.GetAllCategories(_userId);
                listBoxCategories.Items.Clear();
                listBoxCategories.Items.Add("📋 全部");
                listBoxCategories.Items.Add("⭐ 收藏");
                foreach (var cat in categories.OrderBy(c => c))
                {
                    listBoxCategories.Items.Add($"📁 {cat}");
                }
                listBoxCategories.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载分类失败");
            }
        }

        private void LoadTags()
        {
            try
            {
                var tags = _noteService.GetAllTags(_userId);
                listBoxTags.Items.Clear();
                listBoxTags.Items.Add("全部标签");
                foreach (var tag in tags.OrderBy(t => t))
                {
                    listBoxTags.Items.Add($"# {tag}");
                }
                listBoxTags.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载标签失败");
            }
        }

        private void ApplyFilters()
        {
            IEnumerable<NoteItem> query = _allNotes;

            if (_isFavoriteFilter)
            {
                query = query.Where(n => n.IsFavorite);
            }
            else if (_currentCategory != "全部")
            {
                query = query.Where(n => n.Category == _currentCategory);
            }

            if (!string.IsNullOrEmpty(_currentTag))
            {
                query = query.Where(n => n.Tags?.Contains(_currentTag) == true);
            }

            string searchText = textBoxSearch.Text.Trim();
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(n =>
                    (n.Title?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (n.Content?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
            }

            _filteredNotes = query.OrderByDescending(n => n.UpdatedAt).ToList();
            _currentPage = 1;
            UpdateNotesList();
        }

        private void UpdateNotesList()
        {
            listBoxNotes.Items.Clear();

            int totalPages = Math.Max(1, (int)Math.Ceiling((double)_filteredNotes.Count / PageSize));
            if (_currentPage > totalPages) _currentPage = totalPages;

            int startIndex = (_currentPage - 1) * PageSize;
            int count = Math.Min(PageSize, _filteredNotes.Count - startIndex);

            if (count > 0)
            {
                var pageItems = _filteredNotes.Skip(startIndex).Take(count).ToList();
                foreach (var note in pageItems)
                {
                    listBoxNotes.Items.Add(note);
                }
            }

            labelPagination.Text = $"{_currentPage} / {totalPages}";
            buttonPrevPage.Enabled = _currentPage > 1;
            buttonNextPage.Enabled = _currentPage < totalPages;
        }

        private void ListBoxCategories_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (listBoxCategories.SelectedIndex < 0) return;

            string selected = listBoxCategories.SelectedItem?.ToString() ?? "";

            if (selected == "📋 全部")
            {
                _currentCategory = "全部";
                _isFavoriteFilter = false;
            }
            else if (selected == "⭐ 收藏")
            {
                _currentCategory = "全部";
                _isFavoriteFilter = true;
            }
            else
            {
                _currentCategory = selected.Replace("📁 ", "").Trim();
                _isFavoriteFilter = false;
            }

            ApplyFilters();
        }

        private void ListBoxTags_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (listBoxTags.SelectedIndex < 0) return;

            string selected = listBoxTags.SelectedItem?.ToString() ?? "";

            if (selected == "全部标签")
            {
                _currentTag = "";
            }
            else
            {
                _currentTag = selected.Replace("# ", "").Trim();
            }

            ApplyFilters();
        }

        private void TextBoxSearch_TextChanged(object? sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void ListBoxNotes_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (listBoxNotes.SelectedIndex < 0 || listBoxNotes.SelectedItem is not NoteItem note)
            {
                ClearEditor();
                return;
            }

            _currentNote = note;
            LoadNoteToEditor(note);
            SetEditorEnabled(true);
            _isEditing = false;
        }

        private void LoadNoteToEditor(NoteItem note)
        {
            textBoxNoteTitle.Text = note.Title;
            comboBoxCategory.Text = string.IsNullOrEmpty(note.Category) ? "其他" : note.Category;
            textBoxTags.Text = note.Tags ?? "";
            richTextBoxContent.Text = note.Content;
            buttonFavorite.Text = note.IsFavorite ? "★ 已收藏" : "☆ 收藏";
            labelNoteInfo.Text = $"创建: {note.CreatedAt:yyyy-MM-dd HH:mm} | 更新: {note.UpdatedAt:yyyy-MM-dd HH:mm} | 复习: {note.ReviewCount}次";
        }

        private void ClearEditor()
        {
            _currentNote = null;
            textBoxNoteTitle.Text = "";
            comboBoxCategory.SelectedIndex = -1;
            textBoxTags.Text = "";
            richTextBoxContent.Text = "";
            buttonFavorite.Text = "☆ 收藏";
            labelNoteInfo.Text = "";
            SetEditorEnabled(false);
            _isEditing = false;
        }

        private void SetEditorEnabled(bool enabled)
        {
            textBoxNoteTitle.Enabled = enabled;
            comboBoxCategory.Enabled = enabled;
            textBoxTags.Enabled = enabled;
            richTextBoxContent.Enabled = enabled;
            buttonSave.Enabled = enabled;
            buttonDelete.Enabled = enabled;
            buttonFavorite.Enabled = enabled;
            buttonExport.Enabled = enabled;
        }

        private void ButtonNewNote_Click(object? sender, EventArgs e)
        {
            _currentNote = new NoteItem
            {
                UserId = _userId,
                Category = _currentCategory == "全部" ? "其他" : _currentCategory,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            LoadNoteToEditor(_currentNote);
            SetEditorEnabled(true);
            _isEditing = true;
            textBoxNoteTitle.Focus();
        }

        private void ButtonSave_Click(object? sender, EventArgs e)
        {
            if (_currentNote == null) return;

            try
            {
                _currentNote.Title = textBoxNoteTitle.Text.Trim();
                _currentNote.Category = comboBoxCategory.Text;
                _currentNote.Tags = textBoxTags.Text.Trim();
                _currentNote.Content = richTextBoxContent.Text;
                _currentNote.UpdatedAt = DateTime.Now;

                if (_isEditing)
                {
                    _noteService.AddNote(_userId, _currentNote);
                    _allNotes.Add(_currentNote);
                    _isEditing = false;
                }
                else
                {
                    _noteService.UpdateNote(_userId, _currentNote);
                    int index = _allNotes.FindIndex(n => n.Id == _currentNote.Id);
                    if (index >= 0) _allNotes[index] = _currentNote;
                }

                LoadCategories();
                LoadTags();
                ApplyFilters();
                MessageBox.Show("保存成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存笔记失败");
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonDelete_Click(object? sender, EventArgs e)
        {
            if (_currentNote == null) return;

            var result = MessageBox.Show(
                $"确定要删除这篇笔记吗？\n\n{_currentNote.Title}",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                _noteService.DeleteNote(_userId, _currentNote.Id);
                _allNotes.RemoveAll(n => n.Id == _currentNote.Id);
                LoadCategories();
                LoadTags();
                ApplyFilters();
                ClearEditor();
                MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "删除笔记失败");
                MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonFavorite_Click(object? sender, EventArgs e)
        {
            if (_currentNote == null) return;

            try
            {
                bool newFavorite = !_currentNote.IsFavorite;
                _noteService.SetFavorite(_userId, _currentNote.Id, newFavorite);
                _currentNote.IsFavorite = newFavorite;
                buttonFavorite.Text = newFavorite ? "★ 已收藏" : "☆ 收藏";

                int index = _allNotes.FindIndex(n => n.Id == _currentNote.Id);
                if (index >= 0) _allNotes[index].IsFavorite = newFavorite;

                LoadCategories();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "收藏操作失败");
                MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonExport_Click(object? sender, EventArgs e)
        {
            if (_currentNote == null) return;

            using var dialog = new SaveFileDialog
            {
                Filter = "文本文件|*.txt|Markdown文件|*.md",
                FileName = $"{_currentNote.Title}.md",
                DefaultExt = "md"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                string format = Path.GetExtension(dialog.FileName).TrimStart('.');
                _noteService.ExportNotes(_userId, dialog.FileName, format);
                MessageBox.Show("导出成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导出笔记失败");
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;

            if (splitContainerMain != null)
                splitContainerMain.BackColor = colors.ThemeMode == ThemeMode.Dark
                    ? Color.FromArgb(60, 60, 70)
                    : Color.FromArgb(230, 230, 240);

            ApplyThemeToPanel(panelCategory, colors);
            ApplyThemeToPanel(panelTag, colors);
            ApplyThemeToPanel(panelSearchBar, colors);
            ApplyThemeToPanel(panelEditor, colors);
            ApplyThemeToPanel(panelPagination, colors);
            ApplyThemeToPanel(panelMiddle, colors);

            if (labelCategoryTitle != null) labelCategoryTitle.ForeColor = colors.TextPrimary;
            if (labelTagTitle != null) labelTagTitle.ForeColor = colors.TextPrimary;
            if (labelEditorTitle != null) labelEditorTitle.ForeColor = colors.TextPrimary;

            if (listBoxCategories != null)
            {
                listBoxCategories.BackColor = colors.Surface;
                listBoxCategories.ForeColor = colors.TextPrimary;
            }

            if (listBoxTags != null)
            {
                listBoxTags.BackColor = colors.Surface;
                listBoxTags.ForeColor = colors.TextPrimary;
            }

            if (listBoxNotes != null)
            {
                listBoxNotes.BackColor = colors.Background;
                listBoxNotes.ForeColor = colors.TextPrimary;
            }

            if (textBoxSearch != null)
            {
                textBoxSearch.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : Color.White;
                textBoxSearch.ForeColor = colors.TextPrimary;
            }

            if (textBoxNoteTitle != null)
            {
                textBoxNoteTitle.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : Color.White;
                textBoxNoteTitle.ForeColor = colors.TextPrimary;
            }

            if (textBoxTags != null)
            {
                textBoxTags.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : Color.White;
                textBoxTags.ForeColor = colors.TextPrimary;
            }

            if (richTextBoxContent != null)
            {
                richTextBoxContent.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : Color.White;
                richTextBoxContent.ForeColor = colors.TextPrimary;
            }

            if (comboBoxCategory != null)
            {
                comboBoxCategory.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : Color.White;
                comboBoxCategory.ForeColor = colors.TextPrimary;
            }

            labelCategoryLabel.ForeColor = colors.TextSecondary;
            labelTagsLabel.ForeColor = colors.TextSecondary;
            labelNoteInfo.ForeColor = colors.TextSecondary;
        }

        private void ApplyThemeToPanel(Panel panel, ThemeColors colors)
        {
            if (panel == null) return;
            panel.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : Color.White;

            foreach (Control control in panel.Controls)
            {
                if (control is Label lbl && lbl != labelCategoryTitle && lbl != labelTagTitle && lbl != labelEditorTitle)
                {
                    if (lbl.ForeColor == Color.FromArgb(80, 80, 80) || lbl.ForeColor == Color.FromArgb(150, 150, 150))
                    {
                        lbl.ForeColor = colors.TextSecondary;
                    }
                }
            }
        }
    }
}
