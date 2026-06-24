using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using LearningAssistant.Forms.UserControls;

namespace LearningAssistant.Forms
{
    public partial class ResultForm : Form, IResultView, IThemeable
    {
        private readonly ILogger _logger;
        private readonly IThemeService? _themeService;
        private bool _disposed = false;
        private ChartControl chartControl;
        private int _knownCount = 0;
        private int _unknownCount = 0;
        private double _accuracyRate = 0.0;
        private System.Windows.Forms.Timer animationTimer;
        private double animationProgress = 0;
        private Label labelMotivational;
        private ProgressBar animatedProgressBar;
        private Panel _panelPrescription;
        private Label _labelPrescriptionTitle;
        private Label _labelPrescriptionContent;
        private Button _buttonJumpToWeakness;
        private string _weakCategory = string.Empty;
        private Panel _panelUnknownFooter;
        private Button _buttonRetryWrong;
        private Label _labelWrongCountDetail;
        private List<string> _wrongItemsList = new();



        public ResultForm(ILogger? logger = null, IThemeService? themeService = null)
        {
            InitializeComponent();
            _logger = logger;
            _themeService = themeService;
            MinimumSize = new Size(900, 600);

            _themeService?.RegisterThemeable(this);
        }



        #region IResultView Implementation

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Statistics { get; set; } = string.Empty;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string UnknownItems
        {
            set
            {
                listBoxUnknown.Items.Clear();
                _wrongItemsList.Clear();
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
                _labelWrongCountDetail.Text = $"共 {_unknownCount} 道错题";
                _buttonRetryWrong.Visible = _unknownCount > 0;
                UpdateChart();
            }
        }

        public event EventHandler? ReviewUnknownClicked;
        public event EventHandler? CloseClicked;
        public event EventHandler? JumpToWeaknessClicked;
        public event EventHandler<List<string>>? RetryWrongClicked;

        public void ShowMessage(string msg)
        {
            MessageBox.Show(msg);
        }

        public void CloseView()
        {
            Close();
        }

