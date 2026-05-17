namespace UnifiedLearningAssistant.Models.Pdf
{
    public class PdfHighlight
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PdfPath { get; set; } = string.Empty;
        public int PageIndex { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public HighlightColor Color { get; set; } = HighlightColor.Yellow;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum HighlightColor
    {
        Yellow,
        Green,
        Blue,
        Pink,
        Orange
    }

    public class PdfHighlightCollection
    {
        public string PdfPath { get; set; } = string.Empty;
        public List<PdfHighlight> Highlights { get; set; } = new List<PdfHighlight>();
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
