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
        //void ShowTranslationDialog(string original, string translation, string grammar);
        void UpdateAiAnswer(string answer);
        void SetQuestionInput(string text);
        // 新增功能：中等级 - UI响应性改进，加载状态管理
        void SetLoadingState(bool isLoading);
        void ShowMessage(string message);
        // 新增功能：中等级 - PDF页面缩略图
        void ClearThumbnails();
        void AddThumbnail(int pageIndex, Image thumbnail);
        void HighlightThumbnail(int pageIndex);

        // 新增功能：低优先级 - 夜间模式
        void NightMode();
        // 新增功能：OCR语言切换
        void SetCurrentLanguage(string language);
        void UpdateLanguageButtonText(string text);
        string GetCurrentLanguage();

        string GetSelectedFile();
        string GetPageText();
        string GetQuestionText();
        string GetTranslationText();
        string GetOriginalText();
        void SetTranslationText(string text);
        void SetOriginalText(string text);
        void SetOcrResultText(string text);
        string GetAiAnswerText();
        Image? GetCurrentImage();
        Rectangle? GetSelectionRect();
        Rectangle GetDisplayRect();
        Rectangle GetImageDisplayRect();
        void ShowOcrOverlay(Bitmap? image);
        void HideOcrOverlay();

        event EventHandler? FileSelected;
        event EventHandler? PageChanged;
        event EventHandler? OcrSelectionComplete;
        event EventHandler? AiQuestionAsked;
        event EventHandler? AddToLearningList;
        event EventHandler<AddToEditorEventArgs>? AddToEditor;
        event EventHandler? SpeakOriginal;
        event EventHandler? SpeakTranslation;
        event EventHandler<string>? SpeakText;
        event EventHandler<string>? AskAiWithText;
        event EventHandler? SelectOcrClicked;
        event EventHandler? TranslateClicked;
        event EventHandler? ToggleNightMode;
        event EventHandler? LanguageChanged;
        event EventHandler? SpeakAnswer;
    }

    public class AddToEditorEventArgs : EventArgs
    {
        public string Text { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
    }
}
