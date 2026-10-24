namespace LearningAssistant.Models.Pdf
{
    /// <summary>目录级匹配档案（schemaVersion 3）：规则共享、书级配置分区。</summary>
    public class PdfAudioDirectoryProfile
    {
        public int SchemaVersion { get; set; } = 3;
        public PdfAudioDirectoryScope Scope { get; set; } = new();
        public PdfAudioDirectoryDefaults Defaults { get; set; } = new();
        /// <summary>扫描来源：固定含 "audio"（PDF 同目录），其余为外部目录绝对路径</summary>
        public List<string> Sources { get; set; } = new();
        public List<AudioMatchRule> Rules { get; set; } = new();
        /// <summary>文件名 → 书级分区；查找不区分大小写</summary>
        public Dictionary<string, PdfAudioBookProfile> Books { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
        /// <summary>从旧版一书一档迁移时写入 "v2"，避免重复迁移</summary>
        public string? MigratedFrom { get; set; }
    }

    public class PdfAudioDirectoryScope
    {
        public string Kind { get; set; } = "directory";
        public string Id { get; set; } = "";
        public string Path { get; set; } = "";
        public string PathKey { get; set; } = "";
        /// <summary>目录内 PDF 文件名 + 大小的哈希，只用于候选排序</summary>
        public string Signature { get; set; } = "";
    }

    public class PdfAudioDirectoryDefaults
    {
        public PlaybackScope PlaybackScope { get; set; } = PlaybackScope.FollowPage;
        public bool IncludeSubDirectories { get; set; }
        public double PlaybackSpeed { get; set; } = 1.0;
    }

    /// <summary>书级分区：同一目录内每本书一份。</summary>
    public class PdfAudioBookProfile
    {
        public string? QuickFingerprint { get; set; }
        public string? FullFingerprint { get; set; }
        public int PageCount { get; set; }
        /// <summary>整篇单音频的唯一真相；非空即 WholeBook</summary>
        public string? SingleAudio { get; set; }
        public PlaybackScope PlaybackScope { get; set; } = PlaybackScope.FollowPage;
        /// <summary>用户是否手动选择过播放范围；未选过时打开书籍默认识别整篇音频并入「整篇听」</summary>
        public bool ScopeExplicit { get; set; }
        /// <summary>书级规则增量补丁；null 表示完全继承目录规则</summary>
        public PdfAudioRulePatch? RulePatch { get; set; }
        public Dictionary<int, PdfAudioOverride> Overrides { get; set; } = new();
        public bool FollowPage { get; set; } = true;
        public bool SyncPageOnTrackChange { get; set; } = true;
        /// <summary>书级倍速覆盖；null 表示继承目录 defaults.playbackSpeed</summary>
        public double? PlaybackSpeed { get; set; }
        /// <summary>按书记忆的播放位置（秒）</summary>
        public double PositionSeconds { get; set; }
        /// <summary>按书记忆的曲目标识（相对路径优先）</summary>
        public string? LastTrack { get; set; }
        public PdfAudioBookCache? Cache { get; set; }
    }

    public class PdfAudioRulePatch
    {
        public List<string> DisabledRuleIds { get; set; } = new();
        public List<AudioMatchRule> ExtraRules { get; set; } = new();
        public bool ReplaceAll { get; set; }
    }

    public class PdfAudioOverride
    {
        public string Path { get; set; } = "";
        /// <summary>manual 或 ai；用于 AI 结果一键清除</summary>
        public string Source { get; set; } = "manual";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class PdfAudioBookCache
    {
        public Dictionary<int, string> PageIndex { get; set; } = new();
        public string ScanFingerprint { get; set; } = "";
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
