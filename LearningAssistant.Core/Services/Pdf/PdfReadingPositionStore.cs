using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>某份 PDF 的连续朗读断点。</summary>
    public sealed class PdfReadingPosition
    {
        /// <summary>页号（0 基）。</summary>
        public int PageIndex { get; set; }

        /// <summary>页内句子序号（0 基）。</summary>
        public int SentenceIndex { get; set; }

        /// <summary>最近一次更新时间（UTC）。</summary>
        public DateTime UpdatedAtUtc { get; set; }
    }

    /// <summary>
    /// 按“文档路径 + 修改时间 + 文件大小”存放连续朗读断点，
    /// 换文档或文件被改动后自然失效，避免断点串位。
    /// 目录按当前用户懒解析，单例实例可安全跨用户复用。
    /// </summary>
    public sealed class PdfReadingPositionStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

        private readonly Func<string> _directoryProvider;

        /// <summary>按当前用户目录懒解析；切换用户后自动读写对应目录。</summary>
        public PdfReadingPositionStore(Func<string> directoryProvider)
        {
            _directoryProvider = directoryProvider ?? throw new ArgumentNullException(nameof(directoryProvider));
        }

        /// <summary>固定目录：测试与单用户场景使用。</summary>
        public PdfReadingPositionStore(string directoryPath)
            : this(CreateFixedProvider(directoryPath))
        {
        }

        private static Func<string> CreateFixedProvider(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("断点目录不能为空", nameof(directoryPath));

            string fixedPath = directoryPath;
            return () => fixedPath;
        }

        private string ResolveDirectory()
        {
            string directory = _directoryProvider();
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("断点目录未配置");

            return directory;
        }

        /// <summary>读取断点；不存在或内容损坏时返回 null。</summary>
        public PdfReadingPosition? Load(string? pdfPath)
        {
            if (string.IsNullOrWhiteSpace(pdfPath))
                return null;

            try
            {
                string file = GetFilePath(pdfPath);
                if (!File.Exists(file))
                    return null;

                var position = JsonSerializer.Deserialize<PdfReadingPosition>(File.ReadAllText(file));
                if (position == null || position.PageIndex < 0 || position.SentenceIndex < 0)
                    return null;
                return position;
            }
            catch (Exception)
            {
                // 断点只是辅助信息，读取失败不应影响打开文档
                return null;
            }
        }

        /// <summary>保存断点；写入失败时静默忽略。</summary>
        public void Save(string? pdfPath, int pageIndex, int sentenceIndex)
        {
            if (string.IsNullOrWhiteSpace(pdfPath) || pageIndex < 0 || sentenceIndex < 0)
                return;

            try
            {
                string directory = ResolveDirectory();
                Directory.CreateDirectory(directory);
                var position = new PdfReadingPosition
                {
                    PageIndex = pageIndex,
                    SentenceIndex = sentenceIndex,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                File.WriteAllText(GetFilePath(pdfPath, directory), JsonSerializer.Serialize(position, JsonOptions));
            }
            catch (Exception)
            {
                // 断点保存失败不影响朗读
            }
        }

        /// <summary>清除断点。</summary>
        public void Clear(string? pdfPath)
        {
            if (string.IsNullOrWhiteSpace(pdfPath))
                return;

            try
            {
                string file = GetFilePath(pdfPath);
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch (Exception)
            {
                // 忽略删除失败
            }
        }

        private string GetFilePath(string pdfPath)
            => GetFilePath(pdfPath, ResolveDirectory());

        private static string GetFilePath(string pdfPath, string directory)
            => Path.Combine(directory, ComputeKey(pdfPath) + ".json");

        private static string ComputeKey(string pdfPath)
        {
            string fullPath = pdfPath;
            long length = 0;
            long ticks = 0;
            try
            {
                var info = new FileInfo(pdfPath);
                if (info.Exists)
                {
                    fullPath = info.FullName;
                    length = info.Length;
                    ticks = info.LastWriteTimeUtc.Ticks;
                }
            }
            catch (Exception)
            {
                // 取不到文件信息时退回路径本身
            }

            string raw = $"{fullPath}|{ticks}|{length}";
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(hash);
        }
    }
}
