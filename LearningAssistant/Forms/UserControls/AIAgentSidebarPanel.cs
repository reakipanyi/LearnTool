using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls
{
    /// <summary>
    /// AI教练侧栏模式枚举
    /// </summary>
    public enum AIAgentMode
    {
        /// <summary>问答模式</summary>
        Chat,
        /// <summary>费曼学习模式</summary>
        Feynman,
        /// <summary>渐进提示模式</summary>
        Hints,
        /// <summary>联想学习模式</summary>
        Association
    }

    public class AIAgentSidebarPanel : UserControl, IThemeable
    {
        #region 常量
        /// <summary>侧栏宽度 360px</summary>
        public const int SidebarWidth = 360;
        /// <summary>标签按钮高度</summary>
        private const int TabButtonHeight = 44;
        #endregion

        #region 全局复用字体（Dispose统一释放，防止GDI句柄泄漏）
        private readonly Font _fontHeaderTitle = new Font("Microsoft YaHei", 14f, FontStyle.Bold);
        private readonly Font _fontCloseBtn = new Font("Microsoft YaHei", 12f);
        private readonly Font _fontTabBtn = new Font("Microsoft YaHei", 11f);
        private readonly Font _fontNormalSmall = new Font("Microsoft YaHei", 10f);
        private readonly Font _fontBoldSmall = new Font("Microsoft YaHei", 10f, FontStyle.Bold);
        private readonly Font _fontInputText = new Font("Microsoft YaHei", 11f);
        private readonly Font _fontContentBtn = new Font("Microsoft YaHei", 11f, FontStyle.Bold);
        private readonly Font _fontLargeContentBtn = new Font("Microsoft YaHei", 12f, FontStyle.Bold);
        private readonly Font _fontDescTiny = new Font("Microsoft YaHei", 9f);
        #endregion

        #region 控件字段
        private Panel _panelHeader;
        private Label _labelTitle;
        private Button _buttonClose;
        private Panel _panelTabs;
        private Button _buttonChat;
        private Button _buttonFeynman;
        private Button _buttonHints;
        private Button _buttonAssociation;
        private Panel _panelContent;
        private Panel _panelContext;
        private Label _labelContext;

        // 各模式内容面板
        private Panel _panelChatContent;
        private Panel _panelFeynmanContent;
        private Panel _panelHintsContent;
        private Panel _panelAssociationContent;
        private TextBox _textBoxChatQuestion;
        #endregion

        #region 状态字段
        private AIAgentMode _currentMode = AIAgentMode.Chat;
        private ThemeMode _currentTheme = ThemeMode.Light;
        private string _currentLearningItem = string.Empty;
        private string _currentCategory = string.Empty;
        #endregion

        #region 属性
        /// <summary>当前学习项内容</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentLearningItem
        {
            get => _currentLearningItem;
            set
            {
                _currentLearningItem = value;
                UpdateContextLabel();
            }
        }

        /// <summary>当前学科/分类</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentCategory
        {
            get => _currentCategory;
            set
            {
                _currentCategory = value;
                UpdateContextLabel();
            }
        }

        /// <summary>当前模式</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public AIAgentMode CurrentMode
        {
            get => _currentMode;
            set
            {
                _currentMode = value;
                UpdateTabDisplay();
                ShowModeContent();
            }
        }
        #endregion

        #region 事件
        /// <summary>关闭按钮点击事件</summary>
        public event EventHandler? CloseClicked;
        /// <summary>切换到费曼学习事件</summary>
        public event EventHandler? FeynmanRequested;
        /// <summary>切换到渐进提示事件</summary>
        public event EventHandler? HintsRequested;
        /// <summary>切换到联想学习事件</summary>
        public event EventHandler? AssociationRequested;
        /// <summary>问答内容变更事件</summary>
        public event EventHandler<string>? ChatContentChanged;
        #endregion

        #region 构造函数
        public AIAgentSidebarPanel()
        {
            InitializeComponent();
            UpdateTabDisplay();
            ShowModeContent();
        }
        #endregion

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            this._panelHeader = new Panel();
            this._buttonClose = new Button();
            this._labelTitle = new Label();
            this._panelTabs = new Panel();
            this._buttonChat = new Button();
            this._buttonFeynman = new Button();
            this._buttonHints = new Button();
            this._buttonAssociation = new Button();
            this._panelContent = new Panel();
            this._panelAssociationContent = new Panel();
            this._panelHintsContent = new Panel();
            this._panelFeynmanContent = new Panel();
            this._panelChatContent = new Panel();
            this._panelContext = new Panel();
            this._labelContext = new Label();
            this._panelHeader.SuspendLayout();
            this._panelTabs.SuspendLayout();
            this._panelContent.SuspendLayout();
            this._panelContext.SuspendLayout();
            this.SuspendLayout();

            // 
            // AIAgentSidebarPanel
            // 
            this.Width = SidebarWidth;
            this.Dock = DockStyle.Right;
            this.BackColor = Color.White;
            this.Name = "AIAgentSidebarPanel";
            this.Size = new Size(SidebarWidth, 600);

            // 
            // _panelHeader
            // 
            this._panelHeader.Dock = DockStyle.Top;
            this._panelHeader.Height = 50;
            this._panelHeader.BackColor = Color.FromArgb(108, 92, 231);
            this._panelHeader.Controls.Add(this._labelTitle);
            this._panelHeader.Controls.Add(this._buttonClose);
            this._panelHeader.Name = "_panelHeader";
            this._panelHeader.TabIndex = 0;

            // 
            // _labelTitle
            // 
            this._labelTitle.Text = "🧠 AI教练";
            this._labelTitle.Dock = DockStyle.Fill;
            this._labelTitle.Font = _fontHeaderTitle;
            this._labelTitle.ForeColor = Color.White;
            this._labelTitle.TextAlign = ContentAlignment.MiddleCenter;
            this._labelTitle.Padding = new Padding(0, 0, 40, 0);
            this._labelTitle.Name = "_labelTitle";
            this._labelTitle.TabIndex = 0;

            // 
            // _buttonClose
            // 
            this._buttonClose.Dock = DockStyle.Right;
            this._buttonClose.Width = 40;
            this._buttonClose.FlatStyle = FlatStyle.Flat;
            this._buttonClose.FlatAppearance.BorderSize = 0;
            this._buttonClose.Text = "✕";
            this._buttonClose.Font = _fontCloseBtn;
            this._buttonClose.ForeColor = Color.White;
            this._buttonClose.BackColor = Color.Transparent;
            this._buttonClose.Cursor = Cursors.Hand;
            this._buttonClose.Click += new EventHandler(this.ButtonClose_Click);
            this._buttonClose.Name = "_buttonClose";
            this._buttonClose.TabIndex = 1;

            // 
            // _panelTabs
            // 
            this._panelTabs.Dock = DockStyle.Top;
            this._panelTabs.Height = TabButtonHeight * 2;
            this._panelTabs.BackColor = Color.FromArgb(245, 245, 250);
            this._panelTabs.Padding = new Padding(8);
            this._panelTabs.Controls.Add(this._buttonChat);
            this._panelTabs.Controls.Add(this._buttonFeynman);
            this._panelTabs.Controls.Add(this._buttonHints);
            this._panelTabs.Controls.Add(this._buttonAssociation);
            this._panelTabs.Name = "_panelTabs";
            this._panelTabs.TabIndex = 1;

            // 计算按钮尺寸位置
            int btnWidth = (this._panelTabs.Width - 24) / 2;
            int btnHeight = 36;
            int btnMargin = 4;

            // 
            // _buttonChat
            // 
            this._buttonChat = CreateTabButton("💬 问答", AIAgentMode.Chat, btnWidth, btnHeight);
            this._buttonChat.Location = new Point(btnMargin, btnMargin);
            this._buttonChat.Name = "_buttonChat";
            this._buttonChat.TabIndex = 0;

            // 
            // _buttonFeynman
            // 
            this._buttonFeynman = CreateTabButton("🧠 费曼", AIAgentMode.Feynman, btnWidth, btnHeight);
            this._buttonFeynman.Location = new Point(btnWidth + btnMargin * 3, btnMargin);
            this._buttonFeynman.Name = "_buttonFeynman";
            this._buttonFeynman.TabIndex = 1;

            // 
            // _buttonHints
            // 
            this._buttonHints = CreateTabButton("💡 提示", AIAgentMode.Hints, btnWidth, btnHeight);
            this._buttonHints.Location = new Point(btnMargin, btnHeight + btnMargin * 3);
            this._buttonHints.Name = "_buttonHints";
            this._buttonHints.TabIndex = 2;

            // 
            // _buttonAssociation
            // 
            this._buttonAssociation = CreateTabButton("🔗 联想", AIAgentMode.Association, btnWidth, btnHeight);
            this._buttonAssociation.Location = new Point(btnWidth + btnMargin * 3, btnHeight + btnMargin * 3);
            this._buttonAssociation.Name = "_buttonAssociation";
            this._buttonAssociation.TabIndex = 3;

            // 
            // _panelContent
            // 
            this._panelContent.Dock = DockStyle.Fill;
            this._panelContent.BackColor = Color.White;
            this._panelContent.Controls.Add(this._panelChatContent);
            this._panelContent.Controls.Add(this._panelFeynmanContent);
            this._panelContent.Controls.Add(this._panelHintsContent);
            this._panelContent.Controls.Add(this._panelAssociationContent);
            this._panelContent.Name = "_panelContent";
            this._panelContent.TabIndex = 2;

            // 初始化四类内容面板
            this._panelChatContent = CreateChatContentPanel();
            this._panelFeynmanContent = CreateFeynmanContentPanel();
            this._panelHintsContent = CreateHintsContentPanel();
            this._panelAssociationContent = CreateAssociationContentPanel();

            // 
            // _panelContext
            // 
            this._panelContext.Dock = DockStyle.Bottom;
            this._panelContext.Height = 40;
            this._panelContext.BackColor = Color.FromArgb(248, 248, 252);
            this._panelContext.Padding = new Padding(12, 8, 12, 8);
            this._panelContext.Controls.Add(this._labelContext);
            this._panelContext.Name = "_panelContext";
            this._panelContext.TabIndex = 3;

            // 
            // _labelContext
            // 
            this._labelContext.Dock = DockStyle.Fill;
            this._labelContext.Font = _fontNormalSmall;
            this._labelContext.ForeColor = Color.FromArgb(102, 102, 102);
            this._labelContext.TextAlign = ContentAlignment.MiddleLeft;
            this._labelContext.Name = "_labelContext";
            this._labelContext.TabIndex = 0;

            // 控件添加顺序（Dock倒序）
            this.Controls.Add(this._panelContent);
            this.Controls.Add(this._panelTabs);
            this.Controls.Add(this._panelHeader);
            this.Controls.Add(this._panelContext);

            this._panelHeader.ResumeLayout(false);
            this._panelTabs.ResumeLayout(false);
            this._panelContent.ResumeLayout(false);
            this._panelContext.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion

        #region 内部UI创建工具方法
        /// <summary>创建模式切换标签按钮</summary>
        private Button CreateTabButton(string text, AIAgentMode mode, int width, int height)
        {
            Button btn = new Button
            {
                Text = text,
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance =
                {
                    BorderSize = 0,
                    MouseDownBackColor = Color.FromArgb(200, 200, 200),
                    MouseOverBackColor = Color.FromArgb(230, 230, 235)
                },
                Font = _fontTabBtn,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 60, 60),
                Cursor = Cursors.Hand,
                Tag = mode
            };
            btn.Click += new EventHandler(TabButton_Click);
            return btn;
        }

        /// <summary>问答面板</summary>
        private Panel CreateChatContentPanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                Padding = new Padding(12)
            };

            Label labelTip = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Text = "💬 向AI提问关于当前学习内容的问题",
                Font = _fontNormalSmall,
                ForeColor = Color.FromArgb(102, 102, 102)
            };

            Label labelExample = new Label
            {
                Dock = DockStyle.Top,
                Height = 60,
                Text = $"示例问题：\n• \"{CurrentLearningItem}\" 是什么意思？\n• 用简单的话解释 \"{CurrentLearningItem}\"",
                Font = _fontNormalSmall,
                ForeColor = Color.FromArgb(80, 80, 80),
                Padding = new Padding(0, 10, 0, 0)
            };

            Label labelInputTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 25,
                Text = "输入你的问题：",
                Font = _fontBoldSmall,
                ForeColor = Color.FromArgb(60, 60, 60),
                Padding = new Padding(0, 15, 0, 0)
            };

            _textBoxChatQuestion = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 80,
                Multiline = true,
                Font = _fontInputText,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = $"请输入关于「{CurrentLearningItem}」的问题..."
            };

            Button btnAsk = new Button
            {
                Dock = DockStyle.Top,
                Height = 36,
                Text = "提问 ➤",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Font = _fontContentBtn,
                BackColor = ThemeHelper.BrandColors.Primary,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 10, 0, 0)
            };
            btnAsk.Click += ChatAskButton_Click;

            panel.Controls.Add(btnAsk);
            panel.Controls.Add(_textBoxChatQuestion);
            panel.Controls.Add(labelInputTitle);
            panel.Controls.Add(labelExample);
            panel.Controls.Add(labelTip);
            return panel;
        }

        /// <summary>费曼学习面板</summary>
        private Panel CreateFeynmanContentPanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                Padding = new Padding(12)
            };

            Label labelDesc = new Label
            {
                Dock = DockStyle.Top,
                Height = 60,
                Text = "🧠 费曼学习法\n用简单的话解释概念，检验是否真正理解",
                Font = _fontNormalSmall,
                ForeColor = Color.FromArgb(102, 102, 102),
                TextAlign = ContentAlignment.TopLeft
            };

            Button btnStart = new Button
            {
                Dock = DockStyle.Top,
                Height = 44,
                Text = "开始费曼学习",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Font = _fontLargeContentBtn,
                BackColor = ThemeHelper.BrandColors.Primary,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 20, 0, 0)
            };
            btnStart.Click += FeynmanStartButton_Click;

            Label labelStep = new Label
            {
                Dock = DockStyle.Top,
                Height = 120,
                Text = "费曼学习法四步：\n\n① 学习\n仔细阅读理解概念\n\n② 教学\n用简单的话解释给他人\n\n③ 复习\n回顾卡壳的地方\n\n④ 简化\n用最简洁的语言总结",
                Font = _fontDescTiny,
                ForeColor = Color.FromArgb(80, 80, 80),
                TextAlign = ContentAlignment.TopLeft,
                Margin = new Padding(0, 15, 0, 0)
            };

            panel.Controls.Add(btnStart);
            panel.Controls.Add(labelDesc);
            panel.Controls.Add(labelStep);
            return panel;
        }

        /// <summary>渐进提示面板</summary>
        private Panel CreateHintsContentPanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                Padding = new Padding(12)
            };

            Label labelDesc = new Label
            {
                Dock = DockStyle.Top,
                Height = 60,
                Text = "💡 渐进式提示\n先自己思考，实在想不出来再看提示",
                Font = _fontNormalSmall,
                ForeColor = Color.FromArgb(102, 102, 102),
                TextAlign = ContentAlignment.TopLeft
            };

            Button btnStart = new Button
            {
                Dock = DockStyle.Top,
                Height = 44,
                Text = "开始自我测试",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Font = _fontLargeContentBtn,
                BackColor = ThemeHelper.WarningColors.Main,
                ForeColor = Color.FromArgb(60, 60, 60),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 20, 0, 0)
            };
            btnStart.Click += HintsStartButton_Click;

            Label labelHintLevel = new Label
            {
                Dock = DockStyle.Top,
                Height = 80,
                Text = "提示等级：\n\n• 提示1（绿色）：最弱暗示\n• 提示2（蓝色）：较弱暗示\n• 提示3（橙色）：中等暗示\n• 提示4（红色）：接近答案",
                Font = _fontDescTiny,
                ForeColor = Color.FromArgb(80, 80, 80),
                TextAlign = ContentAlignment.TopLeft,
                Margin = new Padding(0, 15, 0, 0)
            };

            panel.Controls.Add(btnStart);
            panel.Controls.Add(labelDesc);
            panel.Controls.Add(labelHintLevel);
            return panel;
        }

        /// <summary>联想学习面板</summary>
        private Panel CreateAssociationContentPanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                Padding = new Padding(12)
            };

            Label labelDesc = new Label
            {
                Dock = DockStyle.Top,
                Height = 60,
                Text = "🔗 联想学习\n查看与当前内容相关的词汇和概念",
                Font = _fontNormalSmall,
                ForeColor = Color.FromArgb(102, 102, 102),
                TextAlign = ContentAlignment.TopLeft
            };

            Button btnView = new Button
            {
                Dock = DockStyle.Top,
                Height = 44,
                Text = "查看联想图谱",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Font = _fontLargeContentBtn,
                BackColor = ThemeHelper.SuccessColors.Main,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 20, 0, 0)
            };
            btnView.Click += AssociationViewButton_Click;

            Label labelAssocTip = new Label
            {
                Dock = DockStyle.Top,
                Height = 60,
                Text = "联想图谱展示：\n• 同义词 / 相关词\n• 知识点细节\n• 关联强度可视化",
                Font = _fontDescTiny,
                ForeColor = Color.FromArgb(80, 80, 80),
                TextAlign = ContentAlignment.TopLeft,
                Margin = new Padding(0, 15, 0, 0)
            };

            panel.Controls.Add(btnView);
            panel.Controls.Add(labelDesc);
            panel.Controls.Add(labelAssocTip);
            return panel;
        }
        #endregion

        #region 事件处理
        private void ButtonClose_Click(object sender, EventArgs e)
        {
            CloseClicked?.Invoke(this, EventArgs.Empty);
        }

        private void TabButton_Click(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is AIAgentMode mode)
                CurrentMode = mode;
        }

        private void ChatAskButton_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_textBoxChatQuestion.Text))
                ChatContentChanged?.Invoke(this, _textBoxChatQuestion.Text);
        }

        private void FeynmanStartButton_Click(object? sender, EventArgs e)
        {
            FeynmanRequested?.Invoke(this, EventArgs.Empty);
        }

        private void HintsStartButton_Click(object? sender, EventArgs e)
        {
            HintsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void AssociationViewButton_Click(object? sender, EventArgs e)
        {
            AssociationRequested?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region 私有状态更新逻辑
        /// <summary>更新标签按钮选中高亮</summary>
        private void UpdateTabDisplay()
        {
            ResetTabButtonStyle(_buttonChat);
            ResetTabButtonStyle(_buttonFeynman);
            ResetTabButtonStyle(_buttonHints);
            ResetTabButtonStyle(_buttonAssociation);

            Button selectedButton = _currentMode switch
            {
                AIAgentMode.Chat => _buttonChat,
                AIAgentMode.Feynman => _buttonFeynman,
                AIAgentMode.Hints => _buttonHints,
                AIAgentMode.Association => _buttonAssociation,
                _ => _buttonChat
            };
            selectedButton.BackColor = ThemeHelper.BrandColors.Primary;
            selectedButton.ForeColor = Color.White;
        }

        /// <summary>重置标签按钮默认样式</summary>
        private void ResetTabButtonStyle(Button button)
        {
            button.BackColor = Color.White;
            button.ForeColor = Color.FromArgb(60, 60, 60);
        }

        /// <summary>切换显示对应模式面板</summary>
        private void ShowModeContent()
        {
            _panelChatContent.Visible = _currentMode == AIAgentMode.Chat;
            _panelFeynmanContent.Visible = _currentMode == AIAgentMode.Feynman;
            _panelHintsContent.Visible = _currentMode == AIAgentMode.Hints;
            _panelAssociationContent.Visible = _currentMode == AIAgentMode.Association;
        }

        /// <summary>更新底部上下文文本</summary>
        private void UpdateContextLabel()
        {
            if (string.IsNullOrEmpty(_currentLearningItem))
            {
                _labelContext.Text = "未选择学习内容";
                return;
            }
            string categoryText = string.IsNullOrEmpty(_currentCategory) ? "" : $"（{_currentCategory}）";
            _labelContext.Text = $"正在学习：{_currentLearningItem}{categoryText}";
        }
        #endregion

        #region IThemeable 主题接口实现
        public void ApplyTheme(ThemeColors colors)
        {
            _currentTheme = colors.ThemeMode;
            bool isDark = colors.ThemeMode == ThemeMode.Dark;

            BackColor = isDark ? Color.FromArgb(30, 30, 30) : Color.White;
            _panelHeader.BackColor = isDark ? Color.FromArgb(50, 50, 50) : ThemeHelper.BrandColors.Primary;
            _panelTabs.BackColor = isDark ? Color.FromArgb(35, 35, 35) : Color.FromArgb(245, 245, 250);
            _panelContext.BackColor = isDark ? Color.FromArgb(40, 40, 40) : Color.FromArgb(248, 248, 252);
            _labelContext.ForeColor = isDark ? Color.FromArgb(176, 176, 176) : Color.FromArgb(102, 102, 102);

            UpdateTabDisplay();
        }
        #endregion

        #region 公共对外方法
        /// <summary>设置上下文学习信息</summary>
        public void SetContext(string learningItem, string category)
        {
            CurrentLearningItem = learningItem;
            CurrentCategory = category;
        }

        /// <summary>切换指定模式</summary>
        public void SwitchToMode(AIAgentMode mode)
        {
            CurrentMode = mode;
        }
        #endregion

        #region 资源释放（释放全部字体，解决GDI泄漏）
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fontHeaderTitle?.Dispose();
                _fontCloseBtn?.Dispose();
                _fontTabBtn?.Dispose();
                _fontNormalSmall?.Dispose();
                _fontBoldSmall?.Dispose();
                _fontInputText?.Dispose();
                _fontContentBtn?.Dispose();
                _fontLargeContentBtn?.Dispose();
                _fontDescTiny?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}