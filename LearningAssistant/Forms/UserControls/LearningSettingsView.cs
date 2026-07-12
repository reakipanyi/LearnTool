using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls
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

        // 学科选择
        private GroupBox _groupBoxSubject = null!;
        private ComboBox _comboBoxSubject = null!;

        // 子类别
        private Label _labelSubCategory = null!;
        private ComboBox _comboBoxSubCategory = null!;

        // 玩家选择
        private Label _labelUser = null!;
        private ComboBox _comboBoxUser = null!;

        // Quiz 模式
        private Panel _panelQuizMode = null!;
        private Button _buttonShowAnswer = null!;
        private Label _labelQuizHint = null!;

        // 主题切换
        private Button _buttonThemeToggle = null!;

        // 发音范围
        private RadioButton _radioOriginal = null!;
        private RadioButton _radioExplanation = null!;
        private RadioButton _radioBoth = null!;

        // 自动朗读
        private CheckBox _checkBoxVoice = null!;

        // 发音范围面板
        private FlowLayoutPanel _pronunciationFlowLayoutPanel = null!;

        private SpeedSelectorControl _speedSelector = null!;

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
        public ComboBox ComboBoxSubject => _comboBoxSubject;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ComboBox ComboBoxSubCategory => _comboBoxSubCategory;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ComboBox ComboBoxUser => _comboBoxUser;

        public event EventHandler? UserChanged;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonShowAnswer => _buttonShowAnswer;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Button ButtonThemeToggle => _buttonThemeToggle;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public GroupBox GroupBoxMode => _groupBoxMode;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public GroupBox GroupBoxSort => _groupBoxSort;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public GroupBox GroupBoxSubject => _groupBoxSubject;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label LabelSubCategory => _labelSubCategory;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Panel PanelQuizMode => _panelQuizMode;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label LabelQuizHint => _labelQuizHint;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CheckBox CheckBoxVoice => _checkBoxVoice;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FlowLayoutPanel PronunciationFlowLayoutPanel => _pronunciationFlowLayoutPanel;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RadioButton RadioOriginal => _radioOriginal;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RadioButton RadioExplanation => _radioExplanation;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RadioButton RadioBoth => _radioBoth;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SpeedSelectorControl SpeedSelector => _speedSelector;

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
            _groupBoxSubject = new GroupBox();
            _comboBoxSubject = new ComboBox();
            _labelSubCategory = new Label();
            _comboBoxSubCategory = new ComboBox();
            _labelUser = new Label();
            _comboBoxUser = new ComboBox();
            _checkBoxVoice = new CheckBox();
            _pronunciationFlowLayoutPanel = new FlowLayoutPanel();
            _radioOriginal = new RadioButton();
            _radioExplanation = new RadioButton();
            _radioBoth = new RadioButton();
            _panelQuizMode = new Panel();
            _buttonShowAnswer = new Button();
            _labelQuizHint = new Label();
            _buttonThemeToggle = new Button();
            _speedSelector = new SpeedSelectorControl();
            _panelConfig.SuspendLayout();
            _groupBoxMode.SuspendLayout();
            _groupBoxSort.SuspendLayout();
            _groupBoxSubject.SuspendLayout();
            _pronunciationFlowLayoutPanel.SuspendLayout();
            _panelQuizMode.SuspendLayout();
            SuspendLayout();
            // 
            // _panelConfig
            // 
            _panelConfig.BackColor = Color.FromArgb(245, 245, 250);
            _panelConfig.BorderStyle = BorderStyle.FixedSingle;
            _panelConfig.Controls.Add(_labelConfigTitle);
            _panelConfig.Controls.Add(_groupBoxMode);
            _panelConfig.Controls.Add(_groupBoxSort);
            _panelConfig.Controls.Add(_groupBoxSubject);
            _panelConfig.Controls.Add(_labelSubCategory);
            _panelConfig.Controls.Add(_comboBoxSubCategory);
            _panelConfig.Controls.Add(_labelUser);
            _panelConfig.Controls.Add(_comboBoxUser);
            _panelConfig.Controls.Add(_checkBoxVoice);
            _panelConfig.Controls.Add(_pronunciationFlowLayoutPanel);
            _panelConfig.Controls.Add(_speedSelector);
            _panelConfig.Controls.Add(_panelQuizMode);
            _panelConfig.Controls.Add(_buttonThemeToggle);
            _panelConfig.Dock = DockStyle.Fill;
            _panelConfig.Location = new Point(0, 0);
            _panelConfig.Name = "_panelConfig";
            _panelConfig.Size = new Size(220, 837);
            _panelConfig.TabIndex = 0;
            // 
            // _labelConfigTitle
            // 
            _labelConfigTitle.BackColor = Color.FromArgb(103, 58, 183);
            _labelConfigTitle.Dock = DockStyle.Top;
            _labelConfigTitle.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            _labelConfigTitle.ForeColor = Color.White;
            _labelConfigTitle.Location = new Point(0, 0);
            _labelConfigTitle.Name = "_labelConfigTitle";
            _labelConfigTitle.Size = new Size(218, 40);
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
            _groupBoxMode.Location = new Point(10, 51);
            _groupBoxMode.Name = "_groupBoxMode";
            _groupBoxMode.Size = new Size(180, 77);
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
            _radioStudyMode.Location = new Point(15, 28);
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
            _radioQuickMode.Location = new Point(90, 28);
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
            _groupBoxSort.Location = new Point(10, 147);
            _groupBoxSort.Name = "_groupBoxSort";
            _groupBoxSort.Size = new Size(180, 85);
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
            _radioSequential.Location = new Point(15, 28);
            _radioSequential.Name = "_radioSequential";
            _radioSequential.Size = new Size(68, 21);
            _radioSequential.TabIndex = 0;
            _radioSequential.TabStop = true;
            _radioSequential.Text = "📋 顺序";
            // 
            // _radioRandom
            // 
            _radioRandom.AutoSize = true;
            _radioRandom.Font = new Font("微软雅黑", 9F);
            _radioRandom.ForeColor = Color.FromArgb(70, 90, 110);
            _radioRandom.Location = new Point(90, 28);
            _radioRandom.Name = "_radioRandom";
            _radioRandom.Size = new Size(69, 21);
            _radioRandom.TabIndex = 1;
            _radioRandom.Text = "🎲 随机";
            // 
            // _groupBoxSubject
            // 
            _groupBoxSubject.Controls.Add(_comboBoxSubject);
            _groupBoxSubject.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _groupBoxSubject.ForeColor = Color.FromArgb(60, 80, 100);
            _groupBoxSubject.Location = new Point(10, 244);
            _groupBoxSubject.Name = "_groupBoxSubject";
            _groupBoxSubject.Size = new Size(180, 70);
            _groupBoxSubject.TabIndex = 3;
            _groupBoxSubject.TabStop = false;
            _groupBoxSubject.Text = "学科";
            // 
            // _comboBoxSubject
            // 
            _comboBoxSubject.DropDownStyle = ComboBoxStyle.DropDownList;
            _comboBoxSubject.Font = new Font("微软雅黑", 9F);
            _comboBoxSubject.FormattingEnabled = true;
            _comboBoxSubject.Location = new Point(15, 28);
            _comboBoxSubject.Name = "_comboBoxSubject";
            _comboBoxSubject.Size = new Size(150, 25);
            _comboBoxSubject.TabIndex = 0;
            // 
            // _labelSubCategory
            // 
            _labelSubCategory.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _labelSubCategory.ForeColor = Color.FromArgb(70, 90, 110);
            _labelSubCategory.Location = new Point(10, 340);
            _labelSubCategory.Name = "_labelSubCategory";
            _labelSubCategory.Size = new Size(50, 26);
            _labelSubCategory.TabIndex = 4;
            _labelSubCategory.Text = "📖";
            _labelSubCategory.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _comboBoxSubCategory
            // 
            _comboBoxSubCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            _comboBoxSubCategory.Font = new Font("微软雅黑", 9F);
            _comboBoxSubCategory.FormattingEnabled = true;
            _comboBoxSubCategory.Location = new Point(65, 337);
            _comboBoxSubCategory.Name = "_comboBoxSubCategory";
            _comboBoxSubCategory.Size = new Size(125, 25);
            _comboBoxSubCategory.TabIndex = 5;
            //
            // _labelUser
            //
            _labelUser.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _labelUser.ForeColor = Color.FromArgb(70, 90, 110);
            _labelUser.Location = new Point(10, 463);
            _labelUser.Name = "_labelUser";
            _labelUser.Size = new Size(50, 26);
            _labelUser.TabIndex = 6;
            _labelUser.Text = "👤";
            _labelUser.TextAlign = ContentAlignment.MiddleLeft;
            //
            // _comboBoxUser
            //
            _comboBoxUser.DropDownStyle = ComboBoxStyle.DropDownList;
            _comboBoxUser.Font = new Font("微软雅黑", 9F);
            _comboBoxUser.FormattingEnabled = true;
            _comboBoxUser.Location = new Point(65, 460);
            _comboBoxUser.Name = "_comboBoxUser";
            _comboBoxUser.Size = new Size(125, 25);
            _comboBoxUser.TabIndex = 7;
            _comboBoxUser.SelectedIndexChanged += ComboBoxUser_SelectedIndexChanged;
            //
            // _checkBoxVoice
            // 
            _checkBoxVoice.Checked = true;
            _checkBoxVoice.CheckState = CheckState.Checked;
            _checkBoxVoice.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _checkBoxVoice.ForeColor = Color.FromArgb(70, 90, 110);
            _checkBoxVoice.Location = new Point(10, 498);
            _checkBoxVoice.Name = "_checkBoxVoice";
            _checkBoxVoice.Size = new Size(100, 25);
            _checkBoxVoice.TabIndex = 8;
            _checkBoxVoice.Text = "🔊 自动朗读";
            // 
            // _pronunciationFlowLayoutPanel
            // 
            _pronunciationFlowLayoutPanel.Controls.Add(_radioOriginal);
            _pronunciationFlowLayoutPanel.Controls.Add(_radioExplanation);
            _pronunciationFlowLayoutPanel.Controls.Add(_radioBoth);
            _pronunciationFlowLayoutPanel.Location = new Point(10, 529);
            _pronunciationFlowLayoutPanel.Name = "_pronunciationFlowLayoutPanel";
            _pronunciationFlowLayoutPanel.Size = new Size(126, 71);
            _pronunciationFlowLayoutPanel.TabIndex = 9;
            // 
            // _radioOriginal
            // 
            _radioOriginal.AutoSize = true;
            _radioOriginal.Checked = true;
            _radioOriginal.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _radioOriginal.ForeColor = Color.FromArgb(70, 90, 110);
            _radioOriginal.Location = new Point(3, 3);
            _radioOriginal.Name = "_radioOriginal";
            _radioOriginal.Size = new Size(50, 21);
            _radioOriginal.TabIndex = 0;
            _radioOriginal.TabStop = true;
            _radioOriginal.Text = "原文";
            // 
            // _radioExplanation
            // 
            _radioExplanation.AutoSize = true;
            _radioExplanation.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _radioExplanation.ForeColor = Color.FromArgb(70, 90, 110);
            _radioExplanation.Location = new Point(59, 3);
            _radioExplanation.Name = "_radioExplanation";
            _radioExplanation.Size = new Size(50, 21);
            _radioExplanation.TabIndex = 1;
            _radioExplanation.Text = "释义";
            // 
            // _radioBoth
            // 
            _radioBoth.Location = new Point(3, 30);
            _radioBoth.Name = "_radioBoth";
            _radioBoth.Size = new Size(104, 24);
            _radioBoth.TabIndex = 2;
            _radioBoth.Text = "原文+释义";
            // 
            // _panelQuizMode
            // 
            _panelQuizMode.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _panelQuizMode.BackColor = Color.FromArgb(255, 248, 220);
            _panelQuizMode.BorderStyle = BorderStyle.FixedSingle;
            _panelQuizMode.Controls.Add(_buttonShowAnswer);
            _panelQuizMode.Controls.Add(_labelQuizHint);
            _panelQuizMode.Location = new Point(10, 669);
            _panelQuizMode.Name = "_panelQuizMode";
            _panelQuizMode.Size = new Size(180, 90);
            _panelQuizMode.TabIndex = 10;
            // 
            // _buttonShowAnswer
            // 
            _buttonShowAnswer.BackColor = Color.FromArgb(255, 193, 7);
            _buttonShowAnswer.FlatAppearance.BorderSize = 0;
            _buttonShowAnswer.FlatAppearance.MouseDownBackColor = Color.FromArgb(245, 183, 0);
            _buttonShowAnswer.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 203, 27);
            _buttonShowAnswer.FlatStyle = FlatStyle.Flat;
            _buttonShowAnswer.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _buttonShowAnswer.ForeColor = Color.White;
            _buttonShowAnswer.Location = new Point(10, 11);
            _buttonShowAnswer.Name = "_buttonShowAnswer";
            _buttonShowAnswer.Size = new Size(160, 40);
            _buttonShowAnswer.TabIndex = 0;
            _buttonShowAnswer.Text = "🎮 答题模式";
            _buttonShowAnswer.UseVisualStyleBackColor = false;
            // 
            // _labelQuizHint
            // 
            _labelQuizHint.Font = new Font("微软雅黑", 8.5F);
            _labelQuizHint.ForeColor = Color.FromArgb(139, 119, 101);
            _labelQuizHint.Location = new Point(10, 57);
            _labelQuizHint.Name = "_labelQuizHint";
            _labelQuizHint.Size = new Size(160, 28);
            _labelQuizHint.TabIndex = 1;
            _labelQuizHint.Text = "先隐藏答案，测试自己";
            // 
            // _buttonThemeToggle
            // 
            _buttonThemeToggle.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _buttonThemeToggle.BackColor = Color.FromArgb(103, 58, 183);
            _buttonThemeToggle.FlatAppearance.BorderSize = 0;
            _buttonThemeToggle.FlatAppearance.MouseDownBackColor = Color.FromArgb(93, 48, 173);
            _buttonThemeToggle.FlatAppearance.MouseOverBackColor = Color.FromArgb(113, 68, 193);
            _buttonThemeToggle.FlatStyle = FlatStyle.Flat;
            _buttonThemeToggle.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _buttonThemeToggle.ForeColor = Color.White;
            _buttonThemeToggle.Location = new Point(10, 782);
            _buttonThemeToggle.Name = "_buttonThemeToggle";
            _buttonThemeToggle.Size = new Size(180, 45);
            _buttonThemeToggle.TabIndex = 11;
            _buttonThemeToggle.Text = "🌙 深色模式";
            _buttonThemeToggle.UseVisualStyleBackColor = false;
            // 
            // 
            // _speedSelector
            // 
            _speedSelector.Location = new Point(10, 607);
            _speedSelector.Name = "_speedSelector";
            _speedSelector.Size = new Size(135, 32);
            _speedSelector.TabIndex = 13;
            // LearningSettingsView
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(_panelConfig);
            Name = "LearningSettingsView";
            Size = new Size(220, 837);
            _panelConfig.ResumeLayout(false);
            _groupBoxMode.ResumeLayout(false);
            _groupBoxMode.PerformLayout();
            _groupBoxSort.ResumeLayout(false);
            _groupBoxSort.PerformLayout();
            _groupBoxSubject.ResumeLayout(false);
            _groupBoxSubject.PerformLayout();
            _pronunciationFlowLayoutPanel.ResumeLayout(false);
            _pronunciationFlowLayoutPanel.PerformLayout();
            _panelQuizMode.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        #region Private Methods

        private void ComboBoxUser_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UserChanged?.Invoke(sender, e);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 设置子类别列表
        /// </summary>
        public void SetSubCategories(List<string> subCategories)
        {
            _comboBoxSubCategory.Items.Clear();
            foreach (var category in subCategories)
            {
                _comboBoxSubCategory.Items.Add(category);
            }
            if (_comboBoxSubCategory.Items.Count > 0)
            {
                _comboBoxSubCategory.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 设置子类别选中项
        /// </summary>
        public void SetSubCategoryIndex(int index)
        {
            if (index >= 0 && index < _comboBoxSubCategory.Items.Count)
            {
                _comboBoxSubCategory.SelectedIndex = index;
            }
        }

        /// <summary>
        /// 获取当前子类别
        /// </summary>
        public string GetSubCategory()
        {
            return _comboBoxSubCategory.SelectedItem?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// 设置答题模式按钮文本
        /// </summary>
        public void SetQuizModeButtonText(string text)
        {
            _buttonShowAnswer.Text = text;
        }

        /// <summary>
        /// 设置主题切换按钮文本
        /// </summary>
        public void SetThemeToggleButtonText(string text)
        {
            _buttonThemeToggle.Text = text;
        }

        #endregion

    }
}