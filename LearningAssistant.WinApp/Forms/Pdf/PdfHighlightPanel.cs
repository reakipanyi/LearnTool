using System.ComponentModel;
using System.Drawing.Drawing2D;
using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

/// <summary>「高亮」侧栏面板：页头 + 颜色行 + 摘录列表 + 底部操作。</summary>
public partial class PdfHighlightPanel : UserControl
{
    public PdfHighlightPanel()
    {
        InitializeComponent();
        RoundColorButtons();
        _tip.SetToolTip(yellowRadio, "黄色高亮");
        _tip.SetToolTip(greenRadio, "绿色高亮");
        _tip.SetToolTip(blueRadio, "蓝色高亮");
        _tip.SetToolTip(pinkRadio, "粉色高亮");
        _tip.SetToolTip(orangeRadio, "橙色高亮");
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
    public RadioButton YellowButton => yellowRadio;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RadioButton GreenButton => greenRadio;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RadioButton BlueButton => blueRadio;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RadioButton PinkButton => pinkRadio;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RadioButton OrangeButton => orangeRadio;

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

    /// <summary>把颜色按钮裁剪成圆形，设计期与运行期一致。</summary>
    private void RoundColorButtons()
    {
        foreach (var button in new[] { yellowRadio, greenRadio, blueRadio, pinkRadio, orangeRadio })
        {
            using var path = new GraphicsPath();
            path.AddEllipse(0, 0, button.Width, button.Height);
            button.Region = new Region(path);
        }
    }
}
