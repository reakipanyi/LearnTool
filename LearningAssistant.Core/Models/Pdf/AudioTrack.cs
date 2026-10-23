namespace LearningAssistant.Models.Pdf
{
    public class AudioTrack
    {
        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public string RelativePath { get; set; } = "";
        public int MappedPageStart { get; set; } = -1;
        public int MappedPageEnd { get; set; } = -1;
        public int DurationMs { get; set; }
        public bool IsManual { get; set; }
        /// <summary>是否为整篇单音频模式下选定的音频</summary>
        public bool IsSingleAudio { get; set; }
        public string? SubtitlePath { get; set; }
    }
}