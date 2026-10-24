namespace LearningAssistant.Models.Pdf
{
    public class PdfAudioProfile
    {
        public string PdfPath { get; set; } = "";
        /// <summary>匹配方式：按页多音频（默认）或整篇单音频</summary>
        public PdfAudioMatchMode MatchMode { get; set; } = PdfAudioMatchMode.PerPage;
        /// <summary>整篇单音频模式下选定的音频；PDF 同目录内存相对路径，目录外存绝对路径</summary>
        public string? SingleAudioFile { get; set; }
        public List<AudioMatchRule> Rules { get; set; } = new();
        public Dictionary<int, string> ManualOverrides { get; set; } = new();
        /// <summary>页 → 音频相对路径的匹配结果缓存；用于指纹命中时恢复正则匹配，避免重开后丢失</summary>
        public Dictionary<int, string> PageIndex { get; set; } = new();
        public List<AudioTrack> Tracks { get; set; } = new();
        /// <summary>额外合并进来的外部音频目录（绝对路径），重新打开 PDF 时仍会扫描</summary>
        public List<string> ExternalAudioDirectories { get; set; } = new();
        public bool IncludeSubDirectories { get; set; }
        public string ScanFingerprint { get; set; } = "";
        /// <summary>生成匹配结果时的页数；页数变化时需重新匹配</summary>
        public int PageCount { get; set; }
        /// <summary>播放范围：跟随翻页或整篇单音频；与 MatchMode 冲突时打开时自动纠正</summary>
        public PlaybackScope PlaybackScope { get; set; } = PlaybackScope.FollowPage;
        /// <summary>翻页是否自动切换音频（与“切歌同步页码”相互独立）</summary>
        public bool FollowPage { get; set; } = true;
        /// <summary>上一曲/下一曲或列表选曲后，正文是否跳到该曲目起始页</summary>
        public bool SyncPageOnTrackChange { get; set; } = true;
    }
}