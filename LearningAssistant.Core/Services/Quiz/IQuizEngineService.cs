using LearningAssistant.Models.Quiz;

namespace LearningAssistant.Services.Quiz
{
    /// <summary>
    /// 测验引擎服务接口
    /// </summary>
    public interface IQuizEngineService
    {
        /// <summary>
        /// 创建测验会话
        /// </summary>
        Task<QuizSession> CreateSessionAsync(string userId, string title, int questionCount, QuestionType[] types);

        /// <summary>
        /// 生成练习题（使用AI）
        /// </summary>
        Task<List<QuizQuestion>> GenerateQuestionsAsync(string content, string language, int count, QuestionType type);

        /// <summary>
        /// 提交答案
        /// </summary>
        void SubmitAnswer(QuizSession session, int questionIndex, List<int> selectedIndices, string? textAnswer = null);

        /// <summary>
        /// 获取测验结果
        /// </summary>
        QuizResult GetResult(QuizSession session);

        /// <summary>
        /// AI评估简答题答案
        /// </summary>
        Task<string> EvaluateShortAnswerAsync(string question, string userAnswer, CancellationToken ct = default);
    }
}
