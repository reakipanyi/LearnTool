using LearningAssistant.Forms;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Managers
{
    /// <summary>
    /// 思考功能集成示例 - 演示如何在 LearningForm 中使用思考激发功能
    /// </summary>
    public class ThinkingFeaturesDemo
    {
        private LearningForm _learningForm;
        private readonly ILogger<ThinkingFeaturesDemo> _logger;

        /// <summary>
        /// 初始化思考功能
        /// </summary>
        public void InitializeThinkingFeatures(LearningForm form)
        {
            _learningForm = form;

            // 在 LearningForm 中添加思考功能按钮
            AddThinkingButtons();

            // 绑定事件
            BindThinkingEvents();
        }

        /// <summary>
        /// 添加思考功能按钮
        /// </summary>
        private void AddThinkingButtons()
        {
            // 这些按钮应该在 LearningForm 的 InitializeComponent 方法中添加
            // 这里只是示例如何创建

            // 渐进提示按钮
            var btnProgressiveHint = new Button
            {
                Name = "buttonProgressiveHint",
                Text = "💡 渐进提示",
                Size = new System.Drawing.Size(100, 45),
                BackColor = System.Drawing.Color.FromArgb(76, 175, 80),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Bold)
            };
            btnProgressiveHint.Click += OnProgressiveHintClick;

            // 联想学习按钮
            var btnAssociation = new Button
            {
                Name = "buttonAssociation",
                Text = "🔗 联想学习",
                Size = new System.Drawing.Size(100, 45),
                BackColor = System.Drawing.Color.FromArgb(103, 58, 183),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Bold)
            };
            btnAssociation.Click += OnAssociationClick;

            // 主动回忆按钮
            var btnActiveRecall = new Button
            {
                Name = "buttonActiveRecall",
                Text = "🧠 主动回忆",
                Size = new System.Drawing.Size(100, 45),
                BackColor = System.Drawing.Color.FromArgb(255, 152, 0),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Bold)
            };
            btnActiveRecall.Click += OnActiveRecallClick;

            // 每日思考按钮
            var btnDailyThinking = new Button
            {
                Name = "buttonDailyThinking",
                Text = "📋 思考任务",
                Size = new System.Drawing.Size(100, 45),
                BackColor = System.Drawing.Color.FromArgb(66, 133, 244),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Bold)
            };
            btnDailyThinking.Click += OnDailyThinkingClick;

            MessageBox.Show(
                "思考功能按钮已准备好！\n\n" +
                "请在 LearningForm 的 InitializeComponent 方法中添加这些按钮到适当位置。\n\n" +
                "推荐位置：\n" +
                "1. 💡 渐进提示 - 在【显示答案】按钮附近\n" +
                "2. 🔗 联想学习 - 在【收藏】按钮附近\n" +
                "3. 🧠 主动回忆 - 在【答题模式】按钮附近\n" +
                "4. 📋 思考任务 - 在【学习统计】附近",
                "集成提示",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        /// <summary>
        /// 绑定思考功能事件
        /// </summary>
        private void BindThinkingEvents()
        {
            // 事件绑定应该在 LearningForm 的构造函数或 Load 事件中完成
        }

        /// <summary>
        /// 渐进提示按钮点击事件
        /// </summary>
        private void OnProgressiveHintClick(object sender, EventArgs e)
        {
            if (_learningForm == null) return;

            // 获取当前学习内容
            var currentContent = GetCurrentContent();
            var currentAnswer = GetCurrentAnswer();

            if (string.IsNullOrEmpty(currentContent))
            {
                MessageBox.Show("请先选择要学习的内容！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 创建渐进提示
            var hints = GenerateSmartHints(currentContent, currentAnswer);

            var hintForm = new ProgressiveHintForm(currentContent, currentAnswer, hints, (revealedAnswer) =>
            {
                // 记录用户查看答案
                LogAnswerRevealed(currentContent, revealedAnswer);
            });

            hintForm.ShowDialog();
        }

        /// <summary>
        /// 联想学习按钮点击事件
        /// </summary>
        private void OnAssociationClick(object sender, EventArgs e)
        {
            if (_learningForm == null) return;

            var currentContent = GetCurrentContent();

            if (string.IsNullOrEmpty(currentContent))
            {
                MessageBox.Show("请先选择要学习的内容！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var associationForm = new AssociationLearningForm(currentContent);
            associationForm.ShowDialog();
        }

        /// <summary>
        /// 主动回忆按钮点击事件
        /// </summary>
        private void OnActiveRecallClick(object sender, EventArgs e)
        {
            if (_learningForm == null) return;

            // 获取当前学习列表中的所有项目
            var reviewItems = GetReviewItemsFromLearningList();

            if (reviewItems.Count == 0)
            {
                MessageBox.Show("没有可复习的内容！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var recallForm = new ActiveRecallForm();
            recallForm.LoadReviewItems(reviewItems);
            recallForm.ShowDialog();
        }

        /// <summary>
        /// 每日思考任务按钮点击事件
        /// </summary>
        private void OnDailyThinkingClick(object sender, EventArgs e)
        {
            var thinker = new ThinkingStimulator(_learningForm);
            thinker.ShowDailyThinkingTask();
        }

        #region 辅助方法

        /// <summary>
        /// 获取当前学习内容
        /// </summary>
        private string GetCurrentContent()
        {
            // 这个方法需要根据 LearningForm 的实际实现来获取当前内容
            // 示例实现：
            try
            {
                var field = _learningForm.GetType().GetField("_currentItem",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (field != null)
                {
                    var currentItem = field.GetValue(_learningForm);
                    if (currentItem != null)
                    {
                        var method = currentItem.GetType().GetMethod("GetMainContent");
                        return method?.Invoke(currentItem, null)?.ToString() ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                // 使用正确的日志方法签名 (Exception, string)
                _logger?.LogWarning(ex, "获取当前学习内容失败");
            }

            return "";
        }

        /// <summary>
        /// 获取当前答案
        /// </summary>
        private string GetCurrentAnswer()
        {
            try
            {
                var field = _learningForm.GetType().GetField("_currentItem",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (field != null)
                {
                    var currentItem = field.GetValue(_learningForm);
                    if (currentItem != null)
                    {
                        var method = currentItem.GetType().GetMethod("GetDisplayText");
                        return method?.Invoke(currentItem, null)?.ToString() ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                // 使用正确的日志方法签名 (Exception, string)
                _logger?.LogWarning(ex, "获取当前答案失败");
            }

            return "";
        }

        /// <summary>
        /// 从学习列表获取复习项目
        /// </summary>
        private List<ReviewItem> GetReviewItemsFromLearningList()
        {
            var items = new List<ReviewItem>();

            // 这里应该从 LearningForm 的学习列表中获取所有项目
            // 示例：
            try
            {
                // 获取 listBoxItems 中的内容
                var listBoxField = _learningForm.GetType().GetField("listBoxItems",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (listBoxField != null)
                {
                    var listBox = listBoxField.GetValue(_learningForm) as ListBox;
                    if (listBox != null && listBox.Items.Count > 0)
                    {
                        // 限制数量，避免过多
                        int maxItems = Math.Min(listBox.Items.Count, 20);
                        Random random = new Random();

                        for (int i = 0; i < maxItems; i++)
                        {
                            var itemText = listBox.Items[i].ToString();
                            if (!string.IsNullOrEmpty(itemText))
                            {
                                // 移除序号
                                var content = itemText.Contains('.')
                                    ? itemText.Substring(itemText.IndexOf('.') + 1).Trim()
                                    : itemText;

                                items.Add(new ReviewItem
                                {
                                    Question = $"回忆这个内容：{content}",
                                    Answer = content,
                                    Hint = GenerateHint(content)
                                });
                            }
                        }

                        // 随机打乱顺序
                        items = items.OrderBy(x => random.Next()).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取复习列表失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return items;
        }

        /// <summary>
        /// 生成智能提示
        /// </summary>
        private List<string> GenerateSmartHints(string content, string answer)
        {
            var hints = new List<string>();

            if (IsChineseContent(content))
            {
                // 汉字内容提示
                hints.Add($"📝 这个内容有 {content.Length} 个字");

                if (content.Length > 1)
                {
                    hints.Add($"💡 第一个字是：'{content[0]}'");
                }

                hints.Add($"🔄 可以组成词语或句子");
                hints.Add($"🤔 想想在什么场景下会用到");
            }
            else if (IsEnglishContent(content))
            {
                // 英文内容提示
                hints.Add($"📝 这个内容有 {content.Length} 个字母");
                hints.Add($"🔤 首字母是：{content[0].ToString().ToUpper()}");

                if (content.Contains(" "))
                {
                    var words = content.Split(' ');
                    hints.Add($"💡 这个短语有 {words.Length} 个单词");
                }

                hints.Add($"🌐 想想其中文意思是什么");
            }
            else
            {
                // 默认提示
                hints.Add("💡 先回忆相关的背景知识");
                hints.Add("🔗 这个内容和什么有关？");
                hints.Add("📖 想想之前学过的类似内容");
                hints.Add("❓ 如果要解释给朋友听，你会怎么说？");
            }

            return hints.Take(4).ToList();
        }

        /// <summary>
        /// 生成单条提示
        /// </summary>
        private string GenerateHint(string content)
        {
            if (IsChineseContent(content))
            {
                return $"这个词和'学习'、'知识'有关";
            }
            else if (IsEnglishContent(content))
            {
                return "想想这个单词的中文意思";
            }
            else
            {
                return "回忆一下之前学过的内容";
            }
        }

        /// <summary>
        /// 判断是否为中文内容
        /// </summary>
        private bool IsChineseContent(string text)
        {
            return text.Any(c => c >= 0x4e00 && c <= 0x9fa5);
        }

        /// <summary>
        /// 判断是否为英文内容
        /// </summary>
        private bool IsEnglishContent(string text)
        {
            return text.All(c =>
                (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                c == ' ');
        }

        /// <summary>
        /// 记录用户查看答案
        /// </summary>
        private void LogAnswerRevealed(string content, string answer)
        {
            // 记录到日志或数据库
            System.Diagnostics.Debug.WriteLine(
                $"用户查看了答案 - 内容: {content}, 答案: {answer}"
            );
        }

        #endregion

        #region 推荐的学习流程

        /// <summary>
        /// 推荐的学习流程 - 整合所有思考功能
        /// </summary>
        public void ShowRecommendedLearningFlow()
        {
            MessageBox.Show(
                "🎯 推荐的学习流程\n\n" +
                "1️⃣ 【预习】先看联想学习，了解相关知识\n" +
                "   ↓\n" +
                "2️⃣ 【思考】用渐进提示尝试回忆\n" +
                "   ↓\n" +
                "3️⃣ 【练习】主动回忆训练巩固记忆\n" +
                "   ↓\n" +
                "4️⃣ 【总结】完成每日思考任务\n" +
                "   ↓\n" +
                "5️⃣ 【复习】定期进行间隔复习\n\n" +
                "💡 这样学习，记忆更深刻，理解更透彻！",
                "学习流程建议",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        #endregion
    }
}
