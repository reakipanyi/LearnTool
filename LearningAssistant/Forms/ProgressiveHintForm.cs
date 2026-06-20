using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LearningAssistant.Common;

namespace LearningAssistant.Forms
{
    /// <summary>
    /// 渐进式提示对话框
    /// 
    /// 核心功能：激发用户主动思考，避免直接查看答案，通过逐步提示引导用户回忆知识。
    /// 
    /// 设计原理：
    /// 1. 先让用户主动猜测答案，而非直接给出答案
    /// 2. 提供多级提示（由弱到强），逐步引导思考
    /// 3. 通过颜色区分提示强度（绿色最弱，红色最强）
    /// 4. 智能匹配用户答案（支持完全匹配、包含匹配、相似度匹配）
    /// 
    /// 使用场景：
    /// - 学习单词、汉字时的主动回忆训练
    /// - 知识点复习时的渐进式提示
    /// - 帮助用户建立主动思考习惯
    /// </summary>
    public partial class ProgressiveHintForm : Form
    {
        #region 常量定义

        /// <summary>
        /// 最大提示等级数
        /// </summary>
        private const int MaxHintLevels = 4;

        /// <summary>
        /// 答案相似度匹配阈值
        /// </summary>
        private const double SimilarityThreshold = 0.7;

        #endregion

        #region 字段定义

        /// <summary>
        /// 提示列表（最多4级，由弱到强）
        /// </summary>
        private readonly List<string> _hints;

        /// <summary>
        /// 当前提示等级（0表示未使用任何提示）
        /// </summary>
        private int _currentHintLevel = 0;

        /// <summary>
        /// 最大提示数量
        /// </summary>
        private readonly int _maxHints;

        /// <summary>
        /// 正确答案
        /// </summary>
        private readonly string _correctAnswer;

        /// <summary>
        /// 答案被查看时的回调函数
        /// </summary>
        private readonly Action<string>? _onAnswerRevealed;

        // UI 控件
        private Label labelTitle;
        private Label labelContent;
        private Panel panelHints;
        private FlowLayoutPanel hintButtonsPanel;
        private Button buttonRevealAnswer;
        private Label labelHintStatus;
        private TextBox textBoxGuess;
        private Button buttonSubmitGuess;
        private Label labelGuessResult;
        private Panel panelMain;
        private Label _labelHintContent;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化渐进式提示对话框
        /// </summary>
        /// <param name="question">问题内容（要学习的内容）</param>
        /// <param name="correctAnswer">正确答案</param>
        /// <param name="hints">提示列表（最多4条，按强度递增顺序）</param>
        /// <param name="onAnswerRevealed">答案被查看时的回调</param>
        public ProgressiveHintForm(string question, string correctAnswer, List<string> hints, Action<string>? onAnswerRevealed = null)
        {
            if (question == null) throw new ArgumentNullException(nameof(question));
            if (correctAnswer == null) throw new ArgumentNullException(nameof(correctAnswer));
            if (hints == null) throw new ArgumentNullException(nameof(hints));

            _correctAnswer = correctAnswer;
            _hints = hints;
            _maxHints = Math.Min(hints.Count, MaxHintLevels);
            _onAnswerRevealed = onAnswerRevealed;

            InitializeComponent();
            LoadHints();
            labelContent.Text = question;
        }

        #endregion

        #region UI 初始化

        /// <summary>
        /// 初始化窗体组件
        /// 
        /// 创建完整的渐进式提示界面，包括：
        /// - 标题区域
        /// - 问题显示区域
        /// - 用户猜测输入区域
        /// - 提示按钮区域（4个等级）
        /// - 提示内容显示区域
        /// - 答案揭示按钮
        /// </summary>
        private void InitializeComponent()
        {
            // 窗体基本设置
            this.Text = "💡 渐进式思考";
            this.Size = new Size(600, 540);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(250, 248, 245);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 主面板
            panelMain = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20)
            };
            this.Controls.Add(panelMain);

