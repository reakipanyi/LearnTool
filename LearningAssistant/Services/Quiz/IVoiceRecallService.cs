using LearningAssistant.Services.Learning;
using LearningAssistant.Services.AI;

namespace LearningAssistant.Services.Quiz
{
    /// <summary>
    /// 语音回忆服务接口
    /// 结合语音识别和AI评估，实现语音回忆学习
    /// </summary>
    public interface IVoiceRecallService
    {
        /// <summary>
        /// 开始语音回忆
        /// </summary>
        Task<VoiceRecallResult> StartRecallAsync(ReviewItem item, CancellationToken ct = default);

        /// <summary>
        /// 评估用户回答
        /// </summary>
        Task<VoiceRecallResult> EvaluateAnswerAsync(ReviewItem item, string spokenAnswer, CancellationToken ct = default);

        /// <summary>
        /// 获取下一步提示
        /// </summary>
        Task<string> GetHintAsync(ReviewItem item, int hintLevel, CancellationToken ct = default);

        /// <summary>
        /// 是否支持语音
        /// </summary>
        bool IsVoiceSupported { get; }
    }

    /// <summary>
    /// 语音回忆结果
    /// </summary>
    public class VoiceRecallResult
    {
        /// <summary>
        /// 是否正确
        /// </summary>
        public bool IsCorrect { get; set; }

        /// <summary>
        /// 评分 (0-100)
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// 用户回答
        /// </summary>
        public string UserAnswer { get; set; } = string.Empty;

        /// <summary>
        /// 正确答案
        /// </summary>
        public string CorrectAnswer { get; set; } = string.Empty;

        /// <summary>
        /// AI评估反馈
        /// </summary>
        public string Feedback { get; set; } = string.Empty;

        /// <summary>
        /// 发音评分（如果是语言学习）
        /// </summary>
        public double PronunciationScore { get; set; }

        /// <summary>
        /// 建议的SM-2评分 (1-5)
        /// </summary>
        public int SuggestedRating => Score switch
        {
            >= 90 => 5,
            >= 75 => 4,
            >= 60 => 3,
            >= 40 => 2,
            _ => 1
        };
    }
}
