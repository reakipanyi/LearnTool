namespace LearningAssistant.Forms.Pdf
{

    /// <summary>
    /// AI 闪卡生成设置：选择页码范围。控件均在设计器中创建，便于可视化预览。
    /// </summary>
    public sealed partial class PdfAiFlashcardDialog : Form
    {
        public PdfAiFlashcardOptions Options { get; private set; } = new();

        /// <summary>供 Visual Studio 设计器实例化使用。</summary>
        public PdfAiFlashcardDialog() : this(1)
        {
        }

        public PdfAiFlashcardDialog(int pageCount)
        {
            InitializeComponent();
            labelRange.Text = $"页码范围（共 {pageCount} 页，留空表示全部页面）";
        }

        private void ButtonCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void ButtonOk_Click(object? sender, EventArgs e)
        {
            if (!PdfAiNotesExportDialog.TryParsePageRange(textRange.Text, out var startIndex, out var endIndex))
            {
                MessageBox.Show(this, "页码范围格式不正确，请填写如 1-5，或留空表示全部页面。", "AI 闪卡生成",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Options = new PdfAiFlashcardOptions
            {
                StartPageIndex = startIndex,
                EndPageIndex = endIndex
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }

    /// <summary>AI 闪卡生成的页码范围选项</summary>
    public sealed class PdfAiFlashcardOptions
    {
        /// <summary>起始页（0 基），-1 表示第一页</summary>
        public int StartPageIndex { get; init; } = -1;
        /// <summary>结束页（0 基），-1 表示最后一页</summary>
        public int EndPageIndex { get; init; } = -1;
    }

    /// <summary>单张 AI 闪卡</summary>
    public sealed class PdfAiFlashcardCard
    {
        /// <summary>正面问题或概念</summary>
        public string Front { get; init; } = string.Empty;
        /// <summary>背面答案或解释</summary>
        public string Back { get; init; } = string.Empty;
        /// <summary>来源页码（从 1 开始，0 表示未标注）</summary>
        public int PageNumber { get; init; }
    }

}
