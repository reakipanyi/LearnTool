using Microsoft.Extensions.Logging;
using UnifiedLearningAssistant.Services.AI;
using UnifiedLearningAssistant.Services.TTS;

namespace UnifiedLearningAssistant.Forms
{
    public partial class AiQuestionDialog : Form
    {
        private readonly IAiQuestionService _aiQuestionService;
        private readonly ITTSService _ttsService;
        private readonly ILogger<AiQuestionDialog> _logger;
        private bool _disposed = false;

        public AiQuestionDialog(IAiQuestionService aiQuestionService, ITTSService ttsService, ILogger<AiQuestionDialog> logger)
        {
            _aiQuestionService = aiQuestionService ?? throw new ArgumentNullException(nameof(aiQuestionService));
            _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializeComponent();
        }

        public string QuestionText
        {
            get => textBoxQuestion.Text;
            set => textBoxQuestion.Text = value;
        }

        private async void ButtonAskAi_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxQuestion.Text))
                return;

            buttonAskAi.Enabled = false;
            richTextBoxAnswer.Text = "正在思考...";

            try
            {
                var answer = await _aiQuestionService.AskAsync(textBoxQuestion.Text);
                richTextBoxAnswer.Text = answer;
            }
            catch (Exception ex)
            {
                richTextBoxAnswer.Text = $"获取答案失败: {ex.Message}";
                labelStatus.Text = "提示: 请检查API配置和网络连接";
                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 3000 };
                timer.Tick += (s, e) => { labelStatus.Text = ""; timer.Stop(); };
                timer.Start();
            }

            buttonAskAi.Enabled = true;
        }

        private void ButtonSpeak_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(richTextBoxAnswer.Text) && _ttsService.Available)
            {
                _ = _ttsService.SpeakAsync(richTextBoxAnswer.Text, "zh");
            }
        }

        private void ButtonCopy_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(richTextBoxAnswer.Text))
            {
                Clipboard.SetText(richTextBoxAnswer.Text);
                labelStatus.Text = "答案已复制到剪贴板";
                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 2000 };
                timer.Tick += (s, e) => { labelStatus.Text = ""; timer.Stop(); };
                timer.Start();
            }
        }

        private void ButtonClear_Click(object? sender, EventArgs e)
        {
            textBoxQuestion.Text = "";
            richTextBoxAnswer.Text = "";
        }

        #region WinForms Designer Generated Code

        private System.ComponentModel.IContainer components = null;
        private TextBox textBoxQuestion;
        private Button buttonAskAi;
        private RichTextBox richTextBoxAnswer;
        private Button buttonSpeak;
        private Button buttonCopy;
        private Button buttonClear;
        private Label labelQuestion;
        private Label labelAnswer;
        private Label labelStatus;

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            textBoxQuestion = new TextBox();
            buttonAskAi = new Button();
            richTextBoxAnswer = new RichTextBox();
            buttonSpeak = new Button();
            buttonCopy = new Button();
            buttonClear = new Button();
            labelQuestion = new Label();
            labelAnswer = new Label();
            labelStatus = new Label();

            textBoxQuestion.Location = new Point(15, 40);
            textBoxQuestion.Size = new Size(450, 25);
            textBoxQuestion.TabIndex = 0;

            buttonAskAi.Location = new Point(470, 38);
            buttonAskAi.Size = new Size(100, 29);
            buttonAskAi.TabIndex = 1;
            buttonAskAi.Text = "向AI提问";
            buttonAskAi.Click += ButtonAskAi_Click;

            richTextBoxAnswer.Location = new Point(15, 100);
            richTextBoxAnswer.Size = new Size(555, 300);
            richTextBoxAnswer.ReadOnly = true;
            richTextBoxAnswer.ScrollBars = RichTextBoxScrollBars.Vertical;
            richTextBoxAnswer.TabIndex = 2;

            buttonSpeak.Location = new Point(15, 410);
            buttonSpeak.Size = new Size(120, 35);
            buttonSpeak.TabIndex = 3;
            buttonSpeak.Text = "🔊 朗读答案";
            buttonSpeak.Click += ButtonSpeak_Click;

            buttonCopy.Location = new Point(145, 410);
            buttonCopy.Size = new Size(120, 35);
            buttonCopy.TabIndex = 4;
            buttonCopy.Text = "📋 复制答案";
            buttonCopy.Click += ButtonCopy_Click;

            buttonClear.Location = new Point(275, 410);
            buttonClear.Size = new Size(120, 35);
            buttonClear.TabIndex = 5;
            buttonClear.Text = "🗑️ 清空";
            buttonClear.Click += ButtonClear_Click;

            labelQuestion.Location = new Point(15, 15);
            labelQuestion.Size = new Size(100, 20);
            labelQuestion.Text = "问题:";

            labelAnswer.Location = new Point(15, 80);
            labelAnswer.Size = new Size(100, 20);
            labelAnswer.Text = "AI回答:";

            labelStatus.Location = new Point(400, 415);
            labelStatus.Size = new Size(170, 25);
            labelStatus.Text = "";
            labelStatus.ForeColor = Color.Green;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(585, 460);
            Controls.Add(labelQuestion);
            Controls.Add(labelAnswer);
            Controls.Add(textBoxQuestion);
            Controls.Add(buttonAskAi);
            Controls.Add(richTextBoxAnswer);
            Controls.Add(buttonSpeak);
            Controls.Add(buttonCopy);
            Controls.Add(buttonClear);
            Controls.Add(labelStatus);
            Name = "AiQuestionDialog";
            Text = "🤖 AI智能提问";
            StartPosition = FormStartPosition.CenterScreen;
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }

            _disposed = true;
            base.Dispose(disposing);
        }
    }
}
