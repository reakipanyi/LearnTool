using System.Data;

namespace UnifiedLearningAssistant.Views
{
    public interface IContentEditorView
    {
        string SelectedLanguage { get; }
        string SelectedSubCategory { get; }

        DataTable ItemData { set; }
        string CurrentEditItemJson { get; set; }
        string GenerateCount { get; set; }
        string GenerateRange { get; set; }
        string PromptText { get; set; }
        object? GridDataSource { get; set; }
        List&lt;int&gt; SelectedRowIndices { get; }

        event EventHandler? LanguageChanged;
        event EventHandler? SubCategoryChanged;
        event EventHandler? TemplateAddClicked;
        event EventHandler? TemplateSaveClicked;
        event EventHandler? TemplateDeleteClicked;
        event EventHandler? ImportClicked;
        event EventHandler? ExportClicked;
        event EventHandler? GenerateWithAIClicked;
        event EventHandler? GridCellEndEdit;
        event EventHandler ItemSelected;
        event EventHandler? GridRowsAdded;

        void ShowMessage(string msg);

        void RefreshSubCategories(IEnumerable&lt;string&gt; subCategories);
        void RefreshTreeView(TreeNodeCollection nodes);
        void LoadItemForEdit(dynamic item);
        void ClearEditForm();

        void UpdateItemList();
        void AppendJson(string json);
        void UpdateGridFromJson();
        void SetInitialLanguage(string language);
        void SetInitialSubCategory(string subCategory);
    }
}
