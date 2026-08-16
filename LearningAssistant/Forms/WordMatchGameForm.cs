using LearningAssistant.Common.Themes;
using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Text.Json;

namespace LearningAssistant.Forms
{
    /// <summary>
    /// 单词消消乐游戏窗体：通过 WebView2 展示本地静态网页游戏，数据来自内容编辑器。
    /// </summary>
    public partial class WordMatchGameForm : Form, IThemeable
    {
        private readonly WordMatchGameService _gameService;
        private readonly IContentLoaderService _contentLoaderService;
        private readonly IWrongAnswerService _wrongAnswerService;
        private readonly IUserSessionService _userSessionService;
        private readonly IThemeService _themeService;
        private readonly ILogger<WordMatchGameForm> _logger;

        private WebView2? _webView;
        private CoreWebView2Environment? _webViewEnvironment;

        private ComboBox _comboSubject;
        private ComboBox _comboSubCategory;
        private Button _btnStart;
        private Panel _panelWeb;

        private SubjectType _subject = SubjectType.English;
        private SubCategoryType _subCategory = SubCategoryType.EnglishWord;
        private bool _webViewReady;
        private string? _pendingPayload;

        public WordMatchGameForm(
            WordMatchGameService gameService,
            IContentLoaderService contentLoaderService,
            IWrongAnswerService wrongAnswerService,
            IUserSessionService userSessionService,
            IThemeService themeService,
            ILogger<WordMatchGameForm> logger)
        {
            _gameService = gameService;
            _contentLoaderService = contentLoaderService;
            _wrongAnswerService = wrongAnswerService;
            _userSessionService = userSessionService;
            _themeService = themeService;
            _logger = logger;

            BuildUi();
            WindowState = FormWindowState.Maximized;
            Load += WordMatchGameForm_Load;
            FormClosing += WordMatchGameForm_FormClosing;

            _themeService.RegisterThemeable(this);
        }

        private void BuildUi()
        {
            Text = "🧩 单词消消乐";
            BackColor = Color.FromArgb(255, 244, 230);

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(8, 8, 8, 8) };

            _comboSubject = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("微软雅黑", 10F),
                Width = 140
            };
            _comboSubject.Items.AddRange(new object[]
            {
                Constants.Subject.English,
                Constants.Subject.Chinese,
                Constants.Subject.Math
            });
            _comboSubject.SelectedIndex = 0;
            _comboSubject.SelectedIndexChanged += (s, e) => RefreshSubCategories();

