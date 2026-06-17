using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Microsoft.Extensions.Logging;
using System.Windows.Forms;

namespace LearningAssistant.Forms
{
    public class PdfReaderBookmarkManager : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IBookmarkService _bookmarkService;
        private readonly IPdfReaderFormAccess _form;
        private bool _disposed = false;

        public PdfReaderBookmarkManager(ILogger logger, IPdfReaderFormAccess form, IBookmarkService bookmarkService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _bookmarkService = bookmarkService ?? throw new ArgumentNullException(nameof(bookmarkService));
        }

        public void AddBookmark()
        {
            if (string.IsNullOrEmpty(_form.CurrentPdfPath)) return;

            var title = _form.TextBoxBookmarkTitle?.Text ?? $"第 {_form.CurrentPageIndex + 1} 页";
            _bookmarkService.AddBookmark(_form.CurrentPdfPath, _form.CurrentPageIndex, title);
            RefreshBookmarkList();
            _form.TextBoxBookmarkTitle!.Text = string.Empty;
        }

        public void RemoveBookmark()
        {
            if (_form.ListBoxBookmarks?.SelectedItem is PdfBookmark bookmark)
            {
                _bookmarkService.RemoveBookmark(_form.CurrentPdfPath, bookmark.PageIndex, bookmark.Title);
                RefreshBookmarkList();
            }
        }

        public void NavigateToBookmark(PdfBookmark bookmark)
        {
            _form.Presenter?.RenderPage(bookmark.PageIndex);
        }

        public void RefreshBookmarkList()
        {
            if (_form.ListBoxBookmarks == null || string.IsNullOrEmpty(_form.CurrentPdfPath)) return;

            _form.ListBoxBookmarks.Items.Clear();
            var bookmarks = _bookmarkService.GetBookmarks(_form.CurrentPdfPath);
            foreach (var bookmark in bookmarks)
            {
                _form.ListBoxBookmarks.Items.Add(bookmark);
            }
        }

        public void ClearCache()
        {
            _bookmarkService.ClearCache();
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
