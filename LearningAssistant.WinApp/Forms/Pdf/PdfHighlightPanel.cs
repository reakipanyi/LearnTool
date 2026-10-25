using System.ComponentModel;
using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

/// <summary>「高亮」侧栏面板：页头（删除 / 编辑 / 批量删除 / 导出）+ 摘录列表。</summary>
public partial class PdfHighlightPanel : UserControl
{
    public PdfHighlightPanel()
    {
        InitializeComponent();
        header.Resize += (_, _) => LayoutHeaderActions();
        LayoutHeaderActions();
        _tip.SetToolTip(removeButton, "删除所选高亮");
        _tip.SetToolTip(editButton, "编辑所选高亮");
        _tip.SetToolTip(batchRemoveButton, "批量删除所选高亮");
        removeButton.AccessibleName = "删除所选高亮";
        editButton.AccessibleName = "编辑所选高亮";
        batchRemoveButton.AccessibleName = "批量删除所选高亮";
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

    /// <summary>把三个操作按钮排到页头右侧，与「导出」同一行，并压缩副标题宽度避免遮挡。</summary>
    private void LayoutHeaderActions()
    {
        var exportButton = header.ActionButton;
        int right = exportButton != null ? exportButton.Left - 4 : header.Width - 4;
        foreach (var button in new[] { batchRemoveButton, editButton, removeButton })
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
