using LearningAssistant.Models.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.ComponentModel;

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
        private Panel promptPanel;
        private readonly Dictionary<string, RadioButton> _radioButtons = new();

        public bool IsWebViewInitialized => _isWebViewInitialized;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string PromptText
        {
            get => _textBoxPrompt?.Text ?? string.Empty;
            set
            {
                if (_textBoxPrompt != null)
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
                foreach (var kvp in _radioButtons)
                {
                    string providerUrl = GetProviderWebViewUrl(kvp.Key);
                    if (value.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                        value.Equals(providerUrl, StringComparison.OrdinalIgnoreCase))
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

        public AIAbilityPanel(ILogger<AIAbilityPanel>? logger = null)
        {
            _logger = logger;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _panelTop = new Panel();
            _labelStatus = new Label();
            promptPanel = new Panel();
            _textBoxPrompt = new TextBox();
            _buttonSend = new Button();
            _panelRadioButtons = new Panel();
            _panelBrowser = new Panel();
            _panelTop.SuspendLayout();
            promptPanel.SuspendLayout();
            SuspendLayout();
            // 
            // _panelTop
            // 
            _panelTop.BackColor = Color.FromArgb(245, 245, 245);
            _panelTop.Controls.Add(_labelStatus);
            _panelTop.Controls.Add(promptPanel);
            _panelTop.Controls.Add(_panelRadioButtons);
            _panelTop.Dock = DockStyle.Top;
            _panelTop.Location = new Point(0, 0);
            _panelTop.Name = "_panelTop";
            _panelTop.Size = new Size(800, 75);
            _panelTop.TabIndex = 1;
            // 
            // _labelStatus
            // 
            _labelStatus.Dock = DockStyle.Top;
            _labelStatus.Font = new Font("微软雅黑", 8F);
            _labelStatus.ForeColor = Color.Gray;
            _labelStatus.Location = new Point(0, 28);
            _labelStatus.Name = "_labelStatus";
            _labelStatus.Size = new Size(800, 20);
            _labelStatus.TabIndex = 0;
            _labelStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // promptPanel
            // 
            promptPanel.Controls.Add(_textBoxPrompt);
            promptPanel.Controls.Add(_buttonSend);
            promptPanel.Dock = DockStyle.Fill;
            promptPanel.Location = new Point(0, 28);
            promptPanel.Name = "promptPanel";
            promptPanel.Padding = new Padding(5);
            promptPanel.Size = new Size(800, 47);
            promptPanel.TabIndex = 1;
            // 
            // _textBoxPrompt
            // 
            _textBoxPrompt.Dock = DockStyle.Fill;
            _textBoxPrompt.Font = new Font("微软雅黑", 10F);
            _textBoxPrompt.Location = new Point(5, 5);
            _textBoxPrompt.Margin = new Padding(5);
            _textBoxPrompt.Name = "_textBoxPrompt";
            _textBoxPrompt.PlaceholderText = "输入提示词或问题...";
            _textBoxPrompt.Size = new Size(710, 25);
            _textBoxPrompt.TabIndex = 0;
            // 
            // _buttonSend
            // 
            _buttonSend.BackColor = Color.FromArgb(33, 150, 243);
            _buttonSend.Dock = DockStyle.Right;
            _buttonSend.FlatAppearance.BorderSize = 0;
            _buttonSend.FlatStyle = FlatStyle.Flat;
            _buttonSend.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _buttonSend.ForeColor = Color.White;
            _buttonSend.Location = new Point(715, 5);
            _buttonSend.Name = "_buttonSend";
            _buttonSend.Size = new Size(80, 37);
            _buttonSend.TabIndex = 1;
            _buttonSend.Text = "发送";
            _buttonSend.UseVisualStyleBackColor = false;
            _buttonSend.Click += ButtonSend_Click;
            // 
            // _panelRadioButtons
            // 
            _panelRadioButtons.BackColor = Color.FromArgb(245, 245, 245);
            _panelRadioButtons.Dock = DockStyle.Top;
            _panelRadioButtons.Location = new Point(0, 0);
            _panelRadioButtons.Name = "_panelRadioButtons";
            _panelRadioButtons.Padding = new Padding(5);
            _panelRadioButtons.Size = new Size(800, 28);
            _panelRadioButtons.TabIndex = 2;
            // 
            // _panelBrowser
            // 
            _panelBrowser.BackColor = Color.White;
            _panelBrowser.Dock = DockStyle.Fill;
            _panelBrowser.Location = new Point(0, 75);
            _panelBrowser.Name = "_panelBrowser";
            _panelBrowser.Size = new Size(800, 425);
            _panelBrowser.TabIndex = 0;
            // 
            // AIAbilityPanel
            // 
            Controls.Add(_panelBrowser);
            Controls.Add(_panelTop);
            Name = "AIAbilityPanel";
            Size = new Size(800, 500);
            _panelTop.ResumeLayout(false);
            promptPanel.ResumeLayout(false);
            promptPanel.PerformLayout();
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
                radio.Text = providerName.Contains('(') ?
                    providerName.Substring(0, providerName.IndexOf('(')) : providerName;
                radio.Location = new Point(xPos, 3);
                radio.Size = new Size(Math.Min(spacing - 10, TextRenderer.MeasureText(providerName, new Font("微软雅黑", 9F)).Width + 20), 22);
                radio.Font = new Font("微软雅黑", 9F);
                radio.FlatStyle = FlatStyle.Flat;
                radio.Tag = providerKey;

                _radioButtons[providerKey] = radio;
                _panelRadioButtons.Controls.Add(radio);
                xPos += spacing;

                if (_radioButtons.Count == 1)
                    radio.Checked = true;
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

            try
            {
                SetStatus("正在打开AI网页...");
                OpenWebView(CurrentAIUrl, prompt);
            }
            catch (Exception ex)
            {
                ShowError($"发送失败: {ex.Message}");
            }
        }

        public async void OpenWebView(string url, string? prompt = null)
        {
            _pendingUrl = url;
            _pendingPrompt = prompt;

            if (!string.IsNullOrEmpty(url))
            {
                CurrentAIUrl = url;
            }
            if (!string.IsNullOrEmpty(prompt))
            {
                _textBoxPrompt.Text = prompt;
            }

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
                {
                    Directory.CreateDirectory(cacheDir);
                }

                var environment = await CoreWebView2Environment.CreateAsync(null, cacheDir);

                _webView = new WebView2
                {
                    Dock = DockStyle.Fill
                };

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
            }
        }

        private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (_webView == null || !e.IsSuccess)
            {
                if (!e.IsSuccess)
                {
                    ShowError($"页面加载失败 (错误码: {e.HttpStatusCode})");
                }
                return;
            }

            SetStatus("页面加载完成");

            var host = _webView.Source?.Host;

            if (!string.IsNullOrEmpty(_pendingPrompt))
            {
                try
                {
                    SetStatus("正在填充提示词...");
                    await Task.Delay(500);
                    await FillPromptAsync(_pendingPrompt, host);
                    SetStatus("就绪");
                    _pendingPrompt = null;
                }
                catch (Exception ex)
                {
                    ShowError($"填充提示词失败: {ex.Message}");
                    SetStatus("就绪");
                }
            }
        }

        private async Task FillPromptAsync(string prompt, string? host)
        {
            if (_webView?.CoreWebView2 == null) return;

            try
            {
                string script = GetFillPromptScript(host, prompt);

                if (!script.StartsWith("/*"))
                {
                    await _webView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch { }
        }

        private string GetFillPromptScript(string? host, string prompt)
        {
            if (string.IsNullOrEmpty(host))
                return "/* unknown host */";

            var escapedPrompt = EscapeForJavaScript(prompt);

            // 通用脚本：查找textarea或input[type="text"]或contenteditable元素
            return $@"(function(){{
                var t = document.querySelector('textarea');
                if(!t) t = document.querySelector('input[type=""text""]');
                if(!t) t = document.querySelector('[contenteditable=""true""]');
                if(!t) t = document.querySelector('[contenteditable]');
                if(t){{
                    t.value='{escapedPrompt}';
                    t.innerText='{escapedPrompt}';
                    t.textContent='{escapedPrompt}';
                    t.dispatchEvent(new Event('input',{{bubbles:true}}));
                    t.dispatchEvent(new Event('change',{{bubbles:true}}));
                    return 'ok';
                }}
                return 'not found';
            }})();";
        }

        private static string EscapeForJavaScript(string text)
        {
            return text.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private void SetStatus(string message)
        {
            if (_labelStatus != null)
            {
                _labelStatus.Text = message;
            }
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