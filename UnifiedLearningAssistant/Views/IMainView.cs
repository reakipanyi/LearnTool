namespace UnifiedLearningAssistant.Views
{
    public interface IMainView
    {
        string SelectedUser { get; set; }
        string SelectedLanguage { get; set; }
        string SelectedSubCategory { get; set; }
        string SelectedMode { get; set; }
        string SelectedWordBankFile { get; set; }
        string ProgressSummary { get; set; }
        string SelectedSortOrder { get; set; }
        string StatusText { get; set; }

        event EventHandler? UserChanged;
        event EventHandler? LanguageChanged;
        event EventHandler? SubCategoryChanged;
        event EventHandler? ModeChanged;
        event EventHandler? WordBankChanged;
        event EventHandler? StartLearningClicked;
        event EventHandler? ContinueLearningClicked;
        event EventHandler? OpenSettingsClicked;
        event EventHandler? OpenEditorClicked;
        event EventHandler? OpenStatisticsClicked;
        // 新增功能：错题本导出 - 添加导出事件
        event EventHandler? ExportErrorBookClicked;
        event EventHandler? SortOrderChanged;
        event EventHandler? TabChanged;

        void ShowMessage(string msg);
        void RefreshUserList(IEnumerable<string> users);
        void RefreshSubCategories(IEnumerable<string> subCats);
        void RefreshWordBankFiles(IEnumerable<string> files);
        void SetTabPage(string tabName);
        void UpdateStatus(string status);
    }
}