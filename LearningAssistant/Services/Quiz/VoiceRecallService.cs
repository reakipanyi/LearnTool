using LearningAssistant.Services.Learning;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Speech;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Quiz
{
    /// <summary>
    /// 语音回忆服务实现
    /// 结合语音识别和AI评估，实现沉浸式语音学习
    /// </summary>
    public class VoiceRecallService : IVoiceRecallService
    {
        private readonly IWebSpeechService _speechService;
        private readonly IAiQuestionService _aiService;
        private readonly ILogger<VoiceRecallService>? _logger;

        public bool IsVoiceSupported => _speechService.IsRecognitionSupported;

        public VoiceRecallService(
            IWebSpeechService speechService,
            IAiQuestionService aiService,
            ILogger<VoiceRecallService>? logger = null)
        {
            _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _logger = logger;
        }

        /// <summary>
        /// 开始语音回忆
        /// </summary>
        public async Task<VoiceRecallResult> StartRecallAsync(ReviewItem item, CancellationToken ct = default)
        {
            var result = new VoiceRecallResult
            {
                CorrectAnswer = item.Answer
            };

            try
            {
                // 朗读问题
                _logger?.LogDebug("朗读问题: {Content}", item.Content);
                await _speechService.SpeakAsync(item.Content);

                // 等待一段时间让用户准备
                await Task.Delay(1500, ct);

                // 开始语音识别
                _logger?.LogDebug("开始语音识别");
                var recognitionResult = await _speechService.RecognizeOnceAsync();

                if (recognitionResult.IsSuccess)
                {
                    result.UserAnswer = recognitionResult.Text;
                    _logger?.LogDebug("识别结果: {Text}", result.UserAnswer);

                    // 评估答案
                    return await EvaluateAnswerAsync(item, result.UserAnswer, ct);
                }
                else
                {
                    result.Feedback = $"语音识别失败: {recognitionResult.Error ?? "未知错误"}";
                    _logger?.LogWarning("语音识别失败: {Error}", recognitionResult.Error);
                }
            }
            catch (OperationCanceledException)
            {
                result.Feedback = "操作已取消";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "语音回忆失败");
                result.Feedback = $"发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 评估用户回答
        /// </summary>
        public async Task<VoiceRecallResult> EvaluateAnswerAsync(
            ReviewItem item, string spokenAnswer, CancellationToken ct = default)
        {
            var result = new VoiceRecallResult
            {
                UserAnswer = spokenAnswer,
                CorrectAnswer = item.Answer
            };

            try
            {
                // 构建评估提示
                var evaluationPrompt = BuildEvaluationPrompt(item.Content, item.Answer, spokenAnswer);

                // 调用AI评估
                var feedback = await _aiService.AskAsync(evaluationPrompt, "", ct);

                result.Feedback = feedback;

                // 解析评分
                result.Score = ParseScore(feedback);
                result.IsCorrect = result.Score >= 60;

                // 如果是语言学习，评估发音
                if (IsLanguageContent(item.Content))
                {
                    result.PronunciationScore = EvaluatePronunciation(spokenAnswer, item.Answer);
                }

                // 朗读反馈
                await _speechService.SpeakAsync(result.GetFeedbackMessage());

                _logger?.LogInformation("评估结果: 得分={Score}, 正确={Correct}", result.Score, result.IsCorrect);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "评估答案失败");
                result.Feedback = "评估服务暂时不可用";
                result.Score = 0;
            }

            return result;
        }

        /// <summary>
        /// 获取提示
        /// </summary>
        public async Task<string> GetHintAsync(ReviewItem item, int hintLevel, CancellationToken ct = default)
        {
            var hint = hintLevel switch
            {
                1 => GetFirstHint(item),
                2 => GetSecondHint(item),
                3 => GetThirdHint(item),
                _ => "无法提供更多提示"
            };

            // 朗读提示
            await _speechService.SpeakAsync(hint);

            return hint;
        }

        #region 私有方法

        private static string BuildEvaluationPrompt(string question, string correctAnswer, string userAnswer)
        {
            return $"请评估以下学习问答的质量。\n\n" +
                   $"问题：{question}\n" +
                   $"正确答案：{correctAnswer}\n" +
                   $"用户回答：{userAnswer}\n\n" +
                   "请从以下方面评估：\n" +
                   "1. 答案是否准确匹配（重要）\n" +
                   "2. 意思是否正确（即使表述略有不同）\n" +
                   "3. 发音/拼写是否有问题\n\n" +
                   "请给出评分（0-100）和简要反馈，格式如下：\n" +
                   "评分：85\n" +
                   "反馈：回答基本正确...";
        }

        private static int ParseScore(string feedback)
        {
            // 尝试从反馈中提取分数
            var patterns = new[]
            {
                @"评分[：:]\s*(\d+)",
                @"score[：:]\s*(\d+)",
                @"(\d+)\s*分",
                @"(\d+)/100"
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(feedback, pattern);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var score))
                {
                    return Math.Clamp(score, 0, 100);
                }
            }

            // 如果找不到明确的分数，根据关键词判断
            if (feedback.Contains("完全正确") || feedback.Contains("非常准确"))
                return 95;
            if (feedback.Contains("正确") || feedback.Contains("准确"))
                return 80;
            if (feedback.Contains("部分正确") || feedback.Contains("基本正确"))
                return 60;
            if (feedback.Contains("不太准确") || feedback.Contains("有误"))
                return 40;
            if (feedback.Contains("错误") || feedback.Contains("不正确"))
                return 20;

            return 50; // 默认分数
        }

        private static string GetFirstHint(ReviewItem item)
        {
            if (item.Answer.Length > 2)
            {
                var hint = item.Answer.Substring(0, item.Answer.Length / 2);
                return $"提示：答案是以「{hint}」开头的";
            }
            return "提示：再仔细想想...";
        }

        private static string GetSecondHint(ReviewItem item)
        {
            if (item.Answer.Length > 4)
            {
                var firstChar = item.Answer.Substring(0, 1);
                return $"提示：第一个字是「{firstChar}」，这个词有 {item.Answer.Length} 个字";
            }
            return "提示：这个问题有几个要点...";
        }

        private static string GetThirdHint(ReviewItem item)
        {
            if (!string.IsNullOrEmpty(item.Hint))
            {
                return $"提示：{item.Hint}";
            }

            // 如果有相关词汇，可以给出关联提示
            return $"提示：这个知识点和某个概念相关哦~";
        }

        private static bool IsLanguageContent(string content)
        {
            // 简单判断是否为语言学习内容
            var langIndicators = new[] { "word", "spell", "pronounce", "音标", "拼写", "读音", "单词", "拼音" };
            return langIndicators.Any(i => content.Contains(i, StringComparison.OrdinalIgnoreCase));
        }

        private static double EvaluatePronunciation(string spoken, string correct)
        {
            if (string.IsNullOrEmpty(spoken) || string.IsNullOrEmpty(correct))
                return 0;

            // 简单的相似度计算
            spoken = spoken.Trim().ToLower();
            correct = correct.Trim().ToLower();

            if (spoken == correct)
                return 100;

            // 计算编辑距离
            var distance = CalculateLevenshteinDistance(spoken, correct);
            var maxLength = Math.Max(spoken.Length, correct.Length);
            var similarity = (1 - (double)distance / maxLength) * 100;

            return Math.Max(0, similarity);
        }

        private static int CalculateLevenshteinDistance(string s1, string s2)
        {
            var m = s1.Length;
            var n = s2.Length;
            var d = new int[m + 1, n + 1];

            for (int i = 0; i <= m; i++) d[i, 0] = i;
            for (int j = 0; j <= n; j++) d[0, j] = j;

            for (int j = 1; j <= n; j++)
            {
                for (int i = 1; i <= m; i++)
                {
                    var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[m, n];
        }

        #endregion
    }

    /// <summary>
    /// VoiceRecallResult扩展方法
    /// </summary>
    public static class VoiceRecallResultExtensions
    {
        /// <summary>
        /// 获取反馈消息（用于语音朗读）
        /// </summary>
        public static string GetFeedbackMessage(this VoiceRecallResult result)
        {
            if (result.IsCorrect)
            {
                return result.Score >= 90
                    ? "太棒了！完全正确！"
                    : result.Score >= 75
                        ? "回答正确，很好！"
                        : "正确！继续保持。";
            }

            return $"回答不正确。正确答案是：{result.CorrectAnswer}";
        }
    }
}
