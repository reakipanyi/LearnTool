using LearningAssistant.Common;
using LearningAssistant.Models.Pdf;
using System.Security.Cryptography;
using System.Text;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// AI 摘要条目存储：按用户 + 文档路径哈希落盘 JSON，追加时去重。
    /// </summary>
    public class PdfAiSummaryStore : IPdfAiSummaryStore
    {
        private readonly string? _rootPath;
        private readonly object _syncRoot = new();

        public PdfAiSummaryStore(string? rootPath = null)
        {
            _rootPath = rootPath;
        }

        public string GetStorePath(string pdfPath)
        {
            var root = _rootPath ?? AppPaths.GetUserPdfAiSummaryDir();
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(pdfPath))))[..16];
            return Path.Combine(root, hash + ".json");
        }

        public IReadOnlyList<PdfAiSummaryEntry> GetEntries(string pdfPath)
        {
            if (string.IsNullOrWhiteSpace(pdfPath)) return Array.Empty<PdfAiSummaryEntry>();

            lock (_syncRoot)
            {
                var collection = JsonHelper.LoadFromFile<PdfAiSummaryCollection>(GetStorePath(pdfPath));
                return collection?.Entries ?? new List<PdfAiSummaryEntry>();
            }
        }

        public void AddEntries(string pdfPath, IEnumerable<PdfAiSummaryEntry> entries)
        {
            if (string.IsNullOrWhiteSpace(pdfPath) || entries == null) return;

            lock (_syncRoot)
            {
                var path = GetStorePath(pdfPath);
                var collection = JsonHelper.LoadFromFile<PdfAiSummaryCollection>(path)
                    ?? new PdfAiSummaryCollection { PdfPath = Path.GetFullPath(pdfPath) };

                collection.PdfPath = Path.GetFullPath(pdfPath);
                foreach (var entry in entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Text)) continue;
                    if (collection.Entries.Any(e => IsSameEntry(e, entry))) continue;

                    entry.PdfPath = collection.PdfPath;
                    collection.Entries.Add(entry);
                }

                collection.UpdatedAt = DateTime.Now;
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                JsonHelper.SaveToFile(path, collection);
            }
        }

        public void Clear(string pdfPath)
        {
            if (string.IsNullOrWhiteSpace(pdfPath)) return;

            lock (_syncRoot)
            {
                var path = GetStorePath(pdfPath);
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private static bool IsSameEntry(PdfAiSummaryEntry left, PdfAiSummaryEntry right)
        {
            return left.PageIndex == right.PageIndex
                && left.TimestampMs == right.TimestampMs
                && string.Equals(left.Text, right.Text, StringComparison.Ordinal);
        }
    }
}
