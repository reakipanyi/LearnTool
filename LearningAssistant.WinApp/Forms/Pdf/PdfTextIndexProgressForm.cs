using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Forms.Pdf
{
    public partial class PdfReaderForm
    {
        /// <summary>文本索引构建进度窗口：显示逐页构建进度，并支持取消。</summary>
        private sealed class PdfTextIndexProgressForm : Form
        {
            private readonly Label _labelStatus;
            private readonly ProgressBar _progressBar;
            private readonly Button _buttonCancel;

            /// <summary>用户请求取消构建</summary>
            public event EventHandler? CancelRequested;

            public PdfTextIndexProgressForm()
            {
                Text = "建立文本索引";
                Font = new Font("Microsoft YaHei UI", 9F);
                FormBorderStyle = FormBorderStyle.FixedDialog;
                ControlBox = false;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.CenterParent;
                ClientSize = new Size(480, 172);
                BackColor = Color.White;

                var labelTitle = new Label
                {
                    Text = "正在建立文档文本索引",
                    Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(35, 42, 58),
                    Location = new Point(20, 18),
                    AutoSize = true
                };

                _labelStatus = new Label
                {
                    Text = "准备中…",
                    Location = new Point(20, 48),
                    Size = new Size(440, 20),
                    ForeColor = Color.FromArgb(70, 78, 96)
                };

                var labelDetail = new Label
                {
                    Text = "文本层优先，扫描页自动 OCR；结果会缓存，下次直接复用。",
                    Location = new Point(20, 70),
                    Size = new Size(440, 34),
                    ForeColor = Color.FromArgb(130, 138, 155)
                };

                _progressBar = new ProgressBar
                {
                    Location = new Point(20, 108),
                    Size = new Size(440, 16),
                    Minimum = 0,
                    Maximum = 100
                };

                _buttonCancel = new Button
                {
                    Text = "取消",
                    Location = new Point(370, 134),
                    Size = new Size(90, 30),
                    BackColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                _buttonCancel.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 225);
                _buttonCancel.Click += (_, _) =>
                {
                    _buttonCancel.Enabled = false;
                    _labelStatus.Text = "正在取消，等待当前页处理完成…";
                    CancelRequested?.Invoke(this, EventArgs.Empty);
                };

                Controls.AddRange(new Control[] { labelTitle, _labelStatus, labelDetail, _progressBar, _buttonCancel });
            }

            /// <summary>按索引构建进度刷新状态与进度条</summary>
            public void UpdateProgress(PdfTextIndexProgress progress)
            {
                if (IsDisposed || progress == null) return;

                if (progress.FromCache)
                {
                    _labelStatus.Text = "已命中文本索引缓存";
                    _progressBar.Value = 100;
                    return;
                }

                if (progress.TotalPages > 0)
                {
                    _progressBar.Value = Math.Clamp(
                        (int)Math.Round(100.0 * progress.ProcessedPages / progress.TotalPages), 0, 100);
                }

                var source = progress.Source switch
                {
                    PdfTextSource.TextLayer => "文本层",
                    PdfTextSource.Ocr => "OCR",
                    _ => "跳过"
                };
                _labelStatus.Text =
                    $"正在建立文本索引（{progress.ProcessedPages}/{progress.TotalPages}）· 第 {progress.PageIndex + 1} 页 · {source}";
            }
        }
    }
}
