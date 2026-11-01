namespace LearningAssistant.Models.Pdf
{
    /// <summary>打卡表生成请求。</summary>
    public sealed class PdfReadingCheckInRequest
    {
        /// <summary>分组根目录。</summary>
        public string RootPath { get; init; } = string.Empty;

        /// <summary>阅读器清单；内部只保留 .pdf，为空则按历史记录与目录枚举。</summary>
        public IReadOnlyList<string>? Documents { get; init; }

        /// <summary>统计起始日（含）。</summary>
        public DateTime PeriodStart { get; init; }

        /// <summary>统计结束日（含），内部夹取到不晚于今天。</summary>
        public DateTime PeriodEnd { get; init; }

        /// <summary>有效打卡阈值（秒）。</summary>
        public int MinSeconds { get; init; } = 60;

        /// <summary>是否包含区间内没有阅读记录的 PDF；默认 false = 仅有记录。</summary>
        public bool IncludeUnread { get; init; }

        /// <summary>是否包含已失效文件；默认 true。</summary>
        public bool IncludeMissing { get; init; } = true;
    }

    /// <summary>PDF 在统计区间内的打卡状态。</summary>
    public enum PdfCheckInDocumentStatus
    {
        /// <summary>区间内无任何阅读秒数。</summary>
        Unread,

        /// <summary>有阅读秒数但没有任何一天达到阈值。</summary>
        UnderThreshold,

        /// <summary>至少有一天达到阈值。</summary>
        CheckedIn
    }

    /// <summary>打卡表报表（纯数据，不含 UI 依赖）。</summary>
    public sealed class PdfReadingCheckInReport
    {
        /// <summary>分组根目录（绝对路径）。</summary>
        public string RootPath { get; init; } = string.Empty;

        /// <summary>实际统计起始日。</summary>
        public DateTime PeriodStart { get; init; }

        /// <summary>实际统计结束日（不晚于今天）。</summary>
        public DateTime PeriodEnd { get; init; }

        /// <summary>有效打卡阈值（秒）。</summary>
        public int MinSeconds { get; init; }

        /// <summary>矩阵列（逐日）。</summary>
        public IReadOnlyList<DateTime> Days { get; init; } = Array.Empty<DateTime>();

        /// <summary>目录分组。</summary>
        public IReadOnlyList<PdfCheckInFolderGroup> Folders { get; init; } = Array.Empty<PdfCheckInFolderGroup>();

        /// <summary>合计指标。</summary>
        public PdfCheckInTotals Totals { get; init; } = new();

        /// <summary>列宽、颜色分档与分页计划，界面、打印、HTML 共用。</summary>
        public PdfCheckInLayoutModel Layout { get; init; } = new();

        /// <summary>根目录不存在（如移动磁盘未连接）时为 true，界面应提示并禁用导出。</summary>
        public bool RootUnavailable { get; init; }
    }

    /// <summary>按相对目录分组的打卡汇总。</summary>
    public sealed class PdfCheckInFolderGroup
    {
        /// <summary>相对根目录的路径；空串表示根层。</summary>
        public string RelativePath { get; init; } = string.Empty;

        /// <summary>相对路径展示文本；根层为“（根目录）”。</summary>
        public string DisplayPath { get; init; } = string.Empty;

        /// <summary>相对路径层级，根层为 0。</summary>
        public int Depth { get; init; }

        /// <summary>组内 PDF 行。</summary>
        public IReadOnlyList<PdfCheckInDocumentRow> Documents { get; init; } = Array.Empty<PdfCheckInDocumentRow>();

        /// <summary>组内 PDF 数量。</summary>
        public int DocumentCount { get; init; }

        /// <summary>区间内有阅读记录的 PDF 数量（含未达标）。</summary>
        public int ReadDocumentCount { get; init; }

        /// <summary>组内任一 PDF 达到阈值的日期去重数。</summary>
        public int CheckInDays { get; init; }

        /// <summary>组内阅读秒数合计。</summary>
        public int TotalSeconds { get; init; }

        /// <summary>组内最近一个打卡日。</summary>
        public DateTime? LastCheckInDay { get; init; }

        /// <summary>组内当前连续打卡天数（全量历史回溯）。</summary>
        public int CurrentStreak { get; init; }
    }

    /// <summary>单个 PDF 的打卡明细。</summary>
    public sealed class PdfCheckInDocumentRow
    {
        /// <summary>真实文件路径（重命名命中时为匹配到的路径）。</summary>
        public string FullPath { get; init; } = string.Empty;

        /// <summary>文件名。</summary>
        public string FileName { get; init; } = string.Empty;

        /// <summary>磁盘文件是否存在。</summary>
        public bool Exists { get; init; }

        /// <summary>原路径失效后通过唯一匹配找到真实文件。</summary>
        public bool RenamedMatched { get; init; }

        /// <summary>区间内打卡状态。</summary>
        public PdfCheckInDocumentStatus Status { get; init; }

        /// <summary>区间内逐日秒数（仅含有秒数的日期）。</summary>
        public IReadOnlyDictionary<DateTime, int> DaySeconds { get; init; } =
            new Dictionary<DateTime, int>();

        /// <summary>区间内达标天数（同日多次累计不重复计天）。</summary>
        public int CheckInDays { get; init; }

        /// <summary>区间内阅读秒数合计（含未达标零碎时长）。</summary>
        public int TotalSeconds { get; init; }

        /// <summary>区间内最近一个达标日。</summary>
        public DateTime? LastCheckInDay { get; init; }

        /// <summary>当前连续打卡天数（全量历史回溯）。</summary>
        public int CurrentStreak { get; init; }

        /// <summary>覆盖率：达标天数 / 已过天数，0-1；已过天数为 0 时为 0。</summary>
        public double Coverage { get; init; }
    }

    /// <summary>报表合计指标。</summary>
    public sealed class PdfCheckInTotals
    {
        /// <summary>目录分组数。</summary>
        public int FolderCount { get; init; }

        /// <summary>PDF 总数。</summary>
        public int DocumentCount { get; init; }

        /// <summary>区间内有阅读记录的 PDF 数。</summary>
        public int ReadDocumentCount { get; init; }

        /// <summary>根目录内任一 PDF 达标的日期去重数。</summary>
        public int CheckInDays { get; init; }

        /// <summary>阅读秒数合计。</summary>
        public int TotalSeconds { get; init; }

        /// <summary>目录级当前连续的最大值。</summary>
        public int BestStreak { get; init; }

        /// <summary>根目录内任一 PDF 达标的当前连续天数。</summary>
        public int OverallCurrentStreak { get; init; }

        /// <summary>根目录外有区间记录的 PDF 数（按路径去重）。</summary>
        public int OutOfScopeDocumentCount { get; init; }
    }

    /// <summary>矩阵单元格显示模式。</summary>
    public enum PdfCheckInCellDisplayMode
    {
        /// <summary>达标显示勾选。</summary>
        CheckMark,

        /// <summary>显示分钟数字。</summary>
        Minutes
    }

    /// <summary>打印分段模式。</summary>
    public enum PdfCheckInPrintSegmentMode
    {
        /// <summary>单页 31 列。</summary>
        SinglePage,

        /// <summary>按 1-15 / 16-31 分两段。</summary>
        TwoSegments
    }

    /// <summary>矩阵日期列。</summary>
    public sealed class PdfCheckInMatrixColumn
    {
        /// <summary>日期。</summary>
        public DateTime Day { get; init; }

        /// <summary>日号（1-31）。</summary>
        public int DayNumber { get; init; }

        /// <summary>星期展示文本（日 / 一 / … / 六）。</summary>
        public string WeekdayLabel { get; init; } = string.Empty;

        /// <summary>是否周六或周日。</summary>
        public bool IsWeekend { get; init; }

        /// <summary>是否晚于今天（未来日期浅灰且不可交互）。</summary>
        public bool IsFuture { get; init; }

        /// <summary>是否属于第二段（16 日及以后）。</summary>
        public bool InSecondSegment { get; init; }

        /// <summary>列宽（毫米）。</summary>
        public int WidthMm { get; init; }
    }

    /// <summary>打印/导出的页类型。</summary>
    public enum PdfCheckInPageKind
    {
        /// <summary>目录汇总页。</summary>
        Summary,

        /// <summary>打卡矩阵页。</summary>
        Matrix
    }

    /// <summary>一页中的一行：目录分组行或 PDF 明细行。</summary>
    public sealed class PdfCheckInPageRow
    {
        /// <summary>是否为目录分组行。</summary>
        public bool IsFolderRow { get; init; }

        /// <summary>目录相对路径；根层为空串，失效文件组为 "__missing__"。</summary>
        public string FolderRelativePath { get; init; } = string.Empty;

        /// <summary>目录展示文本。</summary>
        public string FolderDisplayPath { get; init; } = string.Empty;

        /// <summary>PDF 明细行的完整路径；目录行时为 null。</summary>
        public string? DocumentFullPath { get; init; }

        /// <summary>PDF 明细行的文件名；目录行时为 null。</summary>
        public string? DocumentFileName { get; init; }

        /// <summary>该行是否为跨页目录/文档的续行。</summary>
        public bool IsContinuation { get; init; }
    }

    /// <summary>“页 -> 行”打印计划，页数在绘制前即确定。</summary>
    public sealed class PdfCheckInPagePlan
    {
        /// <summary>页码（从 1 开始，覆盖全部页）。</summary>
        public int PageNumber { get; init; }

        /// <summary>页类型。</summary>
        public PdfCheckInPageKind Kind { get; init; }

        /// <summary>页标题，如“目录汇总”“打卡矩阵（1-15）”。</summary>
        public string Title { get; init; } = string.Empty;

        /// <summary>分段标签：空串 / "1-15" / "16-31"。</summary>
        public string SegmentLabel { get; init; } = string.Empty;

        /// <summary>本页行清单。</summary>
        public IReadOnlyList<PdfCheckInPageRow> Rows { get; init; } = Array.Empty<PdfCheckInPageRow>();

        /// <summary>是否在每页重复表头。</summary>
        public bool RepeatsHeader { get; init; } = true;
    }

    /// <summary>矩阵配色分档。</summary>
    public sealed class PdfCheckInColorBucket
    {
        /// <summary>图例文本。</summary>
        public string Label { get; init; } = string.Empty;

        /// <summary>本档下限秒数（含）。</summary>
        public int MinSeconds { get; init; }

        /// <summary>本档上限秒数（含）。</summary>
        public int MaxSeconds { get; init; }

        /// <summary>背景色（#RRGGBB）。</summary>
        public string BackgroundColor { get; init; } = string.Empty;
    }

    /// <summary>矩阵布局与配色：列宽、颜色分档、打印分段与“页 -> 行”计划。</summary>
    public sealed class PdfCheckInLayoutModel
    {
        /// <summary>日期列。</summary>
        public IReadOnlyList<PdfCheckInMatrixColumn> Columns { get; init; } = Array.Empty<PdfCheckInMatrixColumn>();

        /// <summary>打印页计划（汇总页在前，矩阵页在后）。</summary>
        public IReadOnlyList<PdfCheckInPagePlan> Pages { get; init; } = Array.Empty<PdfCheckInPagePlan>();

        /// <summary>日期单元格宽度（毫米）。</summary>
        public int CellWidthMm { get; init; }

        /// <summary>表格行高（毫米）。</summary>
        public int RowHeightMm { get; init; }

        /// <summary>打印分段模式。</summary>
        public PdfCheckInPrintSegmentMode SegmentMode { get; init; }

        /// <summary>颜色分档。</summary>
        public IReadOnlyList<PdfCheckInColorBucket> ColorBuckets { get; init; } = Array.Empty<PdfCheckInColorBucket>();
    }

    /// <summary>布局计算规则：列宽、分档、分页计划（界面/打印/HTML 共用）。</summary>
    public static class PdfCheckInLayoutRules
    {
        /// <summary>A4 横向扣除 10mm 边距后的可用宽度。</summary>
        public const int A4LandscapeUsableWidthMm = 277;

        /// <summary>A4 横向扣除 10mm 边距后的可用高度。</summary>
        public const int A4LandscapeUsableHeightMm = 190;

        /// <summary>5 个固定列（目录、文档、打卡、时长、连续）合计宽度。</summary>
        public const int FixedColumnsWidthMm = 90;

        /// <summary>日期列可用宽度 = 可用宽度 - 固定列。</summary>
        public const int DefaultDateColumnsWidthMm = A4LandscapeUsableWidthMm - FixedColumnsWidthMm;

        /// <summary>日期列可读宽度下限；低于该值自动分为两段。</summary>
        public const double MinReadableColumnWidthMm = 5.5;

        /// <summary>行高（毫米）。</summary>
        public const int DefaultRowHeightMm = 6;

        /// <summary>每页可容纳的行数（扣除标题、表头、页脚）。</summary>
        public const int RowsPerPage = 26;

        /// <summary>缺失文件保留组的相对路径键。</summary>
        public const string MissingGroupKey = "__missing__";

        /// <summary>根层目录展示文本。</summary>
        public const string RootFolderDisplay = "（根目录）";

        /// <summary>失效文件组展示文本。</summary>
        public const string MissingGroupDisplay = "已失效文件";

        /// <summary>构造布局模型（含列宽、分档与分页计划）。</summary>
        /// <param name="days">矩阵逐日列。</param>
        /// <param name="folders">已构建的目录分组。</summary>
        /// <param name="today">今天，用于标记未来列。</param>
        /// <param name="dateColumnsWidthMm">日期列可用总宽度（默认 A4 横向扣除固定列）。</param>
        public static PdfCheckInLayoutModel Create(
            IReadOnlyList<DateTime> days,
            IReadOnlyList<PdfCheckInFolderGroup> folders,
            DateTime today,
            int dateColumnsWidthMm = DefaultDateColumnsWidthMm)
        {
            int daysCount = days.Count;
            double singleWidth = daysCount > 0
                ? dateColumnsWidthMm / (double)daysCount
                : dateColumnsWidthMm;

            // 31 列单页放不下时按 1-15 / 16-31 分两段
            bool twoSegments = daysCount > 15 && singleWidth < MinReadableColumnWidthMm;
            int firstSegmentCount = days.Count(d => d.Day <= 15);
            int segmentColumnCount = twoSegments
                ? Math.Max(1, Math.Max(firstSegmentCount, daysCount - firstSegmentCount))
                : Math.Max(1, daysCount);
            double cellWidth = daysCount > 0 ? dateColumnsWidthMm / (double)segmentColumnCount : 0;

            var columns = days
                .Select(day => new PdfCheckInMatrixColumn
                {
                    Day = day.Date,
                    DayNumber = day.Day,
                    WeekdayLabel = GetWeekdayLabel(day.DayOfWeek),
                    IsWeekend = day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday,
                    IsFuture = day.Date > today.Date,
                    InSecondSegment = twoSegments && day.Day > 15,
                    WidthMm = (int)Math.Floor(cellWidth)
                })
                .ToList();

            var segmentModes = twoSegments
                ? new[] { (Label: "1-15", Second: false), (Label: "16-31", Second: true) }
                : new[] { (Label: string.Empty, Second: false) };

            var pages = new List<PdfCheckInPagePlan>();
            AddPageSequence(pages, PdfCheckInPageKind.Summary, "目录汇总", string.Empty, folders, RowsPerPage);
            foreach (var segment in segmentModes)
            {
                string title = twoSegments ? $"打卡矩阵（{segment.Label}）" : "打卡矩阵";
                AddPageSequence(pages, PdfCheckInPageKind.Matrix, title, segment.Label, folders, RowsPerPage);
            }

            return new PdfCheckInLayoutModel
            {
                Columns = columns,
                Pages = pages,
                CellWidthMm = (int)Math.Floor(cellWidth),
                RowHeightMm = DefaultRowHeightMm,
                SegmentMode = twoSegments ? PdfCheckInPrintSegmentMode.TwoSegments : PdfCheckInPrintSegmentMode.SinglePage,
                ColorBuckets = DefaultColorBuckets
            };
        }

        private static void AddPageSequence(
            List<PdfCheckInPagePlan> pages,
            PdfCheckInPageKind kind,
            string title,
            string segmentLabel,
            IReadOnlyList<PdfCheckInFolderGroup> folders,
            int rowsPerPage)
        {
            int capacity = Math.Max(1, rowsPerPage);
            var pageRows = new List<PdfCheckInPageRow>();
            bool markContinuation = false;

            void Flush()
            {
                if (pageRows.Count == 0)
                    return;

                pages.Add(new PdfCheckInPagePlan
                {
                    PageNumber = pages.Count + 1,
                    Kind = kind,
                    Title = title,
                    SegmentLabel = segmentLabel,
                    Rows = pageRows.ToList(),
                    RepeatsHeader = true
                });
                pageRows.Clear();
            }

            foreach (var folder in folders)
            {
                var block = new List<PdfCheckInPageRow>
                {
                    new()
                    {
                        IsFolderRow = true,
                        FolderRelativePath = folder.RelativePath,
                        FolderDisplayPath = folder.DisplayPath
                    }
                };

                foreach (var document in folder.Documents)
                {
                    block.Add(new PdfCheckInPageRow
                    {
                        FolderRelativePath = folder.RelativePath,
                        FolderDisplayPath = folder.DisplayPath,
                        DocumentFullPath = document.FullPath,
                        DocumentFileName = document.FileName
                    });
                }

                // 目录分组尽量不跨页
                if (pageRows.Count > 0 && pageRows.Count + block.Count > capacity)
                {
                    Flush();
                    markContinuation = false;
                }

                foreach (var row in block)
                {
                    if (pageRows.Count >= capacity)
                    {
                        Flush();
                        markContinuation = true;
                    }

                    pageRows.Add(markContinuation ? WithContinuation(row) : row);
                    markContinuation = false;
                }
            }

            Flush();
        }

        private static PdfCheckInPageRow WithContinuation(PdfCheckInPageRow row) => new()
        {
            IsFolderRow = row.IsFolderRow,
            FolderRelativePath = row.FolderRelativePath,
            FolderDisplayPath = row.FolderDisplayPath,
            DocumentFullPath = row.DocumentFullPath,
            DocumentFileName = row.DocumentFileName,
            IsContinuation = true
        };

        /// <summary>默认四档配色（10 / 30 / 60 分钟）。</summary>
        public static IReadOnlyList<PdfCheckInColorBucket> DefaultColorBuckets { get; } = new[]
        {
            new PdfCheckInColorBucket { Label = "<10 分钟", MinSeconds = 0, MaxSeconds = 599, BackgroundColor = "#F1F8E9" },
            new PdfCheckInColorBucket { Label = "10-30 分钟", MinSeconds = 600, MaxSeconds = 1799, BackgroundColor = "#DCEDC8" },
            new PdfCheckInColorBucket { Label = "30-60 分钟", MinSeconds = 1800, MaxSeconds = 3599, BackgroundColor = "#C5E1A5" },
            new PdfCheckInColorBucket { Label = "≥60 分钟", MinSeconds = 3600, MaxSeconds = int.MaxValue, BackgroundColor = "#AED581" }
        };

        /// <summary>按秒数取配色档；未达到阈值时返回 null（由调用方绘制“·”）。</summary>
        public static PdfCheckInColorBucket? FindBucket(int seconds)
        {
            if (seconds < 0)
                return null;

            foreach (var bucket in DefaultColorBuckets)
            {
                if (seconds >= bucket.MinSeconds && seconds <= bucket.MaxSeconds)
                    return bucket;
            }

            return null;
        }

        private static string GetWeekdayLabel(DayOfWeek dayOfWeek)
            => dayOfWeek switch
            {
                DayOfWeek.Monday => "一",
                DayOfWeek.Tuesday => "二",
                DayOfWeek.Wednesday => "三",
                DayOfWeek.Thursday => "四",
                DayOfWeek.Friday => "五",
                DayOfWeek.Saturday => "六",
                _ => "日"
            };
    }
}
