using System.ComponentModel;
using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

/// <summary>「高亮」侧栏面板：页头（删除 / 编辑 / 批量删除 / 导出）+ 摘录列表。</summary>
public partial class PdfHighlightPanel : UserControl
{
    public PdfHighlightPanel()
    {
        InitializeComponent();
        ConfigureExportButton();
        header.Resize += (_, _) => LayoutHeaderActions();
        LayoutHeaderActions();
        _tip.SetToolTip(removeButton, "删除所选高亮");
        _tip.SetToolTip(editButton, "编辑所选高亮");
        _tip.SetToolTip(batchRemoveButton, "批量删除所选高亮");
        _tip.SetToolTip(autoHighlightButton, "自动高亮：框选本页文字（右键整本）");
        _tip.SetToolTip(mergeMultiLineCheck, "开启后连续多行合并为一个高亮块（默认）；关闭后每行单独一个高亮块");
        removeButton.AccessibleName = "删除所选高亮";
        editButton.AccessibleName = "编辑所选高亮";
        batchRemoveButton.AccessibleName = "批量删除所选高亮";
        autoHighlightButton.AccessibleName = "自动高亮";
        mergeMultiLineCheck.AccessibleName = "多行合并开关";
    }

    /// <summary>
    /// 把页头的「导出」改成与删除/编辑/批量删除一致的图标按钮，
    /// 四个操作保持相同尺寸、圆角边框和配色体系。
    /// </summary>
    private void ConfigureExportButton()
    {
        var exportButton = header.ActionButton;
        if (exportButton == null) return;

        exportButton.Text = "⬇";
        exportButton.Size = new Size(32, 26);
        exportButton.Font = new Font("Segoe UI Emoji", 9F);
        exportButton.FlatStyle = FlatStyle.Flat;
        exportButton.BackColor = Color.FromArgb(220, 252, 231);
        exportButton.ForeColor = Color.FromArgb(22, 101, 52);
        exportButton.FlatAppearance.BorderSize = 1;
        exportButton.FlatAppearance.BorderColor = Color.FromArgb(134, 239, 172);
        exportButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(187, 247, 208);
        exportButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(134, 239, 172);
        exportButton.Cursor = Cursors.Hand;
        exportButton.Margin = Padding.Empty;
        // keep：主题切换时保留这组自定义配色，交给 PdfHighlightPanel 自己维护
        exportButton.Tag = "keep";
        exportButton.AccessibleName = "导出高亮";
        _tip.SetToolTip(exportButton, "导出高亮");
    }

    /// <summary>页头，宿主用于更新摘录条数等状态。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SidebarHeaderBar Header => header;

    /// <summary>摘录列表容器，宿主可放入空状态提示。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Panel ListHost => listHost;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ListBox HighlightList => highlightList;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button RemoveButton => removeButton;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button EditButton => editButton;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button BatchRemoveButton => batchRemoveButton;

    /// <summary>自动高亮入口；右键菜单由宿主挂接。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button AutoHighlightButton => autoHighlightButton;

    /// <summary>多行合并开关，默认选中；关闭后每个文字行单独生成高亮块。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CheckBox MergeMultiLineCheck => mergeMultiLineCheck;

    /// <summary>
    /// 同步自动高亮按钮状态：忙碌时禁用；本页已有自动框时切换为「清除」语义。
    /// </summary>
    public void UpdateAutoHighlightState(bool busy, bool hasAutoHighlights)
    {
        if (busy)
        {
            autoHighlightButton.Enabled = false;
            autoHighlightButton.Text = "⏳";
            return;
        }

        autoHighlightButton.Enabled = true;
        autoHighlightButton.Text = "🖍";
        autoHighlightButton.BackColor = hasAutoHighlights
            ? Color.FromArgb(255, 237, 213)
            : Color.FromArgb(255, 247, 237);
        _tip.SetToolTip(
            autoHighlightButton,
            hasAutoHighlights ? "清除本页自动高亮（右键整本）" : "自动高亮：框选本页文字（右键整本）");
    }

    /// <summary>整本框选进度收敛到页头副标题，避免挤压 32px 图标按钮。</summary>
    public void ShowAutoHighlightProgress(int current, int total, bool ocr)
    {
        header.Subtitle.Text = $"框选中 {current}/{total}{(ocr ? " · OCR" : string.Empty)}";
    }

    /// <summary>把三个操作按钮和自动高亮排到页头右侧，与「导出」同一行，并压缩副标题宽度。</summary>
    private void LayoutHeaderActions()
    {
        var exportButton = header.ActionButton;
        if (exportButton != null)
            exportButton.Location = new Point(exportButton.Left, 11);

        int right = exportButton != null ? exportButton.Left - 4 : header.Width - 4;
        foreach (var button in new[] { autoHighlightButton, batchRemoveButton, editButton, removeButton })
        {
            button.Location = new Point(right - button.Width, 11);
            right = button.Left - 4;
        }

        header.Subtitle.AutoSize = false;
        header.Subtitle.AutoEllipsis = true;
        header.Subtitle.Location = new Point(28, 25);
        header.Subtitle.Size = new Size(Math.Max(40, right - 32), 18);
    }
}
