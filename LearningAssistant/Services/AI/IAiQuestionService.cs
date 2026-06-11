namespace LearningAssistant.Services.AI
{
    /// <summary>
    /// AI问题服务接口 - 提供基于AI的学习问答和内容生成功能
    /// </summary>
    public interface IAiQuestionService
    {
        /// <summary>
        /// 异步提问
        /// </summary>
        /// <param name="text">问题文本</param>
        /// <param name="context">上下文信息（可选）</param>
        /// <returns>AI回答</returns>
        Task<string> AskAsync(string text, string context = "");

        /// <summary>
        /// 异步生成练习题
        /// </summary>
        /// <param name="text">学习内容文本</param>
        /// <param name="language">学习语言</param>
        /// <returns>生成的练习题</returns>
        Task<string> GenerateExerciseAsync(string text, string language);

        /// <summary>
        /// 异步摘要文本
        /// </summary>
        /// <param name="text">要摘要的文本</param>
        /// <returns>生成的摘要</returns>
        Task<string> SummarizeTextAsync(string text);
    }
}
