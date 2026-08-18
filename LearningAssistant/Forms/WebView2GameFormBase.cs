using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Text.Json;

namespace LearningAssistant.Forms
{
    /// <summary>
    /// WebView2 网页游戏宿主窗体基类。
    /// 封装 WebView2 初始化、科目/子类别选择栏、"开始游戏"数据注入（含页面加载完成后的补发）、
    /// 前端 gameEnd 消息上报分发、明暗主题联动。具体游戏只需实现取词(BuildData)与成绩回写(OnGameEnd)。
    /// </summary>
    public abstract class WebView2GameFormBase : Form, IThemeable
    {
        private readonly IContentLoaderService _contentLoaderService;
        private readonly IUserSessionService _userSessionService;
        private readonly IThemeService _themeService;
        protected readonly ILogger _logger;

        /// <summary>当前登录用户 Id。</summary>
        protected string CurrentUserId => _userSessionService.CurrentUserId;

        /// <summary>当前 WebView2 控件。</summary>
        protected WebView2? _webView;
        private CoreWebView2Environment? _webViewEnvironment;

        private readonly ComboBox _comboSubject = new();
        private readonly ComboBox _comboSubCategory = new();
        private readonly Panel _panelWeb = new();
        private readonly NumericUpDown _numRows = new();
        private readonly NumericUpDown _numCols = new();
        private readonly IUserSettingsService _settingsService;

        private SubjectType _subject = SubjectType.English;
        private SubCategoryType _subCategory = SubCategoryType.EnglishWord;
        private bool _webViewReady;
        private string? _pendingPayload;

        /// <summary>每组展示行数（本地持久化，由顶部行数控件设置）。</summary>
        protected int GameRows => _gameRows;

        /// <summary>每组展示列数（本地持久化，由顶部列数控件设置）。</summary>
        protected int GameColumns => _gameCols;

        /// <summary>是否跳过已知项（false=加载所有）；由前端"换一组"旁的单选按钮控制。</summary>
        protected bool SkipKnown => _skipKnown;

        private int _gameRows = 5;
        private int _gameCols = 8;
        private bool _skipKnown = true;

        /// <summary>当前游戏的科目上下文（开始游戏时构建，供 OnGameEnd 使用）。</summary>
        private LearningContext? _currentContext;

        /// <summary>按用户记录"本窗口已答对"的词条 Id（区分用户，换一组时不再出现）。</summary>
        private readonly Dictionary<string, HashSet<string>> _correctIdsByUser = new();

        /// <summary>JSON 序列化参数：统一 camelCase，避免前后端字段大小写不一致。</summary>
        protected static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>游戏标题（窗体标题栏）。</summary>
        protected virtual string FormTitle => "WebView2 游戏";

        /// <summary>游戏 HTML 资源相对基目录的路径（如 "Resources\\WordMatchGame\\index.html"）。</summary>
        protected abstract string HtmlFileRelativePath { get; }

        protected WebView2GameFormBase(
            IContentLoaderService contentLoaderService,
            IUserSessionService userSessionService,
            IThemeService themeService,
            IUserSettingsService settingsService,
            ILogger logger)
        {
            _contentLoaderService = contentLoaderService;
            _userSessionService = userSessionService;
            _themeService = themeService;
            _settingsService = settingsService;
            _logger = logger;

            BuildUi();
            WindowState = FormWindowState.Maximized;
            Load += FormBase_Load;
            FormClosing += FormBase_FormClosing;

            _themeService.RegisterThemeable(this);
        }

        /// <summary>
        /// 构建游戏数据。返回需要注入前端 init.data 的对象；返回 null 表示无可玩数据（子类可自行提示用户），此时不启动本局。
        /// </summary>
        protected abstract object? BuildData(LearningContext context, string themeName);

        /// <summary>
        /// 处理前端上报的 gameEnd 消息。子类解析结果并回写成绩/错题本。
        /// </summary>
        protected abstract void OnGameEnd(JsonElement gameRoot, LearningContext context);

        // ---------- UI ----------

        private void BuildUi()
        {
            Text = FormTitle;
            BackColor = Color.FromArgb(255, 244, 230);

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(8, 8, 8, 8) };

