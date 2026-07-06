using LearningAssistant.Forms;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Learning;

namespace LearningAssistant.Managers
{
    /// <summary>
    /// 思考激发服务接口
    /// </summary>
    public interface IThinkingStimulator
    {
        void StartProgressiveHint(string content, string answer);
        void StartAssociationLearning(string content);
        void StartActiveRecall(List<ReviewItem> reviewItems);
        List<string> CreateFeynmanQuestions(string content);
        void ShowDailyThinkingTask();
    }

    /// <summary>
    /// 思考激发管理器
    /// 
    /// 核心功能：整合所有激发思考的功能，提供统一的调用接口。
    /// 
    /// 主要功能：
    /// 1. 渐进式提示 - 引导用户逐步思考，避免直接给出答案
    /// 2. 联想学习 - 帮助用户建立知识网络，关联不同知识点
    /// 3. 主动回忆训练 - 基于艾宾浩斯遗忘曲线的复习训练
    /// 4. 费曼学习法问题生成 - 帮助用户深入理解知识
    /// 5. 每日思考任务 - 鼓励用户进行深度思考
    /// 
    /// 使用场景：
    /// - 学习单词、汉字时激发主动思考
    /// - 复习阶段的主动回忆训练
    /// - 帮助用户建立知识关联
    /// - 培养用户的深度思考习惯
    /// </summary>
    public class ThinkingStimulator : IThinkingStimulator
    {
        private readonly IAiQuestionService? _aiService;
        private readonly IProgressiveHintStateService? _hintStateService;
        private readonly string _userId = "default";

        #region 构造函数

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ThinkingStimulator()
        {
        }

        /// <summary>
        /// 带 AI 服务的构造函数
        /// </summary>
        public ThinkingStimulator(IAiQuestionService? aiService, IProgressiveHintStateService? hintStateService = null, string userId = "default")
        {
            _aiService = aiService;
            _hintStateService = hintStateService;
            _userId = userId;
        }

        #endregion

        #region 核心功能方法

        /// <summary>
        /// 启动渐进式提示
        /// 
        /// 渐进式提示系统引导用户先主动思考，逐步提供提示，避免直接给出答案。
        /// 提示分为4级，由弱到强，用户需要按顺序解锁。
        /// </summary>
        /// <param name="content">要学习的内容（问题）</param>
        /// <param name="answer">正确答案</param>
        public void StartProgressiveHint(string content, string answer)
        {
            var hints = GenerateHints(content, answer);

            ProgressiveHintForm? hintForm = null;

            if (_hintStateService != null)
            {
                var savedProgress = _hintStateService.GetProgress(content, _userId);
                if (savedProgress != null && savedProgress.ViewedHints.Count > 0)
                {
                    var viewedSet = new HashSet<int>(savedProgress.ViewedHints);
                    hintForm = new ProgressiveHintForm(content, answer, hints,
                        savedProgress.UserGuess,
                        savedProgress.CurrentHintLevel,
                        viewedSet,
                        (revealedAnswer) =>
                        {
                            System.Diagnostics.Trace.TraceInformation($"用户查看了答案: {revealedAnswer}");
                        });
                }
            }

            hintForm ??= new ProgressiveHintForm(content, answer, hints, (revealedAnswer) =>
            {
                System.Diagnostics.Trace.TraceInformation($"用户查看了答案: {revealedAnswer}");
            });

            hintForm.FormClosing += (s, e) =>
            {
                if (_hintStateService != null)
                {
                    var progress = new HintProgress
                    {
                        CurrentHintLevel = hintForm.CurrentHintLevel,
                        ViewedHints = hintForm.ViewedHints.ToList(),
                        UserGuess = hintForm.UserGuess
                    };
                    _hintStateService.SaveProgress(content, _userId, progress);
                }
            };

            hintForm.ShowDialog();
        }

        /// <summary>
        /// 启动联想学习
        /// 
        /// 联想学习系统帮助用户建立知识网络，通过展示同类词、反义词、相关词等，
        /// 帮助用户理解知识点之间的关联，增强记忆效果。
        /// </summary>
        /// <param name="content">要学习的内容</param>
        public void StartAssociationLearning(string content)
        {
            var associationForm = new AssociationLearningForm(content, _aiService);
            associationForm.ShowDialog();
        }

