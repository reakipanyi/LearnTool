using System.ComponentModel;
using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

/// <summary>「学习」侧栏面板：本页概览、一键操作与本页学习项列表。</summary>
public partial class PdfStudyPanel : UserControl
{
    public PdfStudyPanel()
    {
        InitializeComponent();
        // 卡片内容区由面板自行挂载，避免 VS 设计器回写时丢失
        overviewCard.Body.Controls.Add(overviewLayout);
        actionCard.Body.Controls.Add(actionLayout);
        listCard.Body.Controls.Add(itemList);
        root.Resize += (_, _) => UpdateRowHeights();
        UpdateRowHeights();
    }

    /// <summary>页头，宿主用于刷新统计时更新状态。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SidebarHeaderBar Header => header;

    /// <summary>本页高亮条数标签。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Label HighlightCountLabel => highlightCount;

    /// <summary>本页已加入学习项条数标签。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Label LearningCountLabel => learningCount;

    /// <summary>本页可跟读句子条数标签。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Label SentenceCountLabel => sentenceCount;

    /// <summary>「本页高亮全部加入学习」按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button AddHighlightsButton => addHighlightsButton;

    /// <summary>「生成本页闪卡」按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button FlashcardsButton => flashcardsButton;

    /// <summary>「开始本页跟读」按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button ShadowingButton => shadowingButton;

    /// <summary>本页学习项列表容器。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public FlowLayoutPanel ItemList => itemList;

    /// <summary>卡片高度随侧栏宽度/DPI 变化重算：概览三条统计 + 三个按钮 + 会换行的说明文字。</summary>
    private void UpdateRowHeights()
    {
        int buttonHeight = Math.Max(32, addHighlightsButton.Font.Height + 16);
        int statHeight = Math.Max(26, highlightCount.Font.Height + 12);

        for (int i = 0; i < 3; i++)
            actionLayout.RowStyles[i].Height = buttonHeight + 6;

        int cardWidth = Math.Max(140, root.ClientSize.Width - 12);
        int hintWidth = Math.Max(80, cardWidth - 24);
        int hintHeight = Math.Max(20, TextRenderer.MeasureText(hint.Text, hint.Font,
            new Size(hintWidth, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height);

        float overviewHeight = statHeight * 3 + 44;
        float actionHeight = 40 + 3 * (buttonHeight + 6) + hintHeight + 6 + 4;

        if (Math.Abs(root.RowStyles[1].Height - overviewHeight) > 0.5f)
            root.RowStyles[1].Height = overviewHeight;
        if (Math.Abs(root.RowStyles[2].Height - actionHeight) > 0.5f)
            root.RowStyles[2].Height = actionHeight;
    }
}