            // 标题
            labelTitle = new Label
            {
                Text = "🤔 先思考，再看提示",
                Font = new Font("微软雅黑", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(66, 133, 244),
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelMain.Controls.Add(labelTitle);

            // 问题内容标签
            Label labelQuestionTitle = new Label
            {
                Text = "📖 题目：",
                Font = new Font("微软雅黑", 11F, FontStyle.Bold),
                Location = new Point(20, 60),
                AutoSize = true
            };
            panelMain.Controls.Add(labelQuestionTitle);

            // 问题内容显示
            labelContent = new Label
            {
                Font = new Font("微软雅黑", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                Location = new Point(20, 90),
                Size = new Size(540, 60),
                TextAlign = ContentAlignment.TopLeft
            };
            panelMain.Controls.Add(labelContent);

            // 猜测输入区域标题
            Label labelGuess = new Label
            {
                Text = "✏️ 你的猜测：",
                Font = new Font("微软雅黑", 11F, FontStyle.Bold),
                Location = new Point(20, 160),
                AutoSize = true
            };
            panelMain.Controls.Add(labelGuess);

            // 用户猜测输入框
            textBoxGuess = new TextBox
            {
                Location = new Point(20, 190),
                Size = new Size(400, 35),
                Font = new Font("微软雅黑", 14F),
                PlaceholderText = "先自己想想，写下答案..."
            };
            panelMain.Controls.Add(textBoxGuess);

            // 提交猜测按钮
            buttonSubmitGuess = new Button
            {
                Text = "提交猜测",
                Location = new Point(430, 190),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(66, 133, 244),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold)
            };
            buttonSubmitGuess.Click += ButtonSubmitGuess_Click;
            panelMain.Controls.Add(buttonSubmitGuess);

            // 猜测结果显示
            labelGuessResult = new Label
            {
                Location = new Point(20, 230),
                Size = new Size(510, 30),
                Font = new Font("微软雅黑", 10F),
                ForeColor = Color.FromArgb(100, 100, 100)
            };
            panelMain.Controls.Add(labelGuessResult);

            // 提示按钮区域标题
            Label labelHintsTitle = new Label
            {
                Text = "💡 提示（由弱到强）：",
                Font = new Font("微软雅黑", 11F, FontStyle.Bold),
                Location = new Point(20, 270),
                AutoSize = true
            };
            panelMain.Controls.Add(labelHintsTitle);

            // 提示按钮面板
            hintButtonsPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 300),
                Size = new Size(540, 50),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
            };
            panelMain.Controls.Add(hintButtonsPanel);

            // 创建提示按钮
            CreateHintButtons();

            // 提示状态显示
            labelHintStatus = new Label
            {
                Text = $"提示等级：0/{_maxHints}",
                Location = new Point(20, 360),
                Size = new Size(540, 25),
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.FromArgb(120, 120, 120)
            };
            panelMain.Controls.Add(labelHintStatus);

            // 提示内容显示区域
            panelHints = new Panel
            {
                Location = new Point(20, 390),
                Size = new Size(540, 100),
                BackColor = Color.FromArgb(255, 253, 230),
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };

