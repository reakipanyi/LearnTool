using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models;
using LearningAssistant.Models.Config;
using LearningAssistant.Models.UI;
using LearningAssistant.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls
{
    public class AIAbilityPanel : UserControl, IThemeable
    {
        #region 字段
        private readonly ILogger<AIAbilityPanel>? _logger;
        private readonly PromptHistoryService _historyService = new();
        private readonly Dictionary<string, RadioButton> _radioButtons = new();
        private readonly Dictionary<string, Button> _quickActionButtons = new();
        private readonly Dictionary<string, Color> _quickActionOriginalColors = new();
        private readonly Dictionary<Button, Color> _toolButtonOriginalColors = new();

        private WebView2? _webView;
        private bool _isWebViewInitialized = false;
        private string? _pendingPrompt;
        private string? _pendingUrl;
        private string? _currentContext;
        private ThemeMode _currentTheme = ThemeMode.Light;
        private string _statusMessage = "就绪";
        private int _hoveredSuggestionIndex = -1;

        private Panel _panelTop;
        private Panel _panelRadioButtons;
        private TextBox _textBoxPrompt;
        private Button _buttonSend;
        private Panel _panelBrowser;
        private Label _labelStatus;
        private Panel _panelQuickActions;
        private Panel _panelTools;
        private Button _buttonClear;
        private Button _buttonCopy;
        private Button _buttonFavorite;
        private Button _buttonExportHistory;
        private ComboBox _comboBoxHistory;
        private ListBox _listBoxSuggestions;
        #endregion

        #region 属性
        public bool IsWebViewInitialized => _isWebViewInitialized;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string PromptText
        {
            get => _textBoxPrompt?.Text ?? string.Empty;
            set
            {
                if (_textBoxPrompt != null && !_textBoxPrompt.IsDisposed)
                    _textBoxPrompt.Text = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentAIUrl
        {
            get => GetSelectedProviderUrl();
            set => SetSelectedProviderByUrl(value);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentAIName
        {
            get => GetSelectedProviderName();
            set => SetSelectedProviderByName(value);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? ContextText
        {
            get => _currentContext;
            set => _currentContext = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsNightMode => _currentTheme == ThemeMode.Dark;
        #endregion

        #region 构造函数
        public AIAbilityPanel(ILogger<AIAbilityPanel>? logger = null)
        {
            _logger = logger;
            InitializeComponent();
            CreateRadioButtons();
            CreateQuickActionButtons();
            SetupEventHandlers();
            UpdateHistoryComboBox();
        }
        #endregion

        #region IThemeable 实现
        public void ApplyTheme(ThemeColors colors)
        {
            _currentTheme = colors.ThemeMode;
            ApplyThemeToControls(colors);
        }

        private void ApplyThemeToControls(ThemeColors colors)
        {
            var isDark = colors.ThemeMode == ThemeMode.Dark;

            _panelTop.BackColor = isDark ? colors.Surface : Color.FromArgb(245, 245, 245);
            _panelRadioButtons.BackColor = isDark ? colors.SurfaceElevated : Color.FromArgb(245, 245, 245);
            _panelQuickActions.BackColor = isDark ? colors.Surface : Color.FromArgb(250, 250, 250);
            _panelBrowser.BackColor = isDark ? colors.Background : Color.White;

            _textBoxPrompt.BackColor = isDark ? colors.SurfaceElevated : Color.White;
            _textBoxPrompt.ForeColor = isDark ? colors.TextPrimary : Color.Black;
            _comboBoxHistory.BackColor = isDark ? colors.SurfaceElevated : Color.White;
            _comboBoxHistory.ForeColor = isDark ? colors.TextPrimary : Color.Black;
            _listBoxSuggestions.BackColor = isDark ? colors.SurfaceElevated : Color.FromArgb(250, 250, 250);
            _listBoxSuggestions.ForeColor = isDark ? colors.TextPrimary : Color.Black;
            _labelStatus.ForeColor = isDark ? colors.TextSecondary : Color.Gray;
            _labelStatus.BackColor = isDark ? colors.Surface : Color.FromArgb(245, 245, 245);

            foreach (var kvp in _radioButtons)
            {
                kvp.Value.ForeColor = isDark ? colors.TextPrimary : Color.Black;
                kvp.Value.BackColor = isDark ? colors.SurfaceElevated : Color.FromArgb(245, 245, 245);
            }

            foreach (var kvp in _quickActionButtons)
            {
                var config = QuickActionDefinitions.DefaultActions.FirstOrDefault(a => a.Key == kvp.Key);
                if (config != null)
                {
                    var baseColor = isDark ? config.DarkColor : config.LightColor;
                    kvp.Value.BackColor = baseColor;
                    kvp.Value.ForeColor = Color.White;
                    _quickActionOriginalColors[kvp.Key] = baseColor;
                }
            }

            var clearColor = isDark ? Color.FromArgb(180, 70, 70) : Color.FromArgb(230, 100, 100);
            var copyColor = isDark ? Color.FromArgb(70, 140, 70) : Color.FromArgb(100, 180, 100);
            var favoriteColor = isDark ? Color.FromArgb(180, 140, 50) : Color.FromArgb(255, 193, 7);
            var exportColor = isDark ? Color.FromArgb(70, 110, 160) : Color.FromArgb(100, 150, 200);
            var sendColor = isDark ? Color.FromArgb(60, 120, 200) : Color.DodgerBlue;

            ApplyToolButtonTheme(_buttonClear, clearColor);
            ApplyToolButtonTheme(_buttonCopy, copyColor);
            ApplyToolButtonTheme(_buttonFavorite, favoriteColor);
            ApplyToolButtonTheme(_buttonExportHistory, exportColor);
            ApplyToolButtonTheme(_buttonSend, sendColor);

            UpdateStatusText();
        }

        private void ApplyToolButtonTheme(Button button, Color backColor)
        {
            if (button == null || button.IsDisposed) return;
            button.BackColor = backColor;
            button.ForeColor = Color.White;
            _toolButtonOriginalColors[button] = backColor;
        }
        #endregion

        #region 公共方法
        public void SetNightMode(bool enable)
        {
            var colors = ThemeService.GetColors(enable ? ThemeMode.Dark : ThemeMode.Light);
            ApplyTheme(colors);
        }

        public async Task OpenWebViewAsync(string url, string? prompt = null)
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

        public void ExportConversationHistory(string filePath)
        {
            try
            {
                _historyService.ExportConversationHistory(filePath);
                SetStatus($"已导出 {_historyService.ConversationHistory.Count} 条记录");
            }
            catch (Exception ex)
            {
                ShowError($"导出失败: {ex.Message}");
                _logger?.LogError(ex, "导出历史异常");
            }
        }

        public void ClearConversationHistory()
        {
            _historyService.ClearConversationHistory();
            SetStatus("对话历史已清空");
        }
        #endregion

        #region 初始化方法
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
            _panelRadioButtons = new Panel();
            _panelBrowser = new Panel();
            _labelStatus = new Label();

            _panelTop.SuspendLayout();
            _panelTools.SuspendLayout();
            _panelQuickActions.SuspendLayout();
            SuspendLayout();

            SetupStatusLabel();
            SetupTopPanel();
            SetupPromptTextBox();
            SetupHistoryComboBox();
            SetupToolsPanel();
            SetupSuggestionsListBox();
            SetupQuickActionsPanel();
            SetupRadioButtonsPanel();
            SetupBrowserPanel();
            SetupMainPanel();

            _panelTop.ResumeLayout(false);
            _panelTools.ResumeLayout(false);
            _panelQuickActions.ResumeLayout(false);
            _panelQuickActions.PerformLayout();
            ResumeLayout(false);
        }

        private void SetupStatusLabel()
        {
            _labelStatus.Dock = DockStyle.Bottom;
            _labelStatus.Height = 24;
            _labelStatus.Text = "🤖 就绪 | 当前: 豆包";
            _labelStatus.ForeColor = Color.Gray;
            _labelStatus.Font = new Font("微软雅黑", 9F);
            _labelStatus.Padding = new Padding(8, 3, 8, 3);
            _labelStatus.TextAlign = ContentAlignment.MiddleLeft;
        }

        private void SetupTopPanel()
        {
            _panelTop.BackColor = Color.FromArgb(245, 245, 245);
            _panelTop.Controls.Add(_panelQuickActions);
            _panelTop.Controls.Add(_panelRadioButtons);
            _panelTop.Dock = DockStyle.Top;
            _panelTop.Location = new Point(0, 0);
            _panelTop.Name = "_panelTop";
            _panelTop.Size = new Size(1263, 136);
            _panelTop.TabIndex = 1;
        }

        private void SetupPromptTextBox()
        {
            _textBoxPrompt.Font = new Font("微软雅黑", 10F);
            _textBoxPrompt.Location = new Point(163, 34);
            _textBoxPrompt.Margin = new Padding(5);
            _textBoxPrompt.Name = "_textBoxPrompt";
            _textBoxPrompt.PlaceholderText = "输入提示词或问题... (Ctrl+Enter发送, Esc清空)";
            _textBoxPrompt.Size = new Size(683, 28);
            _textBoxPrompt.TabIndex = 0;
            _textBoxPrompt.BorderStyle = BorderStyle.FixedSingle;
            _textBoxPrompt.Cursor = Cursors.IBeam;
        }

        private void SetupHistoryComboBox()
        {
            _comboBoxHistory.DropDownStyle = ComboBoxStyle.DropDownList;
            _comboBoxHistory.Font = new Font("微软雅黑", 9F);
            _comboBoxHistory.FormattingEnabled = true;
            _comboBoxHistory.Location = new Point(5, 68);
            _comboBoxHistory.MaxDropDownItems = 10;
            _comboBoxHistory.Name = "_comboBoxHistory";
            _comboBoxHistory.Size = new Size(150, 28);
            _comboBoxHistory.TabIndex = 3;
            _comboBoxHistory.Cursor = Cursors.Hand;
        }

        private void SetupToolsPanel()
        {
            _panelTools.Controls.Add(_buttonClear);
            _panelTools.Controls.Add(_buttonCopy);
            _panelTools.Controls.Add(_buttonFavorite);
            _panelTools.Controls.Add(_buttonExportHistory);
            _panelTools.Controls.Add(_buttonSend);
            _panelTools.Location = new Point(854, 25);
            _panelTools.Name = "_panelTools";
            _panelTools.Size = new Size(417, 48);
            _panelTools.TabIndex = 4;

            SetupToolButton(_buttonClear, "🗑️ 清空", 0, Color.FromArgb(230, 100, 100));
            SetupToolButton(_buttonCopy, "📋 复制", 66, Color.FromArgb(100, 180, 100));
            SetupToolButton(_buttonFavorite, "⭐ 收藏", 132, Color.FromArgb(255, 193, 7));
            SetupToolButton(_buttonExportHistory, "📤 导出", 198, Color.FromArgb(100, 150, 200));
            SetupToolButton(_buttonSend, "➤ 发送", 264, Color.DodgerBlue);
        }

        private void SetupToolButton(Button button, string text, int locationX, Color backColor)
        {
            button.BackColor = backColor;
            button.FlatAppearance.BorderSize = 0;
            button.FlatStyle = FlatStyle.Flat;
            button.Font = new Font("微软雅黑", 9F);
            button.ForeColor = Color.White;
            button.Location = new Point(locationX, 4);
            button.Name = button.Text;
            button.Size = new Size(61, 32);
            button.TabIndex = 0;
            button.Text = text;
            button.UseVisualStyleBackColor = false;
            button.Cursor = Cursors.Hand;
            _toolButtonOriginalColors[button] = backColor;
        }

        private void SetupSuggestionsListBox()
        {
            _listBoxSuggestions.BackColor = Color.FromArgb(250, 250, 250);
            _listBoxSuggestions.BorderStyle = BorderStyle.FixedSingle;
            _listBoxSuggestions.Font = new Font("微软雅黑", 9F);
            _listBoxSuggestions.FormattingEnabled = true;
            _listBoxSuggestions.Location = new Point(163, 63);
            _listBoxSuggestions.Name = "_listBoxSuggestions";
            _listBoxSuggestions.Size = new Size(683, 140);
            _listBoxSuggestions.TabIndex = 5;
            _listBoxSuggestions.Visible = false;
            _listBoxSuggestions.Cursor = Cursors.Hand;
            _listBoxSuggestions.ItemHeight = 28;
            _listBoxSuggestions.DrawMode = DrawMode.OwnerDrawFixed;
            _listBoxSuggestions.DrawItem += SuggestionsList_DrawItem;
            _listBoxSuggestions.MouseMove += SuggestionsList_MouseMove;
        }

        private void SetupQuickActionsPanel()
        {
            _panelQuickActions.BackColor = Color.FromArgb(250, 250, 250);
            _panelQuickActions.Controls.Add(_panelTools);
            _panelQuickActions.Controls.Add(_textBoxPrompt);
            _panelQuickActions.Controls.Add(_listBoxSuggestions);
            _panelQuickActions.Controls.Add(_comboBoxHistory);
            _panelQuickActions.Dock = DockStyle.Fill;
            _panelQuickActions.Location = new Point(0, 28);
            _panelQuickActions.Name = "_panelQuickActions";
            _panelQuickActions.Padding = new Padding(5, 3, 5, 3);
            _panelQuickActions.Size = new Size(1263, 106);
            _panelQuickActions.TabIndex = 3;
        }

        private void SetupRadioButtonsPanel()
        {
            _panelRadioButtons.BackColor = Color.FromArgb(245, 245, 245);
            _panelRadioButtons.Dock = DockStyle.Top;
            _panelRadioButtons.Location = new Point(0, 0);
            _panelRadioButtons.Name = "_panelRadioButtons";
            _panelRadioButtons.Padding = new Padding(5);
            _panelRadioButtons.Size = new Size(1263, 28);
            _panelRadioButtons.TabIndex = 2;
        }

        private void SetupBrowserPanel()
        {
            _panelBrowser.BackColor = Color.White;
            _panelBrowser.Dock = DockStyle.Fill;
            _panelBrowser.Location = new Point(0, 134);
            _panelBrowser.Name = "_panelBrowser";
            _panelBrowser.Size = new Size(1263, 512);
            _panelBrowser.TabIndex = 0;
        }

        private void SetupMainPanel()
        {
            Controls.Add(_panelBrowser);
            Controls.Add(_labelStatus);
            Controls.Add(_panelTop);
            Name = "AIAbilityPanel";
            Size = new Size(1263, 668);
        }

        private void CreateRadioButtons()
        {
            int xPos = 5;
            int spacing = 95;

            foreach (var provider in AiConfig.Providers)
            {
                var radio = new RadioButton();
                radio.Text = GetShortProviderName(provider.Value.Name);
                radio.Location = new Point(xPos, 2);
                radio.Size = new Size(Math.Min(spacing - 10, GetTextWidth(radio.Text) + 24), 24);
                radio.Font = new Font("微软雅黑", 9F);
                radio.FlatStyle = FlatStyle.Flat;
                radio.FlatAppearance.BorderSize = 0;
                radio.Cursor = Cursors.Hand;
                radio.Tag = provider.Key;
                radio.CheckedChanged += RadioButton_CheckedChanged;
                radio.MouseEnter += RadioButton_MouseEnter;
                radio.MouseLeave += RadioButton_MouseLeave;

                _radioButtons[provider.Key] = radio;
                _panelRadioButtons.Controls.Add(radio);
                xPos += spacing;

                if (_radioButtons.Count == 1)
                    radio.Checked = true;
            }
        }

        private void RadioButton_MouseEnter(object? sender, EventArgs e)
        {
            if (sender is RadioButton radio && !radio.Checked)
            {
                radio.ForeColor = _currentTheme == ThemeMode.Dark
                    ? Color.FromArgb(140, 180, 240)
                    : Color.FromArgb(40, 100, 200);
            }
        }

        private void RadioButton_MouseLeave(object? sender, EventArgs e)
        {
            if (sender is RadioButton radio && !radio.Checked)
            {
                radio.ForeColor = _currentTheme == ThemeMode.Dark
                    ? Color.FromArgb(250, 250, 250)
                    : Color.Black;
            }
        }

        private void CreateQuickActionButtons()
        {
            int xPos = 5;
            foreach (var config in QuickActionDefinitions.DefaultActions)
            {
                var button = new Button();
                button.BackColor = config.LightColor;
                button.FlatAppearance.BorderSize = 0;
                button.FlatStyle = FlatStyle.Flat;
                button.Font = new Font("微软雅黑", 9F);
                button.ForeColor = Color.White;
                button.Location = new Point(xPos, 3);
                button.Name = $"_button{config.Key}";
                button.Size = new Size(config.Width, 26);
                button.TabIndex = _quickActionButtons.Count;
                button.Text = config.DisplayText;
                button.UseVisualStyleBackColor = false;
                button.Tag = config.Key;
                button.Cursor = Cursors.Hand;
                button.Click += QuickActionButton_Click;
                button.MouseEnter += QuickActionButton_MouseEnter;
                button.MouseLeave += QuickActionButton_MouseLeave;

                _quickActionButtons[config.Key] = button;
                _quickActionOriginalColors[config.Key] = config.LightColor;
                _panelQuickActions.Controls.Add(button);
                xPos += config.Width + 3;
            }
        }

        private void SetupEventHandlers()
        {
            _textBoxPrompt.KeyDown += TextBoxPrompt_KeyDown;
            _textBoxPrompt.TextChanged += TextBoxPrompt_TextChanged;
            _textBoxPrompt.GotFocus += TextBoxPrompt_GotFocus;
            _textBoxPrompt.LostFocus += TextBoxPrompt_LostFocus;
            _comboBoxHistory.SelectedIndexChanged += ComboBoxHistory_SelectedIndexChanged;
            _listBoxSuggestions.SelectedIndexChanged += ListBoxSuggestions_SelectedIndexChanged;
            _buttonClear.Click += ButtonClear_Click;
            _buttonCopy.Click += ButtonCopy_Click;
            _buttonFavorite.Click += ButtonFavorite_Click;
            _buttonExportHistory.Click += ButtonExportHistory_Click;
            _buttonSend.Click += ButtonSend_Click;

            AddToolButtonHoverEffect(_buttonClear);
            AddToolButtonHoverEffect(_buttonCopy);
            AddToolButtonHoverEffect(_buttonFavorite);
            AddToolButtonHoverEffect(_buttonExportHistory);
            AddToolButtonHoverEffect(_buttonSend);
        }

        private void AddToolButtonHoverEffect(Button button)
        {
            button.MouseEnter += (s, e) =>
            {
                if (_toolButtonOriginalColors.TryGetValue(button, out var originalColor))
                {
                    button.BackColor = ThemeHelper.GetHoverColor(originalColor, -25);
                }
            };
            button.MouseLeave += (s, e) =>
            {
                if (_toolButtonOriginalColors.TryGetValue(button, out var originalColor))
                {
                    button.BackColor = originalColor;
                }
            };
        }
        #endregion

        #region 事件处理
        private void QuickActionButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button button && button.Tag is string key)
            {
                var config = QuickActionDefinitions.DefaultActions.FirstOrDefault(a => a.Key == key);
                if (config != null)
                {
                    var context = GetContextWithPrompt();
                    _textBoxPrompt.Text = config.PromptTemplate.Replace("{context}", context);
                }
            }
        }

        private void QuickActionButton_MouseEnter(object? sender, EventArgs e)
        {
            if (sender is Button button && button.Tag is string key && _quickActionOriginalColors.TryGetValue(key, out var originalColor))
            {
                button.BackColor = ThemeHelper.GetHoverColor(originalColor, -25);
            }
        }

        private void QuickActionButton_MouseLeave(object? sender, EventArgs e)
        {
            if (sender is Button button && button.Tag is string key && _quickActionOriginalColors.TryGetValue(key, out var originalColor))
            {
                button.BackColor = originalColor;
            }
        }

        private void TextBoxPrompt_GotFocus(object? sender, EventArgs e)
        {
            _textBoxPrompt.BackColor = _currentTheme == ThemeMode.Dark
                ? Color.FromArgb(60, 60, 60)
                : Color.White;
        }

        private void TextBoxPrompt_LostFocus(object? sender, EventArgs e)
        {
            _textBoxPrompt.BackColor = _currentTheme == ThemeMode.Dark
                ? Color.FromArgb(50, 50, 50)
                : Color.White;
        }

        private void SuggestionsList_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || _listBoxSuggestions.Items.Count == 0) return;

            e.DrawBackground();
            var item = _listBoxSuggestions.Items[e.Index]?.ToString();
            if (string.IsNullOrEmpty(item)) return;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool isHovered = e.Index == _hoveredSuggestionIndex;

            Color backColor;
            Color textColor;
            Color subTextColor;

            if (isSelected)
            {
                backColor = _currentTheme == ThemeMode.Dark
                    ? Color.FromArgb(60, 120, 200)
                    : Color.DodgerBlue;
                textColor = Color.White;
                subTextColor = Color.FromArgb(220, 220, 220);
            }
            else if (isHovered)
            {
                backColor = _currentTheme == ThemeMode.Dark
                    ? Color.FromArgb(55, 55, 55)
                    : Color.FromArgb(240, 240, 240);
                textColor = _currentTheme == ThemeMode.Dark ? Color.White : Color.Black;
                subTextColor = _currentTheme == ThemeMode.Dark ? Color.Gray : Color.Gray;
            }
            else
            {
                backColor = _currentTheme == ThemeMode.Dark
                    ? Color.FromArgb(50, 50, 50)
                    : Color.FromArgb(250, 250, 250);
                textColor = _currentTheme == ThemeMode.Dark ? Color.White : Color.Black;
                subTextColor = _currentTheme == ThemeMode.Dark ? Color.Gray : Color.Gray;
            }

            using (var brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            var parts = item.Split(':', 2);
            string keyText = parts.Length > 0 ? parts[0].Trim() : item;
            string descText = parts.Length > 1 ? parts[1].Trim() : "";

            using var keyBrush = new SolidBrush(textColor);
            using var descBrush = new SolidBrush(subTextColor);
            using var font = new Font("微软雅黑", 9F);
            using var smallFont = new Font("微软雅黑", 8F);

            e.Graphics.DrawString(keyText, font, keyBrush, e.Bounds.X + 10, e.Bounds.Y + 5);
            if (!string.IsNullOrEmpty(descText))
            {
                string truncatedDesc = descText.Length > 30 ? descText.Substring(0, 30) + "..." : descText;
                e.Graphics.DrawString(truncatedDesc, smallFont, descBrush, e.Bounds.X + 10, e.Bounds.Y + 16);
            }
        }

        private void SuggestionsList_MouseMove(object? sender, MouseEventArgs e)
        {
            int hoverIndex = _listBoxSuggestions.IndexFromPoint(e.Location);
            if (hoverIndex != _hoveredSuggestionIndex)
            {
                _hoveredSuggestionIndex = hoverIndex;
                _listBoxSuggestions.Invalidate();
            }
        }

        private void RadioButton_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is RadioButton radio && radio.Checked)
            {
                if (_isWebViewInitialized && _webView != null)
                {
                    string url = GetProviderWebViewUrl(radio.Tag?.ToString() ?? "doubao");
                    _webView.Source = new Uri(url);
                    SetStatus($"已切换到: {GetProviderName(radio.Tag?.ToString() ?? "doubao")}");
                }
            }
        }

        private async void ButtonSend_Click(object? sender, EventArgs e)
        {
            var prompt = _textBoxPrompt?.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                MessageBox.Show("请输入提示词", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 将「目录内容」等上下文一并拼入要发送给 AI 网页的文本，保证指令与内容一起注入输入框
            var payload = BuildSendPayload(prompt);

            _historyService.AddToHistory(prompt);
            _historyService.SaveConversationRecord(prompt, CurrentAIName, _currentContext);
            UpdateHistoryComboBox();

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

                await OpenWebViewAsync(CurrentAIUrl, payload);
            }
            catch (Exception ex)
            {
                ShowError($"发送失败: {ex.Message}");
                _logger?.LogError(ex, "发送按钮异常");
            }
        }

        /// <summary>
        /// 组装最终发送给 AI 网页的文本：指令 + 上下文（如目录内容）。
        /// 若上下文已包含在指令文本中，则不再重复拼接，避免重复发送。
        /// </summary>
        private string BuildSendPayload(string prompt)
        {
            if (string.IsNullOrEmpty(_currentContext))
                return prompt;
            if (prompt.Contains(_currentContext, StringComparison.Ordinal))
                return prompt;
            return prompt + "\r\n\r\n【目录内容】\r\n" + _currentContext;
        }

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
                _historyService.AddToHistory(prompt, true);
                UpdateHistoryComboBox();
                SetStatus("已收藏");
            }
            else
            {
                SetStatus("没有可收藏的内容");
            }
        }

        private void ButtonExportHistory_Click(object? sender, EventArgs e)
        {
            if (_historyService.ConversationHistory.Count == 0)
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
                MessageBox.Show($"已导出 {_historyService.ConversationHistory.Count} 条记录到:\n{saveDialog.FileName}", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ComboBoxHistory_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_comboBoxHistory == null || _comboBoxHistory.SelectedIndex <= 0) return;

            var selected = _comboBoxHistory.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selected)) return;

            _textBoxPrompt.Text = PromptHistoryService.ExtractPromptFromHistoryItem(selected);
        }

        private void TextBoxPrompt_TextChanged(object? sender, EventArgs e)
        {
            var input = _textBoxPrompt.Text.Trim();

            if (string.IsNullOrEmpty(input) || input.Length < 1)
            {
                HideSuggestions();
                return;
            }

            var suggestions = QuickActionDefinitions.PromptTemplates
                .Where(kv => kv.Key.Contains(input) || input.Contains(kv.Key))
                .Select(kv => $"{kv.Key}: {kv.Value}")
                .Take(5)
                .ToList();

            if (suggestions.Any())
                ShowSuggestions(suggestions);
            else
                HideSuggestions();
        }

        private void TextBoxPrompt_KeyDown(object? sender, KeyEventArgs e)
        {
            if (_listBoxSuggestions.Visible)
            {
                HandleSuggestionsKeyDown(e);
                if (e.SuppressKeyPress) return;
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

        private void HandleSuggestionsKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                    e.SuppressKeyPress = true;
                    if (_listBoxSuggestions.SelectedIndex < _listBoxSuggestions.Items.Count - 1)
                        _listBoxSuggestions.SelectedIndex++;
                    break;
                case Keys.Up:
                    e.SuppressKeyPress = true;
                    if (_listBoxSuggestions.SelectedIndex > 0)
                        _listBoxSuggestions.SelectedIndex--;
                    break;
                case Keys.Enter:
                case Keys.Tab:
                    e.SuppressKeyPress = true;
                    ListBoxSuggestions_SelectedIndexChanged(null, e);
                    break;
                case Keys.Escape:
                    e.SuppressKeyPress = true;
                    HideSuggestions();
                    break;
            }
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
        #endregion

        #region WebView相关
        private async Task InitializeWebViewAsync()
        {
            try
            {
                var cacheDir = Path.Combine(CachePaths.WebView2, "ai_panel");
                if (!Directory.Exists(cacheDir))
                    Directory.CreateDirectory(cacheDir);

                var environment = await CoreWebView2Environment.CreateAsync(null, cacheDir);

                _webView = new WebView2 { Dock = DockStyle.Fill };
                await _webView.EnsureCoreWebView2Async(environment);

                if (_webView.CoreWebView2 != null)
                {
                    ConfigureWebViewSettings(_webView.CoreWebView2.Settings);
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

        private void ConfigureWebViewSettings(CoreWebView2Settings settings)
        {
            settings.AreDefaultContextMenusEnabled = true;
            settings.IsWebMessageEnabled = true;
            settings.IsScriptEnabled = true;
            settings.AreDevToolsEnabled = true;
            settings.IsZoomControlEnabled = true;
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

            if (string.IsNullOrEmpty(_pendingPrompt)) return;

            try
            {
                await FillPromptWithRetry();
                _pendingPrompt = null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "填充Prompt JS执行异常");
                ShowError($"填充提示词失败: {ex.Message}");
                SetStatus("就绪");
            }
        }

        private async Task FillPromptWithRetry()
        {
            await Task.Delay(1000);
            int retryCount = 0;
            const int maxRetry = 3;
            bool filled = false;

            while (!filled && retryCount < maxRetry)
            {
                SetStatus($"正在填充提示词... (尝试 {retryCount + 1}/{maxRetry})");
                string script = GetFillPromptScript(_webView?.Source?.Host, _pendingPrompt!);
                var result = await _webView!.CoreWebView2.ExecuteScriptAsync(script);

                if (result.Contains("\"filled"))
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
        }

        private async Task TryBackupFillMethod()
        {
            if (_webView?.CoreWebView2 == null || string.IsNullOrEmpty(_pendingPrompt)) return;

            try
            {
                var base64Prompt = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(_pendingPrompt));
                string backupScript = string.Format(
                    @"
(function(){{
    var promptText = '';
    try {{
        var bytes = Uint8Array.from(atob('{0}'), function(c){{ return c.charCodeAt(0); }});
        promptText = new TextDecoder('utf-8').decode(bytes);
    }} catch(e) {{ return 'decode error'; }}

    var textareas = document.getElementsByTagName('textarea');
    for(var i=0;i<textareas.length;i++){{
        if(textareas[i].offsetParent !== null){{
            textareas[i].value = promptText;
            textareas[i].focus();
            textareas[i].dispatchEvent(new Event('input',{{bubbles:true,cancelable:true}}));
            return 'backup success';
        }}
    }}
    return 'backup failed';
}})();
", base64Prompt);
                await _webView.CoreWebView2.ExecuteScriptAsync(backupScript);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "备用填充方法失败");
            }
        }

        private string GetFillPromptScript(string? host, string prompt)
        {
            if (string.IsNullOrEmpty(host))
                return "/* unknown host */";

            var base64Prompt = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(prompt ?? string.Empty));

            return string.Format(
                @"
(function(){{
    var promptText = '';
    try {{
        var bytes = Uint8Array.from(atob('{0}'), function(c){{ return c.charCodeAt(0); }});
        promptText = new TextDecoder('utf-8').decode(bytes);
    }} catch(e) {{
        return 'decode error';
    }}

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
        try {{
            targetEl = document.querySelector(selectors[i]);
        }} catch(e) {{ targetEl = null; }}
        if(targetEl) break;
    }}

    if(!targetEl){{
        var inputContainers = document.querySelectorAll('[class*=""input""],[class*=""chat""]');
        if(inputContainers.length>0) inputContainers[0].click();
        return 'not found';
    }}

    var tag = targetEl.tagName.toLowerCase();
    if(tag === 'textarea' || tag === 'input'){{
        targetEl.value = promptText;
    }}else if(targetEl.isContentEditable){{
        targetEl.textContent = promptText;
    }}

    targetEl.dispatchEvent(new Event('input',{{bubbles:true,cancelable:true}}));
    targetEl.dispatchEvent(new Event('change',{{bubbles:true,cancelable:true}}));
    targetEl.dispatchEvent(new KeyboardEvent('keydown',{{bubbles:true,cancelable:true,key:''}}));
    targetEl.focus();
    return 'filled: ' + targetEl.tagName;
}})();
", base64Prompt);
        }
        #endregion

        #region 辅助方法
        private string GetContextWithPrompt()
        {
            if (!string.IsNullOrEmpty(_currentContext))
                return _currentContext;
            return string.IsNullOrWhiteSpace(_textBoxPrompt?.Text) ? "请输入需要处理的内容" : _textBoxPrompt.Text;
        }

        private void ShowSuggestions(List<string> suggestions)
        {
            _listBoxSuggestions.Items.Clear();
            foreach (var suggestion in suggestions)
                _listBoxSuggestions.Items.Add(suggestion);
            _listBoxSuggestions.Visible = true;
            _listBoxSuggestions.BringToFront();
        }

        private void HideSuggestions()
        {
            _listBoxSuggestions.Visible = false;
        }

        private void UpdateHistoryComboBox()
        {
            if (_comboBoxHistory == null || _comboBoxHistory.IsDisposed) return;
            _comboBoxHistory.Items.Clear();
            foreach (var item in _historyService.GetHistoryComboBoxItems())
                _comboBoxHistory.Items.Add(item);
            _comboBoxHistory.SelectedIndex = 0;
        }

        private void SetStatus(string message)
        {
            _statusMessage = message;
            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            if (_labelStatus == null || _labelStatus.IsDisposed) return;
            string providerName = GetSelectedProviderName();
            _labelStatus.Text = $"🤖 {_statusMessage} | 当前: {providerName}";
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static string GetShortProviderName(string name)
        {
            return name.Contains('(')
                ? name.Substring(0, name.IndexOf('('))
                : name;
        }

        private static int GetTextWidth(string text)
        {
            return TextRenderer.MeasureText(text, new Font("微软雅黑", 9F)).Width;
        }

        private string GetSelectedProviderUrl()
        {
            foreach (var kvp in _radioButtons)
            {
                if (kvp.Value.Checked)
                    return GetProviderWebViewUrl(kvp.Key);
            }
            return GetDefaultWebViewUrl();
        }

        private void SetSelectedProviderByUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            foreach (var kvp in _radioButtons)
            {
                string providerUrl = GetProviderWebViewUrl(kvp.Key);
                if (url.Equals(providerUrl, StringComparison.OrdinalIgnoreCase))
                {
                    kvp.Value.Checked = true;
                    return;
                }
            }
            _radioButtons.Values.FirstOrDefault()?.Checked = true;
        }

        private string GetSelectedProviderName()
        {
            foreach (var kvp in _radioButtons)
            {
                if (kvp.Value.Checked)
                    return GetProviderName(kvp.Key);
            }
            return "豆包";
        }

        private void SetSelectedProviderByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            foreach (var kvp in _radioButtons)
            {
                string providerName = GetProviderName(kvp.Key);
                if (name.Equals(providerName, StringComparison.OrdinalIgnoreCase) ||
                    name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    kvp.Value.Checked = true;
                    return;
                }
            }
            _radioButtons.Values.FirstOrDefault()?.Checked = true;
        }

        private string GetProviderWebViewUrl(string providerKey)
        {
            if (AiConfig.Providers.TryGetValue(providerKey, out var info))
                return info.WebViewUrl;
            return "https://www.doubao.com/chat";
        }

        private string GetProviderName(string providerKey)
        {
            if (AiConfig.Providers.TryGetValue(providerKey, out var info))
                return info.Name;
            return providerKey;
        }

        private string GetDefaultWebViewUrl()
        {
            if (AiConfig.Providers.TryGetValue("doubao", out var info))
                return info.WebViewUrl;
            return "https://www.doubao.com/chat";
        }
        #endregion

        #region 资源释放
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
        #endregion
    }
}