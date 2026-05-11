namespace UnifiedLearningAssistant.Views
{
    public interface IContentEditorView
    {
        string SelectedCategory { get; }
        string SelectedSubCategory { get; }
        string CurrentEditItemJson { get; set; }
        string GenerateCount { get; set; }
        string GenerateRange { get; set; }

        event EventHandler? CategoryChanged;
        event EventHandler? TemplateAddClicked;
        event EventHandler? TemplateSaveClicked;
        event EventHandler? TemplateDeleteClicked;
        event EventHandler? ImportClicked;
        event EventHandler? ExportClicked;
        event EventHandler? GenerateWithAIClicked;
        event EventHandler? InsertTemplateClicked;

        void ShowMessage(string msg);
        void ClearEditForm();
        void AppendJson(string json);
    }
}