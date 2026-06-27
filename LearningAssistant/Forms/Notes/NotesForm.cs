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
        private readonly IUserSessionService? _userSessionService;
        private readonly string _userId;
        private List<NoteItem> _allNotes = new();
        private List<NoteItem> _filteredNotes = new();
        private NoteItem? _currentNote;
        private string _currentCategory = "全部";
        private string _currentTag = "";
        private bool _isFavoriteFilter = false;
        private bool _isEditing = false;
        private bool _isBatchMode = false;
        private HashSet<string> _selectedNoteIds = new();

        private Button buttonBatchMode;
        private Button buttonBatchDelete;
        private Button buttonBatchMove;
        private Button buttonBatchCancel;
        private Label labelSelectedCount;

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
            IUserSessionService? userSessionService = null,
            string? userId = null)
        {
            _noteService = noteService ?? throw new ArgumentNullException(nameof(noteService));
            _logger = logger;
            _themeService = themeService;
            _userSessionService = userSessionService;
            _userId = userId ?? userSessionService?.CurrentUserId ?? Environment.UserName;

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

            buttonBatchMode = new Button
            {
                Text = "☑️ 批量",
                Location = new Point(330, 10),
                Size = new Size(80, 32),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold)
            };
            buttonBatchMode.FlatAppearance.BorderSize = 0;
            buttonBatchMode.Click += ButtonBatchMode_Click;

            labelSelectedCount = new Label
            {
                Text = "",
                Location = new Point(420, 14),
                Size = new Size(100, 24),
                ForeColor = Color.FromArgb(63, 81, 181),
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                Visible = false
            };

            panelSearchBar.Controls.Add(textBoxSearch);
            panelSearchBar.Controls.Add(buttonNewNote);
            panelSearchBar.Controls.Add(buttonBatchMode);
            panelSearchBar.Controls.Add(labelSelectedCount);

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
            buttonPrevPage.Click += ButtonPrevPage_Click;

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
            buttonNextPage.Click += ButtonNextPage_Click;

            buttonBatchDelete = new Button
            {
                Text = "🗑️ 批量删除",
                Location = new Point(255, 8),
                Size = new Size(90, 26),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F),
                Visible = false
            };
            buttonBatchDelete.FlatAppearance.BorderSize = 0;
            buttonBatchDelete.Click += ButtonBatchDelete_Click;

            buttonBatchMove = new Button
            {
                Text = "📁 批量移动",
                Location = new Point(355, 8),
                Size = new Size(90, 26),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F),
                Visible = false
            };
            buttonBatchMove.FlatAppearance.BorderSize = 0;
            buttonBatchMove.Click += ButtonBatchMove_Click;

            buttonBatchCancel = new Button
            {
                Text = "取消",
                Location = new Point(455, 8),
                Size = new Size(50, 26),
                BackColor = Color.FromArgb(240, 240, 245),
                ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F),
                Visible = false
            };
            buttonBatchCancel.FlatAppearance.BorderSize = 0;
            buttonBatchCancel.Click += ButtonBatchCancel_Click;

            panelPagination.Controls.Add(buttonPrevPage);
            panelPagination.Controls.Add(labelPagination);
            panelPagination.Controls.Add(buttonNextPage);
            panelPagination.Controls.Add(buttonBatchDelete);
            panelPagination.Controls.Add(buttonBatchMove);
            panelPagination.Controls.Add(buttonBatchCancel);

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

            // splitContainerRight: 左右分割（中间列表 + 右侧编辑器）
            SplitContainer splitContainerRight = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 1,
                BackColor = Color.FromArgb(230, 230, 240),
                Panel1MinSize = 100,
                Panel2MinSize = 100
            };

            splitContainerRight.Panel1.Controls.Add(panelMiddle);
            splitContainerRight.Panel2.Controls.Add(panelEditor);

            // splitContainerMain: 左右分割（左侧筛选栏 + 右侧区域）
            splitContainerMain.Panel1.Controls.Add(splitContainerLeft);
            splitContainerMain.Panel2.Controls.Add(splitContainerRight);

            this.Controls.Add(splitContainerMain);

            // 在窗体加载后设置 SplitterDistance，避免超出范围
            this.Load += (s, e) =>
            {
                SafeSetSplitterDistance(splitContainerMain, 260);
                SafeSetSplitterDistance(splitContainerLeft, 300);
                SafeSetSplitterDistance(splitContainerRight, 320);
            };

            // 窗体尺寸变化时重新钳制 SplitterDistance
            this.SizeChanged += (s, e) =>
            {
                if (splitContainerMain.IsHandleCreated)
                    SafeSetSplitterDistance(splitContainerMain, splitContainerMain.SplitterDistance);
                if (splitContainerLeft.IsHandleCreated)
                    SafeSetSplitterDistance(splitContainerLeft, splitContainerLeft.SplitterDistance);
                if (splitContainerRight.IsHandleCreated)
                    SafeSetSplitterDistance(splitContainerRight, splitContainerRight.SplitterDistance);
            };

            this.ResumeLayout(false);
        }

        private static void SafeSetSplitterDistance(SplitContainer sc, int desiredDistance)
        {
            int availableSize = sc.Orientation == Orientation.Vertical ? sc.Width : sc.Height;
            int min1 = sc.Panel1MinSize;
            int min2 = sc.Panel2MinSize;

            if (availableSize <= 0) return;

            int minTotal = min1 + min2;
            if (minTotal > availableSize)
            {
                double ratio = (double)min1 / minTotal;
                int newMin1 = (int)(availableSize * ratio);
                int newMin2 = availableSize - newMin1;
                sc.Panel1MinSize = newMin1;
                sc.Panel2MinSize = newMin2;
                min1 = newMin1;
                min2 = newMin2;
            }

            int maxValid = availableSize - min2;
            int clamped = Math.Max(min1, Math.Min(desiredDistance, maxValid));
            sc.SplitterDistance = clamped;
        }

        private void ListBoxNotes_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _filteredNotes.Count) return;

            var note = _filteredNotes[e.Index];
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool isMultiSelected = _isBatchMode && _selectedNoteIds.Contains(note.Id);

            Color bgColor = isMultiSelected ? Color.FromArgb(220, 240, 220) :
                            isSelected ? Color.FromArgb(230, 230, 250) : Color.White;
            Color textColor = isMultiSelected ? Color.FromArgb(56, 142, 60) :
                              isSelected ? Color.FromArgb(63, 63, 180) : Color.FromArgb(50, 50, 50);
            Color subTextColor = Color.FromArgb(150, 150, 150);

            using var bgBrush = new SolidBrush(bgColor);
            g.FillRectangle(bgBrush, e.Bounds);

            // 批量模式下显示勾选状态
            if (_isBatchMode)
            {
                int checkboxX = 8;
                int checkboxY = e.Bounds.Y + (e.Bounds.Height - 16) / 2;
                Rectangle checkboxRect = new Rectangle(checkboxX, checkboxY, 16, 16);

                using var checkboxPen = new Pen(Color.FromArgb(180, 180, 180), 1);
                g.DrawRectangle(checkboxPen, checkboxRect);

                if (isMultiSelected)
                {
                    using var checkBrush = new SolidBrush(Color.FromArgb(76, 175, 80));
                    g.FillRectangle(checkBrush, new Rectangle(checkboxX + 2, checkboxY + 2, 12, 12));

                    using var checkPen = new Pen(Color.White, 2);
                    g.DrawLines(checkPen, new Point[] {
                        new Point(checkboxX + 4, checkboxY + 8),
                        new Point(checkboxX + 7, checkboxY + 11),
                        new Point(checkboxX + 12, checkboxY + 4)
                    });
                }
            }

            int padding = _isBatchMode ? 30 : 10;
            int titleY = e.Bounds.Y + 10;

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
                if (!_isBatchMode)
                    ClearEditor();
                return;
            }

            if (_isBatchMode)
            {
                // 批量模式下切换选中状态
                if (_selectedNoteIds.Contains(note.Id))
                    _selectedNoteIds.Remove(note.Id);
                else
                    _selectedNoteIds.Add(note.Id);

                UpdateSelectedCount();
                listBoxNotes.Invalidate();
            }
            else
            {
                _currentNote = note;
                LoadNoteToEditor(note);
                SetEditorEnabled(true);
                _isEditing = false;
            }
        }

        private void LoadNoteToEditor(NoteItem note)
        {
            textBoxNoteTitle.Text = note.Title;
            comboBoxCategory.Text = string.IsNullOrEmpty(note.Category) ? "其他" : note.Category;
            textBoxTags.Text = note.Tags != null ? string.Join(", ", note.Tags) : "";
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
                _currentNote.Tags = textBoxTags.Text.Trim()
                    .Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .ToList();
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

        private void ButtonBatchMode_Click(object? sender, EventArgs e)
        {
            _isBatchMode = true;
            _selectedNoteIds.Clear();

            buttonBatchMode.BackColor = Color.FromArgb(76, 175, 80);
            buttonBatchMode.Text = "✓ 已启用";
            labelSelectedCount.Visible = true;
            UpdateSelectedCount();

            buttonPrevPage.Visible = false;
            buttonNextPage.Visible = false;
            labelPagination.Visible = false;
            buttonBatchDelete.Visible = true;
            buttonBatchMove.Visible = true;
            buttonBatchCancel.Visible = true;

            ClearEditor();
            SetEditorEnabled(false);
            listBoxNotes.Invalidate();
        }

        private void ButtonBatchCancel_Click(object? sender, EventArgs e)
        {
            ExitBatchMode();
        }

        private void ExitBatchMode()
        {
            _isBatchMode = false;
            _selectedNoteIds.Clear();

            buttonBatchMode.BackColor = Color.FromArgb(108, 117, 125);
            buttonBatchMode.Text = "☑️ 批量";
            labelSelectedCount.Visible = false;

            buttonPrevPage.Visible = true;
            buttonNextPage.Visible = true;
            labelPagination.Visible = true;
            buttonBatchDelete.Visible = false;
            buttonBatchMove.Visible = false;
            buttonBatchCancel.Visible = false;

            listBoxNotes.Invalidate();
        }

        private void UpdateSelectedCount()
        {
            labelSelectedCount.Text = $"已选 {_selectedNoteIds.Count} 项";
        }

        private void ButtonBatchDelete_Click(object? sender, EventArgs e)
        {
            if (_selectedNoteIds.Count == 0)
            {
                MessageBox.Show("请先选择要删除的笔记", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"确定要删除选中的 {_selectedNoteIds.Count} 篇笔记吗？\n此操作不可恢复！",
                "确认批量删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                foreach (var noteId in _selectedNoteIds.ToList())
                {
                    _noteService.DeleteNote(_userId, noteId);
                    _allNotes.RemoveAll(n => n.Id == noteId);
                }

                LoadCategories();
                LoadTags();
                ApplyFilters();
                ExitBatchMode();
                MessageBox.Show($"成功删除 {_selectedNoteIds.Count} 篇笔记", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "批量删除笔记失败");
                MessageBox.Show($"批量删除失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonBatchMove_Click(object? sender, EventArgs e)
        {
            if (_selectedNoteIds.Count == 0)
            {
                MessageBox.Show("请先选择要移动的笔记", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 弹出分类选择对话框
            var categories = _noteService.GetAllCategories(_userId).ToList();
            categories.AddRange(new[] { "识字", "组词", "短语", "成语", "诗词", "英语单词", "英语短语", "英语句子", "其他" });
            categories = categories.Distinct().OrderBy(c => c).ToList();

            using var form = new Form
            {
                Text = "选择目标分类",
                Size = new Size(300, 150),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var comboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(20, 20),
                Size = new Size(260, 30),
                Font = new Font("微软雅黑", 10F)
            };
            comboBox.Items.AddRange(categories.ToArray());
            if (categories.Count > 0) comboBox.SelectedIndex = 0;

            var btnConfirm = new Button
            {
                Text = "确认移动",
                Location = new Point(100, 70),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += (s, ev) =>
            {
                if (comboBox.SelectedItem == null)
                {
                    MessageBox.Show("请选择一个分类", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string targetCategory = comboBox.SelectedItem.ToString()!;
                try
                {
                    int movedCount = 0;
                    foreach (var noteId in _selectedNoteIds.ToList())
                    {
                        var note = _allNotes.FirstOrDefault(n => n.Id == noteId);
                        if (note != null)
                        {
                            note.Category = targetCategory;
                            note.UpdatedAt = DateTime.Now;
                            _noteService.UpdateNote(_userId, note);
                            movedCount++;
                        }
                    }

                    LoadCategories();
                    LoadTags();
                    ApplyFilters();
                    ExitBatchMode();
                    form.DialogResult = DialogResult.OK;
                    MessageBox.Show($"成功将 {movedCount} 篇笔记移动到「{targetCategory}」", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "批量移动笔记失败");
                    MessageBox.Show($"批量移动失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            form.Controls.Add(comboBox);
            form.Controls.Add(btnConfirm);
            form.ShowDialog(this);
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

        #region IDisposable Support
        private bool _disposed = false;

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // 注销主题服务
                _themeService?.UnregisterThemeable(this);

                // 注销事件订阅（命名方法）
                if (listBoxCategories != null) listBoxCategories.SelectedIndexChanged -= ListBoxCategories_SelectedIndexChanged;
                if (listBoxTags != null) listBoxTags.SelectedIndexChanged -= ListBoxTags_SelectedIndexChanged;
                if (textBoxSearch != null) textBoxSearch.TextChanged -= TextBoxSearch_TextChanged;
                if (buttonNewNote != null) buttonNewNote.Click -= ButtonNewNote_Click;
                if (buttonBatchMode != null) buttonBatchMode.Click -= ButtonBatchMode_Click;
                if (listBoxNotes != null)
                {
                    listBoxNotes.SelectedIndexChanged -= ListBoxNotes_SelectedIndexChanged;
                    listBoxNotes.DrawItem -= ListBoxNotes_DrawItem;
                }
                if (buttonPrevPage != null) buttonPrevPage.Click -= ButtonPrevPage_Click;
                if (buttonNextPage != null) buttonNextPage.Click -= ButtonNextPage_Click;
                if (buttonBatchDelete != null) buttonBatchDelete.Click -= ButtonBatchDelete_Click;
                if (buttonBatchMove != null) buttonBatchMove.Click -= ButtonBatchMove_Click;
                if (buttonBatchCancel != null) buttonBatchCancel.Click -= ButtonBatchCancel_Click;
                if (buttonSave != null) buttonSave.Click -= ButtonSave_Click;
                if (buttonDelete != null) buttonDelete.Click -= ButtonDelete_Click;
                if (buttonFavorite != null) buttonFavorite.Click -= ButtonFavorite_Click;
                if (buttonExport != null) buttonExport.Click -= ButtonExport_Click;

            }

            _disposed = true;
            base.Dispose(disposing);
        }

        // 保存分页按钮的事件处理程序引用
        private void ButtonPrevPage_Click(object? sender, EventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdateNotesList();
            }
        }

        private void ButtonNextPage_Click(object? sender, EventArgs e)
        {
            var totalPages = (int)Math.Ceiling((double)_filteredNotes.Count / PageSize);
            if (_currentPage < totalPages)
            {
                _currentPage++;
                UpdateNotesList();
            }
        }
        #endregion
    }
}
