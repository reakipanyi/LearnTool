using LearningAssistant.Common;
using LearningAssistant.Models.AI;
using LearningAssistant.Services.AI;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls
{
    /// <summary>
    /// AI导师面板 - 多轮对话式AI学习助手
    /// </summary>
    public class MentorAIPanel : UserControl
    {
        #region 控件字段

        private Panel _panelHeader = null!;
        private Label _labelTitle = null!;
        private ComboBox _comboPersona = null!;
        private Panel _panelMessages = null!;
        private Panel _panelInput = null!;
        private TextBox _textInput = null!;
        private Button _buttonSend = null!;
        private Button _buttonClear = null!;
        private Label _labelThinking = null!;
        private FlowLayoutPanel _panelQuickActions = null!;

        #endregion

        #region 属性

        private IConversationContextService? _contextService;
        private string _currentUserId = Constants.DefaultUserId;
        private bool _isLoading;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IConversationContextService? ContextService
        {
            get => _contextService;
            set
            {
                if (_contextService != null)
                {
                    _contextService.MessageReceived -= OnMessageReceived;
                }
                _contextService = value;
                if (_contextService != null)
                {
                    _contextService.MessageReceived += OnMessageReceived;
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentUserId
        {
            get => _currentUserId;
            set => _currentUserId = value;
        }

        #endregion

        #region 事件

        public event EventHandler<string>? MessageSent;
        public event EventHandler<MentorPersonaType>? PersonaChanged;

        #endregion

        public MentorAIPanel()
        {
            InitializeComponent();
            InitializeQuickActions();
        }

        #region 初始化

        private void InitializeComponent()
        {
            SuspendLayout();

            // _panelHeader
            _panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(33, 150, 243)
            };

            _labelTitle = new Label
            {
                Text = "🤖 AI导师",
                Font = new Font("微软雅黑", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 10),
                Size = new Size(150, 30),
                AutoSize = true
            };

            _comboPersona = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(180, 12),
                Size = new Size(150, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Font = new Font("微软雅黑", 9F)
            };
            _comboPersona.Items.AddRange(new object[]
            {
                new PersonaItem(MentorPersonaType.Tutor, "📖 答疑导师"),
                new PersonaItem(MentorPersonaType.Socratic, "💭 苏格拉底"),
                new PersonaItem(MentorPersonaType.Feynman, "🎓 费曼教练"),
                new PersonaItem(MentorPersonaType.Diagnostician, "🔍 诊断专家")
            });
            _comboPersona.SelectedIndex = 0;
            _comboPersona.SelectedIndexChanged += ComboPersona_SelectedIndexChanged;

            _panelHeader.Controls.AddRange(new Control[] { _labelTitle, _comboPersona });

            // _panelMessages
            _panelMessages = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 249, 250),
                AutoScroll = true,
                Padding = new Padding(10)
            };

            // _panelQuickActions
            _panelQuickActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.White,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(10, 5, 10, 5)
            };

            // _labelThinking
            _labelThinking = new Label
            {
                Text = "🤔 AI正在思考...",
                Visible = false,
                AutoSize = true,
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.FromArgb(102, 102, 102),
                Margin = new Padding(15, 10, 0, 0)
            };

            // _panelInput
            _panelInput = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            _textInput = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                Font = new Font("微软雅黑", 10F),
                PlaceholderText = "输入您的问题...",
                BorderStyle = BorderStyle.FixedSingle,
                MaxLength = 2000
            };
            _textInput.KeyDown += TextInput_KeyDown;

            _buttonSend = new Button
            {
                Text = "发送",
                Size = new Size(70, 40),
                Location = new Point(0, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _buttonSend.Click += ButtonSend_Click;

            _buttonClear = new Button
            {
                Text = "清空",
                Size = new Size(60, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.FromArgb(102, 102, 102),
                Font = new Font("微软雅黑", 9F),
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 0, 0)
            };
            _buttonClear.Click += ButtonClear_Click;

            // 调整输入面板布局
            var inputContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            inputContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            inputContainer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            inputContainer.Controls.Add(_textInput, 0, 0);
            inputContainer.Controls.Add(_buttonSend, 1, 0);

            _panelInput.Controls.Add(inputContainer);
            _panelInput.Controls.Add(_buttonClear);
            _buttonClear.Location = new Point(_panelInput.Width - 70, 10);

            // 主容器
            Controls.Add(_panelMessages);
            Controls.Add(_panelQuickActions);
            Controls.Add(_panelInput);
            Controls.Add(_panelHeader);

            // 添加欢迎消息
            AddWelcomeMessage();

            // 样式设置
            DoubleBuffered = true;
            BackColor = Color.FromArgb(248, 249, 250);

            ResumeLayout(false);
        }

        private void InitializeQuickActions()
        {
            var actions = new[]
            {
                ("解释这个概念", "📖"),
                ("出一道练习题", "📝"),
                ("帮我记忆", "🧠"),
                ("检查我的理解", "✅")
            };

            foreach (var (text, icon) in actions)
            {
                var btn = new Button
                {
                    Text = $"{icon} {text}",
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(33, 150, 243),
                    Font = new Font("微软雅黑", 8.5F),
                    Cursor = Cursors.Hand,
                    AutoSize = true,
                    Padding = new Padding(10, 5, 10, 5),
                    Margin = new Padding(0, 0, 8, 0)
                };
                btn.Click += (s, e) => QuickAction_Click(text);

                // 圆角效果通过 Paint 事件实现
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Color.FromArgb(33, 150, 243);

                _panelQuickActions.Controls.Add(btn);
            }
        }

        #endregion

        #region 事件处理

        private async void ButtonSend_Click(object? sender, EventArgs e)
        {
            await SendMessageAsync();
        }

        private async void TextInput_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                await SendMessageAsync();
            }
        }

        private async void ButtonClear_Click(object? sender, EventArgs e)
        {
            ClearChat();
            _contextService?.ClearCurrentSession();
        }

        private void ComboPersona_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_comboPersona.SelectedItem is PersonaItem item)
            {
                _contextService?.SwitchPersona(item.Type);

                // 更新标题
                _labelTitle.Text = $"{item.Icon} {item.DisplayName}";
                PersonaChanged?.Invoke(this, item.Type);
            }
        }

        private async void QuickAction_Click(string action)
        {
            _textInput.Text = action;
            await SendMessageAsync();
        }

        private void OnMessageReceived(object? sender, ConversationTurn turn)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(() => OnMessageReceived(sender, turn));
                return;
            }

            AddMessageBubble(turn.UserMessage, isUser: true);
            AddMessageBubble(turn.AiResponse, isUser: false);
            SetLoading(false);
        }

        #endregion

        #region 核心方法

        private async Task SendMessageAsync()
        {
            if (_isLoading) return;

            var message = _textInput.Text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            _textInput.Clear();
            SetLoading(true);

            try
            {
                // 添加用户消息气泡
                AddMessageBubble(message, isUser: true);
                MessageSent?.Invoke(this, message);

                // 发送到AI服务
                if (_contextService != null)
                {
                    var response = await _contextService.AddMessageAsync(message);
                    // 响应会在 MessageReceived 事件中处理
                }
                else
                {
                    // 如果没有配置服务，模拟响应
                    await Task.Delay(500);
                    AddMessageBubble("请先配置对话服务", isUser: false);
                    SetLoading(false);
                }
            }
            catch (Exception ex)
            {
                AddMessageBubble($"错误: {ex.Message}", isUser: false);
                SetLoading(false);
            }
        }

        private void SetLoading(bool loading)
        {
            _isLoading = loading;
            _buttonSend.Enabled = !loading;
            _textInput.Enabled = !loading;
            _labelThinking.Visible = loading;

            if (loading)
            {
                _panelMessages.Controls.Add(_labelThinking);
                _labelThinking.BringToFront();
                ScrollToBottom();
            }
            else
            {
                _panelMessages.Controls.Remove(_labelThinking);
            }
        }

        private void AddWelcomeMessage()
        {
            var session = _contextService?.CurrentSession;
            var personaName = session?.Persona.Name ?? "答疑导师";
            var personaIcon = session?.Persona.Icon ?? "📖";

            var welcomeText = $"{personaIcon} 你好！我是{personaName}。有什么学习上的问题可以问我，我会尽力帮助你理解和掌握知识。";

            var welcomePanel = CreateMessageBubble(welcomeText, isUser: false);
            _panelMessages.Controls.Add(welcomePanel);
        }

        private void AddMessageBubble(string message, bool isUser)
        {
            var bubble = CreateMessageBubble(message, isUser);
            _panelMessages.Controls.Add(bubble);
            ScrollToBottom();
        }

        private Panel CreateMessageBubble(string message, bool isUser)
        {
            var panel = new Panel
            {
                AutoSize = true,
                MaximumSize = new Size(_panelMessages.Width - 80, 0),
                Margin = new Padding(isUser ? 60 : 10, 5, isUser ? 10 : 60, 5)
            };

            var label = new Label
            {
                Text = message,
                AutoSize = false,
                MaximumSize = new Size(_panelMessages.Width - 100, 0),
                Size = new Size(_panelMessages.Width - 100, 0),
                Font = new Font("微软雅黑", 9.5F),
                ForeColor = isUser ? Color.White : Color.FromArgb(51, 51, 51),
                BackColor = isUser ? Color.FromArgb(33, 150, 243) : Color.White,
                Padding = new Padding(12, 10, 12, 10),
                AutoEllipsis = false
            };

            // 测量实际高度
            using (var g = CreateGraphics())
            {
                var size = g.MeasureString(message, label.Font, label.MaximumSize.Width);
                label.Height = (int)Math.Ceiling(size.Height) + 20;
            }

            // 圆角效果
            panel.Paint += (s, e) =>
            {
                var rect = new Rectangle(0, 0, panel.Width, panel.Height);
                var radius = 12;
                var path = GetRoundedRectPath(rect, radius);

                using var brush = new SolidBrush(label.BackColor);
                e.Graphics.FillPath(brush, path);

                // 添加阴影效果
                if (!isUser)
                {
                    using var shadowBrush = new SolidBrush(Color.FromArgb(20, 0, 0, 0));
                    e.Graphics.TranslateTransform(2, 2);
                    e.Graphics.FillPath(shadowBrush, path);
                    e.Graphics.TranslateTransform(-2, -2);
                }
            };

            panel.Controls.Add(label);
            return panel;
        }

        private static GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            var diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        private void ScrollToBottom()
        {
            _panelMessages.ScrollControlIntoView(_panelMessages.Controls[_panelMessages.Controls.Count - 1]);
        }

        private void ClearChat()
        {
            _panelMessages.Controls.Clear();
            AddWelcomeMessage();
        }

        /// <summary>
        /// 设置学习上下文
        /// </summary>
        public void SetLearningContext(string context)
        {
            _contextService?.SetLearningContext(context);
        }

        /// <summary>
        /// 获取当前导师角色
        /// </summary>
        public MentorPersonaType CurrentPersona
        {
            get
            {
                if (_comboPersona.SelectedItem is PersonaItem item)
                    return item.Type;
                return MentorPersonaType.Tutor;
            }
        }

        #endregion

        #region 辅助类

        private class PersonaItem
        {
            public MentorPersonaType Type { get; }
            public string DisplayName { get; }
            public string Icon { get; }

            public PersonaItem(MentorPersonaType type, string displayName)
            {
                Type = type;
                DisplayName = displayName;
                Icon = displayName.Split(' ')[0];
            }

            public override string ToString() => DisplayName;
        }

        #endregion
    }
}
