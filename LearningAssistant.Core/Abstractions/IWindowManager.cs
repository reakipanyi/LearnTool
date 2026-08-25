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
    }
}
