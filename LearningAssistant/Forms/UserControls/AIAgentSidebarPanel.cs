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
        private readonly Dictionary<Button, Color> _tabButtonOriginalColors = new();
        private readonly Dictionary<Button, Color> _contentButtonOriginalColors = new();
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
            _panelHeader = new Panel();
            _labelTitle = new Label();
            _buttonClose = new Button();
            _panelTabs = new Panel();
            _buttonChat = new Button();
            _buttonFeynman = new Button();
            _buttonHints = new Button();
            _buttonAssociation = new Button();
            _panelContent = new Panel();
            _panelChatContent = new Panel();
            _panelFeynmanContent = new Panel();
            _panelHintsContent = new Panel();
            _panelAssociationContent = new Panel();
            _panelContext = new Panel();
            _labelContext = new Label();
            _panelHeader.SuspendLayout();
            _panelTabs.SuspendLayout();
            _panelContent.SuspendLayout();
            _panelContext.SuspendLayout();
            SuspendLayout();
            // 
            // _panelHeader
            // 
            _panelHeader.BackColor = Color.FromArgb(108, 92, 231);
            _panelHeader.Controls.Add(_labelTitle);
            _panelHeader.Controls.Add(_buttonClose);
            _panelHeader.Dock = DockStyle.Top;
            _panelHeader.Location = new Point(0, 0);
            _panelHeader.Name = "_panelHeader";
            _panelHeader.Size = new Size(150, 50);
            _panelHeader.TabIndex = 0;
            // 
            // _labelTitle
            // 
            _labelTitle.Dock = DockStyle.Fill;
            _labelTitle.ForeColor = Color.White;
            _labelTitle.Location = new Point(0, 0);
            _labelTitle.Name = "_labelTitle";
            _labelTitle.Padding = new Padding(0, 0, 40, 0);
            _labelTitle.Size = new Size(110, 50);
            _labelTitle.TabIndex = 0;
            _labelTitle.Text = "\U0001f9e0 AI教练";
            _labelTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // _buttonClose
            // 
            _buttonClose.BackColor = Color.Transparent;
            _buttonClose.Cursor = Cursors.Hand;
            _buttonClose.Dock = DockStyle.Right;
            _buttonClose.FlatAppearance.BorderSize = 0;
            _buttonClose.FlatStyle = FlatStyle.Flat;
            _buttonClose.ForeColor = Color.White;
            _buttonClose.Location = new Point(110, 0);
            _buttonClose.Name = "_buttonClose";
            _buttonClose.Size = new Size(40, 50);
            _buttonClose.TabIndex = 1;
            _buttonClose.Text = "✕";
            _buttonClose.UseVisualStyleBackColor = false;
            _buttonClose.Click += ButtonClose_Click;
            // 
            // _panelTabs
            // 
            _panelTabs.BackColor = Color.FromArgb(245, 245, 250);
            _panelTabs.Controls.Add(_buttonChat);
            _panelTabs.Controls.Add(_buttonFeynman);
            _panelTabs.Controls.Add(_buttonHints);
            _panelTabs.Controls.Add(_buttonAssociation);
            _panelTabs.Dock = DockStyle.Top;
            _panelTabs.Location = new Point(0, 50);
            _panelTabs.Name = "_panelTabs";
            _panelTabs.Padding = new Padding(8);
            _panelTabs.Size = new Size(150, 100);
            _panelTabs.TabIndex = 1;
            // 
            // _buttonChat
            // 
            _buttonChat.Location = new Point(4, 4);
            _buttonChat.Name = "_buttonChat";
            _buttonChat.Size = new Size(75, 23);
            _buttonChat.TabIndex = 0;
            // 
            // _buttonFeynman
            // 
            _buttonFeynman.Location = new Point(200, 4);
            _buttonFeynman.Name = "_buttonFeynman";
            _buttonFeynman.Size = new Size(75, 23);
            _buttonFeynman.TabIndex = 1;
            // 
            // _buttonHints
            // 
            _buttonHints.Location = new Point(4, 36);
            _buttonHints.Name = "_buttonHints";
            _buttonHints.Size = new Size(75, 23);
            _buttonHints.TabIndex = 2;
            // 
            // _buttonAssociation
            // 
            _buttonAssociation.Location = new Point(200, 36);
            _buttonAssociation.Name = "_buttonAssociation";
            _buttonAssociation.Size = new Size(75, 23);
            _buttonAssociation.TabIndex = 3;
            // 
            // _panelContent
            // 
            _panelContent.BackColor = Color.White;
            _panelContent.Controls.Add(_panelChatContent);
            _panelContent.Controls.Add(_panelFeynmanContent);
            _panelContent.Controls.Add(_panelHintsContent);
            _panelContent.Controls.Add(_panelAssociationContent);
            _panelContent.Dock = DockStyle.Fill;
            _panelContent.Location = new Point(0, 150);
            _panelContent.Name = "_panelContent";
            _panelContent.Size = new Size(150, 674);
            _panelContent.TabIndex = 2;
            // 
            // _panelChatContent
            // 
            _panelChatContent.Location = new Point(0, 0);
            _panelChatContent.Name = "_panelChatContent";
            _panelChatContent.Size = new Size(200, 100);
            _panelChatContent.TabIndex = 0;
            // 
            // _panelFeynmanContent
            // 
            _panelFeynmanContent.Location = new Point(0, 0);
            _panelFeynmanContent.Name = "_panelFeynmanContent";
            _panelFeynmanContent.Size = new Size(200, 100);
            _panelFeynmanContent.TabIndex = 1;
            // 
            // _panelHintsContent
            // 
            _panelHintsContent.Location = new Point(0, 0);
            _panelHintsContent.Name = "_panelHintsContent";
            _panelHintsContent.Size = new Size(200, 100);
            _panelHintsContent.TabIndex = 2;
            // 
            // _panelAssociationContent
            // 
            _panelAssociationContent.Location = new Point(0, 0);
            _panelAssociationContent.Name = "_panelAssociationContent";
            _panelAssociationContent.Size = new Size(200, 100);
            _panelAssociationContent.TabIndex = 3;
            // 
            // _panelContext
            // 
            _panelContext.BackColor = Color.FromArgb(248, 248, 252);
            _panelContext.Controls.Add(_labelContext);
            _panelContext.Dock = DockStyle.Bottom;
            _panelContext.Location = new Point(0, 824);
            _panelContext.Name = "_panelContext";
            _panelContext.Padding = new Padding(12, 8, 12, 8);
            _panelContext.Size = new Size(150, 40);
            _panelContext.TabIndex = 3;
            // 
            // _labelContext
            // 
            _labelContext.Dock = DockStyle.Fill;
            _labelContext.ForeColor = Color.FromArgb(102, 102, 102);
            _labelContext.Location = new Point(12, 8);
            _labelContext.Name = "_labelContext";
            _labelContext.Size = new Size(126, 24);
            _labelContext.TabIndex = 0;
            _labelContext.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // AIAgentSidebarPanel
            // 
            BackColor = Color.White;
            Controls.Add(_panelContent);
            Controls.Add(_panelTabs);
            Controls.Add(_panelHeader);
            Controls.Add(_panelContext);
            Name = "AIAgentSidebarPanel";
            Size = new Size(150, 864);
            _panelHeader.ResumeLayout(false);
            _panelTabs.ResumeLayout(false);
            _panelContent.ResumeLayout(false);
            _panelContext.ResumeLayout(false);
            ResumeLayout(false);
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
            btn.MouseEnter += TabButton_MouseEnter;
            btn.MouseLeave += TabButton_MouseLeave;
            _tabButtonOriginalColors[btn] = Color.White;
            return btn;
        }

        private void TabButton_MouseEnter(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is AIAgentMode mode && mode != _currentMode)
            {
                btn.BackColor = _currentTheme == ThemeMode.Dark
                    ? Color.FromArgb(55, 55, 55)
                    : Color.FromArgb(240, 240, 245);
            }
        }

        private void TabButton_MouseLeave(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is AIAgentMode mode && mode != _currentMode)
            {
                btn.BackColor = _currentTheme == ThemeMode.Dark
                    ? Color.FromArgb(30, 30, 30)
                    : Color.White;
            }
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
                PlaceholderText = $"请输入关于「{CurrentLearningItem}」的问题...",
                Cursor = Cursors.IBeam
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
            AddContentButtonHoverEffect(btnAsk, ThemeHelper.BrandColors.Primary);

            panel.Controls.Add(btnAsk);
            panel.Controls.Add(_textBoxChatQuestion);
            panel.Controls.Add(labelInputTitle);
            panel.Controls.Add(labelExample);
            panel.Controls.Add(labelTip);
            return panel;
        }

        private void AddContentButtonHoverEffect(Button button, Color baseColor)
        {
            _contentButtonOriginalColors[button] = baseColor;
            button.MouseEnter += (s, e) =>
            {
                button.BackColor = ThemeHelper.GetHoverColor(baseColor, -20);
            };
            button.MouseLeave += (s, e) =>
            {
                button.BackColor = baseColor;
            };
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
            AddContentButtonHoverEffect(btnStart, ThemeHelper.BrandColors.Primary);

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
            AddContentButtonHoverEffect(btnStart, ThemeHelper.WarningColors.Main);

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
            AddContentButtonHoverEffect(btnView, ThemeHelper.SuccessColors.Main);

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
            bool isDark = _currentTheme == ThemeMode.Dark;
            button.BackColor = isDark ? Color.FromArgb(30, 30, 30) : Color.White;
            button.ForeColor = isDark ? Color.FromArgb(220, 220, 220) : Color.FromArgb(60, 60, 60);
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

            BackColor = isDark ? colors.Surface : Color.White;
            _panelHeader.BackColor = isDark ? colors.SurfaceElevated : ThemeHelper.BrandColors.Primary;
            _panelTabs.BackColor = isDark ? colors.Background : Color.FromArgb(245, 245, 250);
            _panelContext.BackColor = isDark ? colors.SurfaceElevated : Color.FromArgb(248, 248, 252);
            _labelContext.ForeColor = isDark ? colors.TextSecondary : Color.FromArgb(102, 102, 102);
            _panelContent.BackColor = isDark ? colors.Surface : Color.White;

            ApplyThemeToContentPanels(colors);
            UpdateTabDisplay();
        }

        private void ApplyThemeToContentPanels(ThemeColors colors)
        {
            bool isDark = colors.ThemeMode == ThemeMode.Dark;

            var contentPanels = new[] { _panelChatContent, _panelFeynmanContent, _panelHintsContent, _panelAssociationContent };
            foreach (var panel in contentPanels)
            {
                if (panel == null) continue;
                panel.BackColor = isDark ? colors.Surface : Color.White;

                foreach (Control ctrl in panel.Controls)
                {
                    if (ctrl is Label lbl)
                    {
                        if (lbl.Font.Bold)
                            lbl.ForeColor = isDark ? colors.TextPrimary : Color.FromArgb(60, 60, 60);
                        else
                            lbl.ForeColor = isDark ? colors.TextSecondary : Color.FromArgb(102, 102, 102);
                    }
                    else if (ctrl is TextBox txt)
                    {
                        txt.BackColor = isDark ? colors.SurfaceElevated : Color.White;
                        txt.ForeColor = isDark ? colors.TextPrimary : Color.Black;
                    }
                }
            }
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