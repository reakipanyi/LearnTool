using LearningAssistant.Presenters;

namespace LearningAssistant.Managers
{
    public interface IPdfReaderFormAccess
    {
        string CurrentPdfPath { get; set; }
        int CurrentPageIndex { get; set; }
        Bitmap? CurrentPageImage { get; set; }
        Bitmap? SecondPageImage { get; set; }
        bool IsDualPage { get; }
        bool IsTranslationEnabled { get; set; }
        bool IsNightMode { get; }

        PictureBox PictureBoxPdf { get; }
        PdfPresenter? Presenter { get; set; }
        TextBox TextBoxOriginal { get; }
        TextBox TextBoxPage { get; }
        Label LabelZoom { get; }
        TrackBar TrackBarZoom { get; }

        Button? ButtonNightMode { get; }
        Button? ButtonLanguage { get; }
        Button? ButtonAskAi { get; }

        TabPage? TabPageTranslate { get; }
        GroupBox? GroupBoxProgress { get; }
        TextBox? TextBoxTranslation { get; }
        Label? LabelOriginal { get; }
        Label? LabelTranslation { get; }
        Button? ButtonTranslate { get; }
        Button? ButtonSpeakOriginal { get; }
        Button? ButtonSpeakTranslation { get; }

        TabPage? TabPageBookmarksAndHighlights { get; }
        GroupBox? GroupBoxBookmarks { get; }
        ListBox? ListBoxBookmarks { get; }
        TextBox? TextBoxBookmarkTitle { get; }
        Button? ButtonAddBookmark { get; }
        Button? ButtonRemoveBookmark { get; }

        GroupBox? GroupBoxHighlights { get; }
        ListBox? ListBoxHighlights { get; }
        GroupBox? GroupBoxHighlightColor { get; }
        Button? ButtonRemoveHighlight { get; }
        Button? ButtonBatchRemoveHighlight { get; }
        Button? ButtonExportHighlights { get; }
        Button? ButtonUndoHighlight { get; }

        Panel? PanelPdf { get; }
        Panel? PanelNavigation { get; }
        Panel? PanelLeftContainer { get; }
        TreeView? TreeViewFiles { get; }
        TabControl? TabControlLeft { get; }
        Panel? PanelThumbnails { get; }
        FlowLayoutPanel? FlowLayoutPanelThumbnails { get; }

        Panel? PageTransitionOverlay { get; }
        System.Windows.Forms.Timer? PageTransitionTimer { get; }
        Button? ButtonLockView { get; }

        Panel? StatusBar { get; }
        Label? StatusLabelLeft { get; }
        Label? StatusLabelRight { get; }

        Color BackColor { get; set; }

        Rectangle GetImageDisplayRect();
        (int pageIndex, Rectangle pageRect, Bitmap? pageImage) GetPageAtPoint(Point clientPoint);
        void DisplayImage(Bitmap bmp);
        void ShowWarning(string message);
        bool ShowConfirm(string message, string title);
        void ShowMessage(string message, string title);

        event EventHandler? TranslateClicked;
        event EventHandler? SelectOcrClicked;

        void OnTranslateClicked();
        void OnSelectOcrClicked();

        Form Form { get; }
    }
}
