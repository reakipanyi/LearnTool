namespace LearningAssistant.Models.Pdf
{
    /// <summary>
    /// 录音用途：整页跟读或句子跟读。用于让不同消费方过滤掉不属于自己的录音。
    /// </summary>
    public enum AudioRecordingScope
    {
        Page,
        Sentence,
        /// <summary>单词跟读（P2）：与整页/句子跟读隔离，避免污染既有录音列表。</summary>
        WordShadowing
    }

    public class AudioRecording
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string PdfPath { get; set; } = string.Empty;
        public string PdfFileName { get; set; } = string.Empty;
        public int PageIndex { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public int DurationMs { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? Label { get; set; }

        /// <summary>
        /// 录音来源，默认整页跟读，兼容旧录音数据。
        /// </summary>
        public AudioRecordingScope Scope { get; set; } = AudioRecordingScope.Page;

        /// <summary>
        /// 句子跟读时对应的句子 Id；整页录音为 null。
        /// </summary>
        public string? SentenceId { get; set; }
    }
}
