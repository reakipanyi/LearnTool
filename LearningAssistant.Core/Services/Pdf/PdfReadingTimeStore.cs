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
    /// </summary>
    public sealed class PdfReadingTimeStore
    {
        private const string FileName = "reading_time.json";
        private const int MaxRecords = 800;

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

        private readonly string _directory;
        private readonly object _gate = new();
        private List<PdfReadingTimeRecord>? _records;

        public PdfReadingTimeStore(string directoryPath)
        {
            _directory = string.IsNullOrWhiteSpace(directoryPath)
                ? throw new ArgumentException("阅读时长目录不能为空", nameof(directoryPath))
                : directoryPath;
        }

        /// <summary>累加指定文档的阅读秒数；seconds 小于等于 0 时忽略。</summary>
        public void Add(string? pdfPath, int seconds, DateTime? when = null)
        {
            if (string.IsNullOrWhiteSpace(pdfPath) || seconds <= 0)
                return;

            string docPath = NormalizePath(pdfPath!);
            string day = (when ?? DateTime.Now).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            lock (_gate)
            {
                var records = EnsureLoaded();
                var record = records.FirstOrDefault(r =>
                    r.Day == day && string.Equals(r.DocPath, docPath, StringComparison.OrdinalIgnoreCase));
                if (record == null)
                {
                    record = new PdfReadingTimeRecord { DocPath = docPath, Day = day };
                    records.Add(record);
                }

                record.Seconds += seconds;
                Trim(records);
                Persist(records);
            }
        }

        /// <summary>读取今日 / 近 7 天 / 本文档 / 累计四项汇总。</summary>
        public PdfReadingTimeSnapshot GetSnapshot(string? pdfPath, DateTime? when = null)
        {
            var now = when ?? DateTime.Now;
            string today = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            DateTime weekStart = now.Date.AddDays(-6);
            string docPath = string.IsNullOrWhiteSpace(pdfPath) ? string.Empty : NormalizePath(pdfPath!);

            double todaySeconds = 0;
            double weekSeconds = 0;
            double documentSeconds = 0;
            double totalSeconds = 0;

            lock (_gate)
            {
                foreach (var record in EnsureLoaded())
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

        private List<PdfReadingTimeRecord> EnsureLoaded()
        {
            if (_records != null)
                return _records;

            var records = new List<PdfReadingTimeRecord>();
            try
            {
                string file = Path.Combine(_directory, FileName);
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

            _records = records;
            return _records;
        }

        private void Persist(List<PdfReadingTimeRecord> records)
        {
            try
            {
                Directory.CreateDirectory(_directory);
                File.WriteAllText(
                    Path.Combine(_directory, FileName),
                    JsonSerializer.Serialize(records, JsonOptions));
            }
            catch (Exception)
            {
                // 写入失败不影响阅读
            }
        }

        /// <summary>只保留最近的若干条记录，避免长期使用后文件无限增长。</summary>
        private static void Trim(List<PdfReadingTimeRecord> records)
        {
            if (records.Count <= MaxRecords)
                return;

            records.Sort((a, b) => string.CompareOrdinal(a.Day, b.Day));
            records.RemoveRange(0, records.Count - MaxRecords);
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