        /// <summary>
        /// 启动主动回忆训练
        /// 
        /// 主动回忆训练基于艾宾浩斯遗忘曲线，让用户先回忆再检查答案，
        /// 通过主动提取记忆来巩固知识，比被动复习效果更好。
        /// </summary>
        /// <param name="reviewItems">复习项目列表</param>
        public void StartActiveRecall(List<ReviewItem> reviewItems)
        {
            // 检查是否有可复习的内容
            if (reviewItems == null || reviewItems.Count == 0)
            {
                MessageBox.Show("没有需要复习的内容！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 创建并显示主动回忆训练器
            var recallForm = new ActiveRecallForm();
            recallForm.LoadReviewItems(reviewItems);
            recallForm.ShowDialog();
        }

        #endregion

        #region 提示生成辅助方法

        /// <summary>
        /// 生成渐进式提示
        /// 
        /// 根据内容类型（中文/英文/其他）生成不同的提示策略：
        /// - 中文：字数、首字、组词、近义词
        /// - 英文：字母数、首字母、中文意思、动作关联
        /// - 其他：通用提示策略
        /// </summary>
        /// <param name="content">要学习的内容</param>
        /// <param name="answer">正确答案（未使用，预留参数）</param>
        /// <returns>提示列表（最多4条）</returns>
        private List<string> GenerateHints(string content, string answer)
        {
            var hints = new List<string>();

            // 根据内容类型生成不同的提示
            if (IsChineseWord(content))
            {
                // 汉字/词语提示策略
                hints.Add($"💡 这个词有 {content.Length} 个字");
                hints.Add($"💡 第一个字是：{content[0]}");
                hints.Add($"💡 可以组词：{GenerateRelatedWords(content)}");
                hints.Add($"💡 想想学过的近义词...");
            }
            else if (IsEnglishWord(content))
            {
                // 英文单词提示策略
                hints.Add($"💡 这个单词有 {content.Length} 个字母");
                hints.Add($"💡 首字母是：{content[0].ToString().ToUpper()}");
                hints.Add($"💡 想想它的中文意思...");
                hints.Add($"💡 和某个动作有关？");
            }
            else
            {
                // 默认提示策略（适用于其他类型内容）
                hints.Add("💡 先回忆一下相关背景知识");
                hints.Add("💡 和生活经验有什么联系？");
                hints.Add("💡 用自己的话描述一下");
                hints.Add("💡 别急，想想类似的知识点");
            }

            // 返回最多4条提示
            return hints.Take(4).ToList();
        }

        /// <summary>
        /// 生成相关词语
        /// 
        /// 生成与给定词语相关的词汇，用于提示用户联想记忆。
        /// 简化版本使用预设词汇，实际应用中可以从词典API获取。
        /// </summary>
        /// <param name="word">输入词语</param>
        /// <returns>相关词语（用顿号分隔）</returns>
        private string GenerateRelatedWords(string word)
        {
            // 简化版本，实际可以从词典API获取更准确的相关词
            var wordList = new List<string> { "学习", "练习", "复习", "思考" };
            return string.Join("、", wordList.Take(3));
        }

        /// <summary>
        /// 检查是否为中文词语
        /// 
        /// 通过Unicode编码范围判断是否为中文字符。
        /// </summary>
        /// <param name="text">输入文本</param>
        /// <returns>是否包含中文字符</returns>
        private bool IsChineseWord(string text)
        {
            return text.Any(c => c >= 0x4e00 && c <= 0x9fa5);
        }

        /// <summary>
        /// 检查是否为英文单词
        /// 
        /// 判断文本是否仅包含英文字母。
        /// </summary>
        /// <param name="text">输入文本</param>
        /// <returns>是否为纯英文单词</returns>
        private bool IsEnglishWord(string text)
        {
            return text.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));
        }

        #endregion

        #region 费曼学习法支持

        /// <summary>
        /// 创建费曼学习法问题
        /// 
        /// 费曼学习法核心思想：通过向别人解释知识来检验自己的理解程度。
        /// 生成的问题引导用户深入思考，用简单易懂的方式解释知识。
        /// </summary>
        /// <param name="content">学习内容</param>
        /// <returns>思考问题列表</returns>
        public List<string> CreateFeynmanQuestions(string content)
        {
            return new List<string>
            {
                $"📖 {content} 是什么意思？",
                "🔍 能举一个生活中的例子吗？",
                "💡 如果要向小学生解释，你会怎么说？",
                "🔗 它和之前学过的什么知识有关？",
                "❓ 有什么有趣的记忆方法？"
            };
        }

        #endregion

        #region 每日思考任务

