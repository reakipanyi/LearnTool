namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// PDF学习集成接口 - 连接PDF阅读和学习模块
    /// 支持从PDF中添加生词到学习列表
    /// </summary>
    public interface IPdfStudyIntegration
    {
        /// <summary>
        /// 设置当前用户和学习配置
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="language">学习语言</param>
        /// <param name="subCategory">学习子类别</param>
        void SetCurrentUserAndConfig(string userId, string language, string subCategory);

        /// <summary>
        /// 添加单词到学习列表
        /// 当用户在PDF中选中生词时调用
        /// </summary>
        /// <param name="word">要学习的单词</param>
        /// <returns>添加成功返回true</returns>
        bool AddWordToLearningList(string word);

        /// <summary>
        /// 生词添加事件 - 当从PDF添加单词到学习列表时触发
        /// </summary>
        event EventHandler<WordAddedEventArgs>? WordAdded;
    }

    /// <summary>
    /// 生词添加事件参数
    /// </summary>
    public class WordAddedEventArgs : EventArgs
    {
        /// <summary>
        /// 添加的单词
        /// </summary>
        public string Word { get; set; } = string.Empty;

        /// <summary>
        /// 学习语言
        /// </summary>
        public string Language { get; set; } = string.Empty;
    }
}
