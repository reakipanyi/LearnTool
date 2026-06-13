namespace LearningAssistant.Services.AI
{
    /// <summary>
    /// AI服务接口 - 提供AI解释和问答功能
    /// </summary>
    public interface IAIService
    {
        /// <summary>
        /// 获取文本解释
        /// </summary>
        /// <param name="text">要解释的文本</param>
        /// <param name="language">语言</param>
        /// <param name="subType">子类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>解释文本</returns>
        Task<string> GetExplanationAsync(string text, string language, string subType, CancellationToken cancellationToken = default);

        /// <summary>
        /// 回答问题
        /// </summary>
        /// <param name="question">问题</param>
        /// <param name="context">上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>回答文本</returns>
        Task<string> AskQuestionAsync(string question, string context = "", CancellationToken cancellationToken = default);

        /// <summary>
        /// 模型名称
        /// </summary>
        string ModelName { get; }
    }
}