            // 每组行/列配置（放在科目选择前面，用户可自由调整并保存到本地）
            var labelRows = new Label
            {
                Text = "行",
                Font = new Font("微软雅黑", 9F),
                AutoSize = false,
                Size = new Size(20, 34),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _numRows.Minimum = 2;
            _numRows.Maximum = 8;
            _numRows.Value = 5;
            _numRows.Width = 50;
            _numRows.Font = new Font("微软雅黑", 10F);
            _numRows.TextAlign = HorizontalAlignment.Center;
            _numRows.ValueChanged += (s, e) => OnLayoutConfigChanged();

            var labelCols = new Label
            {
                Text = "列",
                Font = new Font("微软雅黑", 9F),
                AutoSize = false,
                Size = new Size(20, 34),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _numCols.Minimum = 2;
            _numCols.Maximum = 12;
            _numCols.Value = 8;
            _numCols.Width = 50;
            _numCols.Font = new Font("微软雅黑", 10F);
            _numCols.TextAlign = HorizontalAlignment.Center;
            _numCols.ValueChanged += (s, e) => OnLayoutConfigChanged();

            _comboSubject.DropDownStyle = ComboBoxStyle.DropDownList;
            _comboSubject.Font = new Font("微软雅黑", 10F);
            _comboSubject.Width = 140;
            _comboSubject.Items.AddRange(new object[]
            {
                Constants.Subject.English,
                Constants.Subject.Chinese,
                Constants.Subject.Math
            });
            _comboSubject.SelectedIndex = 0;
            _comboSubject.SelectedIndexChanged += (s, e) => RefreshSubCategories();

            _comboSubCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            _comboSubCategory.Font = new Font("微软雅黑", 10F);
            _comboSubCategory.Width = 160;

            var btnStart = new Button
            {
                Text = "🎮 开始游戏",
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(99, 102, 241),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width = 120,
                Height = 36
            };
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Click += (s, e) => StartGame();

            topPanel.Controls.Add(labelRows);
            topPanel.Controls.Add(_numRows);
            topPanel.Controls.Add(labelCols);
            topPanel.Controls.Add(_numCols);
            topPanel.Controls.Add(_comboSubject);
            topPanel.Controls.Add(_comboSubCategory);
            topPanel.Controls.Add(btnStart);

            labelRows.Location = new Point(8, 8);
            _numRows.Location = new Point(28, 8);
            labelCols.Location = new Point(84, 8);
            _numCols.Location = new Point(104, 8);
            _comboSubject.Location = new Point(160, 8);
            _comboSubCategory.Location = new Point(310, 8);
            btnStart.Location = new Point(482, 8);

            _panelWeb.Dock = DockStyle.Fill;

            Controls.Add(_panelWeb);
            Controls.Add(topPanel);

            RefreshSubCategories();
        }

        /// <summary>
        /// 行/列配置变更：更新内存值并保存到本地设置，下次打开自动加载。
        /// </summary>
        private void OnLayoutConfigChanged()
        {
            _gameRows = (int)_numRows.Value;
            _gameCols = (int)_numCols.Value;
            PersistLayoutConfig();
        }

        /// <summary>保存行/列配置到用户设置。</summary>
        private void PersistLayoutConfig()
        {
            try
            {
                var userId = _userSessionService.CurrentUserId;
                if (string.IsNullOrEmpty(userId)) return;
                var settings = _settingsService.LoadSettingsAsync(userId).GetAwaiter().GetResult();
                settings.GameRows = _gameRows;
                settings.GameColumns = _gameCols;
                _ = _settingsService.SaveSettingsAsync(userId, settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存游戏行/列配置失败");
            }
        }

        /// <summary>保存"跳过已知项"开关到用户设置。</summary>
        private void PersistSkipKnown(bool value)
        {
            try
            {
                var userId = _userSessionService.CurrentUserId;
                if (string.IsNullOrEmpty(userId)) return;
                var settings = _settingsService.LoadSettingsAsync(userId).GetAwaiter().GetResult();
                settings.SkipKnown = value;
                _ = _settingsService.SaveSettingsAsync(userId, settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存游戏跳过已知项配置失败");
            }
        }

        /// <summary>
        /// 按当前行/列配置计算每组词条数（每词条两张卡，卡片数为 行×列）。
        /// </summary>
        protected int MaxCountForGrid() => Math.Max(2, (_gameRows * _gameCols) / 2);

        /// <summary>
        /// 统计当前词库仍可学习的条目总数（供前端"总剩余"展示）。子类基于各自词库服务实现。
        /// </summary>
        protected virtual int CountRemainingTotal(LearningContext context) => 0;

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

        // ---------- WebView2 ----------

        private async void FormBase_Load(object? sender, EventArgs e)
        {
            LoadSavedSettings();
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

        /// <summary>加载本地保存的行/列与跳过已知项配置（下次打开自动恢复）。</summary>
        private void LoadSavedSettings()
        {
            try
            {
                var userId = _userSessionService.CurrentUserId;
                if (string.IsNullOrEmpty(userId)) return;
                var settings = _settingsService.LoadSettingsAsync(userId).GetAwaiter().GetResult();
                _gameRows = Math.Clamp(settings.GameRows, (int)_numRows.Minimum, (int)_numRows.Maximum);
                _gameCols = Math.Clamp(settings.GameColumns, (int)_numCols.Minimum, (int)_numCols.Maximum);
                _skipKnown = settings.SkipKnown;

                _numRows.Value = _gameRows;
                _numCols.Value = _gameCols;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载游戏行/列配置失败");
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

                // 诊断：捕获前端 window.onerror 与页面加载快照，转发到宿主写日志
                _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                    "window.addEventListener('error',function(ev){try{if(window.chrome&&window.chrome.webview)window.chrome.webview.postMessage({type:'__diag',kind:'jserror',msg:String((ev&&ev.message)||'')});}catch(_){}});" +
                    "window.addEventListener('DOMContentLoaded',function(){try{if(window.chrome&&window.chrome.webview)window.chrome.webview.postMessage({type:'__diag',kind:'loaded',gameui:!!window.GameUI,site:location.origin+location.pathname});}catch(_){}});");

                NavigateToGamePage();
            }
        }

        /// <summary>
        /// 通过虚拟主机映射加载游戏页面，保证 Resources 目录内所有文件（含 Shared 公共件）在统一 origin 下可被相对引用，
        /// 避免 file:// 协议下跨目录引用 ../Shared/* 不可靠的问题。
        /// </summary>
        private void NavigateToGamePage()
        {
            const string host = "games.local";
            var resourcesRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");

            if (_webView?.CoreWebView2 != null && Directory.Exists(resourcesRoot))
            {
                try
                {
                    _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        host, resourcesRoot, CoreWebView2HostResourceAccessKind.Allow);

                    var relative = HtmlFileRelativePath.Replace('\\', '/');
                    // HtmlFileRelativePath 形如 "Resources/WordMatchGame/index.html"，映射根是 Resources，去掉该前缀
                    if (relative.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
                    {
                        relative = relative.Substring("Resources/".Length);
                    }
                    _webView.CoreWebView2.Navigate($"https://{host}/{relative}");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "虚拟主机映射失败，回退到 file:// 导航");
                }
            }

            // 回退：直接以 file:// 打开 HTML
            var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, HtmlFileRelativePath);
            if (File.Exists(htmlPath))
            {
                _webView?.CoreWebView2?.Navigate(new Uri(htmlPath).AbsoluteUri);
            }
            else
            {
                MessageBox.Show("无法找到游戏页面文件：" + htmlPath, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 开始游戏：构建上下文与数据，序列化后注入前端。
        /// 若页面尚未加载完成，先缓存 _pendingPayload，待 NavigationCompleted 后补发，避免消息竞态丢失。
        /// </summary>
        protected virtual void StartGame()
        {
            if (_webView?.CoreWebView2 == null)
            {
                MessageBox.Show("游戏页面尚未初始化，请等待页面加载完成后再试。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var userId = _userSessionService.CurrentUserId;
            _currentContext = new LearningContext(userId, _subject, _subCategory);

            var themeName = _themeService.CurrentTheme == ThemeMode.Dark ? "dark" : "light";
            var data = BuildData(_currentContext, themeName);
            if (data == null)
            {
                _currentContext = null;
                return; // 子类已提示用户无可玩数据
            }

            var payload = JsonSerializer.Serialize(new
            {
                type = "init",
                data,
                theme = themeName,
                meta = new
                {
                    rows = _gameRows,
                    cols = _gameCols,
                    skipKnown = _skipKnown,
                    totalRemaining = CountRemainingTotal(_currentContext)
                }
            }, SerializerOptions);
            Diag($"StartGame ready={_webViewReady} 数据非空={data != null}");
            _pendingPayload = payload;
            if (_webViewReady)
            {
                _pendingPayload = null;
                _webView.CoreWebView2.PostWebMessageAsJson(payload);
            }
        }

        private void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            Diag($"NavigationCompleted 成功={e.IsSuccess}");
            if (e.IsSuccess && _webView?.CoreWebView2 != null)
            {
                _webViewReady = true;
                Diag($"页面已就绪，Source={_webView.CoreWebView2.Source}");
                // 「开始游戏」若在页面加载完成前被点击，此处补发待发送数据。
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

                if (type == "gameEnd" && _currentContext != null)
                {
                    TrackAnsweredCorrect(root, CurrentUserId);
                    OnGameEnd(root, _currentContext);
                }
                // 前端"换一组"：按当前科目/子类别重新抽词并下发新数据
                else if (type == "restart" && _webViewReady)
                {
                    StartGame();
                }
                // 前端"换一组"旁单选按钮：跳过已知项 / 加载所有
                else if (type == "setting")
                {
                    if (root.TryGetProperty("skipKnown", out var skProp) &&
                        (skProp.ValueKind == JsonValueKind.True || skProp.ValueKind == JsonValueKind.False))
                    {
                        _skipKnown = skProp.ValueKind == JsonValueKind.True;
                        PersistSkipKnown(_skipKnown);
                    }
                }
                // 前端监听器已就绪：若 init 已缓存且尚未发出，补发数据
                else if (type == "__ready")
                {
                    if (_pendingPayload != null)
                    {
                        var payload = _pendingPayload;
                        _pendingPayload = null;
                        _webView.CoreWebView2.PostWebMessageAsJson(payload);
                    }
                }
                // 前端请求朗读：用系统 TTS 播放，比 WebView2 speechSynthesis 更可靠
                else if (type == "speak")
                {
                    var text = root.TryGetProperty("text", out var t) ? t.GetString() : "";
                    var lang = root.TryGetProperty("lang", out var l) ? l.GetString() : "en-US";
                    if (!string.IsNullOrWhiteSpace(text)) SpeakHost(text, lang);
                }
                // 诊断消息：前端脚本错误 / 页面加载快照
                else if (type == "__diag")
                {
                    var kind = root.TryGetProperty("kind", out var k) ? k.GetString() : "";
                    if (kind == "jserror")
                    {
                        var msg = root.TryGetProperty("msg", out var m) ? m.GetString() : "";
                        Diag($"[前端JS错误] {msg}");
                    }
                    else if (kind == "loaded")
                    {
                        var gameui = root.TryGetProperty("gameui", out var g) && g.GetBoolean();
                        var site = root.TryGetProperty("site", out var s) ? s.GetString() : "";
                        Diag($"[页面已加载] GameUI存在={gameui} 地址={site}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理游戏消息失败");
            }
        }

        /// <summary>记录某用户本窗口"已答对"的词条 Id；换一组(BuildData)时据此排除。</summary>
        private void TrackAnsweredCorrect(JsonElement gameRoot, string userId)
        {
            if (string.IsNullOrEmpty(userId) || !gameRoot.TryGetProperty("results", out var resultsProp)) return;
            if (!_correctIdsByUser.TryGetValue(userId, out var set))
            {
                set = new HashSet<string>();
                _correctIdsByUser[userId] = set;
            }
            foreach (var r in resultsProp.EnumerateArray())
            {
                if (!r.TryGetProperty("id", out var idProp) || !r.TryGetProperty("correct", out var cProp)) continue;
                if (cProp.ValueKind != JsonValueKind.True) continue;
                var id = idProp.GetString();
                if (!string.IsNullOrEmpty(id)) set.Add(id);
            }
        }

        /// <summary>当前用户在本次窗口已答对的词条 Id（换一组时排除；区分用户）。</summary>
        protected IReadOnlyCollection<string> ExcludeAnsweredCorrectIds()
        {
            var uid = CurrentUserId;
            if (string.IsNullOrEmpty(uid)) return Array.Empty<string>();
            return _correctIdsByUser.TryGetValue(uid, out var set) ? set : (IReadOnlyCollection<string>)Array.Empty<string>();
        }

        /// <summary>写诊断日志到输出目录 game-diag.log，便于定位页面加载/启动问题。</summary>
        private void Diag(string msg)
        {
            try
            {
                File.AppendAllText(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game-diag.log"),
                    $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\r\n");
            }
            catch { /* 诊断日志失败不影响功能 */ }
        }

        /// <summary>用系统 TTS 后台朗读文本（WebView2 前端 speechSynthesis 多不稳定，改用宿主发声）。</summary>
        private void SpeakHost(string text, string lang)
        {
            Task.Run(() =>
            {
                try
                {
                    using var synth = new System.Speech.Synthesis.SpeechSynthesizer();
                    synth.Rate = 0;
                    synth.Volume = 100;
                    // 尽量挑选匹配语种的声音（常见 en-US）
                    var want = lang.StartsWith("en") ? "en" : (lang.StartsWith("zh") ? "zh" : null);
                    if (want != null)
                    {
                        foreach (var v in synth.GetInstalledVoices())
                        {
                            var info = v.VoiceInfo;
                            if (!string.IsNullOrEmpty(info.Culture?.Name) && info.Culture.Name.StartsWith(want, StringComparison.OrdinalIgnoreCase))
                            {
                                synth.SelectVoice(v.VoiceInfo.Name);
                                break;
                            }
                        }
                    }
                    synth.Speak(text);
                }
                catch { /* 无语音输出时不抛错 */ }
            });
        }

        private void FormBase_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _webView?.Dispose();
        }

        // ---------- 主题 ----------

        public virtual void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;
            _panelWeb.BackColor = colors.Background;
            _comboSubject.BackColor = colors.Surface;
            _comboSubject.ForeColor = colors.TextPrimary;
            _comboSubCategory.BackColor = colors.Surface;
            _comboSubCategory.ForeColor = colors.TextPrimary;
            _numRows.BackColor = colors.Surface;
            _numRows.ForeColor = colors.TextPrimary;
            _numCols.BackColor = colors.Surface;
            _numCols.ForeColor = colors.TextPrimary;
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