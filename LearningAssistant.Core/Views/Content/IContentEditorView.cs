using LearningAssistant.Common;
using System.Data;

namespace LearningAssistant.Views
{
    public interface IContentEditorView
    {
        SubjectType SelectedSubject { get; }
        SubCategoryType SelectedSubCategory { get; }

        /// <summary>
        /// 学习项数据表
        /// </summary>
        DataTable ItemData { set; }

        /// <summary>
        /// 当前编辑项的JSON格式数据
        /// </summary>
        string CurrentEditItemJson { get; set; }


        /// <summary>
        /// 网格数据源
        /// </summary>
        object? GridDataSource { get; set; }

        /// <summary>
        /// 选中的行索引列表
        /// </summary>
        List<int> SelectedRowIndices { get; }

        event EventHandler? SubjectChanged;
        event EventHandler? SubCategoryChanged;

        /// <summary>
        /// 模板添加点击事件
        /// </summary>
        event EventHandler? TemplateAddClicked;

        /// <summary>
        /// 模板保存点击事件
        /// </summary>
        event EventHandler? TemplateSaveClicked;

        /// <summary>
        /// JSON编辑框内容变更事件
        /// </summary>
        event EventHandler? JsonTextChanged;

        /// <summary>
        /// 模板删除点击事件
        /// </summary>
        event EventHandler? TemplateDeleteClicked;

        /// <summary>
        /// 导入点击事件
        /// </summary>
        event EventHandler? ImportClicked;

        /// <summary>
        /// 导出点击事件
        /// </summary>
        event EventHandler? ExportClicked;


        /// <summary>
        /// 网格单元格编辑完成事件
        /// </summary>
        event EventHandler? GridCellEndEdit;

        /// <summary>
        /// 学习项选中事件
        /// </summary>
        event EventHandler? ItemSelected;

        /// <summary>
        /// 网格行添加事件
        /// </summary>
        event EventHandler? GridRowsAdded;

        /// <summary>
        /// 显示消息
        /// </summary>
        /// <param name="msg">消息内容</param>
        void ShowMessage(string msg);

        /// <summary>
        /// 显示三选确认对话框（是/否/取消），用于未保存更改时的询问。
        /// </summary>
        /// <param name="message">提示消息</param>
        /// <param name="title">对话框标题</param>
        /// <returns>true=Yes(保存), false=No(放弃), null=Cancel(取消操作)</returns>
        bool? ShowConfirm(string message, string title);

        /// <summary>
        /// 显示打开文件对话框
        /// </summary>
        /// <param name="filter">文件过滤器</param>
        /// <param name="title">对话框标题</param>
        /// <param name="defaultFileName">默认文件名</param>
        /// <returns>选择的文件路径，取消返回null</returns>
        string? ShowOpenFileDialog(string filter, string title, string defaultFileName);

        /// <summary>
        /// 显示保存文件对话框
        /// </summary>
        /// <param name="filter">文件过滤器</param>
        /// <param name="title">对话框标题</param>
        /// <param name="defaultFileName">默认文件名</param>
        /// <returns>选择的文件路径，取消返回null</returns>
        string? ShowSaveFileDialog(string filter, string title, string defaultFileName);

        void RefreshSubCategories(List<SubCategoryType> subCategories);
        void RefreshTreeView(object? nodes);
        void LoadItemForEdit(dynamic item);
        void ClearEditForm();
        void UpdateItemList();
        void AppendJson(string json);
        void UpdateGridFromJson();
        void SetInitialSubject(SubjectType subject);
        void SetInitialSubCategory(SubCategoryType subCategory);
        void UpdateDirtyStatus(bool isDirty);
    }
}