        public void ShowReport(string reportText)
        {
            labelTotal.Text = reportText;
            UpdateMotivationalMessage();
            GeneratePrescription();
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
            animationTimer = new System.Windows.Forms.Timer(components);
            _panelPrescription = new Panel();
            _labelPrescriptionTitle = new Label();
            _labelPrescriptionContent = new Label();
            _buttonJumpToWeakness = new Button();
            _panelUnknownFooter = new Panel();
            _buttonRetryWrong = new Button();
            _labelWrongCountDetail = new Label();
            groupBoxKnown.SuspendLayout();
            groupBoxUnknown.SuspendLayout();
            groupBoxChart.SuspendLayout();
            headerPanel.SuspendLayout();
            SuspendLayout();
            // 
            // labelAccuracy
            // 
            labelAccuracy.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            labelAccuracy.ForeColor = Color.FromArgb(33, 33, 33);
            labelAccuracy.Location = new Point(43, 112);
            labelAccuracy.Margin = new Padding(4, 0, 4, 0);
            labelAccuracy.Name = "labelAccuracy";
            labelAccuracy.Size = new Size(286, 42);
            labelAccuracy.TabIndex = 1;
            labelAccuracy.Text = "正确率";
            // 
            // labelTotal
            // 
            labelTotal.Font = new Font("微软雅黑", 12F);
            labelTotal.Location = new Point(43, 322);
            labelTotal.Margin = new Padding(4, 0, 4, 0);
            labelTotal.Name = "labelTotal";
            labelTotal.Size = new Size(286, 35);
            labelTotal.TabIndex = 3;
            labelTotal.Text = "总题数: 0";
            // 
            // labelKnown
            // 
            labelKnown.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            labelKnown.Location = new Point(43, 378);
            labelKnown.Margin = new Padding(4, 0, 4, 0);
            labelKnown.Name = "labelKnown";
            labelKnown.Size = new Size(357, 35);
            labelKnown.TabIndex = 4;
            labelKnown.Text = "✅ 已掌握: 0";
            // 
            // labelUnknown
            // 
            labelUnknown.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            labelUnknown.Location = new Point(43, 644);
            labelUnknown.Margin = new Padding(4, 0, 4, 0);
            labelUnknown.Name = "labelUnknown";
            labelUnknown.Size = new Size(357, 35);
            labelUnknown.TabIndex = 5;
            labelUnknown.Text = "📘 未掌握: 0";
            // 
            // labelTime
            // 
            labelTime.Location = new Point(0, 0);
            labelTime.Name = "labelTime";
            labelTime.Size = new Size(100, 23);
            labelTime.TabIndex = 0;
            // 
            // listBoxKnown
            // 
            listBoxKnown.BackColor = Color.White;
            listBoxKnown.BorderStyle = BorderStyle.None;
            listBoxKnown.Dock = DockStyle.Fill;
            listBoxKnown.FormattingEnabled = true;
            listBoxKnown.ItemHeight = 21;
            listBoxKnown.Location = new Point(7, 28);
            listBoxKnown.Margin = new Padding(4, 4, 4, 4);
            listBoxKnown.Name = "listBoxKnown";
            listBoxKnown.Size = new Size(529, 175);
            listBoxKnown.TabIndex = 0;
            // 
            // listBoxUnknown
            // 
            listBoxUnknown.BackColor = Color.White;
            listBoxUnknown.BorderStyle = BorderStyle.None;
            listBoxUnknown.Dock = DockStyle.Fill;
            listBoxUnknown.FormattingEnabled = true;
            listBoxUnknown.ItemHeight = 21;
            listBoxUnknown.Location = new Point(7, 28);
            listBoxUnknown.Margin = new Padding(4, 4, 4, 4);
            listBoxUnknown.Name = "listBoxUnknown";
            listBoxUnknown.Size = new Size(529, 175);
            listBoxUnknown.TabIndex = 0;
            // 
            // buttonReview
            // 
            buttonReview.FlatAppearance.BorderSize = 0;
            buttonReview.FlatStyle = FlatStyle.Flat;
            buttonReview.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            buttonReview.ForeColor = Color.White;
            buttonReview.Location = new Point(614, 910);
            buttonReview.Margin = new Padding(4, 4, 4, 4);
            buttonReview.Name = "buttonReview";
            buttonReview.Size = new Size(257, 63);
            buttonReview.TabIndex = 9;
            buttonReview.Text = "🔄 复习未掌握内容";
            buttonReview.UseVisualStyleBackColor = false;
            buttonReview.Click += ButtonReview_Click;
            buttonReview.MouseEnter += Button_HoverEnter;
            buttonReview.MouseLeave += Button_HoverLeave;
            // 
            // buttonBack
            // 
            buttonBack.FlatAppearance.BorderSize = 0;
            buttonBack.FlatStyle = FlatStyle.Flat;
            buttonBack.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            buttonBack.ForeColor = Color.White;
            buttonBack.Location = new Point(900, 910);
            buttonBack.Margin = new Padding(4, 4, 4, 4);
            buttonBack.Name = "buttonBack";
            buttonBack.Size = new Size(257, 63);
            buttonBack.TabIndex = 10;
            buttonBack.Text = "🏠 返回主界面";
            buttonBack.UseVisualStyleBackColor = false;
            buttonBack.Click += ButtonBack_Click;
            buttonBack.MouseEnter += Button_HoverEnter;
            buttonBack.MouseLeave += Button_HoverLeave;
            // 
            // groupBoxKnown
            // 
            groupBoxKnown.BackColor = Color.FromArgb(255, 250, 240);
            groupBoxKnown.Controls.Add(listBoxKnown);
            groupBoxKnown.FlatStyle = FlatStyle.Flat;
            groupBoxKnown.Location = new Point(43, 420);
            groupBoxKnown.Margin = new Padding(4, 4, 4, 4);
            groupBoxKnown.Name = "groupBoxKnown";
            groupBoxKnown.Padding = new Padding(7, 7, 7, 7);
            groupBoxKnown.Size = new Size(543, 210);
            groupBoxKnown.TabIndex = 7;
            groupBoxKnown.TabStop = false;
            // 
            // groupBoxUnknown
            // 
            groupBoxUnknown.BackColor = Color.FromArgb(245, 250, 255);
            groupBoxUnknown.Controls.Add(listBoxUnknown);
            groupBoxUnknown.FlatStyle = FlatStyle.Flat;
            groupBoxUnknown.Location = new Point(43, 686);
            groupBoxUnknown.Margin = new Padding(4, 4, 4, 4);
            groupBoxUnknown.Name = "groupBoxUnknown";
            groupBoxUnknown.Padding = new Padding(7, 7, 7, 7);
            groupBoxUnknown.Size = new Size(543, 210);
            groupBoxUnknown.TabIndex = 8;
            groupBoxUnknown.TabStop = false;
            // 
            // _panelUnknownFooter
            // 
            _panelUnknownFooter.BackColor = Color.FromArgb(245, 250, 255);
            _panelUnknownFooter.Location = new Point(43, 896);
            _panelUnknownFooter.Name = "_panelUnknownFooter";
            _panelUnknownFooter.Size = new Size(543, 45);
            _panelUnknownFooter.TabIndex = 17;
            // 
            // _labelWrongCountDetail
            // 
            _labelWrongCountDetail.Font = new Font("微软雅黑", 9F);
            _labelWrongCountDetail.ForeColor = Color.FromArgb(102, 102, 102);
            _labelWrongCountDetail.Location = new Point(15, 12);
            _labelWrongCountDetail.Name = "_labelWrongCountDetail";
            _labelWrongCountDetail.Size = new Size(200, 25);
            _labelWrongCountDetail.TabIndex = 0;
            _labelWrongCountDetail.Text = "共 0 道错题";
            _labelWrongCountDetail.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _buttonRetryWrong
            // 
            _buttonRetryWrong.BackColor = Color.FromArgb(244, 67, 54);
            _buttonRetryWrong.FlatAppearance.BorderSize = 0;
            _buttonRetryWrong.FlatStyle = FlatStyle.Flat;
            _buttonRetryWrong.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _buttonRetryWrong.ForeColor = Color.White;
            _buttonRetryWrong.Location = new Point(410, 7);
            _buttonRetryWrong.Name = "_buttonRetryWrong";
            _buttonRetryWrong.Size = new Size(120, 32);
            _buttonRetryWrong.TabIndex = 1;
            _buttonRetryWrong.Text = "🎯 重练错题";
            _buttonRetryWrong.UseVisualStyleBackColor = false;
            _buttonRetryWrong.Click += ButtonRetryWrong_Click;
            _buttonRetryWrong.MouseEnter += Button_HoverEnter;
            _buttonRetryWrong.MouseLeave += Button_HoverLeave;
            // 
            _panelUnknownFooter.Controls.Add(_buttonRetryWrong);
            _panelUnknownFooter.Controls.Add(_labelWrongCountDetail);
            // 
            // progressBarAccuracy
            // 
            progressBarAccuracy.Location = new Point(0, 0);
            progressBarAccuracy.Name = "progressBarAccuracy";
            progressBarAccuracy.Size = new Size(100, 23);
            progressBarAccuracy.TabIndex = 0;
            // 
            // labelTitle
            // 
            labelTitle.Font = new Font("微软雅黑", 20F, FontStyle.Bold);
            labelTitle.ForeColor = Color.White;
            labelTitle.Location = new Point(400, 14);
            labelTitle.Margin = new Padding(4, 0, 4, 0);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(429, 56);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "🎉 测试结果报告";
            labelTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // groupBoxChart
            // 
            groupBoxChart.BackColor = Color.White;
            groupBoxChart.Controls.Add(chartControl);
            groupBoxChart.FlatStyle = FlatStyle.Flat;
            groupBoxChart.Font = new Font("微软雅黑", 10F);
            groupBoxChart.Location = new Point(614, 98);
            groupBoxChart.Margin = new Padding(4, 4, 4, 4);
            groupBoxChart.Name = "groupBoxChart";
            groupBoxChart.Padding = new Padding(7, 7, 7, 7);
            groupBoxChart.Size = new Size(571, 320);
            groupBoxChart.TabIndex = 11;
            groupBoxChart.TabStop = false;
            groupBoxChart.Text = "📊 学习统计图表";
            // 
            // _panelPrescription
            // 
            _panelPrescription.BackColor = Color.FromArgb(255, 248, 235);
            _panelPrescription.Location = new Point(614, 430);
            _panelPrescription.Name = "_panelPrescription";
            _panelPrescription.Size = new Size(571, 200);
            _panelPrescription.TabIndex = 16;
            // 
            // _labelPrescriptionTitle
            // 
            _labelPrescriptionTitle.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _labelPrescriptionTitle.ForeColor = Color.FromArgb(102, 62, 0);
            _labelPrescriptionTitle.Location = new Point(20, 15);
            _labelPrescriptionTitle.Name = "_labelPrescriptionTitle";
            _labelPrescriptionTitle.Size = new Size(300, 30);
            _labelPrescriptionTitle.TabIndex = 0;
            _labelPrescriptionTitle.Text = "💊 智能学习处方";
            // 
            // _labelPrescriptionContent
            // 
            _labelPrescriptionContent.Font = new Font("微软雅黑", 9.5F);
            _labelPrescriptionContent.ForeColor = Color.FromArgb(80, 80, 80);
            _labelPrescriptionContent.Location = new Point(20, 50);
            _labelPrescriptionContent.Name = "_labelPrescriptionContent";
            _labelPrescriptionContent.Size = new Size(530, 100);
            _labelPrescriptionContent.TabIndex = 1;
            _labelPrescriptionContent.Text = "正在分析测试结果...";
            // 
            // _buttonJumpToWeakness
            // 
            _buttonJumpToWeakness.BackColor = Color.FromArgb(255, 152, 0);
            _buttonJumpToWeakness.FlatAppearance.BorderSize = 0;
            _buttonJumpToWeakness.FlatStyle = FlatStyle.Flat;
            _buttonJumpToWeakness.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _buttonJumpToWeakness.ForeColor = Color.White;
            _buttonJumpToWeakness.Location = new Point(20, 155);
            _buttonJumpToWeakness.Name = "_buttonJumpToWeakness";
            _buttonJumpToWeakness.Size = new Size(200, 35);
            _buttonJumpToWeakness.TabIndex = 2;
            _buttonJumpToWeakness.Text = "🎯 强化薄弱知识点";
            _buttonJumpToWeakness.UseVisualStyleBackColor = false;
            _buttonJumpToWeakness.Click += ButtonJumpToWeakness_Click;
            _buttonJumpToWeakness.MouseEnter += Button_HoverEnter;
            _buttonJumpToWeakness.MouseLeave += Button_HoverLeave;
            // 
            _panelPrescription.Controls.Add(_buttonJumpToWeakness);
            _panelPrescription.Controls.Add(_labelPrescriptionContent);
            _panelPrescription.Controls.Add(_labelPrescriptionTitle);
            // 
            // chartControl
            // 
            chartControl.BackColor = Color.White;
            chartControl.Dock = DockStyle.Fill;
            chartControl.Location = new Point(7, 30);
            chartControl.Margin = new Padding(4, 4, 4, 4);
            chartControl.Name = "chartControl";
            chartControl.Size = new Size(557, 495);
            chartControl.TabIndex = 0;
            // 
            // headerPanel
            // 
            headerPanel.Controls.Add(labelTitle);
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Location = new Point(0, 0);
            headerPanel.Margin = new Padding(4, 4, 4, 4);
            headerPanel.Name = "headerPanel";
            headerPanel.Size = new Size(1229, 84);
            headerPanel.TabIndex = 12;
            // 
            // labelMotivational
            // 
            labelMotivational.Font = new Font("微软雅黑", 11F, FontStyle.Italic);
            labelMotivational.Location = new Point(43, 266);
            labelMotivational.Margin = new Padding(4, 0, 4, 0);
            labelMotivational.Name = "labelMotivational";
            labelMotivational.Size = new Size(543, 35);
            labelMotivational.TabIndex = 15;
            labelMotivational.Text = "✨ 继续加油！";
            // 
            // animatedProgressBar
            // 
            animatedProgressBar.Location = new Point(0, 0);
            animatedProgressBar.Name = "animatedProgressBar";
            animatedProgressBar.Size = new Size(100, 23);
            animatedProgressBar.TabIndex = 0;
            // 
            // labelAccuracyValue
            // 
            labelAccuracyValue.Font = new Font("微软雅黑", 28F, FontStyle.Bold);
            labelAccuracyValue.Location = new Point(43, 147);
            labelAccuracyValue.Margin = new Padding(4, 0, 4, 0);
            labelAccuracyValue.Name = "labelAccuracyValue";
            labelAccuracyValue.Size = new Size(214, 70);
            labelAccuracyValue.TabIndex = 13;
            labelAccuracyValue.Text = "0%";
            // 
            // progressBarAccuracyGradient
            // 
            progressBarAccuracyGradient.Location = new Point(43, 224);
            progressBarAccuracyGradient.Margin = new Padding(4, 4, 4, 4);
            progressBarAccuracyGradient.Name = "progressBarAccuracyGradient";
            progressBarAccuracyGradient.Size = new Size(543, 35);
            progressBarAccuracyGradient.Style = ProgressBarStyle.Continuous;
            progressBarAccuracyGradient.TabIndex = 14;
            // 
            // animationTimer
            // 
            animationTimer.Interval = 30;
            animationTimer.Tick += AnimationTimer_Tick;
            // 
            // ResultForm
            // 
            AutoScaleDimensions = new SizeF(10F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1229, 994);
            Controls.Add(buttonReview);
            Controls.Add(buttonBack);
            Controls.Add(labelMotivational);
            Controls.Add(labelAccuracyValue);
            Controls.Add(progressBarAccuracyGradient);
            Controls.Add(headerPanel);
            Controls.Add(_panelPrescription);
            Controls.Add(labelAccuracy);
            Controls.Add(labelTotal);
            Controls.Add(labelKnown);
            Controls.Add(labelUnknown);
            Controls.Add(groupBoxKnown);
            Controls.Add(groupBoxUnknown);
            Controls.Add(_panelUnknownFooter);
            Controls.Add(groupBoxChart);
            Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            Margin = new Padding(4, 4, 4, 4);
            Name = "ResultForm";
            Text = "测试结果报告";
            groupBoxKnown.ResumeLayout(false);
            groupBoxUnknown.ResumeLayout(false);
            groupBoxChart.ResumeLayout(false);
            headerPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        #region Event Handlers

        private void ButtonReview_Click(object? sender, EventArgs e)
        {
            ReviewUnknownClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonJumpToWeakness_Click(object? sender, EventArgs e)
        {
            JumpToWeaknessClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonRetryWrong_Click(object? sender, EventArgs e)
        {
            if (_wrongItemsList.Count == 0)
            {
                for (int i = 0; i < listBoxUnknown.Items.Count; i++)
                {
                    _wrongItemsList.Add(listBoxUnknown.Items[i]?.ToString() ?? string.Empty);
                }
            }
            RetryWrongClicked?.Invoke(this, _wrongItemsList);
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

            var timer = new System.Windows.Forms.Timer();
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

        private void GeneratePrescription()
        {
            try
            {
                int totalCount = _knownCount + _unknownCount;
                if (totalCount == 0)
                {
                    _labelPrescriptionContent.Text = "暂无测试数据，无法生成学习处方。";
                    _buttonJumpToWeakness.Visible = false;
                    return;
                }

                var suggestions = new List<string>();

                if (_accuracyRate >= 90)
                {
                    suggestions.Add("✅ 知识掌握非常扎实，建议挑战更高难度内容。");
                    suggestions.Add("📖 可以尝试学习新知识，拓展知识边界。");
                    _weakCategory = "进阶学习";
                }
                else if (_accuracyRate >= 70)
                {
                    suggestions.Add("👍 整体掌握良好，重点巩固薄弱知识点。");
                    suggestions.Add("🔄 建议对未掌握内容进行间隔重复复习。");
                    _weakCategory = "巩固提升";
                }
                else if (_accuracyRate >= 50)
                {
                    suggestions.Add("📚 基础知识需要加强，建议系统复习。");
                    suggestions.Add("🎯 重点突破高频易错知识点。");
                    _weakCategory = "基础强化";
                }
                else
                {
                    suggestions.Add("⚠️ 需要从头梳理知识体系，夯实基础。");
                    suggestions.Add("📝 建议放慢学习节奏，确保每个知识点都掌握。");
                    _weakCategory = "基础夯实";
                }

                if (_unknownCount > 0)
                {
                    suggestions.Add($"❌ 本次有 {_unknownCount} 道题未掌握，已加入错题本。");
                }

                _labelPrescriptionContent.Text = string.Join("\n", suggestions);
                _buttonJumpToWeakness.Visible = true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "生成学习处方失败");
                _labelPrescriptionContent.Text = "学习处方生成中遇到问题，请稍后重试。";
                _buttonJumpToWeakness.Visible = false;
            }
        }

        private void Button_HoverEnter(object? sender, EventArgs e)
        {
            if (sender is Button button)
            {
                button.BackColor = Color.FromArgb(
                    Math.Min(255, (int)button.BackColor.R + 30),
                    Math.Min(255, (int)button.BackColor.G + 30),
                    Math.Min(255, (int)button.BackColor.B + 30));
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
                var colors = new[] { ThemeHelper.Colors.Success, ThemeHelper.Colors.SoftBlue };
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
                _themeService?.UnregisterThemeable(this);
                if (components != null)
                {
                    components.Dispose();
                }
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;

            if (headerPanel != null)
            {
                headerPanel.BackColor = colors.Primary;
            }

            if (labelTitle != null)
            {
                labelTitle.ForeColor = Color.White;
            }

            if (groupBoxChart != null)
            {
                groupBoxChart.BackColor = colors.Surface;
                groupBoxChart.ForeColor = colors.TextPrimary;
            }

            if (groupBoxKnown != null)
            {
                groupBoxKnown.BackColor = colors.Surface;
                groupBoxKnown.ForeColor = colors.TextPrimary;
            }

            if (groupBoxUnknown != null)
            {
                groupBoxUnknown.BackColor = colors.Surface;
                groupBoxUnknown.ForeColor = colors.TextPrimary;
            }

            if (listBoxKnown != null)
            {
                listBoxKnown.BackColor = colors.Surface;
                listBoxKnown.ForeColor = colors.TextPrimary;
            }

            if (listBoxUnknown != null)
            {
                listBoxUnknown.BackColor = colors.Surface;
                listBoxUnknown.ForeColor = colors.TextPrimary;
            }

            if (chartControl != null)
            {
                chartControl.BackColor = colors.Surface;
            }

            if (progressBarAccuracyGradient != null)
            {
                progressBarAccuracyGradient.BackColor = colors.Surface;
            }

            foreach (Control control in Controls)
            {
                ApplyThemeToControl(control, colors);
            }
        }

        private void ApplyThemeToControl(Control control, ThemeColors colors)
        {
            if (control is Label label)
            {
                if (label.Name == "labelTitle" || label.Name == "labelMotivational")
                {
                    // 标题和鼓励语使用专属颜色
                    return;
                }
                label.ForeColor = colors.TextPrimary;
            }
            else if (control is ProgressBar progressBar)
            {
                progressBar.BackColor = colors.Surface;
            }
            else if (control is GroupBox groupBox)
            {
                groupBox.BackColor = colors.Surface;
                groupBox.ForeColor = colors.TextPrimary;
            }
            else if (control is ListBox listBox)
            {
                listBox.BackColor = colors.Surface;
                listBox.ForeColor = colors.TextPrimary;
            }
            else if (control is Panel panel)
            {
                if (panel.Name == "headerPanel")
                {
                    panel.BackColor = colors.Primary;
                }
                else
                {
                    panel.BackColor = colors.Background;
                }
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child, colors);
            }
        }
    }
}
