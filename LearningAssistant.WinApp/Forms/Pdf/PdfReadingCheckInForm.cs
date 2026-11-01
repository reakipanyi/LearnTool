using System.Drawing.Drawing2D;
using System.Globalization;
using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Forms.Pdf
{
    /// <summary>
    /// 阅读打卡表：按目录折叠展示统计区间内的打卡情况。
    /// 汇总页与矩阵页共用同一份报表与布局模型，双击目录行展开/折叠，双击文档行回到阅读器打开。
    /// </summary>
    public sealed partial class PdfReadingCheckInForm : Form, IThemeable
    {
        private const int MatrixFixedColumnCount = 5;
        private const int MatrixDateColumnWidth = 30;
        private const int MaxEnumeratedPdfs = 5000;
        private const int SummaryCoverageColumn = 7;
        private const int SummaryStreakColumn = 6;
        private const int MatrixStreakColumn = 4;

        // 运行时设置的像素尺寸（行高、缩进、进度条）不会随 AutoScaleMode.Dpi 自动缩放，按 DPI 换算
        private const int FolderRowHeight = 28;
        private const int DocumentRowHeight = 26;
        private const int SummaryDocumentIndent = 24;
        private const int MatrixDocumentIndent = 20;
        private const int CoverageBarInset = 8;
        private const int CoverageBarHeight = 4;
        private const int CoverageBarBottomOffset = 10;

        private static readonly IReadOnlyDictionary<DateTime, int> EmptyDaySeconds =
            new Dictionary<DateTime, int>();

        private readonly PdfReadingCheckInService _service;
        private readonly PdfReadingCheckInPreferenceStore _preferenceStore;
        private readonly PdfReadingCheckInExportService _exportService;
        private readonly Action<string>? _openDocument;
        private readonly ILogger<PdfReadingCheckInForm>? _logger;
        private readonly IThemeService? _themeService;
        private readonly string _initialRootPath;
        private readonly IReadOnlyList<string> _initialDocuments;
        private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 150 };
        private readonly Font _folderRowFont = new("Microsoft YaHei UI", 9F, FontStyle.Bold);
        private readonly Font _streakFont = new("Microsoft YaHei UI", 9F, FontStyle.Bold);
        private readonly Font _smallFont = new("Microsoft YaHei UI", 7F);

        private PdfReadingCheckInPreferences _preferences;
        private IReadOnlyList<PdfCheckInMatrixColumn> _matrixColumns = Array.Empty<PdfCheckInMatrixColumn>();
        private CancellationTokenSource? _refreshCts;
        private PdfReadingCheckInReport? _report;
        private IReadOnlyList<string> _documents;
        private string _rootPath;
        private string _documentLimitNote = string.Empty;
        private DateTime _month;
        private ThemeColors _colors = ThemeService.GetColors(ThemeMode.Light);
        private bool _suppressFilterEvents;
        private bool _disposed;
        private bool _refreshing;
        // 大目录导出在后台线程执行，导出期间禁用导出按钮，避免界面卡顿与重复点击
        private bool _exporting;
        // 矩阵页不在前台时延迟渲染，切换页签后再构建，降低大目录首次统计的界面等待
        private bool _matrixDirty;

        /// <summary>创建打卡表窗口。</summary>
        /// <param name="service">报表聚合服务。</param>
        /// <param name="preferenceStore">按用户持久化的筛选偏好。</param>
        /// <param name="rootPath">初始根目录。</param>
        /// <param name="documents">阅读器清单（仅保留 .pdf）。</param>
        /// <param name="openDocument">双击文档行时由阅读器打开文件；为空则只提示路径。</param>
        /// <param name="logger">日志。</param>
        /// <param name="themeService">主题服务；为空时使用浅色主题。</param>
        /// <param name="exportService">导出与打印服务；为空时按默认配置创建。</param>
        public PdfReadingCheckInForm(
            PdfReadingCheckInService service,
            PdfReadingCheckInPreferenceStore preferenceStore,
            string rootPath,
            IEnumerable<string>? documents,
            Action<string>? openDocument = null,
            ILogger<PdfReadingCheckInForm>? logger = null,
            IThemeService? themeService = null,
            PdfReadingCheckInExportService? exportService = null)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _preferenceStore = preferenceStore ?? throw new ArgumentNullException(nameof(preferenceStore));
            _exportService = exportService ?? new PdfReadingCheckInExportService();
            _openDocument = openDocument;
            _logger = logger;
            _themeService = themeService;

            InitializeComponent();

            _initialRootPath = rootPath?.Trim() ?? string.Empty;
            _initialDocuments = NormalizeDocuments(documents);
            _rootPath = _initialRootPath;
            _documents = _initialDocuments;
            _preferences = _preferenceStore.Load();
            _month = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            _refreshTimer.Tick += RefreshTimer_Tick;

            WireEvents();
            ApplyPreferencesToControls();
            UpdateRootDisplay();
            UpdateMonthDisplay();
            ApplyTheme(_themeService?.CurrentColors ?? ThemeService.GetColors(ThemeMode.Light));
            _themeService?.RegisterThemeable(this);

            Shown += (_, _) => StartRefresh();
        }

        #region 初始化与主题

        private void WireEvents()
        {
            buttonChooseRoot.Click += (_, _) => ChooseRootDirectory();
            buttonUseCurrent.Click += (_, _) => UseInitialRoot();
            buttonPreviousMonth.Click += (_, _) => ShiftMonth(-1);
            buttonNextMonth.Click += (_, _) => ShiftMonth(1);
            buttonThisMonth.Click += (_, _) => SetMonth(CurrentMonth);
            buttonRefresh.Click += (_, _) =>
            {
                _refreshTimer.Stop();
                StartRefresh();
            };
            buttonClose.Click += (_, _) => Close();
            buttonExportExcel.Click += async (_, _) => await ExportExcelAsync();
            buttonExportHtml.Click += async (_, _) => await ExportHtmlAsync();
            buttonPrint.Click += (_, _) => ShowPrintPreview();

            numericThreshold.ValueChanged += (_, _) => OnFilterChanged();
            comboScope.SelectedIndexChanged += (_, _) => OnFilterChanged();
            comboDisplayMode.SelectedIndexChanged += (_, _) =>
            {
                OnFilterChanged();
                // 显示模式只影响单元格内容与配色，旧报表可立即重绘，无需等待重新聚合
                if (_report != null)
                    RenderMatrixIfVisible();
            };

            tabMain.SelectedIndexChanged += (_, _) =>
            {
                if (_matrixDirty && tabMain.SelectedTab == tabMatrix)
                    RenderMatrix();
            };

            gridSummary.CellDoubleClick += Grid_CellDoubleClick;
            gridMatrix.CellDoubleClick += Grid_CellDoubleClick;
            gridMatrix.CellFormatting += GridMatrix_CellFormatting;
            gridMatrix.CellPainting += GridMatrix_CellPainting;
            gridSummary.CellPainting += GridSummary_CellPainting;
        }

        private void ApplyPreferencesToControls()
        {
            _suppressFilterEvents = true;
            try
            {
                numericThreshold.Value = Math.Clamp(
                    _preferences.MinSeconds,
                    (int)numericThreshold.Minimum,
                    (int)numericThreshold.Maximum);
                comboScope.SelectedIndex = _preferences.DisplayScope switch
                {
                    PdfCheckInDisplayScope.All => 1,
                    PdfCheckInDisplayScope.AllIncludingMissing => 2,
                    _ => 0
                };
                comboDisplayMode.SelectedIndex =
                    _preferences.DisplayMode == PdfCheckInCellDisplayMode.Minutes ? 1 : 0;
            }
            finally
            {
                _suppressFilterEvents = false;
            }
        }

        /// <summary>应用主题色；网格与目录行同步刷新。</summary>
        public void ApplyTheme(ThemeColors colors)
        {
            if (colors == null)
                return;

            _colors = colors;
            BackColor = colors.Background;
            ForeColor = colors.TextPrimary;

            panelTop.BackColor = colors.Surface;
            panelBottom.BackColor = colors.Surface;
            tabMain.BackColor = colors.Background;
            tabSummary.BackColor = colors.Background;
            tabMatrix.BackColor = colors.Background;

            foreach (var label in new[] { labelRoot, labelMonth, labelThreshold, labelThresholdUnit, labelScope, labelDisplayMode })
                label.ForeColor = colors.TextSecondary;

            labelMonthValue.ForeColor = colors.TextPrimary;
            labelMetrics.ForeColor = colors.Primary;
            labelBanner.BackColor = colors.ErrorLight;
            labelBanner.ForeColor = colors.Error;
            labelStatus.ForeColor = colors.TextSecondary;
            labelSummaryEmpty.ForeColor = colors.TextDisabled;
            labelMatrixEmpty.ForeColor = colors.TextDisabled;

            textBoxRoot.BackColor = colors.SurfaceElevated;
            textBoxRoot.ForeColor = colors.TextPrimary;
            numericThreshold.BackColor = colors.SurfaceElevated;
            numericThreshold.ForeColor = colors.TextPrimary;
            comboScope.BackColor = colors.SurfaceElevated;
            comboScope.ForeColor = colors.TextPrimary;
            comboDisplayMode.BackColor = colors.SurfaceElevated;
            comboDisplayMode.ForeColor = colors.TextPrimary;
            comboPrintScope.BackColor = colors.SurfaceElevated;
            comboPrintScope.ForeColor = colors.TextPrimary;

            ApplyGridTheme(gridSummary);
            ApplyGridTheme(gridMatrix);
            RenderReport();
        }

        private void ApplyGridTheme(DataGridView grid)
        {
            grid.BackgroundColor = _colors.Background;
            grid.GridColor = _colors.Divider;
            grid.DefaultCellStyle.BackColor = _colors.Background;
            grid.DefaultCellStyle.ForeColor = _colors.TextPrimary;
            grid.DefaultCellStyle.SelectionBackColor = _colors.InfoLight;
            grid.DefaultCellStyle.SelectionForeColor = _colors.TextPrimary;
            grid.ColumnHeadersDefaultCellStyle.BackColor = _colors.SurfaceElevated;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = _colors.TextPrimary;
            grid.EnableHeadersVisualStyles = false;
        }

        /// <summary>按当前 DPI 缩放运行时设置的像素尺寸，保证 125% / 150% 下行高与字号匹配。</summary>
        private int ScaleForDpi(int logicalPixels)
            => (int)Math.Round(logicalPixels * DeviceDpi / 96.0, MidpointRounding.AwayFromZero);

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _themeService?.UnregisterThemeable(this);

                try
                {
                    _refreshTimer.Stop();
                    _refreshTimer.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // 计时器已随窗体释放
                }

                _refreshCts?.Cancel();
                _refreshCts?.Dispose();
                _refreshCts = null;

                _folderRowFont.Dispose();
                _streakFont.Dispose();
                _smallFont.Dispose();
                toolTipMain?.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region 筛选与刷新

        private static DateTime CurrentMonth => new(DateTime.Now.Year, DateTime.Now.Month, 1);

        private void OnFilterChanged()
        {
            if (_suppressFilterEvents)
                return;

            CapturePreferences();
            _preferenceStore.Save(_preferences);
            ScheduleRefresh();
        }

        private void ScheduleRefresh()
        {
            _refreshTimer.Stop();
            _refreshTimer.Start();
        }

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            _refreshTimer.Stop();
            StartRefresh();
        }

        private void CapturePreferences()
        {
            _preferences.MinSeconds = (int)numericThreshold.Value;
            _preferences.DisplayScope = comboScope.SelectedIndex switch
            {
                1 => PdfCheckInDisplayScope.All,
                2 => PdfCheckInDisplayScope.AllIncludingMissing,
                _ => PdfCheckInDisplayScope.RecordedOnly
            };
            _preferences.DisplayMode = comboDisplayMode.SelectedIndex == 1
                ? PdfCheckInCellDisplayMode.Minutes
                : PdfCheckInCellDisplayMode.CheckMark;
        }

        private PdfReadingCheckInRequest BuildRequest()
        {
            bool includeUnread;
            bool includeMissing;
            switch (_preferences.DisplayScope)
            {
                case PdfCheckInDisplayScope.All:
                    (includeUnread, includeMissing) = (true, false);
                    break;
                case PdfCheckInDisplayScope.AllIncludingMissing:
                    (includeUnread, includeMissing) = (true, true);
                    break;
                default:
                    (includeUnread, includeMissing) = (false, false);
                    break;
            }

            return new PdfReadingCheckInRequest
            {
                RootPath = _rootPath,
                Documents = _documents,
                PeriodStart = _month,
                PeriodEnd = _month.AddMonths(1).AddDays(-1),
                MinSeconds = _preferences.MinSeconds,
                IncludeUnread = includeUnread,
                IncludeMissing = includeMissing
            };
        }

        private void StartRefresh()
        {
            _refreshCts?.Cancel();
            _refreshCts?.Dispose();
            var cts = new CancellationTokenSource();
            _refreshCts = cts;

            SetRefreshing(true);
            _ = RefreshAsync(BuildRequest(), cts.Token);
        }

        private async Task RefreshAsync(PdfReadingCheckInRequest request, CancellationToken token)
        {
            try
            {
                var report = await Task.Run(() => _service.Build(request), token).ConfigureAwait(true);
                if (token.IsCancellationRequested || _disposed || IsDisposed || !IsHandleCreated)
                    return;

                // 仅接受最新一次刷新的结果，避免旧数据回填
                if (_refreshCts?.Token != token || report.PeriodStart != _month)
                    return;

                _report = report;
                RenderReport();
                UpdateMetrics();
                UpdateBanner();
                UpdateEmptyState();
                UpdateStatusText();
                UpdateMonthDisplay();
                UpdateActionAvailability();
            }
            catch (OperationCanceledException)
            {
                // 已切换到新的筛选条件，丢弃本次结果
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成阅读打卡表失败");
                if (_refreshCts?.Token == token && !_disposed)
                {
                    SetStatus($"统计失败：{ex.Message}");
                    if (_report == null)
                    {
                        labelMetrics.Text = "统计失败";
                        ShowEmptyHint($"统计失败：{ex.Message}");
                    }

                    UpdateActionAvailability();
                }
            }
            finally
            {
                if (_refreshCts?.Token == token && !_disposed)
                    SetRefreshing(false);
            }
        }

        private void SetRefreshing(bool refreshing)
        {
            _refreshing = refreshing;
            progressBar.Visible = refreshing;
            UpdateActionAvailability();

            if (refreshing && _report == null)
            {
                labelMetrics.Text = "正在统计…";
                ShowEmptyHint("正在统计…");
            }
        }

        private void ChooseRootDirectory()
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "选择要统计的根目录（仅统计 PDF，图片不参与）",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };
            if (Directory.Exists(_rootPath))
                dialog.SelectedPath = _rootPath;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            var (files, truncated) = EnumeratePdfs(dialog.SelectedPath);
            SetRoot(
                dialog.SelectedPath,
                files,
                truncated ? $"当前目录 PDF 超过 {MaxEnumeratedPdfs} 份，仅统计前 {MaxEnumeratedPdfs} 份" : string.Empty);
        }

        private void UseInitialRoot()
        {
            if (string.IsNullOrEmpty(_initialRootPath))
            {
                SetStatus("阅读器尚未打开目录或文档");
                return;
            }

            SetRoot(_initialRootPath, _initialDocuments, string.Empty);
        }

        private void SetRoot(string rootPath, IEnumerable<string>? documents, string limitNote)
        {
            _rootPath = rootPath?.Trim() ?? string.Empty;
            _documents = NormalizeDocuments(documents);
            _documentLimitNote = limitNote ?? string.Empty;
            UpdateRootDisplay();
            UpdateStatusText();
            StartRefresh();
        }

        private static (List<string> Files, bool Truncated) EnumeratePdfs(string rootPath)
        {
            var files = new List<string>();
            try
            {
                var options = new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    MatchCasing = MatchCasing.CaseInsensitive
                };
                foreach (string file in Directory.EnumerateFiles(rootPath, "*.pdf", options))
                {
                    if (files.Count >= MaxEnumeratedPdfs)
                        return (files, true);
                    files.Add(file);
                }
            }
            catch (Exception)
            {
                // 目录不可读时按空清单处理，界面会显示“根目录不可用”或空态
            }

            return (files, false);
        }

        private void ShiftMonth(int delta)
        {
            var target = _month.AddMonths(delta);
            if (delta > 0 && target > CurrentMonth)
                return;

            SetMonth(target);
        }

        private void SetMonth(DateTime month)
        {
            var target = new DateTime(month.Year, month.Month, 1);
            if (target > CurrentMonth)
                return;

            _month = target;
            UpdateMonthDisplay();
            StartRefresh();
        }

        private void UpdateMonthDisplay()
        {
            labelMonthValue.Text = _month.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            buttonNextMonth.Enabled = _month < CurrentMonth;
            buttonThisMonth.Enabled = _month != CurrentMonth;
        }

        private void UpdateRootDisplay()
        {
            string text = string.IsNullOrEmpty(_rootPath) ? "（尚未选择目录）" : _rootPath;
            textBoxRoot.Text = text;
            toolTipMain.SetToolTip(textBoxRoot, text);
            toolTipMain.SetToolTip(buttonUseCurrent, _initialRootPath.Length == 0
                ? "阅读器尚未打开目录或文档"
                : $"回到阅读器当前目录：{_initialRootPath}");
        }

        #endregion

        #region 汇总页渲染

        private void RenderReport()
        {
            if (_report == null)
                return;

            RenderSummary();
            RenderMatrixIfVisible();
        }

        /// <summary>矩阵页不在前台时只标记待渲染，避免在汇总页等待两页同时构建。</summary>
        private void RenderMatrixIfVisible()
        {
            if (tabMain.SelectedTab == tabMatrix)
            {
                RenderMatrix();
                _matrixDirty = false;
            }
            else
            {
                _matrixDirty = true;
            }
        }

        private void RenderSummary()
        {
            var rows = new List<DataGridViewRow>();
            foreach (var folder in _report!.Folders)
            {
                rows.Add(CreateSummaryFolderRow(folder));
                if (!IsFolderExpanded(folder.RelativePath))
                    continue;

                foreach (var document in folder.Documents)
                    rows.Add(CreateSummaryDocumentRow(folder, document));
            }

            gridSummary.SuspendLayout();
            gridSummary.Rows.Clear();
            if (rows.Count > 0)
                gridSummary.Rows.AddRange(rows.ToArray());
            gridSummary.ResumeLayout();
        }

        private DataGridViewRow CreateSummaryFolderRow(PdfCheckInFolderGroup folder)
        {
            var row = new DataGridViewRow
            {
                Height = ScaleForDpi(FolderRowHeight),
                Tag = new CheckInRowTag
                {
                    IsFolder = true,
                    FolderKey = folder.RelativePath,
                    DocumentCount = folder.DocumentCount
                }
            };
            row.CreateCells(gridSummary);

            row.Cells[0].Value = FormatFolderTitle(folder, includeCount: true);
            row.Cells[0].ToolTipText = GetFolderTooltip(folder);
            row.Cells[1].Value = folder.DocumentCount.ToString(CultureInfo.CurrentCulture);
            row.Cells[2].Value = folder.ReadDocumentCount.ToString(CultureInfo.CurrentCulture);
            row.Cells[3].Value = folder.CheckInDays > 0 ? $"{folder.CheckInDays} 天" : "—";
            row.Cells[4].Value = FormatDuration(folder.TotalSeconds);
            row.Cells[5].Value = FormatDay(folder.LastCheckInDay);
            row.Cells[6].Value = FormatStreak(folder.CurrentStreak);
            row.Cells[7].Value = string.Empty;
            row.Cells[8].Value = string.Empty;

            if (folder.CurrentStreak >= 7)
                row.Cells[SummaryStreakColumn].Style.Font = _streakFont;

            ApplyFolderRowStyle(row);
            return row;
        }

        private DataGridViewRow CreateSummaryDocumentRow(
            PdfCheckInFolderGroup folder,
            PdfCheckInDocumentRow document)
        {
            var row = new DataGridViewRow
            {
                Height = ScaleForDpi(DocumentRowHeight),
                Tag = new CheckInRowTag
                {
                    FolderKey = folder.RelativePath,
                    DocumentPath = document.FullPath,
                    Exists = document.Exists,
                    Coverage = document.Coverage,
                    DaySeconds = document.DaySeconds
                }
            };
            row.CreateCells(gridSummary);

            row.Cells[0].Value = FormatDocumentTitle(document);
            row.Cells[0].ToolTipText = document.FullPath;
            row.Cells[0].Style.Padding = new Padding(ScaleForDpi(SummaryDocumentIndent), 0, 0, 0);
            row.Cells[1].Value = string.Empty;
            row.Cells[2].Value = string.Empty;
            row.Cells[3].Value = document.CheckInDays > 0 ? $"{document.CheckInDays} 天" : "—";
            row.Cells[4].Value = FormatDuration(document.TotalSeconds);
            row.Cells[5].Value = FormatDay(document.LastCheckInDay);
            row.Cells[6].Value = FormatStreak(document.CurrentStreak);
            row.Cells[7].Value = FormatCoverage(document.Coverage);
            row.Cells[8].Value = FormatStatus(document.Status);

            if (document.CurrentStreak >= 7)
                row.Cells[SummaryStreakColumn].Style.Font = _streakFont;
            if (!document.Exists)
                row.DefaultCellStyle.ForeColor = _colors.TextDisabled;
            if (document.RenamedMatched)
                row.Cells[0].Style.ForeColor = _colors.Warning;

            return row;
        }

        private string GetFolderTooltip(PdfCheckInFolderGroup folder)
        {
            if (folder.RelativePath == PdfCheckInLayoutRules.MissingGroupKey)
                return "路径已失效，仅保留历史阅读记录";
            return folder.RelativePath.Length == 0
                ? _rootPath
                : Path.Combine(_rootPath, folder.RelativePath);
        }

        private void ApplyFolderRowStyle(DataGridViewRow row)
        {
            row.DefaultCellStyle.BackColor = _colors.Surface;
            row.DefaultCellStyle.ForeColor = _colors.TextPrimary;
            row.DefaultCellStyle.Font = _folderRowFont;
        }

        private static string FormatFolderTitle(PdfCheckInFolderGroup folder, bool includeCount)
        {
            string indent = folder.Depth > 0 ? new string('　', Math.Min(folder.Depth, 8)) : string.Empty;
            string title = string.IsNullOrEmpty(folder.RelativePath)
                ? PdfCheckInLayoutRules.RootFolderDisplay
                : folder.DisplayPath;
            string count = includeCount ? $"（{folder.DocumentCount} 本）" : string.Empty;
            return indent + title + count;
        }

        private static string FormatDocumentTitle(PdfCheckInDocumentRow document)
        {
            string suffix = document.RenamedMatched
                ? "（已重命名）"
                : !document.Exists
                    ? "（已失效）"
                    : string.Empty;
            return document.FileName + suffix;
        }

        private static string FormatDuration(int seconds)
            => PdfReadingTimeSnapshot.Format(TimeSpan.FromSeconds(Math.Max(0, seconds)));

        private static string FormatDay(DateTime? day)
            => day.HasValue ? day.Value.ToString("M-d", CultureInfo.InvariantCulture) : "—";

        private static string FormatStreak(int days)
            => days > 0 ? $"{days} 天" : "—";

        private static string FormatCoverage(double coverage)
        {
            if (coverage <= 0)
                return "0%";
            return $"{(int)Math.Round(Math.Clamp(coverage, 0, 1) * 100)}%";
        }

        private static string FormatStatus(PdfCheckInDocumentStatus status)
        {
            return status switch
            {
                PdfCheckInDocumentStatus.CheckedIn => "已打卡",
                PdfCheckInDocumentStatus.UnderThreshold => "有记录",
                _ => "未读"
            };
        }

        #endregion

        #region 矩阵页渲染

        private void RenderMatrix()
        {
            gridMatrix.SuspendLayout();
            // 先清空行再重建日期列，避免旧行与列数不一致
            gridMatrix.Rows.Clear();
            RebuildMatrixColumns();

            var rows = new List<DataGridViewRow>();
            foreach (var folder in _report!.Folders)
            {
                var folderDaySeconds = BuildFolderDaySeconds(folder);
                rows.Add(CreateMatrixFolderRow(folder, folderDaySeconds));
                if (!IsFolderExpanded(folder.RelativePath))
                    continue;

                foreach (var document in folder.Documents)
                    rows.Add(CreateMatrixDocumentRow(folder, document));
            }

            if (rows.Count > 0)
                gridMatrix.Rows.AddRange(rows.ToArray());
            gridMatrix.ResumeLayout();
        }

        private void RebuildMatrixColumns()
        {
            for (int index = gridMatrix.Columns.Count - 1; index >= MatrixFixedColumnCount; index--)
                gridMatrix.Columns.RemoveAt(index);

            _matrixColumns = _report?.Layout.Columns ?? Array.Empty<PdfCheckInMatrixColumn>();
            int dateColumnWidth = ScaleForDpi(MatrixDateColumnWidth);
            foreach (var column in _matrixColumns)
            {
                var gridColumn = new DataGridViewTextBoxColumn
                {
                    HeaderText = column.DayNumber.ToString(CultureInfo.InvariantCulture),
                    Width = dateColumnWidth,
                    MinimumWidth = dateColumnWidth,
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    ToolTipText = $"{column.Day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} 周{column.WeekdayLabel}"
                };
                gridColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                gridMatrix.Columns.Add(gridColumn);
            }
        }

        private static IReadOnlyDictionary<DateTime, int> BuildFolderDaySeconds(PdfCheckInFolderGroup folder)
        {
            var result = new Dictionary<DateTime, int>();
            foreach (var document in folder.Documents)
            {
                foreach (var pair in document.DaySeconds)
                {
                    result.TryGetValue(pair.Key, out int existing);
                    result[pair.Key] = existing + pair.Value;
                }
            }

            return result;
        }

        private DataGridViewRow CreateMatrixFolderRow(
            PdfCheckInFolderGroup folder,
            IReadOnlyDictionary<DateTime, int> folderDaySeconds)
        {
            var row = new DataGridViewRow
            {
                Height = ScaleForDpi(FolderRowHeight),
                Tag = new CheckInRowTag
                {
                    IsFolder = true,
                    FolderKey = folder.RelativePath,
                    DocumentCount = folder.DocumentCount,
                    DaySeconds = folderDaySeconds
                }
            };
            row.CreateCells(gridMatrix);
            row.Cells[0].Value = FormatFolderTitle(folder, includeCount: false);
            row.Cells[0].ToolTipText = GetFolderTooltip(folder);
            row.Cells[1].Value = $"{folder.DocumentCount} 本";
            row.Cells[2].Value = folder.CheckInDays > 0 ? folder.CheckInDays.ToString(CultureInfo.CurrentCulture) : "—";
            row.Cells[3].Value = FormatDuration(folder.TotalSeconds);
            row.Cells[4].Value = FormatStreak(folder.CurrentStreak);
            if (folder.CurrentStreak >= 7)
                row.Cells[MatrixStreakColumn].Style.Font = _streakFont;

            ApplyFolderRowStyle(row);
            return row;
        }

        private DataGridViewRow CreateMatrixDocumentRow(
            PdfCheckInFolderGroup folder,
            PdfCheckInDocumentRow document)
        {
            var row = new DataGridViewRow
            {
                Height = ScaleForDpi(DocumentRowHeight),
                Tag = new CheckInRowTag
                {
                    FolderKey = folder.RelativePath,
                    DocumentPath = document.FullPath,
                    Exists = document.Exists,
                    Coverage = document.Coverage,
                    DaySeconds = document.DaySeconds
                }
            };
            row.CreateCells(gridMatrix);
            row.Cells[0].Value = string.Empty;
            row.Cells[1].Value = FormatDocumentTitle(document);
            row.Cells[1].ToolTipText = document.FullPath;
            row.Cells[1].Style.Padding = new Padding(ScaleForDpi(MatrixDocumentIndent), 0, 0, 0);
            row.Cells[2].Value = document.CheckInDays > 0
                ? document.CheckInDays.ToString(CultureInfo.CurrentCulture)
                : "—";
            row.Cells[3].Value = FormatDuration(document.TotalSeconds);
            row.Cells[4].Value = FormatStreak(document.CurrentStreak);
            if (document.CurrentStreak >= 7)
                row.Cells[MatrixStreakColumn].Style.Font = _streakFont;
            if (!document.Exists)
                row.DefaultCellStyle.ForeColor = _colors.TextDisabled;
            if (document.RenamedMatched)
                row.Cells[1].Style.ForeColor = _colors.Warning;

            return row;
        }

        /// <summary>矩阵日期单元格：按显示模式输出勾选/分钟，并按分档着色。</summary>
        private void GridMatrix_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_report == null || e.RowIndex < 0 || e.ColumnIndex < MatrixFixedColumnCount)
                return;

            int dayIndex = e.ColumnIndex - MatrixFixedColumnCount;
            if (dayIndex >= _matrixColumns.Count)
                return;
            if (gridMatrix.Rows[e.RowIndex].Tag is not CheckInRowTag tag)
                return;

            var column = _matrixColumns[dayIndex];
            var cell = gridMatrix.Rows[e.RowIndex].Cells[e.ColumnIndex];
            var style = e.CellStyle;

            if (column.IsFuture)
            {
                e.Value = string.Empty;
                style.BackColor = FutureCellColor;
                style.ForeColor = _colors.TextDisabled;
                cell.ToolTipText = "未来日期";
                return;
            }

            tag.DaySeconds.TryGetValue(column.Day, out int seconds);
            if (seconds <= 0)
            {
                e.Value = string.Empty;
                style.BackColor = tag.IsFolder ? _colors.Surface : _colors.Background;
                style.ForeColor = tag.Exists ? _colors.TextPrimary : _colors.TextDisabled;
                cell.ToolTipText = tag.Exists ? string.Empty : "文件已失效";
                return;
            }

            bool checkedIn = seconds >= _report.MinSeconds;
            e.Value = _preferences.DisplayMode == PdfCheckInCellDisplayMode.CheckMark
                ? checkedIn ? "✓" : "·"
                : FormatMinuteCell(seconds);

            if (checkedIn)
            {
                var bucket = PdfCheckInLayoutRules.FindBucket(seconds);
                style.BackColor = bucket != null ? GetBucketColor(bucket) : _colors.SuccessLight;
                style.ForeColor = BucketTextColor;
            }
            else
            {
                style.BackColor = UnderThresholdCellColor;
                style.ForeColor = tag.Exists ? _colors.TextSecondary : _colors.TextDisabled;
            }

            cell.ToolTipText = BuildDayTooltip(tag, seconds, checkedIn);
        }

        /// <summary>矩阵表头（数字 + 星期、周末浅灰、今日描边）与未达标单元格虚线边框。</summary>
        private void GridMatrix_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex == -1 && e.ColumnIndex >= MatrixFixedColumnCount)
            {
                PaintDateHeader(e);
                return;
            }

            if (_report == null || e.RowIndex < 0 || e.ColumnIndex < MatrixFixedColumnCount)
                return;

            int dayIndex = e.ColumnIndex - MatrixFixedColumnCount;
            if (dayIndex >= _matrixColumns.Count)
                return;
            var column = _matrixColumns[dayIndex];
            if (column.IsFuture || gridMatrix.Rows[e.RowIndex].Tag is not CheckInRowTag tag)
                return;
            if (!tag.DaySeconds.TryGetValue(column.Day, out int seconds)
                || seconds <= 0
                || seconds >= _report.MinSeconds)
            {
                return;
            }

            // 未达阈值：数字模式下补一圈虚线框，提示“有记录但不算打卡”
            e.Paint(e.ClipBounds, DataGridViewPaintParts.All);
            using var pen = new Pen(_colors.TextDisabled) { DashStyle = DashStyle.Dot };
            var rect = new Rectangle(e.CellBounds.X, e.CellBounds.Y, e.CellBounds.Width - 1, e.CellBounds.Height - 1);
            e.Graphics.DrawRectangle(pen, rect);
            e.Handled = true;
        }

        /// <summary>汇总页覆盖率列：在百分比下方绘制细进度条。</summary>
        private void GridSummary_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != SummaryCoverageColumn)
                return;
            if (gridSummary.Rows[e.RowIndex].Tag is not CheckInRowTag tag || tag.IsFolder)
                return;

            e.Paint(e.ClipBounds, DataGridViewPaintParts.All);

            double coverage = Math.Clamp(tag.Coverage, 0, 1);
            var bounds = e.CellBounds;
            int inset = ScaleForDpi(CoverageBarInset);
            var barRect = new Rectangle(
                bounds.X + inset,
                bounds.Bottom - ScaleForDpi(CoverageBarBottomOffset),
                Math.Max(0, bounds.Width - inset * 2),
                ScaleForDpi(CoverageBarHeight));
            using (var track = new SolidBrush(_colors.Divider))
                e.Graphics.FillRectangle(track, barRect);
            int fillWidth = (int)Math.Round(barRect.Width * coverage);
            if (fillWidth > 0)
            {
                using var fill = new SolidBrush(_colors.Primary);
                e.Graphics.FillRectangle(fill, new Rectangle(barRect.X, barRect.Y, fillWidth, barRect.Height));
            }

            e.Handled = true;
        }

        private void PaintDateHeader(DataGridViewCellPaintingEventArgs e)
        {
            int dayIndex = e.ColumnIndex - MatrixFixedColumnCount;
            if (dayIndex >= _matrixColumns.Count)
                return;

            var column = _matrixColumns[dayIndex];
            var bounds = e.CellBounds;

            Color background = column.IsFuture
                ? FutureCellColor
                : column.IsWeekend
                    ? WeekendHeaderColor
                    : _colors.SurfaceElevated;
            using (var brush = new SolidBrush(background))
                e.Graphics.FillRectangle(brush, bounds);
            using (var pen = new Pen(_colors.Divider))
                e.Graphics.DrawLine(pen, bounds.Left, bounds.Bottom - 1, bounds.Right - 1, bounds.Bottom - 1);

            Color foreColor = column.IsFuture ? _colors.TextDisabled : _colors.TextPrimary;
            var topRect = new Rectangle(bounds.X, bounds.Y + 2, bounds.Width, bounds.Height / 2 - 2);
            var bottomRect = new Rectangle(bounds.X, bounds.Y + bounds.Height / 2, bounds.Width, bounds.Height / 2 - 3);
            TextRenderer.DrawText(
                e.Graphics,
                column.DayNumber.ToString(CultureInfo.InvariantCulture),
                gridMatrix.ColumnHeadersDefaultCellStyle.Font ?? Font,
                topRect,
                foreColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(
                e.Graphics,
                column.WeekdayLabel,
                _smallFont,
                bottomRect,
                column.IsWeekend ? _colors.TextSecondary : _colors.TextDisabled,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            if (column.Day == DateTime.Now.Date)
            {
                using var pen = new Pen(_colors.Primary, 1.6f);
                e.Graphics.DrawRectangle(pen, new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1));
            }

            e.Handled = true;
        }

        private string BuildDayTooltip(CheckInRowTag tag, int seconds, bool checkedIn)
        {
            string clock = FormatClock(seconds);
            if (!checkedIn)
            {
                int threshold = _report?.MinSeconds ?? _preferences.MinSeconds;
                return $"{clock}（未达打卡阈值 {threshold} 秒）";
            }

            return tag.IsFolder
                ? $"{tag.DocumentCount} 份 PDF · 当日 {clock}"
                : $"当日 {clock}";
        }

        private static string FormatMinuteCell(int seconds)
            => seconds < 60 ? "<1" : (seconds / 60).ToString(CultureInfo.CurrentCulture);

        private static string FormatClock(int seconds)
        {
            if (seconds < 60)
                return $"{seconds} 秒";

            int minutes = seconds / 60;
            int remainder = seconds % 60;
            return remainder == 0 ? $"{minutes} 分钟" : $"{minutes} 分 {remainder} 秒";
        }

        #endregion

        #region 交互、状态与空态

        private void Grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || sender is not DataGridView grid)
                return;
            if (grid.Rows[e.RowIndex].Tag is not CheckInRowTag tag)
                return;

            if (tag.IsFolder)
            {
                ToggleFolder(tag.FolderKey);
                return;
            }

            string? path = tag.DocumentPath;
            if (string.IsNullOrEmpty(path))
                return;

            if (!File.Exists(path))
            {
                MessageBox.Show(
                    this,
                    $"文件已失效：\n{path}",
                    "阅读打卡表",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (_openDocument == null)
            {
                MessageBox.Show(this, path, "文档路径", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                _openDocument(path);
                Close();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "从打卡表打开 PDF 失败");
                MessageBox.Show(
                    this,
                    $"打开文档失败：{ex.Message}",
                    "阅读打卡表",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void ToggleFolder(string folderKey)
        {
            int index = _preferences.ExpandedFolderKeys.FindIndex(
                key => string.Equals(key, folderKey, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
                _preferences.ExpandedFolderKeys.RemoveAt(index);
            else
                _preferences.ExpandedFolderKeys.Add(folderKey);

            _preferenceStore.Save(_preferences);

            int summaryOffset = TryGetFirstDisplayedRow(gridSummary);
            int matrixOffset = TryGetFirstDisplayedRow(gridMatrix);
            RenderReport();
            // 展开/折叠不改变目录行位置，按原偏移回到原处
            TryRestoreFirstDisplayedRow(gridSummary, summaryOffset);
            TryRestoreFirstDisplayedRow(gridMatrix, matrixOffset);
        }

        private bool IsFolderExpanded(string folderKey)
            => _preferences.ExpandedFolderKeys.Contains(folderKey, StringComparer.OrdinalIgnoreCase);

        private static int TryGetFirstDisplayedRow(DataGridView grid)
        {
            try
            {
                return grid.Rows.Count > 0 ? grid.FirstDisplayedScrollingRowIndex : -1;
            }
            catch (InvalidOperationException)
            {
                return -1;
            }
        }

        private static void TryRestoreFirstDisplayedRow(DataGridView grid, int offset)
        {
            if (offset < 0 || offset >= grid.Rows.Count)
                return;
            try
            {
                grid.FirstDisplayedScrollingRowIndex = offset;
            }
            catch (Exception)
            {
                // 行数变化后偏移失效时保持默认滚动位置
            }
        }

        private void UpdateMetrics()
        {
            if (_report == null)
                return;

            if (_report.RootUnavailable)
            {
                labelMetrics.Text = "根目录不可用 · 无法统计";
                return;
            }

            var totals = _report.Totals;
            string periodLabel = _month == CurrentMonth ? "本月打卡" : "该月打卡";
            labelMetrics.Text =
                $"总体连续 {Math.Max(0, totals.OverallCurrentStreak)} 天 · " +
                $"目录 {totals.FolderCount} 个 · PDF {totals.DocumentCount} 份 · " +
                $"{periodLabel} {totals.CheckInDays} 天";
        }

        private void UpdateBanner()
        {
            bool unavailable = _report?.RootUnavailable == true;
            labelBanner.Visible = unavailable;
            if (!unavailable)
                return;

            labelBanner.Text = string.IsNullOrEmpty(_rootPath)
                ? "尚未选择根目录，请点击「选择目录…」"
                : "根目录不可用，请检查磁盘是否连接后再选择目录";
        }

        private void UpdateEmptyState()
        {
            if (_report == null)
            {
                ShowEmptyHint(_refreshing ? "正在统计…" : "该月份暂无阅读记录");
                return;
            }

            if (_report.Folders.Count == 0)
            {
                string hint = _report.RootUnavailable
                    ? "根目录不可用，请检查磁盘是否连接"
                    : _preferences.DisplayScope == PdfCheckInDisplayScope.RecordedOnly
                        ? "该月份暂无阅读记录，试试切换月份或把显示范围改为“全部”"
                        : "该月份暂无阅读记录，试试切换月份";
                ShowEmptyHint(hint);
                return;
            }

            gridSummary.Visible = true;
            labelSummaryEmpty.Visible = false;
            gridMatrix.Visible = true;
            labelMatrixEmpty.Visible = false;
        }

        private void ShowEmptyHint(string text)
        {
            gridSummary.Visible = false;
            gridMatrix.Visible = false;
            labelSummaryEmpty.Text = text;
            labelMatrixEmpty.Text = text;
            labelSummaryEmpty.Visible = true;
            labelMatrixEmpty.Visible = true;
        }

        private void UpdateStatusText()
        {
            var parts = new List<string> { "仅统计阅读器前台停留时间", "图片不参与统计" };
            if (!string.IsNullOrEmpty(_documentLimitNote))
                parts.Add(_documentLimitNote);
            if (_report?.Totals.OutOfScopeDocumentCount > 0)
            {
                parts.Add($"另有 {_report.Totals.OutOfScopeDocumentCount} 份 PDF 不在当前目录，可切换根目录查看");
            }

            if (string.IsNullOrEmpty(_rootPath))
                parts.Add("尚未选择根目录");

            SetStatus(string.Join(" · ", parts));
        }

        #endregion

        #region 导出与打印

        /// <summary>状态栏统一入口：空态、不可用与导出结果都走同一处，避免各处措辞不一致。</summary>
        private void SetStatus(string text) => labelStatus.Text = text;

        private static string DescribeExportFailure(Exception exception)
        {
            return exception switch
            {
                UnauthorizedAccessException => "目标位置没有写入权限，请换一个目录后重试",
                DirectoryNotFoundException => "目标目录不存在，请重新选择保存位置",
                IOException => $"写入文件失败（文件可能被其他程序占用）：{exception.Message}",
                _ => exception.Message
            };
        }

        /// <summary>导出/打印前校验：报表可用且当前月份确实有内容。</summary>
        private bool HasExportableReport()
        {
            if (_report == null)
            {
                SetStatus("请等待统计完成后再导出");
                return false;
            }

            if (_report.RootUnavailable)
            {
                SetStatus("根目录不可用，无法导出");
                return false;
            }

            if (_report.Folders.Count == 0)
            {
                SetStatus("该月份暂无阅读记录，没有可导出的内容");
                return false;
            }

            return true;
        }

        private void UpdateActionAvailability()
        {
            bool ready = _report != null
                && !_report.RootUnavailable
                && _report.Folders.Count > 0
                && !_refreshing
                && !_exporting;

            buttonExportExcel.Enabled = ready;
            buttonExportHtml.Enabled = ready;
            buttonPrint.Enabled = ready;
            comboPrintScope.Enabled = ready;
        }

        /// <summary>导出期间切换等待光标并禁用入口，大目录导出（数千行）不阻塞界面。</summary>
        private void SetExporting(bool exporting, string? status = null)
        {
            _exporting = exporting;
            if (_disposed || IsDisposed)
                return;

            UseWaitCursor = exporting;
            if (status != null)
                SetStatus(status);
            UpdateActionAvailability();
        }

        private async Task ExportExcelAsync()
        {
            if (!HasExportableReport())
                return;

            // 导出使用当前报表快照，期间刷新筛选不会影响写出的内容
            var report = _report!;
            using var dialog = new SaveFileDialog
            {
                Filter = "Excel 工作簿 (*.xlsx)|*.xlsx",
                FileName = PdfReadingCheckInExportService.BuildFileName(report, "xlsx"),
                DefaultExt = "xlsx",
                Title = "导出阅读打卡表（Excel）",
                InitialDirectory = AppPaths.ExportsDir
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            SetExporting(true, "正在导出 Excel…");
            try
            {
                string target = dialog.FileName;
                string path = await Task.Run(() => _exportService.ExportExcel(
                    report,
                    target,
                    new PdfReadingCheckInExportOptions
                    {
                        UseRelativePaths = true,
                        GeneratedAt = DateTime.Now
                    }));

                if (_disposed || IsDisposed)
                    return;

                SetStatus($"已导出 Excel：{path}");
                MessageBox.Show(this, $"已导出到：\n{path}", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导出阅读打卡表 Excel 失败");
                if (_disposed || IsDisposed)
                    return;

                string reason = DescribeExportFailure(ex);
                SetStatus($"导出失败：{reason}");
                MessageBox.Show(this, $"导出失败：{reason}", "导出 Excel", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetExporting(false);
            }
        }

        private async Task ExportHtmlAsync()
        {
            if (!HasExportableReport())
                return;

            var report = _report!;
            using var dialog = new SaveFileDialog
            {
                Filter = "HTML 文件 (*.html)|*.html",
                FileName = PdfReadingCheckInExportService.BuildFileName(report, "html"),
                DefaultExt = "html",
                Title = "导出阅读打卡表（HTML）",
                InitialDirectory = AppPaths.ExportsDir
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            SetExporting(true, "正在导出 HTML…");
            try
            {
                string target = dialog.FileName;
                string path = await Task.Run(() => _exportService.ExportHtml(
                    report,
                    target,
                    new PdfReadingCheckInExportOptions
                    {
                        UseRelativePaths = true,
                        GeneratedAt = DateTime.Now
                    }));

                if (_disposed || IsDisposed)
                    return;

                SetStatus($"已导出 HTML：{path}");
                MessageBox.Show(this, $"已导出到：\n{path}", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导出阅读打卡表 HTML 失败");
                if (_disposed || IsDisposed)
                    return;

                string reason = DescribeExportFailure(ex);
                SetStatus($"导出失败：{reason}");
                MessageBox.Show(this, $"导出失败：{reason}", "导出 HTML", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetExporting(false);
            }
        }

        private PdfReadingCheckInPrintScope CurrentPrintScope => comboPrintScope.SelectedIndex switch
        {
            1 => PdfReadingCheckInPrintScope.SummaryOnly,
            2 => PdfReadingCheckInPrintScope.MatrixOnly,
            _ => PdfReadingCheckInPrintScope.All
        };

        private void ShowPrintPreview()
        {
            if (!HasExportableReport())
                return;

            var scope = CurrentPrintScope;
            try
            {
                using var printable = _exportService.CreatePrintDocument(
                    _report!,
                    new PdfReadingCheckInPrintOptions
                    {
                        Scope = scope,
                        DisplayMode = _preferences.DisplayMode,
                        UseRelativePaths = true,
                        GeneratedAt = DateTime.Now
                    });

                if (printable.PageCount == 0)
                {
                    MessageBox.Show(
                        this,
                        "当前打印范围没有可打印的页面。",
                        "打印预览",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                using var preview = new PrintPreviewDialog
                {
                    Document = printable.Document,
                    UseAntiAlias = true,
                    ShowIcon = false,
                    StartPosition = FormStartPosition.CenterParent,
                    Text = "打印预览 · 阅读打卡表",
                    Width = 1080,
                    Height = 760
                };
                preview.ShowDialog(this);
                SetStatus($"打印预览：{DescribeScope(scope)} · 共 {printable.PageCount} 页");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "打开阅读打卡表打印预览失败");
                string message = $"无法打开打印预览：{ex.Message}\n请确认已安装打印机后重试。";
                SetStatus("打印预览失败：请确认已安装打印机");
                MessageBox.Show(
                    this,
                    message,
                    "打印预览",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private static string DescribeScope(PdfReadingCheckInPrintScope scope)
            => scope switch
            {
                PdfReadingCheckInPrintScope.SummaryOnly => "仅目录汇总",
                PdfReadingCheckInPrintScope.MatrixOnly => "仅矩阵",
                _ => "全部"
            };

        #endregion

        #region 配色

        private Color BucketTextColor => _colors.ThemeMode == ThemeMode.Dark
            ? _colors.TextPrimary
            : Color.FromArgb(38, 50, 56);

        private Color WeekendHeaderColor => _colors.ThemeMode == ThemeMode.Dark
            ? _colors.Surface
            : Color.FromArgb(243, 244, 246);

        private Color FutureCellColor => _colors.ThemeMode == ThemeMode.Dark
            ? _colors.Surface
            : Color.FromArgb(238, 238, 238);

        private Color UnderThresholdCellColor => _colors.ThemeMode == ThemeMode.Dark
            ? _colors.SurfaceElevated
            : Color.FromArgb(247, 247, 247);

        private Color GetBucketColor(PdfCheckInColorBucket bucket)
        {
            Color baseColor = ColorTranslator.FromHtml(bucket.BackgroundColor);
            if (_colors.ThemeMode != ThemeMode.Dark)
                return baseColor;

            // 深色主题下把浅绿分档压暗，保证白字对比度
            return Blend(baseColor, _colors.Background, 0.55);
        }

        private static Color Blend(Color baseColor, Color overlay, double overlayWeight)
        {
            double weight = Math.Clamp(overlayWeight, 0, 1);
            int BlendChannel(int from, int to) => (int)Math.Round(from * (1 - weight) + to * weight);
            return Color.FromArgb(
                BlendChannel(baseColor.R, overlay.R),
                BlendChannel(baseColor.G, overlay.G),
                BlendChannel(baseColor.B, overlay.B));
        }

        #endregion

        #region 行标签

        /// <summary>表格行附加数据：目录行与文档行共用，供绘制与双击处理读取。</summary>
        private sealed class CheckInRowTag
        {
            public bool IsFolder { get; init; }
            public string FolderKey { get; init; } = string.Empty;
            public string? DocumentPath { get; init; }
            public bool Exists { get; init; } = true;
            public double Coverage { get; init; }
            public int DocumentCount { get; init; }
            public IReadOnlyDictionary<DateTime, int> DaySeconds { get; init; } = EmptyDaySeconds;
        }

        private static IReadOnlyList<string> NormalizeDocuments(IEnumerable<string>? documents)
        {
            if (documents == null)
                return Array.Empty<string>();

            return documents
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Trim())
                .Where(path => path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        #endregion
    }
}
