using LearningAssistant.Abstractions;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Managers
{
    public class PdfZoomController
    {
        private readonly ILogger _logger;
        private readonly IPdfReaderFormAccess _form;
        private int _zoomLevel = 100;
        private Point _imageOffset = Point.Empty;
        private bool _isLocked = false;

        // 缩放去抖：拖动滑块/连续滚轮时合并为一次渲染，避免每个刻度都触发一次高分辨率光栅化
        private const int ZoomRenderDebounceMs = 180;
        private CancellationTokenSource? _zoomRenderCts;

        public PdfZoomController(ILogger logger, IPdfReaderFormAccess form)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _form = form ?? throw new ArgumentNullException(nameof(form));
        }

        public int ZoomLevel => _zoomLevel;
        public Point ImageOffset => _imageOffset;
        public void SetImageOffset(Point offset) => _imageOffset = offset;
        public bool IsLocked => _isLocked;

        public void Zoom(int value)
        {
            if (_isLocked) return;

            // 与滚轮保持同一区间（10%–400%），防止滑块值越界抛异常
            _zoomLevel = Math.Min(400, Math.Max(10, value));
            _form.TrackBarZoom.Value = _zoomLevel;
            _form.LabelZoom.Text = $"{_zoomLevel}%";
            ScheduleRender();
        }

        public void ZoomByMouseWheel(int delta)
        {
            if (_isLocked) return;
            if (delta == 0) return;

            if (delta > 0) _zoomLevel = Math.Min(400, _zoomLevel + 10);
            else _zoomLevel = Math.Max(10, _zoomLevel - 10);

            _form.TrackBarZoom.Value = _zoomLevel;
            _form.LabelZoom.Text = $"{_zoomLevel}%";
            ScheduleRender();
        }

        public void ResetZoom()
        {
            _zoomLevel = 100;
            _imageOffset = Point.Empty;
            _form.TrackBarZoom.Value = 100;
            _form.LabelZoom.Text = "100%";
            ScheduleRender();
        }

        public void ToggleLockView()
        {
            _isLocked = !_isLocked;
            if (_form.ButtonLockView != null)
            {
                _form.ButtonLockView.Text = _isLocked ? "🔒" : "🔓";
            }
            // 开启态统一交给窗体登记，这样夜间切换能按主题令牌重刷，不会残留日间浅紫
            _form.ApplyLockViewButtonState(_isLocked);
            _form.TrackBarZoom.Enabled = !_isLocked;
        }

        /// <summary>
        /// 登记一次缩放渲染请求：取消上一次等待，180ms 内的连续缩放合并为最后一次。
        /// </summary>
        private void ScheduleRender()
        {
            var cts = new CancellationTokenSource();
            var previous = Interlocked.Exchange(ref _zoomRenderCts, cts);
            if (previous != null)
            {
                try
                {
                    previous.Cancel();
                    previous.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // 已被释放，忽略
                }
            }

            _ = RenderPageAtZoomAsync(cts.Token);
        }

        private async Task RenderPageAtZoomAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(ZoomRenderDebounceMs, cancellationToken);
                if (cancellationToken.IsCancellationRequested) return;

                var presenter = _form.Presenter;
                if (presenter == null) return;

                var page = int.TryParse(_form.TextBoxPage.Text, out var p) ? p - 1 : 0;
                if (page < 0) page = 0;

                // 渲染、尺寸上限、代次校验与显示统一由 Presenter 负责，避免这里再做 PNG 往返
                await presenter.RenderPageAtZoomAsync(page, _zoomLevel);
            }
            catch (OperationCanceledException)
            {
                // 缩放请求已被更晚的请求取代，正常丢弃
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering page during zoom");
            }
        }
    }
}