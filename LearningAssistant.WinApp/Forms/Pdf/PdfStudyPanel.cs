using System.ComponentModel;
using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

/// <summary>「学习」侧栏面板：本页概览、一键操作与本页学习项列表。</summary>
public partial class PdfStudyPanel : UserControl
{
    public PdfStudyPanel()
    {
        InitializeComponent();
        // 整页录音入口已下线：隐藏按钮，由「本页跟读」与「整本跟读」占两行区域
        if (!PdfHiddenFeatures.EnablePageRecording)
        {
            recordingButton.Visible = false;
            practiceLayout.SetRow(shadowingButton, 0);
            practiceLayout.SetRow(wholeBookShadowingButton, 1);
            practiceLayout.RowCount = 2;
            practiceLayout.RowStyles.Clear();
            practiceLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            practiceLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        }
        // 卡片内容区由面板自行挂载，避免 VS 设计器回写时丢失
        overviewCard.Body.Controls.Add(overviewLayout);
        actionCard.Body.Controls.Add(actionLayout);
        audioCard.Body.Controls.Add(audioLayout);
        practiceCard.Body.Controls.Add(practiceLayout);
        aiCard.Body.Controls.Add(aiLayout);
        listCard.Body.Controls.Add(itemList);
        var tip = new ToolTip(components);
        tip.SetToolTip(autoHighlightButton,
            "自动框选当前页正文区域（排除页码）；再次点击清除本页框选；右键可整本框选");
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

    /// <summary>「自动高亮」按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button AutoHighlightButton => autoHighlightButton;

    /// <summary>
    /// 更新自动高亮按钮状态：任务进行中禁用并提示；当前页已有自动框选时显示清除文案。
    /// </summary>
    public void UpdateAutoHighlightState(bool busy, bool hasAutoHighlights)
    {
        if (busy)
        {
            autoHighlightButton.Enabled = false;
            autoHighlightButton.Text = "⏳ 正在框选";
            return;
        }

        autoHighlightButton.Enabled = true;
        autoHighlightButton.Text = hasAutoHighlights ? "🧹 清除本页" : "🖍 自动高亮";
    }

    /// <summary>「本页高亮全部加入学习」按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button AddHighlightsButton => addHighlightsButton;

    /// <summary>「打开高亮面板」按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button OpenHighlightButton => openHighlightButton;

    /// <summary>「生成本页闪卡」按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button FlashcardsButton => flashcardsButton;

    /// <summary>「打开配套音频」按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button AudioOpenButton => audioOpenButton;

    /// <summary>「打开翻译面板」按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button TranslateButton => translateButton;

    /// <summary>「整页录音」按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button RecordingButton => recordingButton;

    /// <summary>「开始本页跟读」按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button ShadowingButton => shadowingButton;

    /// <summary>「整本跟读」按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button WholeBookShadowingButton => wholeBookShadowingButton;

    /// <summary>「AI 总结」按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button SummaryButton => summaryButton;

    /// <summary>「全文问答」按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button AskButton => askButton;

    /// <summary>「生成大纲」按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button OutlineButton => outlineButton;

    /// <summary>「导出笔记」按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button NotesButton => notesButton;

    /// <summary>「批量 OCR」按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button BatchOcrButton => batchOcrButton;

    /// <summary>本页学习项列表容器。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public FlowLayoutPanel ItemList => itemList;

    /// <summary>卡片高度随侧栏宽度/DPI 变化重算：概览三条统计 + 四个按钮 + 会换行的说明文字。</summary>
    private void UpdateRowHeights()
    {
        int buttonHeight = Math.Max(32, addHighlightsButton.Font.Height + 16);
        int statHeight = Math.Max(26, highlightCount.Font.Height + 12);

        for (int i = 0; i < 4; i++)
            actionLayout.RowStyles[i].Height = buttonHeight + 6;

        int cardWidth = Math.Max(140, root.ClientSize.Width - 12);
        int hintWidth = Math.Max(80, cardWidth - 24);
        int hintHeight = Math.Max(20, TextRenderer.MeasureText(hint.Text, hint.Font,
            new Size(hintWidth, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height);

        float overviewHeight = statHeight * 3 + 44;
        float actionHeight = 40 + 4 * (buttonHeight + 6) + hintHeight + 6 + 4;
        float audioHeight = 40 + 2 * (buttonHeight + 4);
        // 录音按钮显示时三行（录音/本页跟读/整本跟读），隐藏时两行（本页/整本）
        int practiceRows = recordingButton.Visible ? 3 : 2;
        float practiceHeight = 40 + practiceRows * (buttonHeight + 6);
        float aiHeight = 40 + 3 * (buttonHeight + 4);

        if (Math.Abs(root.RowStyles[1].Height - overviewHeight) > 0.5f)
            root.RowStyles[1].Height = overviewHeight;
        if (Math.Abs(root.RowStyles[2].Height - actionHeight) > 0.5f)
            root.RowStyles[2].Height = actionHeight;
        if (root.RowStyles.Count > 3 && Math.Abs(root.RowStyles[3].Height - audioHeight) > 0.5f)
            root.RowStyles[3].Height = audioHeight;
        if (root.RowStyles.Count > 4 && Math.Abs(root.RowStyles[4].Height - practiceHeight) > 0.5f)
            root.RowStyles[4].Height = practiceHeight;
        if (root.RowStyles.Count > 5 && Math.Abs(root.RowStyles[5].Height - aiHeight) > 0.5f)
            root.RowStyles[5].Height = aiHeight;
    }
}
