namespace LearningAssistant.Abstractions
{
    public interface IWindowManager
    {
        event EventHandler? SettingUsersChanged;

        Task OpenLearningWindowAsync(string userId, string language, string subCategory, string wordBankFile, bool continueMode);
        void OpenSettingsWindow();
        void OpenEditorWindow();
        void OpenEditorWindowWithContext(string? text, string? language, string? subCategory);
        void OpenStatisticsWindow();
        void OpenLearningManagementWindow();
        /// <summary>
        /// 打开 PDF 阅读器。可选直接定位到指定文件的指定页（学习项回跳原文用）。
        /// </summary>
        void OpenPdfReaderWindow(string? pdfPath = null, int pageIndex = -1);
        void OpenNotesWindow();
        void OpenAIWebViewWindow(string? initialPrompt = null);
        void OpenWordMatchGameWindow();
        void OpenMemoryMatchGameWindow();
        void OpenLinkMatchGameWindow();
        void OpenSpellingGameWindow();
        void OpenWhackAMoleGameWindow();
        void OpenSchulteGameWindow();
        void OpenSudokuGameWindow();
        void OpenStroopGameWindow();
        void OpenInhibitionGameWindow();
        void OpenMemoryGameWindow();
        void OpenSearchGameWindow();
        void OpenTraceGameWindow();
        void OpenFlexGameWindow();
        void OpenAttentionGameWindow();
        void OpenPlanGameWindow();
        void OpenWrongAnswerWindow();
    }
}
