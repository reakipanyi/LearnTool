namespace LearningAssistant.Models.Pdf
{
    public class PdfAudioProfile
    {
        public string PdfPath { get; set; } = "";
        public List<AudioMatchRule> Rules { get; set; } = new();
        public Dictionary<int, string> ManualOverrides { get; set; } = new();
        /// <summary>页 → 音频相对路径的匹配结果缓存；用于指纹命中时恢复正则匹配，避免重开后丢失</summary>
        public Dictionary<int, string> PageIndex { get; set; } = new();
        public List<AudioTrack> Tracks { get; set; } = new();
        public bool IncludeSubDirectories { get; set; }
        public string ScanFingerprint { get; set; } = "";
        /// <summary>生成匹配结果时的页数；页数变化时需重新匹配</summary>
        public int PageCount { get; set; }
    }
}