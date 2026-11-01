using System.Text.Json;
using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// 打卡表筛选偏好：按用户目录落盘为单个 JSON。
    /// 目录按当前用户懒解析，单例实例可安全跨用户复用；读取失败时回退默认值，不影响功能。
    /// </summary>
    public sealed class PdfReadingCheckInPreferenceStore
    {
        /// <summary>偏好文件名。</summary>
        public const string FileName = "pdf_checkin_prefs.json";

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private readonly Func<string> _directoryProvider;
        private readonly object _gate = new();

        /// <summary>按当前用户目录懒解析；切换用户后自动读写对应目录。</summary>
        public PdfReadingCheckInPreferenceStore(Func<string> directoryProvider)
        {
            _directoryProvider = directoryProvider ?? throw new ArgumentNullException(nameof(directoryProvider));
        }

        /// <summary>固定目录：测试与单用户场景使用。</summary>
        public PdfReadingCheckInPreferenceStore(string directoryPath)
            : this(CreateFixedProvider(directoryPath))
        {
        }

        private static Func<string> CreateFixedProvider(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("打卡表偏好目录不能为空", nameof(directoryPath));

            string fixedPath = directoryPath;
            return () => fixedPath;
        }

        /// <summary>读取偏好；文件缺失、损坏或目录未配置时返回默认值。</summary>
        public PdfReadingCheckInPreferences Load()
        {
            try
            {
                string directory = ResolveDirectory();
                string file = Path.Combine(directory, FileName);
                if (!File.Exists(file))
                    return new PdfReadingCheckInPreferences();

                var preferences = JsonSerializer.Deserialize<PdfReadingCheckInPreferences>(File.ReadAllText(file));
                return (preferences ?? new PdfReadingCheckInPreferences()).Normalize();
            }
            catch (Exception)
            {
                // 偏好只是辅助信息，读取失败按默认值处理
                return new PdfReadingCheckInPreferences();
            }
        }

        /// <summary>保存偏好；写入失败静默忽略。</summary>
        public void Save(PdfReadingCheckInPreferences preferences)
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
                // 写入失败不影响打卡表使用
            }
        }

        private string ResolveDirectory()
        {
            string directory = _directoryProvider();
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("打卡表偏好目录未配置");

            return directory;
        }
    }
}
