using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LearningAssistant.Forms
{
    public partial class DictationForm : Form
    {
        private readonly IAdvancedSpeechService _speechService;
        private readonly ILogger<DictationForm>? _logger;
        private bool _isListening = false;
        private CancellationTokenSource? _cts;

        public DictationForm(IAdvancedSpeechService speechService, ILogger<DictationForm>? logger = null)
        {
            InitializeComponent();
            _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
            _logger = logger;
        }

        private async void buttonStartDictation_Click(object sender, EventArgs e)
        {
            if (_isListening)
            {
                StopDictation();
            }
            else
            {
                await StartDictationAsync();
            }
        }

        private async Task StartDictationAsync()
        {
            try
            {
                _isListening = true;
                buttonStartDictation.Text = "⏹ 停止听写";
                buttonStartDictation.BackColor = Color.Red;
                labelStatus.Text = "🎤 正在听...请说话";
                labelStatus.ForeColor = Color.Green;
                textBoxResult.Text = string.Empty;
                panelWave.Visible = true;

                var expectedText = textBoxExpected.Text.Trim();
                
                if (!string.IsNullOrEmpty(expectedText))
                {
                    _cts = new CancellationTokenSource();
                    var score = await Task.Run(() => 
                        _speechService.StartDictationSessionWithScore(expectedText, 30), 
                        _cts.Token);
                    
                    UpdateResult(score.RecognizedText, score.Score, score.Passed);
                }
                else
                {
                    _speechService.StartDictationWithFeedback((text, confidence) =>
                    {
                        UpdatePartialResult(text, confidence);
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "听写启动失败");
                labelStatus.Text = $"❌ 启动失败: {ex.Message}";
                labelStatus.ForeColor = Color.Red;
                StopDictation();
            }
        }

        private void StopDictation()
        {
            try
            {
                _cts?.Cancel();
                _speechService.StopDictation();
            }
            finally
            {
                _isListening = false;
                buttonStartDictation.Text = "🎤 开始听写";
                buttonStartDictation.BackColor = Color.Green;
                labelStatus.Text = "📝 准备就绪";
                labelStatus.ForeColor = Color.Black;
                panelWave.Visible = false;
            }
        }

        private void UpdatePartialResult(string text, double confidence)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, double>(UpdatePartialResult), text, confidence);
                return;
            }

            textBoxResult.Text = text;
            
            if (confidence >= 0.8)
            {
                labelConfidence.Text = $"置信度: {confidence:P0} ✓";
                labelConfidence.ForeColor = Color.Green;
            }
            else if (confidence >= 0.5)
            {
                labelConfidence.Text = $"置信度: {confidence:P0} ~";
                labelConfidence.ForeColor = Color.Orange;
            }
            else
            {
                labelConfidence.Text = $"置信度: {confidence:P0} ✗";
                labelConfidence.ForeColor = Color.Red;
            }
        }

        private void UpdateResult(string recognizedText, int score, bool passed)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, int, bool>(UpdateResult), recognizedText, score, passed);
                return;
            }

            textBoxResult.Text = recognizedText;
            
            if (score >= 80)
            {
                labelScore.Text = $"得分: {score}分 🎉";
                labelScore.ForeColor = Color.Green;
            }
            else if (score >= 60)
            {
                labelScore.Text = $"得分: {score}分 💪";
                labelScore.ForeColor = Color.Orange;
            }
            else
            {
                labelScore.Text = $"得分: {score}分 😢";
                labelScore.ForeColor = Color.Red;
            }

            StopDictation();
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            textBoxExpected.Text = string.Empty;
            textBoxResult.Text = string.Empty;
            labelScore.Text = string.Empty;
            labelConfidence.Text = string.Empty;
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            StopDictation();
            Close();
        }

        private void DictationForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopDictation();
        }

        #region WinForms Designer Generated Code

        private System.ComponentModel.IContainer components = null;
        private Button buttonStartDictation;
        private TextBox textBoxExpected;
        private TextBox textBoxResult;
        private Label labelExpected;
        private Label labelResult;
        private Label labelStatus;
        private Label labelScore;
        private Label labelConfidence;
        private Button buttonClear;
        private Button buttonExit;
        private Panel panelWave;
        private Label labelWave1;
        private Label labelWave2;
        private Label labelWave3;
        private Label labelWave4;
        private Label labelWave5;

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            buttonStartDictation = new Button();
            textBoxExpected = new TextBox();
            textBoxResult = new TextBox();
            labelExpected = new Label();
            labelResult = new Label();
            labelStatus = new Label();
            labelScore = new Label();
            labelConfidence = new Label();
            buttonClear = new Button();
            buttonExit = new Button();
            panelWave = new Panel();
            labelWave5 = new Label();
            labelWave4 = new Label();
            labelWave3 = new Label();
            labelWave2 = new Label();
            labelWave1 = new Label();
            panelWave.SuspendLayout();
            SuspendLayout();
            // 
            // buttonStartDictation
            // 
            buttonStartDictation.BackColor = Color.Green;
            buttonStartDictation.FlatAppearance.BorderSize = 0;
            buttonStartDictation.FlatStyle = FlatStyle.Flat;
            buttonStartDictation.Font = new Font("微软雅黑", 16F, FontStyle.Bold);
            buttonStartDictation.ForeColor = Color.White;
            buttonStartDictation.Location = new Point(360, 250);
            buttonStartDictation.Name = "buttonStartDictation";
            buttonStartDictation.Size = new Size(200, 70);
            buttonStartDictation.TabIndex = 0;
            buttonStartDictation.Text = "🎤 开始听写";
            buttonStartDictation.UseVisualStyleBackColor = false;
            buttonStartDictation.Click += buttonStartDictation_Click;
            // 
            // textBoxExpected
            // 
            textBoxExpected.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxExpected.Font = new Font("微软雅黑", 12F);
            textBoxExpected.Location = new Point(150, 40);
            textBoxExpected.Name = "textBoxExpected";
            textBoxExpected.Size = new Size(650, 33);
            textBoxExpected.TabIndex = 1;
            // 
            // textBoxResult
            // 
            textBoxResult.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxResult.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            textBoxResult.Location = new Point(150, 120);
            textBoxResult.Multiline = true;
            textBoxResult.Name = "textBoxResult";
            textBoxResult.ReadOnly = true;
            textBoxResult.Size = new Size(650, 100);
            textBoxResult.TabIndex = 2;
            textBoxResult.TextAlign = HorizontalAlignment.Center;
            // 
            // labelExpected
            // 
            labelExpected.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            labelExpected.Location = new Point(30, 40);
            labelExpected.Name = "labelExpected";
            labelExpected.Size = new Size(110, 33);
            labelExpected.TabIndex = 3;
            labelExpected.Text = "预期文本:";
            labelExpected.TextAlign = ContentAlignment.MiddleRight;
            // 
            // labelResult
            // 
            labelResult.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            labelResult.Location = new Point(30, 120);
            labelResult.Name = "labelResult";
            labelResult.Size = new Size(110, 33);
            labelResult.TabIndex = 4;
            labelResult.Text = "识别结果:";
            labelResult.TextAlign = ContentAlignment.MiddleRight;
            // 
            // labelStatus
            // 
            labelStatus.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            labelStatus.Location = new Point(30, 250);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(320, 35);
            labelStatus.TabIndex = 5;
            labelStatus.Text = "📝 准备就绪";
            labelStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelScore
            // 
            labelScore.Font = new Font("微软雅黑", 16F, FontStyle.Bold);
            labelScore.Location = new Point(600, 250);
            labelScore.Name = "labelScore";
            labelScore.Size = new Size(200, 35);
            labelScore.TabIndex = 6;
            labelScore.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelConfidence
            // 
            labelConfidence.Font = new Font("微软雅黑", 12F);
            labelConfidence.Location = new Point(600, 290);
            labelConfidence.Name = "labelConfidence";
            labelConfidence.Size = new Size(200, 30);
            labelConfidence.TabIndex = 7;
            labelConfidence.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // buttonClear
            // 
            buttonClear.BackColor = Color.Gray;
            buttonClear.FlatAppearance.BorderSize = 0;
            buttonClear.FlatStyle = FlatStyle.Flat;
            buttonClear.Font = new Font("微软雅黑", 12F);
            buttonClear.ForeColor = Color.White;
            buttonClear.Location = new Point(30, 350);
            buttonClear.Name = "buttonClear";
            buttonClear.Size = new Size(120, 40);
            buttonClear.TabIndex = 8;
            buttonClear.Text = "🗑 清空";
            buttonClear.UseVisualStyleBackColor = false;
            buttonClear.Click += buttonClear_Click;
            // 
            // buttonExit
            // 
            buttonExit.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonExit.BackColor = Color.DarkGray;
            buttonExit.FlatAppearance.BorderSize = 0;
            buttonExit.FlatStyle = FlatStyle.Flat;
            buttonExit.Font = new Font("微软雅黑", 12F);
            buttonExit.ForeColor = Color.White;
            buttonExit.Location = new Point(730, 350);
            buttonExit.Name = "buttonExit";
            buttonExit.Size = new Size(120, 40);
            buttonExit.TabIndex = 9;
            buttonExit.Text = "🏠 返回";
            buttonExit.UseVisualStyleBackColor = false;
            buttonExit.Click += buttonExit_Click;
            // 
            // panelWave
            // 
            panelWave.Controls.Add(labelWave5);
            panelWave.Controls.Add(labelWave4);
            panelWave.Controls.Add(labelWave3);
            panelWave.Controls.Add(labelWave2);
            panelWave.Controls.Add(labelWave1);
            panelWave.Location = new Point(360, 200);
            panelWave.Name = "panelWave";
            panelWave.Size = new Size(200, 40);
            panelWave.TabIndex = 10;
            panelWave.Visible = false;
            // 
            // labelWave1
            // 
            labelWave1.BackColor = Color.Green;
            labelWave1.Location = new Point(10, 10);
            labelWave1.Name = "labelWave1";
            labelWave1.Size = new Size(20, 25);
            labelWave1.TabIndex = 0;
            // 
            // labelWave2
            // 
            labelWave2.BackColor = Color.Green;
            labelWave2.Location = new Point(50, 5);
            labelWave2.Name = "labelWave2";
            labelWave2.Size = new Size(20, 35);
            labelWave2.TabIndex = 1;
            // 
            // labelWave3
            // 
            labelWave3.BackColor = Color.Green;
            labelWave3.Location = new Point(90, 15);
            labelWave3.Name = "labelWave3";
            labelWave3.Size = new Size(20, 20);
            labelWave3.TabIndex = 2;
            // 
            // labelWave4
            // 
            labelWave4.BackColor = Color.Green;
            labelWave4.Location = new Point(130, 8);
            labelWave4.Name = "labelWave4";
            labelWave4.Size = new Size(20, 30);
            labelWave4.TabIndex = 3;
            // 
            // labelWave5
            // 
            labelWave5.BackColor = Color.Green;
            labelWave5.Location = new Point(170, 12);
            labelWave5.Name = "labelWave5";
            labelWave5.Size = new Size(20, 25);
            labelWave5.TabIndex = 4;
            // 
            // DictationForm
            // 
            AutoScaleDimensions = new SizeF(10F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(250, 245, 235);
            ClientSize = new Size(880, 420);
            Controls.Add(panelWave);
            Controls.Add(buttonExit);
            Controls.Add(buttonClear);
            Controls.Add(labelConfidence);
            Controls.Add(labelScore);
            Controls.Add(labelStatus);
            Controls.Add(labelResult);
            Controls.Add(labelExpected);
            Controls.Add(textBoxResult);
            Controls.Add(textBoxExpected);
            Controls.Add(buttonStartDictation);
            Font = new Font("微软雅黑", 12F);
            Name = "DictationForm";
            Text = "🎤 听写练习";
            FormClosing += DictationForm_FormClosing;
            panelWave.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
