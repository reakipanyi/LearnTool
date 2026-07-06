using LearningAssistant.Services.KnowledgeGraph;
using System.Windows.Forms;
using System.Drawing;

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

        private IKnowledgeGraphService? _graphService;
        private string _currentUserId = "default";

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
                Text = "🌐 知识图谱\n\nWebView2 可视化版本\n需要安装 WebView2 Runtime",
                Font = new Font("微软雅黑", 12F),
                ForeColor = Color.FromArgb(180, 180, 180),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            _panelContent.Controls.Add(_labelPlaceholder);

            Controls.Add(_panelContent);
            Controls.Add(_panelToolbar);

            BackColor = Color.FromArgb(20, 20, 30);
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

                    UpdatePlaceholder(graph);
                }
                else
                {
                    UpdateStatus("暂无数据");
                    _labelPlaceholder.Text = "📊 暂无知识图谱数据\n\n请先添加学习内容以构建知识图谱";
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"加载失败: {ex.Message}");
                System.Diagnostics.Trace.TraceError($"加载图谱失败: {ex}");
            }
        }

        private void UpdatePlaceholder(Models.KnowledgeGraph.KnowledgeGraph graph)
        {
            var weakNodes = graph.Nodes
                .OrderBy(n => n.MasteryLevel)
                .Take(5)
                .ToList();

            var weakList = string.Join("\n", weakNodes.Select(n => $"  • {n.Label} ({(n.MasteryLevel * 100):0}%)"));

            _labelPlaceholder.Text =
                $"📊 知识图谱统计\n\n" +
                $"节点数: {graph.NodeCount}\n" +
                $"关系数: {graph.EdgeCount}\n\n" +
                $"薄弱知识点 Top 5:\n{weakList}\n\n" +
                $"💡 安装 WebView2 Runtime 可查看 3D 可视化";
        }

        public async Task HighlightNodeAsync(string nodeId)
        {
            await Task.CompletedTask;
        }

        public async Task ClearHighlightAsync()
        {
            await Task.CompletedTask;
        }

        public async Task RefreshGraphAsync()
        {
            await LoadGraphAsync();
        }

        private void SetDimension(int dim)
        {
            _button2D.BackColor = dim == 2 ? Color.FromArgb(33, 150, 243) : Color.FromArgb(60, 60, 60);
            _button3D.BackColor = dim == 3 ? Color.FromArgb(33, 150, 243) : Color.FromArgb(60, 60, 60);
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
    }
}
