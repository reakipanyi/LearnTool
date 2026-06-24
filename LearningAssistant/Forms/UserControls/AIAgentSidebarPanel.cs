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

        /// <summary>
        /// 侧栏宽度 360px
        /// </summary>
        public const int SidebarWidth = 360;

        /// <summary>
        /// 标签按钮高度
        /// </summary>
        private const int TabButtonHeight = 44;

        #endregion

        #region 字段

        private Panel _panelHeader = null!;
        private Label _labelTitle = null!;
        private Button _buttonClose = null!;
        private Panel _panelTabs = null!;
        private Button _buttonChat = null!;
        private Button _buttonFeynman = null!;
        private Button _buttonHints = null!;
        private Button _buttonAssociation = null!;
        private Panel _panelContent = null!;
        private Panel _panelContext = null!;
        private Label _labelContext = null!;

        private AIAgentMode _currentMode = AIAgentMode.Chat;
        private ThemeMode _currentTheme = ThemeMode.Light;
        private string _currentLearningItem = string.Empty;
        private string _currentCategory = string.Empty;

        // 各模式内容面板
        private Panel _panelChatContent = null!;
        private Panel _panelFeynmanContent = null!;
        private Panel _panelHintsContent = null!;
        private Panel _panelAssociationContent = null!;

        #endregion

        #region 属性

        /// <summary>
        /// 当前学习项内容
        /// </summary>
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

        /// <summary>
        /// 当前学科/分类
        /// </summary>
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

        /// <summary>
        /// 当前模式
        /// </summary>
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

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        public event EventHandler? CloseClicked;

        /// <summary>
        /// 切换到费曼学习事件
        /// </summary>
        public event EventHandler? FeynmanRequested;

        /// <summary>
        /// 切换到渐进提示事件
        /// </summary>
        public event EventHandler? HintsRequested;

        /// <summary>
        /// 切换到联想学习事件
        /// </summary>
        public event EventHandler? AssociationRequested;

        /// <summary>
        /// 问答内容变更事件
        /// </summary>
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

        #region 初始化

        private void InitializeComponent()
        {
            SuspendLayout();

            // 主面板
            Width = SidebarWidth;
            Dock = DockStyle.Right;
            BackColor = Color.White;

            // 顶部面板
            _panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(108, 92, 231) // 品牌紫色
            };

            _labelTitle = new Label
            {
                Text = "🧠 AI教练",
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei", 14f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(0, 0, 40, 0) // 为关闭按钮留出空间
            };

            _buttonClose = new Button
            {
                Dock = DockStyle.Right,
                Width = 40,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Text = "✕",
                Font = new Font("Microsoft YaHei", 12f),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            _buttonClose.Click += (s, e) => CloseClicked?.Invoke(this, EventArgs.Empty);

            _panelHeader.Controls.Add(_labelTitle);
            _panelHeader.Controls.Add(_buttonClose);

            // 标签面板
            _panelTabs = new Panel
            {
                Dock = DockStyle.Top,
                Height = TabButtonHeight * 2, // 2行2列
                BackColor = Color.FromArgb(245, 245, 250),
                Padding = new Padding(8)
            };

            int btnWidth = (_panelTabs.Width - 24) / 2;
            int btnHeight = 36;
            int btnMargin = 4;

            // 创建标签按钮
            _buttonChat = CreateTabButton("💬 问答", AIAgentMode.Chat, btnWidth, btnHeight);
            _buttonFeynman = CreateTabButton("🧠 费曼", AIAgentMode.Feynman, btnWidth, btnHeight);
            _buttonHints = CreateTabButton("💡 提示", AIAgentMode.Hints, btnWidth, btnHeight);
            _buttonAssociation = CreateTabButton("🔗 联想", AIAgentMode.Association, btnWidth, btnHeight);

            // 布局：2x2 网格
            _buttonChat.Location = new Point(btnMargin, btnMargin);
            _buttonFeynman.Location = new Point(btnWidth + btnMargin * 3, btnMargin);
            _buttonHints.Location = new Point(btnMargin, btnHeight + btnMargin * 3);
            _buttonAssociation.Location = new Point(btnWidth + btnMargin * 3, btnHeight + btnMargin * 3);

            _panelTabs.Controls.Add(_buttonChat);
            _panelTabs.Controls.Add(_buttonFeynman);
            _panelTabs.Controls.Add(_buttonHints);
            _panelTabs.Controls.Add(_buttonAssociation);

            // 内容面板
            _panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            // 创建各模式内容面板
            _panelChatContent = CreateChatContentPanel();
            _panelFeynmanContent = CreateFeynmanContentPanel();
            _panelHintsContent = CreateHintsContentPanel();
            _panelAssociationContent = CreateAssociationContentPanel();

            _panelContent.Controls.Add(_panelChatContent);
            _panelContent.Controls.Add(_panelFeynmanContent);
            _panelContent.Controls.Add(_panelHintsContent);
            _panelContent.Controls.Add(_panelAssociationContent);

            // 上下文面板
            _panelContext = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(248, 248, 252),
                Padding = new Padding(12, 8, 12, 8)
            };

            _labelContext = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei", 10f),
                ForeColor = Color.FromArgb(102, 102, 102),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _panelContext.Controls.Add(_labelContext);

            // 添加到控件
            Controls.Add(_panelContent);
            Controls.Add(_panelTabs);
            Controls.Add(_panelHeader);
            Controls.Add(_panelContext);

            ResumeLayout(false);
        }

        private Button CreateTabButton(string text, AIAgentMode mode, int width, int height)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = {
                    BorderSize = 0,
                    MouseDownBackColor = Color.FromArgb(200, 200, 200),
                    MouseOverBackColor = Color.FromArgb(230, 230, 235)
                },
                Font = new Font("Microsoft YaHei", 11f),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 60, 60),
                Cursor = Cursors.Hand,
                Tag = mode
            };
            btn.Click += TabButton_Click;
            return btn;
        }

        private Panel CreateChatContentPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                Padding = new Padding(12)
            };

            var label = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Text = "💬 向AI提问关于当前学习内容的问题",
                Font = new Font("Microsoft YaHei", 10f),
                ForeColor = Color.FromArgb(102, 102, 102)
            };

            var hintLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 60,
                Text = $"示例问题：\n• \"{CurrentLearningItem}\" 是什么意思？\n• 用简单的话解释 \"{CurrentLearningItem}\"",
                Font = new Font("Microsoft YaHei", 10f),
                ForeColor = Color.FromArgb(80, 80, 80),
                Padding = new Padding(0, 10, 0, 0)
            };

            var promptLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 25,
                Text = "输入你的问题：",
                Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Padding = new Padding(0, 15, 0, 0)
            };

            var textBox = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 80,
                Multiline = true,
                Font = new Font("Microsoft YaHei", 11f),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = $"请输入关于「{CurrentLearningItem}」的问题..."
            };

            var buttonAsk = new Button
            {
                Dock = DockStyle.Top,
                Height = 36,
                Text = "提问 ➤",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold),
                BackColor = ThemeHelper.BrandColors.Primary,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 10, 0, 0)
            };
            buttonAsk.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    ChatContentChanged?.Invoke(this, textBox.Text);
                }
            };

            panel.Controls.Add(buttonAsk);
            panel.Controls.Add(textBox);
            panel.Controls.Add(promptLabel);
            panel.Controls.Add(hintLabel);
            panel.Controls.Add(label);

            return panel;
        }

        private Panel CreateFeynmanContentPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                Padding = new Padding(12)
            };

            var label = new Label
            {
                Dock = DockStyle.Top,
                Height = 60,
                Text = "🧠 费曼学习法\n用简单的话解释概念，检验是否真正理解",
                Font = new Font("Microsoft YaHei", 10f),
                ForeColor = Color.FromArgb(102, 102, 102),
                TextAlign = ContentAlignment.TopLeft
            };

            var startButton = new Button
            {
                Dock = DockStyle.Top,
                Height = 44,
                Text = "开始费曼学习",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Font = new Font("Microsoft YaHei", 12f, FontStyle.Bold),
                BackColor = ThemeHelper.BrandColors.Primary,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 20, 0, 0)
            };
            startButton.Click += (s, e) => FeynmanRequested?.Invoke(this, EventArgs.Empty);

            var stepsLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 120,
                Text = "费曼学习法四步：\n\n① 学习\n仔细阅读理解概念\n\n② 教学\n用简单的话解释给他人\n\n③ 复习\n回顾卡壳的地方\n\n④ 简化\n用最简洁的语言总结",
                Font = new Font("Microsoft YaHei", 9f),
                ForeColor = Color.FromArgb(80, 80, 80),
                TextAlign = ContentAlignment.TopLeft,
                Margin = new Padding(0, 15, 0, 0)
            };

            panel.Controls.Add(startButton);
            panel.Controls.Add(label);
            panel.Controls.Add(stepsLabel);

            return panel;
        }

        private Panel CreateHintsContentPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                Padding = new Padding(12)
            };

            var label = new Label
            {
                Dock = DockStyle.Top,
                Height = 60,
                Text = "💡 渐进式提示\n先自己思考，实在想不出来再看提示",
                Font = new Font("Microsoft YaHei", 10f),
                ForeColor = Color.FromArgb(102, 102, 102),
                TextAlign = ContentAlignment.TopLeft
            };

            var startButton = new Button
            {
                Dock = DockStyle.Top,
                Height = 44,
                Text = "开始自我测试",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Font = new Font("Microsoft YaHei", 12f, FontStyle.Bold),
                BackColor = ThemeHelper.WarningColors.Main,
                ForeColor = Color.FromArgb(60, 60, 60),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 20, 0, 0)
            };
            startButton.Click += (s, e) => HintsRequested?.Invoke(this, EventArgs.Empty);

            var hintDescLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 80,
                Text = "提示等级：\n\n• 提示1（绿色）：最弱暗示\n• 提示2（蓝色）：较弱暗示\n• 提示3（橙色）：中等暗示\n• 提示4（红色）：接近答案",
                Font = new Font("Microsoft YaHei", 9f),
                ForeColor = Color.FromArgb(80, 80, 80),
                TextAlign = ContentAlignment.TopLeft,
                Margin = new Padding(0, 15, 0, 0)
            };

            panel.Controls.Add(startButton);
            panel.Controls.Add(label);
            panel.Controls.Add(hintDescLabel);

            return panel;
        }

        private Panel CreateAssociationContentPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                Padding = new Padding(12)
            };

            var label = new Label
            {
                Dock = DockStyle.Top,
                Height = 60,
                Text = "🔗 联想学习\n查看与当前内容相关的词汇和概念",
                Font = new Font("Microsoft YaHei", 10f),
                ForeColor = Color.FromArgb(102, 102, 102),
                TextAlign = ContentAlignment.TopLeft
            };

            var viewButton = new Button
            {
                Dock = DockStyle.Top,
                Height = 44,
                Text = "查看联想图谱",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Font = new Font("Microsoft YaHei", 12f, FontStyle.Bold),
                BackColor = ThemeHelper.SuccessColors.Main,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 20, 0, 0)
            };
            viewButton.Click += (s, e) => AssociationRequested?.Invoke(this, EventArgs.Empty);

            var assocDescLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 60,
                Text = "联想图谱展示：\n• 同义词 / 相关词\n• 知识点细节\n• 关联强度可视化",
                Font = new Font("Microsoft YaHei", 9f),
                ForeColor = Color.FromArgb(80, 80, 80),
                TextAlign = ContentAlignment.TopLeft,
                Margin = new Padding(0, 15, 0, 0)
            };

            panel.Controls.Add(viewButton);
            panel.Controls.Add(label);
            panel.Controls.Add(assocDescLabel);

            return panel;
        }

        #endregion

        #region 事件处理

        private void TabButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is AIAgentMode mode)
            {
                CurrentMode = mode;
            }
        }

        #endregion

        #region 私有方法

        private void UpdateTabDisplay()
        {
            // 重置所有按钮样式
            ResetTabButtonStyle(_buttonChat);
            ResetTabButtonStyle(_buttonFeynman);
            ResetTabButtonStyle(_buttonHints);
            ResetTabButtonStyle(_buttonAssociation);

            // 高亮当前选中按钮
            var selectedButton = _currentMode switch
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

        private void ResetTabButtonStyle(Button button)
        {
            button.BackColor = Color.White;
            button.ForeColor = Color.FromArgb(60, 60, 60);
        }

        private void ShowModeContent()
        {
            _panelChatContent.Visible = _currentMode == AIAgentMode.Chat;
            _panelFeynmanContent.Visible = _currentMode == AIAgentMode.Feynman;
            _panelHintsContent.Visible = _currentMode == AIAgentMode.Hints;
            _panelAssociationContent.Visible = _currentMode == AIAgentMode.Association;
        }

        private void UpdateContextLabel()
        {
            if (string.IsNullOrEmpty(_currentLearningItem))
            {
                _labelContext.Text = "未选择学习内容";
            }
            else
            {
                var category = string.IsNullOrEmpty(_currentCategory) ? "" : $"（{_currentCategory}）";
                _labelContext.Text = $"正在学习：{_currentLearningItem}{category}";
            }
        }

        #endregion

        #region IThemeable 实现

        public void ApplyTheme(ThemeColors colors)
        {
            _currentTheme = colors.ThemeMode;
            var isDark = colors.ThemeMode == ThemeMode.Dark;

            BackColor = isDark ? Color.FromArgb(30, 30, 30) : Color.White;

            _panelHeader.BackColor = isDark
                ? Color.FromArgb(50, 50, 50)
                : ThemeHelper.BrandColors.Primary;

            _panelTabs.BackColor = isDark
                ? Color.FromArgb(35, 35, 35)
                : Color.FromArgb(245, 245, 250);

            _panelContext.BackColor = isDark
                ? Color.FromArgb(40, 40, 40)
                : Color.FromArgb(248, 248, 252);

            _labelContext.ForeColor = isDark
                ? Color.FromArgb(176, 176, 176)
                : Color.FromArgb(102, 102, 102);

            UpdateTabDisplay();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置上下文信息
        /// </summary>
        public void SetContext(string learningItem, string category)
        {
            CurrentLearningItem = learningItem;
            CurrentCategory = category;
        }

        /// <summary>
        /// 切换到指定模式
        /// </summary>
        public void SwitchToMode(AIAgentMode mode)
        {
            CurrentMode = mode;
        }

        #endregion
    }
}
