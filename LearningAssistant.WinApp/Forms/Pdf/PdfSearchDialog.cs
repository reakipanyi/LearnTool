namespace LearningAssistant.Forms.Pdf;

/// <summary>PDF 文本查找对话框。</summary>
public partial class PdfSearchDialog : Form
{
    public PdfSearchDialog()
    {
        InitializeComponent();
    }

    /// <summary>查找按钮，宿主负责绑定查找逻辑。</summary>
    public Button FindButton => buttonFind;

    /// <summary>查找内容。</summary>
    public string SearchText => textBoxSearch.Text;

    /// <summary>是否区分大小写。</summary>
    public bool CaseSensitive => checkCase.Checked;

    /// <summary>是否全字匹配。</summary>
    public bool WholeWord => checkWhole.Checked;
}
