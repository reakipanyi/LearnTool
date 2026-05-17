using Microsoft.Extensions.Logging;
using UnifiedLearningAssistant.Views;
using UnifiedLearningAssistant.Views.UI;
using System.Drawing.Drawing2D;

namespace UnifiedLearningAssistant.Forms
{
    public partial class ResultForm : Form, IResultView
    {
        private readonly ILogger<ResultForm> _logger;
        private bool _disposed = false;
        private ChartControl chartControl;
        private int _knownCount = 0;
        private int _unknownCount = 0;
        private double _accuracyRate = 0.0;
        private Timer animationTimer;
        private double animationProgress = 0;
        private Label labelMotivational;
        private ProgressBar animatedProgressBar;



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
                    StartProgressAnimation((int)Math.Round(rate));
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
                UpdateMotivationalMessage();
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
        private GroupBox groupBoxChart;
        private Panel headerPanel;
        private Label labelAccuracyValue;
        private ProgressBar progressBarAccuracyGradient;

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
            headerPanel = new Panel();
            labelMotivational = new Label();
            animatedProgressBar = new ProgressBar();
            labelAccuracyValue = new Label();
            progressBarAccuracyGradient = new ProgressBar();
            animationTimer = new Timer(components);

            animationTimer.Interval = 30;
            animationTimer.Tick += AnimationTimer_Tick;

            groupBoxKnown.SuspendLayout();
            groupBoxUnknown.SuspendLayout();
            groupBoxChart.SuspendLayout();
            headerPanel.SuspendLayout();
            SuspendLayout();

            headerPanel.BackColor = WarmOrange;
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Location = new Point(0, 0);
            headerPanel.Name = "headerPanel";
            headerPanel.Size = new Size(860, 60);
            headerPanel.TabIndex = 12;

            labelTitle.Font = new Font("Microsoft YaHei", 20F, FontStyle.Bold, GraphicsUnit.Point);
            labelTitle.ForeColor = Color.White;
            labelTitle.Location = new Point(280, 10);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(300, 40);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "🎉 测试结果报告";
            labelTitle.TextAlign = ContentAlignment.MiddleCenter;
            headerPanel.Controls.Add(labelTitle);

            labelAccuracy.Font = new Font("Microsoft YaHei", 14F, FontStyle.Bold, GraphicsUnit.Point);
            labelAccuracy.ForeColor = Color.FromArgb(33, 33, 33);
            labelAccuracy.Location = new Point(30, 80);
            labelAccuracy.Name = "labelAccuracy";
            labelAccuracy.Size = new Size(200, 30);
            labelAccuracy.TabIndex = 1;
            labelAccuracy.Text = "正确率";

            labelAccuracyValue.Font = new Font("Microsoft YaHei", 28F, FontStyle.Bold, GraphicsUnit.Point);
            labelAccuracyValue.ForeColor = WarmOrange;
            labelAccuracyValue.Location = new Point(30, 105);
            labelAccuracyValue.Name = "labelAccuracyValue";
            labelAccuracyValue.Size = new Size(150, 50);
            labelAccuracyValue.TabIndex = 13;
            labelAccuracyValue.Text = "0%";

            progressBarAccuracyGradient.Location = new Point(30, 160);
            progressBarAccuracyGradient.Name = "progressBarAccuracyGradient";
            progressBarAccuracyGradient.Size = new Size(380, 25);
            progressBarAccuracyGradient.TabIndex = 14;
            progressBarAccuracyGradient.Maximum = 100;
            progressBarAccuracyGradient.Style = ProgressBarStyle.Continuous;

            labelMotivational.Font = new Font("Microsoft YaHei", 11F, FontStyle.Italic, GraphicsUnit.Point);
            labelMotivational.ForeColor = SuccessGreen;
            labelMotivational.Location = new Point(30, 190);
            labelMotivational.Name = "labelMotivational";
            labelMotivational.Size = new Size(380, 25);
            labelMotivational.TabIndex = 15;
            labelMotivational.Text = "✨ 继续加油！";

            labelTotal.Font = new Font("Microsoft YaHei", 12F, FontStyle.Regular, GraphicsUnit.Point);
            labelTotal.Location = new Point(30, 230);
            labelTotal.Name = "labelTotal";
            labelTotal.Size = new Size(200, 25);
            labelTotal.TabIndex = 3;
            labelTotal.Text = "总题数: 0";

