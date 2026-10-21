using LearningAssistant.Services.Pdf;

namespace LearningAssistant.Forms.Pdf;

/// <summary>
/// AI 自动匹配结果预览：可勾选/修改页码，确认后写入手动匹配并立即生效。
/// </summary>
public sealed class PdfAudioAiMatchDialog : Form
{
    private readonly List<PdfAudioAiMatchSuggestion> _suggestions;
    private readonly int _pageCount;
    private readonly DataGridView _grid = new();
    private readonly Label _labelSummary = new();

    /// <summary>页（0 基）→ 音频完整路径</summary>
    public Dictionary<int, string> SelectedMatches { get; } = new();

    public PdfAudioAiMatchDialog(IReadOnlyList<PdfAudioAiMatchSuggestion> suggestions, int pageCount)
    {
        _suggestions = suggestions.ToList();
        _pageCount = pageCount;

        Text = "AI 自动匹配预览";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(760, 480);
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        Font = new Font("Microsoft YaHei UI", 9F);

        BuildLayout();
        FillGrid();
    }

    private void BuildLayout()
    {
        _labelSummary.Text = $"AI 给出 {_suggestions.Count} 条建议，可修改页码或取消勾选后应用。";
        _labelSummary.AutoSize = true;
        _labelSummary.Dock = DockStyle.Top;
        _labelSummary.Padding = new Padding(10, 10, 10, 4);

        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "选择",
            Width = 50,
            FillWeight = 50,
            TrueValue = true,
            FalseValue = false
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "音频文件",
            ReadOnly = true,
            FillWeight = 220
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "页码",
            FillWeight = 60
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "置信度",
            ReadOnly = true,
            FillWeight = 60
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "AI 依据",
            ReadOnly = true,
            FillWeight = 280
        });
        // 行顺序与建议列表一一对应，禁止点击表头排序打乱映射
        foreach (DataGridViewColumn column in _grid.Columns)
            column.SortMode = DataGridViewColumnSortMode.NotSortable;

        var btnSelectAll = new Button { Text = "全选", Width = 76, Height = 30 };
        var btnSelectNone = new Button { Text = "全不选", Width = 76, Height = 30 };
        var btnOk = new Button { Text = "应用匹配", Width = 96, Height = 30 };
        var btnCancel = new Button { Text = "取消", Width = 76, Height = 30, DialogResult = DialogResult.Cancel };

        btnSelectAll.Click += (_, _) => SetAllChecked(true);
        btnSelectNone.Click += (_, _) => SetAllChecked(false);
        btnOk.Click += OnConfirm;

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 46,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8, 8, 8, 8),
            WrapContents = false
        };
        buttons.Controls.Add(btnCancel);
        buttons.Controls.Add(btnOk);
        buttons.Controls.Add(btnSelectNone);
        buttons.Controls.Add(btnSelectAll);

        Controls.Add(_grid);
        Controls.Add(buttons);
        Controls.Add(_labelSummary);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    private void FillGrid()
    {
        // 同一页有多个候选时，默认只勾选置信度最高的一条
        var bestByPage = _suggestions
            .GroupBy(s => s.PageIndex)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.Confidence).First().FilePath);

        foreach (var suggestion in _suggestions)
        {
            var isBest = string.Equals(bestByPage[suggestion.PageIndex], suggestion.FilePath,
                StringComparison.OrdinalIgnoreCase);
            var reason = suggestion.Reason;
            if (!isBest)
                reason = string.IsNullOrWhiteSpace(reason)
                    ? "同页已有更优建议"
                    : reason + "（同页已有更优建议）";

            _grid.Rows.Add(
                isBest,
                suggestion.FileName,
                suggestion.PageIndex + 1,
                suggestion.Confidence.ToString("P0"),
                reason);
        }
    }

    private void SetAllChecked(bool value)
    {
        _grid.EndEdit();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow) continue;
            row.Cells[0].Value = value;
        }
    }

    private void OnConfirm(object? sender, EventArgs e)
    {
        _grid.EndEdit();
        SelectedMatches.Clear();
        var errors = new List<string>();
        var usedPages = new HashSet<int>();

        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow) continue;
            if (row.Cells[0].Value is not bool selected || !selected) continue;

            var suggestion = _suggestions[row.Index];
            if (!int.TryParse(Convert.ToString(row.Cells[2].Value), out var page) ||
                page < 1 || page > _pageCount)
            {
                errors.Add($"{suggestion.FileName}：页码需在 1-{_pageCount} 之间");
                continue;
            }
            if (!usedPages.Add(page - 1))
            {
                errors.Add($"第 {page} 页被多个音频同时选中，请取消多余的勾选");
                continue;
            }
            SelectedMatches[page - 1] = suggestion.FilePath;
        }

        if (errors.Count > 0)
        {
            MessageBox.Show(this, string.Join(Environment.NewLine, errors.Take(8)),
                "请检查页码", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (SelectedMatches.Count == 0)
        {
            MessageBox.Show(this, "请至少勾选一条匹配结果", "AI 自动匹配",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
