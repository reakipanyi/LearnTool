namespace LearningAssistant.Services.AI
{
    /// <summary>
    /// AI服务接口 - 提供AI相关的学习和问答功能
    /// </summary>
    public interface IAIService
    {
        /// <summary>
        /// 获取文本的解释
        /// </summary>
        /// <param name="text">需要解释的文本内容</param>
        /// <param name="language">目标语言（如 zh-CN, en-US）</param>
        /// <param name="subType">学习子类型（如 word, sentence, grammar）</param>
        /// <returns>文本的AI解释结果</returns>
        Task<string> GetExplanationAsync(string text, string language, string subType);

        /// <summary>
        /// 提问功能 - 基于上下文回答用户问题
        /// </summary>
        /// <param name="question">用户的问题</param>
        /// <param name="context">可选的上下文信息，用于提供背景</param>
        /// <returns>AI生成的回答</returns>
        Task<string> AskQuestionAsync(string question, string context = "");

        /// <summary>
        /// 当前使用的AI模型名称
        /// </summary>
        string ModelName { get; }
    }

    /// <summary>
    /// AI响应数据结构 - 包含文本解析后的多维度信息
    /// </summary>
    public class AIResponse
    {
        /// <summary>
        /// 原始文本内容
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 音标/发音信息
        /// </summary>
        public string Phonetic { get; set; } = string.Empty;

        /// <summary>
        /// 词义/含义
        /// </summary>
        public string Meaning { get; set; } = string.Empty;

        /// <summary>
        /// 用法示例
        /// </summary>
        public string Example { get; set; } = string.Empty;

        /// <summary>
        /// 语法说明
        /// </summary>
        public string Grammar { get; set; } = string.Empty;
    }
}
