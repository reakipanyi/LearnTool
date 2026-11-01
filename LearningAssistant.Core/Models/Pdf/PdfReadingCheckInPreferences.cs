namespace LearningAssistant.Models.Pdf
{
    /// <summary>打卡表显示范围。</summary>
    public enum PdfCheckInDisplayScope
    {
        /// <summary>仅显示区间内有阅读记录的 PDF（默认）。</summary>
        RecordedOnly,

        /// <summary>显示根目录内全部 PDF（含未读，不含已失效）。</summary>
        All,

        /// <summary>显示全部 PDF，并保留已失效文件记录。</summary>
        AllIncludingMissing
    }

    /// <summary>打卡表筛选与展示偏好，按用户持久化。</summary>
    public sealed class PdfReadingCheckInPreferences
    {
        /// <summary>有效打卡阈值默认值（秒）。</summary>
        public const int DefaultMinSeconds = 60;

        /// <summary>阈值下限（秒）。</summary>
        public const int MinAllowedSeconds = 1;

        /// <summary>阈值上限（秒）。</summary>
        public const int MaxAllowedSeconds = 3600;

        /// <summary>有效打卡阈值（秒）。</summary>
        public int MinSeconds { get; set; } = DefaultMinSeconds;

        /// <summary>显示范围。</summary>
        public PdfCheckInDisplayScope DisplayScope { get; set; } = PdfCheckInDisplayScope.RecordedOnly;

        /// <summary>矩阵单元格显示模式。</summary>
        public PdfCheckInCellDisplayMode DisplayMode { get; set; } = PdfCheckInCellDisplayMode.CheckMark;

        /// <summary>
        /// 已展开的目录键（相对根目录的路径；失效文件组为 "__missing__"）。
        /// 默认全部折叠，只保留目录汇总行。
        /// </summary>
        public List<string> ExpandedFolderKeys { get; set; } = new();

        /// <summary>把外部读入的值收敛到合法范围，避免脏数据影响界面。</summary>
        public PdfReadingCheckInPreferences Normalize()
        {
            var scope = Enum.IsDefined(DisplayScope)
                ? DisplayScope
                : PdfCheckInDisplayScope.RecordedOnly;
            var mode = Enum.IsDefined(DisplayMode)
                ? DisplayMode
                : PdfCheckInCellDisplayMode.CheckMark;

            // 根层目录的相对路径是空串，需要保留；纯空白项按脏数据丢弃
            var rawKeys = ExpandedFolderKeys ?? new List<string>();
            var keys = rawKeys
                .Where(key => key != null && (key.Length == 0 || !string.IsNullOrWhiteSpace(key)))
                .Select(key => key.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new PdfReadingCheckInPreferences
            {
                MinSeconds = Math.Clamp(MinSeconds, MinAllowedSeconds, MaxAllowedSeconds),
                DisplayScope = scope,
                DisplayMode = mode,
                ExpandedFolderKeys = keys
            };
        }
    }
}
