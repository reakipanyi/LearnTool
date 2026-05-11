using Microsoft.Extensions.Logging;
using UnifiedLearningAssistant.Views;

namespace UnifiedLearningAssistant.Forms
{
    public partial class ResultForm : Form, IResultView
    {
        private readonly ILogger<ResultForm> _logger;
        private bool _disposed = false;

        public ResultForm(ILogger<ResultForm> logger)
        {
            InitializeComponent();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region IResultView Implementation

        public string AccuracyRate
        {
            get => labelAccuracy.Text;
            set => labelAccuracy.Text = value;
        }

        public string Statistics { get; set; } = string.Empty;

        public string KnownItems
        {
            set
            {
                listBoxKnown.Items.Clear();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    foreach (var item in value.Split('|'))
                    {
                        listBoxKnown.Items.Add(item.Trim());
                    }
                }
            }
        }

        public string UnknownItems
        {
            set
            {
                listBoxUnknown.Items.Clear();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    foreach (var item in value.Split('|'))
                    {
                        listBoxUnknown.Items.Add(item.Trim());
                    }
                }
            }
        }

        public event EventHandler? ReviewUnknownClicked;
        public event EventHandler? CloseClicked;

        public void ShowMessage(string msg)
        {
            MessageBox.Show(msg);
        }

        public void CloseView()
        {
            Close();
        }

        #endregion

        #region WinForms Designer Generated Code

        private System.ComponentModel.IContainer components = null;
        private Label labelAccuracy;
        private Label labelTotal;
        private Label labelKnown;
        private Label labelUnknown;
        private Label labelTime;
        private ListBox listBoxKnown;
        private ListBox listBoxUnknown;
        private Button buttonReview;
        private Button buttonBack;
        private GroupBox groupBoxKnown;
        private GroupBox groupBoxUnknown;
        private ProgressBar progressBarAccuracy;
        private Label labelTitle;

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            labelAccuracy = new Label();
            labelTotal = new Label();
            labelKnown = new Label();
            labelUnknown = new Label();
            labelTime = new Label();
            listBoxKnown = new ListBox();
            listBoxUnknown = new ListBox();
            buttonReview = new Button();
            buttonBack = new Button();
            groupBoxKnown = new GroupBox();
            groupBoxUnknown = new GroupBox();
            progressBarAccuracy = new ProgressBar();
            labelTitle = new Label();
            groupBoxKnown.SuspendLayout();
            groupBoxUnknown.SuspendLayout();
            SuspendLayout();

            labelTitle.Font = new Font("Microsoft YaHei", 18F, FontStyle.Bold, GraphicsUnit.Point);
            labelTitle.Location = new Point(200, 20);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(300, 40);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "🎉 测试结果报告";
            labelTitle.TextAlign = ContentAlignment.MiddleCenter;

            labelAccuracy.Font = new Font("Microsoft YaHei", 16F, FontStyle.Bold, GraphicsUnit.Point);
            labelAccuracy.Location = new Point(30, 70);
            labelAccuracy.Name = "labelAccuracy";
            labelAccuracy.Size = new Size(200, 35);
            labelAccuracy.TabIndex = 1;
            labelAccuracy.Text = "正确率: 0%";

            progressBarAccuracy.Location = new Point(30, 110);
            progressBarAccuracy.Name = "progressBarAccuracy";
            progressBarAccuracy.Size = new Size(640, 25);
            progressBarAccuracy.TabIndex = 2;
            progressBarAccuracy.Maximum = 100;

            labelTotal.Font = new Font("Microsoft YaHei", 12F, FontStyle.Regular, GraphicsUnit.Point);
            labelTotal.Location = new Point(30, 150);
            labelTotal.Name = "labelTotal";
            labelTotal.Size = new Size(200, 25);
            labelTotal.TabIndex = 3;
            labelTotal.Text = "总题数: 0";

            labelKnown.Font = new Font("Microsoft YaHei", 12F, FontStyle.Regular, GraphicsUnit.Point);
            labelKnown.Location = new Point(250, 150);
            labelKnown.Name = "labelKnown";
            labelKnown.Size = new Size(200, 25);
            labelKnown.TabIndex = 4;
            labelKnown.Text = "掌握: 0";

