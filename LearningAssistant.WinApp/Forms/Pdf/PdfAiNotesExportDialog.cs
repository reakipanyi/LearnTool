using System.Text.RegularExpressions;

namespace LearningAssistant.Forms.Pdf
{

    /// <summary>
    /// AI 学习笔记导出设置：页码范围与输出目录。控件均在设计器中创建，便于可视化预览。
    /// </summary>
    public sealed partial class PdfAiNotesExportDialog : Form
    {
        public PdfAiNotesExportOptions Options { get; private set; } = new();

        /// <summary>供 Visual Studio 设计器实例化使用。</summary>
        public PdfAiNotesExportDialog() : this(1, string.Empty)
        {
        }

        public PdfAiNotesExportDialog(int pageCount, string documentName)
        {
            InitializeComponent();

            labelDoc.Text = $"{documentName}（共 {pageCount} 页）";
            textOutput.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private void ButtonBrowse_Click(object? sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog
            {
                Description = "选择笔记输出目录",
                SelectedPath = Directory.Exists(textOutput.Text) ? textOutput.Text : string.Empty
            };
            if (folderDialog.ShowDialog(this) == DialogResult.OK)
            {
                textOutput.Text = folderDialog.SelectedPath;
            }
        }

        private void ButtonCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void ButtonOk_Click(object? sender, EventArgs e)
        {
            var outputDirectory = textOutput.Text.Trim();
            if (string.IsNullOrEmpty(outputDirectory))
            {
                MessageBox.Show(this, "请选择输出目录。", "AI 学习笔记导出", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "无法创建输出目录：" + ex.Message, "AI 学习笔记导出",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!TryParsePageRange(textRange.Text, out var startIndex, out var endIndex))
            {
                MessageBox.Show(this, "页码范围格式不正确，请填写如 1-5，或留空表示全部页面。", "AI 学习笔记导出",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Options = new PdfAiNotesExportOptions
            {
                OutputDirectory = outputDirectory,
                StartPageIndex = startIndex,
                EndPageIndex = endIndex,
                SaveToNotes = checkSaveToNotes.Checked
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>解析页码范围文本，返回 0 基起止页；留空时返回 -1/-1 表示全部页面</summary>
        internal static bool TryParsePageRange(string? text, out int startIndex, out int endIndex)
        {
            startIndex = -1;
            endIndex = -1;

            var rangeText = text?.Trim();
            if (string.IsNullOrEmpty(rangeText)) return true;

            var match = Regex.Match(rangeText, @"^(\d+)\s*(?:-\s*(\d+))?$");
            if (!match.Success) return false;

            int start = int.Parse(match.Groups[1].Value);
            int end = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : start;
            if (start <= 0 || end <= 0 || end < start) return false;

            startIndex = start - 1;
            endIndex = end - 1;
            return true;
        }
    }
    /// <summary>AI 学习笔记导出选项</summary>
    public sealed class PdfAiNotesExportOptions
    {
        /// <summary>输出目录</summary>
        public string OutputDirectory { get; init; } = string.Empty;
        /// <summary>起始页（0 基），-1 表示第一页</summary>
        public int StartPageIndex { get; init; } = -1;
        /// <summary>结束页（0 基），-1 表示最后一页</summary>
        public int EndPageIndex { get; init; } = -1;
        /// <summary>是否同时写入笔记库</summary>
        public bool SaveToNotes { get; init; } = true;
    }
}
