namespace LearningAssistant.Models.Pdf
{
    public class PdfBookmark
    {
        public string PdfPath { get; set; } = string.Empty;
        public int PageIndex { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public override string ToString()
        {
            var pageNum = PageIndex + 1;
            return !string.IsNullOrEmpty(Title) 
                ? $"{pageNum}页 - {Title}" 
                : $"{pageNum}页 - 书签";
        }
    }
}