using System.Text.RegularExpressions;

namespace LearningAssistant.Forms.Pdf
{
    /// <summary>AI 学习笔记导出选项</summary>
    public sealed class PdfAiNotesExportOptions
    {
        /// <summary>输出目录</summary>
        public string OutputDirectory { get; init; } = string.Empty;
        /// <summary>起始页（0 基），-1 表示第一页</summary>
        public int StartPageIndex { get; init; } = -1;
        /// <summary>结束页（0 基），-1 表示最后一页</summary>
        public int EndPageIndex { get; init; } = -1;
    }

    /// <summary>
    /// AI 学习笔记导出设置：页码范围与输出目录。纯代码构建，避免设计器文件。
    /// </summary>
    public sealed class PdfAiNotesExportDialog : Form
    {
        private static readonly Color _accentColor = Color.FromArgb(139, 92, 246);
        private static readonly Color _textColor = Color.FromArgb(50, 50, 60);

        private readonly TextBox _textRange;
        private readonly TextBox _textOutput;

        public PdfAiNotesExportOptions Options { get; private set; } = new();

        public PdfAiNotesExportDialog(int pageCount, string documentName)
        {
            Text = "AI 学习笔记导出";
            Font = new Font("Microsoft YaHei UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(480, 262);
            BackColor = Color.White;

            var accentBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 4,
                BackColor = _accentColor
            };

            var labelDoc = new Label
            {
                Text = $"{documentName}（共 {pageCount} 页）",
                Location = new Point(20, 22),
                Size = new Size(440, 24),
                ForeColor = _textColor,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                AutoEllipsis = true
            };

            var labelRange = new Label
            {
                Text = "页码范围（留空表示全部页面）",
                Location = new Point(20, 56),
                Size = new Size(300, 22),
                ForeColor = _textColor
            };

            _textRange = new TextBox
            {
                Location = new Point(20, 80),
                Size = new Size(200, 27),
                PlaceholderText = "例如：1-5"
            };

            var labelRangeHint = new Label
            {
                Text = "支持单页或区间，超出页数时按文档实际页数截断",
                Location = new Point(230, 84),
                Size = new Size(230, 22),
                ForeColor = Color.FromArgb(150, 150, 160)
            };

            var labelOutput = new Label
            {
                Text = "输出目录",
                Location = new Point(20, 118),
                Size = new Size(300, 22),
                ForeColor = _textColor
            };

            _textOutput = new TextBox
            {
                Location = new Point(20, 142),
                Size = new Size(356, 27),
                Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            var buttonBrowse = new Button
            {
                Text = "浏览…",
                Location = new Point(386, 140),
                Size = new Size(74, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            buttonBrowse.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 225);
            buttonBrowse.Click += ButtonBrowse_Click;

            var labelFormat = new Label
            {
                Text = "输出格式：Markdown（.md），含页码锚点与结构化标题",
                Location = new Point(20, 182),
                Size = new Size(440, 22),
                ForeColor = Color.FromArgb(120, 120, 132)
            };

            var buttonOk = new Button
            {
                Text = "生成并导出",
                Location = new Point(250, 214),
                Size = new Size(100, 34),
                BackColor = _accentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            buttonOk.FlatAppearance.BorderSize = 0;
            buttonOk.Click += ButtonOk_Click;

            var buttonCancel = new Button
            {
                Text = "取消",
                Location = new Point(360, 214),
                Size = new Size(100, 34),
                BackColor = Color.White,
                ForeColor = _textColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            buttonCancel.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 225);
            buttonCancel.Click += (_, _) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.AddRange(new Control[]
            {
                accentBar, labelDoc, labelRange, _textRange, labelRangeHint,
                labelOutput, _textOutput, buttonBrowse, labelFormat, buttonOk, buttonCancel
            });

            AcceptButton = buttonOk;
            CancelButton = buttonCancel;
        }

        private void ButtonBrowse_Click(object? sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog
            {
                Description = "选择笔记输出目录",
                SelectedPath = Directory.Exists(_textOutput.Text) ? _textOutput.Text : string.Empty
            };
            if (folderDialog.ShowDialog(this) == DialogResult.OK)
            {
                _textOutput.Text = folderDialog.SelectedPath;
            }
        }

        private void ButtonOk_Click(object? sender, EventArgs e)
        {
            var outputDirectory = _textOutput.Text.Trim();
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

            if (!TryParsePageRange(_textRange.Text, out var startIndex, out var endIndex))
            {
                MessageBox.Show(this, "页码范围格式不正确，请填写如 1-5，或留空表示全部页面。", "AI 学习笔记导出",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Options = new PdfAiNotesExportOptions
            {
                OutputDirectory = outputDirectory,
                StartPageIndex = startIndex,
                EndPageIndex = endIndex
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>解析页码范围文本，返回 0 基起止页；留空时返回 -1/-1 表示全部页面</summary>
        private static bool TryParsePageRange(string? text, out int startIndex, out int endIndex)
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
}
