using LearningAssistant.Models.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.ComponentModel;
using System.Text;

namespace LearningAssistant.Views.UI
{
    public class AIAbilityPanel : UserControl
    {
        private readonly ILogger<AIAbilityPanel>? _logger;
        private WebView2? _webView;
        private bool _isWebViewInitialized = false;
        private string? _pendingPrompt;
        private string? _pendingUrl;

        private Panel _panelTop;
        private Panel _panelRadioButtons;
        private TextBox _textBoxPrompt;
        private Button _buttonSend;
        private Panel _panelBrowser;
        private Label _labelStatus;
        private Panel _panelQuickActions;
        private Button _buttonExplain;
        private Button _buttonTranslate;
        private Button _buttonSummarize;
        private Button _buttonExercise;
        private Button _buttonGrammar;
        private Button _buttonWriting;
        private Button _buttonExpand;
        private Button _buttonSimplify;
        private Panel _panelTools;
        private Button _buttonClear;
        private Button _buttonCopy;
        private Button _buttonFavorite;
        private Button _buttonExportHistory;
        private ComboBox _comboBoxHistory;
        private ListBox _listBoxSuggestions;
        private readonly Dictionary<string, RadioButton> _radioButtons = new();
        private List<string> _promptHistory = new();
        private List<ConversationRecord> _conversationHistory = new();
        private string? _currentContext;
        private const int MaxHistoryCount = 20;
        private const int MaxConversationCount = 100;

        // 对话记录结构
        private class ConversationRecord
        {
            public DateTime Timestamp { get; set; }
            public string AIProvider { get; set; } = "";
            public string Prompt { get; set; } = "";
            public string? Context { get; set; }
        }

        // 智能提示词建议模板
        private readonly Dictionary<string, string> _promptTemplates = new()
        {
            { "解释", "请解释以下内容：" },
            { "翻译", "请将以下内容翻译成中文：" },
            { "总结", "请总结以下内容：" },
            { "练习", "请根据以下内容生成练习题：" },
            { "语法", "请分析以下文本的语法结构：" },
            { "写作", "请对以下写作内容提供改进建议：" },
            { "扩写", "请扩写以下内容：" },
            { "简化", "请简化以下内容：" },
            { "举例", "请举例说明以下概念：" },
            { "对比", "请对比分析以下内容：" },
            { "应用", "请说明以下内容在实际中的应用：" },
            { "原理", "请解释以下内容的原理：" },
            { "步骤", "请列出以下操作的步骤：" },
            { "原因", "请分析以下现象的原因：" },
            { "影响", "请分析以下内容的影响：" },
            { "优缺点", "请分析以下内容的优缺点：" },
            { "定义", "请给出以下概念的定义：" },
            { "关系", "请分析以下内容之间的关系：" },
            { "分类", "请对以下内容进行分类：" },
            { "推导", "请推导以下结论：" }
        };

