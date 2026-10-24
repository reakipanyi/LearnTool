using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

/// <summary>命令面板中的一条命令。</summary>
public sealed class PdfCommandPaletteItem
{
    public PdfCommandPaletteItem(string title, string hint, Action execute)
    {
        Title = title;
        Hint = hint;
        Execute = execute;
    }

    public string Title { get; }
    public string Hint { get; }
    public Action Execute { get; }
}

/// <summary>
/// Ctrl+K 命令搜索：输入关键字筛选，回车执行；只承载入口，不直接操作业务状态。
/// </summary>
public sealed class PdfCommandPaletteDialog : Form
{
    private readonly List<PdfCommandPaletteItem> _items;
    private readonly List<PdfCommandPaletteItem> _visible = new();
    private readonly TextBox _filter;
    private readonly ListBox _list;
    private readonly Label _footHint;

    public PdfCommandPaletteDialog(IEnumerable<PdfCommandPaletteItem> items)
    {
        _items = items.ToList();

        Text = "命令搜索";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(468, 372);
        KeyPreview = true;
        Padding = new Padding(10, 8, 10, 6);

        _filter = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei UI", 11F),
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 0, 8)
        };
        _filter.TextChanged += (_, _) => RefreshList();
        _filter.KeyDown += Filter_KeyDown;

        _list = new ListBox
        {
            Dock = DockStyle.Fill,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 40,
            BorderStyle = BorderStyle.None,
            IntegralHeight = false,
            Margin = Padding.Empty
        };
        _list.DrawItem += List_DrawItem;
        _list.DoubleClick += (_, _) => ExecuteSelected();
        _list.KeyDown += List_KeyDown;

        _footHint = new Label
        {
            Dock = DockStyle.Fill,
            Text = "↑↓ 选择 · Enter 执行 · Esc 取消",
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Microsoft YaHei UI", 8.5F),
            Margin = new Padding(0, 6, 0, 0)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        layout.Controls.Add(_filter, 0, 0);
        layout.Controls.Add(_list, 0, 1);
        layout.Controls.Add(_footHint, 0, 2);
        Controls.Add(layout);

        RefreshList();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        PdfSidebarTheme.SetWindowDark(this, Owner != null && PdfSidebarTheme.IsDarkFor(Owner));
        var palette = PdfSidebarTheme.PaletteFor(this);
        BackColor = palette.Background;
        _filter.BackColor = palette.FieldBackground;
        _filter.ForeColor = palette.Text;
        _list.BackColor = palette.Surface;
        _list.ForeColor = palette.Text;
        _footHint.ForeColor = palette.Muted;

        _filter.Focus();
    }

    private void RefreshList()
    {
        string keyword = _filter.Text.Trim();
        _visible.Clear();

        foreach (var item in _items)
        {
            if (keyword.Length == 0 ||
                item.Title.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.Hint.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _visible.Add(item);
            }
        }

        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var item in _visible)
            _list.Items.Add(item);
        _list.EndUpdate();

        if (_visible.Count > 0)
            _list.SelectedIndex = 0;
    }

    private void Filter_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Down:
                MoveSelection(1);
                break;
            case Keys.Up:
                MoveSelection(-1);
                break;
            case Keys.Enter:
                ExecuteSelected();
                break;
            case Keys.Escape:
                Close();
                break;
            default:
                return;
        }

        e.Handled = e.SuppressKeyPress = true;
    }

    private void List_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            ExecuteSelected();
            e.Handled = e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            Close();
            e.Handled = e.SuppressKeyPress = true;
        }
    }

    private void MoveSelection(int delta)
    {
        if (_visible.Count == 0) return;

        int index = _list.SelectedIndex + delta;
        _list.SelectedIndex = Math.Max(0, Math.Min(_visible.Count - 1, index));
    }

    private void ExecuteSelected()
    {
        if (_list.SelectedIndex < 0 || _list.SelectedIndex >= _visible.Count) return;

        var item = _visible[_list.SelectedIndex];
        Close();
        item.Execute();
    }

    private void List_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _visible.Count) return;

        var palette = PdfSidebarTheme.PaletteFor(this);
        var item = _visible[e.Index];
        bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

        using var background = new SolidBrush(selected ? palette.BrandSoft : palette.Surface);
        e.Graphics.FillRectangle(background, e.Bounds);

        var titleFont = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
        var hintFont = new Font("Microsoft YaHei UI", 8.5F);
        try
        {
            TextRenderer.DrawText(e.Graphics, item.Title, titleFont,
                new Rectangle(e.Bounds.X + 10, e.Bounds.Y + 4, e.Bounds.Width - 20, 20),
                palette.Text, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(e.Graphics, item.Hint, hintFont,
                new Rectangle(e.Bounds.X + 10, e.Bounds.Y + 20, e.Bounds.Width - 20, 18),
                palette.Muted, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
        finally
        {
            titleFont.Dispose();
            hintFont.Dispose();
        }
    }
}
