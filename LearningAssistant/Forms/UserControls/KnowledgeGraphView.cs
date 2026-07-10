using LearningAssistant.Services.KnowledgeGraph;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Windows.Forms;
using System.Drawing;
using System.Text.Json;
using System.IO;

namespace LearningAssistant.Forms.UserControls
{
    public class KnowledgeGraphView : UserControl
    {
        private Panel _panelToolbar = null!;
        private Button _button2D = null!;
        private Button _button3D = null!;
        private Button _buttonRefresh = null!;
        private ComboBox _comboFilter = null!;
        private Label _labelStatus = null!;
        private Panel _panelContent = null!;
        private Label _labelPlaceholder = null!;
        private WebView2? _webView;
        private CoreWebView2Environment? _webViewEnvironment;

        private IKnowledgeGraphService? _graphService;
        private string _currentUserId = "default";
        private bool _isWebViewInitialized = false;

        public event EventHandler<string>? NodeClicked;
        public event EventHandler? GraphLoaded;

        public KnowledgeGraphView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _panelToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.FromArgb(33, 33, 33),
                Padding = new Padding(10, 5, 10, 5)
            };

            var toolbarLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1
            };

            for (int i = 0; i < 5; i++)
            {
                toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            }

            _button2D = CreateToolbarButton("2D", true);
            _button2D.Click += (s, e) => SetDimension(2);

            _button3D = CreateToolbarButton("3D", false);
            _button3D.Click += (s, e) => SetDimension(3);

            _buttonRefresh = CreateToolbarButton("刷新", false);
            _buttonRefresh.Click += async (s, e) => await RefreshGraphAsync();

            _comboFilter = new ComboBox
            {
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 120,
                Font = new Font("微软雅黑", 9F)
            };
            _comboFilter.Items.Add("全部");
            _comboFilter.SelectedIndex = 0;
            _comboFilter.SelectedIndexChanged += async (s, e) => await ApplyFilterAsync();

            _labelStatus = new Label
            {
                Text = "就绪",
                ForeColor = Color.FromArgb(180, 180, 180),
                AutoSize = true,
                Font = new Font("微软雅黑", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            };

            toolbarLayout.Controls.Add(_button2D, 0, 0);
            toolbarLayout.Controls.Add(_button3D, 1, 0);
            toolbarLayout.Controls.Add(_buttonRefresh, 2, 0);
            toolbarLayout.Controls.Add(_comboFilter, 3, 0);
            toolbarLayout.Controls.Add(_labelStatus, 4, 0);

            _panelToolbar.Controls.Add(toolbarLayout);

            _panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 30)
            };

            _labelPlaceholder = new Label
            {
                Text = "🌐 知识图谱\n\n加载中...",
                Font = new Font("微软雅黑", 12F),
                ForeColor = Color.FromArgb(180, 180, 180),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            _panelContent.Controls.Add(_labelPlaceholder);

            Controls.Add(_panelContent);
            Controls.Add(_panelToolbar);

            BackColor = Color.FromArgb(20, 20, 30);

            Load += KnowledgeGraphView_Load;
        }

        private async void KnowledgeGraphView_Load(object? sender, EventArgs e)
        {
            await InitializeWebViewAsync();
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    "LearningAssistant", "WebView2Cache");
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }

                _webViewEnvironment = await CoreWebView2Environment.CreateAsync(null, cacheDir);

                _webView = new WebView2
                {
                    Dock = DockStyle.Fill,
                    Visible = false
                };

                _panelContent.Controls.Add(_webView);

                await _webView.EnsureCoreWebView2Async(_webViewEnvironment);

                if (_webView.CoreWebView2 != null)
                {
                    _webView.CoreWebView2.Settings.IsScriptEnabled = true;
                    _webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
#if DEBUG
                    _webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
#endif

                    _webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                    _webView.NavigationCompleted += WebView_NavigationCompleted;

                    var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                        "Resources", "KnowledgeGraph", "kg-visualization.html");
                    if (File.Exists(htmlPath))
                    {
                        _webView.CoreWebView2.Navigate($"file:///{htmlPath}");
                    }
                    else
                    {
                        _labelPlaceholder.Text = "📊 知识图谱\n\n无法找到可视化文件";
                    }
                }
            }
            catch (Exception ex)
            {
                _labelPlaceholder.Text = "🌐 知识图谱\n\nWebView2 Runtime 未安装\n请安装后重试";
                System.Diagnostics.Trace.TraceError($"初始化 WebView2 失败: {ex}");
            }
        }

        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                _isWebViewInitialized = true;
                _labelPlaceholder.Visible = false;
                _webView?.Show();
                UpdateStatus("可视化已就绪");
            }
        }

        private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = JsonSerializer.Deserialize<GraphMessage>(e.WebMessageAsJson);
                if (message?.Type == "nodeClicked" && !string.IsNullOrEmpty(message.Data?.Id))
                {
                    NodeClicked?.Invoke(this, message.Data.Id);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"解析图谱消息失败: {ex}");
            }
        }

        private class GraphMessage
        {
            public string? Type { get; set; }
            public GraphMessageData? Data { get; set; }
        }

        private class GraphMessageData
        {
            public string? Id { get; set; }
            public string? Label { get; set; }
        }

        private static Button CreateToolbarButton(string text, bool active)
        {
            return new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = active ? Color.FromArgb(33, 150, 243) : Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Size = new Size(50, 30),
                Margin = new Padding(5, 0, 5, 0),
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
        }

        public void SetService(IKnowledgeGraphService service)
        {
            _graphService = service;
        }

        public void SetUserId(string userId)
        {
            _currentUserId = userId;
        }

        public async Task LoadGraphAsync()
        {
            if (_graphService == null) return;

            try
            {
                UpdateStatus("正在加载图谱...");

                var graph = await _graphService.GetGraphAsync(_currentUserId);

                if (graph != null && graph.NodeCount > 0)
                {
                    UpdateFilterOptions(graph);
                    UpdateStatus($"已加载 {graph.NodeCount} 个节点, {graph.EdgeCount} 条关系");
                    GraphLoaded?.Invoke(this, EventArgs.Empty);

                    await SendGraphToWebView(graph);
                }
                else
                {
                    UpdateStatus("暂无数据");
                    await SendGraphToWebView(null);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"加载失败: {ex.Message}");
                System.Diagnostics.Trace.TraceError($"加载图谱失败: {ex}");
            }
        }

        private async Task SendGraphToWebView(Models.KnowledgeGraph.KnowledgeGraph? graph)
        {
            if (!_isWebViewInitialized || _webView?.CoreWebView2 == null)
                return;

            try
            {
                var data = new
                {
                    nodes = (graph?.Nodes ?? new List<Models.KnowledgeGraph.KGNode>()).Select(n => new
                    {
                        id = n.Id,
                        label = n.Label,
                        category = n.Category,
                        masteryLevel = n.MasteryLevel,
                        size = Math.Max(10, n.MasteryLevel * 50),
                        color = GetNodeColor(n.MasteryLevel)
                    }),
                    links = (graph?.Edges ?? new List<Models.KnowledgeGraph.KGEdge>()).Select(e => new
                    {
                        source = e.Source,
                        target = e.Target,
                        label = e.Label,
                        strength = e.Strength,
                        color = "#757575"
                    })
                };

                var json = JsonSerializer.Serialize(data);
                var script = $"window.setData({json});";
                await _webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"发送图谱数据失败: {ex}");
            }
        }

        private string GetNodeColor(double masteryLevel)
        {
            if (masteryLevel < 0.3) return "#F44336";
            if (masteryLevel < 0.7) return "#FFC107";
            return "#4CAF50";
        }

        public async Task HighlightNodeAsync(string nodeId)
        {
            if (!_isWebViewInitialized || _webView?.CoreWebView2 == null)
                return;

            try
            {
                var escapedId = JsonSerializer.Serialize(nodeId);
                var script = $"window.highlight({escapedId});";
                await _webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"高亮节点失败: {ex}");
            }
        }

        public async Task ClearHighlightAsync()
        {
            if (!_isWebViewInitialized || _webView?.CoreWebView2 == null)
                return;

            try
            {
                await _webView.CoreWebView2.ExecuteScriptAsync("window.clearHighlightAll();");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"清除高亮失败: {ex}");
            }
        }

        public async Task RefreshGraphAsync()
        {
            await LoadGraphAsync();
        }

        private void SetDimension(int dim)
        {
            _button2D.BackColor = dim == 2 ? Color.FromArgb(33, 150, 243) : Color.FromArgb(60, 60, 60);
            _button3D.BackColor = dim == 3 ? Color.FromArgb(33, 150, 243) : Color.FromArgb(60, 60, 60);

            if (_isWebViewInitialized && _webView?.CoreWebView2 != null)
            {
                try
                {
                    _webView.CoreWebView2.ExecuteScriptAsync($"window.setDimension({dim});");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError($"设置维度失败: {ex}");
                }
            }
        }

        private void UpdateFilterOptions(Models.KnowledgeGraph.KnowledgeGraph graph)
        {
            _comboFilter.Items.Clear();
            _comboFilter.Items.Add("全部");

            var categories = graph.Nodes
                .Select(n => n.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c);

            _comboFilter.Items.AddRange(categories.ToArray());
            _comboFilter.SelectedIndex = 0;
        }

        private async Task ApplyFilterAsync()
        {
            var selected = _comboFilter.SelectedItem?.ToString() ?? "全部";
            UpdateStatus($"筛选: {selected}");
            await Task.CompletedTask;
        }

        private void UpdateStatus(string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => UpdateStatus(status));
                return;
            }
            _labelStatus.Text = status;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_webView?.CoreWebView2 != null)
                {
                    _webView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
                }
                _webView?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