        /// <summary>
        /// 显示每日思考任务
        /// 
        /// 每日思考任务鼓励用户进行深度思考和知识整合，
        /// 帮助用户巩固所学知识，建立知识网络。
        /// </summary>
        public void ShowDailyThinkingTask()
        {
            var tasks = new List<string>
            {
                "🎯 今天学习的内容中，哪个最难理解？为什么？",
                "🔗 尝试找出一个今天学到的知识和以前知识的联系",
                "📝 用自己的话解释今天学到的三个知识点",
                "💡 想想这些知识在生活中可以用在哪里？",
                "🤔 如果你是老师，你会怎么教这个知识点？"
            };

            // 构建任务消息
            string message = "💪 每日思考任务\n\n";
            for (int i = 0; i < tasks.Count; i++)
            {
                message += $"{i + 1}. {tasks[i]}\n\n";
            }
            message += "\n完成思考后，你的理解会更深刻！";

            // 显示任务对话框
            MessageBox.Show(message, "🧠 思考任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion
    }

    #region 间隔重复学习器

    /// <summary>
    /// 间隔重复学习器
    /// 
    /// 基于艾宾浩斯遗忘曲线实现间隔重复学习，
    /// 根据用户的学习情况动态调整复习间隔，
    /// 实现高效的长期记忆巩固。
    /// </summary>
    public class SpacedRepetitionLearner
    {
        /// <summary>
        /// 学习记录字典（键：学习内容，值：学习记录）
        /// </summary>
        private Dictionary<string, LearningRecord> _records = new Dictionary<string, LearningRecord>();

        /// <summary>
        /// 获取需要复习的内容
        /// 
        /// 根据下次复习日期筛选出当前需要复习的项目，
        /// 按紧急程度排序（连续正确次数越少越紧急）。
        /// </summary>
        /// <returns>待复习项目列表</returns>
        public List<ReviewItem> GetItemsToReview()
        {
            var itemsToReview = new List<ReviewItem>();

            foreach (var record in _records)
            {
                // 检查是否到了复习时间
                if (record.Value.NextReviewDate <= DateTime.Now)
                {
                    itemsToReview.Add(new ReviewItem
                    {
                        Question = record.Value.Question,
                        Answer = record.Value.Answer,
                        Hint = record.Value.Hint,
                        CorrectStreak = record.Value.CorrectStreak
                    });
                }
            }

            // 按紧急程度排序（连续正确次数越少越紧急）
            return itemsToReview.OrderBy(i => i.CorrectStreak).ToList();
        }

        /// <summary>
        /// 添加学习记录
        /// 
        /// 创建新的学习记录，设置首次复习时间为10分钟后。
        /// </summary>
        /// <param name="content">学习内容</param>
        /// <param name="answer">正确答案</param>
        /// <param name="hint">提示信息（可选）</param>
        public void AddLearningRecord(string content, string answer, string hint = "")
        {
            if (!_records.ContainsKey(content))
            {
                _records[content] = new LearningRecord
                {
                    Question = $"回忆: {content}",
                    Answer = answer,
                    Hint = hint,
                    CorrectStreak = 0,
                    NextReviewDate = DateTime.Now.AddMinutes(10) // 10分钟后首次复习
                };
            }
        }

        /// <summary>
        /// 更新学习记录
        /// 
        /// 根据用户答题情况更新学习记录：
        /// - 答对：增加连续正确次数，延长复习间隔（2^n 天）
        /// - 答错：重置连续正确次数，缩短复习间隔（30分钟后重试）
        /// </summary>
        /// <param name="content">学习内容</param>
        /// <param name="isCorrect">答案是否正确</param>
        public void UpdateRecord(string content, bool isCorrect)
        {
            if (!_records.ContainsKey(content))
                return;

            var record = _records[content];

            if (isCorrect)
            {
                // 答对：增加连续正确次数，按指数规律延长复习间隔
                record.CorrectStreak++;
                record.NextReviewDate = DateTime.Now.AddDays(Math.Pow(2, record.CorrectStreak));
            }
            else
            {
                // 答错：重置连续正确次数，30分钟后重试
                record.CorrectStreak = 0;
                record.NextReviewDate = DateTime.Now.AddMinutes(30);
            }
        }

        /// <summary>
        /// 学习记录内部类
        /// </summary>
        private class LearningRecord
        {
            /// <summary>
            /// 问题（回忆任务）
            /// </summary>
            public string Question { get; set; }

            /// <summary>
            /// 正确答案
            /// </summary>
            public string Answer { get; set; }

            /// <summary>
            /// 提示信息
            /// </summary>
            public string Hint { get; set; }

            /// <summary>
            /// 连续正确次数
            /// </summary>
            public int CorrectStreak { get; set; }

            /// <summary>
            /// 下次复习日期
            /// </summary>
            public DateTime NextReviewDate { get; set; }
        }
    }

    #endregion
}
