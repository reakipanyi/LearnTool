using Microsoft.Extensions.Logging;
using UnifiedLearningAssistant.Views;
using UnifiedLearningAssistant.Views.UI;

namespace UnifiedLearningAssistant.Forms
{
    public partial class ResultForm : Form, IResultView
    {
        private readonly ILogger<ResultForm> _logger;
        private bool _disposed = false;
        // 新增功能：中等级 - 添加图表控件
        private ChartControl chartControl;
        private int _knownCount = 0;
        private int _unknownCount = 0;
        private double _accuracyRate = 0.0;

        public ResultForm(ILogger<ResultForm> logger)
        {
            InitializeComponent();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region IResultView Implementation

        public string AccuracyRate
        {
            get => labelAccuracy.Text;
            set
            {
                labelAccuracy.Text = value;
                if (double.TryParse(value.Split(':')[1].Trim('%', ' '), out var rate))
                {
                    _accuracyRate = rate;
                    progressBarAccuracy.Value = (int)Math.Round(rate);
                }
                UpdateChart();
            }
        }

        public string Statistics { get; set; } = string.Empty;

        public string KnownItems
        {
            set
            {
                listBoxKnown.Items.Clear();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var parts = value.Split(':');
                    if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out var count))
                    {
                        _knownCount = count;
                    }
                    listBoxKnown.Items.Add(value);
                }
                labelKnown.Text = $"✅ 已掌握: {_knownCount}";
                UpdateChart();
            }
        }

        public string UnknownItems
        {
            set
            {
                listBoxUnknown.Items.Clear();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var parts = value.Split(':');
                    if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out var count))
                    {
                        _unknownCount = count;
                    }
                    listBoxUnknown.Items.Add(value);
                }
                labelUnknown.Text = $"📘 未掌握: {_unknownCount}";
                UpdateChart();
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
        // 新增功能：中等级 - 统计图表区域
        private GroupBox groupBoxChart;

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
            groupBoxChart = new GroupBox();
            chartControl = new ChartControl();
            groupBoxKnown.SuspendLayout();
            groupBoxUnknown.SuspendLayout();
            groupBoxChart.SuspendLayout();
            SuspendLayout();

            labelTitle.Font = new Font("Microsoft YaHei", 18F, FontStyle.Bold, GraphicsUnit.Point);
            labelTitle.Location = new Point(300, 20);
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
            progressBarAccuracy.Size = new Size(380, 25);
            progressBarAccuracy.TabIndex = 2;
            progressBarAccuracy.Maximum = 100;

            labelTotal.Font = new Font("Microsoft YaHei", 12F, FontStyle.Regular, GraphicsUnit.Point);
            labelTotal.Location = new Point(30, 150);
            labelTotal.Name = "labelTotal";
            labelTotal.Size = new Size(200, 25);
            labelTotal.TabIndex = 3;
            labelTotal.Text = "总题数: 0";

            labelKnown.Font = new Font("Microsoft YaHei", 12F, FontStyle.Regular, GraphicsUnit.Point);
            labelKnown.Location = new Point(30, 220);
            labelKnown.Name = "labelKnown";
            labelKnown.Size = new Size(200, 25);
            labelKnown.TabIndex = 4;
            labelKnown.Text = "✅ 已掌握: 0";

            labelUnknown.Font = new Font("Microsoft YaHei", 12F, FontStyle.Regular, GraphicsUnit.Point);
            labelUnknown.Location = new Point(30, 430);
            labelUnknown.Name = "labelUnknown";
            labelUnknown.Size = new Size(200, 25);
            labelUnknown.TabIndex = 5;
            labelUnknown.Text = "📘 未掌握: 0";

            groupBoxKnown.Controls.Add(listBoxKnown);
            groupBoxKnown.Location = new Point(30, 250);
            groupBoxKnown.Name = "groupBoxKnown";
            groupBoxKnown.Size = new Size(380, 170);
            groupBoxKnown.TabIndex = 7;
            groupBoxKnown.TabStop = false;
            groupBoxKnown.Text = "已会列表";

            listBoxKnown.Dock = DockStyle.Fill;
            listBoxKnown.FormattingEnabled = true;
            listBoxKnown.Location = new Point(3, 22);
            listBoxKnown.Name = "listBoxKnown";
            listBoxKnown.Size = new Size(374, 145);
            listBoxKnown.TabIndex = 0;

            groupBoxUnknown.Controls.Add(listBoxUnknown);
            groupBoxUnknown.Location = new Point(30, 460);
            groupBoxUnknown.Name = "groupBoxUnknown";
            groupBoxUnknown.Size = new Size(380, 170);
            groupBoxUnknown.TabIndex = 8;
            groupBoxUnknown.TabStop = false;
            groupBoxUnknown.Text = "未掌握清单";

            listBoxUnknown.Dock = DockStyle.Fill;
            listBoxUnknown.FormattingEnabled = true;
            listBoxUnknown.Location = new Point(3, 22);
            listBoxUnknown.Name = "listBoxUnknown";
            listBoxUnknown.Size = new Size(374, 145);
            listBoxUnknown.TabIndex = 0;

            groupBoxChart.Controls.Add(chartControl);
            groupBoxChart.Location = new Point(430, 70);
            groupBoxChart.Name = "groupBoxChart";
            groupBoxChart.Size = new Size(400, 380);
            groupBoxChart.TabIndex = 11;
            groupBoxChart.TabStop = false;
            groupBoxChart.Text = "📊 学习统计图表";

            chartControl.Dock = DockStyle.Fill;
            chartControl.Location = new Point(3, 22);
            chartControl.Name = "chartControl";
            chartControl.Size = new Size(394, 355);
            chartControl.TabIndex = 0;

            buttonReview.Font = new Font("Microsoft YaHei", 12F, FontStyle.Regular, GraphicsUnit.Point);
            buttonReview.Location = new Point(430, 470);
            buttonReview.Name = "buttonReview";
            buttonReview.Size = new Size(180, 45);
            buttonReview.TabIndex = 9;
            buttonReview.Text = "复习未掌握内容";
            buttonReview.Click += ButtonReview_Click;

            buttonBack.Font = new Font("Microsoft YaHei", 12F, FontStyle.Regular, GraphicsUnit.Point);
            buttonBack.Location = new Point(650, 470);
            buttonBack.Name = "buttonBack";
            buttonBack.Size = new Size(180, 45);
            buttonBack.TabIndex = 10;
            buttonBack.Text = "返回主界面";
            buttonBack.Click += ButtonBack_Click;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(255, 244, 230);
            ClientSize = new Size(860, 540);
            Controls.Add(labelTitle);
            Controls.Add(labelAccuracy);
            Controls.Add(progressBarAccuracy);
            Controls.Add(labelTotal);
            Controls.Add(labelKnown);
            Controls.Add(labelUnknown);
            Controls.Add(groupBoxKnown);
            Controls.Add(groupBoxUnknown);
            Controls.Add(groupBoxChart);
            Controls.Add(buttonReview);
            Controls.Add(buttonBack);
            Name = "ResultForm";
            Text = "测试结果报告";
            groupBoxKnown.ResumeLayout(false);
            groupBoxUnknown.ResumeLayout(false);
            groupBoxChart.ResumeLayout(false);
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

        // 新增功能：中等级 - 更新图表
        private void UpdateChart()
        {
            try
            {
                var values = new[] { (double)_knownCount, (double)_unknownCount };
                var labels = new[] { "已掌握", "未掌握" };
                var colors = new[] { Color.FromArgb(76, 175, 80), Color.FromArgb(244, 67, 54) };
                chartControl.SetData(values, labels, colors);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to update chart");
            }
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
