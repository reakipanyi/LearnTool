using System.Data;

namespace LearningAssistant.Views
{
    /// <summary>
    /// 内容编辑器视图接口 - 提供学习内容编辑界面的显示和交互功能
    /// 支持模板管理、导入导出、AI生成等功能
    /// </summary>
    public interface IContentEditorView
    {
        /// <summary>
        /// 当前选中的语言
        /// </summary>
        string SelectedLanguage { get; }

        /// <summary>
        /// 当前选中的子类别
        /// </summary>
        string SelectedSubCategory { get; }

        /// <summary>
        /// 学习项数据表
        /// </summary>
        DataTable ItemData { set; }

        /// <summary>
        /// 当前编辑项的JSON格式数据
        /// </summary>
        string CurrentEditItemJson { get; set; }

        /// <summary>
        /// AI生成项数量
        /// </summary>
        string GenerateCount { get; set; }

        /// <summary>
        /// AI生成范围
        /// </summary>
        string GenerateRange { get; set; }

        /// <summary>
        /// AI生成提示词
        /// </summary>
        string PromptText { get; set; }

        /// <summary>
        /// 网格数据源
        /// </summary>
        object? GridDataSource { get; set; }

        /// <summary>
        /// 选中的行索引列表
        /// </summary>
        List<int> SelectedRowIndices { get; }

        /// <summary>
        /// 语言变更事件
        /// </summary>
        event EventHandler? LanguageChanged;

        /// <summary>
        /// 子类别变更事件
        /// </summary>
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
        /// AI生成点击事件
        /// </summary>
        event EventHandler? GenerateWithAIClicked;

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
        /// 刷新子类别列表
        /// </summary>
        /// <param name="subCategories">子类别列表</param>
        void RefreshSubCategories(IEnumerable<string> subCategories);

        /// <summary>
        /// 刷新树形视图
        /// </summary>
        /// <param name="nodes">树节点集合</param>
        void RefreshTreeView(TreeNodeCollection nodes);

        /// <summary>
        /// 加载学习项进行编辑
        /// </summary>
        /// <param name="item">学习项对象</param>
        void LoadItemForEdit(dynamic item);

        /// <summary>
        /// 清空编辑表单
        /// </summary>
        void ClearEditForm();

        /// <summary>
        /// 更新学习项列表显示
        /// </summary>
        void UpdateItemList();

        /// <summary>
        /// 追加JSON内容
        /// </summary>
        /// <param name="json">JSON字符串</param>
        void AppendJson(string json);

        /// <summary>
        /// 从JSON更新网格数据
        /// </summary>
        void UpdateGridFromJson();

        /// <summary>
        /// 设置初始语言
        /// </summary>
        /// <param name="language">语言代码</param>
        void SetInitialLanguage(string language);

        /// <summary>
        /// 设置初始子类别
        /// </summary>
        /// <param name="subCategory">子类别</param>
        void SetInitialSubCategory(string subCategory);
    }
}
