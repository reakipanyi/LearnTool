using LearningAssistant.Models.Learning;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 内容加载服务接口 - 负责学习内容的加载、保存和分类管理
    /// </summary>
    public interface IContentLoaderService
    {
        /// <summary>
        /// 加载指定子类别的学习项列表
        /// </summary>
        /// <param name="subCategory">子类别（如CET4, CET6, GRE）</param>
        /// <param name="wordBankFile">词库文件路径，为空则使用默认</param>
        /// <returns>学习项对象列表</returns>
        List<object> LoadItems(string subCategory, string wordBankFile = "");

        /// <summary>
        /// 保存学习项列表到文件
        /// </summary>
        /// <param name="subCategory">子类别</param>
        /// <param name="items">要保存的学习项列表</param>
        /// <param name="wordBankFile">词库文件路径，为空则使用默认</param>
        void SaveItems(string subCategory, List<object> items, string wordBankFile = "");

        /// <summary>
        /// 获取指定语言的所有子类别列表
        /// </summary>
        /// <param name="language">学习语言</param>
        /// <returns>子类别名称列表</returns>
        List<string> GetSubCategories(string language);

        /// <summary>
        /// 获取指定子类别关联的所有词库文件
        /// </summary>
        /// <param name="subCategory">子类别</param>
        /// <returns>词库文件路径列表</returns>
        List<string> GetWordBankFiles(string subCategory);

        /// <summary>
        /// 获取指定子类别对应的默认词库文件
        /// </summary>
        /// <param name="subCategory">子类别</param>
        /// <returns>默认词库文件路径</returns>
        string GetDefaultWordBankFile(string subCategory);

        /// <summary>
        /// 获取指定子类别对应的学习项类型
        /// </summary>
        /// <param name="subCategory">子类别</param>
        /// <returns>LearningItem的具体类型</returns>
        Type GetItemType(string subCategory);

        /// <summary>
        /// 保存用户自定义内容（如笔记、标注等）
        /// </summary>
        /// <param name="content">用户内容对象</param>
        void SaveUserContent(UserContent content);
    }
}
