namespace LearningAssistant.Views
{
    public interface IMainView
    {
        string SelectedUser { get; set; }
        string ProgressSummary { get; set; }
        string StatusText { get; set; }

        event EventHandler? UserChanged;
        event EventHandler? OpenLearningWindowClicked;
        event EventHandler? OpenSettingsClicked;
        event EventHandler? OpenEditorClicked;
        event EventHandler? TabChanged;
        event EventHandler? NewUserClicked;

        void ShowMessage(string msg);
        void RefreshUserList(IEnumerable<string> users);
        void SetTabPage(string tabName);
        void UpdateStatus(string status);
        void UpdateStreakInfo(int consecutiveDays, string studyTimeSummary);
    }
}