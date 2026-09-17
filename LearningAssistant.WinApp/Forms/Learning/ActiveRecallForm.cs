using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Services.Learning;

namespace LearningAssistant.Forms.Learning
{
    /// <summary>
    /// 主动回忆训练器
    /// 
    /// 核心功能：基于艾宾浩斯遗忘曲线的主动回忆训练，帮助用户通过主动提取记忆来巩固知识。
    /// 
    /// 设计原理：
    /// 1. 主动回忆优于被动复习：让用户先回忆再检查答案
    /// 2. 间隔重复：根据答题情况调整复习间隔
    /// 3. 即时反馈：提供正确/错误反馈，帮助用户及时纠正
    /// 4. 统计追踪：记录正确率，提供学习进度反馈
    /// 
    /// 使用场景：
    /// - 单词、知识点的复习巩固
    /// - 间隔重复学习计划
    /// - 考前突击复习
    /// </summary>
    public partial class ActiveRecallForm : Form
    {
        #region 字段定义

        /// <summary>
        /// 复习队列（待复习的项目列表）
        /// </summary>
        private List<ReviewItem> _reviewQueue;

        /// <summary>
        /// 当前复习索引
        /// </summary>
        private int _currentIndex = 0;

        /// <summary>
        /// 正确答题次数
        /// </summary>
        private int _correctCount = 0;

        /// <summary>
        /// 总答题次数
        /// </summary>
        private int _totalAttempts = 0;

        /// <summary>
        /// 过渡计时器（用于答题后延迟进入下一题）
        /// </summary>
        private System.Windows.Forms.Timer _transitionTimer;

        /// <summary>
        /// 用户ID
        /// </summary>
        private readonly string _userId = "default";

        /// <summary>
        /// 事件总线
        /// </summary>
        private readonly IEventBus? _eventBus;

        // UI 控件
        private Panel panelMain;
        private Label labelQuestion;
        private TextBox textBoxAnswer;
        private Button buttonCheck;
        private Label labelResult;
        private ProgressBar progressBar;
        private Label labelProgress;
        private Label labelCorrect;
        private Label labelAccuracy;
        private Label labelTitle;
        private Label labelQuestionTitle;
        private Label labelHint;
        private Label labelAnswerTitle;
        private Panel panelStats;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化主动回忆训练器
        /// </summary>
        public ActiveRecallForm()
        {
            _reviewQueue = new List<ReviewItem>();
            InitializeComponent();
            this.FormClosing += ActiveRecallForm_FormClosing;
        }

        /// <summary>
        /// 初始化主动回忆训练器（带事件总线）
        /// </summary>
        public ActiveRecallForm(IEventBus? eventBus, string userId = Constants.DefaultUserId) : this()
        {
            _eventBus = eventBus;
            _userId = userId;
        }

