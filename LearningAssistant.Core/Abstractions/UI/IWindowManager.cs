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
        void OpenPdfReaderWindow();
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
    }
}