            labelKnown.Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold, GraphicsUnit.Point);
            labelKnown.ForeColor = SuccessGreen;
            labelKnown.Location = new Point(30, 270);
            labelKnown.Name = "labelKnown";
            labelKnown.Size = new Size(250, 25);
            labelKnown.TabIndex = 4;
            labelKnown.Text = "✅ 已掌握: 0";

            groupBoxKnown.BackColor = Color.FromArgb(255, 250, 240);
            groupBoxKnown.Controls.Add(listBoxKnown);
            groupBoxKnown.Location = new Point(30, 300);
            groupBoxKnown.Name = "groupBoxKnown";
            groupBoxKnown.Size = new Size(380, 150);
            groupBoxKnown.TabIndex = 7;
            groupBoxKnown.TabStop = false;
            groupBoxKnown.FlatStyle = FlatStyle.Flat;
            groupBoxKnown.Padding = new Padding(5);

            listBoxKnown.BackColor = Color.White;
            listBoxKnown.Dock = DockStyle.Fill;
            listBoxKnown.FormattingEnabled = true;
            listBoxKnown.Location = new Point(3, 22);
            listBoxKnown.Name = "listBoxKnown";
            listBoxKnown.Size = new Size(374, 125);
            listBoxKnown.TabIndex = 0;
            listBoxKnown.BorderStyle = BorderStyle.None;

