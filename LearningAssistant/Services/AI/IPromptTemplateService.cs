using LearningAssistant.Models.AI;

namespace LearningAssistant.Services.AI
{
    /// <summary>
    /// 提示词模板管理服务接口
    /// 提供提示词模板的增删改查、变量替换、分类管理等功能
    /// </summary>
    public interface IPromptTemplateService
    {
        /// <summary>
        /// 获取所有模板分类
        /// </summary>
        List<PromptTemplateCategory> GetAllCategories();

        /// <summary>
        /// 获取所有模板
        /// </summary>
        List<PromptTemplate> GetAllTemplates();

        /// <summary>
        /// 根据分类获取模板
        /// </summary>
        List<PromptTemplate> GetTemplatesByCategory(string category);

        /// <summary>
        /// 获取指定模板
        /// </summary>
        PromptTemplate? GetTemplate(string templateId);

        /// <summary>
        /// 搜索模板
        /// </summary>
        List<PromptTemplate> SearchTemplates(string keyword);

        /// <summary>
        /// 添加模板
        /// </summary>
        void AddTemplate(PromptTemplate template);

        /// <summary>
        /// 更新模板
        /// </summary>
        void UpdateTemplate(PromptTemplate template);

        /// <summary>
        /// 删除模板
        /// </summary>
        void DeleteTemplate(string templateId);

        /// <summary>
        /// 收藏/取消收藏模板
        /// </summary>
        void SetFavorite(string templateId, bool isFavorite);

        /// <summary>
        /// 获取收藏的模板
        /// </summary>
        List<PromptTemplate> GetFavoriteTemplates();

        /// <summary>
        /// 渲染模板（替换变量）
        /// </summary>
        string RenderTemplate(string templateId, Dictionary<string, string> variables);

        /// <summary>
        /// 记录使用次数
        /// </summary>
        void RecordUsage(string templateId);

        /// <summary>
        /// 获取常用模板
        /// </summary>
        List<PromptTemplate> GetFrequentlyUsed(int count = 10);

        /// <summary>
        /// 导出模板
        /// </summary>
        void ExportTemplates(string filePath, List<string>? templateIds = null);

        /// <summary>
        /// 导入模板
        /// </summary>
        int ImportTemplates(string filePath, bool overwrite = false);

        /// <summary>
        /// 添加分类
        /// </summary>
        void AddCategory(string categoryName, string icon = "📁");

        /// <summary>
        /// 删除分类
        /// </summary>
        void RemoveCategory(string categoryName);

        /// <summary>
        /// 保存到文件
        /// </summary>
        void SaveToFile();

        /// <summary>
        /// 从文件加载
        /// </summary>
        void LoadFromFile();

        /// <summary>
        /// 重置为默认模板
        /// </summary>
        void ResetToDefault();
    }
}
