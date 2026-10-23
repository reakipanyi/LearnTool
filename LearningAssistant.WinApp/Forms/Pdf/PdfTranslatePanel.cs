using System.ComponentModel;
using LearningAssistant.Common.UI;
using LearningAssistant.Forms.UserControls.Common;

namespace LearningAssistant.Forms.Pdf;

/// <summary>「翻译」侧栏面板：页头 + 原文/译文卡片 + 操作区 + 选项区。</summary>
public partial class PdfTranslatePanel : UserControl
{
    public PdfTranslatePanel()
    {
        InitializeComponent();
        // 卡片内容区由面板自行挂载：VS 设计器无法序列化自定义容器的 Body，
        // 放在构造里可保证设计视图与运行期都能看到文本框。
        originalCard.Body.Controls.Add(originalBox);
        translationCard.Body.Controls.Add(translationBox);
        _tip.SetToolTip(ocrButton, "仅识别本页文字，不翻译；无文本层时自动调用 OCR");
        _tip.SetToolTip(shadowingButton, "用当前页原文进入句子跟读");
        _tip.SetToolTip(addToLearningButton, "把选区或本页原文加入个人学习库");
        _tip.SetToolTip(autoTranslateCheck, "识别后自动翻译原文");
        _tip.SetToolTip(autoSpeakCheck, "识别后自动朗读原文");
        _tip.SetToolTip(aiPolishCheck, "使用 AI 术语表润色译文，保持术语前后一致（失败时自动保留机器译文）");

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

    /// <summary>原文卡片，宿主为卡片右上角朗读按钮绑定事件。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SidebarCard OriginalCard => originalCard;

    /// <summary>译文卡片，宿主为卡片右上角朗读按钮绑定事件。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SidebarCard TranslationCard => translationCard;

    /// <summary>原文文本框。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SyncedTextBox OriginalBox => originalBox;

    /// <summary>译文文本框。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SyncedTextBox TranslationBox => translationBox;

    /// <summary>整页识别（OCR）按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button OcrButton => ocrButton;

    /// <summary>句子跟读按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button ShadowingButton => shadowingButton;

    /// <summary>添加到学习按钮。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button AddToLearningButton => addToLearningButton;

    /// <summary>识别后自动翻译。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CheckBox AutoTranslateCheck => autoTranslateCheck;

    /// <summary>识别后自动朗读。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CheckBox AutoSpeakCheck => autoSpeakCheck;

    /// <summary>AI 润色。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CheckBox AiPolishCheck => aiPolishCheck;

    /// <summary>语速选择。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SpeedSelectorControl TtsSpeedSelector => speedSelector;

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

/// <summary>带纵向滚动通知的多行文本框，用于原文/译文同步滚动。</summary>
public sealed class SyncedTextBox : TextBox
{
    private const int WM_VSCROLL = 0x0115;
    private const int WM_MOUSEWHEEL = 0x020A;

    public event EventHandler? VerticalScrolled;

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == WM_VSCROLL || m.Msg == WM_MOUSEWHEEL)
        {
            VerticalScrolled?.Invoke(this, EventArgs.Empty);
        }
    }
}