            _labelHintContent = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 10F),
                ForeColor = Color.FromArgb(80, 80, 80),
                Padding = new Padding(10),
                Text = "点击上方按钮查看提示"
            };
            panelHints.Controls.Add(_labelHintContent);
            panelMain.Controls.Add(panelHints);

            // 直接显示答案按钮
            buttonRevealAnswer = new Button
            {
                Text = "🤔 我还是想不出来...",
                Location = new Point(20, 460),
                Size = new Size(540, 35),
                BackColor = Color.FromArgb(255, 193, 7),  // 橙色
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold)
            };
            buttonRevealAnswer.Click += ButtonRevealAnswer_Click;
            panelMain.Controls.Add(buttonRevealAnswer);
        }

        /// <summary>
        /// 创建提示按钮
        /// 
        /// 按钮按颜色区分强度：
        /// - 绿色（最弱提示）
        /// - 蓝色（较弱提示）
        /// - 橙色（中等提示）
        /// - 红色（强提示）
        /// 
        /// 按钮初始状态为禁用，需要按顺序解锁。
        /// </summary>
        private void CreateHintButtons()
        {
            string[] buttonTexts = { "提示1", "提示2", "提示3", "提示4" };
            Color[] buttonColors = {
                Color.FromArgb(76, 175, 80),   // 绿色 - 最弱提示
                Color.FromArgb(33, 150, 243),  // 蓝色 - 较弱提示
                Color.FromArgb(255, 152, 0),   // 橙色 - 中等提示
                Color.FromArgb(244, 67, 54)    // 红色 - 强提示
            };

            for (int i = 0; i < _maxHints; i++)
            {
                Button hintButton = new Button
                {
                    Text = $"💡 {buttonTexts[i]}",
                    Size = new Size(128, 40),
                    Margin = new Padding(0, 0, 4, 0),
                    BackColor = buttonColors[i],
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                    Tag = i,           // 存储提示等级
                    Enabled = false    // 初始禁用
                };
                hintButton.Click += HintButton_Click;
                hintButtonsPanel.Controls.Add(hintButton);
            }
        }

        /// <summary>
        /// 加载提示（启用第一个提示按钮）
        /// </summary>
        private void LoadHints()
        {
            if (hintButtonsPanel.Controls.Count > 0)
            {
                hintButtonsPanel.Controls[0].Enabled = true;
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 提示按钮点击事件处理
        /// 
        /// 点击提示按钮后：
        /// 1. 显示当前等级的提示
        /// 2. 解锁下一级提示按钮
        /// 3. 更新提示状态显示
        /// </summary>
        /// <param name="sender">触发事件的按钮</param>
        /// <param name="e">事件参数</param>
        private void HintButton_Click(object sender, EventArgs e)
        {
            if (sender is Button button && button.Tag is int level)
            {
                // 验证提示等级有效性
                if (level < _hints.Count)
                {
                    // 解锁下一个提示按钮
                    if (level + 1 < hintButtonsPanel.Controls.Count)
                    {
                        hintButtonsPanel.Controls[level + 1].Enabled = true;
                    }

                    // 更新当前提示等级
                    _currentHintLevel = level + 1;
                    labelHintStatus.Text = $"提示等级：{_currentHintLevel}/{_maxHints}";

                    // 显示所有已解锁的提示内容
                    string hintContent = "";
                    for (int i = 0; i <= level; i++)
                    {
                        hintContent += $"提示{i + 1}：{_hints[i]}\n";
                    }
                    _labelHintContent.Text = hintContent;
                }
            }
        }

        /// <summary>
        /// 提交猜测按钮点击事件处理
        /// 
        /// 验证用户猜测的答案是否正确：
        /// 1. 检查输入是否为空
        /// 2. 使用智能匹配验证答案（完全匹配、包含匹配、相似度匹配）
        /// 3. 根据结果显示反馈信息
        /// 4. 正确时启用更多提示按钮，错误时自动解锁下一个提示
        /// </summary>
        /// <param name="sender">触发事件的按钮</param>
        /// <param name="e">事件参数</param>
        private void ButtonSubmitGuess_Click(object sender, EventArgs e)
        {
            string guess = textBoxGuess.Text.Trim();

            // 检查输入是否为空
            if (string.IsNullOrEmpty(guess))
            {
                labelGuessResult.Text = "⚠️ 请先输入你的猜测";
                labelGuessResult.ForeColor = Color.Orange;
                return;
            }

            // 使用智能匹配验证答案正确性
            bool isCorrect = StringSimilarityHelper.CheckAnswer(guess, _correctAnswer, SimilarityThreshold);

            if (isCorrect)
            {
                // 答案正确
                labelGuessResult.Text = "🎉 太棒了！你答对了！";
                labelGuessResult.ForeColor = Color.Green;
                textBoxGuess.BackColor = Color.FromArgb(200, 255, 200);  // 绿色背景

                // 启用所有提示按钮
                for (int i = 0; i < hintButtonsPanel.Controls.Count; i++)
                {
                    hintButtonsPanel.Controls[i].Enabled = true;
                }
                _currentHintLevel = _maxHints;
                labelHintStatus.Text = $"提示等级：{_currentHintLevel}/{_maxHints}";
                labelHintStatus.ForeColor = Color.FromArgb(120, 120, 120);
            }
            else
            {
                // 答案错误
                labelGuessResult.Text = "❌ 不对哦，再想想... （提示：先思考，不要急着看答案）";
                labelGuessResult.ForeColor = Color.Red;
                textBoxGuess.BackColor = Color.FromArgb(255, 230, 230);  // 红色背景

                // 自动启用下一个提示按钮
                if (_currentHintLevel < _maxHints && _currentHintLevel < hintButtonsPanel.Controls.Count)
                {
                    hintButtonsPanel.Controls[_currentHintLevel].Enabled = true;
                    labelHintStatus.Text = $"💡 建议：尝试查看下一个提示";
                    labelHintStatus.ForeColor = Color.Orange;
                }
            }
        }

        /// <summary>
        /// 揭示答案按钮点击事件处理
        /// 
        /// 用户点击此按钮后：
        /// 1. 弹出确认对话框，提醒用户尽量自己思考
        /// 2. 如果确认，显示正确答案
        /// 3. 触发回调函数（如果设置）
        /// 4. 关闭对话框
        /// </summary>
        /// <param name="sender">触发事件的按钮</param>
        /// <param name="e">事件参数</param>
        private void ButtonRevealAnswer_Click(object sender, EventArgs e)
        {
            // 弹出确认对话框（不包含答案内容）
            DialogResult result = MessageBox.Show(
                "确定要查看答案吗？\n\n💡 建议：先努力回忆，实在想不出来再看答案，这样记忆更深刻！",
                "确认查看答案",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                // 触发回调（记录用户查看答案的行为）
                _onAnswerRevealed?.Invoke(_correctAnswer);

                // 显示答案
                MessageBox.Show($"答案是：{_correctAnswer}\n\n📚 记住这个知识点，下次遇到类似的要能举一反三！",
                    "答案揭晓",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // 关闭对话框
                this.Close();
            }
        }

        #endregion
    }
}
