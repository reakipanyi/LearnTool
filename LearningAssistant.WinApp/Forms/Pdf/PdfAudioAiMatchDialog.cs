using LearningAssistant.Services.Pdf;

namespace LearningAssistant.Forms.Pdf;

/// <summary>
/// AI 自动匹配结果预览：可勾选/修改页码，确认后写入手动匹配并立即生效。
/// 控件均在设计器中创建，便于可视化预览。
/// </summary>
public sealed partial class PdfAudioAiMatchDialog : Form
{
    private readonly List<PdfAudioAiMatchSuggestion> _suggestions;
    private readonly int _pageCount;

    /// <summary>页（0 基）→ 音频完整路径</summary>
    public Dictionary<int, string> SelectedMatches { get; } = new();

    /// <summary>供 Visual Studio 设计器实例化使用。</summary>
    public PdfAudioAiMatchDialog() : this(Array.Empty<PdfAudioAiMatchSuggestion>(), 1)
    {
    }

    public PdfAudioAiMatchDialog(IReadOnlyList<PdfAudioAiMatchSuggestion> suggestions, int pageCount)
    {
        _suggestions = suggestions.ToList();
        _pageCount = pageCount;

        InitializeComponent();

        labelSummary.Text = $"AI 给出 {_suggestions.Count} 条建议，可修改页码或取消勾选后应用。";
        FillGrid();
    }

    private void ButtonSelectAll_Click(object? sender, EventArgs e) => SetAllChecked(true);

    private void ButtonSelectNone_Click(object? sender, EventArgs e) => SetAllChecked(false);

    private void ButtonOk_Click(object? sender, EventArgs e) => OnConfirm();

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

            grid.Rows.Add(
                isBest,
                suggestion.FileName,
                suggestion.PageIndex + 1,
                suggestion.Confidence.ToString("P0"),
                reason);
        }
    }

    private void SetAllChecked(bool value)
    {
        grid.EndEdit();
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow) continue;
            row.Cells[0].Value = value;
        }
    }

    private void OnConfirm()
    {
        grid.EndEdit();
        SelectedMatches.Clear();
        var errors = new List<string>();
        var usedPages = new HashSet<int>();

        foreach (DataGridViewRow row in grid.Rows)
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
