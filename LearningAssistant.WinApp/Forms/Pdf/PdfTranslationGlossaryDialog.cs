using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Forms.Pdf;

/// <summary>
/// 翻译术语表管理：查看/编辑/删除本文档的术语条目，并显示术语在当前页的命中次数。
/// 控件均在设计器中创建，便于可视化预览。
/// </summary>
public sealed partial class PdfTranslationGlossaryDialog : Form
{
    private readonly string _pageText;

    /// <summary>保存时收集的术语条目。</summary>
    public List<PdfGlossaryEntry> Entries { get; } = new();

    /// <summary>供 Visual Studio 设计器实例化使用。</summary>
    public PdfTranslationGlossaryDialog() : this("当前文档", null, string.Empty)
    {
    }

    public PdfTranslationGlossaryDialog(string documentName, PdfTranslationGlossary? glossary, string? pageText)
    {
        InitializeComponent();

        _pageText = pageText ?? string.Empty;
        Text = $"翻译术语表 · {documentName}";
        FillGrid(glossary?.Entries);
    }

    private void FillGrid(IReadOnlyList<PdfGlossaryEntry>? entries)
    {
        grid.Rows.Clear();
        if (entries != null)
        {
            foreach (var entry in entries)
                grid.Rows.Add(entry.Source, entry.Target, CountOccurrences(_pageText, entry.Source));
        }

        UpdateSummary();
    }

    private void ButtonAdd_Click(object? sender, EventArgs e)
    {
        int index = grid.Rows.Add(string.Empty, string.Empty, 0);
        grid.CurrentCell = grid.Rows[index].Cells[0];
        grid.BeginEdit(true);
        UpdateSummary();
    }

    private void ButtonDelete_Click(object? sender, EventArgs e)
    {
        var rows = grid.SelectedCells
            .Cast<DataGridViewCell>()
            .Select(cell => cell.RowIndex)
            .Distinct()
            .OrderByDescending(index => index)
            .ToList();

        foreach (int rowIndex in rows)
        {
            if (rowIndex >= 0 && rowIndex < grid.Rows.Count)
                grid.Rows.RemoveAt(rowIndex);
        }

        UpdateSummary();
    }

    /// <summary>编辑原文术语后重新统计本页命中次数。</summary>
    private void Grid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex != columnSource.Index || e.RowIndex < 0) return;

        var row = grid.Rows[e.RowIndex];
        var source = Convert.ToString(row.Cells[columnSource.Index].Value);
        row.Cells[columnHit.Index].Value = CountOccurrences(_pageText, source);
        UpdateSummary();
    }

    private void ButtonSave_Click(object? sender, EventArgs e)
    {
        grid.EndEdit();

        Entries.Clear();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow) continue;

            var source = Convert.ToString(row.Cells[columnSource.Index].Value)?.Trim();
            var target = Convert.ToString(row.Cells[columnTarget.Index].Value)?.Trim();
            if (string.IsNullOrWhiteSpace(source) && string.IsNullOrWhiteSpace(target)) continue;

            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
            {
                MessageBox.Show(this, "每条术语都需要同时填写「原文术语」和「统一译名」。",
                    "术语表", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 原文术语去重，保留第一条译名，避免润色提示词出现冲突译名
            if (!seen.Add(source)) continue;
            Entries.Add(new PdfGlossaryEntry { Source = source, Target = target });
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    /// <summary>刷新顶部统计，并同步每行的本页命中数。</summary>
    private void UpdateSummary()
    {
        int total = 0;
        int hitTerms = 0;
        int hitOccurrences = 0;

        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow) continue;

            var source = Convert.ToString(row.Cells[columnSource.Index].Value);
            if (string.IsNullOrWhiteSpace(source)) continue;

            total++;
            int hits = CountOccurrences(_pageText, source);
            row.Cells[columnHit.Index].Value = hits;
            if (hits > 0)
            {
                hitTerms++;
                hitOccurrences += hits;
            }
        }

        labelSummary.Text = total == 0
            ? "本文档还没有术语表：可手动添加条目，或在开启「润色」并翻译一次后由 AI 自动生成。"
            : $"共 {total} 条术语；本页命中 {hitTerms} 个术语（{hitOccurrences} 次）。双击单元格可编辑，保存后立即用于后续翻译润色。";
    }

    /// <summary>统计术语在当前页原文中的出现次数（不区分大小写）。</summary>
    private static int CountOccurrences(string? text, string? term)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(term)) return 0;

        int count = 0;
        int index = 0;
        while (index < text.Length)
        {
            int found = text.IndexOf(term, index, StringComparison.OrdinalIgnoreCase);
            if (found < 0) break;

            count++;
            index = found + term.Length;
        }

        return count;
    }
}
