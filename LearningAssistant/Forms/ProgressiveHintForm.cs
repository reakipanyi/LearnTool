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
    ///
    /// UI/UX 优化规范：
    /// - 正误判定反馈：接近→绿色边框；正确→全屏绿色闪动；错误→红色边框+自动展开提示1
    /// - 提示按钮状态：已查看→灰色+✓；当前可查看→主色高亮；不可查看→🔒
    /// - 圆点步进器替代文字提示等级
    /// </summary>
    public partial class ProgressiveHintForm : Form
    {
        #region 常量定义

        /// <summary>
        /// 最大提示等级数
        /// </summary>
        private const int MaxHintLevels = 4;

        /// <summary>
        /// 答案相似度匹配阈值（完全匹配）
        /// </summary>
        private const double SimilarityThresholdExact = 0.9;

        /// <summary>
        /// 答案相似度匹配阈值（接近）
        /// </summary>
        private const double SimilarityThresholdClose = 0.6;

        /// <summary>
        /// 反馈动画持续时间（毫秒）
        /// </summary>
        private const int FeedbackAnimationDuration = 300;

        /// <summary>
        /// 正确反馈持续时间（毫秒）
        /// </summary>
        private const int CorrectFeedbackDuration = 1000;

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
        /// 已查看的提示等级集合
        /// </summary>
        private readonly HashSet<int> _viewedHints = new();

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
        private Panel _panelStepIndicator;
        private Label _labelStepIndicator;

        // 反馈相关
        private System.Windows.Forms.Timer? _feedbackTimer;
        private Color _originalTextBoxBackColor;
        private Color _originalGuessResultColor;

        #endregion

        #region 属性

        /// <summary>
        /// 当前提示等级
        /// </summary>
        public int CurrentHintLevel => _currentHintLevel;

        /// <summary>
        /// 已查看的提示等级
        /// </summary>
        public IReadOnlyCollection<int> ViewedHints => _viewedHints;

        /// <summary>
        /// 用户的猜测内容
        /// </summary>
        public string UserGuess => textBoxGuess?.Text ?? string.Empty;

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

            _originalTextBoxBackColor = textBoxGuess.BackColor;
            _originalGuessResultColor = labelGuessResult.ForeColor;
        }

        /// <summary>
        /// 恢复进度构造函数（用于状态持久化）
        /// </summary>
        public ProgressiveHintForm(string question, string correctAnswer, List<string> hints,
            string savedGuess, int currentHintLevel, HashSet<int> viewedHints,
            Action<string>? onAnswerRevealed = null)
            : this(question, correctAnswer, hints, onAnswerRevealed)
        {
            if (!string.IsNullOrEmpty(savedGuess))
            {
                textBoxGuess.Text = savedGuess;
            }

            _currentHintLevel = currentHintLevel;
            _viewedHints.UnionWith(viewedHints);

            RestoreHintState();
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
        /// - 圆点步进器（替代文字提示等级）
        /// - 提示按钮区域（4个等级，带状态视觉反馈）
        /// - 提示内容显示区域
        /// - 答案揭示按钮
        /// </summary>
        private void InitializeComponent()
        {
            // 窗体基本设置
            this.Text = "💡 渐进式思考";
            this.Size = new Size(600, 580);
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
                ForeColor = Color.FromArgb(108, 92, 231), // 品牌紫色
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
                PlaceholderText = "先自己想想，写下答案...",
                BorderStyle = BorderStyle.FixedSingle
            };
            panelMain.Controls.Add(textBoxGuess);

            // 提交猜测按钮
            buttonSubmitGuess = new Button
            {
                Text = "提交猜测",
                Location = new Point(430, 190),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(108, 92, 231), // 品牌紫色
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            buttonSubmitGuess.FlatAppearance.BorderSize = 0;
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

            // 圆点步进器指示器
            _panelStepIndicator = new Panel
            {
                Location = new Point(20, 265),
                Size = new Size(200, 20)
            };

            _labelStepIndicator = new Label
            {
                Dock = DockStyle.Left,
                Text = "●○○○",
                Font = new Font("Microsoft YaHei", 12F),
                ForeColor = Color.FromArgb(108, 92, 231),
                AutoSize = true
            };
            _panelStepIndicator.Controls.Add(_labelStepIndicator);
            panelMain.Controls.Add(_panelStepIndicator);

            // 提示按钮区域标题
            Label labelHintsTitle = new Label
            {
                Text = "💡 提示（由弱到强）：",
                Font = new Font("微软雅黑", 11F, FontStyle.Bold),
                Location = new Point(20, 295),
                AutoSize = true
            };
            panelMain.Controls.Add(labelHintsTitle);

            // 提示按钮面板
            hintButtonsPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 325),
                Size = new Size(540, 50),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
            };
            panelMain.Controls.Add(hintButtonsPanel);

            // 创建提示按钮
            CreateHintButtons();

            // 提示状态显示（辅助说明）
            labelHintStatus = new Label
            {
                Text = $"提示等级：0/{_maxHints}",
                Location = new Point(20, 380),
                Size = new Size(540, 20),
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.FromArgb(120, 120, 120)
            };
            panelMain.Controls.Add(labelHintStatus);

            // 提示内容显示区域
            panelHints = new Panel
            {
                Location = new Point(20, 405),
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
                Location = new Point(20, 510),
                Size = new Size(540, 35),
                BackColor = Color.FromArgb(255, 193, 7),  // 橙色
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            buttonRevealAnswer.FlatAppearance.BorderSize = 0;
            buttonRevealAnswer.Click += ButtonRevealAnswer_Click;
            panelMain.Controls.Add(buttonRevealAnswer);
        }

        /// <summary>
        /// 创建提示按钮
        ///
        /// 按钮状态（UI/UX优化规范）：
        /// - 已查看：灰色背景 + ✓ 对勾标记
        /// - 当前可查看：对应等级颜色高亮
        /// - 不可查看：🔒 锁头标记 + 灰色
        ///
        /// 按钮按颜色区分强度：
        /// - 绿色（最弱提示）
        /// - 蓝色（较弱提示）
        /// - 橙色（中等提示）
        /// - 红色（强提示）
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
                int level = i;
                Button hintButton = new Button
                {
                    Text = $"💡 {buttonTexts[i]}", // 初始状态，待更新
                    Size = new Size(128, 40),
                    Margin = new Padding(0, 0, 4, 0),
                    BackColor = Color.FromArgb(200, 200, 200), // 初始灰色（锁定状态）
                    ForeColor = Color.Gray,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                    Tag = level,
                    Enabled = false,
                    Cursor = Cursors.Hand
                };
                hintButton.FlatAppearance.BorderSize = 0;
                hintButton.Click += HintButton_Click;
                hintButtonsPanel.Controls.Add(hintButton);

                // 更新按钮状态显示
                UpdateHintButtonAppearance(hintButton, level, false);
            }
        }

        /// <summary>
        /// 更新提示按钮的视觉状态
        /// </summary>
        /// <param name="button">按钮控件</param>
        /// <param name="level">提示等级</param>
        /// <param name="isViewed">是否已查看</param>
        private void UpdateHintButtonAppearance(Button button, int level, bool isViewed)
        {
            Color[] buttonColors = {
                Color.FromArgb(76, 175, 80),   // 绿色
                Color.FromArgb(33, 150, 243),  // 蓝色
                Color.FromArgb(255, 152, 0),   // 橙色
                Color.FromArgb(244, 67, 54)    // 红色
            };

            if (isViewed)
            {
                // 已查看状态：灰色 + ✓
                button.BackColor = Color.FromArgb(180, 180, 180);
                button.ForeColor = Color.White;
                button.Text = $"✓ 提示{level + 1}";
            }
            else
            {
                // 锁定状态：🔒 + 灰色
                button.BackColor = Color.FromArgb(220, 220, 220);
                button.ForeColor = Color.Gray;
                button.Text = $"🔒 提示{level + 1}";
            }
        }

        /// <summary>
        /// 更新单个提示按钮为可查看状态
        /// </summary>
        private void UnlockHintButton(int level)
        {
            if (level < 0 || level >= hintButtonsPanel.Controls.Count)
                return;

            var button = hintButtonsPanel.Controls[level] as Button;
            if (button == null) return;

            Color[] buttonColors = {
                Color.FromArgb(76, 175, 80),
                Color.FromArgb(33, 150, 243),
                Color.FromArgb(255, 152, 0),
                Color.FromArgb(244, 67, 54)
            };

            button.Enabled = true;
            button.BackColor = buttonColors[level];
            button.ForeColor = Color.White;
            button.Text = $"💡 提示{level + 1}";
        }

        /// <summary>
        /// 更新圆点步进器显示
        /// </summary>
        private void UpdateStepIndicator()
        {
            string indicator = "";
            for (int i = 0; i < _maxHints; i++)
            {
                if (i < _currentHintLevel)
                {
                    indicator += "●"; // 已使用
                }
                else
                {
                    indicator += "○"; // 未使用
                }
            }
            _labelStepIndicator.Text = indicator;
        }

        /// <summary>
        /// 恢复提示状态（用于状态持久化）
        /// </summary>
        private void RestoreHintState()
        {
            // 恢复已查看的提示
            foreach (var level in _viewedHints)
            {
                if (level < hintButtonsPanel.Controls.Count)
                {
                    var button = hintButtonsPanel.Controls[level] as Button;
                    if (button != null)
                    {
                        button.Enabled = true;
                        UpdateHintButtonAppearance(button, level, true);
                    }
                }
            }

            // 解锁当前可查看的下一个提示
            if (_currentHintLevel < _maxHints)
            {
                UnlockHintButton(_currentHintLevel);
            }

            // 恢复提示内容显示
            if (_viewedHints.Count > 0)
            {
                string hintContent = "";
                foreach (var level in _viewedHints.OrderBy(x => x))
                {
                    if (level < _hints.Count)
                    {
                        hintContent += $"提示{level + 1}：{_hints[level]}\n";
                    }
                }
                _labelHintContent.Text = hintContent;
            }

            UpdateStepIndicator();
            labelHintStatus.Text = $"提示等级：{_currentHintLevel}/{_maxHints}";
        }

        /// <summary>
        /// 加载提示（启用第一个提示按钮）
        /// </summary>
        private void LoadHints()
        {
            if (hintButtonsPanel.Controls.Count > 0)
            {
                UnlockHintButton(0);
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 提示按钮点击事件处理
        ///
        /// 点击提示按钮后：
        /// 1. 标记为已查看状态（灰色+✓）
        /// 2. 显示当前等级的提示
        /// 3. 解锁下一级提示按钮
        /// 4. 更新圆点步进器和状态显示
        /// </summary>
        private void HintButton_Click(object sender, EventArgs e)
        {
            if (sender is Button button && button.Tag is int level)
            {
                // 验证提示等级有效性
                if (level < _hints.Count)
                {
                    // 标记为已查看
                    _viewedHints.Add(level);
                    UpdateHintButtonAppearance(button, level, true);

                    // 解锁下一个提示按钮
                    if (level + 1 < hintButtonsPanel.Controls.Count)
                    {
                        UnlockHintButton(level + 1);
                    }

                    // 更新当前提示等级
                    _currentHintLevel = level + 1;
                    UpdateStepIndicator();
                    labelHintStatus.Text = $"提示等级：{_currentHintLevel}/{_maxHints}";

                    // 显示所有已解锁的提示内容
                    string hintContent = "";
                    foreach (var viewedLevel in _viewedHints.OrderBy(x => x))
                    {
                        if (viewedLevel < _hints.Count)
                        {
                            hintContent += $"提示{viewedLevel + 1}：{_hints[viewedLevel]}\n";
                        }
                    }
                    _labelHintContent.Text = hintContent;
                }
            }
        }

        /// <summary>
        /// 提交猜测按钮点击事件处理
        ///
        /// 验证用户猜测的答案是否正确（UI/UX优化规范）：
        /// 1. 检查输入是否为空
        /// 2. 使用智能匹配验证答案：
        ///    - 完全正确 → 🎉 全屏绿色闪动 + "太棒了！继续下一题"
        ///    - 接近 → 绿色边框闪烁 + "接近！看看提示2再做补充"
        ///    - 错误 → 红色边框闪烁 + 自动展开提示1
        /// 3. 根据结果显示反馈信息
        /// 4. 正确时启用所有提示，错误时自动解锁下一个提示
        /// </summary>
        private void ButtonSubmitGuess_Click(object sender, EventArgs e)
        {
            string guess = textBoxGuess.Text.Trim();

            // 检查输入是否为空
            if (string.IsNullOrEmpty(guess))
            {
                ShowGuessFeedback("⚠️ 请先输入你的猜测", FeedbackType.Warning);
                return;
            }

            // 使用智能匹配验证答案正确性
            var matchResult = EvaluateAnswer(guess);

            switch (matchResult)
            {
                case AnswerMatchResult.Correct:
                    ShowCorrectFeedback();
                    break;

                case AnswerMatchResult.Close:
                    ShowCloseFeedback();
                    break;

                case AnswerMatchResult.Wrong:
                    ShowWrongFeedback();
                    break;
            }
        }

        /// <summary>
        /// 评估答案匹配结果
        /// </summary>
        private AnswerMatchResult EvaluateAnswer(string guess)
        {
            // 完全匹配检查
            if (StringSimilarityHelper.CheckAnswer(guess, _correctAnswer, SimilarityThresholdExact))
            {
                return AnswerMatchResult.Correct;
            }

            // 接近匹配检查
            if (StringSimilarityHelper.CheckAnswer(guess, _correctAnswer, SimilarityThresholdClose))
            {
                return AnswerMatchResult.Close;
            }

            return AnswerMatchResult.Wrong;
        }

        /// <summary>
        /// 显示正确反馈 - 全屏绿色闪动
        /// </summary>
        private void ShowCorrectFeedback()
        {
            // 全屏绿色效果
            var overlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(50, 0, 184, 148), // 绿色半透明
                Tag = "feedback_overlay"
            };
            this.Controls.Add(overlay);
            overlay.BringToFront();

            // 停止输入
            textBoxGuess.Enabled = false;
            buttonSubmitGuess.Enabled = false;

            // 显示反馈文字
            ShowGuessFeedback("🎉 太棒了！你答对了！", FeedbackType.Correct);

            // 启用所有提示按钮
            for (int i = 0; i < hintButtonsPanel.Controls.Count; i++)
            {
                var btn = hintButtonsPanel.Controls[i] as Button;
                if (btn != null)
                {
                    btn.Enabled = true;
                    UpdateHintButtonAppearance(btn, i, true);
                }
            }
            _currentHintLevel = _maxHints;
            UpdateStepIndicator();
            labelHintStatus.Text = $"提示等级：{_currentHintLevel}/{_maxHints}";

            // 延迟移除覆盖层并关闭
            _feedbackTimer = new System.Windows.Forms.Timer { Interval = CorrectFeedbackDuration };
            _feedbackTimer.Tick += (s, args) =>
            {
                _feedbackTimer.Stop();
                _feedbackTimer.Dispose();
                _feedbackTimer = null;

                // 移除覆盖层
                var overlayToRemove = this.Controls.OfType<Panel>().FirstOrDefault(p => p.Tag?.ToString() == "feedback_overlay");
                overlayToRemove?.Dispose();

                // 恢复输入
                textBoxGuess.Enabled = true;
                buttonSubmitGuess.Enabled = true;

                // 提示继续下一题
                MessageBox.Show("🎉 太棒了！继续下一题吧！", "回答正确", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            };
            _feedbackTimer.Start();
        }

        /// <summary>
        /// 显示接近反馈 - 绿色边框闪烁
        /// </summary>
        private void ShowCloseFeedback()
        {
            // 绿色边框闪烁效果
            StartBorderBlink(ThemeHelper.SuccessColors.Main);

            // 显示接近提示
            ShowGuessFeedback("接近！✨ 想想提示，再做补充", FeedbackType.Close);

            // 自动展开下一个提示
            if (_currentHintLevel < _maxHints && _currentHintLevel < hintButtonsPanel.Controls.Count)
            {
                UnlockHintButton(_currentHintLevel);
            }
        }

        /// <summary>
        /// 显示错误反馈 - 红色边框闪烁 + 自动展开提示1
        /// </summary>
        private void ShowWrongFeedback()
        {
            // 红色边框闪烁效果
            StartBorderBlink(ThemeHelper.DangerColors.Main);

            // 显示错误提示
            ShowGuessFeedback("❌ 不对哦，再想想...", FeedbackType.Wrong);

            // 自动解锁并展开提示1（如果尚未查看）
            if (!_viewedHints.Contains(0) && hintButtonsPanel.Controls.Count > 0)
            {
                HintButton_Click(hintButtonsPanel.Controls[0], EventArgs.Empty);
                ShowGuessFeedback("💡 建议：尝试查看提示1", FeedbackType.Hint);
            }
        }

        /// <summary>
        /// 显示猜测结果反馈
        /// </summary>
        private void ShowGuessFeedback(string message, FeedbackType type)
        {
            labelGuessResult.Text = message;

            switch (type)
            {
                case FeedbackType.Correct:
                    labelGuessResult.ForeColor = ThemeHelper.SuccessColors.Main;
                    textBoxGuess.BackColor = Color.FromArgb(200, 255, 240);
                    break;

                case FeedbackType.Close:
                    labelGuessResult.ForeColor = ThemeHelper.SuccessColors.Main;
                    textBoxGuess.BackColor = Color.FromArgb(255, 250, 220);
                    break;

                case FeedbackType.Wrong:
                    labelGuessResult.ForeColor = ThemeHelper.DangerColors.Main;
                    textBoxGuess.BackColor = Color.FromArgb(255, 240, 240);
                    break;

                case FeedbackType.Hint:
                    labelGuessResult.ForeColor = ThemeHelper.WarningColors.Main;
                    break;

                case FeedbackType.Warning:
                    labelGuessResult.ForeColor = ThemeHelper.WarningColors.Main;
                    textBoxGuess.BackColor = _originalTextBoxBackColor;
                    break;

                default:
                    labelGuessResult.ForeColor = _originalGuessResultColor;
                    textBoxGuess.BackColor = _originalTextBoxBackColor;
                    break;
            }

            // 延迟恢复原始颜色
            if (type != FeedbackType.Correct && type != FeedbackType.Close && type != FeedbackType.Wrong)
            {
                _feedbackTimer?.Stop();
                _feedbackTimer?.Dispose();
                _feedbackTimer = new System.Windows.Forms.Timer { Interval = FeedbackAnimationDuration * 2 };
                _feedbackTimer.Tick += (s, args) =>
                {
                    _feedbackTimer.Stop();
                    _feedbackTimer.Dispose();
                    _feedbackTimer = null;
                    labelGuessResult.ForeColor = _originalGuessResultColor;
                    textBoxGuess.BackColor = _originalTextBoxBackColor;
                };
                _feedbackTimer.Start();
            }
        }

        /// <summary>
        /// 开始边框闪烁效果
        /// </summary>
        private void StartBorderBlink(Color blinkColor)
        {
            _feedbackTimer?.Stop();
            _feedbackTimer?.Dispose();

            int blinkCount = 3;
            var originalBorderColor = textBoxGuess.BorderStyle;

            _feedbackTimer = new System.Windows.Forms.Timer { Interval = FeedbackAnimationDuration };
            _feedbackTimer.Tick += (s, args) =>
            {
                blinkCount--;
                if (blinkCount <= 0)
                {
                    _feedbackTimer.Stop();
                    _feedbackTimer.Dispose();
                    _feedbackTimer = null;
                    textBoxGuess.BackColor = blinkColor == ThemeHelper.DangerColors.Main
                        ? Color.FromArgb(255, 240, 240)
                        : Color.FromArgb(255, 250, 220);
                }
                else
                {
                    textBoxGuess.BackColor = blinkCount % 2 == 0
                        ? blinkColor
                        : (blinkColor == ThemeHelper.DangerColors.Main
                            ? Color.FromArgb(255, 240, 240)
                            : Color.FromArgb(255, 250, 220));
                }
            };
            _feedbackTimer.Start();
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

        #region 内部枚举

        private enum AnswerMatchResult
        {
            Correct,
            Close,
            Wrong
        }

        private enum FeedbackType
        {
            Normal,
            Correct,
            Close,
            Wrong,
            Hint,
            Warning
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取当前进度（用于状态持久化）
        /// </summary>
        public (string guess, int currentLevel, HashSet<int> viewedLevels) GetProgress()
        {
            return (textBoxGuess.Text, _currentHintLevel, new HashSet<int>(_viewedHints));
        }

        #endregion

        #region 资源释放

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _feedbackTimer?.Stop();
                _feedbackTimer?.Dispose();
                _feedbackTimer = null;
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
