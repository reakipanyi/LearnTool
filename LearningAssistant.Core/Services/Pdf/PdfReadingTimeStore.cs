using System.Globalization;
using System.Text.Json;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>某份文档在某一天累计的阅读秒数。</summary>
    public sealed class PdfReadingTimeRecord
    {
        /// <summary>文档完整路径。</summary>
        public string DocPath { get; set; } = string.Empty;

        /// <summary>日期（本地时区，yyyy-MM-dd）。</summary>
        public string Day { get; set; } = string.Empty;

        /// <summary>当日在该文档上累计的秒数。</summary>
        public int Seconds { get; set; }
    }

    /// <summary>阅读时长汇总，供统计弹窗展示。</summary>
    public sealed class PdfReadingTimeSnapshot
    {
        /// <summary>今日阅读时长。</summary>
        public TimeSpan Today { get; init; }

        /// <summary>近 7 天阅读时长。</summary>
        public TimeSpan LastSevenDays { get; init; }

        /// <summary>本文档累计阅读时长。</summary>
        public TimeSpan Document { get; init; }

        /// <summary>累计阅读时长。</summary>
        public TimeSpan Total { get; init; }

        /// <summary>把时长格式化成易读文本。</summary>
        public static string Format(TimeSpan value)
        {
            if (value < TimeSpan.FromMinutes(1))
                return "不足 1 分钟";

            int hours = (int)value.TotalHours;
            int minutes = value.Minutes;
            if (hours <= 0)
                return $"{minutes} 分钟";
            if (minutes <= 0)
                return $"{hours} 小时";
            return $"{hours} 小时 {minutes} 分钟";
        }
    }

    /// <summary>
    /// 阅读时长累计：按“文档路径 + 日期”记账，单文件 JSON 持久化。
    /// 只记录时长，不参与阅读渲染；读写失败时静默忽略，不影响阅读。
    /// 目录按当前用户懒解析并按目录缓存，单例实例可安全跨用户复用。
    /// </summary>
    public sealed class PdfReadingTimeStore
    {
        private const string FileName = "reading_time.json";

        /// <summary>留存天数：约两年，足以支撑月度打卡表。</summary>
        private const int MaxRetentionDays = 730;

        /// <summary>硬上限：防止极端使用下文件无限增长。</summary>
        private const int MaxRecords = 20000;

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

        private readonly Func<string> _directoryProvider;
        private readonly object _gate = new();
        private readonly Dictionary<string, List<PdfReadingTimeRecord>> _caches =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>按当前用户目录懒解析；切换用户后自动读写对应目录。</summary>
        public PdfReadingTimeStore(Func<string> directoryProvider)
        {
            _directoryProvider = directoryProvider ?? throw new ArgumentNullException(nameof(directoryProvider));
        }

        /// <summary>固定目录：测试与单用户场景使用。</summary>
        public PdfReadingTimeStore(string directoryPath)
            : this(CreateFixedProvider(directoryPath))
        {
        }

        private static Func<string> CreateFixedProvider(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("阅读时长目录不能为空", nameof(directoryPath));

            string fixedPath = directoryPath;
            return () => fixedPath;
        }

        /// <summary>读取指定日期区间的明细记录（返回副本，供打卡表聚合使用）。</summary>
        public IReadOnlyList<PdfReadingTimeRecord> GetRecords(DateTime? fromDay = null, DateTime? toDay = null)
        {
            string directory = ResolveDirectory();
            bool hasFrom = fromDay.HasValue;
            bool hasTo = toDay.HasValue;
            DateTime from = (fromDay ?? DateTime.MinValue).Date;
            DateTime to = (toDay ?? DateTime.MaxValue).Date;

            lock (_gate)
            {
                var records = GetRecordsCore(directory);
                var result = new List<PdfReadingTimeRecord>(records.Count);
                foreach (var record in records)
                {
                    if (hasFrom || hasTo)
                    {
                        if (!TryParseDay(record.Day, out var day))
                            continue;
                        if (hasFrom && day < from)
                            continue;
                        if (hasTo && day > to)
                            continue;
                    }

                    result.Add(new PdfReadingTimeRecord
                    {
                        DocPath = record.DocPath,
                        Day = record.Day,
                        Seconds = record.Seconds
                    });
                }

                result.Sort((a, b) =>
                {
                    int byDay = string.CompareOrdinal(a.Day, b.Day);
                    return byDay != 0
                        ? byDay
                        : string.Compare(a.DocPath, b.DocPath, StringComparison.OrdinalIgnoreCase);
                });
                return result;
            }
        }

        /// <summary>累加指定文档的阅读秒数；seconds 小于等于 0 时忽略。</summary>
        public void Add(string? pdfPath, int seconds, DateTime? when = null)
        {
            if (string.IsNullOrWhiteSpace(pdfPath) || seconds <= 0)
                return;

            string directory = ResolveDirectory();
            string docPath = NormalizePath(pdfPath!);
            string day = (when ?? DateTime.Now).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            lock (_gate)
            {
                var records = GetRecordsCore(directory);
                var record = records.FirstOrDefault(r =>
                    r.Day == day && string.Equals(r.DocPath, docPath, StringComparison.OrdinalIgnoreCase));
                if (record == null)
                {
                    record = new PdfReadingTimeRecord { DocPath = docPath, Day = day };
                    records.Add(record);
                }

                record.Seconds += seconds;
                Trim(records, DateTime.Now.Date);
                Persist(directory, records);
            }
        }

        /// <summary>读取今日 / 近 7 天 / 本文档 / 累计四项汇总。</summary>
        public PdfReadingTimeSnapshot GetSnapshot(string? pdfPath, DateTime? when = null)
        {
            var now = when ?? DateTime.Now;
            string today = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            DateTime weekStart = now.Date.AddDays(-6);
            string docPath = string.IsNullOrWhiteSpace(pdfPath) ? string.Empty : NormalizePath(pdfPath!);
            string directory = ResolveDirectory();

            double todaySeconds = 0;
            double weekSeconds = 0;
            double documentSeconds = 0;
            double totalSeconds = 0;

            lock (_gate)
            {
                foreach (var record in GetRecordsCore(directory))
                {
                    totalSeconds += record.Seconds;

                    if (record.Day == today)
                        todaySeconds += record.Seconds;

                    if (TryParseDay(record.Day, out var day) && day >= weekStart && day <= now.Date)
                        weekSeconds += record.Seconds;

                    if (docPath.Length > 0
                        && string.Equals(record.DocPath, docPath, StringComparison.OrdinalIgnoreCase))
                    {
                        documentSeconds += record.Seconds;
                    }
                }
            }

            return new PdfReadingTimeSnapshot
            {
                Today = TimeSpan.FromSeconds(todaySeconds),
                LastSevenDays = TimeSpan.FromSeconds(weekSeconds),
                Document = TimeSpan.FromSeconds(documentSeconds),
                Total = TimeSpan.FromSeconds(totalSeconds)
            };
        }

        private string ResolveDirectory()
        {
            string directory = _directoryProvider();
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("阅读时长目录未配置");

            return directory;
        }

        /// <summary>按目录取缓存；首次加载时顺带裁剪，确实裁剪则回写文件。</summary>
        private List<PdfReadingTimeRecord> GetRecordsCore(string directory)
        {
            if (_caches.TryGetValue(directory, out var cached))
                return cached;

            var records = new List<PdfReadingTimeRecord>();
            try
            {
                string file = Path.Combine(directory, FileName);
                if (File.Exists(file))
                {
                    var loaded = JsonSerializer.Deserialize<List<PdfReadingTimeRecord>>(File.ReadAllText(file));
                    if (loaded != null)
                    {
                        records = loaded
                            .Where(r => r != null && r.Seconds > 0 && !string.IsNullOrWhiteSpace(r.DocPath))
                            .ToList();
                    }
                }
            }
            catch (Exception)
            {
                // 统计只是辅助信息，读取失败按空账本处理
                records = new List<PdfReadingTimeRecord>();
            }

            if (Trim(records, DateTime.Now.Date))
            {
                // 历史数据超过留存范围时顺手回写；失败仅忽略
                Persist(directory, records);
            }

            _caches[directory] = records;
            return records;
        }

        private void Persist(string directory, List<PdfReadingTimeRecord> records)
        {
            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(
                    Path.Combine(directory, FileName),
                    JsonSerializer.Serialize(records, JsonOptions));
            }
            catch (Exception)
            {
                // 写入失败不影响阅读
            }
        }

        /// <summary>按天数与条数裁剪，返回是否发生裁剪。</summary>
        private static bool Trim(List<PdfReadingTimeRecord> records, DateTime today)
        {
            if (records.Count == 0)
                return false;

            bool changed = false;
            DateTime cutoff = today.AddDays(-MaxRetentionDays);
            var expired = records
                .Where(r => TryParseDay(r.Day, out var day) && day < cutoff)
                .ToList();
            if (expired.Count > 0)
            {
                foreach (var record in expired)
                    records.Remove(record);
                changed = true;
            }

            if (records.Count > MaxRecords)
            {
                records.Sort((a, b) => string.CompareOrdinal(a.Day, b.Day));
                records.RemoveRange(0, records.Count - MaxRecords);
                changed = true;
            }

            return changed;
        }

        private static bool TryParseDay(string? day, out DateTime value)
            => DateTime.TryParseExact(
                day,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out value);

        private static string NormalizePath(string pdfPath)
        {
            try
            {
                return Path.GetFullPath(pdfPath);
            }
            catch (Exception)
            {
                return pdfPath;
            }
        }
    }
}
