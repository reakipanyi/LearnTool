using System.ComponentModel;
using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

/// <summary>「高亮」侧栏面板：页头 + 颜色行 + 摘录列表 + 底部操作。</summary>
public partial class PdfHighlightPanel : UserControl
{
    public PdfHighlightPanel()
    {
        InitializeComponent();
    }

    /// <summary>页头，宿主用于更新摘录条数等状态。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SidebarHeaderBar Header => header;

    /// <summary>颜色按钮行，宿主把颜色单选按钮放入其中。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public FlowLayoutPanel ColorHost => colors;

    /// <summary>摘录列表容器，宿主放入列表与空状态提示。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Panel ListHost => listHost;

    /// <summary>底部操作行，宿主放入删除/编辑/批量删除按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public FlowLayoutPanel FooterPanel => footer;
}