        public bool IsWebViewInitialized => _isWebViewInitialized;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string PromptText
        {
            get => _textBoxPrompt?.Text ?? string.Empty;
            set
            {
                if (_textBoxPrompt != null && !_textBoxPrompt.IsDisposed)
                {
                    _textBoxPrompt.Text = value;
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentAIUrl
        {
            get
            {
                foreach (var kvp in _radioButtons)
                {
                    if (kvp.Value.Checked)
                    {
                        return GetProviderWebViewUrl(kvp.Key);
                    }
                }
                return GetDefaultWebViewUrl();
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                foreach (var kvp in _radioButtons)
                {
                    string providerUrl = GetProviderWebViewUrl(kvp.Key);
                    if (value.Equals(providerUrl, StringComparison.OrdinalIgnoreCase))
                    {
                        kvp.Value.Checked = true;
                        return;
                    }
                }
                _radioButtons.Values.FirstOrDefault()?.Checked = true;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentAIName
        {
            get
            {
                foreach (var kvp in _radioButtons)
                {
                    if (kvp.Value.Checked)
                    {
                        return GetProviderName(kvp.Key);
                    }
                }
                return "豆包";
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                foreach (var kvp in _radioButtons)
                {
                    string providerName = GetProviderName(kvp.Key);
                    if (value.Equals(providerName, StringComparison.OrdinalIgnoreCase) ||
                        value.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        kvp.Value.Checked = true;
                        return;
                    }
                }
                _radioButtons.Values.FirstOrDefault()?.Checked = true;
            }
        }

        /// <summary>
        /// 当前上下文文本（如PDF选中的内容）
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? ContextText
        {
            get => _currentContext;
            set => _currentContext = value;
        }

        /// <summary>
        /// 是否为暗色模式
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsNightMode { get; private set; }

        /// <summary>
        /// 设置暗色模式
        /// </summary>
        /// <param name="enable">是否启用暗色模式</param>
        public void SetNightMode(bool enable)
        {
            IsNightMode = enable;

            if (enable)
            {
                // 暗色模式
                _panelTop.BackColor = Color.FromArgb(40, 40, 40);
                _panelRadioButtons.BackColor = Color.FromArgb(45, 45, 45);
                _panelQuickActions.BackColor = Color.FromArgb(45, 45, 45);

                _panelBrowser.BackColor = Color.FromArgb(30, 30, 30);
                _textBoxPrompt.BackColor = Color.FromArgb(50, 50, 50);
                _textBoxPrompt.ForeColor = Color.White;
                _comboBoxHistory.BackColor = Color.FromArgb(50, 50, 50);
                _comboBoxHistory.ForeColor = Color.White;
                _listBoxSuggestions.BackColor = Color.FromArgb(50, 50, 50);
                _listBoxSuggestions.ForeColor = Color.White;
                _labelStatus.ForeColor = Color.Gray;

                SetButtonDarkMode(_buttonExplain);
                SetButtonDarkMode(_buttonTranslate);
                SetButtonDarkMode(_buttonSummarize);
                SetButtonDarkMode(_buttonExercise);
                SetButtonDarkMode(_buttonGrammar);
                SetButtonDarkMode(_buttonWriting);
                SetButtonDarkMode(_buttonExpand);
                SetButtonDarkMode(_buttonSimplify);

                _buttonClear.BackColor = Color.FromArgb(100, 50, 50);
                _buttonClear.ForeColor = Color.White;
                _buttonCopy.BackColor = Color.FromArgb(50, 100, 50);
                _buttonCopy.ForeColor = Color.White;
                _buttonFavorite.BackColor = Color.FromArgb(120, 80, 30);
                _buttonFavorite.ForeColor = Color.White;
                _buttonExportHistory.BackColor = Color.FromArgb(50, 80, 120);
                _buttonExportHistory.ForeColor = Color.White;
            }
            else
            {
                // 亮色模式（恢复默认）
                _panelTop.BackColor = Color.FromArgb(245, 245, 245);
                _panelRadioButtons.BackColor = Color.FromArgb(245, 245, 245);
                _panelQuickActions.BackColor = Color.FromArgb(250, 250, 250);

                _panelBrowser.BackColor = Color.White;
                _textBoxPrompt.BackColor = Color.White;
                _textBoxPrompt.ForeColor = Color.Black;
                _comboBoxHistory.BackColor = Color.White;
                _comboBoxHistory.ForeColor = Color.Black;
                _listBoxSuggestions.BackColor = Color.FromArgb(250, 250, 250);
                _listBoxSuggestions.ForeColor = Color.Black;
                _labelStatus.ForeColor = Color.Gray;

                SetButtonLightMode(_buttonExplain, Color.FromArgb(100, 180, 100));
                SetButtonLightMode(_buttonTranslate, Color.FromArgb(70, 150, 200));
                SetButtonLightMode(_buttonSummarize, Color.FromArgb(180, 100, 180));
                SetButtonLightMode(_buttonExercise, Color.FromArgb(200, 120, 100));
                SetButtonLightMode(_buttonGrammar, Color.FromArgb(120, 100, 200));
                SetButtonLightMode(_buttonWriting, Color.FromArgb(100, 150, 180));
                SetButtonLightMode(_buttonExpand, Color.FromArgb(180, 180, 100));
                SetButtonLightMode(_buttonSimplify, Color.FromArgb(150, 150, 150));

                _buttonClear.BackColor = Color.FromArgb(240, 240, 240);
                _buttonClear.ForeColor = Color.FromArgb(100, 100, 100);
                _buttonCopy.BackColor = Color.FromArgb(240, 240, 240);
                _buttonCopy.ForeColor = Color.FromArgb(100, 100, 100);
                _buttonFavorite.BackColor = Color.FromArgb(255, 200, 100);
                _buttonFavorite.ForeColor = Color.FromArgb(80, 80, 80);
                _buttonExportHistory.BackColor = Color.FromArgb(100, 150, 200);
                _buttonExportHistory.ForeColor = Color.White;
            }
        }

        private void SetButtonDarkMode(Button? button)
        {
            if (button == null || button.IsDisposed) return;
            button.BackColor = Color.FromArgb(60, 60, 60);
            button.ForeColor = Color.White;
        }

        private void SetButtonLightMode(Button? button, Color backColor)
        {
            if (button == null || button.IsDisposed) return;
            button.BackColor = backColor;
            button.ForeColor = Color.White;
        }

        public AIAbilityPanel(ILogger<AIAbilityPanel>? logger = null)
        {
            _logger = logger;
            InitializeComponent();

            // 【修复】构造函数补充调用生成服务商单选按钮
            CreateRadioButtons();

            _textBoxPrompt.KeyDown += TextBoxPrompt_KeyDown;
            _textBoxPrompt.TextChanged += TextBoxPrompt_TextChanged;

            UpdateHistoryComboBox();
        }

        private void TextBoxPrompt_TextChanged(object? sender, EventArgs e)
        {
            // 【修复】删除强行抢焦点逻辑，会干扰正常输入
            var input = _textBoxPrompt.Text.Trim();

            if (string.IsNullOrEmpty(input) || input.Length < 1)
            {
                HideSuggestions();
                return;
            }

            var suggestions = _promptTemplates
                .Where(kv => kv.Key.Contains(input) || input.Contains(kv.Key))
                .Select(kv => $"{kv.Key}: {kv.Value}")
                .Take(5)
                .ToList();

            if (suggestions.Any())
            {
                ShowSuggestions(suggestions);
            }
            else
            {
                HideSuggestions();
            }
        }

        private void ShowSuggestions(List<string> suggestions)
        {
            _listBoxSuggestions.Items.Clear();
            foreach (var suggestion in suggestions)
            {
                _listBoxSuggestions.Items.Add(suggestion);
            }
            _listBoxSuggestions.Visible = true;
            _listBoxSuggestions.BringToFront();
        }

        private void HideSuggestions()
        {
            _listBoxSuggestions.Visible = false;
        }

        private void ListBoxSuggestions_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_listBoxSuggestions.SelectedIndex < 0) return;

            var selected = _listBoxSuggestions.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selected)) return;

            var parts = selected.Split(':', 2);
            if (parts.Length > 1)
            {
                var template = parts[1].Trim();
                var context = GetContextWithPrompt();
                _textBoxPrompt.Text = $"{template}\n{context}";
            }
            HideSuggestions();
        }

        private void TextBoxPrompt_KeyDown(object? sender, KeyEventArgs e)
        {
            if (_listBoxSuggestions.Visible)
            {
                switch (e.KeyCode)
                {
                    case Keys.Down:
                        e.SuppressKeyPress = true;
                        if (_listBoxSuggestions.SelectedIndex < _listBoxSuggestions.Items.Count - 1)
                            _listBoxSuggestions.SelectedIndex++;
                        return;
                    case Keys.Up:
                        e.SuppressKeyPress = true;
                        if (_listBoxSuggestions.SelectedIndex > 0)
                            _listBoxSuggestions.SelectedIndex--;
                        return;
                    case Keys.Enter:
                    case Keys.Tab:
                        e.SuppressKeyPress = true;
                        ListBoxSuggestions_SelectedIndexChanged(sender, e);
                        return;
                    case Keys.Escape:
                        e.SuppressKeyPress = true;
                        HideSuggestions();
                        return;
                }
            }

            if (e.Control && e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ButtonSend_Click(sender, e);
            }
            else if (e.Control && e.KeyCode == Keys.C && string.IsNullOrEmpty(_textBoxPrompt.SelectedText))
            {
                ButtonCopy_Click(sender, e);
            }
            else if (e.KeyCode == Keys.Escape && !_listBoxSuggestions.Visible)
            {
                ButtonClear_Click(sender, e);
            }
        }

        private void InitializeComponent()
        {
            _panelTop = new Panel();
            _textBoxPrompt = new TextBox();
            _comboBoxHistory = new ComboBox();
            _panelTools = new Panel();
            _buttonClear = new Button();
            _buttonCopy = new Button();
            _buttonFavorite = new Button();
            _buttonExportHistory = new Button();
            _buttonSend = new Button();
            _listBoxSuggestions = new ListBox();
            _panelQuickActions = new Panel();
            _buttonExplain = new Button();
            _buttonTranslate = new Button();
            _buttonSummarize = new Button();
            _buttonExercise = new Button();
            _buttonGrammar = new Button();
            _buttonWriting = new Button();
            _buttonExpand = new Button();
            _buttonSimplify = new Button();
            _panelRadioButtons = new Panel();
            _panelBrowser = new Panel();
            _labelStatus = new Label(); // 【修复】实例化状态Label
            _panelTop.SuspendLayout();
            _panelTools.SuspendLayout();
            _panelQuickActions.SuspendLayout();
            SuspendLayout();

            // labelStatus
            _labelStatus.Dock = DockStyle.Bottom;
            _labelStatus.Height = 22;
            _labelStatus.Text = "就绪";
            _labelStatus.ForeColor = Color.Gray;

            // _panelTop
            _panelTop.BackColor = Color.FromArgb(245, 245, 245);
            _panelTop.Controls.Add(_panelQuickActions);
            _panelTop.Controls.Add(_panelRadioButtons);
            _panelTop.Dock = DockStyle.Top;
            _panelTop.Location = new Point(0, 0);
            _panelTop.Name = "_panelTop";
            _panelTop.Size = new Size(1263, 134);
            _panelTop.TabIndex = 1;

            // _textBoxPrompt
            _textBoxPrompt.Font = new Font("微软雅黑", 10F);
            _textBoxPrompt.Location = new Point(163, 36);
            _textBoxPrompt.Margin = new Padding(5);
            _textBoxPrompt.Name = "_textBoxPrompt";
            _textBoxPrompt.PlaceholderText = "输入提示词或问题...";
            _textBoxPrompt.Size = new Size(683, 25);
            _textBoxPrompt.TabIndex = 0;

            // _comboBoxHistory
            _comboBoxHistory.DropDownStyle = ComboBoxStyle.DropDownList;
            _comboBoxHistory.Font = new Font("微软雅黑", 9F);
            _comboBoxHistory.FormattingEnabled = true;
            _comboBoxHistory.Location = new Point(5, 66);
            _comboBoxHistory.MaxDropDownItems = 10;
            _comboBoxHistory.Name = "_comboBoxHistory";
            _comboBoxHistory.Size = new Size(150, 25);
            _comboBoxHistory.TabIndex = 3;
            _comboBoxHistory.SelectedIndexChanged += ComboBoxHistory_SelectedIndexChanged;

            // _panelTools
            _panelTools.Controls.Add(_buttonClear);
            _panelTools.Controls.Add(_buttonCopy);
            _panelTools.Controls.Add(_buttonFavorite);
            _panelTools.Controls.Add(_buttonExportHistory);
            _panelTools.Controls.Add(_buttonSend);
            _panelTools.Location = new Point(854, 26);
            _panelTools.Name = "_panelTools";
            _panelTools.Size = new Size(417, 45);
            _panelTools.TabIndex = 4;

            // _buttonClear
            _buttonClear.BackColor = Color.FromArgb(230, 100, 100);
            _buttonClear.FlatAppearance.BorderSize = 0;
            _buttonClear.FlatStyle = FlatStyle.Flat;
            _buttonClear.Font = new Font("微软雅黑", 8F);
            _buttonClear.ForeColor = Color.White;
            _buttonClear.Location = new Point(0, 5);
            _buttonClear.Name = "_buttonClear";
            _buttonClear.Size = new Size(61, 30);
            _buttonClear.TabIndex = 0;
            _buttonClear.Text = "🗑️ 清空";
            _buttonClear.UseVisualStyleBackColor = false;
            _buttonClear.Click += ButtonClear_Click;

            // _buttonCopy
            _buttonCopy.BackColor = Color.FromArgb(100, 180, 100);
            _buttonCopy.FlatAppearance.BorderSize = 0;
            _buttonCopy.FlatStyle = FlatStyle.Flat;
            _buttonCopy.Font = new Font("微软雅黑", 8F);
            _buttonCopy.ForeColor = Color.White;
            _buttonCopy.Location = new Point(66, 5);
            _buttonCopy.Name = "_buttonCopy";
            _buttonCopy.Size = new Size(61, 30);
            _buttonCopy.TabIndex = 1;
            _buttonCopy.Text = "📋 复制";
            _buttonCopy.UseVisualStyleBackColor = false;
            _buttonCopy.Click += ButtonCopy_Click;

            // _buttonFavorite
            _buttonFavorite.BackColor = Color.FromArgb(255, 200, 100);
            _buttonFavorite.FlatAppearance.BorderSize = 0;
            _buttonFavorite.FlatStyle = FlatStyle.Flat;
            _buttonFavorite.Font = new Font("微软雅黑", 8F);
            _buttonFavorite.ForeColor = Color.FromArgb(80, 80, 80);
            _buttonFavorite.Location = new Point(132, 3);
            _buttonFavorite.Name = "_buttonFavorite";
            _buttonFavorite.Size = new Size(61, 30);
            _buttonFavorite.TabIndex = 2;
            _buttonFavorite.Text = "⭐收藏";
            _buttonFavorite.UseVisualStyleBackColor = false;
            _buttonFavorite.Click += ButtonFavorite_Click;

            // _buttonExportHistory
            _buttonExportHistory.BackColor = Color.FromArgb(100, 150, 200);
            _buttonExportHistory.FlatAppearance.BorderSize = 0;
            _buttonExportHistory.FlatStyle = FlatStyle.Flat;
            _buttonExportHistory.Font = new Font("微软雅黑", 8F);
            _buttonExportHistory.ForeColor = Color.White;
            _buttonExportHistory.Location = new Point(198, 3);
            _buttonExportHistory.Name = "_buttonExportHistory";
            _buttonExportHistory.Size = new Size(61, 30);
            _buttonExportHistory.TabIndex = 3;
            _buttonExportHistory.Text = "📤 导出";
            _buttonExportHistory.UseVisualStyleBackColor = false;
            _buttonExportHistory.Click += ButtonExportHistory_Click;

            // _buttonSend
            _buttonSend.BackColor = Color.DodgerBlue;
            _buttonSend.FlatAppearance.BorderSize = 0;
            _buttonSend.FlatStyle = FlatStyle.Flat;
            _buttonSend.Font = new Font("微软雅黑", 8F);
            _buttonSend.ForeColor = Color.White;
            _buttonSend.Location = new Point(264, 3);
            _buttonSend.Name = "_buttonSend";
            _buttonSend.Size = new Size(61, 30);
            _buttonSend.TabIndex = 1;
            _buttonSend.Text = "➤ 发送";
            _buttonSend.UseVisualStyleBackColor = false;
            _buttonSend.Click += ButtonSend_Click;

            // _listBoxSuggestions
            _listBoxSuggestions.BackColor = Color.FromArgb(250, 250, 250);
            _listBoxSuggestions.BorderStyle = BorderStyle.FixedSingle;
            _listBoxSuggestions.Font = new Font("微软雅黑", 9F);
            _listBoxSuggestions.FormattingEnabled = true;
            _listBoxSuggestions.Location = new Point(163, 61);
            _listBoxSuggestions.Name = "_listBoxSuggestions";
            _listBoxSuggestions.Size = new Size(683, 138);
            _listBoxSuggestions.TabIndex = 5;
            _listBoxSuggestions.Visible = false;
            _listBoxSuggestions.SelectedIndexChanged += ListBoxSuggestions_SelectedIndexChanged;

            // _panelQuickActions
            _panelQuickActions.BackColor = Color.FromArgb(250, 250, 250);
            _panelQuickActions.Controls.Add(_panelTools);
            _panelQuickActions.Controls.Add(_textBoxPrompt);
            _panelQuickActions.Controls.Add(_listBoxSuggestions);
            _panelQuickActions.Controls.Add(_buttonExplain);
            _panelQuickActions.Controls.Add(_comboBoxHistory);
            _panelQuickActions.Controls.Add(_buttonTranslate);
            _panelQuickActions.Controls.Add(_buttonSummarize);
            _panelQuickActions.Controls.Add(_buttonExercise);
            _panelQuickActions.Controls.Add(_buttonGrammar);
            _panelQuickActions.Controls.Add(_buttonWriting);
            _panelQuickActions.Controls.Add(_buttonExpand);
            _panelQuickActions.Controls.Add(_buttonSimplify);
            _panelQuickActions.Dock = DockStyle.Fill;
            _panelQuickActions.Location = new Point(0, 28);
            _panelQuickActions.Name = "_panelQuickActions";
            _panelQuickActions.Padding = new Padding(5, 3, 5, 3);
            _panelQuickActions.Size = new Size(1263, 106);
            _panelQuickActions.TabIndex = 3;

            // _buttonExplain
            _buttonExplain.BackColor = Color.FromArgb(100, 180, 100);
            _buttonExplain.FlatAppearance.BorderSize = 0;
            _buttonExplain.FlatStyle = FlatStyle.Flat;
            _buttonExplain.Font = new Font("微软雅黑", 9F);
            _buttonExplain.ForeColor = Color.White;
            _buttonExplain.Location = new Point(5, 3);
            _buttonExplain.Name = "_buttonExplain";
            _buttonExplain.Size = new Size(75, 24);
            _buttonExplain.TabIndex = 0;
            _buttonExplain.Text = "📖 解释";
            _buttonExplain.UseVisualStyleBackColor = false;
            _buttonExplain.Click += ButtonExplain_Click;

            // _buttonTranslate
            _buttonTranslate.BackColor = Color.FromArgb(70, 150, 200);
            _buttonTranslate.FlatAppearance.BorderSize = 0;
            _buttonTranslate.FlatStyle = FlatStyle.Flat;
            _buttonTranslate.Font = new Font("微软雅黑", 9F);
            _buttonTranslate.ForeColor = Color.White;
            _buttonTranslate.Location = new Point(82, 3);
            _buttonTranslate.Name = "_buttonTranslate";
            _buttonTranslate.Size = new Size(75, 24);
            _buttonTranslate.TabIndex = 1;
            _buttonTranslate.Text = "🌐 翻译";
            _buttonTranslate.UseVisualStyleBackColor = false;
            _buttonTranslate.Click += ButtonTranslate_Click;

            // _buttonSummarize
            _buttonSummarize.BackColor = Color.FromArgb(180, 100, 180);
            _buttonSummarize.FlatAppearance.BorderSize = 0;
            _buttonSummarize.FlatStyle = FlatStyle.Flat;
            _buttonSummarize.Font = new Font("微软雅黑", 9F);
            _buttonSummarize.ForeColor = Color.White;
            _buttonSummarize.Location = new Point(159, 3);
            _buttonSummarize.Name = "_buttonSummarize";
            _buttonSummarize.Size = new Size(75, 24);
            _buttonSummarize.TabIndex = 2;
            _buttonSummarize.Text = "📝 总结";
            _buttonSummarize.UseVisualStyleBackColor = false;
            _buttonSummarize.Click += ButtonSummarize_Click;

            // _buttonExercise
            _buttonExercise.BackColor = Color.FromArgb(200, 120, 100);
            _buttonExercise.FlatAppearance.BorderSize = 0;
            _buttonExercise.FlatStyle = FlatStyle.Flat;
            _buttonExercise.Font = new Font("微软雅黑", 9F);
            _buttonExercise.ForeColor = Color.White;
            _buttonExercise.Location = new Point(236, 3);
            _buttonExercise.Name = "_buttonExercise";
            _buttonExercise.Size = new Size(90, 24);
            _buttonExercise.TabIndex = 3;
            _buttonExercise.Text = "✏️ 生成练习";
            _buttonExercise.UseVisualStyleBackColor = false;
            _buttonExercise.Click += ButtonExercise_Click;

            // _buttonGrammar
            _buttonGrammar.BackColor = Color.FromArgb(120, 100, 200);
            _buttonGrammar.FlatAppearance.BorderSize = 0;
            _buttonGrammar.FlatStyle = FlatStyle.Flat;
            _buttonGrammar.Font = new Font("微软雅黑", 9F);
            _buttonGrammar.ForeColor = Color.White;
            _buttonGrammar.Location = new Point(349, 3);
            _buttonGrammar.Name = "_buttonGrammar";
            _buttonGrammar.Size = new Size(90, 24);
            _buttonGrammar.TabIndex = 4;
            _buttonGrammar.Text = "📚 语法分析";
            _buttonGrammar.UseVisualStyleBackColor = false;
            _buttonGrammar.Click += ButtonGrammar_Click;

            // _buttonWriting
            _buttonWriting.BackColor = Color.FromArgb(100, 150, 180);
            _buttonWriting.FlatAppearance.BorderSize = 0;
            _buttonWriting.FlatStyle = FlatStyle.Flat;
            _buttonWriting.Font = new Font("微软雅黑", 9F);
            _buttonWriting.ForeColor = Color.White;
            _buttonWriting.Location = new Point(441, 3);
            _buttonWriting.Name = "_buttonWriting";
            _buttonWriting.Size = new Size(90, 24);
            _buttonWriting.TabIndex = 5;
            _buttonWriting.Text = "✍️ 写作建议";
            _buttonWriting.UseVisualStyleBackColor = false;
            _buttonWriting.Click += ButtonWriting_Click;

            // _buttonExpand
            _buttonExpand.BackColor = Color.FromArgb(180, 180, 100);
            _buttonExpand.FlatAppearance.BorderSize = 0;
            _buttonExpand.FlatStyle = FlatStyle.Flat;
            _buttonExpand.Font = new Font("微软雅黑", 9F);
            _buttonExpand.ForeColor = Color.White;
            _buttonExpand.Location = new Point(533, 3);
            _buttonExpand.Name = "_buttonExpand";
            _buttonExpand.Size = new Size(75, 24);
            _buttonExpand.TabIndex = 6;
            _buttonExpand.Text = "📈 扩写";
            _buttonExpand.UseVisualStyleBackColor = false;
            _buttonExpand.Click += ButtonExpand_Click;

            // _buttonSimplify
            _buttonSimplify.BackColor = Color.FromArgb(150, 150, 150);
            _buttonSimplify.FlatAppearance.BorderSize = 0;
            _buttonSimplify.FlatStyle = FlatStyle.Flat;
            _buttonSimplify.Font = new Font("微软雅黑", 9F);
            _buttonSimplify.ForeColor = Color.White;
            _buttonSimplify.Location = new Point(610, 3);
            _buttonSimplify.Name = "_buttonSimplify";
            _buttonSimplify.Size = new Size(80, 24);
            _buttonSimplify.TabIndex = 7;
            _buttonSimplify.Text = "📉 简化";
            _buttonSimplify.UseVisualStyleBackColor = false;
            _buttonSimplify.Click += ButtonSimplify_Click;

            // _panelRadioButtons
            _panelRadioButtons.BackColor = Color.FromArgb(245, 245, 245);
            _panelRadioButtons.Dock = DockStyle.Top;
            _panelRadioButtons.Location = new Point(0, 0);
            _panelRadioButtons.Name = "_panelRadioButtons";
            _panelRadioButtons.Padding = new Padding(5);
            _panelRadioButtons.Size = new Size(1263, 28);
            _panelRadioButtons.TabIndex = 2;

            // _panelBrowser
            _panelBrowser.BackColor = Color.White;
            _panelBrowser.Dock = DockStyle.Fill;
            _panelBrowser.Location = new Point(0, 134);
            _panelBrowser.Name = "_panelBrowser";
            _panelBrowser.Size = new Size(1263, 512);
            _panelBrowser.TabIndex = 0;

            // AIAbilityPanel
            Controls.Add(_panelBrowser);
            Controls.Add(_labelStatus);  // 【修复】状态栏加入控件树
            Controls.Add(_panelTop);
            Name = "AIAbilityPanel";
            Size = new Size(1263, 668);
            _panelTop.ResumeLayout(false);
            _panelTools.ResumeLayout(false);
            _panelQuickActions.ResumeLayout(false);
            _panelQuickActions.PerformLayout();
            ResumeLayout(false);
        }

        private void CreateRadioButtons()
        {
            int xPos = 5;
            int spacing = 85;

            foreach (var provider in AiConfig.Providers)
            {
                string providerKey = provider.Key;
                string providerName = provider.Value.Name;

                var radio = new RadioButton();
                radio.Text = providerName.Contains('(')
                    ? providerName.Substring(0, providerName.IndexOf('('))
                    : providerName;
                radio.Location = new Point(xPos, 3);
                int textWidth = TextRenderer.MeasureText(providerName, new Font("微软雅黑", 9F)).Width + 20;
                radio.Size = new Size(Math.Min(spacing - 10, textWidth), 22);
                radio.Font = new Font("微软雅黑", 9F);
                radio.FlatStyle = FlatStyle.Flat;
                radio.Tag = providerKey;
                radio.CheckedChanged += RadioButton_CheckedChanged;

                _radioButtons[providerKey] = radio;
                _panelRadioButtons.Controls.Add(radio);
                xPos += spacing;

                if (_radioButtons.Count == 1)
                    radio.Checked = true;
            }
        }

        private void RadioButton_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is RadioButton radio && radio.Checked)
            {
                // 当选择不同的AI厂家时，刷新WebView到新URL
                if (_isWebViewInitialized && _webView != null)
                {
                    string url = GetProviderWebViewUrl(radio.Tag?.ToString() ?? "doubao");
                    _webView.Source = new Uri(url);
                    SetStatus($"已切换到: {GetProviderName(radio.Tag?.ToString() ?? "doubao")}");
                }
            }
        }

