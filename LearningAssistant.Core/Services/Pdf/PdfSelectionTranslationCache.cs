using LearningAssistant.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// PDF 选区翻译本地缓存实现：一条缓存一个文件，按文档指纹/页码/OCR 文本哈希存放。
    /// 单条目单文件可避免多阅读窗口同时写入时互相覆盖。
    /// </summary>
    public sealed class PdfSelectionTranslationCache : IPdfSelectionTranslationCache
    {
        private const string CacheVersion = "selection-translation-v1";
        private const int MaxEntriesPerPage = 200;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
        private static readonly Regex ZeroWidthRegex = new(@"[\u200B-\u200D\uFEFF]", RegexOptions.Compiled);

        private readonly string _rootPath;

        public PdfSelectionTranslationCache(string? rootPath = null)
        {
            _rootPath = string.IsNullOrWhiteSpace(rootPath) ? AppPaths.PdfSelectionTranslationDir : rootPath;
        }

        /// <summary>规范化 OCR 文本：全半角统一、去零宽字符、折叠空白。</summary>
        public static string NormalizeText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalized = text.Normalize(NormalizationForm.FormKC);
            normalized = ZeroWidthRegex.Replace(normalized, string.Empty);
            normalized = WhitespaceRegex.Replace(normalized, " ").Trim();
            return normalized;
        }

        public string BuildKey(string pdfPath, int pageIndex, string ocrText, string targetLanguage)
        {
            var normalizedText = NormalizeText(ocrText);
            var textHash = ComputeHash($"{normalizedText}|{targetLanguage}|{CacheVersion}");
            var documentHash = GetDocumentHash(pdfPath);
            return ComputeHash($"{documentHash}|{pageIndex}|{textHash}");
        }

        public bool TryGet(
            string pdfPath,
            int pageIndex,
            string ocrText,
            string targetLanguage,
            out string translation)
        {
            translation = string.Empty;
            if (string.IsNullOrWhiteSpace(pdfPath) || pageIndex < 0)
                return false;

            var path = GetEntryPath(pdfPath, pageIndex, ocrText, targetLanguage);
            if (!File.Exists(path))
                return false;

            var entry = TryReadEntry(path);
            if (entry == null || string.IsNullOrWhiteSpace(entry.Translation))
                return false;

            translation = entry.Translation;
            return true;
        }

        public void Set(
            string pdfPath,
            int pageIndex,
            string ocrText,
            string targetLanguage,
            string translation)
        {
            targetLanguage ??= string.Empty;
            if (string.IsNullOrWhiteSpace(pdfPath) || pageIndex < 0)
                return;

            var normalizedText = NormalizeText(ocrText);
            if (string.IsNullOrWhiteSpace(normalizedText) || string.IsNullOrWhiteSpace(translation))
                return;

            var entry = new PdfSelectionTranslationEntry
            {
                OriginalText = ocrText.Trim(),
                Translation = translation.Trim(),
                TargetLanguage = targetLanguage ?? string.Empty,
                PageIndex = pageIndex,
                TextHash = ComputeHash($"{normalizedText}|{targetLanguage}|{CacheVersion}"),
                CreatedAtUtc = DateTime.UtcNow
            };

            var path = GetEntryPath(pdfPath, pageIndex, ocrText, targetLanguage);
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
                File.WriteAllText(tempPath, JsonSerializer.Serialize(entry, JsonOptions), new UTF8Encoding(false));
                File.Move(tempPath, path, true);
                PrunePage(pdfPath, pageIndex);
            }
            catch (IOException)
            {
                // 缓存写入失败不影响主流程，下一次选择会重新请求。
            }
            catch (UnauthorizedAccessException)
            {
                // 缓存目录不可写时静默降级。
            }
        }

        public PdfSelectionTranslationEntry? GetLatest(
            string pdfPath,
            int pageIndex,
            string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(pdfPath) || pageIndex < 0)
                return null;

            var pageDirectory = GetPageDirectory(pdfPath, pageIndex);
            if (!Directory.Exists(pageDirectory))
                return null;

            PdfSelectionTranslationEntry? latest = null;
            try
            {
                foreach (var file in Directory.EnumerateFiles(pageDirectory, "*.json"))
                {
                    var entry = TryReadEntry(file);
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Translation))
                        continue;

                    if (!string.Equals(entry.TargetLanguage, targetLanguage, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (latest == null || entry.CreatedAtUtc > latest.CreatedAtUtc)
                        latest = entry;
                }
            }
            catch (IOException)
            {
                return latest;
            }
            catch (UnauthorizedAccessException)
            {
                return latest;
            }

            return latest;
        }

        private string GetEntryPath(string pdfPath, int pageIndex, string ocrText, string targetLanguage)
        {
            var textHash = ComputeHash($"{NormalizeText(ocrText)}|{targetLanguage}|{CacheVersion}");
            return Path.Combine(GetPageDirectory(pdfPath, pageIndex), $"{textHash}.json");
        }

        private string GetPageDirectory(string pdfPath, int pageIndex)
        {
            return Path.Combine(_rootPath, GetDocumentHash(pdfPath), $"p{pageIndex:D4}");
        }

        private static PdfSelectionTranslationEntry? TryReadEntry(string path)
        {
            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                return JsonSerializer.Deserialize<PdfSelectionTranslationEntry>(json, JsonOptions);
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private void PrunePage(string pdfPath, int pageIndex)
        {
            var pageDirectory = GetPageDirectory(pdfPath, pageIndex);
            if (!Directory.Exists(pageDirectory))
                return;

            try
            {
                var files = Directory.EnumerateFiles(pageDirectory, "*.json")
                    .Select(path => new FileInfo(path))
                    .OrderByDescending(info => info.LastWriteTimeUtc)
                    .Skip(MaxEntriesPerPage)
                    .ToList();

                foreach (var file in files)
                    file.Delete();
            }
            catch (IOException)
            {
                // 淘汰失败不阻断缓存写入。
            }
            catch (UnauthorizedAccessException)
            {
                // 同上。
            }
        }

        private static string GetDocumentHash(string pdfPath)
        {
            long length = 0;
            long ticks = 0;
            try
            {
                var info = new FileInfo(Path.GetFullPath(pdfPath));
                if (info.Exists)
                {
                    length = info.Length;
                    ticks = info.LastWriteTimeUtc.Ticks;
                }
            }
            catch (Exception)
            {
                // 文件不可访问时退化为仅按路径缓存。
            }

            return ComputeHash($"{Path.GetFullPath(pdfPath)}|{length}|{ticks}")[..16];
        }

        private static string ComputeHash(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
