using LearningAssistant.Models.Quiz;
using LearningAssistant.Services.AI;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Quiz
{
    /// <summary>
    /// 测验引擎服务实现
    /// 使用AI生成练习题，支持多种题型
    /// </summary>
    public class QuizEngineService : IQuizEngineService
    {
        private readonly IAiQuestionService _aiService;
        private readonly ILogger<QuizEngineService>? _logger;

        public QuizEngineService(
            IAiQuestionService aiService,
            ILogger<QuizEngineService>? logger = null)
        {
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _logger = logger;
        }

        /// <summary>
        /// 创建测验会话
        /// </summary>
        public Task<QuizSession> CreateSessionAsync(string userId, string title, int questionCount, QuestionType[] types)
        {
            var session = new QuizSession
            {
                UserId = userId,
                Title = title,
                Questions = new List<QuizQuestion>(),
                CurrentIndex = 0,
                StartTime = DateTime.Now
            };

            _logger?.LogInformation("创建测验会话: 用户={UserId}, 标题={Title}, 题数={Count}",
                userId, title, questionCount);

            return Task.FromResult(session);
        }

        /// <summary>
        /// 生成练习题（使用AI）
        /// </summary>
        public async Task<List<QuizQuestion>> GenerateQuestionsAsync(
            string content, string language, int count, QuestionType type)
        {
            if (string.IsNullOrWhiteSpace(content))
                return new List<QuizQuestion>();

            var questions = new List<QuizQuestion>();

            try
            {
                _logger?.LogDebug("开始生成测验题目: 类型={Type}, 数量={Count}", type, count);

                // 调用AI生成题目
                var prompt = BuildQuestionPrompt(content, language, count, type);
                var aiResponse = await _aiService.GenerateExerciseAsync(content, language);

                if (string.IsNullOrWhiteSpace(aiResponse))
                {
                    _logger?.LogWarning("AI未返回题目内容");
                    return questions;
                }

                // 解析AI返回的题目
                questions = ParseQuestions(aiResponse, type, content);

                _logger?.LogInformation("生成测验题目成功: 生成={Count}, 解析={Parsed}",
                    count, questions.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成测验题目失败");
            }

            return questions;
        }

        /// <summary>
        /// 构建题目生成提示词
        /// </summary>
        private static string BuildQuestionPrompt(string content, string language, int count, QuestionType type)
        {
            var typeName = type switch
            {
                QuestionType.SingleChoice => "单选题",
                QuestionType.MultipleChoice => "多选题",
                QuestionType.TrueFalse => "判断题",
                QuestionType.FillInBlank => "填空题",
                QuestionType.ShortAnswer => "简答题",
                _ => "练习题"
            };

            var lang = language == "中文" ? "中文" : "英文";

            return $"请根据以下{lang}学习内容，生成{count}道{typeName}。\n" +
                   "要求：\n" +
                   "1. 题目清晰明确，答案唯一\n" +
                   "2. 选项要合理，不要有明显的错误选项\n" +
                   "3. 如果是选择题，请用以下格式返回：\n" +
                   "   [题目]\n" +
                   "   A. 选项1\n" +
                   "   B. 选项2\n" +
                   "   C. 选项3\n" +
                   "   D. 选项4\n" +
                   "   答案: B\n\n" +
                   $"学习内容：\n{content}";
        }

        /// <summary>
        /// 解析AI返回的题目
        /// </summary>
        private List<QuizQuestion> ParseQuestions(string aiResponse, QuestionType type, string sourceContent)
        {
            var questions = new List<QuizQuestion>();
            var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            string? currentQuestion = null;
            var options = new List<string>();
            var correctAnswers = new List<int>();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // 检测题目（以数字开头或包含问号）
                if (trimmed.Length > 3 && (char.IsDigit(trimmed[0]) || trimmed.Contains('?')))
                {
                    // 保存上一题
                    if (currentQuestion != null)
                    {
                        questions.Add(CreateQuestion(currentQuestion, options, correctAnswers, type, sourceContent));
                    }

                    currentQuestion = trimmed;
                    options.Clear();
                    correctAnswers.Clear();
                }
                // 检测选项
                else if (trimmed.StartsWith("A.") || trimmed.StartsWith("A、") ||
                         trimmed.StartsWith("a.") || trimmed.StartsWith("a、"))
                {
                    options.Add(ExtractOptionText(trimmed));
                }
                else if (trimmed.StartsWith("B.") || trimmed.StartsWith("B、") ||
                         trimmed.StartsWith("b.") || trimmed.StartsWith("b、"))
                {
                    options.Add(ExtractOptionText(trimmed));
                }
                else if (trimmed.StartsWith("C.") || trimmed.StartsWith("C、") ||
                         trimmed.StartsWith("c.") || trimmed.StartsWith("c、"))
                {
                    options.Add(ExtractOptionText(trimmed));
                }
                else if (trimmed.StartsWith("D.") || trimmed.StartsWith("D、") ||
                         trimmed.StartsWith("d.") || trimmed.StartsWith("d、"))
                {
                    options.Add(ExtractOptionText(trimmed));
                }
                // 检测答案
                else if (trimmed.Contains("答案") || trimmed.Contains("Answer"))
                {
                    var answerPart = ExtractAnswer(trimmed);
                    correctAnswers = ParseAnswerIndices(answerPart);
                }
            }

            // 保存最后一题
            if (currentQuestion != null)
            {
                questions.Add(CreateQuestion(currentQuestion, options, correctAnswers, type, sourceContent));
            }

            return questions;
        }

        private static string ExtractOptionText(string line)
        {
            // 移除 "A. " 或 "A、 " 等前缀
            if (line.Length > 2)
            {
                var text = line.Substring(2).Trim();
                // 移除答案标记（如"[答案]"）
                text = System.Text.RegularExpressions.Regex.Replace(text, @"\[.*?\]", "");
                return text;
            }
            return line;
        }

        private static string ExtractAnswer(string line)
        {
            // 提取答案部分
            var colonIndex = line.IndexOf(':');
            if (colonIndex >= 0 && colonIndex < line.Length - 1)
            {
                return line.Substring(colonIndex + 1).Trim();
            }
            return line.Replace("答案", "").Replace("Answer", "").Trim();
        }

        private static List<int> ParseAnswerIndices(string answer)
        {
            var indices = new List<int>();
            var upper = answer.ToUpper();

            if (upper.Contains("A") || upper.Contains("正确") && upper.Contains("T"))
                indices.Add(0);
            if (upper.Contains("B") || (upper.Contains("正确") && !upper.Contains("T")))
                indices.Add(1);
            if (upper.Contains("C"))
                indices.Add(2);
            if (upper.Contains("D"))
                indices.Add(3);

            // 如果是数字，尝试解析
            if (indices.Count == 0 && int.TryParse(answer.Trim(), out var num))
            {
                indices.Add(num - 1);
            }

            return indices;
        }

        private static QuizQuestion CreateQuestion(
            string content, List<string> options, List<int> correctAnswers,
            QuestionType type, string sourceContent)
        {
            return new QuizQuestion
            {
                Content = content,
                Type = type,
                Options = options,
                CorrectOptionIndices = correctAnswers,
                SourceContent = sourceContent,
                Difficulty = QuestionDifficulty.Medium
            };
        }

        /// <summary>
        /// 提交答案
        /// </summary>
        public void SubmitAnswer(QuizSession session, int questionIndex, List<int> selectedIndices, string? textAnswer = null)
        {
            if (questionIndex < 0 || questionIndex >= session.Questions.Count)
                return;

            var question = session.Questions[questionIndex];
            question.IsAnswered = true;
            question.UserSelectedIndices = selectedIndices ?? new List<int>();

            if (!string.IsNullOrEmpty(textAnswer))
            {
                question.UserTextAnswer = textAnswer;
            }

            _logger?.LogDebug("提交答案: 题目={Index}, 选择={Selected}, 正确={Correct}",
                questionIndex, string.Join(",", selectedIndices), question.IsCorrect);
        }

        /// <summary>
        /// 获取测验结果
        /// </summary>
        public QuizResult GetResult(QuizSession session)
        {
            session.Complete();

            var result = new QuizResult
            {
                SessionId = session.SessionId,
                UserId = session.UserId,
                Title = session.Title,
                TotalQuestions = session.TotalCount,
                CorrectAnswers = session.CorrectCount,
                AccuracyRate = session.AccuracyRate,
                TimeSpentSeconds = session.ElapsedSeconds,
                CompletedAt = DateTime.Now,
                WrongQuestions = session.Questions.Where(q => q.IsAnswered && !q.IsCorrect).ToList()
            };

            // 分析薄弱知识点
            var tagStats = new Dictionary<string, (int total, int wrong)>();
            foreach (var q in session.Questions.Where(q => q.IsAnswered))
            {
                foreach (var tag in q.Tags)
                {
                    if (!tagStats.ContainsKey(tag))
                        tagStats[tag] = (0, 0);
                    var (total, wrong) = tagStats[tag];
                    tagStats[tag] = (total + 1, q.IsCorrect ? wrong : wrong + 1);
                }
            }

            result.WeakTags = tagStats
                .Where(t => t.Value.wrong > 0)
                .OrderByDescending(t => (double)t.Value.wrong / t.Value.total)
                .Select(t => t.Key)
                .Take(5)
                .ToList();

            return result;
        }

        /// <summary>
        /// AI评估简答题答案
        /// </summary>
        public async Task<string> EvaluateShortAnswerAsync(
            string question, string userAnswer, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userAnswer))
                return "请先输入您的答案";

            var prompt = $"请评估以下简答题答案的质量。\n\n" +
                         $"题目：{question}\n\n" +
                         $"学生答案：{userAnswer}\n\n" +
                         "请从以下方面评估：\n" +
                         "1. 答案是否准确\n" +
                         "2. 表述是否清晰\n" +
                         "3. 给出改进建议\n\n" +
                         "评估结果：";

            try
            {
                var evaluation = await _aiService.AskAsync(prompt, "", ct);
                return evaluation;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "评估简答题答案失败");
                return "评估服务暂时不可用";
            }
        }
    }
}
