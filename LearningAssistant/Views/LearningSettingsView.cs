using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LearningAssistant.Views
{
    /// <summary>
    /// 学习设置视图 - 右侧设置面板（模式/排序/语言/操作按钮）
    /// 注意：所有事件由 LearningForm 统一订阅，本类仅负责 UI 创建
    /// </summary>
    public class LearningSettingsView : UserControl
    {
        #region Controls

        private Panel _panelConfig = null!;
        private Label _labelConfigTitle = null!;

        // 模式选择
        private GroupBox _groupBoxMode = null!;
        private RadioButton _radioStudyMode = null!;
        private RadioButton _radioQuickMode = null!;

        // 排序选择
        private GroupBox _groupBoxSort = null!;
        private RadioButton _radioSequential = null!;
        private RadioButton _radioRandom = null!;

        // 语言选择
        private GroupBox _groupBoxLanguage = null!;
        private RadioButton _radioChinese = null!;
        private RadioButton _radioEnglish = null!;

        // 子类别
        private Label _labelSubCategory = null!;
        private ComboBox _comboBoxSubCategory = null!;

        // 操作按钮
        private Button _buttonOpenStatistics = null!;
        private Button _buttonExportErrorBook = null!;

        // Quiz 模式
        private Panel _panelQuizMode = null!;
        private Button _buttonQuizMode = null!;
        private Label _labelQuizHint = null!;

        // 主题切换
        private Button _buttonThemeToggle = null!;

        // 发音范围
        private GroupBox _groupBoxPronunciationScope = null!;
        private RadioButton _radioOriginal = null!;
        private RadioButton _radioExplanation = null!;
        private RadioButton _radioBoth = null!;

        // 设置面板容器
        private FlowLayoutPanel _settingsFlowLayoutPanel = null!;

        #endregion

        #region Public Controls

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Panel PanelConfig => _panelConfig;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RadioButton RadioStudyMode => _radioStudyMode;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RadioButton RadioQuickMode => _radioQuickMode;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RadioButton RadioSequential => _radioSequential;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RadioButton RadioRandom => _radioRandom;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RadioButton RadioChinese => _radioChinese;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RadioButton RadioEnglish => _radioEnglish;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ComboBox ComboBoxSubCategory => _comboBoxSubCategory;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonOpenStatistics => _buttonOpenStatistics;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonExportErrorBook => _buttonExportErrorBook;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonQuizMode => _buttonQuizMode;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonThemeToggle => _buttonThemeToggle;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public GroupBox GroupBoxPronunciationScope => _groupBoxPronunciationScope;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public GroupBox GroupBoxMode => _groupBoxMode;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public GroupBox GroupBoxSort => _groupBoxSort;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public GroupBox GroupBoxLanguage => _groupBoxLanguage;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label LabelSubCategory => _labelSubCategory;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FlowLayoutPanel SettingsFlowLayoutPanel => _settingsFlowLayoutPanel;

        #endregion

        #region Initialization

        public LearningSettingsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _panelConfig = new Panel();
            _labelConfigTitle = new Label();
            _groupBoxMode = new GroupBox();
            _radioStudyMode = new RadioButton();
            _radioQuickMode = new RadioButton();
            _groupBoxSort = new GroupBox();
            _radioSequential = new RadioButton();
            _radioRandom = new RadioButton();
            _groupBoxLanguage = new GroupBox();
            _radioChinese = new RadioButton();
            _radioEnglish = new RadioButton();
            _labelSubCategory = new Label();
            _comboBoxSubCategory = new ComboBox();
            _buttonOpenStatistics = new Button();
            _buttonExportErrorBook = new Button();
            _panelQuizMode = new Panel();
            _buttonQuizMode = new Button();
            _labelQuizHint = new Label();
            _buttonThemeToggle = new Button();
            _groupBoxPronunciationScope = new GroupBox();
            _radioOriginal = new RadioButton();
            _radioExplanation = new RadioButton();
            _radioBoth = new RadioButton();
            _settingsFlowLayoutPanel = new FlowLayoutPanel();

            SuspendLayout();

            //
            // _panelConfig
            //
            _panelConfig.BackColor = Color.FromArgb(245, 245, 250);
            _panelConfig.BorderStyle = BorderStyle.FixedSingle;
            _panelConfig.Dock = DockStyle.Fill;
            _panelConfig.Location = new Point(0, 0);
            _panelConfig.Name = "_panelConfig";
            _panelConfig.Size = new Size(214, 838);
            _panelConfig.TabIndex = 19;

            //
            // _labelConfigTitle
            //
            _labelConfigTitle.BackColor = Color.FromArgb(103, 58, 183);
            _labelConfigTitle.Dock = DockStyle.Top;
            _labelConfigTitle.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _labelConfigTitle.ForeColor = Color.White;
            _labelConfigTitle.Location = new Point(0, 0);
            _labelConfigTitle.Name = "_labelConfigTitle";
            _labelConfigTitle.Size = new Size(212, 35);
            _labelConfigTitle.TabIndex = 0;
            _labelConfigTitle.Text = "⚙️ 设置";
            _labelConfigTitle.TextAlign = ContentAlignment.MiddleCenter;

            //
            // _groupBoxMode
            //
            _groupBoxMode.Controls.Add(_radioStudyMode);
            _groupBoxMode.Controls.Add(_radioQuickMode);
            _groupBoxMode.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _groupBoxMode.ForeColor = Color.FromArgb(60, 80, 100);
            _groupBoxMode.Location = new Point(10, 45);
            _groupBoxMode.Name = "_groupBoxMode";
            _groupBoxMode.Size = new Size(180, 68);
            _groupBoxMode.TabIndex = 1;
            _groupBoxMode.TabStop = false;
            _groupBoxMode.Text = "学习模式";

            //
            // _radioStudyMode
            //
            _radioStudyMode.AutoSize = true;
            _radioStudyMode.Checked = true;
            _radioStudyMode.Font = new Font("微软雅黑", 9F);
            _radioStudyMode.ForeColor = Color.FromArgb(70, 90, 110);
            _radioStudyMode.Location = new Point(15, 25);
            _radioStudyMode.Name = "_radioStudyMode";
            _radioStudyMode.Size = new Size(70, 21);
            _radioStudyMode.TabIndex = 0;
            _radioStudyMode.TabStop = true;
            _radioStudyMode.Text = "📝 练习";

            //
            // _radioQuickMode
            //
            _radioQuickMode.AutoSize = true;
            _radioQuickMode.Font = new Font("微软雅黑", 9F);
            _radioQuickMode.ForeColor = Color.FromArgb(70, 90, 110);
            _radioQuickMode.Location = new Point(90, 25);
            _radioQuickMode.Name = "_radioQuickMode";
            _radioQuickMode.Size = new Size(70, 21);
            _radioQuickMode.TabIndex = 1;
            _radioQuickMode.Text = "🔄 复习";

            //
            // _groupBoxSort
            //
            _groupBoxSort.Controls.Add(_radioSequential);
            _groupBoxSort.Controls.Add(_radioRandom);
            _groupBoxSort.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _groupBoxSort.ForeColor = Color.FromArgb(60, 80, 100);
            _groupBoxSort.Location = new Point(10, 125);
            _groupBoxSort.Name = "_groupBoxSort";
            _groupBoxSort.Size = new Size(180, 68);
            _groupBoxSort.TabIndex = 2;
            _groupBoxSort.TabStop = false;
            _groupBoxSort.Text = "排序方式";

            //
            // _radioSequential
            //
            _radioSequential.AutoSize = true;
            _radioSequential.Checked = true;
            _radioSequential.Font = new Font("微软雅黑", 9F);
            _radioSequential.ForeColor = Color.FromArgb(70, 90, 110);
            _radioSequential.Location = new Point(15, 25);
            _radioSequential.Name = "_radioSequential";
            _radioSequential.Size = new Size(60, 21);
            _radioSequential.TabIndex = 0;
            _radioSequential.TabStop = true;
            _radioSequential.Text = "📖 顺序";

            //
            // _radioRandom
            //
            _radioRandom.AutoSize = true;
            _radioRandom.Font = new Font("微软雅黑", 9F);
            _radioRandom.ForeColor = Color.FromArgb(70, 90, 110);
            _radioRandom.Location = new Point(90, 25);
            _radioRandom.Name = "_radioRandom";
            _radioRandom.Size = new Size(60, 21);
            _radioRandom.TabIndex = 1;
            _radioRandom.Text = "🎲 随机";

            //
            // _groupBoxLanguage
            //
            _groupBoxLanguage.Controls.Add(_radioChinese);
            _groupBoxLanguage.Controls.Add(_radioEnglish);
            _groupBoxLanguage.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _groupBoxLanguage.ForeColor = Color.FromArgb(60, 80, 100);
            _groupBoxLanguage.Location = new Point(10, 205);
            _groupBoxLanguage.Name = "_groupBoxLanguage";
            _groupBoxLanguage.Size = new Size(180, 68);
            _groupBoxLanguage.TabIndex = 3;
            _groupBoxLanguage.TabStop = false;
            _groupBoxLanguage.Text = "学习语言";

            //
            // _radioChinese
            //
            _radioChinese.AutoSize = true;
            _radioChinese.Checked = true;
            _radioChinese.Font = new Font("微软雅黑", 9F);
            _radioChinese.ForeColor = Color.FromArgb(70, 90, 110);
            _radioChinese.Location = new Point(15, 25);
            _radioChinese.Name = "_radioChinese";
            _radioChinese.Size = new Size(60, 21);
            _radioChinese.TabIndex = 0;
            _radioChinese.TabStop = true;
            _radioChinese.Text = "🇨🇳 中文";

            //
            // _radioEnglish
            //
            _radioEnglish.AutoSize = true;
            _radioEnglish.Font = new Font("微软雅黑", 9F);
            _radioEnglish.ForeColor = Color.FromArgb(70, 90, 110);
            _radioEnglish.Location = new Point(90, 25);
            _radioEnglish.Name = "_radioEnglish";
            _radioEnglish.Size = new Size(60, 21);
            _radioEnglish.TabIndex = 1;
            _radioEnglish.Text = "🇬🇧 英文";

            //
            // _labelSubCategory
            //
            _labelSubCategory.AutoSize = true;
            _labelSubCategory.Font = new Font("微软雅黑", 9F);
            _labelSubCategory.ForeColor = Color.FromArgb(80, 100, 120);
            _labelSubCategory.Location = new Point(10, 285);
            _labelSubCategory.Name = "_labelSubCategory";
            _labelSubCategory.Size = new Size(52, 17);
            _labelSubCategory.TabIndex = 4;
            _labelSubCategory.Text = "子类:";

            //
            // _comboBoxSubCategory
            //
            _comboBoxSubCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            _comboBoxSubCategory.Font = new Font("微软雅黑", 9F);
            _comboBoxSubCategory.FormattingEnabled = true;
            _comboBoxSubCategory.Location = new Point(10, 305);
            _comboBoxSubCategory.Name = "_comboBoxSubCategory";
            _comboBoxSubCategory.Size = new Size(170, 25);
            _comboBoxSubCategory.TabIndex = 5;

            //
            // _buttonOpenStatistics
            //
            _buttonOpenStatistics.BackColor = Color.FromArgb(255, 152, 0);
            _buttonOpenStatistics.FlatAppearance.BorderSize = 0;
            _buttonOpenStatistics.FlatAppearance.MouseDownBackColor = Color.FromArgb(245, 142, 0);
            _buttonOpenStatistics.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 162, 20);
            _buttonOpenStatistics.FlatStyle = FlatStyle.Flat;
            _buttonOpenStatistics.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _buttonOpenStatistics.ForeColor = Color.White;
            _buttonOpenStatistics.Location = new Point(10, 340);
            _buttonOpenStatistics.Name = "_buttonOpenStatistics";
            _buttonOpenStatistics.Size = new Size(180, 40);
            _buttonOpenStatistics.TabIndex = 6;
            _buttonOpenStatistics.Text = "📊 学习统计";
            _buttonOpenStatistics.UseVisualStyleBackColor = false;

            //
            // _buttonExportErrorBook
            //
            _buttonExportErrorBook.BackColor = Color.FromArgb(244, 67, 54);
            _buttonExportErrorBook.FlatAppearance.BorderSize = 0;
            _buttonExportErrorBook.FlatAppearance.MouseDownBackColor = Color.FromArgb(234, 57, 44);
            _buttonExportErrorBook.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 77, 64);
            _buttonExportErrorBook.FlatStyle = FlatStyle.Flat;
            _buttonExportErrorBook.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _buttonExportErrorBook.ForeColor = Color.White;
            _buttonExportErrorBook.Location = new Point(10, 390);
            _buttonExportErrorBook.Name = "_buttonExportErrorBook";
            _buttonExportErrorBook.Size = new Size(180, 40);
            _buttonExportErrorBook.TabIndex = 7;
            _buttonExportErrorBook.Text = "❌ 导出错题本";
            _buttonExportErrorBook.UseVisualStyleBackColor = false;

            //
            // _panelQuizMode
            //
            _panelQuizMode.Controls.Add(_buttonQuizMode);
            _panelQuizMode.Controls.Add(_labelQuizHint);
            _panelQuizMode.Location = new Point(10, 440);
            _panelQuizMode.Name = "_panelQuizMode";
            _panelQuizMode.Size = new Size(180, 80);
            _panelQuizMode.TabIndex = 8;

            //
            // _buttonQuizMode
            //
            _buttonQuizMode.BackColor = Color.FromArgb(156, 39, 176);
            _buttonQuizMode.FlatAppearance.BorderSize = 0;
            _buttonQuizMode.FlatAppearance.MouseDownBackColor = Color.FromArgb(146, 29, 166);
            _buttonQuizMode.FlatAppearance.MouseOverBackColor = Color.FromArgb(166, 49, 186);
            _buttonQuizMode.FlatStyle = FlatStyle.Flat;
            _buttonQuizMode.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _buttonQuizMode.ForeColor = Color.White;
            _buttonQuizMode.Location = new Point(10, 10);
            _buttonQuizMode.Name = "_buttonQuizMode";
            _buttonQuizMode.Size = new Size(160, 35);
            _buttonQuizMode.TabIndex = 0;
            _buttonQuizMode.Text = "🎮 答题模式";
            _buttonQuizMode.UseVisualStyleBackColor = false;

            //
            // _labelQuizHint
            //
            _labelQuizHint.Font = new Font("微软雅黑", 8.5F);
            _labelQuizHint.ForeColor = Color.FromArgb(139, 119, 101);
            _labelQuizHint.Location = new Point(10, 50);
            _labelQuizHint.Name = "_labelQuizHint";
            _labelQuizHint.Size = new Size(160, 25);
            _labelQuizHint.TabIndex = 1;
            _labelQuizHint.Text = "先隐藏答案，测试自己";

            //
            // _buttonThemeToggle
            //
            _buttonThemeToggle.BackColor = Color.FromArgb(103, 58, 183);
            _buttonThemeToggle.FlatAppearance.BorderSize = 0;
            _buttonThemeToggle.FlatAppearance.MouseDownBackColor = Color.FromArgb(93, 48, 173);
            _buttonThemeToggle.FlatAppearance.MouseOverBackColor = Color.FromArgb(113, 68, 193);
            _buttonThemeToggle.FlatStyle = FlatStyle.Flat;
            _buttonThemeToggle.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _buttonThemeToggle.ForeColor = Color.White;
            _buttonThemeToggle.Location = new Point(10, 760);
            _buttonThemeToggle.Name = "_buttonThemeToggle";
            _buttonThemeToggle.Size = new Size(180, 40);
            _buttonThemeToggle.TabIndex = 10;
            _buttonThemeToggle.Text = "🌙 深色模式";
            _buttonThemeToggle.UseVisualStyleBackColor = false;

            //
            // _settingsFlowLayoutPanel
            //
            _settingsFlowLayoutPanel.Dock = DockStyle.Fill;
            _settingsFlowLayoutPanel.FlowDirection = FlowDirection.TopDown;
            _settingsFlowLayoutPanel.Location = new Point(0, 0);
            _settingsFlowLayoutPanel.Name = "_settingsFlowLayoutPanel";
            _settingsFlowLayoutPanel.Padding = new Padding(5);
            _settingsFlowLayoutPanel.Size = new Size(200, 51);
            _settingsFlowLayoutPanel.TabIndex = 0;
            _settingsFlowLayoutPanel.WrapContents = false;

            Controls.Add(_panelConfig);

            ResumeLayout(false);
        }

        #endregion

        #region Public API

        /// <summary>应用主题色</summary>
        public void ApplyTheme(Color backColor, Color headerColor)
        {
            _panelConfig.BackColor = backColor;
            _labelConfigTitle.BackColor = headerColor;
            _buttonThemeToggle.BackColor = headerColor;
        }

        /// <summary>设置子类别列表</summary>
        public void SetSubCategories(List<string> categories)
        {
            _comboBoxSubCategory.Items.Clear();
            _comboBoxSubCategory.Items.AddRange(categories.ToArray());
            if (_comboBoxSubCategory.Items.Count > 0)
                _comboBoxSubCategory.SelectedIndex = 0;
        }

        /// <summary>设置学习模式</summary>
        public void SetStudyMode(bool isStudyMode)
        {
            if (isStudyMode)
                _radioStudyMode.Checked = true;
            else
                _radioQuickMode.Checked = true;
        }

        /// <summary>设置排序方式</summary>
        public void SetSortOrder(bool isSequential)
        {
            if (isStudyMode)
                _radioSequential.Checked = true;
            else
                _radioRandom.Checked = true;
        }

        /// <summary>设置语言</summary>
        public void SetLanguage(bool isChinese)
        {
            if (isChinese)
                _radioChinese.Checked = true;
            else
                _radioEnglish.Checked = true;
        }

        /// <summary>设置主题按钮文本</summary>
        public void SetThemeButtonText(string text) => _buttonThemeToggle.Text = text;

        #endregion
    }
}
