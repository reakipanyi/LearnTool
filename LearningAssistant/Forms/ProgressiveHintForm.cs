using LearningAssistant.Common;
using System.ComponentModel;

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
        /// <summary>最大提示等级数</summary>
        private const int MaxHintLevels = 4;
        /// <summary>答案相似度匹配阈值（完全匹配）</summary>
        private const double SimilarityThresholdExact = 0.9;
        /// <summary>答案相似度匹配阈值（接近）</summary>
        private const double SimilarityThresholdClose = 0.6;
        /// <summary>反馈动画持续时间（毫秒）</summary>
        private const int FeedbackAnimationDuration = 300;
        /// <summary>正确反馈持续时间（毫秒）</summary>
        private const int CorrectFeedbackDuration = 1000;
        #endregion

        #region 业务字段
        private readonly List<string> _hints;
        private int _currentHintLevel = 0;
        private readonly HashSet<int> _viewedHints = new();
        private readonly int _maxHints;
        private readonly string _correctAnswer;
        private readonly Action<string>? _onAnswerRevealed;
        private System.Windows.Forms.Timer? _feedbackTimer;
        private Color _originalTextBoxBackColor;
        private Color _originalGuessResultColor;
        #endregion

        #region 复用字体（统一Dispose释放，消除GDI泄漏）
        private readonly Font _fontTitleLarge = new Font("微软雅黑", 14F, FontStyle.Bold);
        private readonly Font _fontSubTitle = new Font("微软雅黑", 11F, FontStyle.Bold);
        private readonly Font _fontQuestion = new Font("微软雅黑", 18F, FontStyle.Bold);
        private readonly Font _fontInput = new Font("微软雅黑", 14F);
        private readonly Font _fontBtnBold = new Font("微软雅黑", 10F, FontStyle.Bold);
        private readonly Font _fontNormal = new Font("微软雅黑", 10F);
        private readonly Font _fontSmallTip = new Font("微软雅黑", 9F);
        private readonly Font _fontStepDot = new Font("Microsoft YaHei", 12F);
        private readonly Font _fontHintBtn = new Font("微软雅黑", 9F, FontStyle.Bold);
        #endregion

        #region UI控件字段（全部提升为类字段，无局部var）
        private Panel panelMain;
        private Label labelTitle;
        private Label labelQuestionTitle;
        private Label labelContent;
        private Label labelGuess;
        private TextBox textBoxGuess;
        private Button buttonSubmitGuess;
        private Label labelGuessResult;
        private Panel _panelStepIndicator;
        private Label _labelStepIndicator;
        private Label labelHintsTitle;
        private FlowLayoutPanel hintButtonsPanel;
        private Label labelHintStatus;
        private Panel panelHints;
        private Label _labelHintContent;
        private Button buttonRevealAnswer;
        #endregion

        #region 属性
        public int CurrentHintLevel => _currentHintLevel;
        public IReadOnlyCollection<int> ViewedHints => _viewedHints;
        public string UserGuess => textBoxGuess?.Text ?? string.Empty;
        #endregion

        #region 构造函数
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

            labelHintStatus.Text = $"提示等级：0/{_maxHints}";
            _originalTextBoxBackColor = textBoxGuess.BackColor;
            _originalGuessResultColor = labelGuessResult.ForeColor;
        }

        public ProgressiveHintForm(string question, string correctAnswer, List<string> hints,
            string savedGuess, int currentHintLevel, HashSet<int> viewedHints,
            Action<string>? onAnswerRevealed = null)
            : this(question, correctAnswer, hints, onAnswerRevealed)
        {
            if (!string.IsNullOrEmpty(savedGuess))
                textBoxGuess.Text = savedGuess;

            _currentHintLevel = currentHintLevel;
            _viewedHints.UnionWith(viewedHints);
            RestoreHintState();
        }
        #endregion

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            panelMain = new Panel();
            buttonRevealAnswer = new Button();
            panelHints = new Panel();
            _labelHintContent = new Label();
            labelHintStatus = new Label();
            hintButtonsPanel = new FlowLayoutPanel();
            labelHintsTitle = new Label();
            _panelStepIndicator = new Panel();
            _labelStepIndicator = new Label();
            labelGuessResult = new Label();
            buttonSubmitGuess = new Button();
            textBoxGuess = new TextBox();
            labelGuess = new Label();
            labelContent = new Label();
            labelQuestionTitle = new Label();
            labelTitle = new Label();
            panelMain.SuspendLayout();
            panelHints.SuspendLayout();
            _panelStepIndicator.SuspendLayout();
            SuspendLayout();
            // 
            // panelMain
            // 
            panelMain.BackColor = Color.White;
            panelMain.Controls.Add(buttonRevealAnswer);
            panelMain.Controls.Add(panelHints);
            panelMain.Controls.Add(labelHintStatus);
            panelMain.Controls.Add(hintButtonsPanel);
            panelMain.Controls.Add(labelHintsTitle);
            panelMain.Controls.Add(_panelStepIndicator);
            panelMain.Controls.Add(labelGuessResult);
            panelMain.Controls.Add(buttonSubmitGuess);
            panelMain.Controls.Add(textBoxGuess);
            panelMain.Controls.Add(labelGuess);
            panelMain.Controls.Add(labelContent);
            panelMain.Controls.Add(labelQuestionTitle);
            panelMain.Controls.Add(labelTitle);
            panelMain.Dock = DockStyle.Fill;
            panelMain.Location = new Point(0, 0);
            panelMain.Name = "panelMain";
            panelMain.Padding = new Padding(20);
            panelMain.Size = new Size(584, 541);
            panelMain.TabIndex = 0;
            // 
            // buttonRevealAnswer
            // 
            buttonRevealAnswer.BackColor = Color.FromArgb(255, 193, 7);
            buttonRevealAnswer.Cursor = Cursors.Hand;
            buttonRevealAnswer.FlatAppearance.BorderSize = 0;
            buttonRevealAnswer.FlatStyle = FlatStyle.Flat;
            buttonRevealAnswer.ForeColor = Color.White;
            buttonRevealAnswer.Location = new Point(20, 510);
            buttonRevealAnswer.Name = "buttonRevealAnswer";
            buttonRevealAnswer.Size = new Size(540, 35);
            buttonRevealAnswer.TabIndex = 12;
            buttonRevealAnswer.Text = "🤔 我还是想不出来...";
            buttonRevealAnswer.UseVisualStyleBackColor = false;
            buttonRevealAnswer.Click += ButtonRevealAnswer_Click;
            // 
            // panelHints
            // 
            panelHints.AutoScroll = true;
            panelHints.BackColor = Color.FromArgb(255, 253, 230);
            panelHints.BorderStyle = BorderStyle.FixedSingle;
            panelHints.Controls.Add(_labelHintContent);
            panelHints.Location = new Point(20, 405);
            panelHints.Name = "panelHints";
            panelHints.Size = new Size(540, 100);
            panelHints.TabIndex = 11;
            // 
            // _labelHintContent
            // 
            _labelHintContent.Dock = DockStyle.Fill;
            _labelHintContent.ForeColor = Color.FromArgb(80, 80, 80);
            _labelHintContent.Location = new Point(0, 0);
            _labelHintContent.Name = "_labelHintContent";
            _labelHintContent.Padding = new Padding(10);
            _labelHintContent.Size = new Size(538, 98);
            _labelHintContent.TabIndex = 0;
            _labelHintContent.Text = "点击上方按钮查看提示";
            // 
            // labelHintStatus
            // 
            labelHintStatus.ForeColor = Color.FromArgb(120, 120, 120);
            labelHintStatus.Location = new Point(20, 380);
            labelHintStatus.Name = "labelHintStatus";
            labelHintStatus.Size = new Size(540, 20);
            labelHintStatus.TabIndex = 10;
            labelHintStatus.Text = "提示等级：0/0";
            // 
            // hintButtonsPanel
            // 
            hintButtonsPanel.AutoScroll = true;
            hintButtonsPanel.Location = new Point(20, 325);
            hintButtonsPanel.Name = "hintButtonsPanel";
            hintButtonsPanel.Size = new Size(540, 50);
            hintButtonsPanel.TabIndex = 9;
            hintButtonsPanel.WrapContents = false;
            // 
            // labelHintsTitle
            // 
            labelHintsTitle.AutoSize = true;
            labelHintsTitle.Location = new Point(20, 295);
            labelHintsTitle.Name = "labelHintsTitle";
            labelHintsTitle.Size = new Size(132, 17);
            labelHintsTitle.TabIndex = 8;
            labelHintsTitle.Text = "💡 提示（由弱到强）：";
            // 
            // _panelStepIndicator
            // 
            _panelStepIndicator.Controls.Add(_labelStepIndicator);
            _panelStepIndicator.Location = new Point(20, 265);
            _panelStepIndicator.Name = "_panelStepIndicator";
            _panelStepIndicator.Size = new Size(200, 20);
            _panelStepIndicator.TabIndex = 7;
            // 
            // _labelStepIndicator
            // 
            _labelStepIndicator.AutoSize = true;
            _labelStepIndicator.Dock = DockStyle.Left;
            _labelStepIndicator.ForeColor = Color.FromArgb(108, 92, 231);
            _labelStepIndicator.Location = new Point(0, 0);
            _labelStepIndicator.Name = "_labelStepIndicator";
            _labelStepIndicator.Size = new Size(37, 17);
            _labelStepIndicator.TabIndex = 0;
            _labelStepIndicator.Text = "●○○○";
            // 
            // labelGuessResult
            // 
            labelGuessResult.ForeColor = Color.FromArgb(100, 100, 100);
            labelGuessResult.Location = new Point(20, 230);
            labelGuessResult.Name = "labelGuessResult";
            labelGuessResult.Size = new Size(510, 30);
            labelGuessResult.TabIndex = 6;
            // 
            // buttonSubmitGuess
            // 
            buttonSubmitGuess.BackColor = Color.FromArgb(108, 92, 231);
            buttonSubmitGuess.Cursor = Cursors.Hand;
            buttonSubmitGuess.FlatAppearance.BorderSize = 0;
            buttonSubmitGuess.FlatStyle = FlatStyle.Flat;
            buttonSubmitGuess.ForeColor = Color.White;
            buttonSubmitGuess.Location = new Point(430, 190);
            buttonSubmitGuess.Name = "buttonSubmitGuess";
            buttonSubmitGuess.Size = new Size(100, 35);
            buttonSubmitGuess.TabIndex = 5;
            buttonSubmitGuess.Text = "提交猜测";
            buttonSubmitGuess.UseVisualStyleBackColor = false;
            buttonSubmitGuess.Click += ButtonSubmitGuess_Click;
            // 
            // textBoxGuess
            // 
            textBoxGuess.BorderStyle = BorderStyle.FixedSingle;
            textBoxGuess.Location = new Point(20, 190);
            textBoxGuess.Name = "textBoxGuess";
            textBoxGuess.PlaceholderText = "先自己想想，写下答案...";
            textBoxGuess.Size = new Size(400, 23);
            textBoxGuess.TabIndex = 4;
            // 
            // labelGuess
            // 
            labelGuess.AutoSize = true;
            labelGuess.Location = new Point(20, 160);
            labelGuess.Name = "labelGuess";
            labelGuess.Size = new Size(88, 17);
            labelGuess.TabIndex = 3;
            labelGuess.Text = "✏️ 你的猜测：";
            // 
            // labelContent
            // 
            labelContent.ForeColor = Color.FromArgb(50, 50, 50);
            labelContent.Location = new Point(20, 90);
            labelContent.Name = "labelContent";
            labelContent.Size = new Size(540, 60);
            labelContent.TabIndex = 2;
            // 
            // labelQuestionTitle
            // 
            labelQuestionTitle.AutoSize = true;
            labelQuestionTitle.Location = new Point(20, 60);
            labelQuestionTitle.Name = "labelQuestionTitle";
            labelQuestionTitle.Size = new Size(64, 17);
            labelQuestionTitle.TabIndex = 1;
            labelQuestionTitle.Text = "📖 题目：";
            // 
            // labelTitle
            // 
            labelTitle.Dock = DockStyle.Top;
            labelTitle.ForeColor = Color.FromArgb(108, 92, 231);
            labelTitle.Location = new Point(20, 20);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(544, 40);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "🤔 先思考，再看提示";
            labelTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // ProgressiveHintForm
            // 
            BackColor = Color.FromArgb(250, 248, 245);
            ClientSize = new Size(584, 541);
            Controls.Add(panelMain);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ProgressiveHintForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "💡 渐进式思考";
            panelMain.ResumeLayout(false);
            panelMain.PerformLayout();
            panelHints.ResumeLayout(false);
            _panelStepIndicator.ResumeLayout(false);
            _panelStepIndicator.PerformLayout();
            ResumeLayout(false);
        }
        #endregion

        #region UI加载逻辑（无改动，仅内部创建按钮统一复用字体）
        private void CreateHintButtons()
        {
            string[] buttonTexts = { "提示1", "提示2", "提示3", "提示4" };
            Color[] buttonColors = {
                Color.FromArgb(76, 175, 80),
                Color.FromArgb(33, 150, 243),
                Color.FromArgb(255, 152, 0),
                Color.FromArgb(244, 67, 54)
            };

            for (int i = 0; i < _maxHints; i++)
            {
                int level = i;
                Button hintButton = new Button
                {
                    Text = $"💡 {buttonTexts[i]}",
                    Size = new Size(128, 40),
                    Margin = new Padding(0, 0, 4, 0),
                    BackColor = Color.FromArgb(200, 200, 200),
                    ForeColor = Color.Gray,
                    FlatStyle = FlatStyle.Flat,
                    Font = _fontHintBtn,
                    Tag = level,
                    Enabled = false,
                    Cursor = Cursors.Hand
                };
                hintButton.FlatAppearance.BorderSize = 0;
                hintButton.Click += new EventHandler(this.HintButton_Click);
                hintButtonsPanel.Controls.Add(hintButton);
                UpdateHintButtonAppearance(hintButton, level, false);
            }
        }

        private void UpdateHintButtonAppearance(Button button, int level, bool isViewed)
        {
            Color[] buttonColors = {
                Color.FromArgb(76, 175, 80),
                Color.FromArgb(33, 150, 243),
                Color.FromArgb(255, 152, 0),
                Color.FromArgb(244, 67, 54)
            };
            if (isViewed)
            {
                button.BackColor = Color.FromArgb(180, 180, 180);
                button.ForeColor = Color.White;
                button.Text = $"✓ 提示{level + 1}";
            }
            else
            {
                button.BackColor = Color.FromArgb(220, 220, 220);
                button.ForeColor = Color.Gray;
                button.Text = $"🔒 提示{level + 1}";
            }
        }

        private void UnlockHintButton(int level)
        {
            if (level < 0 || level >= hintButtonsPanel.Controls.Count) return;
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

        private void UpdateStepIndicator()
        {
            string indicator = "";
            for (int i = 0; i < _maxHints; i++)
                indicator += i < _currentHintLevel ? "●" : "○";
            _labelStepIndicator.Text = indicator;
        }

        private void RestoreHintState()
        {
            foreach (var level in _viewedHints)
            {
                if (level >= hintButtonsPanel.Controls.Count) continue;
                var btn = hintButtonsPanel.Controls[level] as Button;
                if (btn == null) continue;
                btn.Enabled = true;
                UpdateHintButtonAppearance(btn, level, true);
            }
            if (_currentHintLevel < _maxHints)
                UnlockHintButton(_currentHintLevel);

            if (_viewedHints.Count > 0)
            {
                string content = "";
                foreach (var lvl in _viewedHints.OrderBy(x => x))
                    if (lvl < _hints.Count) content += $"提示{lvl + 1}：{_hints[lvl]}\n";
                _labelHintContent.Text = content;
            }
            UpdateStepIndicator();
            labelHintStatus.Text = $"提示等级：{_currentHintLevel}/{_maxHints}";
        }

        private void LoadHints()
        {
            CreateHintButtons();
            if (hintButtonsPanel.Controls.Count > 0)
                UnlockHintButton(0);
        }
        #endregion

        #region 事件处理（全部保留业务逻辑，仅统一事件绑定格式）
        private void HintButton_Click(object sender, EventArgs e)
        {
            if (sender is not Button button || button.Tag is not int level) return;
            if (level >= _hints.Count) return;

            _viewedHints.Add(level);
            UpdateHintButtonAppearance(button, level, true);
            if (level + 1 < hintButtonsPanel.Controls.Count)
                UnlockHintButton(level + 1);

            _currentHintLevel = level + 1;
            UpdateStepIndicator();
            labelHintStatus.Text = $"提示等级：{_currentHintLevel}/{_maxHints}";

            string hintContent = "";
            foreach (var lvl in _viewedHints.OrderBy(x => x))
                if (lvl < _hints.Count) hintContent += $"提示{lvl + 1}：{_hints[lvl]}\n";
            _labelHintContent.Text = hintContent;
        }

        private void ButtonSubmitGuess_Click(object sender, EventArgs e)
        {
            string guess = textBoxGuess.Text.Trim();
            if (string.IsNullOrEmpty(guess))
            {
                ShowGuessFeedback("⚠️ 请先输入你的猜测", FeedbackType.Warning);
                return;
            }
            var match = EvaluateAnswer(guess);
            switch (match)
            {
                case AnswerMatchResult.Correct: ShowCorrectFeedback(); break;
                case AnswerMatchResult.Close: ShowCloseFeedback(); break;
                case AnswerMatchResult.Wrong: ShowWrongFeedback(); break;
            }
        }

        private AnswerMatchResult EvaluateAnswer(string guess)
        {
            if (StringSimilarityHelper.CheckAnswer(guess, _correctAnswer, SimilarityThresholdExact))
                return AnswerMatchResult.Correct;
            if (StringSimilarityHelper.CheckAnswer(guess, _correctAnswer, SimilarityThresholdClose))
                return AnswerMatchResult.Close;
            return AnswerMatchResult.Wrong;
        }

        private void ShowCorrectFeedback()
        {
            var overlay = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(50, 0, 184, 148), Tag = "feedback_overlay" };
            this.Controls.Add(overlay);
            overlay.BringToFront();
            textBoxGuess.Enabled = false;
            buttonSubmitGuess.Enabled = false;
            ShowGuessFeedback("🎉 太棒了！你答对了！", FeedbackType.Correct);

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

            _feedbackTimer = new System.Windows.Forms.Timer { Interval = CorrectFeedbackDuration };
            _feedbackTimer.Tick += FeedbackCorrectTimer_Tick;
            _feedbackTimer.Start();
        }

        private void FeedbackCorrectTimer_Tick(object s, EventArgs args)
        {
            _feedbackTimer?.Stop();
            _feedbackTimer?.Dispose();
            _feedbackTimer = null;
            var overlay = this.Controls.OfType<Panel>().FirstOrDefault(p => p.Tag?.ToString() == "feedback_overlay");
            overlay?.Dispose();
            textBoxGuess.Enabled = true;
            buttonSubmitGuess.Enabled = true;
            MessageBox.Show("🎉 太棒了！继续下一题吧！", "回答正确", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        private void ShowCloseFeedback()
        {
            StartBorderBlink(ThemeHelper.SuccessColors.Main);
            ShowGuessFeedback("接近！✨ 想想提示，再做补充", FeedbackType.Close);
            if (_currentHintLevel < _maxHints && _currentHintLevel < hintButtonsPanel.Controls.Count)
                UnlockHintButton(_currentHintLevel);
        }

        private void ShowWrongFeedback()
        {
            StartBorderBlink(ThemeHelper.DangerColors.Main);
            ShowGuessFeedback("❌ 不对哦，再想想...", FeedbackType.Wrong);
            if (!_viewedHints.Contains(0) && hintButtonsPanel.Controls.Count > 0)
            {
                HintButton_Click(hintButtonsPanel.Controls[0], EventArgs.Empty);
                ShowGuessFeedback("💡 建议：尝试查看提示1", FeedbackType.Hint);
            }
        }

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

            if (type is FeedbackType.Normal or FeedbackType.Warning or FeedbackType.Hint)
            {
                _feedbackTimer?.Stop();
                _feedbackTimer?.Dispose();
                _feedbackTimer = new System.Windows.Forms.Timer { Interval = FeedbackAnimationDuration * 2 };
                _feedbackTimer.Tick += FeedbackRestoreTimer_Tick;
                _feedbackTimer.Start();
            }
        }

        private void FeedbackRestoreTimer_Tick(object s, EventArgs args)
        {
            _feedbackTimer?.Stop();
            _feedbackTimer?.Dispose();
            _feedbackTimer = null;
            labelGuessResult.ForeColor = _originalGuessResultColor;
            textBoxGuess.BackColor = _originalTextBoxBackColor;
        }

        private void StartBorderBlink(Color blinkColor)
        {
            _feedbackTimer?.Stop();
            _feedbackTimer?.Dispose();
            int blinkCount = 3;
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

        private void ButtonRevealAnswer_Click(object sender, EventArgs e)
        {
            var res = MessageBox.Show(
                "确定要查看答案吗？\n\n💡 建议：先努力回忆，实在想不出来再看答案，这样记忆更深刻！",
                "确认查看答案", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                _onAnswerRevealed?.Invoke(_correctAnswer);
                MessageBox.Show($"答案是：{_correctAnswer}\n\n📚 记住这个知识点，下次遇到类似的要能举一反三！",
                    "答案揭晓", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
        }
        #endregion

        #region 内部枚举
        private enum AnswerMatchResult { Correct, Close, Wrong }
        private enum FeedbackType { Normal, Correct, Close, Wrong, Hint, Warning }
        #endregion

        #region 公共持久化方法
        public (string guess, int currentLevel, HashSet<int> viewedLevels) GetProgress()
        {
            return (textBoxGuess.Text, _currentHintLevel, new HashSet<int>(_viewedHints));
        }
        #endregion

        #region 资源释放
        private IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                _feedbackTimer?.Stop();
                _feedbackTimer?.Dispose();
                _feedbackTimer = null;

                // 统一释放全部字体资源
                _fontTitleLarge?.Dispose();
                _fontSubTitle?.Dispose();
                _fontQuestion?.Dispose();
                _fontInput?.Dispose();
                _fontBtnBold?.Dispose();
                _fontNormal?.Dispose();
                _fontSmallTip?.Dispose();
                _fontStepDot?.Dispose();
                _fontHintBtn?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}