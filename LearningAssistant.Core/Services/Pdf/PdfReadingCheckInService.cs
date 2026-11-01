using LearningAssistant.Models.Pdf;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// 阅读打卡表聚合：按目录分组、统计区间打卡、全量历史连续天数、覆盖率，
    /// 并输出界面 / 打印 / HTML 共用的布局与分页模型。纯计算，无 UI 依赖。
    /// </summary>
    public sealed class PdfReadingCheckInService
    {
        private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;

        private readonly PdfReadingTimeStore _store;
        private readonly ILogger<PdfReadingCheckInService>? _logger;

        public PdfReadingCheckInService(PdfReadingTimeStore store, ILogger<PdfReadingCheckInService>? logger = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = logger;
        }

        /// <summary>构建打卡表报表（含布局模型）；时间区间按自然日。</summary>
        public PdfReadingCheckInReport Build(PdfReadingCheckInRequest request)
        {
            request ??= new PdfReadingCheckInRequest();

            DateTime today = DateTime.Now.Date;
            DateTime start = request.PeriodStart.Date;
            DateTime end = request.PeriodEnd.Date;
            if (end > today)
                end = today;

            int minSeconds = Math.Max(0, request.MinSeconds);
            string root = TryGetFullPath(request.RootPath);
            var days = BuildDays(start, end);

            if (root.Length == 0 || !Directory.Exists(root))
            {
                _logger?.LogWarning("阅读打卡表根目录不可用：{RootPath}", request.RootPath);
                return CreateEmptyReport(root, start, end, minSeconds, days, rootUnavailable: true);
            }

            if (start > end)
                return CreateEmptyReport(root, start, end, minSeconds, days, rootUnavailable: false);

            var periodRecords = _store.GetRecords(start, end);
            var allRecords = _store.GetRecords();

            var periodSecondsByPath = BuildDaySeconds(periodRecords);
            var historySecondsByPath = BuildDaySeconds(allRecords);

            // 根目录外有区间记录的 PDF 只计数，不进入报表
            int outOfScope = CountOutOfScope(root, periodRecords);

            // 1. 候选 PDF：阅读器清单 + 区间记录，均限定当前根目录与 .pdf
            var candidates = new List<string>();
            var seen = new HashSet<string>(PathComparer);
            void AddCandidate(string? path)
            {
                if (!IsPdf(path))
                    return;

                string fullPath = TryGetFullPath(path!);
                if (fullPath.Length == 0 || !IsUnderRoot(root, fullPath))
                    return;

                if (seen.Add(fullPath))
                    candidates.Add(fullPath);
            }

            foreach (var document in request.Documents ?? Array.Empty<string>())
                AddCandidate(document);

            foreach (var record in periodRecords)
                AddCandidate(record.DocPath);

            // 2. 失效文件重命名兜底，并按真实路径合并重复项
            var aggregates = MergeCandidates(candidates, root);

            // 3. 逐 PDF 统计（过滤前保留全量，用于总体连续）
            var rows = new List<PdfCheckInDocumentRow>(aggregates.Count);
            var historyCheckInByPath = new Dictionary<string, HashSet<DateTime>>(PathComparer);
            var overallHistoryCheckInDays = new HashSet<DateTime>();
            int elapsedDays = (end - start).Days + 1;

            foreach (var aggregate in aggregates)
            {
                var daySeconds = new SortedDictionary<DateTime, int>();
                var historyCheckInDays = new HashSet<DateTime>();

                foreach (string sourcePath in aggregate.SourcePaths)
                {
                    if (periodSecondsByPath.TryGetValue(sourcePath, out var periodDays))
                    {
                        foreach (var pair in periodDays)
                        {
                            daySeconds.TryGetValue(pair.Key, out int existing);
                            daySeconds[pair.Key] = existing + pair.Value;
                        }
                    }

                    if (historySecondsByPath.TryGetValue(sourcePath, out var historyDays))
                    {
                        foreach (var pair in historyDays)
                        {
                            if (pair.Value >= minSeconds)
                                historyCheckInDays.Add(pair.Key);
                        }
                    }
                }

                historyCheckInByPath[aggregate.FullPath] = historyCheckInDays;
                overallHistoryCheckInDays.UnionWith(historyCheckInDays);

                var checkInDays = daySeconds
                    .Where(pair => pair.Value >= minSeconds)
                    .Select(pair => pair.Key)
                    .ToList();

                PdfCheckInDocumentStatus status = daySeconds.Count == 0
                    ? PdfCheckInDocumentStatus.Unread
                    : checkInDays.Count > 0
                        ? PdfCheckInDocumentStatus.CheckedIn
                        : PdfCheckInDocumentStatus.UnderThreshold;

                rows.Add(new PdfCheckInDocumentRow
                {
                    FullPath = aggregate.FullPath,
                    FileName = Path.GetFileName(aggregate.FullPath),
                    Exists = aggregate.Exists,
                    RenamedMatched = aggregate.RenamedMatched,
                    Status = status,
                    DaySeconds = daySeconds,
                    CheckInDays = checkInDays.Count,
                    TotalSeconds = daySeconds.Values.Sum(),
                    LastCheckInDay = checkInDays.Count > 0 ? checkInDays.Max() : null,
                    CurrentStreak = ComputeCurrentStreak(historyCheckInDays, end),
                    Coverage = elapsedDays <= 0
                        ? 0
                        : Math.Min(1, checkInDays.Count / (double)elapsedDays)
                });
            }

            // 4. 显示范围过滤
            rows = rows
                .Where(row => request.IncludeUnread || row.Status != PdfCheckInDocumentStatus.Unread)
                .Where(row => request.IncludeMissing || row.Exists)
                .ToList();

            // 5. 目录分组
            var groups = new Dictionary<string, List<PdfCheckInDocumentRow>>(PathComparer);
            foreach (var row in rows)
            {
                string key = row.Exists ? GetRelativeFolder(root, row.FullPath) : PdfCheckInLayoutRules.MissingGroupKey;
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<PdfCheckInDocumentRow>();
                    groups[key] = list;
                }

                list.Add(row);
            }

            var folderGroups = new List<PdfCheckInFolderGroup>(groups.Count);
            foreach (var pair in groups)
            {
                folderGroups.Add(BuildFolderGroup(pair.Key, pair.Value, minSeconds, end, elapsedDays, historyCheckInByPath));
            }

            var comparer = StringComparer.Create(System.Globalization.CultureInfo.CurrentCulture, ignoreCase: true);
            folderGroups.Sort((a, b) =>
            {
                bool aMissing = a.RelativePath == PdfCheckInLayoutRules.MissingGroupKey;
                bool bMissing = b.RelativePath == PdfCheckInLayoutRules.MissingGroupKey;
                if (aMissing != bMissing)
                    return aMissing ? 1 : -1;
                return comparer.Compare(a.DisplayPath, b.DisplayPath);
            });

            // 6. 合计
            var allPeriodCheckInDays = new HashSet<DateTime>();
            int totalSeconds = 0;
            int readDocumentCount = 0;
            foreach (var row in rows)
            {
                totalSeconds += row.TotalSeconds;
                if (row.DaySeconds.Count > 0)
                    readDocumentCount++;
                foreach (var day in row.DaySeconds.Where(pair => pair.Value >= minSeconds))
                    allPeriodCheckInDays.Add(day.Key);
            }

            var totals = new PdfCheckInTotals
            {
                FolderCount = folderGroups.Count,
                DocumentCount = rows.Count,
                ReadDocumentCount = readDocumentCount,
                CheckInDays = allPeriodCheckInDays.Count,
                TotalSeconds = totalSeconds,
                BestStreak = folderGroups.Count == 0 ? 0 : folderGroups.Max(group => group.CurrentStreak),
                OverallCurrentStreak = ComputeCurrentStreak(overallHistoryCheckInDays, end),
                OutOfScopeDocumentCount = outOfScope
            };

            var layout = PdfCheckInLayoutRules.Create(days, folderGroups, today);

            return new PdfReadingCheckInReport
            {
                RootPath = root,
                PeriodStart = start,
                PeriodEnd = end,
                MinSeconds = minSeconds,
                Days = days,
                Folders = folderGroups,
                Totals = totals,
                Layout = layout
            };
        }

        private static PdfReadingCheckInReport CreateEmptyReport(
            string root,
            DateTime start,
            DateTime end,
            int minSeconds,
            IReadOnlyList<DateTime> days,
            bool rootUnavailable)
            => new()
            {
                RootPath = root,
                PeriodStart = start,
                PeriodEnd = end,
                MinSeconds = minSeconds,
                Days = days,
                Folders = Array.Empty<PdfCheckInFolderGroup>(),
                Totals = new PdfCheckInTotals(),
                Layout = PdfCheckInLayoutRules.Create(days, Array.Empty<PdfCheckInFolderGroup>(), DateTime.Now.Date),
                RootUnavailable = rootUnavailable
            };

        private static PdfCheckInFolderGroup BuildFolderGroup(
            string relativePath,
            List<PdfCheckInDocumentRow> documents,
            int minSeconds,
            DateTime baseDay,
            int elapsedDays,
            IReadOnlyDictionary<string, HashSet<DateTime>> historyCheckInByPath)
        {
            documents.Sort((a, b) => StringComparer.Create(
                System.Globalization.CultureInfo.CurrentCulture,
                ignoreCase: true).Compare(a.FileName, b.FileName));

            var unionDaySeconds = new Dictionary<DateTime, int>();
            var historyCheckInDays = new HashSet<DateTime>();
            foreach (var document in documents)
            {
                foreach (var pair in document.DaySeconds)
                {
                    unionDaySeconds.TryGetValue(pair.Key, out int existing);
                    unionDaySeconds[pair.Key] = existing + pair.Value;
                }

                if (historyCheckInByPath.TryGetValue(document.FullPath, out var history))
                    historyCheckInDays.UnionWith(history);
            }

            var checkInDays = unionDaySeconds
                .Where(pair => pair.Value >= minSeconds)
                .Select(pair => pair.Key)
                .ToList();

            bool isMissing = relativePath == PdfCheckInLayoutRules.MissingGroupKey;
            int depth = 0;
            string display;
            if (isMissing)
            {
                display = PdfCheckInLayoutRules.MissingGroupDisplay;
            }
            else if (relativePath.Length == 0)
            {
                display = PdfCheckInLayoutRules.RootFolderDisplay;
            }
            else
            {
                display = relativePath;
                depth = relativePath.Count(c => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar) + 1;
            }

            return new PdfCheckInFolderGroup
            {
                RelativePath = relativePath,
                DisplayPath = display,
                Depth = depth,
                Documents = documents,
                DocumentCount = documents.Count,
                ReadDocumentCount = documents.Count(document => document.DaySeconds.Count > 0),
                CheckInDays = checkInDays.Count,
                TotalSeconds = documents.Sum(document => document.TotalSeconds),
                LastCheckInDay = checkInDays.Count > 0 ? checkInDays.Max() : null,
                CurrentStreak = ComputeCurrentStreak(historyCheckInDays, baseDay)
            };
        }

        /// <summary>当前连续：全量历史回溯，基准日无打卡允许从前一天开始，断签超过 1 天记 0。</summary>
        internal static int ComputeCurrentStreak(IReadOnlySet<DateTime> checkInDays, DateTime baseDay)
        {
            if (checkInDays.Count == 0)
                return 0;

            DateTime baseDate = baseDay.Date;
            DateTime? last = null;
            foreach (var day in checkInDays.OrderByDescending(value => value))
            {
                if (day.Date <= baseDate)
                {
                    last = day.Date;
                    break;
                }
            }

            if (last == null)
                return 0;

            if ((baseDate - last.Value).Days > 1)
                return 0;

            int streak = 0;
            DateTime cursor = last.Value;
            while (checkInDays.Contains(cursor))
            {
                streak++;
                cursor = cursor.AddDays(-1);
            }

            return streak;
        }

        private static List<DateTime> BuildDays(DateTime start, DateTime end)
        {
            if (start > end)
                return new List<DateTime>();

            var days = new List<DateTime>((end - start).Days + 1);
            for (var day = start; day <= end; day = day.AddDays(1))
                days.Add(day.Date);
            return days;
        }

        private static Dictionary<string, Dictionary<DateTime, int>> BuildDaySeconds(
            IEnumerable<PdfReadingTimeRecord> records)
        {
            var result = new Dictionary<string, Dictionary<DateTime, int>>(PathComparer);
            foreach (var record in records)
            {
                if (!IsPdf(record.DocPath) || record.Seconds <= 0)
                    continue;

                string path = TryGetFullPath(record.DocPath);
                if (path.Length == 0 || !TryParseDay(record.Day, out var day))
                    continue;

                if (!result.TryGetValue(path, out var days))
                {
                    days = new Dictionary<DateTime, int>();
                    result[path] = days;
                }

                days.TryGetValue(day, out int existing);
                days[day] = existing + record.Seconds;
            }

            return result;
        }

        private static int CountOutOfScope(string root, IEnumerable<PdfReadingTimeRecord> periodRecords)
        {
            var paths = new HashSet<string>(PathComparer);
            foreach (var record in periodRecords)
            {
                if (!IsPdf(record.DocPath) || record.Seconds <= 0)
                    continue;

                string path = TryGetFullPath(record.DocPath);
                if (path.Length > 0 && !IsUnderRoot(root, path))
                    paths.Add(path);
            }

            return paths.Count;
        }

        private sealed class DocumentAggregate
        {
            public DocumentAggregate(string fullPath, bool exists, bool renamedMatched)
            {
                FullPath = fullPath;
                Exists = exists;
                RenamedMatched = renamedMatched;
            }

            public string FullPath { get; }

            public bool Exists { get; set; }

            public bool RenamedMatched { get; set; }

            public List<string> SourcePaths { get; } = new();
        }

        private static List<DocumentAggregate> MergeCandidates(List<string> candidates, string root)
        {
            var result = new Dictionary<string, DocumentAggregate>(PathComparer);
            foreach (string candidate in candidates)
            {
                string fullPath = candidate;
                bool exists = File.Exists(candidate);
                bool renamed = false;

                if (!exists)
                {
                    string? matched = TryFindRenamedFile(candidate, root);
                    if (matched != null)
                    {
                        fullPath = matched;
                        exists = true;
                        renamed = true;
                    }
                }

                if (result.TryGetValue(fullPath, out var aggregate))
                {
                    aggregate.Exists |= exists;
                    aggregate.RenamedMatched |= renamed;
                }
                else
                {
                    aggregate = new DocumentAggregate(fullPath, exists, renamed);
                    result[fullPath] = aggregate;
                }

                aggregate.SourcePaths.Add(candidate);
            }

            return result.Values.ToList();
        }

        /// <summary>失效路径兜底：先同级目录唯一匹配，再根目录内全局唯一匹配；多候选保持失效。</summary>
        private static string? TryFindRenamedFile(string missingPath, string root)
        {
            try
            {
                string fileName = Path.GetFileName(missingPath);
                if (fileName.Length == 0)
                    return null;

                string? directory = Path.GetDirectoryName(missingPath);
                string? parent = directory == null ? null : Path.GetDirectoryName(directory);
                if (parent != null && Directory.Exists(parent))
                {
                    string? sibling = FindUniqueInSiblings(parent, fileName);
                    if (sibling != null)
                        return sibling;
                }

                return FindUniqueFile(root, fileName, recursive: true);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>在父目录的各同级子目录中按文件名唯一匹配。</summary>
        private static string? FindUniqueInSiblings(string parentDirectory, string fileName)
        {
            try
            {
                string? found = null;
                foreach (string sibling in Directory.EnumerateDirectories(parentDirectory))
                {
                    foreach (string file in Directory.EnumerateFiles(sibling, fileName, SearchOption.TopDirectoryOnly))
                    {
                        if (!string.Equals(Path.GetFileName(file), fileName, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (found != null && !string.Equals(found, file, StringComparison.OrdinalIgnoreCase))
                            return null;

                        found = file;
                    }
                }

                return found;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string? FindUniqueFile(string directory, string fileName, bool recursive)
        {
            try
            {
                string? found = null;
                var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                foreach (string file in Directory.EnumerateFiles(directory, fileName, option))
                {
                    if (!string.Equals(Path.GetFileName(file), fileName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (found != null && !string.Equals(found, file, StringComparison.OrdinalIgnoreCase))
                        return null;

                    found = file;
                }

                return found;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GetRelativeFolder(string root, string fullPath)
        {
            try
            {
                string? directory = Path.GetDirectoryName(fullPath);
                if (directory == null)
                    return string.Empty;

                string relative = Path.GetRelativePath(root, directory);
                if (relative.Length == 0 || relative == "." || Path.IsPathRooted(relative))
                    return string.Empty;

                if (relative == ".."
                    || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                    || relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
                {
                    return string.Empty;
                }

                return relative;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static bool IsUnderRoot(string root, string fullPath)
        {
            if (root.Length == 0 || fullPath.Length == 0)
                return false;

            try
            {
                string relative = Path.GetRelativePath(root, fullPath);
                if (Path.IsPathRooted(relative))
                    return false;

                return relative != ".."
                    && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                    && !relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsPdf(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                return string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TryParseDay(string? day, out DateTime value)
            => DateTime.TryParseExact(
                day,
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out value);

        private static string TryGetFullPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            try
            {
                return Path.GetFullPath(path);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
