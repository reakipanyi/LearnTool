using LearningAssistant.Services.AI;
using LearningAssistant.Services.TTS;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace LearningAssistant.Forms
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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
            textBoxQuestion = new TextBox();
            buttonAskAi = new Button();
            richTextBoxAnswer = new RichTextBox();
            buttonSpeak = new Button();
            buttonCopy = new Button();
            buttonClear = new Button();
            labelQuestion = new Label();
            labelAnswer = new Label();
            labelStatus = new Label();
            SuspendLayout();
            // 
            // textBoxQuestion
            // 
            textBoxQuestion.Location = new Point(21, 56);
            textBoxQuestion.Margin = new Padding(4, 4, 4, 4);
            textBoxQuestion.Name = "textBoxQuestion";
            textBoxQuestion.Size = new Size(641, 28);
            textBoxQuestion.TabIndex = 0;
            // 
            // buttonAskAi
            // 
            buttonAskAi.Location = new Point(671, 53);
            buttonAskAi.Margin = new Padding(4, 4, 4, 4);
            buttonAskAi.Name = "buttonAskAi";
            buttonAskAi.Size = new Size(143, 41);
            buttonAskAi.TabIndex = 1;
            buttonAskAi.Text = "向AI提问";
            buttonAskAi.Click += ButtonAskAi_Click;
            // 
            // richTextBoxAnswer
            // 
            richTextBoxAnswer.Location = new Point(21, 140);
            richTextBoxAnswer.Margin = new Padding(4, 4, 4, 4);
            richTextBoxAnswer.Name = "richTextBoxAnswer";
            richTextBoxAnswer.ReadOnly = true;
            richTextBoxAnswer.ScrollBars = RichTextBoxScrollBars.Vertical;
            richTextBoxAnswer.Size = new Size(791, 418);
            richTextBoxAnswer.TabIndex = 2;
            richTextBoxAnswer.Text = "";
            // 
            // buttonSpeak
            // 
            buttonSpeak.Location = new Point(21, 574);
            buttonSpeak.Margin = new Padding(4, 4, 4, 4);
            buttonSpeak.Name = "buttonSpeak";
            buttonSpeak.Size = new Size(171, 49);
            buttonSpeak.TabIndex = 3;
            buttonSpeak.Text = "🔊 朗读答案";
            buttonSpeak.Click += ButtonSpeak_Click;
            // 
            // buttonCopy
            // 
            buttonCopy.Location = new Point(207, 574);
            buttonCopy.Margin = new Padding(4, 4, 4, 4);
            buttonCopy.Name = "buttonCopy";
            buttonCopy.Size = new Size(171, 49);
            buttonCopy.TabIndex = 4;
            buttonCopy.Text = "📋 复制答案";
            buttonCopy.Click += ButtonCopy_Click;
            // 
            // buttonClear
            // 
            buttonClear.Location = new Point(393, 574);
            buttonClear.Margin = new Padding(4, 4, 4, 4);
            buttonClear.Name = "buttonClear";
            buttonClear.Size = new Size(171, 49);
            buttonClear.TabIndex = 5;
            buttonClear.Text = "🗑️ 清空";
            buttonClear.Click += ButtonClear_Click;
            // 
            // labelQuestion
            // 
            labelQuestion.Location = new Point(21, 21);
            labelQuestion.Margin = new Padding(4, 0, 4, 0);
            labelQuestion.Name = "labelQuestion";
            labelQuestion.Size = new Size(143, 28);
            labelQuestion.TabIndex = 0;
            labelQuestion.Text = "问题:";
            // 
            // labelAnswer
            // 
            labelAnswer.Location = new Point(21, 112);
            labelAnswer.Margin = new Padding(4, 0, 4, 0);
            labelAnswer.Name = "labelAnswer";
            labelAnswer.Size = new Size(143, 28);
            labelAnswer.TabIndex = 1;
            labelAnswer.Text = "AI回答:";
            // 
            // labelStatus
            // 
            labelStatus.ForeColor = Color.Green;
            labelStatus.Location = new Point(571, 581);
            labelStatus.Margin = new Padding(4, 0, 4, 0);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(243, 35);
            labelStatus.TabIndex = 6;
            // 
            // AiQuestionDialog
            // 
            AutoScaleDimensions = new SizeF(10F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(836, 644);
            Controls.Add(labelQuestion);
            Controls.Add(labelAnswer);
            Controls.Add(textBoxQuestion);
            Controls.Add(buttonAskAi);
            Controls.Add(richTextBoxAnswer);
            Controls.Add(buttonSpeak);
            Controls.Add(buttonCopy);
            Controls.Add(buttonClear);
            Controls.Add(labelStatus);
            Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            Margin = new Padding(4, 4, 4, 4);
            Name = "AiQuestionDialog";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "🤖 AI智能提问";
            ResumeLayout(false);
            PerformLayout();
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
