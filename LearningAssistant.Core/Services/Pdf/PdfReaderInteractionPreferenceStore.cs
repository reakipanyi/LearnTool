using System.Text.Json;
using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// 阅读器交互偏好：按用户目录落盘为单个 JSON。
    /// 与打卡表偏好一致，目录按当前用户懒解析；读取失败时回退默认值，不影响阅读功能。
    /// </summary>
    public sealed class PdfReaderInteractionPreferenceStore
    {
        /// <summary>偏好文件名。</summary>
        public const string FileName = "pdf_reader_interaction.json";

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private readonly Func<string> _directoryProvider;
        private readonly object _gate = new();

        /// <summary>按当前用户目录懒解析；切换用户后自动读写对应目录。</summary>
        public PdfReaderInteractionPreferenceStore(Func<string> directoryProvider)
        {
            _directoryProvider = directoryProvider ?? throw new ArgumentNullException(nameof(directoryProvider));
        }

        /// <summary>固定目录：测试与单用户场景使用。</summary>
        public PdfReaderInteractionPreferenceStore(string directoryPath)
            : this(CreateFixedProvider(directoryPath))
        {
        }

        private static Func<string> CreateFixedProvider(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("阅读器交互偏好目录不能为空", nameof(directoryPath));

            string fixedPath = directoryPath;
            return () => fixedPath;
        }

        /// <summary>读取偏好；文件缺失、损坏或目录未配置时返回默认值。</summary>
        public PdfReaderInteractionPreferences Load()
        {
            try
            {
                string directory = ResolveDirectory();
                string file = Path.Combine(directory, FileName);
                if (!File.Exists(file))
                    return new PdfReaderInteractionPreferences();

                var preferences = JsonSerializer.Deserialize<PdfReaderInteractionPreferences>(File.ReadAllText(file));
                return (preferences ?? new PdfReaderInteractionPreferences()).Normalize();
            }
            catch (Exception)
            {
                // 偏好只是交互辅助信息，读取失败按默认值处理
                return new PdfReaderInteractionPreferences();
            }
        }

        /// <summary>保存偏好；写入失败静默忽略。</summary>
        public void Save(PdfReaderInteractionPreferences preferences)
        {
            if (preferences == null)
                return;

            try
            {
                string directory = ResolveDirectory();
                lock (_gate)
                {
                    Directory.CreateDirectory(directory);
                    string file = Path.Combine(directory, FileName);
                    string temp = file + ".tmp";
                    File.WriteAllText(temp, JsonSerializer.Serialize(preferences.Normalize(), JsonOptions));
                    File.Move(temp, file, true);
                }
            }
            catch (Exception)
            {
                // 写入失败不影响阅读器使用
            }
        }

        private string ResolveDirectory()
        {
            string directory = _directoryProvider();
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("阅读器交互偏好目录未配置");

            return directory;
        }
    }
}
