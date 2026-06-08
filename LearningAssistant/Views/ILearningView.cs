using LearningAssistant.Models.Learning;

namespace LearningAssistant.Views
{
    public enum PronunciationScope
    {
        Original,
        Explanation,
        Both
    }

    public interface ILearningView
    {
        string CurrentContent { set; }
        string CurrentDisplayText { set; }
        string AIExplanation { set; }
        string Statistics { set; }
        int ProgressValue { set; }
        int ProgressMax { set; }
        bool IsVoiceEnabled { get; set; }
        bool IsAIExplanationEnabled { get; set; }
        PronunciationScope PronunciationScope { get; set; }
        string CurrentMode { get; }
        string LearningMode { get; }
        string SortOrder { get; }
        string Language { get; }
        string SubCategory { get; set; }

        event EventHandler? MarkAsKnownClicked;
        event EventHandler? MarkAsUnknownClicked;
        event EventHandler? PronounceClicked;
        event EventHandler? NextClicked;
        event EventHandler? ExitClicked;
        event EventHandler? AddToPdfQuestionClicked;
        event EventHandler? SettingsChanged;
        event EventHandler? OpenStatisticsClicked;
        event EventHandler? ExportErrorBookClicked;

        void ShowMessage(string msg);
        void EnableButtons(bool enabled);
        void PlayPronunciation(string text, string language);
        void SetCurrentItem(LearningItem item);
        void UpdateLearningList(List<string> items, int currentIndex);
        void UpdateLearningListSelection(int currentIndex);
        void RefreshSubCategories(List<string> subCategories);
        void SetLoadingState(bool isLoading, string message = "加载中...");
    }
}