namespace LearningAssistant.Forms
{
    /*
    public void ShowTranslationDialog(string original, string translation, string grammar)
    {
        var dialog = new TranslationDialog(original, translation);
        dialog.AddToLearningList += (s, e) =>
        {
            textBoxQuestion.Text = original;
            AddToLearningList?.Invoke(this, EventArgs.Empty);
        };
        dialog.AskAi += (s, text) =>
        {
            textBoxQuestion.Text = text;
            AskAiWithText?.Invoke(this, text);
        };
        dialog.SpeakText += (s, text) =>
        {
            SpeakText?.Invoke(this, text);
        };
        dialog.ShowDialog();
    }*/
    public class TranslationDialog : Form
    {
        private readonly string _originalText;
        private readonly string _translationText;
        private TextBox _textBoxOriginal;
        private TextBox _textBoxTranslation;

        public event EventHandler? AddToLearningList;
        public event EventHandler<string>? AskAi;
        public event EventHandler<string>? SpeakText;

        public TranslationDialog(string original, string translation)
        {
            _originalText = original;
            _translationText = translation;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // 原文标签
            Label labelOriginal = new Label();
            labelOriginal.Text = "原文:";
            labelOriginal.Location = new Point(20, 20);
            labelOriginal.Size = new Size(300, 20);

            // 原文文本框
            _textBoxOriginal = new TextBox();
            _textBoxOriginal.Text = _originalText;
            _textBoxOriginal.Location = new Point(20, 40);
            _textBoxOriginal.Size = new Size(440, 80);
            _textBoxOriginal.Multiline = true;
            _textBoxOriginal.ReadOnly = true;
            _textBoxOriginal.ScrollBars = ScrollBars.Vertical;
            _textBoxOriginal.Font = new Font("Microsoft YaHei UI", 10F);

            // 译文标签
            Label labelTranslation = new Label();
            labelTranslation.Text = "译文:";
            labelTranslation.Location = new Point(20, 130);
            labelTranslation.Size = new Size(300, 20);

            // 译文文本框
            _textBoxTranslation = new TextBox();
            _textBoxTranslation.Text = _translationText;
            _textBoxTranslation.Location = new Point(20, 150);
            _textBoxTranslation.Size = new Size(440, 100);
            _textBoxTranslation.Multiline = true;
            _textBoxTranslation.ReadOnly = true;
            _textBoxTranslation.ScrollBars = ScrollBars.Vertical;
            _textBoxTranslation.Font = new Font("Microsoft YaHei UI", 10F);

            // 加生词本按钮
            Button buttonAddToLearning = new Button();
            buttonAddToLearning.Text = "📝 加生词本";
            buttonAddToLearning.Location = new Point(20, 260);
            buttonAddToLearning.Size = new Size(100, 35);
            buttonAddToLearning.Click += ButtonAddToLearning_Click;

            // AI提问按钮
            Button buttonAskAi = new Button();
            buttonAskAi.Text = "🤖 AI提问";
            buttonAskAi.Location = new Point(130, 260);
            buttonAskAi.Size = new Size(100, 35);
            buttonAskAi.Click += ButtonAskAi_Click;

            // 朗读原文按钮
            Button buttonSpeakOriginal = new Button();
            buttonSpeakOriginal.Text = "🔊 朗读原文";
            buttonSpeakOriginal.Location = new Point(240, 260);
            buttonSpeakOriginal.Size = new Size(100, 35);
            buttonSpeakOriginal.Click += ButtonSpeakOriginal_Click;

            // 朗读译文按钮
            Button buttonSpeakTranslation = new Button();
            buttonSpeakTranslation.Text = "🔊 朗读译文";
            buttonSpeakTranslation.Location = new Point(350, 260);
            buttonSpeakTranslation.Size = new Size(110, 35);
            buttonSpeakTranslation.Click += ButtonSpeakTranslation_Click;

            // 关闭按钮
            Button buttonClose = new Button();
            buttonClose.Text = "关闭";
            buttonClose.Location = new Point(190, 300);
            buttonClose.Size = new Size(100, 35);
            buttonClose.Click += ButtonClose_Click;

            // 添加控件
            Controls.Add(labelOriginal);
            Controls.Add(_textBoxOriginal);
            Controls.Add(labelTranslation);
            Controls.Add(_textBoxTranslation);
            Controls.Add(buttonAddToLearning);
            Controls.Add(buttonAskAi);
            Controls.Add(buttonSpeakOriginal);
            Controls.Add(buttonSpeakTranslation);
            Controls.Add(buttonClose);

            // 窗体设置
            ClientSize = new Size(480, 350);
            Text = "翻译结果";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
        }

        // 加生词本点击事件
        private void ButtonAddToLearning_Click(object sender, EventArgs e)
        {
            AddToLearningList?.Invoke(this, EventArgs.Empty);
        }

        // AI提问点击事件
        private void ButtonAskAi_Click(object sender, EventArgs e)
        {
            AskAi?.Invoke(this, _originalText);
        }

        // 朗读原文点击事件
        private void ButtonSpeakOriginal_Click(object sender, EventArgs e)
        {
            SpeakText?.Invoke(this, _originalText);
        }

        // 朗读译文点击事件
        private void ButtonSpeakTranslation_Click(object sender, EventArgs e)
        {
            SpeakText?.Invoke(this, _translationText);
        }

        // 关闭点击事件
        private void ButtonClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