            labelUnknown.Font = new Font("Microsoft YaHei", 12F, FontStyle.Regular, GraphicsUnit.Point);
            labelUnknown.Location = new Point(470, 150);
            labelUnknown.Name = "labelUnknown";
            labelUnknown.Size = new Size(200, 25);
            labelUnknown.TabIndex = 5;
            labelUnknown.Text = "未掌握: 0";

            labelTime.Font = new Font("Microsoft YaHei", 12F, FontStyle.Regular, GraphicsUnit.Point);
            labelTime.Location = new Point(30, 180);
            labelTime.Name = "labelTime";
            labelTime.Size = new Size(200, 25);
            labelTime.TabIndex = 6;
            labelTime.Text = "总耗时: 0分钟";

            groupBoxKnown.Controls.Add(listBoxKnown);
            groupBoxKnown.Location = new Point(30, 220);
            groupBoxKnown.Name = "groupBoxKnown";
            groupBoxKnown.Size = new Size(300, 200);
            groupBoxKnown.TabIndex = 7;
            groupBoxKnown.TabStop = false;
            groupBoxKnown.Text = "✅ 已会列表";

            listBoxKnown.Dock = DockStyle.Fill;
            listBoxKnown.FormattingEnabled = true;
            listBoxKnown.Location = new Point(3, 22);
            listBoxKnown.Name = "listBoxKnown";
            listBoxKnown.Size = new Size(294, 175);
            listBoxKnown.TabIndex = 0;

            groupBoxUnknown.Controls.Add(listBoxUnknown);
            groupBoxUnknown.Location = new Point(350, 220);
            groupBoxUnknown.Name = "groupBoxUnknown";
            groupBoxUnknown.Size = new Size(320, 200);
            groupBoxUnknown.TabIndex = 8;
            groupBoxUnknown.TabStop = false;
            groupBoxUnknown.Text = "📘 未掌握清单";

            listBoxUnknown.Dock = DockStyle.Fill;
            listBoxUnknown.FormattingEnabled = true;
            listBoxUnknown.Location = new Point(3, 22);
            listBoxUnknown.Name = "listBoxUnknown";
            listBoxUnknown.Size = new Size(314, 175);
            listBoxUnknown.TabIndex = 0;

            buttonReview.Font = new Font("Microsoft YaHei", 12F, FontStyle.Regular, GraphicsUnit.Point);
            buttonReview.Location = new Point(180, 440);
            buttonReview.Name = "buttonReview";
            buttonReview.Size = new Size(150, 40);
            buttonReview.TabIndex = 9;
            buttonReview.Text = "复习未掌握内容";
            buttonReview.Click += ButtonReview_Click;

            buttonBack.Font = new Font("Microsoft YaHei", 12F, FontStyle.Regular, GraphicsUnit.Point);
            buttonBack.Location = new Point(370, 440);
            buttonBack.Name = "buttonBack";
            buttonBack.Size = new Size(150, 40);
            buttonBack.TabIndex = 10;
            buttonBack.Text = "返回主界面";
            buttonBack.Click += ButtonBack_Click;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(255, 244, 230);
            ClientSize = new Size(700, 500);
            Controls.Add(labelTitle);
            Controls.Add(labelAccuracy);
            Controls.Add(progressBarAccuracy);
            Controls.Add(labelTotal);
            Controls.Add(labelKnown);
            Controls.Add(labelUnknown);
            Controls.Add(labelTime);
            Controls.Add(groupBoxKnown);
            Controls.Add(groupBoxUnknown);
            Controls.Add(buttonReview);
            Controls.Add(buttonBack);
            Name = "ResultForm";
            Text = "测试结果报告";
            groupBoxKnown.ResumeLayout(false);
            groupBoxUnknown.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        #region Event Handlers

        private void ButtonReview_Click(object? sender, EventArgs e)
        {
            ReviewUnknownClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonBack_Click(object? sender, EventArgs e)
        {
            CloseClicked?.Invoke(this, EventArgs.Empty);
            Close();
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