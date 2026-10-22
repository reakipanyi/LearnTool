namespace LearningAssistant.Forms.Pdf
{
    /// <summary>
    /// 批量 OCR 导出进度窗口：显示当前文件、页进度，并支持取消。
    /// 控件均在设计器中创建，便于可视化预览。
    /// </summary>
    public sealed partial class BatchOcrExportProgressForm : Form
    {
        /// <summary>用户是否已请求取消</summary>
        public bool IsCancellationRequested { get; private set; }

        /// <summary>供 Visual Studio 设计器实例化使用。</summary>
        public BatchOcrExportProgressForm() : this(1)
        {
        }

        public BatchOcrExportProgressForm(int fileCount)
        {
            InitializeComponent();
            labelStatus.Text = $"准备导出 {fileCount} 个文件…";
        }

        private void ButtonCancel_Click(object? sender, EventArgs e)
        {
            IsCancellationRequested = true;
            buttonCancel.Enabled = false;
            labelStatus.Text = "正在取消，等待当前页处理完成…";
        }

        /// <summary>更新状态文本</summary>
        public void SetStatus(string text)
        {
            if (IsDisposed) return;
            labelStatus.Text = text;
        }

        /// <summary>更新当前文件的页进度</summary>
        public void SetPageProgress(int currentPage, int totalPages)
        {
            if (IsDisposed || totalPages <= 0) return;
            var value = (int)Math.Round(100.0 * currentPage / totalPages);
            progressBar.Value = Math.Clamp(value, 0, 100);
        }
    }
}
