using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.PanAnalysis;
using LearningAssistant.Services.PanAnalysis;

namespace LearningAssistant.Forms
{
    /// <summary>
    /// 百度网盘 AI 分析窗体
    /// </summary>
    public partial class BaiduPanAnalysisForm : Form, IThemeable
    {
        private readonly IBaiduPanAnalysisOrchestrator _orchestrator;
        private readonly string _directoryPath;
        private readonly IThemeService _themeService;
        private CancellationTokenSource? _cts;
        private PanDirectorySnapshot? _snapshot;
        private PanAnalysisResult? _analysisResult;

        /// <summary>是否已成功执行过任何操作（供父窗体判断是否刷新）</summary>
        public bool ExecutedAny { get; private set; }

        // UI 控件
        private Label lblPath;
        private TextBox txtPath;
        private Label lblDepth;
        private ComboBox cmbDepth;
        private CheckBox chkDetectDuplicates;
        private CheckBox chkUseCache;
        private Button btnStartAnalysis;
        private Button btnCancel;
        private Button btnExecute;
        private TextBox txtSummary;
        private TextBox txtStats;
        private ListView lstRecommendations;
        private TextBox txtLog;

        public BaiduPanAnalysisForm(
            IBaiduPanAnalysisOrchestrator orchestrator,
            string directoryPath,
            IThemeService themeService)
        {
            _orchestrator = orchestrator;
            _directoryPath = directoryPath;
            _themeService = themeService;
            InitializeComponent();
            _themeService?.RegisterThemeable(this);
            ApplyTheme(_themeService?.CurrentColors ?? ThemeService.GetColors(ThemeMode.Light));
        }

