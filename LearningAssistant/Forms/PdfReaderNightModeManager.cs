using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Forms
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
            
            if (_form.buttonNightMode != null)
            {
                _form.buttonNightMode.Text = _isNightMode ? "☀️" : "🌙";
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
                _form.panelPdf.BackColor = Color.FromArgb(20, 20, 20);
                _form.panelNavigation.BackColor = Color.FromArgb(45, 45, 45);
                _form.treeViewFiles.BackColor = Color.FromArgb(40, 40, 40);
                _form.treeViewFiles.ForeColor = Color.White;
                _form.tabControlLeft.BackColor = Color.FromArgb(40, 40, 40);
                _form.panelThumbnails.BackColor = Color.FromArgb(40, 40, 40);
                _form.flowLayoutPanelThumbnails.BackColor = Color.FromArgb(40, 40, 40);

                UpdateButtonNightModeColor(_form.buttonNightMode, true);
                UpdateButtonLanguageColor(_form.buttonLanguage, true);
                UpdateButtonAiColor(_form.buttonAskAi, true);
            }
            else
            {
                _form.BackColor = Color.FromArgb(240, 240, 240);
                _form.panelPdf.BackColor = Color.White;
                _form.panelNavigation.BackColor = Color.FromArgb(240, 240, 240);
                _form.treeViewFiles.BackColor = Color.White;
                _form.treeViewFiles.ForeColor = Color.Black;
                _form.tabControlLeft.BackColor = Color.White;
                _form.panelThumbnails.BackColor = Color.FromArgb(240, 240, 240);
                _form.flowLayoutPanelThumbnails.BackColor = Color.FromArgb(240, 240, 240);

                UpdateButtonNightModeColor(_form.buttonNightMode, false);
                UpdateButtonLanguageColor(_form.buttonLanguage, false);
                UpdateButtonAiColor(_form.buttonAskAi, false);
            }
        }

        private void UpdateButtonNightModeColor(Button? button, bool isNightMode)
        {
            if (button == null) return;
            button.BackColor = isNightMode ? Color.FromArgb(45, 45, 45) : Color.White;
        }

        private void UpdateButtonLanguageColor(Button? button, bool isNightMode)
        {
            if (button == null) return;
            button.BackColor = isNightMode ? Color.FromArgb(45, 45, 45) : Color.White;
            button.ForeColor = isNightMode ? Color.White : Color.Black;
        }

        private void UpdateButtonAiColor(Button? button, bool isNightMode)
        {
            if (button == null) return;
            button.BackColor = Color.FromArgb(0, 120, 215);
            button.ForeColor = Color.White;
        }

        private void ApplyNightModeToTabPageTranslate()
        {
            ApplyControlBackColor(_form.tabPageTranslate, _isNightMode);
            ApplyGroupBoxNightMode(_form.groupBoxProgress);
            ApplyTextBoxNightMode(_form.textBoxOriginal);
            ApplyTextBoxNightMode(_form.textBoxTranslation);
            ApplyLabelForeColor(_form.labelOriginal);
            ApplyLabelForeColor(_form.labelTranslation);

            ApplyButtonNightMode(_form.buttonTranslate);
            ApplyButtonNightMode(_form.buttonSpeakOriginal);
            ApplyButtonNightMode(_form.buttonSpeakTranslation);
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
            ApplyButtonNightMode(_form.ButtonUndoHighlight);
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

        public void UpdateThumbnailsBackground()
        {
            if (_form.flowLayoutPanelThumbnails == null) return;

            foreach (Control control in _form.flowLayoutPanelThumbnails.Controls)
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
            }
        }

        public Image InvertImage(Image image)
        {
            Bitmap bitmap = new Bitmap(image);
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
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
            bitmap.UnlockBits(data);
            return bitmap;
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
