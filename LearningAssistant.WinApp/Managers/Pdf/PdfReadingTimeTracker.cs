using LearningAssistant.Services.Pdf;

namespace LearningAssistant.Managers
{
    /// <summary>
    /// 阅读时长统计：窗口激活且打开文档时按 30 秒心跳累计；
    /// 切换文档、失焦、关闭窗口前把不足一个心跳的时长补记进去。
    /// 统计只落盘到本地，不参与阅读渲染。
    /// </summary>
    public sealed class PdfReadingTimeTracker : IDisposable
    {
        private const int TickIntervalMs = 30_000;

        /// <summary>单次补记上限：防止系统休眠/UI 长时间阻塞后一次记入过长时间。</summary>
        private const int MaxFlushSeconds = 60;

        private readonly PdfReadingTimeStore _store;
        private readonly System.Windows.Forms.Timer _timer;

        private string? _documentPath;
        private DateTime _lastFlushUtc = DateTime.UtcNow;
        private bool _active;
        private bool _disposed;

        public PdfReadingTimeTracker(PdfReadingTimeStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _timer = new System.Windows.Forms.Timer { Interval = TickIntervalMs };
            _timer.Tick += (_, _) => FlushElapsed();
        }

        /// <summary>切换统计目标文档；会先把当前文档的剩余时长记账。</summary>
        public void SetDocument(string? pdfPath)
        {
            if (string.Equals(_documentPath, pdfPath, StringComparison.OrdinalIgnoreCase))
                return;

            FlushElapsed();
            _documentPath = string.IsNullOrWhiteSpace(pdfPath) ? null : pdfPath;
            _lastFlushUtc = DateTime.UtcNow;
            UpdateTimer();
        }

        /// <summary>窗口激活/失焦时调用；只在激活且已打开文档时计时。</summary>
        public void SetActive(bool active)
        {
            if (_active == active)
                return;

            FlushElapsed();
            _active = active;
            _lastFlushUtc = DateTime.UtcNow;
            UpdateTimer();
        }

        /// <summary>读取统计数据（含尚未落盘的当前片段）。</summary>
        public PdfReadingTimeSnapshot GetSnapshot()
        {
            FlushElapsed();
            return _store.GetSnapshot(_documentPath);
        }

        private void UpdateTimer()
        {
            if (_active && _documentPath != null)
            {
                if (!_timer.Enabled)
                    _timer.Start();
            }
            else
            {
                _timer.Stop();
            }
        }

        private void FlushElapsed()
        {
            var now = DateTime.UtcNow;
            int seconds = (int)Math.Floor((now - _lastFlushUtc).TotalSeconds);
            _lastFlushUtc = now;

            if (!_active || _documentPath == null || seconds <= 0)
                return;

            _store.Add(_documentPath, Math.Min(seconds, MaxFlushSeconds), DateTime.Now);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            FlushElapsed();
            _timer.Stop();
            _timer.Dispose();
        }
    }
}
