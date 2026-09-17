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

            _zoomLevel = value;
            _form.TrackBarZoom.Value = value;
            _form.LabelZoom.Text = $"{_zoomLevel}%";
            _ = RenderPageAtZoomAsync();
        }

        public void ZoomByMouseWheel(int delta)
        {
            if (_isLocked) return;
            if (delta == 0) return;

            if (delta > 0) _zoomLevel = Math.Min(400, _zoomLevel + 10);
            else _zoomLevel = Math.Max(10, _zoomLevel - 10);

            _form.TrackBarZoom.Value = _zoomLevel;
            _form.LabelZoom.Text = $"{_zoomLevel}%";
            _ = RenderPageAtZoomAsync();
        }

        public void ResetZoom()
        {
            _zoomLevel = 100;
            _imageOffset = Point.Empty;
            _form.TrackBarZoom.Value = 100;
            _form.LabelZoom.Text = "100%";
            _ = RenderPageAtZoomAsync();
        }

        public void ToggleLockView()
        {
            _isLocked = !_isLocked;
            if (_form.ButtonLockView != null)
            {
                _form.ButtonLockView.Text = _isLocked ? "🔒" : "🔓";
                _form.ButtonLockView.BackColor = _isLocked ? Color.LightSalmon : Color.White;
            }
            _form.TrackBarZoom.Enabled = !_isLocked;
        }

        private async Task RenderPageAtZoomAsync()
        {
            try
            {
                var page = int.TryParse(_form.TextBoxPage.Text, out var p) ? p - 1 : 0;
                int targetW = (int)(_form.PictureBoxPdf.ClientSize.Width * _zoomLevel / 100.0);
                int targetH = (int)(_form.PictureBoxPdf.ClientSize.Height * _zoomLevel / 100.0);
                var bmp = await _form.Presenter!.RenderPageAsync(page, Math.Max(1, targetW), Math.Max(1, targetH));
                if (bmp != null)
                {
                    _form.Form.BeginInvoke(() => _form.DisplayImage(new Bitmap(new MemoryStream(bmp))));
                }
            }
            catch (OperationCanceledException)
            {
                // Render cancelled, ignore
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering page during zoom");
            }
        }
    }
}