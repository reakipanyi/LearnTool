using System.Text.RegularExpressions;

namespace LearningAssistant.Forms.Pdf
{
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

    /// <summary>
    /// 批量 OCR 导出设置对话框：页码范围、输出目录与输出格式。
    /// 纯代码构建，避免额外的设计器文件。
    /// </summary>
    public sealed class BatchOcrExportDialog : Form
    {
        private static readonly Color _accentColor = Color.FromArgb(25, 118, 210);
        private static readonly Color _textColor = Color.FromArgb(50, 50, 60);

        private readonly TextBox _textRange;
        private readonly TextBox _textOutput;
        private readonly RadioButton _radioMarkdown;
        private readonly RadioButton _radioText;
        private readonly CheckBox _checkAiCleanup;

        private const int DefaultStartPage = -1;
        private const int DefaultEndPage = -1;

        public BatchOcrExportOptions Options { get; private set; } = new();

        public BatchOcrExportDialog(int fileCount)
        {
            Text = "批量 OCR 导出文本";
            Font = new Font("Microsoft YaHei UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(480, 350);
            BackColor = Color.White;

            var accentBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 4,
                BackColor = _accentColor
            };

            var labelFiles = new Label
            {
                Text = $"已选择 {fileCount} 个 PDF 文件",
                Location = new Point(20, 22),
                Size = new Size(440, 24),
                ForeColor = _textColor,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };

            var labelRange = new Label
            {
                Text = "页码范围（留空表示全部页面）",
                Location = new Point(20, 58),
                Size = new Size(300, 22),
                ForeColor = _textColor
            };

            _textRange = new TextBox
            {
                Location = new Point(20, 82),
                Size = new Size(200, 27),
                PlaceholderText = "例如：1-5"
            };

            var labelRangeHint = new Label
            {
                Text = "支持单页或区间，超出页数时按文档实际页数截断",
                Location = new Point(230, 86),
                Size = new Size(230, 22),
                ForeColor = Color.FromArgb(150, 150, 160)
            };

            var labelOutput = new Label
            {
                Text = "输出目录",
                Location = new Point(20, 120),
                Size = new Size(300, 22),
                ForeColor = _textColor
            };

            _textOutput = new TextBox
            {
                Location = new Point(20, 144),
                Size = new Size(356, 27),
                Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            var buttonBrowse = new Button
            {
                Text = "浏览…",
                Location = new Point(386, 142),
                Size = new Size(74, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            buttonBrowse.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 225);
            buttonBrowse.Click += ButtonBrowse_Click;

            var labelFormat = new Label
            {
                Text = "输出格式",
                Location = new Point(20, 186),
                Size = new Size(100, 22),
                ForeColor = _textColor
            };

            _radioMarkdown = new RadioButton
            {
                Text = "Markdown (.md)",
                Location = new Point(20, 210),
                Size = new Size(160, 24),
                Checked = true,
                ForeColor = _textColor
            };

            _radioText = new RadioButton
            {
                Text = "纯文本 (.txt)",
                Location = new Point(190, 210),
                Size = new Size(140, 24),
                ForeColor = _textColor
            };

            _checkAiCleanup = new CheckBox
            {
                Text = "AI 智能整理（标题层级 / 去页眉页脚 / 段落合并 / 术语统一）",
                Location = new Point(20, 242),
                Size = new Size(440, 24),
                ForeColor = _textColor,
                Checked = false
            };

            var labelAiHint = new Label
            {
                Text = "整理失败时自动回退原始 OCR 文本，不中断导出",
                Location = new Point(40, 266),
                Size = new Size(420, 22),
                ForeColor = Color.FromArgb(150, 150, 160)
            };

            var buttonOk = new Button
            {
                Text = "开始导出",
                Location = new Point(250, 298),
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
                Location = new Point(360, 298),
                Size = new Size(100, 34),
                BackColor = Color.White,
                ForeColor = _textColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            buttonCancel.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 225);
            buttonCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.AddRange(new Control[]
            {
                accentBar, labelFiles, labelRange, _textRange, labelRangeHint,
                labelOutput, _textOutput, buttonBrowse, labelFormat,
                _radioMarkdown, _radioText, _checkAiCleanup, labelAiHint, buttonOk, buttonCancel
            });

            AcceptButton = buttonOk;
            CancelButton = buttonCancel;
        }

        private void ButtonBrowse_Click(object? sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog
            {
                Description = "选择导出目录",
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

            if (!TryParsePageRange(_textRange.Text, out var startIndex, out var endIndex))
            {
                MessageBox.Show(this, "页码范围格式不正确，请填写如 1-5，或留空表示全部页面。", "批量 OCR 导出",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Options = new BatchOcrExportOptions
            {
                OutputDirectory = outputDirectory,
                Markdown = _radioMarkdown.Checked,
                StartPageIndex = startIndex,
                EndPageIndex = endIndex,
                UseAiCleanup = _checkAiCleanup.Checked
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

    /// <summary>
    /// 批量 OCR 导出进度窗口：显示当前文件、页进度，并支持取消。
    /// </summary>
    public sealed class BatchOcrExportProgressForm : Form
    {
        private readonly Label _labelStatus;
        private readonly ProgressBar _progressBar;
        private readonly Button _buttonCancel;

        /// <summary>用户是否已请求取消</summary>
        public bool IsCancellationRequested { get; private set; }

        public BatchOcrExportProgressForm(int fileCount)
        {
            Text = "批量 OCR 导出";
            Font = new Font("Microsoft YaHei UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ControlBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 150);
            BackColor = Color.White;

            _labelStatus = new Label
            {
                Text = $"准备导出 {fileCount} 个文件…",
                Location = new Point(20, 20),
                Size = new Size(420, 40),
                ForeColor = Color.FromArgb(50, 50, 60)
            };

            _progressBar = new ProgressBar
            {
                Location = new Point(20, 70),
                Size = new Size(420, 18),
                Minimum = 0,
                Maximum = 100
            };

            _buttonCancel = new Button
            {
                Text = "取消",
                Location = new Point(350, 106),
                Size = new Size(90, 30),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _buttonCancel.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 225);
            _buttonCancel.Click += (s, e) =>
            {
                IsCancellationRequested = true;
                _buttonCancel.Enabled = false;
                _labelStatus.Text = "正在取消，等待当前页处理完成…";
            };

            Controls.AddRange(new Control[] { _labelStatus, _progressBar, _buttonCancel });
        }

        /// <summary>更新状态文本</summary>
        public void SetStatus(string text)
        {
            if (IsDisposed) return;
            _labelStatus.Text = text;
        }

        /// <summary>更新当前文件的页进度</summary>
        public void SetPageProgress(int currentPage, int totalPages)
        {
            if (IsDisposed || totalPages <= 0) return;
            var value = (int)Math.Round(100.0 * currentPage / totalPages);
            _progressBar.Value = Math.Clamp(value, 0, 100);
        }
    }
}
