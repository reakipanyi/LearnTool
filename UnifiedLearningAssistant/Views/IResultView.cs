namespace UnifiedLearningAssistant.Views
{
    public interface IResultView
    {
        string AccuracyRate { set; }
        string KnownItems { set; }
        string UnknownItems { set; }
        string Statistics { set; }

        event EventHandler? ReviewUnknownClicked;
        event EventHandler? CloseClicked;

        void ShowMessage(string msg);
        void CloseView();
    }
}