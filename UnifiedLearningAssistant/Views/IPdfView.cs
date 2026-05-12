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
        // 新增功能：中等级 - UI响应性改进，加载状态管理
        void SetLoadingState(bool isLoading);
        void ShowMessage(string message);
        // 新增功能：中等级 - PDF页面缩略图
        void ClearThumbnails();
        void AddThumbnail(int pageIndex, Image thumbnail);
        void HighlightThumbnail(int pageIndex);
        // 新增功能：低优先级 - PDF搜索和高亮
        void UpdateSearchResultCount(int count);
        void SetSearchPanelVisible(bool visible);
        // 新增功能：低优先级 - 夜间模式
        void NightMode();

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
        event EventHandler<string>? SearchTextChanged;
        event EventHandler? SearchNext;
        event EventHandler? SearchPrevious;
        event EventHandler? ToggleSearchPanel;
        event EventHandler? ToggleNightMode;
    }
}