        private string GetProviderWebViewUrl(string providerKey)
        {
            if (AiConfig.Providers.TryGetValue(providerKey, out var info))
            {
                return info.WebViewUrl;
            }
            return "https://www.doubao.com/chat";
        }

        private string GetProviderName(string providerKey)
        {
            if (AiConfig.Providers.TryGetValue(providerKey, out var info))
            {
                return info.Name;
            }
            return providerKey;
        }

        private string GetDefaultWebViewUrl()
        {
            if (AiConfig.Providers.TryGetValue("doubao", out var info))
            {
                return info.WebViewUrl;
            }
            return "https://www.doubao.com/chat";
        }

        private async void ButtonSend_Click(object? sender, EventArgs e)
        {
            var prompt = _textBoxPrompt?.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                MessageBox.Show("请输入提示词", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            AddToHistory(prompt);
            SaveConversationRecord(prompt);

            try
            {
                SetStatus("正在打开AI网页...");

                if (!_isWebViewInitialized || _webView == null)
                {
                    SetStatus("正在初始化浏览器...");
                    await InitializeWebViewAsync();
                    if (!_isWebViewInitialized)
                    {
                        ShowError("浏览器初始化失败，无法发送");
                        return;
                    }
                }

                OpenWebView(CurrentAIUrl, prompt);
            }
            catch (Exception ex)
            {
                ShowError($"发送失败: {ex.Message}");
                _logger?.LogError(ex, "发送按钮异常");
            }
        }

        private void SaveConversationRecord(string prompt)
        {
            var record = new ConversationRecord
            {
                Timestamp = DateTime.Now,
                AIProvider = CurrentAIName,
                Prompt = prompt,
                Context = _currentContext
            };

            _conversationHistory.Add(record);
            if (_conversationHistory.Count > MaxConversationCount)
                _conversationHistory.RemoveAt(0);
        }

        public void ExportConversationHistory(string filePath)
        {
            try
            {
                using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                writer.WriteLine("AI对话历史记录");
                writer.WriteLine($"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"总记录数: {_conversationHistory.Count}");
                writer.WriteLine(new string('-', 50));

                foreach (var record in _conversationHistory)
                {
                    writer.WriteLine($"时间: {record.Timestamp:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"AI: {record.AIProvider}");
                    writer.WriteLine($"提示词: {record.Prompt}");
                    if (!string.IsNullOrEmpty(record.Context))
                        writer.WriteLine($"上下文: {record.Context}");
                    writer.WriteLine(new string('-', 30));
                }

                SetStatus($"已导出 {_conversationHistory.Count} 条记录");
            }
            catch (Exception ex)
            {
                ShowError($"导出失败: {ex.Message}");
                _logger?.LogError(ex, "导出历史异常");
            }
        }

        public void ClearConversationHistory()
        {
            _conversationHistory.Clear();
            SetStatus("对话历史已清空");
        }

        #region 快捷操作按钮
        private void ButtonExplain_Click(object? sender, EventArgs e)
        {
            var context = GetContextWithPrompt();
            _textBoxPrompt.Text = $"请解释以下内容：\n{context}";
        }

        private void ButtonTranslate_Click(object? sender, EventArgs e)
        {
            var context = GetContextWithPrompt();
            _textBoxPrompt.Text = $"请将以下内容翻译成中文：\n{context}";
        }

        private void ButtonSummarize_Click(object? sender, EventArgs e)
        {
            var context = GetContextWithPrompt();
            _textBoxPrompt.Text = $"请总结以下内容：\n{context}";
        }

        private void ButtonExercise_Click(object? sender, EventArgs e)
        {
            var context = GetContextWithPrompt();
            _textBoxPrompt.Text = $"请根据以下内容生成练习题（包括选择题和填空题）：\n{context}";
        }

        private void ButtonGrammar_Click(object? sender, EventArgs e)
        {
            var context = GetContextWithPrompt();
            _textBoxPrompt.Text = $"请分析以下文本的语法结构：\n{context}";
        }

        private void ButtonWriting_Click(object? sender, EventArgs e)
        {
            var context = GetContextWithPrompt();
            _textBoxPrompt.Text = $"请对以下写作内容提供改进建议：\n{context}";
        }

        private void ButtonExpand_Click(object? sender, EventArgs e)
        {
            var context = GetContextWithPrompt();
            _textBoxPrompt.Text = $"请扩写以下内容，使其更加丰富详细：\n{context}";
        }

        private void ButtonSimplify_Click(object? sender, EventArgs e)
        {
            var context = GetContextWithPrompt();
            _textBoxPrompt.Text = $"请简化以下内容，使其更加简洁明了：\n{context}";
        }

        private string GetContextWithPrompt()
        {
            if (!string.IsNullOrEmpty(_currentContext))
                return _currentContext;
            return string.IsNullOrWhiteSpace(_textBoxPrompt?.Text) ? "请输入需要处理的内容" : _textBoxPrompt.Text;
        }
        #endregion

        #region 工具按钮
        private void ButtonClear_Click(object? sender, EventArgs e)
        {
            _textBoxPrompt?.Clear();
            _currentContext = null;
            SetStatus("已清空");
        }

        private void ButtonCopy_Click(object? sender, EventArgs e)
        {
            var text = _textBoxPrompt?.Text ?? string.Empty;
            if (!string.IsNullOrEmpty(text))
            {
                Clipboard.SetText(text);
                SetStatus("已复制到剪贴板");
            }
            else
            {
                SetStatus("没有可复制的内容");
            }
        }

        private void ButtonFavorite_Click(object? sender, EventArgs e)
        {
            var prompt = _textBoxPrompt?.Text ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(prompt))
            {
                AddToHistory(prompt, true);
                SetStatus("已收藏");
            }
            else
            {
                SetStatus("没有可收藏的内容");
            }
        }

        private void ButtonExportHistory_Click(object? sender, EventArgs e)
        {
            if (_conversationHistory.Count == 0)
            {
                MessageBox.Show("没有对话历史记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var saveDialog = new SaveFileDialog();
            saveDialog.Filter = "文本文件|*.txt|所有文件|*.*";
            saveDialog.Title = "导出对话历史";
            saveDialog.FileName = $"AI对话历史_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                ExportConversationHistory(saveDialog.FileName);
                MessageBox.Show($"已导出 {_conversationHistory.Count} 条记录到:\n{saveDialog.FileName}", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        #endregion

        #region 历史记录
        private void AddToHistory(string prompt, bool isFavorite = false)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return;

            _promptHistory.Remove(prompt);
            string itemText = isFavorite ? $"⭐ {prompt}" : prompt;
            _promptHistory.Insert(0, itemText);

            if (_promptHistory.Count > MaxHistoryCount)
                _promptHistory.RemoveAt(_promptHistory.Count - 1);

            UpdateHistoryComboBox();
        }

        private void UpdateHistoryComboBox()
        {
            if (_comboBoxHistory == null || _comboBoxHistory.IsDisposed) return;
            _comboBoxHistory.Items.Clear();
            _comboBoxHistory.Items.Add("📜 历史记录");
            foreach (var item in _promptHistory.Take(10))
                _comboBoxHistory.Items.Add(item);
            _comboBoxHistory.SelectedIndex = 0;
        }

        private void ComboBoxHistory_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_comboBoxHistory == null || _comboBoxHistory.SelectedIndex <= 0) return;

            var selected = _comboBoxHistory.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selected)) return;

            if (selected.StartsWith("⭐ "))
                selected = selected.Substring(2);
            _textBoxPrompt.Text = selected;
        }
        #endregion

        public async void OpenWebView(string url, string? prompt = null)
        {
            _pendingUrl = url;
            _pendingPrompt = prompt;

            if (!string.IsNullOrEmpty(url))
                CurrentAIUrl = url;
            if (!string.IsNullOrEmpty(prompt))
                _textBoxPrompt.Text = prompt;

            if (!_isWebViewInitialized)
            {
                SetStatus("正在初始化浏览器...");
                await InitializeWebViewAsync();
            }

            if (_webView != null && !string.IsNullOrEmpty(_pendingUrl))
            {
                SetStatus("正在加载页面...");
                _webView.Source = new Uri(_pendingUrl);
            }
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var cacheDir = Path.Combine(appDataDir, "LearningAssistant", "ai_panel_cache");
                if (!Directory.Exists(cacheDir))
                    Directory.CreateDirectory(cacheDir);

                var environment = await CoreWebView2Environment.CreateAsync(null, cacheDir);

                _webView = new WebView2 { Dock = DockStyle.Fill };
                await _webView.EnsureCoreWebView2Async(environment);

                if (_webView.CoreWebView2 != null)
                {
                    _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                    _webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                    _webView.CoreWebView2.Settings.IsScriptEnabled = true;
                    _webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                    _webView.CoreWebView2.Settings.IsZoomControlEnabled = true;

                    _webView.NavigationCompleted += OnNavigationCompleted;
                    _panelBrowser.Controls.Add(_webView);
                    _isWebViewInitialized = true;
                    SetStatus("浏览器初始化完成");
                }
            }
            catch (Exception ex)
            {
                SetStatus("初始化失败");
                ShowError($"WebView初始化失败: {ex.Message}\n\n请确保已安装WebView2运行时。");
                _logger?.LogError(ex, "WebView2初始化异常");
            }
        }