            _comboSubCategory = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("微软雅黑", 10F),
                Width = 160
            };

            _btnStart = new Button
            {
                Text = "🎮 开始游戏",
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(99, 102, 241),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width = 120,
                Height = 36
            };
            _btnStart.FlatAppearance.BorderSize = 0;
            _btnStart.Click += (s, e) => StartGame();

            topPanel.Controls.Add(_comboSubject);
            topPanel.Controls.Add(_comboSubCategory);
            topPanel.Controls.Add(_btnStart);

            _comboSubject.Location = new Point(8, 8);
            _comboSubCategory.Location = new Point(158, 8);
            _btnStart.Location = new Point(330, 8);

            _panelWeb = new Panel { Dock = DockStyle.Fill };

            Controls.Add(_panelWeb);
            Controls.Add(topPanel);

            RefreshSubCategories();
        }

        private void RefreshSubCategories()
        {
            var subject = SubjectSubCategoryMapping.TryParseSubject(_comboSubject.SelectedItem?.ToString() ?? "", out var parsed)
                ? parsed
                : SubjectType.English;
            _subject = subject;

            _comboSubCategory.Items.Clear();
            var subs = _contentLoaderService.GetSubCategories(subject);
            foreach (var s in subs)
            {
                _comboSubCategory.Items.Add(SubjectSubCategoryMapping.GetSubCategoryDisplayName(s));
            }
            if (_comboSubCategory.Items.Count > 0)
            {
                _comboSubCategory.SelectedIndex = 0;
            }
            _subCategory = _comboSubCategory.SelectedIndex >= 0 && _comboSubCategory.SelectedIndex < subs.Count
                ? subs[_comboSubCategory.SelectedIndex]
                : SubCategoryType.EnglishWord;
        }

        private async void WordMatchGameForm_Load(object? sender, EventArgs e)
        {
            try
            {
                await InitializeWebViewAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化 WebView2 失败");
                MessageBox.Show($"初始化 WebView2 失败：{ex.Message}\n\n可能需要安装 WebView2 Runtime。",
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task InitializeWebViewAsync()
        {
            var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LearningAssistant", "WebView2Cache");
            Directory.CreateDirectory(cacheDir);

            _webViewEnvironment = await CoreWebView2Environment.CreateAsync(null, cacheDir);

            _webView = new WebView2 { Dock = DockStyle.Fill };
            _panelWeb.Controls.Add(_webView);

            await _webView.EnsureCoreWebView2Async(_webViewEnvironment);

            if (_webView.CoreWebView2 != null)
            {
                _webView.CoreWebView2.Settings.IsScriptEnabled = true;
                _webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
#if DEBUG
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
#endif
                _webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                _webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

                var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "Resources", "WordMatchGame", "index.html");
                if (File.Exists(htmlPath))
                {
                    _webView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
                }
                else
                {
                    MessageBox.Show("无法找到游戏页面文件：" + htmlPath, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void StartGame()
        {
            if (_webView?.CoreWebView2 == null)
            {
                MessageBox.Show("游戏页面尚未初始化，请等待页面加载完成后再试。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var userId = _userSessionService.CurrentUserId;
            var context = new LearningContext(userId, _subject, _subCategory);
            var items = _gameService.BuildItems(context, maxCount: 10);

            if (items.Count == 0)
            {
                MessageBox.Show("当前词库没有可用的单词（需要有单词和释义），请先在「内容编辑」中添加内容。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var themeName = _themeService.CurrentTheme == ThemeMode.Dark ? "dark" : "light";
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var payload = JsonSerializer.Serialize(new { type = "init", data = items, theme = themeName }, options);
            _pendingPayload = payload;
            if (_webViewReady)
            {
                _pendingPayload = null;
                _webView.CoreWebView2.PostWebMessageAsJson(payload);
            }
            // 页面尚未加载完成时，等待 NavigationCompleted 补发 _pendingPayload。
        }

        private void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess && _webView?.CoreWebView2 != null)
            {
                _webViewReady = true;
                // 若「开始游戏」在页面加载完成前被点击，此时补发待发送的数据，避免消息丢失。
                if (_pendingPayload != null)
                {
                    var payload = _pendingPayload;
                    _pendingPayload = null;
                    _webView.CoreWebView2.PostWebMessageAsJson(payload);
                }
            }
        }

        private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                using var doc = JsonDocument.Parse(e.WebMessageAsJson);
                var root = doc.RootElement;
                if (!root.TryGetProperty("type", out var typeProp)) return;
                var type = typeProp.GetString();

                if (type == "gameEnd" && root.TryGetProperty("results", out var resultsProp))
                {
                    var results = resultsProp.EnumerateArray()
                        .Select(r => new WordMatchResult
                        {
                            Id = r.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                            Correct = r.TryGetProperty("correct", out var c) && c.GetBoolean()
                        })
                        .ToList();

                    var userId = _userSessionService.CurrentUserId;
                    var context = new LearningContext(userId, _subject, _subCategory);
                    _gameService.ApplyResults(userId, context, results);
                    _logger.LogInformation("单词消消乐收到游戏结束消息，共 {Count} 个结果", results.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理游戏消息失败");
            }
        }

        private void WordMatchGameForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _webView?.Dispose();
        }

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;
            _panelWeb.BackColor = colors.Background;
            _comboSubject.BackColor = colors.Surface;
            _comboSubject.ForeColor = colors.TextPrimary;
            _comboSubCategory.BackColor = colors.Surface;
            _comboSubCategory.ForeColor = colors.TextPrimary;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _themeService.UnregisterThemeable(this);
            }
            base.Dispose(disposing);
        }
    }
}