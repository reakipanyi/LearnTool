namespace UnifiedLearningAssistant.Models.Pdf
{
    public class PdfUserSession
    {
        public string UserId { get; set; } = string.Empty;
        public string LastPdfPath { get; set; } = string.Empty;
        public int LastPageIndex { get; set; } = 0;
        public string LastFolderPath { get; set; } = string.Empty;
        public DateTime LastAccessTime { get; set; } = DateTime.Now;
    }
}