            labelUnknown.Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold, GraphicsUnit.Point);
            labelUnknown.ForeColor = SoftBlue;
            labelUnknown.Location = new Point(30, 460);
            labelUnknown.Name = "labelUnknown";
            labelUnknown.Size = new Size(250, 25);
            labelUnknown.TabIndex = 5;
            labelUnknown.Text = "📘 未掌握: 0";

            groupBoxUnknown.BackColor = Color.FromArgb(245, 250, 255);
            groupBoxUnknown.Controls.Add(listBoxUnknown);
            groupBoxUnknown.Location = new Point(30, 490);
            groupBoxUnknown.Name = "groupBoxUnknown";
            groupBoxUnknown.Size = new Size(380, 150);
            groupBoxUnknown.TabIndex = 8;
            groupBoxUnknown.TabStop = false;
            groupBoxUnknown.FlatStyle = FlatStyle.Flat;
            groupBoxUnknown.Padding = new Padding(5);

            listBoxUnknown.BackColor = Color.White;
            listBoxUnknown.Dock = DockStyle.Fill;
            listBoxUnknown.FormattingEnabled = true;
            listBoxUnknown.Location = new Point(3, 22);
            listBoxUnknown.Name = "listBoxUnknown";
            listBoxUnknown.Size = new Size(374, 125);
            listBoxUnknown.TabIndex = 0;
            listBoxUnknown.BorderStyle = BorderStyle.None;

            groupBoxChart.BackColor = Color.White;
            groupBoxChart.Controls.Add(chartControl);
            groupBoxChart.Location = new Point(430, 70);
            groupBoxChart.Name = "groupBoxChart";
            groupBoxChart.Size = new Size(400, 380);
            groupBoxChart.TabIndex = 11;
            groupBoxChart.TabStop = false;
            groupBoxChart.FlatStyle = FlatStyle.Flat;
            groupBoxChart.Font = new Font("Microsoft YaHei", 10F, FontStyle.Regular, GraphicsUnit.Point);
            groupBoxChart.Text = "📊 学习统计图表";
            groupBoxChart.Padding = new Padding(5);

            chartControl.Dock = DockStyle.Fill;
            chartControl.Location = new Point(3, 22);
            chartControl.Name = "chartControl";
            chartControl.Size = new Size(394, 355);
            chartControl.TabIndex = 0;

            buttonReview.FlatStyle = FlatStyle.Flat;
            buttonReview.FlatAppearance.BorderSize = 0;
            buttonReview.BackColor = WarmOrange;
            buttonReview.ForeColor = Color.White;
            buttonReview.Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold, GraphicsUnit.Point);
            buttonReview.Location = new Point(430, 650);
            buttonReview.Name = "buttonReview";
            buttonReview.Size = new Size(180, 45);
            buttonReview.TabIndex = 9;
            buttonReview.Text = "🔄 复习未掌握内容";
            buttonReview.UseVisualStyleBackColor = false;
            buttonReview.MouseEnter += Button_HoverEnter;
            buttonReview.MouseLeave += Button_HoverLeave;
            buttonReview.Click += ButtonReview_Click;

            buttonBack.FlatStyle = FlatStyle.Flat;
            buttonBack.FlatAppearance.BorderSize = 0;
            buttonBack.BackColor = SuccessGreen;
            buttonBack.ForeColor = Color.White;
            buttonBack.Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold, GraphicsUnit.Point);
            buttonBack.Location = new Point(630, 650);
            buttonBack.Name = "buttonBack";
            buttonBack.Size = new Size(180, 45);
            buttonBack.TabIndex = 10;
            buttonBack.Text = "🏠 返回主界面";
            buttonBack.UseVisualStyleBackColor = false;
            buttonBack.MouseEnter += Button_HoverEnter;
            buttonBack.MouseLeave += Button_HoverLeave;
            buttonBack.Click += ButtonBack_Click;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = WarmBeige;
            ClientSize = new Size(860, 710);
            Controls.Add(buttonReview);
            Controls.Add(buttonBack);
            Controls.Add(labelMotivational);
            Controls.Add(labelAccuracyValue);
            Controls.Add(progressBarAccuracyGradient);
            Controls.Add(headerPanel);
            Controls.Add(labelAccuracy);
            Controls.Add(labelTotal);
            Controls.Add(labelKnown);
            Controls.Add(labelUnknown);
            Controls.Add(groupBoxKnown);
            Controls.Add(groupBoxUnknown);
            Controls.Add(groupBoxChart);
            Name = "ResultForm";
            Text = "测试结果报告";
            groupBoxKnown.ResumeLayout(false);
            groupBoxUnknown.ResumeLayout(false);
            groupBoxChart.ResumeLayout(false);
            headerPanel.ResumeLayout(false);
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

        private void StartProgressAnimation(int targetValue)
        {
            animationProgress = 0;
            animationTimer.Start();

            var timer = new Timer();
            timer.Interval = 50;
            int currentValue = 0;
            timer.Tick += (s, e) =>
            {
                currentValue += 2;
                if (currentValue >= targetValue)
                {
                    currentValue = targetValue;
                    timer.Stop();
                    timer.Dispose();
                }
                progressBarAccuracyGradient.Value = currentValue;
                labelAccuracyValue.Text = $"{currentValue}%";
            };
            timer.Start();
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            animationProgress += 0.05;
            if (animationProgress >= 1)
            {
                animationProgress = 1;
                animationTimer.Stop();
            }
            Invalidate();
        }

        private void UpdateMotivationalMessage()
        {
            if (_accuracyRate >= 90)
            {
                labelMotivational.Text = "🌟 太棒了！你是最优秀的！";
                labelMotivational.ForeColor = ThemeHelper.Colors.Gold;
            }
            else if (_accuracyRate >= 70)
            {
                labelMotivational.Text = "👏 做得很好！继续加油！";
                labelMotivational.ForeColor = ThemeHelper.Colors.Success;
            }
            else if (_accuracyRate >= 50)
            {
                labelMotivational.Text = "💪 再接再厉，你一定可以！";
                labelMotivational.ForeColor = ThemeHelper.Colors.SoftBlue;
            }
            else
            {
                labelMotivational.Text = "📚 努力学习，进步就在眼前！";
                labelMotivational.ForeColor = ThemeHelper.Colors.Orange;
            }
        }

        private void Button_HoverEnter(object? sender, EventArgs e)
        {
            if (sender is Button button)
            {
                button.BackColor = Color.FromArgb(
                    Math.Min(255, button.BackColor.R + 30),
                    Math.Min(255, button.BackColor.G + 30),
                    Math.Min(255, button.BackColor.B + 30));
                button.Cursor = Cursors.Hand;
            }
        }

        private void Button_HoverLeave(object? sender, EventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Name == "buttonReview")
                    button.BackColor = ThemeHelper.Colors.Orange;
                else if (button.Name == "buttonBack")
                    button.BackColor = ThemeHelper.Colors.Success;
            }
        }

        private void UpdateChart()
        {
            try
            {
                var values = new[] { (double)_knownCount, (double)_unknownCount };
                var labels = new[] { "已掌握", "未掌握" };
                var colors = new[] { SuccessGreen, SoftBlue };
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