        private void InitializeComponent()
        {
            Text = "百度网盘 AI 分析";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1050, 720);
            MinimumSize = new Size(900, 600);

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(8),
                Margin = Padding.Empty
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));

            // 顶部选项栏
            var topPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(4, 6, 4, 6),
                AutoSize = false
            };

            lblPath = new Label { Text = "目录：", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 8, 2, 8) };
            txtPath = new TextBox
            {
                Text = _directoryPath,
                ReadOnly = true,
                Width = 260,
                Margin = new Padding(2, 6, 8, 6)
            };

            lblDepth = new Label { Text = "深度：", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 8, 2, 8) };
            cmbDepth = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 70,
                Margin = new Padding(2, 6, 8, 6)
            };
            cmbDepth.Items.AddRange(new object[] { 1, 2, 3, 5, 0 });
            cmbDepth.SelectedIndex = 1; // 默认深度 2

            chkDetectDuplicates = new CheckBox
            {
                Text = "重复检测",
                AutoSize = true,
                Checked = true,
                Margin = new Padding(4, 8, 4, 8)
            };
            chkUseCache = new CheckBox
            {
                Text = "使用缓存",
                AutoSize = true,
                Checked = true,
                Margin = new Padding(4, 8, 8, 8)
            };

            btnStartAnalysis = new Button
            {
                Text = "🚀 开始分析",
                Width = 110,
                Margin = new Padding(8, 4, 4, 4)
            };
            btnStartAnalysis.Click += btnStartAnalysis_Click;

            btnCancel = new Button
            {
                Text = "取消",
                Width = 70,
                Enabled = false,
                Margin = new Padding(4, 4, 4, 4)
            };
            btnCancel.Click += (s, e) => _cts?.Cancel();

            btnExecute = new Button
            {
                Text = "✅ 执行选中操作",
                Width = 140,
                Margin = new Padding(4, 4, 4, 4)
            };
            btnExecute.Click += btnExecute_Click;

            topPanel.Controls.AddRange(new Control[]
            {
                lblPath, txtPath, lblDepth, cmbDepth,
                chkDetectDuplicates, chkUseCache,
                btnStartAnalysis, btnCancel, btnExecute
            });

            // 中间内容区
            var contentPanel = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 320,
                Panel1MinSize = 260,
                Panel2MinSize = 500
            };

            var leftPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 6, 0) };
            txtStats = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = SystemColors.Window,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            leftPanel.Controls.Add(txtStats);
            contentPanel.Panel1.Controls.Add(leftPanel);

            var rightPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 4, 0, 0) };

            txtSummary = new TextBox
            {
                Dock = DockStyle.Top,
                Multiline = true,
                ReadOnly = true,
                Height = 64,
                BackColor = SystemColors.Info,
                Font = new Font("Microsoft YaHei UI", 9F),
                ScrollBars = ScrollBars.Vertical
            };

            lstRecommendations = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                CheckBoxes = true,
                GridLines = true,
                MultiSelect = true,
                HideSelection = false
            };
            lstRecommendations.Columns.Add("选择", 45);
            lstRecommendations.Columns.Add("操作", 90);
            lstRecommendations.Columns.Add("优先级", 70);
            lstRecommendations.Columns.Add("名称", 240);
            lstRecommendations.Columns.Add("目标", 220);
            lstRecommendations.Columns.Add("原因", 320);

            rightPanel.Controls.Add(lstRecommendations);
            rightPanel.Controls.Add(txtSummary);
            contentPanel.Panel2.Controls.Add(rightPanel);

            // 底部日志
            txtLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.Black,
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9F)
            };

            mainPanel.Controls.Add(topPanel, 0, 0);
            mainPanel.Controls.Add(contentPanel, 0, 1);
            mainPanel.Controls.Add(txtLog, 0, 2);

            Controls.Add(mainPanel);

            // 初始状态
            txtPath.Text = _directoryPath;
            AppendLog($"准备分析目录：{_directoryPath}");
            AppendLog("点击「开始分析」获取目录快照并生成整理建议。");
        }

        #region 事件处理

        private async void btnStartAnalysis_Click(object sender, EventArgs e)
        {
            _cts = new CancellationTokenSource();

            // 禁用按钮
            btnStartAnalysis.Enabled = false;
            btnCancel.Enabled = true;
            btnExecute.Enabled = false;
            lstRecommendations.Items.Clear();
            txtSummary.Clear();
            txtStats.Clear();

            var progress = new Progress<PanAnalysisProgress>(UpdateProgress);

            try
            {
                var options = new AnalysisOptions
                {
                    MaxDepth = (int)cmbDepth.SelectedItem,
                    DetectDuplicates = chkDetectDuplicates.Checked,
                    DetectJunkFiles = true,
                    UseCache = chkUseCache.Checked
                };

                // 获取快照
                AppendLog("正在获取目录快照...");
                _snapshot = await _orchestrator.GetSnapshotAsync(
                    _directoryPath, options, progress, _cts.Token);

                // AI 分析
                AppendLog("正在调用 AI 进行分析...");
                _analysisResult = await _orchestrator.AnalyzeDirectoryAsync(
                    _directoryPath, options, progress, _cts.Token);

                // 展示结果
                DisplayStatistics(_snapshot.Statistics);
                DisplayRecommendations(_analysisResult.Recommendations);
                txtSummary.Text = _analysisResult.Summary;
                AppendLog($"分析完成：共 {_analysisResult.Recommendations.Count} 条建议，耗时 {_analysisResult.AnalysisDuration?.TotalSeconds:F1}s");
            }
            catch (OperationCanceledException)
            {
                AppendLog("分析已取消");
            }
            catch (Exception ex)
            {
                AppendLog($"错误：{ex.Message}");
                MessageBox.Show($"分析失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnStartAnalysis.Enabled = true;
                btnCancel.Enabled = false;
                btnExecute.Enabled = lstRecommendations.Items.Count > 0;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async void btnExecute_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedRecommendations();
            if (!selected.Any())
            {
                MessageBox.Show("请至少选择一项操作", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var summary = $"将执行以下操作：\n" +
                          $"- 删除：{selected.Count(r => r.Type == PanRecommendationType.Delete)} 项\n" +
                          $"- 移动：{selected.Count(r => r.Type == PanRecommendationType.Move)} 项\n" +
                          $"- 重命名：{selected.Count(r => r.Type == PanRecommendationType.Rename)} 项\n\n" +
                          $"删除操作会进入回收站（可恢复），确认执行？";

            if (MessageBox.Show(summary, "执行确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            _cts = new CancellationTokenSource();
            btnExecute.Enabled = false;
            btnStartAnalysis.Enabled = false;
            btnCancel.Enabled = true;

            var progress = new Progress<PanAnalysisProgress>(UpdateProgress);

            try
            {
                var report = await _orchestrator.ExecuteRecommendationsAsync(
                    selected, progress, _cts.Token);

                AppendLog($"执行完成：成功 {report.Succeeded}，失败 {report.Failed}，跳过 {report.Skipped}");
                if (report.Failed > 0)
                {
                    foreach (var failed in report.Results.Where(r => !r.Success))
                    {
                        AppendLog($"  ✗ {failed.Recommendation.TargetName}: {failed.ErrorMessage}");
                    }
                }
                MessageBox.Show(report.Summary, "执行结果", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ExecutedAny = report.Succeeded > 0;
            }
            catch (OperationCanceledException)
            {
                AppendLog("执行已取消");
            }
            catch (Exception ex)
            {
                AppendLog($"执行错误：{ex.Message}");
                MessageBox.Show($"执行失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnExecute.Enabled = true;
                btnStartAnalysis.Enabled = true;
                btnCancel.Enabled = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        #endregion

        #region 展示与辅助

        private void UpdateProgress(PanAnalysisProgress progress)
        {
            var phaseName = progress.Phase switch
            {
                PanAnalysisPhase.Initializing => "初始化",
                PanAnalysisPhase.Fetching => "获取文件",
                PanAnalysisPhase.PreComputing => "预计算",
                PanAnalysisPhase.Analyzing => "AI 分析",
                PanAnalysisPhase.Executing => "执行",
                PanAnalysisPhase.Completed => "完成",
                PanAnalysisPhase.Failed => "失败",
                _ => progress.Phase.ToString()
            };
            AppendLog($"[{phaseName}] {progress.Message}");
        }

        private void DisplayStatistics(PanStatistics stats)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("===== 目录统计 =====");
            sb.AppendLine($"文件数：{stats.TotalFileCount:N0}");
            sb.AppendLine($"文件夹数：{stats.TotalFolderCount:N0}");
            sb.AppendLine($"总大小：{stats.TotalSizeFormatted}");
            sb.AppendLine($"无意义文件：{stats.JunkFileCount} 个（{stats.JunkSizeFormatted}）");
            sb.AppendLine($"大文件（>100MB）：{stats.LargeFiles.Count} 个");
            sb.AppendLine($"0 字节文件：{stats.ZeroByteFiles.Count} 个");
            sb.AppendLine();
            sb.AppendLine("===== 文件类型分布 =====");
            foreach (var kv in stats.CountByExtension.OrderByDescending(k => k.Value).Take(15))
            {
                var size = stats.SizeByExtension.GetValueOrDefault(kv.Key, 0);
                sb.AppendLine($"{kv.Key}: {kv.Value} 个（{FormatSize(size)}）");
            }
            sb.AppendLine();
            sb.AppendLine($"===== 重复文件组（{_snapshot?.Duplicates.Count ?? 0} 组） =====");
            if (_snapshot != null)
            {
                foreach (var group in _snapshot.Duplicates.Take(20))
                {
                    sb.AppendLine($"[{group.DisplayName}] 副本×{group.FileCount}（{group.SizeFormatted}）");
                }
            }
            txtStats.Text = sb.ToString();
        }

        private void DisplayRecommendations(List<PanRecommendation> recommendations)
        {
            lstRecommendations.BeginUpdate();
            lstRecommendations.Items.Clear();
            foreach (var rec in recommendations)
            {
                var item = new ListViewItem(rec.IsSelected ? "☑" : "☐");
                item.SubItems.Add(rec.TypeDisplay);
                item.SubItems.Add(rec.PriorityDisplay);
                item.SubItems.Add(rec.TargetName);
                var target = rec.Type switch
                {
                    PanRecommendationType.Move => rec.DestinationPath,
                    PanRecommendationType.Rename => rec.NewName,
                    PanRecommendationType.MergeFolder => rec.DestinationPath,
                    _ => "-"
                };
                item.SubItems.Add(target ?? "-");
                item.SubItems.Add(rec.Reason);
                item.Tag = rec;
                item.Checked = rec.IsSelected;
                lstRecommendations.Items.Add(item);
            }
            lstRecommendations.EndUpdate();
        }

        private List<PanRecommendation> GetSelectedRecommendations()
        {
            var result = new List<PanRecommendation>();
            foreach (ListViewItem item in lstRecommendations.Items)
            {
                if (item.Checked && item.Tag is PanRecommendation rec)
                {
                    rec.IsSelected = true;
                    result.Add(rec);
                }
            }
            return result;
        }

        private void AppendLog(string message)
        {
            if (txtLog == null || txtLog.IsDisposed) return;
            var time = DateTime.Now.ToString("HH:mm:ss");
            if (txtLog.TextLength > 20000)
                txtLog.Clear();
            txtLog.AppendText($"[{time}] {message}\r\n");
        }

        private static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        #endregion

        #region 主题

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;
            ForeColor = colors.TextPrimary;

            if (txtStats != null) txtStats.BackColor = colors.Background;
            if (txtSummary != null)
            {
                txtSummary.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : SystemColors.Info;
                txtSummary.ForeColor = colors.TextPrimary;
            }
            if (lstRecommendations != null)
            {
                lstRecommendations.BackColor = colors.Background;
                lstRecommendations.ForeColor = colors.TextPrimary;
            }
            if (cmbDepth != null) cmbDepth.BackColor = colors.Background;
            if (txtPath != null) txtPath.BackColor = colors.Surface;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _themeService?.UnregisterThemeable(this);
                _cts?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
