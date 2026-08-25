using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls.Ai
{
    public enum FeynmanStep
    {
        Study = 1,
        Teach = 2,
        Review = 3,
        Simplify = 4
    }

    public class FeynmanLearningPanel : UserControl, IThemeable
    {
        #region 字段

        private string _content = string.Empty;
        private string _displayText = string.Empty;
        private FeynmanStep _currentStep = FeynmanStep.Study;
        private ThemeMode _currentTheme = ThemeMode.Light;

        private Panel _panelHeader = null!;
        private Label _labelTitle = null!;
        private Label _labelContentTitle = null!;

        private Panel _panelSteps = null!;
        private Button[] _stepButtons = new Button[4];
        private Label _stepProgressLabel = null!;

        private Panel _panelContent = null!;

        private Panel _panelStepStudy = null!;
        private Label _labelStudyTitle = null!;
        private RichTextBox _richTextBoxStudyContent = null!;
        private Label _labelStudyTip = null!;

        private Panel _panelStepTeach = null!;
        private Label _labelTeachTitle = null!;
        private Label _labelTeachPrompt = null!;
        private RichTextBox _richTextBoxTeachAnswer = null!;
        private Button _buttonAIFeedback = null!;
        private RichTextBox _richTextBoxAIFeedback = null!;
        private Label _labelTeachTip = null!;
        private bool _isAiFeedbackLoading = false;

        private Button _buttonShowHistory = null!;
        private Panel _panelHistory = null!;
        private ComboBox _comboBoxHistory = null!;
        private RichTextBox _richTextBoxHistoryContent = null!;
        private Label _labelHistoryInfo = null!;
        private bool _isHistoryPanelVisible = false;
        private List<HistoryEntry> _historyEntries = new();

        private Panel _panelStepReview = null!;
        private Label _labelReviewTitle = null!;
        private FlowLayoutPanel _flowLayoutPanelQuestions = null!;
        private List<QuestionItem> _questionItems = new();

        private Panel _panelStepSimplify = null!;
        private Label _labelSimplifyTitle = null!;
        private Label _labelSimplifyPrompt = null!;
        private TextBox _textBoxSimplified = null!;
        private Button _buttonGenerateSimplified = null!;
        private Label _labelAnalogyPrompt = null!;
        private TextBox _textBoxAnalogy = null!;
        private Button _buttonGenerateAnalogy = null!;
        private bool _isGeneratingSimplified = false;
        private bool _isGeneratingAnalogy = false;

        private Panel _panelFooter = null!;
        private Button _buttonPrev = null!;
        private Button _buttonNext = null!;
        private Button _buttonClose = null!;

        #endregion

        #region 属性

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                UpdateContentDisplay();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string DisplayText
        {
            get => _displayText;
            set
            {
                _displayText = value;
                UpdateStudyContent();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FeynmanStep CurrentStep
        {
            get => _currentStep;
            private set
            {
                _currentStep = value;
                UpdateStepDisplay();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<string> Questions { get; set; } = new();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? TeachAnswer => _richTextBoxTeachAnswer?.Text;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? SimplifiedText => _textBoxSimplified?.Text;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? AnalogyText => _textBoxAnalogy?.Text;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? AIFeedbackText => _richTextBoxAIFeedback?.Text;

        #endregion

        #region 事件

        public event EventHandler? CloseClicked;
        public event EventHandler<FeynmanStep>? StepChanged;
        public event EventHandler? Completed;
        public event EventHandler<string>? AIFeedbackRequested;
        public event EventHandler? GenerateSimplifiedRequested;
        public event EventHandler? GenerateAnalogyRequested;
        public event EventHandler? VoiceInputRequested;
        public event EventHandler<string>? HistoryRecordSelected;

        #endregion

        #region 构造函数

        public FeynmanLearningPanel()
        {
            InitializeComponent();
            SetupEventHandlers();
            UpdateStepDisplay();
        }

        #endregion

        #region 初始化

        private void InitializeComponent()
        {
            _panelHeader = new Panel();
            _labelTitle = new Label();
            _labelContentTitle = new Label();
            _panelSteps = new Panel();
            _stepProgressLabel = new Label();
            _panelContent = new Panel();

            _panelStepStudy = new Panel();
            _labelStudyTitle = new Label();
            _richTextBoxStudyContent = new RichTextBox();
            _labelStudyTip = new Label();

            _panelStepTeach = new Panel();
            _labelTeachTitle = new Label();
            _labelTeachPrompt = new Label();
            _richTextBoxTeachAnswer = new RichTextBox();
            _buttonAIFeedback = new Button();
            _labelTeachTip = new Label();

            _panelStepReview = new Panel();
            _labelReviewTitle = new Label();
            _flowLayoutPanelQuestions = new FlowLayoutPanel();

            _panelStepSimplify = new Panel();
            _labelSimplifyTitle = new Label();
            _labelSimplifyPrompt = new Label();
            _textBoxSimplified = new TextBox();
            _buttonGenerateSimplified = new Button();
            _labelAnalogyPrompt = new Label();
            _textBoxAnalogy = new TextBox();
            _buttonGenerateAnalogy = new Button();
            _richTextBoxAIFeedback = new RichTextBox();
            _panelFooter = new Panel();
            _buttonPrev = new Button();
            _buttonNext = new Button();
            _buttonClose = new Button();
            _panelHeader.SuspendLayout();
            _panelSteps.SuspendLayout();
            _panelContent.SuspendLayout();
            _panelStepStudy.SuspendLayout();
            _panelStepTeach.SuspendLayout();
            _panelStepReview.SuspendLayout();
            _panelStepSimplify.SuspendLayout();
            _panelFooter.SuspendLayout();
            SuspendLayout();

            //
            // _panelHeader
            //
            _panelHeader.Controls.Add(_labelContentTitle);
            _panelHeader.Controls.Add(_labelTitle);
            _panelHeader.Dock = DockStyle.Top;
            _panelHeader.Padding = new Padding(15, 10, 15, 10);
            _panelHeader.Size = new Size(350, 60);

            //
            // _labelTitle
            //
            _labelTitle.AutoSize = true;
            _labelTitle.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            _labelTitle.Location = new Point(15, 8);
            _labelTitle.Text = "🧠 费曼学习法";

            //
            // _labelContentTitle
            //
            _labelContentTitle.AutoEllipsis = true;
            _labelContentTitle.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _labelContentTitle.Location = new Point(15, 32);
            _labelContentTitle.Size = new Size(320, 20);
            _labelContentTitle.ForeColor = Color.FromArgb(117, 117, 117);

            //
            // _panelSteps
            //
            _panelSteps.Controls.Add(_stepProgressLabel);
            _panelSteps.Dock = DockStyle.Top;
            _panelSteps.Padding = new Padding(10, 5, 10, 5);
            _panelSteps.Size = new Size(350, 50);

            //
            // _stepProgressLabel
            //
            _stepProgressLabel.AutoSize = true;
            _stepProgressLabel.Font = new Font("微软雅黑", 9F);
            _stepProgressLabel.Location = new Point(10, 5);
            _stepProgressLabel.Text = "第 1 步 / 共 4 步";
            _stepProgressLabel.ForeColor = Color.FromArgb(117, 117, 117);

            // 创建步骤按钮
            CreateStepButtons();

            //
            // _panelContent
            //
            _panelContent.Controls.Add(_panelStepSimplify);
            _panelContent.Controls.Add(_panelStepReview);
            _panelContent.Controls.Add(_panelStepTeach);
            _panelContent.Controls.Add(_panelStepStudy);
            _panelContent.Dock = DockStyle.Fill;
            _panelContent.Padding = new Padding(10);

            //
            // _panelStepStudy
            //
            _panelStepStudy.Controls.Add(_labelStudyTip);
            _panelStepStudy.Controls.Add(_richTextBoxStudyContent);
            _panelStepStudy.Controls.Add(_labelStudyTitle);
            _panelStepStudy.Dock = DockStyle.Fill;

            //
            // _labelStudyTitle
            //
            _labelStudyTitle.AutoSize = true;
            _labelStudyTitle.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            _labelStudyTitle.Location = new Point(5, 5);
            _labelStudyTitle.Text = "📖 步骤一：学习概念";

            //
            // _richTextBoxStudyContent
            //
            _richTextBoxStudyContent.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _richTextBoxStudyContent.Font = new Font("微软雅黑", 10F);
            _richTextBoxStudyContent.Location = new Point(5, 35);
            _richTextBoxStudyContent.Size = new Size(320, 200);
            _richTextBoxStudyContent.ReadOnly = true;
            _richTextBoxStudyContent.BackColor = Color.White;
            _richTextBoxStudyContent.BorderStyle = BorderStyle.FixedSingle;

            //
            // _labelStudyTip
            //
            _labelStudyTip.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _labelStudyTip.Font = new Font("微软雅黑", 9F);
            _labelStudyTip.ForeColor = Color.FromArgb(117, 117, 117);
            _labelStudyTip.Location = new Point(5, 245);
            _labelStudyTip.Size = new Size(320, 20);
            _labelStudyTip.Text = "💡 先仔细理解知识点，再进行下一步";

            //
            // _panelStepTeach
            //
            _panelStepTeach.Controls.Add(_labelTeachTip);
            _panelStepTeach.Controls.Add(_richTextBoxAIFeedback);
            _panelStepTeach.Controls.Add(_buttonAIFeedback);
            _panelStepTeach.Controls.Add(_richTextBoxTeachAnswer);
            _panelStepTeach.Controls.Add(_labelTeachPrompt);
            _panelStepTeach.Controls.Add(_labelTeachTitle);
            _panelStepTeach.Dock = DockStyle.Fill;
            _panelStepTeach.Visible = false;

            //
            // _labelTeachTitle
            //
            _labelTeachTitle.AutoSize = true;
            _labelTeachTitle.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            _labelTeachTitle.Location = new Point(5, 5);
            _labelTeachTitle.Text = "👨‍🏫 步骤二：模拟教学";

            //
            // _labelTeachPrompt
            //
            _labelTeachPrompt.Font = new Font("微软雅黑", 10F);
            _labelTeachPrompt.Location = new Point(5, 35);
            _labelTeachPrompt.Size = new Size(320, 30);
            _labelTeachPrompt.Text = "用你自己的话解释这个概念：";

            //
            // _richTextBoxTeachAnswer
            //
            _richTextBoxTeachAnswer.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _richTextBoxTeachAnswer.Font = new Font("微软雅黑", 10F);
            _richTextBoxTeachAnswer.Location = new Point(5, 70);
            _richTextBoxTeachAnswer.Size = new Size(320, 80);
            _richTextBoxTeachAnswer.BackColor = Color.White;
            _richTextBoxTeachAnswer.BorderStyle = BorderStyle.FixedSingle;

            //
            // _buttonAIFeedback
            //
            _buttonAIFeedback.FlatStyle = FlatStyle.Flat;
            _buttonAIFeedback.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _buttonAIFeedback.ForeColor = Color.White;
            _buttonAIFeedback.Location = new Point(5, 155);
            _buttonAIFeedback.Size = new Size(120, 28);
            _buttonAIFeedback.Text = "🤖 获取AI反馈";
            _buttonAIFeedback.UseVisualStyleBackColor = false;
            _buttonAIFeedback.BackColor = Color.FromArgb(0, 120, 215);
            _buttonAIFeedback.Click += ButtonAIFeedback_Click;

            var _buttonVoiceInput = new Button
            {
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(130, 155),
                Size = new Size(100, 28),
                Text = "🎤 语音输入",
                UseVisualStyleBackColor = false,
                BackColor = Color.FromArgb(233, 30, 99),
                Cursor = Cursors.Hand
            };
            _buttonVoiceInput.FlatAppearance.BorderSize = 0;
            _buttonVoiceInput.Click += ButtonVoiceInput_Click;
            _panelStepTeach.Controls.Add(_buttonVoiceInput);

            _buttonShowHistory = new Button
            {
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(235, 155),
                Size = new Size(90, 28),
                Text = "📜 历史版本",
                UseVisualStyleBackColor = false,
                BackColor = Color.FromArgb(255, 152, 0),
                Cursor = Cursors.Hand
            };
            _buttonShowHistory.FlatAppearance.BorderSize = 0;
            _buttonShowHistory.Click += ButtonShowHistory_Click;
            _panelStepTeach.Controls.Add(_buttonShowHistory);

            //
            // _richTextBoxAIFeedback
            //
            _richTextBoxAIFeedback.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _richTextBoxAIFeedback.Font = new Font("微软雅黑", 9F);
            _richTextBoxAIFeedback.Location = new Point(5, 188);
            _richTextBoxAIFeedback.Size = new Size(320, 55);
            _richTextBoxAIFeedback.BackColor = Color.FromArgb(248, 248, 252);
            _richTextBoxAIFeedback.BorderStyle = BorderStyle.FixedSingle;
            _richTextBoxAIFeedback.ReadOnly = true;
            _richTextBoxAIFeedback.Visible = false;

            //
            // _panelHistory
            //
            _panelHistory = new Panel
            {
                Location = new Point(5, 188),
                Size = new Size(320, 75),
                Visible = false,
                BackColor = Color.White
            };

            _comboBoxHistory = new ComboBox
            {
                Location = new Point(0, 0),
                Size = new Size(320, 24),
                Font = new Font("微软雅黑", 9F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            _comboBoxHistory.SelectedIndexChanged += ComboBoxHistory_SelectedIndexChanged;

            _labelHistoryInfo = new Label
            {
                Location = new Point(0, 26),
                Size = new Size(320, 18),
                Font = new Font("微软雅黑", 8F),
                ForeColor = Color.FromArgb(117, 117, 117)
            };

            _richTextBoxHistoryContent = new RichTextBox
            {
                Location = new Point(0, 46),
                Size = new Size(320, 28),
                Font = new Font("微软雅黑", 9F),
                BackColor = Color.FromArgb(255, 248, 220),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                Multiline = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            _panelHistory.Controls.Add(_richTextBoxHistoryContent);
            _panelHistory.Controls.Add(_labelHistoryInfo);
            _panelHistory.Controls.Add(_comboBoxHistory);
            _panelStepTeach.Controls.Add(_panelHistory);

            //
            // _labelTeachTip
            //
            _labelTeachTip.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _labelTeachTip.Font = new Font("微软雅黑", 9F);
            _labelTeachTip.ForeColor = Color.FromArgb(117, 117, 117);
            _labelTeachTip.Location = new Point(5, 268);
            _labelTeachTip.Size = new Size(320, 20);
            _labelTeachTip.Text = "💡 用最简单的语言，避免专业术语";

            //
            // _panelStepReview
            //
            _panelStepReview.Controls.Add(_flowLayoutPanelQuestions);
            _panelStepReview.Controls.Add(_labelReviewTitle);
            _panelStepReview.Dock = DockStyle.Fill;
            _panelStepReview.Visible = false;

            //
            // _labelReviewTitle
            //
            _labelReviewTitle.AutoSize = true;
            _labelReviewTitle.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            _labelReviewTitle.Location = new Point(5, 5);
            _labelReviewTitle.Text = "❓ 步骤三：复习巩固";

            //
            // _flowLayoutPanelQuestions
            //
            _flowLayoutPanelQuestions.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _flowLayoutPanelQuestions.AutoScroll = true;
            _flowLayoutPanelQuestions.FlowDirection = FlowDirection.TopDown;
            _flowLayoutPanelQuestions.Location = new Point(5, 35);
            _flowLayoutPanelQuestions.Size = new Size(320, 230);
            _flowLayoutPanelQuestions.WrapContents = false;

            //
            // _panelStepSimplify
            //
            _panelStepSimplify.Controls.Add(_textBoxAnalogy);
            _panelStepSimplify.Controls.Add(_buttonGenerateAnalogy);
            _panelStepSimplify.Controls.Add(_labelAnalogyPrompt);
            _panelStepSimplify.Controls.Add(_textBoxSimplified);
            _panelStepSimplify.Controls.Add(_buttonGenerateSimplified);
            _panelStepSimplify.Controls.Add(_labelSimplifyPrompt);
            _panelStepSimplify.Controls.Add(_labelSimplifyTitle);
            _panelStepSimplify.Dock = DockStyle.Fill;
            _panelStepSimplify.Visible = false;

            //
            // _labelSimplifyTitle
            //
            _labelSimplifyTitle.AutoSize = true;
            _labelSimplifyTitle.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            _labelSimplifyTitle.Location = new Point(5, 5);
            _labelSimplifyTitle.Text = "✨ 步骤四：简化提炼";

            //
            // _labelSimplifyPrompt
            //
            _labelSimplifyPrompt.Font = new Font("微软雅黑", 10F);
            _labelSimplifyPrompt.Location = new Point(5, 35);
            _labelSimplifyPrompt.Size = new Size(320, 20);
            _labelSimplifyPrompt.Text = "用一句话总结这个知识点：";

            //
            // _textBoxSimplified
            //
            _textBoxSimplified.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _textBoxSimplified.Font = new Font("微软雅黑", 10F);
            _textBoxSimplified.Location = new Point(5, 60);
            _textBoxSimplified.Size = new Size(220, 25);
            _textBoxSimplified.BorderStyle = BorderStyle.FixedSingle;

            //
            // _buttonGenerateSimplified
            //
            _buttonGenerateSimplified.FlatStyle = FlatStyle.Flat;
            _buttonGenerateSimplified.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _buttonGenerateSimplified.ForeColor = Color.White;
            _buttonGenerateSimplified.Location = new Point(230, 58);
            _buttonGenerateSimplified.Size = new Size(95, 28);
            _buttonGenerateSimplified.Text = "✨ AI生成";
            _buttonGenerateSimplified.UseVisualStyleBackColor = false;
            _buttonGenerateSimplified.BackColor = Color.FromArgb(76, 175, 80);
            _buttonGenerateSimplified.Click += ButtonGenerateSimplified_Click;

            //
            // _labelAnalogyPrompt
            //
            _labelAnalogyPrompt.Font = new Font("微软雅黑", 10F);
            _labelAnalogyPrompt.Location = new Point(5, 100);
            _labelAnalogyPrompt.Size = new Size(320, 20);
            _labelAnalogyPrompt.Text = "打个比方，它像什么：";

            //
            // _textBoxAnalogy
            //
            _textBoxAnalogy.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _textBoxAnalogy.Font = new Font("微软雅黑", 10F);
            _textBoxAnalogy.Location = new Point(5, 125);
            _textBoxAnalogy.Size = new Size(220, 25);
            _textBoxAnalogy.BorderStyle = BorderStyle.FixedSingle;

            //
            // _buttonGenerateAnalogy
            //
            _buttonGenerateAnalogy.FlatStyle = FlatStyle.Flat;
            _buttonGenerateAnalogy.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _buttonGenerateAnalogy.ForeColor = Color.White;
            _buttonGenerateAnalogy.Location = new Point(230, 123);
            _buttonGenerateAnalogy.Size = new Size(95, 28);
            _buttonGenerateAnalogy.Text = "💡 AI生成";
            _buttonGenerateAnalogy.UseVisualStyleBackColor = false;
            _buttonGenerateAnalogy.BackColor = Color.FromArgb(255, 152, 0);
            _buttonGenerateAnalogy.Click += ButtonGenerateAnalogy_Click;

            //
            // _panelFooter
            //
            _panelFooter.Controls.Add(_buttonClose);
            _panelFooter.Controls.Add(_buttonNext);
            _panelFooter.Controls.Add(_buttonPrev);
            _panelFooter.Dock = DockStyle.Bottom;
            _panelFooter.Padding = new Padding(10, 5, 10, 10);
            _panelFooter.Size = new Size(350, 55);

            //
            // _buttonPrev
            //
            _buttonPrev.FlatStyle = FlatStyle.Flat;
            _buttonPrev.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _buttonPrev.ForeColor = Color.White;
            _buttonPrev.Location = new Point(10, 5);
            _buttonPrev.Size = new Size(90, 35);
            _buttonPrev.Text = "⬅ 上一步";
            _buttonPrev.UseVisualStyleBackColor = false;
            _buttonPrev.Enabled = false;

            //
            // _buttonNext
            //
            _buttonNext.FlatStyle = FlatStyle.Flat;
            _buttonNext.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _buttonNext.ForeColor = Color.White;
            _buttonNext.Location = new Point(110, 5);
            _buttonNext.Size = new Size(90, 35);
            _buttonNext.Text = "下一步 ➡";
            _buttonNext.UseVisualStyleBackColor = false;

            //
            // _buttonClose
            //
            _buttonClose.FlatStyle = FlatStyle.Flat;
            _buttonClose.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _buttonClose.ForeColor = Color.White;
            _buttonClose.Location = new Point(250, 5);
            _buttonClose.Size = new Size(90, 35);
            _buttonClose.Text = "✕ 关闭";
            _buttonClose.UseVisualStyleBackColor = false;

            //
            // FeynmanLearningPanel
            //
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            Controls.Add(_panelContent);
            Controls.Add(_panelFooter);
            Controls.Add(_panelSteps);
            Controls.Add(_panelHeader);
            Name = "FeynmanLearningPanel";
            Size = new Size(350, 400);

            _panelHeader.ResumeLayout(false);
            _panelHeader.PerformLayout();
            _panelSteps.ResumeLayout(false);
            _panelSteps.PerformLayout();
            _panelContent.ResumeLayout(false);
            _panelStepStudy.ResumeLayout(false);
            _panelStepStudy.PerformLayout();
            _panelStepTeach.ResumeLayout(false);
            _panelStepTeach.PerformLayout();
            _panelStepReview.ResumeLayout(false);
            _panelStepReview.PerformLayout();
            _panelStepSimplify.ResumeLayout(false);
            _panelStepSimplify.PerformLayout();
            _panelFooter.ResumeLayout(false);
            ResumeLayout(false);
        }

        private void CreateStepButtons()
        {
            string[] stepNames = { "学习", "教学", "复习", "简化" };
            int circleSize = 32;
            int totalWidth = _panelSteps.ClientSize.Width - 40;
            int spacing = totalWidth / 4;
            int startX = 20;
            int y = 18;

            for (int i = 0; i < 4; i++)
            {
                int centerX = startX + i * spacing + spacing / 2;

                var btn = new Button
                {
                    Size = new Size(circleSize, circleSize),
                    Location = new Point(centerX - circleSize / 2, y),
                    Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    Tag = i + 1,
                    BackColor = Color.FromArgb(230, 230, 230),
                    ForeColor = Color.FromArgb(120, 120, 120),
                    Cursor = Cursors.Hand,
                    Text = $"{i + 1}",
                    Name = $"stepButton{i}"
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Region = new Region(new System.Drawing.Drawing2D.GraphicsPath(
                    new PointF[]
                    {
                        new PointF(circleSize / 2f, 0),
                        new PointF(circleSize, circleSize / 2f),
                        new PointF(circleSize / 2f, circleSize),
                        new PointF(0, circleSize / 2f)
                    },
                    new byte[]
                    {
                        (byte)System.Drawing.Drawing2D.PathPointType.Start,
                        (byte)System.Drawing.Drawing2D.PathPointType.Line,
                        (byte)System.Drawing.Drawing2D.PathPointType.Line,
                        (byte)System.Drawing.Drawing2D.PathPointType.Line
                    }));
                btn.Click += StepButton_Click;
                btn.Paint += StepButton_Paint;
                _stepButtons[i] = btn;
                _panelSteps.Controls.Add(btn);

                var label = new Label
                {
                    Text = stepNames[i],
                    Font = new Font("微软雅黑", 8.5F),
                    ForeColor = Color.FromArgb(120, 120, 120),
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Name = $"stepLabel{i}"
                };
                label.Location = new Point(centerX - label.PreferredWidth / 2, y + circleSize + 4);
                _panelSteps.Controls.Add(label);

                if (i < 3)
                {
                    var line = new Panel
                    {
                        Size = new Size(spacing - circleSize, 2),
                        Location = new Point(centerX + circleSize / 2, y + circleSize / 2 - 1),
                        BackColor = Color.FromArgb(220, 220, 220),
                        Name = $"stepLine{i}"
                    };
                    _panelSteps.Controls.Add(line);
                }
            }

            _stepProgressLabel.Location = new Point(_stepProgressLabel.Location.X, y + circleSize + 24);
        }

        private void StepButton_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Button btn) return;
            int stepIndex = int.Parse(btn.Name.Replace("stepButton", ""));
            int currentStep = (int)_currentStep;
            bool isPast = stepIndex < currentStep;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            if (isPast)
            {
                int checkSize = 16;
                int x = (btn.Width - checkSize) / 2;
                int y = (btn.Height - checkSize) / 2;

                using var pen = new Pen(Color.White, 2.5f);
                pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                var points = new PointF[]
                {
                    new PointF(x + 3, y + checkSize / 2f),
                    new PointF(x + checkSize / 2f - 1, y + checkSize - 4),
                    new PointF(x + checkSize - 2, y + 3)
                };
                e.Graphics.DrawLines(pen, points);
            }
        }

        private void SetupEventHandlers()
        {
            _buttonPrev.Click += ButtonPrev_Click;
            _buttonNext.Click += ButtonNext_Click;
            _buttonClose.Click += ButtonClose_Click;
        }

        #endregion

        #region 公共方法

        public void SetQuestions(List<string> questions)
        {
            Questions = questions ?? new List<string>();
            UpdateQuestionPanel();
        }

        public void GoToStep(FeynmanStep step)
        {
            CurrentStep = step;
        }

        public void SetAIFeedback(string feedback)
        {
            if (_richTextBoxAIFeedback != null && !_richTextBoxAIFeedback.IsDisposed)
            {
                _richTextBoxAIFeedback.Text = feedback;
                _richTextBoxAIFeedback.Visible = true;
            }
            _isAiFeedbackLoading = false;
            UpdateAIFeedbackButtonState();
        }

        public void AppendVoiceText(string text)
        {
            if (_richTextBoxTeachAnswer != null && !_richTextBoxTeachAnswer.IsDisposed)
            {
                if (!string.IsNullOrEmpty(_richTextBoxTeachAnswer.Text))
                {
                    _richTextBoxTeachAnswer.Text += " ";
                }
                _richTextBoxTeachAnswer.Text += text;
            }
        }

        public void LoadHistoryRecords(List<HistoryEntry> entries)
        {
            _historyEntries = entries ?? new List<HistoryEntry>();
            UpdateHistoryComboBox();
        }

        public void ClearHistory()
        {
            _historyEntries.Clear();
            UpdateHistoryComboBox();
        }

        private void UpdateHistoryComboBox()
        {
            if (_comboBoxHistory == null || _comboBoxHistory.IsDisposed) return;

            _comboBoxHistory.Items.Clear();
            _comboBoxHistory.Items.Add("📜 选择历史版本对比...");

            foreach (var entry in _historyEntries)
            {
                var displayText = string.IsNullOrWhiteSpace(entry.TeachAnswer)
                    ? $"[{entry.Date:MM-dd HH:mm}] (无内容)"
                    : $"[{entry.Date:MM-dd HH:mm}] {entry.TeachAnswer.Length}字";
                _comboBoxHistory.Items.Add(new HistoryComboItem(entry.Id, displayText, entry));
            }

            _comboBoxHistory.SelectedIndex = 0;
            _buttonShowHistory.Enabled = _historyEntries.Count > 0;
            _buttonShowHistory.Text = _historyEntries.Count > 0 ? $"📜 历史({_historyEntries.Count})" : "📜 历史版本";
        }

        private void ButtonShowHistory_Click(object? sender, EventArgs e)
        {
            if (_historyEntries.Count == 0)
            {
                MessageBox.Show("暂无历史版本记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _isHistoryPanelVisible = !_isHistoryPanelVisible;
            _panelHistory.Visible = _isHistoryPanelVisible;
            _richTextBoxAIFeedback.Visible = !_isHistoryPanelVisible && !string.IsNullOrEmpty(_richTextBoxAIFeedback.Text);
            _buttonShowHistory.BackColor = _isHistoryPanelVisible ? Color.FromArgb(255, 183, 77) : Color.FromArgb(255, 152, 0);
        }

        private void ComboBoxHistory_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_comboBoxHistory == null || _comboBoxHistory.IsDisposed) return;
            if (_comboBoxHistory.SelectedIndex <= 0)
            {
                _richTextBoxHistoryContent.Clear();
                _labelHistoryInfo.Text = "";
                return;
            }

            if (_comboBoxHistory.SelectedItem is HistoryComboItem item)
            {
                _richTextBoxHistoryContent.Text = item.Entry.TeachAnswer;
                _labelHistoryInfo.Text = $"📅 {item.Entry.Date:yyyy-MM-dd HH:mm}  ·  {(item.Entry.IsCompleted ? "✅ 已完成" : "📝 未完成")}";
                HistoryRecordSelected?.Invoke(this, item.Entry.Id);
            }
        }

        public void SetAIFeedbackLoading(bool isLoading)
        {
            _isAiFeedbackLoading = isLoading;
            UpdateAIFeedbackButtonState();

            if (_richTextBoxAIFeedback != null && !_richTextBoxAIFeedback.IsDisposed)
            {
                if (isLoading)
                {
                    _richTextBoxAIFeedback.Text = "🤖 AI 正在分析你的解释，请稍候...";
                    _richTextBoxAIFeedback.Visible = true;
                }
            }
        }

        private void UpdateAIFeedbackButtonState()
        {
            if (_buttonAIFeedback != null && !_buttonAIFeedback.IsDisposed)
            {
                _buttonAIFeedback.Enabled = !_isAiFeedbackLoading;
                _buttonAIFeedback.Text = _isAiFeedbackLoading ? "⏳ 分析中..." : "🤖 获取AI反馈";
            }
        }

        public void SetSimplifiedText(string text)
        {
            if (_textBoxSimplified != null && !_textBoxSimplified.IsDisposed)
            {
                _textBoxSimplified.Text = text;
            }
            _isGeneratingSimplified = false;
            UpdateGenerateSimplifiedButtonState();
        }

        public void SetSimplifiedLoading(bool isLoading)
        {
            _isGeneratingSimplified = isLoading;
            UpdateGenerateSimplifiedButtonState();
        }

        private void UpdateGenerateSimplifiedButtonState()
        {
            if (_buttonGenerateSimplified != null && !_buttonGenerateSimplified.IsDisposed)
            {
                _buttonGenerateSimplified.Enabled = !_isGeneratingSimplified;
                _buttonGenerateSimplified.Text = _isGeneratingSimplified ? "⏳ 生成中..." : "✨ AI生成";
            }
        }

        public void SetAnalogyText(string text)
        {
            if (_textBoxAnalogy != null && !_textBoxAnalogy.IsDisposed)
            {
                _textBoxAnalogy.Text = text;
            }
            _isGeneratingAnalogy = false;
            UpdateGenerateAnalogyButtonState();
        }

        public void SetAnalogyLoading(bool isLoading)
        {
            _isGeneratingAnalogy = isLoading;
            UpdateGenerateAnalogyButtonState();
        }

        private void UpdateGenerateAnalogyButtonState()
        {
            if (_buttonGenerateAnalogy != null && !_buttonGenerateAnalogy.IsDisposed)
            {
                _buttonGenerateAnalogy.Enabled = !_isGeneratingAnalogy;
                _buttonGenerateAnalogy.Text = _isGeneratingAnalogy ? "⏳ 生成中..." : "💡 AI生成";
            }
        }

        #endregion

        #region 事件处理

        private void StepButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is int stepNum)
            {
                CurrentStep = (FeynmanStep)stepNum;
                StepChanged?.Invoke(this, CurrentStep);
            }
        }

        private void ButtonPrev_Click(object? sender, EventArgs e)
        {
            if (_currentStep > FeynmanStep.Study)
            {
                CurrentStep--;
                StepChanged?.Invoke(this, CurrentStep);
            }
        }

        private void ButtonNext_Click(object? sender, EventArgs e)
        {
            if (_currentStep < FeynmanStep.Simplify)
            {
                CurrentStep++;
                StepChanged?.Invoke(this, CurrentStep);
            }
            else
            {
                Completed?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ButtonClose_Click(object? sender, EventArgs e)
        {
            CloseClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonAIFeedback_Click(object? sender, EventArgs e)
        {
            if (_isAiFeedbackLoading) return;

            var userAnswer = _richTextBoxTeachAnswer?.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userAnswer))
            {
                MessageBox.Show("请先写下你的解释，再获取 AI 反馈", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            AIFeedbackRequested?.Invoke(this, userAnswer);
        }

        private void ButtonVoiceInput_Click(object? sender, EventArgs e)
        {
            VoiceInputRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonGenerateSimplified_Click(object? sender, EventArgs e)
        {
            if (_isGeneratingSimplified) return;
            GenerateSimplifiedRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonGenerateAnalogy_Click(object? sender, EventArgs e)
        {
            if (_isGeneratingAnalogy) return;
            GenerateAnalogyRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 私有方法

        private void UpdateStepDisplay()
        {
            int stepIndex = (int)_currentStep;
            _stepProgressLabel.Text = $"第 {stepIndex} 步 / 共 4 步";

            Color activeColor = Color.FromArgb(147, 112, 219);
            Color pastColor = Color.FromArgb(186, 156, 238);
            Color inactiveColor = Color.FromArgb(230, 230, 230);
            Color inactiveTextColor = Color.FromArgb(120, 120, 120);

            for (int i = 0; i < 4; i++)
            {
                if (_stepButtons[i] != null)
                {
                    bool isActive = (i + 1) == stepIndex;
                    bool isPast = (i + 1) < stepIndex;

                    _stepButtons[i].BackColor = isActive
                        ? activeColor
                        : isPast
                            ? pastColor
                            : inactiveColor;
                    _stepButtons[i].ForeColor = (isActive || isPast)
                        ? Color.White
                        : inactiveTextColor;
                    _stepButtons[i].Invalidate();

                    var label = _panelSteps.Controls[$"stepLabel{i}"] as Label;
                    if (label != null)
                    {
                        label.ForeColor = isActive
                            ? activeColor
                            : isPast
                                ? pastColor
                                : inactiveTextColor;
                        label.Font = isActive
                            ? new Font("微软雅黑", 8.5F, FontStyle.Bold)
                            : new Font("微软雅黑", 8.5F);
                    }

                    if (i < 3)
                    {
                        var line = _panelSteps.Controls[$"stepLine{i}"] as Panel;
                        if (line != null)
                        {
                            line.BackColor = (i + 1) < stepIndex
                                ? pastColor
                                : Color.FromArgb(220, 220, 220);
                        }
                    }
                }
            }

            _panelStepStudy.Visible = _currentStep == FeynmanStep.Study;
            _panelStepTeach.Visible = _currentStep == FeynmanStep.Teach;
            _panelStepReview.Visible = _currentStep == FeynmanStep.Review;
            _panelStepSimplify.Visible = _currentStep == FeynmanStep.Simplify;

            _buttonPrev.Enabled = _currentStep > FeynmanStep.Study;
            _buttonNext.Text = _currentStep == FeynmanStep.Simplify ? "✓ 完成" : "下一步 ➡";

            if (_currentStep == FeynmanStep.Review && _questionItems.Count == 0 && Questions.Count > 0)
            {
                UpdateQuestionPanel();
            }
        }

        private void UpdateContentDisplay()
        {
            if (_labelContentTitle != null)
            {
                string display = _content;
                if (display.Length > 30)
                    display = display.Substring(0, 30) + "...";
                _labelContentTitle.Text = $"📌 {display}";
            }
        }

        private void UpdateStudyContent()
        {
            if (_richTextBoxStudyContent != null)
            {
                _richTextBoxStudyContent.Text = _displayText;
            }
        }

        private void UpdateQuestionPanel()
        {
            if (_flowLayoutPanelQuestions == null || Questions == null)
                return;

            _flowLayoutPanelQuestions.Controls.Clear();
            _questionItems.Clear();

            foreach (var question in Questions)
            {
                var item = new QuestionItem(question)
                {
                    Width = _flowLayoutPanelQuestions.ClientSize.Width - 10
                };
                _questionItems.Add(item);
                _flowLayoutPanelQuestions.Controls.Add(item);
            }
        }

        #endregion

        #region IThemeable 实现

        public void ApplyTheme(ThemeColors colors)
        {
            _currentTheme = colors.ThemeMode;
            var isDark = colors.ThemeMode == ThemeMode.Dark;

            BackColor = isDark ? Color.FromArgb(30, 30, 30) : Color.White;

            _panelHeader.BackColor = isDark ? Color.FromArgb(40, 40, 40) : Color.FromArgb(248, 248, 252);
            _labelTitle.ForeColor = isDark ? Color.White : colors.TextPrimary;
            _labelContentTitle.ForeColor = colors.TextSecondary;

            _panelSteps.BackColor = isDark ? Color.FromArgb(35, 35, 35) : Color.FromArgb(245, 245, 245);
            _stepProgressLabel.ForeColor = colors.TextSecondary;

            for (int i = 0; i < 4; i++)
            {
                if (_stepButtons[i] != null)
                {
                    bool isActive = (i + 1) == (int)_currentStep;
                    bool isPast = (i + 1) < (int)_currentStep;
                    _stepButtons[i].BackColor = isActive
                        ? Color.FromArgb(147, 112, 219)
                        : isPast
                            ? Color.FromArgb(186, 156, 238)
                            : isDark ? Color.FromArgb(50, 50, 50) : Color.FromArgb(240, 240, 240);
                    _stepButtons[i].ForeColor = (isActive || isPast)
                        ? Color.White
                        : isDark ? Color.FromArgb(150, 150, 150) : Color.FromArgb(100, 100, 100);
                }
            }

            _panelContent.BackColor = isDark ? Color.FromArgb(30, 30, 30) : Color.White;

            _panelStepStudy.BackColor = isDark ? Color.FromArgb(30, 30, 30) : Color.White;
            _labelStudyTitle.ForeColor = isDark ? Color.White : colors.TextPrimary;
            _richTextBoxStudyContent.BackColor = isDark ? Color.FromArgb(45, 45, 45) : Color.White;
            _richTextBoxStudyContent.ForeColor = isDark ? Color.White : colors.TextPrimary;
            _labelStudyTip.ForeColor = colors.TextSecondary;

            _panelStepTeach.BackColor = isDark ? Color.FromArgb(30, 30, 30) : Color.White;
            _labelTeachTitle.ForeColor = isDark ? Color.White : colors.TextPrimary;
            _labelTeachPrompt.ForeColor = isDark ? Color.White : colors.TextPrimary;
            _richTextBoxTeachAnswer.BackColor = isDark ? Color.FromArgb(45, 45, 45) : Color.White;
            _richTextBoxTeachAnswer.ForeColor = isDark ? Color.White : colors.TextPrimary;
            _buttonAIFeedback.BackColor = Color.FromArgb(0, 120, 215);
            _buttonAIFeedback.ForeColor = Color.White;
            _buttonShowHistory.BackColor = _isHistoryPanelVisible ? Color.FromArgb(255, 183, 77) : Color.FromArgb(255, 152, 0);
            _buttonShowHistory.ForeColor = Color.White;
            _richTextBoxAIFeedback.BackColor = isDark ? Color.FromArgb(40, 40, 40) : Color.FromArgb(248, 248, 252);
            _richTextBoxAIFeedback.ForeColor = isDark ? Color.White : colors.TextPrimary;
            _labelTeachTip.ForeColor = colors.TextSecondary;

            _panelHistory.BackColor = isDark ? Color.FromArgb(30, 30, 30) : Color.White;
            _comboBoxHistory.BackColor = isDark ? Color.FromArgb(45, 45, 45) : Color.White;
            _comboBoxHistory.ForeColor = isDark ? Color.White : colors.TextPrimary;
            _richTextBoxHistoryContent.BackColor = isDark ? Color.FromArgb(50, 50, 40) : Color.FromArgb(255, 248, 220);
            _richTextBoxHistoryContent.ForeColor = isDark ? Color.White : colors.TextPrimary;
            _labelHistoryInfo.ForeColor = colors.TextSecondary;

            _panelStepReview.BackColor = isDark ? Color.FromArgb(30, 30, 30) : Color.White;
            _labelReviewTitle.ForeColor = isDark ? Color.White : colors.TextPrimary;
            _flowLayoutPanelQuestions.BackColor = isDark ? Color.FromArgb(30, 30, 30) : Color.White;

            _panelStepSimplify.BackColor = isDark ? Color.FromArgb(30, 30, 30) : Color.White;
            _labelSimplifyTitle.ForeColor = isDark ? Color.White : colors.TextPrimary;
            _labelSimplifyPrompt.ForeColor = isDark ? Color.White : colors.TextPrimary;
            _textBoxSimplified.BackColor = isDark ? Color.FromArgb(45, 45, 45) : Color.White;
            _textBoxSimplified.ForeColor = isDark ? Color.White : colors.TextPrimary;
            _buttonGenerateSimplified.BackColor = Color.FromArgb(76, 175, 80);
            _buttonGenerateSimplified.ForeColor = Color.White;
            _labelAnalogyPrompt.ForeColor = isDark ? Color.White : colors.TextPrimary;
            _textBoxAnalogy.BackColor = isDark ? Color.FromArgb(45, 45, 45) : Color.White;
            _textBoxAnalogy.ForeColor = isDark ? Color.White : colors.TextPrimary;
            _buttonGenerateAnalogy.BackColor = Color.FromArgb(255, 152, 0);
            _buttonGenerateAnalogy.ForeColor = Color.White;

            _panelFooter.BackColor = isDark ? Color.FromArgb(35, 35, 35) : Color.FromArgb(248, 248, 252);
            _buttonPrev.BackColor = isDark ? Color.FromArgb(80, 80, 80) : Color.FromArgb(158, 158, 158);
            _buttonNext.BackColor = Color.FromArgb(147, 112, 219);
            _buttonClose.BackColor = isDark ? Color.FromArgb(80, 80, 80) : Color.FromArgb(158, 158, 158);
        }

        #endregion

        #region QuestionItem 内部控件

        private class QuestionItem : UserControl
        {
            private readonly Label _labelQuestion;
            private readonly PictureBox _iconBox;

            public string Question { get; }

            public QuestionItem(string question)
            {
                Question = question;

                _iconBox = new PictureBox
                {
                    Size = new Size(20, 20),
                    Location = new Point(5, 8),
                    SizeMode = PictureBoxSizeMode.CenterImage
                };

                _labelQuestion = new Label
                {
                    AutoSize = false,
                    Dock = DockStyle.Fill,
                    Font = new Font("微软雅黑", 9F),
                    Padding = new Padding(30, 8, 5, 8),
                    Text = question,
                    ForeColor = Color.FromArgb(33, 33, 33)
                };

                Controls.Add(_labelQuestion);
                Controls.Add(_iconBox);

                Height = 36;
                BackColor = Color.FromArgb(248, 248, 252);
                Margin = new Padding(0, 0, 0, 5);

                try
                {
                    var icon = SystemIcons.Information.ToBitmap();
                    _iconBox.Image = new Bitmap(icon, 16, 16);
                }
                catch
                {
                    _iconBox.Visible = false;
                }
            }
        }

        #endregion

        #region 历史记录数据结构

        public class HistoryEntry
        {
            public string Id { get; set; } = string.Empty;
            public string TeachAnswer { get; set; } = string.Empty;
            public DateTime Date { get; set; }
            public bool IsCompleted { get; set; }
        }

        private class HistoryComboItem
        {
            public string Id { get; }
            public string DisplayText { get; }
            public HistoryEntry Entry { get; }

            public HistoryComboItem(string id, string displayText, HistoryEntry entry)
            {
                Id = id;
                DisplayText = displayText;
                Entry = entry;
            }

            public override string ToString() => DisplayText;
        }

        #endregion
    }
}