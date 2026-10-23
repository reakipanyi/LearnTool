using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Forms.Learning;

/// <summary>
/// 笔记管理窗口：左侧列表、右侧编辑，支持搜索、分类筛选、收藏与导出。
/// 入口来自主导航「笔记」与首页功能卡片。
/// </summary>
public sealed class NotesManagerForm : Form
{
    private readonly INoteService _noteService;
    private readonly IUserSessionService? _sessionService;
    private readonly ILogger<NotesManagerForm>? _logger;

    private readonly ListView _listView;
    private readonly TextBox _searchBox;
    private readonly ComboBox _categoryCombo;
    private readonly CheckBox _favoriteOnlyCheck;

    private readonly TextBox _titleBox;
    private readonly TextBox _categoryBox;
    private readonly TextBox _tagsBox;
    private readonly TextBox _contentBox;
    private readonly Label _statusLabel;

    private List<NoteItem> _notes = new();
    private NoteItem? _currentNote;
    private bool _isNewNote;

    public NotesManagerForm(INoteService noteService, IUserSessionService? sessionService = null,
        ILogger<NotesManagerForm>? logger = null)
    {
        _noteService = noteService;
        _sessionService = sessionService;
        _logger = logger;

        Text = "笔记管理";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 560);
        Size = new Size(1080, 680);
        Font = new Font("Microsoft YaHei UI", 9.5F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8, 6, 8, 6)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty
        };

        _searchBox = new TextBox { Width = 190, PlaceholderText = "搜索标题 / 内容", Margin = new Padding(0, 8, 6, 0) };
        _searchBox.TextChanged += (_, _) => ReloadNotes();
        _categoryCombo = new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 8, 6, 0) };
        _categoryCombo.Items.Add("全部分类");
        _categoryCombo.SelectedIndex = 0;
        _categoryCombo.SelectedIndexChanged += (_, _) => ReloadNotes();
        _favoriteOnlyCheck = new CheckBox { Text = "仅收藏", AutoSize = true, Margin = new Padding(0, 11, 8, 0) };
        _favoriteOnlyCheck.CheckedChanged += (_, _) => ReloadNotes();

        toolbar.Controls.Add(_searchBox);
        toolbar.Controls.Add(_categoryCombo);
        toolbar.Controls.Add(_favoriteOnlyCheck);
        toolbar.Controls.Add(CreateButton("🔄 刷新", (_, _) => ReloadNotes()));
        toolbar.Controls.Add(CreateButton("➕ 新建", (_, _) => NewNote()));
        toolbar.Controls.Add(CreateButton("💾 保存", (_, _) => SaveCurrent()));
        toolbar.Controls.Add(CreateButton("⭐ 收藏", (_, _) => ToggleFavorite()));
        toolbar.Controls.Add(CreateButton("🗑 删除", (_, _) => DeleteCurrent()));
        toolbar.Controls.Add(CreateButton("📤 导出", (_, _) => ExportNotes()));

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 360,
            FixedPanel = FixedPanel.Panel1,
            Margin = Padding.Empty
        };

        _listView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            HideSelection = false,
            BorderStyle = BorderStyle.None
        };
        _listView.Columns.Add("标题", 150);
        _listView.Columns.Add("分类", 80);
        _listView.Columns.Add("更新", 110);
        _listView.Columns.Add("★", 30);
        _listView.SelectedIndexChanged += (_, _) => LoadSelectedNote();
        split.Panel1.Controls.Add(_listView);

        var editor = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(10, 6, 6, 6)
        };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64F));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));

        _titleBox = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4) };
        _categoryBox = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4) };
        _tagsBox = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4), PlaceholderText = "多个标签用逗号分隔" };
        _contentBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            AcceptsReturn = true,
            Margin = new Padding(0, 4, 0, 4)
        };

        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(100, 116, 139),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "—"
        };

        editor.Controls.Add(CreateLabel("标题"), 0, 0);
        editor.Controls.Add(_titleBox, 1, 0);
        editor.Controls.Add(CreateLabel("分类"), 0, 1);
        editor.Controls.Add(_categoryBox, 1, 1);
        editor.Controls.Add(CreateLabel("标签"), 0, 2);
        editor.Controls.Add(_tagsBox, 1, 2);
        editor.Controls.Add(CreateLabel("内容"), 0, 3);
        editor.Controls.Add(_contentBox, 1, 3);
        editor.Controls.Add(_statusLabel, 1, 4);
        split.Panel2.Controls.Add(editor);

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(split, 0, 1);
        Controls.Add(root);

        Load += (_, _) => ReloadCategories();
        Shown += (_, _) => ReloadNotes();
    }

    private string CurrentUserId =>
        _sessionService?.CurrentUserId is { Length: > 0 } id ? id : Constants.DefaultUserId;

    private static Button CreateButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = false,
            Width = 82,
            Height = 28,
            Margin = new Padding(0, 5, 6, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        button.Click += onClick;
        return button;
    }

    private static Label CreateLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = Color.FromArgb(71, 85, 105)
    };

    private void ReloadCategories()
    {
        try
        {
            var categories = _noteService.GetAllCategories(CurrentUserId);
            var selected = _categoryCombo.SelectedItem as string;
            _categoryCombo.Items.Clear();
            _categoryCombo.Items.Add("全部分类");
            foreach (var category in categories)
                _categoryCombo.Items.Add(category);
            _categoryCombo.SelectedItem = selected;
            if (_categoryCombo.SelectedIndex < 0)
                _categoryCombo.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "加载笔记分类失败");
        }
    }

    private void ReloadNotes()
    {
        try
        {
            var keyword = _searchBox.Text.Trim();
            var notes = string.IsNullOrWhiteSpace(keyword)
                ? _noteService.GetNotes(CurrentUserId)
                : _noteService.SearchNotes(CurrentUserId, keyword);

            var category = _categoryCombo.SelectedIndex > 0 ? _categoryCombo.SelectedItem as string : null;
            if (!string.IsNullOrWhiteSpace(category))
                notes = notes.Where(n => string.Equals(n.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();

            if (_favoriteOnlyCheck.Checked)
                notes = notes.Where(n => n.IsFavorite).ToList();

            _notes = notes.OrderByDescending(n => n.UpdatedAt).ToList();
            _listView.BeginUpdate();
            _listView.Items.Clear();
            foreach (var note in _notes)
            {
                var item = new ListViewItem(string.IsNullOrWhiteSpace(note.Title) ? "(无标题)" : note.Title)
                {
                    Tag = note
                };
                item.SubItems.Add(note.Category);
                item.SubItems.Add(note.UpdatedAt.ToString("MM-dd HH:mm"));
                item.SubItems.Add(note.IsFavorite ? "★" : "");
                _listView.Items.Add(item);
            }
            _listView.EndUpdate();

            _statusLabel.Text = $"共 {_notes.Count} 条笔记";
            if (_listView.Items.Count > 0 && _listView.SelectedItems.Count == 0)
                _listView.Items[0].Selected = true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "加载笔记列表失败");
            MessageBox.Show($"加载笔记失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadSelectedNote()
    {
        if (_listView.SelectedItems.Count == 0) return;
        _currentNote = _listView.SelectedItems[0].Tag as NoteItem;
        if (_currentNote == null) return;
        _isNewNote = false;

        _titleBox.Text = _currentNote.Title;
        _categoryBox.Text = _currentNote.Category;
        _tagsBox.Text = string.Join(", ", _currentNote.Tags);
        _contentBox.Text = _currentNote.Content;
        _statusLabel.Text = $"更新于 {_currentNote.UpdatedAt:yyyy-MM-dd HH:mm}";
    }

    private void NewNote()
    {
        _currentNote = new NoteItem
        {
            UserId = CurrentUserId,
            Source = "manual",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        _titleBox.Text = string.Empty;
        _categoryBox.Text = _categoryCombo.SelectedIndex > 0 ? _categoryCombo.SelectedItem as string ?? string.Empty : string.Empty;
        _tagsBox.Text = string.Empty;
        _contentBox.Text = string.Empty;
        _isNewNote = true;
        _listView.SelectedItems.Clear();
        _statusLabel.Text = "新笔记（未保存）";
        _titleBox.Focus();
    }

    private void SaveCurrent()
    {
        if (_currentNote == null)
        {
            MessageBox.Show("请先新建或选择一条笔记。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (string.IsNullOrWhiteSpace(_titleBox.Text) && string.IsNullOrWhiteSpace(_contentBox.Text))
        {
            MessageBox.Show("标题和内容不能同时为空。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            _currentNote.Title = _titleBox.Text.Trim();
            _currentNote.Content = _contentBox.Text;
            _currentNote.Category = _categoryBox.Text.Trim();
            _currentNote.Tags = _tagsBox.Text
                .Split(new[] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .Distinct()
                .ToList();
            _currentNote.UserId = CurrentUserId;
            _currentNote.UpdatedAt = DateTime.Now;

            if (_isNewNote)
            {
                _noteService.AddNote(CurrentUserId, _currentNote);
                _isNewNote = false;
            }
            else
            {
                _noteService.UpdateNote(CurrentUserId, _currentNote);
            }

            ReloadCategories();
            ReloadNotes();
            _statusLabel.Text = $"已保存 · {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "保存笔记失败");
            MessageBox.Show($"保存笔记失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ToggleFavorite()
    {
        if (_currentNote == null) return;

        try
        {
            _currentNote.IsFavorite = !_currentNote.IsFavorite;
            if (_isNewNote)
            {
                _noteService.AddNote(CurrentUserId, _currentNote);
                _isNewNote = false;
            }
            else
            {
                _noteService.SetFavorite(CurrentUserId, _currentNote.Id, _currentNote.IsFavorite);
            }
            ReloadNotes();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "切换笔记收藏失败");
            MessageBox.Show($"操作失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DeleteCurrent()
    {
        if (_currentNote == null || !_notes.Any(n => n.Id == _currentNote.Id)) return;

        if (MessageBox.Show("确定删除当前笔记吗？", "删除笔记", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        try
        {
            _noteService.DeleteNote(CurrentUserId, _currentNote.Id);
            _currentNote = null;
            _titleBox.Text = _categoryBox.Text = _tagsBox.Text = _contentBox.Text = string.Empty;
            _statusLabel.Text = "已删除";
            ReloadNotes();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "删除笔记失败");
            MessageBox.Show($"删除笔记失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportNotes()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "导出笔记",
            Filter = "Markdown 文件|*.md|文本文件|*.txt",
            FileName = $"notes_{DateTime.Now:yyyyMMdd_HHmm}.md"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            _noteService.ExportNotes(CurrentUserId, dialog.FileName, dialog.FileName.EndsWith(".md") ? "md" : "txt");
            _statusLabel.Text = $"已导出到 {dialog.FileName}";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "导出笔记失败");
            MessageBox.Show($"导出笔记失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
