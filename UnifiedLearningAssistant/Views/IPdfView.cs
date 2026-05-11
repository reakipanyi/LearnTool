namespace UnifiedLearningAssistant.Views
{
    public interface IPdfView
    {
        void SetFileList(IEnumerable<string> files);
        void SetPageCount(int count);
        void SetCurrentPageIndex(int pageIndex);
        void SetPageText(int pageIndex, string text);
        void DisplayImage(Bitmap bmp);
        void ShowWarning(string message);
        void ShowError(string message);
        void ShowTranslationDialog(string original, string translation, string grammar);
        void UpdateAiAnswer(string answer);
        void SetQuestionInput(string text);

        string GetSelectedFile();
        string GetPageText();
        Image? GetCurrentImage();
        Rectangle? GetSelectionRect();
        Rectangle GetDisplayRect();

        event EventHandler? FileSelected;
        event EventHandler? PageChanged;
        event EventHandler? OcrSelectionComplete;
        event EventHandler? AiQuestionAsked;
        event EventHandler? AddWordToLearningList;
        event EventHandler? SpeakTranslation;
        event EventHandler? SelectOcrClicked;
        event EventHandler? TranslateClicked;
    }
}