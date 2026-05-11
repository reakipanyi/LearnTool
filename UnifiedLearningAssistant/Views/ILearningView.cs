using UnifiedLearningAssistant.Models.Learning;

namespace UnifiedLearningAssistant.Views
{
    public interface ILearningView
    {
        string CurrentContent { set; }
        string CurrentDisplayText { set; }
        string AIExplanation { set; }
        string Statistics { set; }
        int ProgressValue { set; }
        int ProgressMax { set; }
        bool IsVoiceEnabled { get; set; }
        string CurrentMode { get; }

        event EventHandler? MarkAsKnownClicked;
        event EventHandler? MarkAsUnknownClicked;
        event EventHandler? PronounceClicked;
        event EventHandler? NextClicked;
        event EventHandler? ExitClicked;
        event EventHandler? AddToPdfQuestionClicked;

        void ShowMessage(string msg);
        void EnableButtons(bool enabled);
        void PlayPronunciation(string text, string language);
        void SetCurrentItem(LearningItem item);
    }
}