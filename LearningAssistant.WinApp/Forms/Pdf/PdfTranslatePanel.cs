using System.ComponentModel;
using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

/// <summary>「翻译」侧栏面板：页头 + 原文/译文卡片 + 操作区 + 选项区。</summary>
public partial class PdfTranslatePanel : UserControl
{
    public PdfTranslatePanel()
    {
        InitializeComponent();
        body.Resize += (_, _) =>
        {
            // 设计器内不重排行列样式，避免 VS 把运行期结果写回 Designer 文件
            if (!DesignMode) LayoutCards();
        };
    }

    /// <summary>运行期按实际宽度决定卡片上下堆叠还是左右对照。</summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        LayoutCards();
    }

    /// <summary>页头，宿主用于更新副标题状态。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SidebarHeaderBar Header => header;

    /// <summary>原文卡片，宿主把原文文本框放入其内容区。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SidebarCard OriginalCard => originalCard;

    /// <summary>译文卡片，宿主把译文文本框放入其内容区。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SidebarCard TranslationCard => translationCard;

    /// <summary>操作区：句子跟读、添加到学习。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public FlowLayoutPanel ActionsPanel => actions;

    /// <summary>选项区：自动翻译、自动朗读、AI 润色、倍速。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public FlowLayoutPanel OptionsPanel => options;

    /// <summary>窄侧栏上下堆叠、宽侧栏左右对照。</summary>
    private void LayoutCards()
    {
        bool wide = body.ClientSize.Width >= 420;
        body.SuspendLayout();
        body.ColumnStyles.Clear();
        body.RowStyles.Clear();

        if (wide)
        {
            body.ColumnCount = 2;
            body.RowCount = 1;
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            body.SetCellPosition(originalCard, new TableLayoutPanelCellPosition(0, 0));
            body.SetCellPosition(translationCard, new TableLayoutPanelCellPosition(1, 0));
            originalCard.Margin = new Padding(0, 0, 2, 0);
            translationCard.Margin = new Padding(2, 0, 0, 0);
        }
        else
        {
            body.ColumnCount = 1;
            body.RowCount = 2;
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            body.SetCellPosition(originalCard, new TableLayoutPanelCellPosition(0, 0));
            body.SetCellPosition(translationCard, new TableLayoutPanelCellPosition(0, 1));
            originalCard.Margin = new Padding(0, 0, 0, 2);
            translationCard.Margin = new Padding(0, 2, 0, 0);
        }

        body.ResumeLayout(true);
    }
}
