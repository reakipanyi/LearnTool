namespace LearningAssistant.Services.AI
{
    /// <summary>
    /// AI服务接口 - 提供AI解释和问答功能
    /// 统一的AI服务入口，支持解释、问答、练习生成和摘要功能
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
        /// 生成练习题
        /// </summary>
        /// <param name="text">学习内容文本</param>
        /// <param name="language">学习语言</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>生成的练习题</returns>
        Task<string> GenerateExerciseAsync(string text, string language, CancellationToken cancellationToken = default);

        /// <summary>
        /// 摘要文本
        /// </summary>
        /// <param name="text">要摘要的文本</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>生成的摘要</returns>
        Task<string> SummarizeAsync(string text, CancellationToken cancellationToken = default);

        /// <summary>
        /// 模型名称
        /// </summary>
        string ModelName { get; }

        /// <summary>
        /// 提供商名称
        /// </summary>
        string ProviderName { get; }
    }
}