        /// <summary>
        /// 窗体关闭事件处理
        /// 
        /// 确保 Timer 资源被正确释放，避免内存泄漏。
        /// </summary>
        /// <param name="sender">触发事件的对象</param>
        /// <param name="e">事件参数</param>
        private void ActiveRecallForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_transitionTimer != null)
            {
                _transitionTimer.Stop();
                _transitionTimer.Dispose();
                _transitionTimer = null;
            }
        }

        /// <summary>
        /// 加载复习项目列表
        /// </summary>
        /// <param name="items">复习项目列表</param>
        public void LoadReviewItems(List<ReviewItem> items)
        {
            _reviewQueue = items ?? new List<ReviewItem>();
            _currentIndex = 0;
            _correctCount = 0;
            _totalAttempts = 0;

            UpdateDisplay();
            UpdateStats();
        }

        #endregion

        #region UI 初始化

        /// <summary>
        /// 初始化窗体组件
        /// 
        /// 创建完整的主动回忆训练界面，包括：
        /// - 标题区域
        /// - 进度条
        /// - 问题显示区域
        /// - 答案输入区域
        /// - 检查按钮
        /// - 结果显示区域
        /// - 统计面板
        /// </summary>
        private void InitializeComponent()
        {
            panelMain = new Panel();
            labelTitle = new Label();
            progressBar = new ProgressBar();
            labelProgress = new Label();
            labelQuestionTitle = new Label();
            labelQuestion = new Label();
            labelHint = new Label();
            labelAnswerTitle = new Label();
            textBoxAnswer = new TextBox();
            buttonCheck = new Button();
            labelResult = new Label();
            panelStats = new Panel();
            labelCorrect = new Label();
            labelAccuracy = new Label();
            panelMain.SuspendLayout();
            panelStats.SuspendLayout();
            SuspendLayout();
            // 
            // panelMain
            // 
            panelMain.Controls.Add(labelTitle);
            panelMain.Controls.Add(progressBar);
            panelMain.Controls.Add(labelProgress);
            panelMain.Controls.Add(labelQuestionTitle);
            panelMain.Controls.Add(labelQuestion);
            panelMain.Controls.Add(labelHint);
            panelMain.Controls.Add(labelAnswerTitle);
            panelMain.Controls.Add(textBoxAnswer);
            panelMain.Controls.Add(buttonCheck);
            panelMain.Controls.Add(labelResult);
            panelMain.Controls.Add(panelStats);
            panelMain.Dock = DockStyle.Fill;
            panelMain.Location = new Point(0, 0);
            panelMain.Name = "panelMain";
            panelMain.Size = new Size(584, 484);
            panelMain.TabIndex = 0;
            // 
            // labelTitle
            // 
            labelTitle.Dock = DockStyle.Top;
            labelTitle.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            labelTitle.ForeColor = Color.FromArgb(66, 133, 244);
            labelTitle.Location = new Point(0, 50);
            labelTitle.Name = "labelTitle";
            labelTitle.Padding = new Padding(20, 0, 0, 0);
            labelTitle.Size = new Size(584, 45);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "💪 主动回忆 - 比看答案更有效！";
            labelTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // progressBar
            // 
            progressBar.BackColor = Color.FromArgb(230, 230, 230);
            progressBar.Dock = DockStyle.Top;
            progressBar.ForeColor = Color.FromArgb(76, 175, 80);
            progressBar.Location = new Point(0, 25);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(584, 25);
            progressBar.TabIndex = 1;
            // 
            // labelProgress
            // 
            labelProgress.Dock = DockStyle.Top;
            labelProgress.Font = new Font("微软雅黑", 9F);
            labelProgress.ForeColor = Color.FromArgb(100, 100, 100);
            labelProgress.Location = new Point(0, 0);
            labelProgress.Name = "labelProgress";
            labelProgress.Padding = new Padding(20, 0, 0, 0);
            labelProgress.Size = new Size(584, 25);
            labelProgress.TabIndex = 2;
            labelProgress.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelQuestionTitle
            // 
            labelQuestionTitle.AutoSize = true;
            labelQuestionTitle.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            labelQuestionTitle.Location = new Point(30, 100);
            labelQuestionTitle.Name = "labelQuestionTitle";
            labelQuestionTitle.Size = new Size(104, 19);
            labelQuestionTitle.TabIndex = 3;
            labelQuestionTitle.Text = "🎯 回忆任务：";
            // 
            // labelQuestion
            // 
            labelQuestion.Font = new Font("微软雅黑", 20F, FontStyle.Bold);
            labelQuestion.ForeColor = Color.FromArgb(50, 50, 50);
            labelQuestion.Location = new Point(30, 130);
            labelQuestion.Name = "labelQuestion";
            labelQuestion.Size = new Size(540, 80);
            labelQuestion.TabIndex = 4;
            // 
            // labelHint
            // 
            labelHint.Font = new Font("微软雅黑", 9F);
            labelHint.ForeColor = Color.FromArgb(150, 150, 150);
            labelHint.Location = new Point(30, 215);
            labelHint.Name = "labelHint";
            labelHint.Size = new Size(540, 20);
            labelHint.TabIndex = 5;
            labelHint.Text = "💡 不要看答案！先自己回忆...";
            labelHint.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelAnswerTitle
            // 
            labelAnswerTitle.AutoSize = true;
            labelAnswerTitle.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            labelAnswerTitle.Location = new Point(30, 245);
            labelAnswerTitle.Name = "labelAnswerTitle";
            labelAnswerTitle.Size = new Size(104, 19);
            labelAnswerTitle.TabIndex = 6;
            labelAnswerTitle.Text = "✏️ 你的答案：";
            // 
            // textBoxAnswer
            // 
            textBoxAnswer.Font = new Font("微软雅黑", 14F);
            textBoxAnswer.Location = new Point(30, 275);
            textBoxAnswer.Name = "textBoxAnswer";
            textBoxAnswer.PlaceholderText = "在这里写下你的答案...";
            textBoxAnswer.Size = new Size(540, 32);
            textBoxAnswer.TabIndex = 7;
            textBoxAnswer.KeyDown += TextBoxAnswer_KeyDown;
            // 
            // buttonCheck
            // 
            buttonCheck.BackColor = Color.FromArgb(66, 133, 244);
            buttonCheck.FlatStyle = FlatStyle.Flat;
            buttonCheck.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            buttonCheck.ForeColor = Color.White;
            buttonCheck.Location = new Point(30, 320);
            buttonCheck.Name = "buttonCheck";
            buttonCheck.Size = new Size(540, 40);
            buttonCheck.TabIndex = 8;
            buttonCheck.Text = "✅ 检查答案";
            buttonCheck.UseVisualStyleBackColor = false;
            buttonCheck.Click += ButtonCheck_Click;
            // 
            // labelResult
            // 
            labelResult.Font = new Font("微软雅黑", 11F);
            labelResult.Location = new Point(30, 365);
            labelResult.Name = "labelResult";
            labelResult.Size = new Size(540, 40);
            labelResult.TabIndex = 9;
            labelResult.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panelStats
            // 
            panelStats.BackColor = Color.FromArgb(248, 250, 252);
            panelStats.Controls.Add(labelCorrect);
            panelStats.Controls.Add(labelAccuracy);
            panelStats.Dock = DockStyle.Bottom;
            panelStats.Location = new Point(0, 443);
            panelStats.Name = "panelStats";
            panelStats.Padding = new Padding(15, 0, 0, 0);
            panelStats.Size = new Size(584, 41);
            panelStats.TabIndex = 10;
            // 
            // labelCorrect
            // 
            labelCorrect.Dock = DockStyle.Left;
            labelCorrect.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelCorrect.ForeColor = Color.FromArgb(76, 175, 80);
            labelCorrect.Location = new Point(165, 0);
            labelCorrect.Name = "labelCorrect";
            labelCorrect.Size = new Size(150, 41);
            labelCorrect.TabIndex = 0;
            labelCorrect.Text = "✓ 正确: 0";
            labelCorrect.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelAccuracy
            // 
            labelAccuracy.Dock = DockStyle.Left;
            labelAccuracy.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelAccuracy.ForeColor = Color.FromArgb(255, 152, 0);
            labelAccuracy.Location = new Point(15, 0);
            labelAccuracy.Name = "labelAccuracy";
            labelAccuracy.Size = new Size(150, 41);
            labelAccuracy.TabIndex = 1;
            labelAccuracy.Text = "📊 正确率: 0%";
            labelAccuracy.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // ActiveRecallForm
            // 
            BackColor = Color.FromArgb(250, 248, 245);
            ClientSize = new Size(584, 484);
            Controls.Add(panelMain);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Name = "ActiveRecallForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "\U0001f9e0 主动回忆训练";
            panelMain.ResumeLayout(false);
            panelMain.PerformLayout();
            panelStats.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        #region 界面更新

        /// <summary>
        /// 更新界面显示
        /// 
        /// 根据当前复习进度更新问题显示、进度条和控件状态。
        /// </summary>
        private void UpdateDisplay()
        {
            // 检查是否已完成所有复习
            if (_reviewQueue.Count == 0)
            {
                labelQuestion.Text = "🎉 恭喜！\n所有内容都已复习完成！";
                textBoxAnswer.Visible = false;
                buttonCheck.Visible = false;
                return;
            }

            // 显示当前复习项
            if (_currentIndex >= 0 && _currentIndex < _reviewQueue.Count)
            {
                var item = _reviewQueue[_currentIndex];
                labelQuestion.Text = item.Question;
                textBoxAnswer.Text = "";
                textBoxAnswer.Visible = true;
                buttonCheck.Visible = true;
                labelResult.Text = "";

                // 更新进度
                progressBar.Maximum = _reviewQueue.Count;
                progressBar.Value = _currentIndex + 1;
                labelProgress.Text = $"进度: {_currentIndex + 1}/{_reviewQueue.Count}";

                // 聚焦输入框
                textBoxAnswer.Focus();
            }
        }

        /// <summary>
        /// 更新统计信息
        /// 
        /// 更新正确数和正确率的显示。
        /// </summary>
        private void UpdateStats()
        {
            labelCorrect.Text = $"✓ 正确: {_correctCount}";

            if (_totalAttempts > 0)
            {
                int accuracy = (int)((_correctCount * 100.0) / _totalAttempts);
                labelAccuracy.Text = $"📊 正确率: {accuracy}%";
            }
            else
            {
                labelAccuracy.Text = "📊 正确率: --";
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 检查答案按钮点击事件处理
        /// </summary>
        /// <param name="sender">触发事件的按钮</param>
        /// <param name="e">事件参数</param>
        private void ButtonCheck_Click(object sender, EventArgs e)
        {
            CheckAnswer();
        }

        /// <summary>
        /// 答案输入框键盘事件处理
        /// 
        /// 支持回车键提交答案。
        /// </summary>
        /// <param name="sender">触发事件的文本框</param>
        /// <param name="e">键盘事件参数</param>
        private void TextBoxAnswer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CheckAnswer();
                e.Handled = true;
            }
        }

        /// <summary>
        /// 检查答案
        /// 
        /// 核心逻辑：
        /// 1. 获取用户答案
        /// 2. 使用智能匹配验证答案正确性
        /// 3. 更新统计和反馈
        /// 4. 延迟后进入下一题
        /// </summary>
        private void CheckAnswer()
        {
            // 边界检查
            if (_currentIndex >= _reviewQueue.Count)
                return;

            var item = _reviewQueue[_currentIndex];
            string userAnswer = textBoxAnswer.Text.Trim();

            // 增加答题次数
            _totalAttempts++;

            // 使用智能匹配验证答案
            bool isCorrect = CheckIfCorrect(userAnswer, item.Answer);

            if (isCorrect)
                {
                    // 答案正确
                    _correctCount++;
                    labelResult.Text = $"🎉 正确！\n答案：{item.Answer}";
                    labelResult.ForeColor = Color.Green;
                    textBoxAnswer.BackColor = Color.FromArgb(200, 255, 200);  // 绿色背景

                    // 更新复习计划（间隔重复）
                    item.NextReviewDate = DateTime.Now.AddDays(1);
                    item.CorrectStreak++;
                }
                else
                {
                    // 答案错误
                    labelResult.Text = $"❌ 再想想...\n提示：{item.Hint}\n答案：{item.Answer}";
                    labelResult.ForeColor = Color.Red;
                    textBoxAnswer.BackColor = Color.FromArgb(255, 230, 230);  // 红色背景

                    // 重置连续正确次数
                    item.CorrectStreak = 0;
                }

                // 发布复习完成事件
                if (_eventBus != null)
                {
                    _eventBus.Publish(new ReviewDoneEvent
                    {
                        UserId = _userId,
                        ItemId = item.Id.ToString(),
                        ItemContent = item.Content,
                        WasCorrect = isCorrect,
                        ReviewedAt = DateTime.Now
                    });
                }

            // 更新统计显示
            UpdateStats();

            // 延迟进入下一题（让用户有时间查看结果）
            if (_transitionTimer != null)
            {
                _transitionTimer.Stop();
                _transitionTimer.Dispose();
            }

            _transitionTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            _transitionTimer.Tick += (s, args) =>
            {
                _transitionTimer.Stop();

                // 移动到下一题
                _currentIndex++;
                UpdateDisplay();

                // 重置输入框颜色
                textBoxAnswer.BackColor = Color.White;

                // 检查是否完成所有复习
                if (_currentIndex >= _reviewQueue.Count)
                {
                    ShowCompletion();
                }
            };
            _transitionTimer.Start();
        }

        /// <summary>
        /// 检查答案是否正确
        /// 
        /// 使用 StringSimilarityHelper 进行智能匹配：
        /// - 完全匹配
        /// - 包含匹配
        /// - 相似度匹配（阈值 0.7）
        /// </summary>
        /// <param name="userAnswer">用户答案</param>
        /// <param name="correctAnswer">正确答案</param>
        /// <returns>答案是否正确</returns>
        private bool CheckIfCorrect(string userAnswer, string correctAnswer)
        {
            return StringSimilarityHelper.CheckAnswer(userAnswer, correctAnswer, 0.7);
        }

        /// <summary>
        /// 显示完成提示
        /// 
        /// 显示复习完成消息，包含：
        /// - 正确率统计
        /// - 鼓励信息
        /// - 记忆技巧提示
        /// </summary>
        private void ShowCompletion()
        {
            int accuracy = _totalAttempts > 0 ? (int)((_correctCount * 100.0) / _totalAttempts) : 0;

            string message = $"🎉 复习完成！\n\n" +
                           $"正确: {_correctCount}/{_totalAttempts}\n" +
                           $"正确率: {accuracy}%\n\n";

            // 根据正确率给出不同的鼓励信息
            if (accuracy >= 80)
            {
                message += "🌟 太棒了！掌握得很好！\n";
            }
            else if (accuracy >= 60)
            {
                message += "💪 还不错，继续加油！\n";
            }
            else
            {
                message += "📚 建议再复习一遍...\n";
            }

            // 添加记忆技巧提示
            message += "\n💡 记忆技巧：\n" +
                      "• 主动回忆比被动看答案记得更牢\n" +
                      "• 间隔复习效果最佳\n" +
                      "• 联想记忆法很有用";

            MessageBox.Show(message, "复习完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        #endregion
    }
}
