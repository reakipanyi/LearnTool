using System.ComponentModel;
using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

/// <summary>「句子跟读」侧栏面板：说明文字、本页摘要与打开跟读窗口入口。</summary>
public partial class PdfShadowingPanel : UserControl
{
    public PdfShadowingPanel()
    {
        InitializeComponent();
        // 卡片内容区由面板自行挂载，避免 VS 设计器回写时丢失
        card.Body.Controls.Add(stack);
        card.Body.Resize += (_, _) => ResizeChildren();
        ResizeChildren();
    }

    /// <summary>本页原句摘要标签，由宿主更新内容。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Label SummaryLabel => summaryLabel;

    /// <summary>打开跟读练习窗口的按钮，由宿主订阅点击事件。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button OpenButton => openButton;

    private void ResizeChildren()
    {
        int width = Math.Max(120, card.Body.ClientSize.Width - 4);
        description.Width = width;
        summaryLabel.Width = width;
        hint.Width = width;
    }
}
