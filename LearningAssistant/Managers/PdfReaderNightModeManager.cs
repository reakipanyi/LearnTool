using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Managers
{
    public class PdfReaderNightModeManager : IDisposable
    {
        private readonly ILogger _logger;
        private bool _isNightMode = false;
        private readonly IPdfReaderFormAccess _form;
        private bool _disposed = false;

        public bool IsNightMode => _isNightMode;

        public PdfReaderNightModeManager(ILogger logger, IPdfReaderFormAccess form)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _form = form ?? throw new ArgumentNullException(nameof(form));
        }

        public void ToggleNightMode()
        {
            _isNightMode = !_isNightMode;
            ApplyNightMode();
            
            if (_form.ButtonNightMode != null)
            {
                _form.ButtonNightMode.Text = _isNightMode ? "☀️" : "🌙";
            }

            if (_form.Presenter != null)
            {
                _ = _form.Presenter.RenderAndDisplayCurrentPageAsync();
            }
        }

        public void ApplyNightMode()
        {
            try
            {
                ApplyNightModeToMainForm();
                ApplyNightModeToTabPageTranslate();
                ApplyNightModeToBookmarksAndHighlights();
                ApplyNightModeToStatusBar();
                UpdateThumbnailsBackground();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying night mode");
            }
        }

        private void ApplyNightModeToMainForm()
        {
            if (_isNightMode)
            {
                _form.BackColor = Color.FromArgb(30, 30, 30);
                _form.PanelPdf!.BackColor = Color.FromArgb(20, 20, 20);
                _form.PanelNavigation!.BackColor = Color.FromArgb(45, 45, 45);
                _form.PanelLeftContainer!.BackColor = Color.FromArgb(35, 35, 35);
                _form.TreeViewFiles!.BackColor = Color.FromArgb(40, 40, 40);
                _form.TreeViewFiles.ForeColor = Color.White;
                _form.TabControlLeft!.BackColor = Color.FromArgb(40, 40, 40);
                _form.PanelThumbnails!.BackColor = Color.FromArgb(40, 40, 40);
                _form.FlowLayoutPanelThumbnails!.BackColor = Color.FromArgb(40, 40, 40);

                UpdateButtonNightModeColor(_form.ButtonNightMode, true);
                UpdateButtonAiColor(_form.ButtonAskAi, true);
            }
            else
            {
                _form.BackColor = Color.FromArgb(240, 240, 240);
                _form.PanelPdf!.BackColor = Color.White;
                _form.PanelNavigation!.BackColor = Color.FromArgb(240, 240, 240);
                _form.PanelLeftContainer!.BackColor = Color.FromArgb(240, 240, 240);
                _form.TreeViewFiles!.BackColor = Color.White;
                _form.TreeViewFiles.ForeColor = Color.Black;
                _form.TabControlLeft!.BackColor = Color.White;
                _form.PanelThumbnails!.BackColor = Color.FromArgb(240, 240, 240);
                _form.FlowLayoutPanelThumbnails!.BackColor = Color.FromArgb(240, 240, 240);

                UpdateButtonNightModeColor(_form.ButtonNightMode, false);
                UpdateButtonAiColor(_form.ButtonAskAi, false);
            }
        }

        private void UpdateButtonNightModeColor(Button? button, bool isNightMode)
        {
            if (button == null) return;
            button.BackColor = isNightMode ? Color.FromArgb(45, 45, 45) : Color.White;
        }

        private void UpdateButtonAiColor(Button? button, bool isNightMode)
        {
            if (button == null) return;
            button.BackColor = Color.FromArgb(0, 120, 215);
            button.ForeColor = Color.White;
        }

        private void ApplyNightModeToTabPageTranslate()
        {
            ApplyControlBackColor(_form.TabPageTranslate, _isNightMode);
            ApplyGroupBoxNightMode(_form.GroupBoxProgress);
            ApplyTextBoxNightMode(_form.TextBoxOriginal);
            ApplyTextBoxNightMode(_form.TextBoxTranslation);
            ApplyLabelForeColor(_form.LabelOriginal);
            ApplyLabelForeColor(_form.LabelTranslation);

            ApplyButtonNightMode(_form.ButtonTranslate);
            ApplyButtonNightMode(_form.ButtonSpeakOriginal);
            ApplyButtonNightMode(_form.ButtonSpeakTranslation);
        }

        private void ApplyNightModeToBookmarksAndHighlights()
        {
            ApplyControlBackColor(_form.TabPageBookmarksAndHighlights, _isNightMode);
            
            ApplyGroupBoxNightMode(_form.GroupBoxBookmarks);
            ApplyListBoxNightMode(_form.ListBoxBookmarks);
            ApplyTextBoxNightMode(_form.TextBoxBookmarkTitle);
            ApplyButtonNightMode(_form.ButtonAddBookmark);
            ApplyButtonNightMode(_form.ButtonRemoveBookmark);

            ApplyGroupBoxNightMode(_form.GroupBoxHighlights);
            ApplyListBoxNightMode(_form.ListBoxHighlights);
            ApplyGroupBoxNightMode(_form.GroupBoxHighlightColor);
            ApplyButtonNightMode(_form.ButtonRemoveHighlight);
            ApplyButtonNightMode(_form.ButtonBatchRemoveHighlight);
            ApplyButtonNightMode(_form.ButtonExportHighlights);
        }

        private void ApplyControlBackColor(Control? control, bool isNightMode)
        {
            if (control == null) return;
            control.BackColor = isNightMode ? Color.FromArgb(40, 40, 40) : Color.White;
        }

        private void ApplyGroupBoxNightMode(GroupBox? groupBox)
        {
            if (groupBox == null) return;
            groupBox.BackColor = _isNightMode ? Color.FromArgb(40, 40, 40) : Color.White;
            groupBox.ForeColor = _isNightMode ? Color.White : Color.Black;
        }

        private void ApplyTextBoxNightMode(TextBox? textBox)
        {
            if (textBox == null) return;
            textBox.BackColor = _isNightMode ? Color.FromArgb(30, 30, 30) : Color.White;
            textBox.ForeColor = _isNightMode ? Color.White : Color.Black;
        }

        private void ApplyLabelForeColor(Label? label)
        {
            if (label == null) return;
            label.ForeColor = _isNightMode ? Color.White : Color.Black;
        }

        private void ApplyListBoxNightMode(ListBox? listBox)
        {
            if (listBox == null) return;
            listBox.BackColor = _isNightMode ? Color.FromArgb(30, 30, 30) : Color.White;
            listBox.ForeColor = _isNightMode ? Color.White : Color.Black;
        }

        private void ApplyButtonNightMode(Button? button)
        {
            if (button == null) return;
            if (_isNightMode)
            {
                button.BackColor = Color.FromArgb(45, 45, 45);
                button.ForeColor = Color.White;
            }
            else
            {
                button.BackColor = SystemColors.Control;
                button.ForeColor = SystemColors.ControlText;
            }
        }

        private void ApplyNightModeToStatusBar()
        {
            if (_form.StatusBar == null) return;

            if (_isNightMode)
            {
                _form.StatusBar.BackColor = Color.FromArgb(35, 35, 35);
            }
            else
            {
                _form.StatusBar.BackColor = Color.White;
            }

            ApplyStatusLabelNightMode(_form.StatusLabelLeft);
            ApplyStatusLabelNightMode(_form.StatusLabelRight);
        }

        private void ApplyStatusLabelNightMode(Label? label)
        {
            if (label == null) return;
            label.ForeColor = _isNightMode ? Color.FromArgb(180, 180, 180) : Color.FromArgb(153, 153, 153);
        }

        public void UpdateThumbnailsBackground()
        {
            if (_form.FlowLayoutPanelThumbnails == null) return;

            foreach (Control control in _form.FlowLayoutPanelThumbnails.Controls)
            {
                if (control is Panel panel)
                {
                    panel.BackColor = _isNightMode ? Color.FromArgb(45, 45, 45) : Color.White;
                    foreach (Control child in panel.Controls)
                    {
                        if (child is Label label)
                        {
                            label.ForeColor = _isNightMode ? Color.White : Color.Black;
                        }
                    }
                }
                // 图片模式下的目录分组标题（Tag 标记为 "DirectoryHeader"）
                else if (control is Label headerLabel && headerLabel.Tag is string tag && tag == "DirectoryHeader")
                {
                    headerLabel.BackColor = _isNightMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(230, 230, 230);
                    headerLabel.ForeColor = _isNightMode ? Color.White : Color.FromArgb(64, 64, 64);
                }
            }
        }

        public Image InvertImage(Image image)
        {
            using Bitmap bitmap = new Bitmap(image);
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
            try
            {
                int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
                IntPtr ptr = data.Scan0;
                int bytes = Math.Abs(data.Stride) * bitmap.Height;
                byte[] rgbValues = new byte[bytes];
                System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

                for (int i = 0; i < rgbValues.Length; i += bytesPerPixel)
                {
                    if (bytesPerPixel >= 3)
                    {
                        rgbValues[i] = (byte)(255 - rgbValues[i]);
                        rgbValues[i + 1] = (byte)(255 - rgbValues[i + 1]);
                        rgbValues[i + 2] = (byte)(255 - rgbValues[i + 2]);
                    }
                }

                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
            return new Bitmap(bitmap);
        }

        public void UpdateThumbnailPanelColor(Panel panel)
        {
            panel.BackColor = _isNightMode ? Color.FromArgb(45, 45, 45) : Color.White;
        }

        public void UpdateThumbnailLabelColor(Label label)
        {
            label.ForeColor = _isNightMode ? Color.White : Color.Black;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}
