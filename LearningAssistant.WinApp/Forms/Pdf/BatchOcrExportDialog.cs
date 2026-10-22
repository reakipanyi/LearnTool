using System.Text.RegularExpressions;

namespace LearningAssistant.Forms.Pdf
{

    /// <summary>
    /// 批量 OCR 导出设置对话框：页码范围、输出目录与输出格式。
    /// 控件均在设计器中创建，便于可视化预览。
    /// </summary>
    public sealed partial class BatchOcrExportDialog : Form
    {
        private const int DefaultStartPage = -1;
        private const int DefaultEndPage = -1;

        public BatchOcrExportOptions Options { get; private set; } = new();

        /// <summary>供 Visual Studio 设计器实例化使用。</summary>
        public BatchOcrExportDialog() : this(1)
        {
        }

        public BatchOcrExportDialog(int fileCount)
        {
            InitializeComponent();

            labelFiles.Text = $"已选择 {fileCount} 个 PDF 文件";
            textOutput.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private void ButtonBrowse_Click(object? sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog
            {
                Description = "选择导出目录",
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
                MessageBox.Show(this, "请选择输出目录。", "批量 OCR 导出", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "无法创建输出目录：" + ex.Message, "批量 OCR 导出", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!TryParsePageRange(textRange.Text, out var startIndex, out var endIndex))
            {
                MessageBox.Show(this, "页码范围格式不正确，请填写如 1-5，或留空表示全部页面。", "批量 OCR 导出",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Options = new BatchOcrExportOptions
            {
                OutputDirectory = outputDirectory,
                Markdown = radioMarkdown.Checked,
                StartPageIndex = startIndex,
                EndPageIndex = endIndex,
                UseAiCleanup = checkAiCleanup.Checked
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>解析页码范围文本，返回 0 基起止页；留空时返回 -1/-1 表示全部页面</summary>
        private static bool TryParsePageRange(string? text, out int startIndex, out int endIndex)
        {
            startIndex = DefaultStartPage;
            endIndex = DefaultEndPage;

            var rangeText = text?.Trim();
            if (string.IsNullOrEmpty(rangeText)) return true;

            var match = Regex.Match(rangeText, @"^(\d+)\s*(?:-\s*(\d+))?$");
            if (!match.Success) return false;

            if (!int.TryParse(match.Groups[1].Value, out var startPage) || startPage < 1)
                return false;

            var endPage = startPage;
            if (match.Groups[2].Success && !int.TryParse(match.Groups[2].Value, out endPage))
                return false;

            if (endPage < startPage)
                (startPage, endPage) = (endPage, startPage);

            startIndex = startPage - 1;
            endIndex = endPage - 1;
            return true;
        }
    }
    /// <summary>批量 OCR 导出选项</summary>
    public sealed class BatchOcrExportOptions
    {
        /// <summary>输出目录</summary>
        public string OutputDirectory { get; init; } = string.Empty;
        /// <summary>true 输出 Markdown，false 输出纯文本</summary>
        public bool Markdown { get; init; } = true;
        /// <summary>起始页（0 基），-1 表示第一页</summary>
        public int StartPageIndex { get; init; } = -1;
        /// <summary>结束页（0 基），-1 表示最后一页</summary>
        public int EndPageIndex { get; init; } = -1;
        /// <summary>是否启用 AI 智能整理（标题层级 / 去页眉页脚 / 段落合并 / 术语统一）</summary>
        public bool UseAiCleanup { get; init; }
    }
}
