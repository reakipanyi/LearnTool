namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// PDF AI服务接口 - 提供基于上下文的AI问答功能
    /// </summary>
    public interface IPdfAiService
    {
        /// <summary>
        /// 基于PDF内容提问并获取AI回答
        /// </summary>
        /// <param name="question">用户问题</param>
        /// <param name="context">上下文内容（通常为PDF页面文本）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>AI生成的回答</returns>
        Task<string> GetAnswerAsync(string question, string context = "", CancellationToken cancellationToken = default);
    }
}
