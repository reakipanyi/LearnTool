using LearningAssistant.Models.KnowledgeGraph;
using LearningAssistant.Services.KnowledgeGraph;
using Microsoft.Web.WebView2.Core;

namespace LearningAssistant.Forms.UserControls
{
    /// <summary>
    /// 知识图谱可视化控件 - 基于WebView2 + D3.js
    /// </summary>
    public class KnowledgeGraphView : UserControl
    {
        private Microsoft.Web.WebView2.WinForms.WebView2 _webView = null!;
        private Panel _panelToolbar = null!;
        private Button _button2D = null!;
        private Button _button3D = null!;
        private Button _buttonRefresh = null!;
        private ComboBox _comboFilter = null!;
        private Label _labelStatus = null!;

        private IKnowledgeGraphService? _graphService;
        private string _currentUserId = "default";
        private KnowledgeGraph? _currentGraph;

        public event EventHandler<string>? NodeClicked;
        public event EventHandler? GraphLoaded;

        public KnowledgeGraphView()
        {
            InitializeComponent();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                var env = await Microsoft.Web.WebView2.WinForms.WebView2Environment.CreateAsync();
                _webView = new Microsoft.Web.WebView2.WinForms.WebView2
                {
                    Dock = DockStyle.Fill,
                    Source = new Uri(GetEmbeddedHtmlPath())
                };

                _webView.WebMessageReceived += OnWebMessageReceived;

                Controls.Add(_webView);
            }
            catch (Exception ex)
            {
                // WebView2未安装时显示提示
                var label = new Label
                {
                    Text = "请安装 WebView2 Runtime 以查看知识图谱",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.Gray
                };
                Controls.Add(label);

                System.Diagnostics.Debug.WriteLine($"WebView2初始化失败: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            // 工具栏
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

            // 维度切换按钮
            _button2D = CreateToolbarButton("2D", true);
            _button2D.Click += (s, e) => SetDimension(2);

            _button3D = CreateToolbarButton("3D", false);
            _button3D.Click += (s, e) => SetDimension(3);

            // 刷新按钮
            _buttonRefresh = CreateToolbarButton("刷新", false);
            _buttonRefresh.Click += async (s, e) => await RefreshGraphAsync();

            // 分类筛选
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

            // 状态标签
            _labelStatus = new Label
            {
                Text = "就绪",
                ForeColor = Color.FromArgb(180, 180, 180),
                AutoSize = true,
                Anchor = AnchorStyles.Right | AnchorStyles.VerticalCenter,
                Font = new Font("微软雅黑", 9F)
            };

            toolbarLayout.Controls.Add(_button2D, 0, 0);
            toolbarLayout.Controls.Add(_button3D, 1, 0);
            toolbarLayout.Controls.Add(_buttonRefresh, 2, 0);
            toolbarLayout.Controls.Add(_comboFilter, 3, 0);
            toolbarLayout.Controls.Add(_labelStatus, 4, 0);

            _panelToolbar.Controls.Add(toolbarLayout);
            Controls.Add(_panelToolbar);
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

        #region 公共方法

        /// <summary>
        /// 设置知识图谱服务
        /// </summary>
        public void SetService(IKnowledgeGraphService service)
        {
            _graphService = service;
        }

        /// <summary>
        /// 设置当前用户ID
        /// </summary>
        public void SetUserId(string userId)
        {
            _currentUserId = userId;
        }

        /// <summary>
        /// 加载图谱
        /// </summary>
        public async Task LoadGraphAsync()
        {
            if (_graphService == null) return;

            try
            {
                UpdateStatus("正在加载图谱...");

                _currentGraph = await _graphService.GetGraphAsync(_currentUserId);

                if (_currentGraph != null && _currentGraph.Nodes.Count > 0)
                {
                    // 更新筛选下拉框
                    UpdateFilterOptions();

                    // 发送到WebView渲染
                    await RenderGraphAsync(_currentGraph);

                    UpdateStatus($"已加载 {_currentGraph.NodeCount} 个节点");
                    GraphLoaded?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    UpdateStatus("暂无数据");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"加载失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"加载图谱失败: {ex}");
            }
        }

        /// <summary>
        /// 高亮指定节点
        /// </summary>
        public async Task HighlightNodeAsync(string nodeId)
        {
            await ExecuteScriptAsync($"highlight('{nodeId}')");
        }

        /// <summary>
        /// 清除高亮
        /// </summary>
        public async Task ClearHighlightAsync()
        {
            await ExecuteScriptAsync("clearHighlightAll()");
        }

        /// <summary>
        /// 刷新图谱
        /// </summary>
        public async Task RefreshGraphAsync()
        {
            await LoadGraphAsync();
        }

        #endregion

        #region 私有方法

        private async Task RenderGraphAsync(KnowledgeGraph graph)
        {
            var dto = graph.ToDto();
            var json = System.Text.Json.JsonSerializer.Serialize(dto);

            await ExecuteScriptAsync($"setData({json})");
        }

        private async Task ExecuteScriptAsync(string script)
        {
            if (_webView?.CoreWebView2 == null) return;

            try
            {
                await _webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"执行脚本失败: {ex.Message}");
            }
        }

        private void SetDimension(int dim)
        {
            _button2D.BackColor = dim == 2 ? Color.FromArgb(33, 150, 243) : Color.FromArgb(60, 60, 60);
            _button3D.BackColor = dim == 3 ? Color.FromArgb(33, 150, 243) : Color.FromArgb(60, 60, 60);

            _ = ExecuteScriptAsync($"setDimension({dim})");
        }

        private void UpdateFilterOptions()
        {
            if (_currentGraph == null) return;

            _comboFilter.Items.Clear();
            _comboFilter.Items.Add("全部");

            var categories = _currentGraph.Nodes
                .Select(n => n.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c);

            _comboFilter.Items.AddRange(categories.ToArray());
            _comboFilter.SelectedIndex = 0;
        }

        private async Task ApplyFilterAsync()
        {
            if (_currentGraph == null || _webView?.CoreWebView2 == null) return;

            var selected = _comboFilter.SelectedItem?.ToString() ?? "全部";

            if (selected == "全部")
            {
                await RenderGraphAsync(_currentGraph);
            }
            else
            {
                var filtered = new KnowledgeGraph
                {
                    Id = _currentGraph.Id,
                    Name = _currentGraph.Name,
                    UserId = _currentGraph.UserId,
                    Nodes = _currentGraph.Nodes.Where(n => n.Category == selected).ToList(),
                    Edges = _currentGraph.Edges.Where(e =>
                        filtered != null && filtered.Nodes.Any(n => n.Id == e.Source || n.Id == e.Target)
                    ).ToList()
                };

                await RenderGraphAsync(filtered);
            }
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = System.Text.Json.JsonSerializer.Deserialize<WebViewMessage>(e.WebMessageAsJson);
                if (message?.Type == "nodeClicked")
                {
                    var nodeId = message.Data?.GetProperty("id")?.GetString();
                    if (!string.IsNullOrEmpty(nodeId))
                    {
                        NodeClicked?.Invoke(this, nodeId);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析Web消息失败: {ex.Message}");
            }
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

        private static string GetEmbeddedHtmlPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var htmlPath = Path.Combine(baseDir, "Resources", "KnowledgeGraph", "kg-visualization.html");

            // 如果嵌入资源不存在，创建默认页面
            if (!File.Exists(htmlPath))
            {
                // 返回一个简单的占位页面
                return "about:blank";
            }

            return new Uri(htmlPath).AbsoluteUri;
        }

        private class WebViewMessage
        {
            public string? Type { get; set; }
            public System.Text.Json.JsonElement? Data { get; set; }
        }

        #endregion
    }
}