        private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (_webView == null || !e.IsSuccess)
            {
                if (!e.IsSuccess)
                    ShowError($"页面加载失败 (错误码: {e.HttpStatusCode})");
                return;
            }

            SetStatus("页面加载完成");

            var host = _webView.Source?.Host;
            if (string.IsNullOrEmpty(_pendingPrompt)) return;

            try
            {
                await Task.Delay(1000);
                int retryCount = 0;
                const int maxRetry = 3;
                bool filled = false;

                while (!filled && retryCount < maxRetry)
                {
                    SetStatus($"正在填充提示词... (尝试 {retryCount + 1}/{maxRetry})");
                    string script = GetFillPromptScript(host, _pendingPrompt);
                    var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);

                    if (result.Contains("filled"))
                    {
                        filled = true;
                        SetStatus("就绪");
                    }
                    else
                    {
                        retryCount++;
                        if (retryCount < maxRetry)
                            await Task.Delay(500);
                    }
                }

                if (!filled)
                {
                    _logger?.LogWarning("无法自动填充提示词，执行备用填充");
                    await TryBackupFillMethod();
                }

                _pendingPrompt = null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "填充Prompt JS执行异常");
                ShowError($"填充提示词失败: {ex.Message}");
                SetStatus("就绪");
            }
        }

        private async Task TryBackupFillMethod()
        {
            if (_webView?.CoreWebView2 == null || string.IsNullOrEmpty(_pendingPrompt)) return;

            try
            {
                var escapedPrompt = EscapeForJavaScript(_pendingPrompt);
                string backupScript = $@"(function(){{
                    var textareas = document.getElementsByTagName('textarea');
                    for(var i=0;i<textareas.length;i++){{
                        if(textareas[i].offsetParent !== null){{
                            textareas[i].value = '{escapedPrompt}';
                            textareas[i].focus();
                            textareas[i].dispatchEvent(new Event('input',{{bubbles:true}}));
                            return 'backup success';
                        }}
                    }}
                    return 'backup failed';
                }})();";
                await _webView.CoreWebView2.ExecuteScriptAsync(backupScript);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "备用填充方法失败");
            }
        }

        private async Task FillPromptAsync(string prompt, string? host)
        {
            if (_webView?.CoreWebView2 == null) return;
            try
            {
                string script = GetFillPromptScript(host, prompt);
                if (!script.StartsWith("/*"))
                    await _webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch
            {
                // 静默捕获
            }
        }

        // 【修复】JS双引号转义 + 区分textarea/input/contenteditable赋值，不再重复赋值
        private string GetFillPromptScript(string? host, string prompt)
        {
            if (string.IsNullOrEmpty(host))
                return "/* unknown host */";

            var escapedPrompt = EscapeForJavaScript(prompt);

            return $@"(function(){{
                var selectors = [
                    'textarea',
                    'textarea[placeholder]',
                    'textarea[placeholder*=""输入""]',
                    'textarea[placeholder*=""问题""]',
                    'textarea[placeholder*=""请输入""]',
                    'input[type=""text""]',
                    'input[placeholder*=""输入""]',
                    'input[placeholder*=""问题""]',
                    '[contenteditable=""true""]',
                    '[contenteditable]:not([contenteditable=""false""])',
                    '.chat-input textarea',
                    '#chat-input textarea',
                    '.prompt-textarea',
                    '[class*=""input""] textarea',
                    '[class*=""prompt""] textarea',
                    '[class*=""message""] textarea'
                ];

                var targetEl = null;
                for(var i=0;i<selectors.length;i++){{
                    targetEl = document.querySelector(selectors[i]);
                    if(targetEl) break;
                }}

                if(!targetEl){{
                    var inputContainers = document.querySelectorAll('[class*=""input""],[class*=""chat""]');
                    if(inputContainers.length>0) inputContainers[0].click();
                    return 'not found';
                }}

                var tag = targetEl.tagName.toLowerCase();
                if(tag === 'textarea' || tag === 'input'){{
                    targetEl.value = '{escapedPrompt}';
                }}else if(targetEl.isContentEditable){{
                    targetEl.textContent = '{escapedPrompt}';
                }}

                targetEl.dispatchEvent(new Event('input',{{bubbles:true,cancelable:true}}));
                targetEl.dispatchEvent(new Event('change',{{bubbles:true,cancelable:true}}));
                targetEl.dispatchEvent(new KeyboardEvent('keydown',{{bubbles:true,cancelable:true,key:''}}));
                targetEl.focus();
                return 'filled: ' + selectors[Array.from(selectors).indexOf(targetEl)];
            }})();";
        }

        // 【修复】完整JS字符串转义，杜绝XSS注入
        private static string EscapeForJavaScript(string text)
        {
            if (text == null) return string.Empty;
            return text
                .Replace(@"\", @"\\")
                .Replace("'", @"\'")
                .Replace("\"", "\\\"")
                .Replace("\n", @"\n")
                .Replace("\r", @"\r")
                .Replace("\t", @"\t")
                .Replace("\b", @"\b")
                .Replace("\f", @"\f");
        }

        private void SetStatus(string message)
        {
            if (_labelStatus != null && !_labelStatus.IsDisposed)
                _labelStatus.Text = message;
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_webView != null)
                {
                    _webView.NavigationCompleted -= OnNavigationCompleted;
                    _webView.Dispose();
                    _webView = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
