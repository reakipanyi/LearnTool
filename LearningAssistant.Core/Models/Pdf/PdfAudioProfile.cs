namespace LearningAssistant.Models.Pdf
{
    public class PdfAudioProfile
    {
        public string PdfPath { get; set; } = "";
        /// <summary>匹配方式：按页多音频（默认）或整篇单音频</summary>
        public PdfAudioMatchMode MatchMode { get; set; } = PdfAudioMatchMode.PerPage;
        /// <summary>整篇单音频模式下选定的音频；PDF 同目录内存相对路径，目录外存绝对路径</summary>
        public string? SingleAudioFile { get; set; }
        /// <summary>当前生效规则：目录规则按 RulePatch 合并后的结果</summary>
        public List<AudioMatchRule> Rules { get; set; } = new();
        /// <summary>目录共享规则原文；Rules 为其与 RulePatch 合并后的结果</summary>
        public List<AudioMatchRule> DirectoryRules { get; set; } = new();
        /// <summary>书级规则增量补丁；null 表示完全继承目录规则</summary>
        public PdfAudioRulePatch? RulePatch { get; set; }
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
        /// <summary>用户是否手动选择过播放范围；未选过时打开书籍默认识别整篇音频并入「整篇听」</summary>
        public bool ScopeExplicit { get; set; }
        /// <summary>翻页是否自动切换音频（与“切歌同步页码”相互独立）</summary>
        public bool FollowPage { get; set; } = true;
        /// <summary>上一曲/下一曲或列表选曲后，正文是否跳到该曲目起始页</summary>
        public bool SyncPageOnTrackChange { get; set; } = true;
        /// <summary>书级播放倍速（0.5–2.0），按书记忆；默认继承目录默认值</summary>
        public double PlaybackSpeed { get; set; } = 1.0;
        /// <summary>上次播放到的位置（秒），按书记忆，换曲或关闭时写回</summary>
        public double LastPositionSeconds { get; set; }
        /// <summary>上次播放的曲目标识（相对路径优先），用于恢复进度时确认是同一曲目</summary>
        public string? LastTrackPath { get; set; }
    }
}