using LearningAssistant.Services.TTS;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LearningAssistant.Forms
{
    /// <summary>
    /// 听写练习窗体 - 播放文字让用户手写，然后对比结果
    /// </summary>
    public partial class DictationForm : Form
    {
        private readonly ITTSService _ttsService;
        private readonly ILogger<DictationForm>? _logger;
        private bool _isPlaying = false;
        private int _playCount = 0;
        private const int MaxPlayCount = 3;

        public DictationForm(ITTSService ttsService, ILogger<DictationForm>? logger = null)
        {
            InitializeComponent();
            _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
            _logger = logger;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ExpectedText
        {
            get => textBoxExpected.Text;
            set => textBoxExpected.Text = value ?? string.Empty;
        }

        private async void buttonPlay_Click(object sender, EventArgs e)
        {
            var text = textBoxExpected.Text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("请先输入要听写的文本", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_isPlaying) return;

            try
            {
                _isPlaying = true;
                _playCount++;
                buttonPlay.Text = "🔊 播放中...";
                buttonPlay.BackColor = Color.Orange;
                labelStatus.Text = $"🔊 正在播放 (第{_playCount}次，最多{MaxPlayCount}次)";
                labelStatus.ForeColor = Color.Blue;

                // 清空手写输入区域，让用户重新听写
                if (_playCount == 1)
                {
                    textBoxUserInput.Text = string.Empty;
                    labelResult.Text = string.Empty;
                }

                await _ttsService.SpeakAsync(text, DetectLanguage(text));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "TTS播放失败");
                labelStatus.Text = $"❌ 播放失败: {ex.Message}";
                labelStatus.ForeColor = Color.Red;
            }
            finally
            {
                _isPlaying = false;
                buttonPlay.Text = _playCount >= MaxPlayCount ? "已达上限" : "🔊 再听一次";
                buttonPlay.BackColor = _playCount >= MaxPlayCount ? Color.Gray : Color.FromArgb(33, 150, 243);
                if (_playCount >= MaxPlayCount)
                {
                    buttonPlay.Enabled = false;
                    labelStatus.Text = "📝 请在下方输入你听到的内容";
                    labelStatus.ForeColor = Color.Black;
                }
                else
                {
                    labelStatus.Text = "📝 请在下方输入你听到的内容（可再听）";
                    labelStatus.ForeColor = Color.Black;
                }
            }
        }

        private void buttonCheck_Click(object sender, EventArgs e)
        {
            var expected = textBoxExpected.Text.Trim();
            var userInput = textBoxUserInput.Text.Trim();

            if (string.IsNullOrEmpty(expected))
            {
                MessageBox.Show("请先输入要听写的文本", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrEmpty(userInput))
            {
                MessageBox.Show("请先输入你听写的内容", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 计算相似度
            int score = CalculateSimilarity(expected, userInput);

            if (score >= 90)
            {
                labelResult.Text = $"得分: {score}分 🎉 太棒了！";
                labelResult.ForeColor = Color.Green;
            }
            else if (score >= 70)
            {
                labelResult.Text = $"得分: {score}分 💪 不错！";
                labelResult.ForeColor = Color.FromArgb(255, 165, 0);
            }
            else if (score >= 50)
            {
                labelResult.Text = $"得分: {score}分 📝 继续努力";
                labelResult.ForeColor = Color.Orange;
            }
            else
            {
                labelResult.Text = $"得分: {score}分 😢 需要多练习";
                labelResult.ForeColor = Color.Red;
            }

            // 显示差异
            ShowDifferences(expected, userInput);
        }

        private void ShowDifferences(string expected, string userInput)
        {
            // 简单的差异对比：逐字标记
            textBoxDiff.Text = string.Empty;
            textBoxDiff.AppendText("正确: " + expected + "\n");
            textBoxDiff.AppendText("你的: " + userInput + "\n");

            if (expected != userInput)
            {
                textBoxDiff.AppendText("\n差异:\n");
                int maxLen = Math.Max(expected.Length, userInput.Length);
                for (int i = 0; i < maxLen; i++)
                {
                    char eChar = i < expected.Length ? expected[i] : '\0';
                    char uChar = i < userInput.Length ? userInput[i] : '\0';
                    if (eChar != uChar)
                    {
                        textBoxDiff.SelectionColor = Color.Red;
                        textBoxDiff.AppendText($"[{i + 1}] 期望'{eChar}' 你的'{uChar}'\n");
                    }
                }
            }
            else
            {
                textBoxDiff.AppendText("完全正确！");
            }
            textBoxDiff.SelectionColor = Color.Black;
        }

        private int CalculateSimilarity(string expected, string input)
        {
            if (string.IsNullOrEmpty(expected) && string.IsNullOrEmpty(input))
                return 100;
            if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(input))
                return 0;

            // 去除空格和标点后比较
            var cleanExpected = System.Text.RegularExpressions.Regex.Replace(expected, @"[\s\p{P}]", "");
            var cleanInput = System.Text.RegularExpressions.Regex.Replace(input, @"[\s\p{P}]", "");

            if (cleanExpected.Length == 0 && cleanInput.Length == 0) return 100;
            if (cleanExpected.Length == 0 || cleanInput.Length == 0) return 0;

            int matchCount = 0;
            int minLen = Math.Min(cleanExpected.Length, cleanInput.Length);
            for (int i = 0; i < minLen; i++)
            {
                if (cleanExpected[i] == cleanInput[i])
                    matchCount++;
            }

            return (int)((double)matchCount / Math.Max(cleanExpected.Length, cleanInput.Length) * 100);
        }

        private string DetectLanguage(string text)
        {
            foreach (char c in text)
            {
                if (c >= 0x4E00 && c <= 0x9FFF)
                    return "zh";
            }
            return "en";
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            _playCount = 0;
            _isPlaying = false;
            buttonPlay.Text = "🔊 播放听写";
            buttonPlay.BackColor = Color.FromArgb(33, 150, 243);
            buttonPlay.Enabled = true;
            textBoxUserInput.Text = string.Empty;
            textBoxDiff.Text = string.Empty;
            labelResult.Text = string.Empty;
            labelStatus.Text = "📝 准备就绪，点击播放开始听写";
            labelStatus.ForeColor = Color.Black;
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        #region WinForms Designer Generated Code

        private System.ComponentModel.IContainer components = null;
        private TextBox textBoxExpected;
        private TextBox textBoxUserInput;
        private RichTextBox textBoxDiff;
        private Label labelExpected;
        private Label labelUserInput;
        private Label labelDiff;
        private Label labelStatus;
        private Label labelResult;
        private Button buttonPlay;
        private Button buttonCheck;
        private Button buttonReset;
        private Button buttonExit;

        private void InitializeComponent()
        {
            textBoxExpected = new TextBox();
            textBoxUserInput = new TextBox();
            textBoxDiff = new RichTextBox();
            labelExpected = new Label();
            labelUserInput = new Label();
            labelDiff = new Label();
            labelStatus = new Label();
            labelResult = new Label();
            buttonPlay = new Button();
            buttonCheck = new Button();
            buttonReset = new Button();
            buttonExit = new Button();
            SuspendLayout();
            // 
            // labelExpected
            // 
            labelExpected.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            labelExpected.Location = new Point(20, 15);
            labelExpected.Name = "labelExpected";
            labelExpected.Size = new Size(100, 25);
            labelExpected.Text = "听写内容:";
            labelExpected.TextAlign = ContentAlignment.MiddleRight;
            // 
            // textBoxExpected
            // 
            textBoxExpected.Font = new Font("微软雅黑", 14F);
            textBoxExpected.Location = new Point(130, 12);
            textBoxExpected.Name = "textBoxExpected";
            textBoxExpected.Size = new Size(720, 33);
            // 
            // buttonPlay
            // 
            buttonPlay.BackColor = Color.FromArgb(33, 150, 243);
            buttonPlay.FlatAppearance.BorderSize = 0;
            buttonPlay.FlatStyle = FlatStyle.Flat;
            buttonPlay.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            buttonPlay.ForeColor = Color.White;
            buttonPlay.Location = new Point(130, 55);
            buttonPlay.Name = "buttonPlay";
            buttonPlay.Size = new Size(150, 40);
            buttonPlay.Text = "🔊 播放听写";
            buttonPlay.UseVisualStyleBackColor = false;
            buttonPlay.Click += buttonPlay_Click;
            // 
            // labelStatus
            // 
            labelStatus.Font = new Font("微软雅黑", 11F);
            labelStatus.Location = new Point(290, 60);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(560, 30);
            labelStatus.Text = "📝 准备就绪，点击播放开始听写";
            // 
            // labelUserInput
            // 
            labelUserInput.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            labelUserInput.Location = new Point(20, 105);
            labelUserInput.Name = "labelUserInput";
            labelUserInput.Size = new Size(100, 25);
            labelUserInput.Text = "你的听写:";
            labelUserInput.TextAlign = ContentAlignment.MiddleRight;
            // 
            // textBoxUserInput
            // 
            textBoxUserInput.Font = new Font("微软雅黑", 14F);
            textBoxUserInput.Location = new Point(130, 102);
            textBoxUserInput.Name = "textBoxUserInput";
            textBoxUserInput.Size = new Size(720, 33);
            // 
            // buttonCheck
            // 
            buttonCheck.BackColor = Color.FromArgb(76, 175, 80);
            buttonCheck.FlatAppearance.BorderSize = 0;
            buttonCheck.FlatStyle = FlatStyle.Flat;
            buttonCheck.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            buttonCheck.ForeColor = Color.White;
            buttonCheck.Location = new Point(130, 145);
            buttonCheck.Name = "buttonCheck";
            buttonCheck.Size = new Size(150, 40);
            buttonCheck.Text = "✅ 提交对比";
            buttonCheck.UseVisualStyleBackColor = false;
            buttonCheck.Click += buttonCheck_Click;
            // 
            // labelResult
            // 
            labelResult.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            labelResult.Location = new Point(290, 150);
            labelResult.Name = "labelResult";
            labelResult.Size = new Size(560, 30);
            // 
            // labelDiff
            // 
            labelDiff.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            labelDiff.Location = new Point(20, 195);
            labelDiff.Name = "labelDiff";
            labelDiff.Size = new Size(100, 25);
            labelDiff.Text = "对比详情:";
            labelDiff.TextAlign = ContentAlignment.MiddleRight;
            // 
            // textBoxDiff
            // 
            textBoxDiff.Font = new Font("微软雅黑", 11F);
            textBoxDiff.Location = new Point(130, 192);
            textBoxDiff.Multiline = true;
            textBoxDiff.Name = "textBoxDiff";
            textBoxDiff.ReadOnly = true;
            textBoxDiff.ScrollBars = RichTextBoxScrollBars.Vertical;
            textBoxDiff.Size = new Size(720, 150);
            // 
            // buttonReset
            // 
            buttonReset.BackColor = Color.Gray;
            buttonReset.FlatAppearance.BorderSize = 0;
            buttonReset.FlatStyle = FlatStyle.Flat;
            buttonReset.Font = new Font("微软雅黑", 11F);
            buttonReset.ForeColor = Color.White;
            buttonReset.Location = new Point(350, 360);
            buttonReset.Name = "buttonReset";
            buttonReset.Size = new Size(120, 40);
            buttonReset.Text = "🔄 重新开始";
            buttonReset.UseVisualStyleBackColor = false;
            buttonReset.Click += buttonReset_Click;
            // 
            // buttonExit
            // 
            buttonExit.BackColor = Color.DarkGray;
            buttonExit.FlatAppearance.BorderSize = 0;
            buttonExit.FlatStyle = FlatStyle.Flat;
            buttonExit.Font = new Font("微软雅黑", 11F);
            buttonExit.ForeColor = Color.White;
            buttonExit.Location = new Point(730, 360);
            buttonExit.Name = "buttonExit";
            buttonExit.Size = new Size(120, 40);
            buttonExit.Text = "🏠 返回";
            buttonExit.UseVisualStyleBackColor = false;
            buttonExit.Click += buttonExit_Click;
            // 
            // DictationForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(250, 245, 235);
            ClientSize = new Size(880, 420);
            Controls.Add(textBoxDiff);
            Controls.Add(labelDiff);
            Controls.Add(labelResult);
            Controls.Add(buttonCheck);
            Controls.Add(textBoxUserInput);
            Controls.Add(labelUserInput);
            Controls.Add(labelStatus);
            Controls.Add(buttonPlay);
            Controls.Add(textBoxExpected);
            Controls.Add(labelExpected);
            Controls.Add(buttonExit);
            Controls.Add(buttonReset);
            Font = new Font("微软雅黑", 9F);
            Name = "DictationForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "📝 听写练习";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
