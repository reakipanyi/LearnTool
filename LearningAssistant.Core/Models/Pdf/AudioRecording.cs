namespace LearningAssistant.Models.Pdf
{
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
    }
}