namespace UnifiedLearningAssistant.Models.Pdf
{
    public class PdfBookmark
    {
        public string PdfPath { get; set; } = string.Empty;
        public int PageIndex { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}